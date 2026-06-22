using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class ProductionSystem : ISimulationSystem
    {
        public const string ConfigureProductionCommand = "command:core:configure_production";
        public const string StartProductionCommand = "command:core:start_production";
        public const string CancelProductionCommand = "command:core:cancel_production";
        public const string ProductionConfiguredEvent = "event:core:production_configured";
        public const string ProductionBatchStartedEvent = "event:core:production_batch_started";
        public const string ProductionProgressedEvent = "event:core:production_progressed";
        public const string ProductionPausedEvent = "event:core:production_paused";
        public const string ProductionBatchCompletedEvent = "event:core:production_batch_completed";
        public const string ProductionOutputTransferredEvent = "event:core:production_output_transferred";
        public const string ProductionBatchCancelledEvent = "event:core:production_batch_cancelled";
        public const string ProductionInventoryLostEvent = "event:core:production_inventory_lost";

        private readonly JsonSerializerOptions _jsonOptions;

        public ProductionSystem(JsonSerializerOptions jsonOptions = null)
        {
            _jsonOptions = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
        }

        public void RegisterCommands(CommandBus commandBus)
        {
            commandBus.Register(new ConfigureProductionHandler(_jsonOptions));
            commandBus.Register(new StartProductionHandler(_jsonOptions));
            commandBus.Register(new CancelProductionHandler(_jsonOptions));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            List<GameEvent> events = new List<GameEvent>();
            List<ProductionSlotState> slots = context.State.Production.SlotsByBuildingId.Values
                .Where(slot => slot != null)
                .OrderBy(slot => slot.BuildingId, StringComparer.Ordinal)
                .ToList();

            for (int i = 0; i < slots.Count; i++)
            {
                ProductionSlotState slot = slots[i];
                if (slot.HasBufferedOutput)
                {
                    if (!TryTransferOutput(context, slot, events))
                    {
                        slot.Status = ProductionSlotStatuses.OutputPending;
                        continue;
                    }
                }

                if (slot.HasActiveBatch)
                {
                    ProgressBatch(context, slot, deltaTicks, events);
                    continue;
                }

                if (slot.Continuous && !string.IsNullOrEmpty(slot.RecipeId))
                {
                    if (!TryStartBatch(context, slot, events, out _, out _))
                    {
                        slot.Status = ProductionSlotStatuses.Waiting;
                    }
                }
                else if (!string.IsNullOrEmpty(slot.RecipeId))
                {
                    slot.Status = ProductionSlotStatuses.Idle;
                }
            }

            return events;
        }

        public static ValidationResult ValidateDemolition(GameState state, string buildingId)
        {
            if (state.Buildings.Instances.TryGetValue(buildingId, out BuildingInstanceState building) &&
                building.LocalInventory.Values.Any(stack => stack != null && stack.Amount > 0))
            {
                return ValidationResult.Invalid("Building local inventory must be emptied before demolition.", CommandErrorCodes.ProductionOutputPending);
            }

            if (!state.Production.SlotsByBuildingId.TryGetValue(buildingId, out ProductionSlotState slot) || slot == null)
            {
                return ValidationResult.Valid();
            }

            if (slot.HasBufferedOutput)
            {
                return ValidationResult.Invalid("Building has production output waiting for transfer.", CommandErrorCodes.ProductionOutputPending);
            }

            if (slot.HasActiveBatch)
            {
                return ValidationResult.Invalid("Building has an active production batch.", CommandErrorCodes.ProductionBatchActive);
            }

            return ValidationResult.Valid();
        }

        public static void HandleBuildingDestroyed(SimulationContext context, string buildingId, List<GameEvent> events)
        {
            if (!context.State.Buildings.Instances.TryGetValue(buildingId, out BuildingInstanceState building) || building == null)
            {
                return;
            }

            context.State.Production.SlotsByBuildingId.TryGetValue(buildingId, out ProductionSlotState slot);
            if (slot != null && slot.HasActiveBatch)
            {
                UnlockInputs(context.State, building, slot);
                events.Add(context.Events.Create(ProductionBatchCancelledEvent, "system:core:production", new ProductionBatchPayload
                {
                    BuildingId = buildingId,
                    RecipeId = slot.RecipeId,
                    BatchId = slot.ActiveBatchId,
                    Reason = "building_destroyed"
                }));
            }

            Dictionary<string, int> stranded = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, LocalResourceStack> pair in building.LocalInventory)
            {
                if (pair.Value != null && pair.Value.Amount > 0)
                {
                    stranded[pair.Key] = pair.Value.Amount;
                }
            }
            foreach (KeyValuePair<string, int> pair in slot?.OutputBuffer ?? new Dictionary<string, int>())
            {
                stranded[pair.Key] = checked((stranded.TryGetValue(pair.Key, out int amount) ? amount : 0) + pair.Value);
            }

            if (stranded.Count > 0)
            {
                bool transferred = TryTransferOutputToGlobal(context.State.Resources, stranded);
                events.Add(context.Events.Create(
                    transferred ? ProductionOutputTransferredEvent : ProductionInventoryLostEvent,
                    "system:core:production",
                    new ProductionOutputPayload { BuildingId = buildingId, Outputs = stranded }));
            }

            building.LocalInventory.Clear();
            slot?.OutputBuffer.Clear();

            context.State.Production.SlotsByBuildingId.Remove(buildingId);
        }

        private static void ProgressBatch(
            SimulationContext context,
            ProductionSlotState slot,
            long deltaTicks,
            List<GameEvent> events)
        {
            if (!TryGetOperationalRecipeAndWorkers(context, slot, out RecipeDefinition recipe, out int workerCount))
            {
                if (!StringComparer.Ordinal.Equals(slot.Status, ProductionSlotStatuses.Paused))
                {
                    events.Add(context.Events.Create(ProductionPausedEvent, "system:core:production", new ProductionBatchPayload
                    {
                        BuildingId = slot.BuildingId,
                        RecipeId = slot.RecipeId,
                        BatchId = slot.ActiveBatchId,
                        Reason = "conditions_unavailable"
                    }));
                }

                slot.Status = ProductionSlotStatuses.Paused;
                return;
            }

            int efficiencyBasisPoints = WasteEffectRules.ResolveWorkEfficiencyBasisPoints(context.State);
            long rawWorkTicks = checked(deltaTicks * workerCount);
            int efficiencyRemainder = slot.EfficiencyRemainderBasisPointTicks;
            long addedWork = WasteEffectRules.ApplyEfficiency(
                rawWorkTicks,
                efficiencyBasisPoints,
                ref efficiencyRemainder);
            slot.EfficiencyRemainderBasisPointTicks = efficiencyRemainder;
            slot.ProgressWorkTicks = Math.Min(slot.RequiredWorkTicks, checked(slot.ProgressWorkTicks + addedWork));
            slot.Status = ProductionSlotStatuses.Producing;
            events.Add(context.Events.Create(ProductionProgressedEvent, "system:core:production", new ProductionProgressPayload
            {
                BuildingId = slot.BuildingId,
                BatchId = slot.ActiveBatchId,
                ProgressWorkTicks = slot.ProgressWorkTicks,
                RequiredWorkTicks = slot.RequiredWorkTicks,
                WorkerCount = workerCount,
                EfficiencyBasisPoints = efficiencyBasisPoints
            }));

            if (slot.ProgressWorkTicks < slot.RequiredWorkTicks)
            {
                return;
            }

            string completedBatchId = slot.ActiveBatchId;
            ConsumeLockedInputs(context.State, context.State.Buildings.Instances[slot.BuildingId], slot);
            slot.OutputBuffer = ResolveBatchOutputs(context.State, recipe, completedBatchId);
            ClearActiveBatch(slot);
            slot.Status = ProductionSlotStatuses.OutputPending;
            events.Add(context.Events.Create(ProductionBatchCompletedEvent, "system:core:production", new ProductionBatchPayload
            {
                BuildingId = slot.BuildingId,
                RecipeId = slot.RecipeId,
                BatchId = completedBatchId
            }));

            if (TryTransferOutput(context, slot, events) && slot.Continuous)
            {
                if (!TryStartBatch(context, slot, events, out _, out _))
                {
                    slot.Status = ProductionSlotStatuses.Waiting;
                }
            }
        }

        private static bool TryStartBatch(
            SimulationContext context,
            ProductionSlotState slot,
            List<GameEvent> events,
            out string errorCode,
            out string reason)
        {
            errorCode = string.Empty;
            reason = string.Empty;
            if (slot.HasActiveBatch || slot.HasBufferedOutput)
            {
                errorCode = slot.HasBufferedOutput ? CommandErrorCodes.ProductionOutputPending : CommandErrorCodes.ProductionBatchActive;
                reason = "Production slot is already occupied.";
                return false;
            }

            if (!TryGetOperationalRecipeAndWorkers(context, slot, out RecipeDefinition recipe, out _))
            {
                errorCode = CommandErrorCodes.BuildingNotOperational;
                reason = "Building, recipe, or workers are unavailable.";
                return false;
            }

            BuildingInstanceState building = context.State.Buildings.Instances[slot.BuildingId];
            if (!TryLockInputs(context.State.Resources, building, recipe.Inputs, out Dictionary<string, int> globalLocks, out Dictionary<string, int> localLocks))
            {
                errorCode = CommandErrorCodes.ProductionInputUnavailable;
                reason = "Production inputs are unavailable.";
                return false;
            }

            slot.ActiveBatchId = StableId.Create(
                "batch", "core", context.State.NextProductionBatchSequence.ToString("D12")).ToString();
            context.State.NextProductionBatchSequence++;
            slot.RequiredWorkTicks = recipe.RequiredWorkTicks;
            slot.ProgressWorkTicks = 0;
            slot.LockedGlobalInputs = globalLocks;
            slot.LockedLocalInputs = localLocks;
            slot.Status = ProductionSlotStatuses.Producing;
            events.Add(context.Events.Create(ProductionBatchStartedEvent, "system:core:production", new ProductionBatchPayload
            {
                BuildingId = slot.BuildingId,
                RecipeId = slot.RecipeId,
                BatchId = slot.ActiveBatchId
            }));
            return true;
        }

        private static bool TryGetOperationalRecipeAndWorkers(
            SimulationContext context,
            ProductionSlotState slot,
            out RecipeDefinition recipe,
            out int workerCount)
        {
            recipe = null;
            workerCount = 0;
            if (!context.State.Buildings.Instances.TryGetValue(slot.BuildingId, out BuildingInstanceState building) ||
                !BuildingOperationalRules.CanProduce(building) ||
                !context.Definitions.TryGetRecipe(slot.RecipeId, out recipe) ||
                !StringComparer.Ordinal.Equals(recipe.BuildingDefinitionId, building.DefinitionId))
            {
                return false;
            }

            foreach (WorkAssignmentState assignment in context.State.Npcs.WorkAssignments.Values)
            {
                if (assignment != null && StringComparer.Ordinal.Equals(assignment.BuildingId, slot.BuildingId))
                {
                    workerCount++;
                }
            }

            return workerCount >= recipe.MinimumWorkers;
        }

        private static bool TryLockInputs(
            ResourceState resources,
            BuildingInstanceState building,
            Dictionary<string, int> inputs,
            out Dictionary<string, int> globalLocks,
            out Dictionary<string, int> localLocks)
        {
            globalLocks = new Dictionary<string, int>(StringComparer.Ordinal);
            localLocks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in inputs.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                int globalAvailable = resources.Items.TryGetValue(pair.Key, out ResourceStack global)
                    ? Math.Max(0, global.Amount - global.LockedAmount)
                    : 0;
                int fromGlobal = Math.Min(pair.Value, globalAvailable);
                int remaining = pair.Value - fromGlobal;
                int localAvailable = building.LocalInventory.TryGetValue(pair.Key, out LocalResourceStack local)
                    ? Math.Max(0, local.Amount - local.LockedAmount)
                    : 0;
                if (localAvailable < remaining)
                {
                    return false;
                }

                if (fromGlobal > 0)
                {
                    globalLocks[pair.Key] = fromGlobal;
                }

                if (remaining > 0)
                {
                    localLocks[pair.Key] = remaining;
                }
            }

            foreach (KeyValuePair<string, int> pair in globalLocks)
            {
                resources.Items[pair.Key].LockedAmount = checked(resources.Items[pair.Key].LockedAmount + pair.Value);
            }

            foreach (KeyValuePair<string, int> pair in localLocks)
            {
                building.LocalInventory[pair.Key].LockedAmount = checked(building.LocalInventory[pair.Key].LockedAmount + pair.Value);
            }

            return true;
        }

        private static void UnlockInputs(GameState state, BuildingInstanceState building, ProductionSlotState slot)
        {
            foreach (KeyValuePair<string, int> pair in slot.LockedGlobalInputs)
            {
                ResourceStack stack = state.Resources.Items[pair.Key];
                stack.LockedAmount = checked(stack.LockedAmount - pair.Value);
            }

            foreach (KeyValuePair<string, int> pair in slot.LockedLocalInputs)
            {
                LocalResourceStack stack = building.LocalInventory[pair.Key];
                stack.LockedAmount = checked(stack.LockedAmount - pair.Value);
            }

            ClearActiveBatch(slot);
        }

        private static void ConsumeLockedInputs(GameState state, BuildingInstanceState building, ProductionSlotState slot)
        {
            foreach (KeyValuePair<string, int> pair in slot.LockedGlobalInputs)
            {
                ResourceStack stack = state.Resources.Items[pair.Key];
                stack.Amount = checked(stack.Amount - pair.Value);
                stack.LockedAmount = checked(stack.LockedAmount - pair.Value);
            }

            foreach (KeyValuePair<string, int> pair in slot.LockedLocalInputs)
            {
                LocalResourceStack stack = building.LocalInventory[pair.Key];
                stack.Amount = checked(stack.Amount - pair.Value);
                stack.LockedAmount = checked(stack.LockedAmount - pair.Value);
            }
        }

        private static void ClearActiveBatch(ProductionSlotState slot)
        {
            slot.ActiveBatchId = string.Empty;
            slot.RequiredWorkTicks = 0;
            slot.ProgressWorkTicks = 0;
            slot.EfficiencyRemainderBasisPointTicks = 0;
            slot.LockedGlobalInputs.Clear();
            slot.LockedLocalInputs.Clear();
        }

        private static Dictionary<string, int> ResolveBatchOutputs(
            GameState state, RecipeDefinition recipe, string batchId)
        {
            Dictionary<string, int> outputs = new Dictionary<string, int>(recipe.Outputs, StringComparer.Ordinal);
            if (recipe.WeightedOutputRolls <= 0) return outputs;

            KeyValuePair<string, int>[] choices = recipe.WeightedOutputWeights
                .OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            long totalWeight = choices.Sum(pair => (long)pair.Value);
            for (int roll = 0; roll < recipe.WeightedOutputRolls; roll++)
            {
                string key = $"{state.RngSeed}|{batchId}|{recipe.RecipeId}|{roll}";
                byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(key));
                ulong value = 0;
                for (int index = 0; index < 8; index++) value = (value << 8) | digest[index];
                long selected = (long)(value % (ulong)totalWeight);
                string resourceId = choices[0].Key;
                for (int index = 0; index < choices.Length; index++)
                {
                    if (selected < choices[index].Value)
                    {
                        resourceId = choices[index].Key;
                        break;
                    }
                    selected -= choices[index].Value;
                }
                outputs[resourceId] = checked((outputs.TryGetValue(resourceId, out int amount) ? amount : 0) + 1);
            }
            return outputs;
        }

        private static bool TryTransferOutput(SimulationContext context, ProductionSlotState slot, List<GameEvent> events)
        {
            if (!context.State.Buildings.Instances.TryGetValue(slot.BuildingId, out BuildingInstanceState building) ||
                !BuildingOperationalRules.CanTransferInventory(building))
            {
                return false;
            }

            int localFree = Math.Max(0, building.LocalInventoryCapacity -
                building.LocalInventory.Values.Sum(stack => stack.Amount) - building.LocalInventoryReservedAmount);
            int sharedFree = StorageCapacityRules.SharedFreeCapacity(context.State.Resources);
            Dictionary<string, int> toLocal = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, int> toGlobal = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in slot.OutputBuffer.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                int localAmount = Math.Min(pair.Value, localFree);
                localFree -= localAmount;
                int globalAmount = pair.Value - localAmount;
                int globalFree = context.State.Resources.Items.TryGetValue(pair.Key, out ResourceStack global)
                    ? Math.Min(
                        Math.Max(0, global.Capacity - global.Amount - global.IncomingReservedAmount), sharedFree)
                    : 0;
                if (globalFree < globalAmount)
                {
                    return false;
                }

                toLocal[pair.Key] = localAmount;
                toGlobal[pair.Key] = globalAmount;
                sharedFree -= globalAmount;
            }

            foreach (KeyValuePair<string, int> pair in toLocal)
            {
                if (pair.Value == 0)
                {
                    continue;
                }

                if (!building.LocalInventory.TryGetValue(pair.Key, out LocalResourceStack stack))
                {
                    stack = new LocalResourceStack { ResourceId = pair.Key };
                    building.LocalInventory[pair.Key] = stack;
                }

                stack.Amount = checked(stack.Amount + pair.Value);
            }

            foreach (KeyValuePair<string, int> pair in toGlobal)
            {
                if (pair.Value > 0)
                {
                    context.State.Resources.Items[pair.Key].Amount = checked(context.State.Resources.Items[pair.Key].Amount + pair.Value);
                }
            }

            Dictionary<string, int> transferred = new Dictionary<string, int>(slot.OutputBuffer, StringComparer.Ordinal);
            slot.OutputBuffer.Clear();
            slot.Status = slot.Continuous ? ProductionSlotStatuses.Waiting : ProductionSlotStatuses.Idle;
            events.Add(context.Events.Create(ProductionOutputTransferredEvent, "system:core:production", new ProductionOutputPayload
            {
                BuildingId = slot.BuildingId,
                Outputs = transferred
            }));
            return true;
        }

        private static bool TryTransferOutputToGlobal(ResourceState resources, Dictionary<string, int> output)
        {
            int sharedRequired = 0;
            foreach (KeyValuePair<string, int> pair in output)
            {
                if (!resources.Items.TryGetValue(pair.Key, out ResourceStack stack) ||
                    stack.Capacity - stack.Amount - stack.IncomingReservedAmount < pair.Value)
                {
                    return false;
                }
                sharedRequired = checked(sharedRequired + pair.Value);
            }
            if (StorageCapacityRules.SharedFreeCapacity(resources) < sharedRequired)
            {
                return false;
            }

            foreach (KeyValuePair<string, int> pair in output)
            {
                resources.Items[pair.Key].Amount = checked(resources.Items[pair.Key].Amount + pair.Value);
            }

            return true;
        }

        private sealed class ConfigureProductionHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public ConfigureProductionHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return ConfigureProductionCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                ConfigureProductionPayload payload = Deserialize<ConfigureProductionPayload>(command.Payload, _options);
                if (!context.State.Buildings.Instances.TryGetValue(payload.BuildingId, out BuildingInstanceState building))
                {
                    return ValidationResult.Invalid("Unknown production building.", CommandErrorCodes.BuildingNotFound);
                }

                if (!context.Definitions.TryGetRecipe(payload.RecipeId, out RecipeDefinition recipe))
                {
                    return ValidationResult.Invalid("Unknown production recipe.", CommandErrorCodes.ProductionRecipeNotFound);
                }

                if (!StringComparer.Ordinal.Equals(recipe.BuildingDefinitionId, building.DefinitionId))
                {
                    return ValidationResult.Invalid("Recipe is incompatible with building.", CommandErrorCodes.ProductionRecipeIncompatible);
                }

                if (context.State.Production.SlotsByBuildingId.TryGetValue(payload.BuildingId, out ProductionSlotState slot) &&
                    (slot.HasActiveBatch || slot.HasBufferedOutput) && !StringComparer.Ordinal.Equals(slot.RecipeId, payload.RecipeId))
                {
                    return ValidationResult.Invalid("Cannot change recipe while a batch or output exists.",
                        slot.HasBufferedOutput ? CommandErrorCodes.ProductionOutputPending : CommandErrorCodes.ProductionBatchActive);
                }

                return ValidationResult.Valid();
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                ConfigureProductionPayload payload = Deserialize<ConfigureProductionPayload>(command.Payload, _options);
                if (!context.State.Production.SlotsByBuildingId.TryGetValue(payload.BuildingId, out ProductionSlotState slot))
                {
                    slot = new ProductionSlotState { BuildingId = payload.BuildingId };
                    context.State.Production.SlotsByBuildingId[payload.BuildingId] = slot;
                }

                slot.RecipeId = payload.RecipeId;
                slot.Continuous = payload.Continuous;
                if (!slot.HasActiveBatch && !slot.HasBufferedOutput)
                {
                    slot.Status = payload.Continuous ? ProductionSlotStatuses.Waiting : ProductionSlotStatuses.Idle;
                }

                return new[]
                {
                    context.Events.Create(ProductionConfiguredEvent, command.CommandId, new ProductionConfiguredPayload
                    {
                        BuildingId = slot.BuildingId,
                        RecipeId = slot.RecipeId,
                        Continuous = slot.Continuous
                    })
                };
            }
        }

        private sealed class StartProductionHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public StartProductionHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return StartProductionCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                ProductionBuildingPayload payload = Deserialize<ProductionBuildingPayload>(command.Payload, _options);
                if (!context.State.Production.SlotsByBuildingId.TryGetValue(payload.BuildingId, out ProductionSlotState slot) ||
                    string.IsNullOrEmpty(slot.RecipeId))
                {
                    return ValidationResult.Invalid("Production slot is not configured.", CommandErrorCodes.ProductionNotConfigured);
                }

                if (!CanStartWithoutMutation(context, slot, out string code, out string reason))
                {
                    return ValidationResult.Invalid(reason, code);
                }

                return ValidationResult.Valid();
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                ProductionBuildingPayload payload = Deserialize<ProductionBuildingPayload>(command.Payload, _options);
                ProductionSlotState slot = context.State.Production.SlotsByBuildingId[payload.BuildingId];
                List<GameEvent> events = new List<GameEvent>();
                if (!TryStartBatch(context, slot, events, out _, out string reason))
                {
                    throw new InvalidOperationException(reason);
                }

                return events;
            }
        }

        private sealed class CancelProductionHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public CancelProductionHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return CancelProductionCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                ProductionBuildingPayload payload = Deserialize<ProductionBuildingPayload>(command.Payload, _options);
                return context.State.Production.SlotsByBuildingId.TryGetValue(payload.BuildingId, out ProductionSlotState slot) && slot.HasActiveBatch
                    ? ValidationResult.Valid()
                    : ValidationResult.Invalid("Production building has no active batch.", CommandErrorCodes.ProductionNotConfigured);
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                ProductionBuildingPayload payload = Deserialize<ProductionBuildingPayload>(command.Payload, _options);
                ProductionSlotState slot = context.State.Production.SlotsByBuildingId[payload.BuildingId];
                string batchId = slot.ActiveBatchId;
                UnlockInputs(context.State, context.State.Buildings.Instances[payload.BuildingId], slot);
                slot.Continuous = false;
                slot.Status = ProductionSlotStatuses.Idle;
                return new[]
                {
                    context.Events.Create(ProductionBatchCancelledEvent, command.CommandId, new ProductionBatchPayload
                    {
                        BuildingId = slot.BuildingId,
                        RecipeId = slot.RecipeId,
                        BatchId = batchId,
                        Reason = "player_cancelled"
                    })
                };
            }
        }

        private static bool CanStartWithoutMutation(
            SimulationContext context,
            ProductionSlotState slot,
            out string code,
            out string reason)
        {
            code = string.Empty;
            reason = string.Empty;
            if (slot.HasActiveBatch || slot.HasBufferedOutput)
            {
                code = slot.HasBufferedOutput ? CommandErrorCodes.ProductionOutputPending : CommandErrorCodes.ProductionBatchActive;
                reason = "Production slot is occupied.";
                return false;
            }

            if (!TryGetOperationalRecipeAndWorkers(context, slot, out RecipeDefinition recipe, out _))
            {
                code = CommandErrorCodes.BuildingNotOperational;
                reason = "Building, recipe, or workers are unavailable.";
                return false;
            }

            BuildingInstanceState building = context.State.Buildings.Instances[slot.BuildingId];
            foreach (KeyValuePair<string, int> pair in recipe.Inputs)
            {
                int global = context.State.Resources.Items.TryGetValue(pair.Key, out ResourceStack globalStack)
                    ? Math.Max(0, globalStack.Amount - globalStack.LockedAmount)
                    : 0;
                int local = building.LocalInventory.TryGetValue(pair.Key, out LocalResourceStack localStack)
                    ? Math.Max(0, localStack.Amount - localStack.LockedAmount)
                    : 0;
                if ((long)global + local < pair.Value)
                {
                    code = CommandErrorCodes.ProductionInputUnavailable;
                    reason = "Production inputs are unavailable.";
                    return false;
                }
            }

            return true;
        }

        private static T Deserialize<T>(JsonElement payload, JsonSerializerOptions options)
        {
            T value = payload.Deserialize<T>(options);
            if (value == null) throw new InvalidOperationException("Command payload could not be deserialized.");
            return value;
        }
    }

    public sealed class ConfigureProductionPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public string RecipeId { get; set; } = string.Empty;
        public bool Continuous { get; set; }
    }

    public sealed class ProductionBuildingPayload
    {
        public string BuildingId { get; set; } = string.Empty;
    }

    public sealed class ProductionConfiguredPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public string RecipeId { get; set; } = string.Empty;
        public bool Continuous { get; set; }
    }

    public sealed class ProductionBatchPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public string RecipeId { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class ProductionProgressPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public long ProgressWorkTicks { get; set; }
        public long RequiredWorkTicks { get; set; }
        public int WorkerCount { get; set; }
        public int EfficiencyBasisPoints { get; set; }
    }

    public sealed class ProductionOutputPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public Dictionary<string, int> Outputs { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }
}
