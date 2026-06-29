using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class DiagnosticIssue
    {
        public DiagnosticSeverity Severity { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> TargetIds { get; set; } = new List<string>();
        public int? Priority { get; set; }
        public string SourceSystem { get; set; } = string.Empty;
    }

    public static class StateDiagnostics
    {
        public static string CalculateStateHash(GameState state, JsonSerializerOptions jsonOptions = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            JsonSerializerOptions options = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
            string json = JsonSerializer.Serialize(state, options);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(bytes);
                return ToHex(hash);
            }
        }

        public static IReadOnlyList<DiagnosticIssue> CheckInvariants(GameState state, DefinitionRegistry definitions)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            List<DiagnosticIssue> issues = new List<DiagnosticIssue>();
            CheckSaveMetadata(state, issues);
            CheckResources(state, issues);
            CheckDifficulty(state, issues);
            CheckBuildings(state, definitions, issues);
            CheckNpcs(state, definitions, issues);
            CheckHousing(state, definitions, issues);
            CheckSurvival(state, issues);
            CheckWaste(state, issues);
            CheckProduction(state, definitions, issues);
            CheckContinuousProduction(state, definitions, issues);
            CheckSunlamps(state, issues);
            CheckLogistics(state, issues);
            CheckCommands(state, issues);
            return issues;
        }

        private static void CheckSaveMetadata(GameState state, List<DiagnosticIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(state.SaveVersion))
            {
                AddError(issues, "save.version.empty", "SaveVersion cannot be empty.");
            }

            if (!StableId.IsValid(state.PlayerId))
            {
                AddError(issues, "player.id.invalid", "PlayerId must use namespace:type:id format.");
            }
        }

        private static void CheckResources(GameState state, List<DiagnosticIssue> issues)
        {
            if (state.Resources.SharedCapacity < 0)
            {
                AddError(issues, "resource.shared_capacity.negative", "Shared resource capacity cannot be negative.");
            }
            foreach (KeyValuePair<string, ResourceStack> pair in state.Resources.Items)
            {
                if (!StableId.IsValid(pair.Key))
                {
                    AddError(issues, "resource.id.invalid", $"Resource key {pair.Key} must use namespace:type:id format.");
                }

                if (pair.Value == null)
                {
                    AddError(issues, "resource.stack.null", $"Resource stack {pair.Key} cannot be null.");
                    continue;
                }

                if (pair.Value.Amount < 0)
                {
                    AddError(issues, "resource.amount.negative", $"Resource {pair.Key} amount cannot be negative.");
                }

                if (pair.Value.Capacity < 0)
                {
                    AddError(issues, "resource.capacity.negative", $"Resource {pair.Key} capacity cannot be negative.");
                }

                if (pair.Value.Amount > pair.Value.Capacity)
                {
                    AddWarning(
                        issues,
                        "resource.amount.over_capacity",
                        $"Resource {pair.Key} amount is greater than capacity.",
                        new[] { pair.Key },
                        85,
                        "resource_storage");
                }

                if (pair.Value.LockedAmount < 0 || pair.Value.LockedAmount > pair.Value.Amount)
                {
                    AddError(issues, "resource.locked.invalid", $"Resource {pair.Key} has an invalid locked amount.");
                }
            }
            if (state.Resources.SharedCapacity >= 0 &&
                StorageCapacityRules.UsedCapacity(state.Resources) + StorageCapacityRules.ReservedCapacity(state.Resources) >
                state.Resources.SharedCapacity)
            {
                AddWarning(issues, "resource.shared_capacity.exceeded",
                    "Global resource amount and reservations exceed shared storage capacity.",
                    null,
                    85,
                    "resource_storage");
            }
        }

        private static void CheckDifficulty(GameState state, List<DiagnosticIssue> issues)
        {
            if (state.Difficulty == null)
            {
                AddError(issues, "difficulty.state.null", "Difficulty state cannot be null.");
                return;
            }

            try
            {
                DifficultyProfiles.ResolveStructuralFailure(state.Difficulty);
            }
            catch (InvalidOperationException exception)
            {
                AddError(issues, "difficulty.structural_policy.invalid", exception.Message);
            }
        }

        private static void CheckBuildings(GameState state, DefinitionRegistry definitions, List<DiagnosticIssue> issues)
        {
            if (state.Buildings.NextStructuralCollapseTick < 0)
            {
                AddError(issues, "structure.collapse_tick.negative", "Next structural collapse tick cannot be negative.");
            }

            HashSet<string> buildingIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance == null)
                {
                    AddError(issues, "building.instance.null", "Building instance cannot be null.");
                    continue;
                }

                CheckBuildingIdentity(instance.BuildingId, instance.DefinitionId, definitions, issues);
                CheckPlacement(
                    instance.BuildingId,
                    instance.Layer,
                    instance.BaseLayer,
                    instance.RotationQuarterTurns,
                    instance.PlacedWidth,
                    instance.PlacedDepth,
                    instance.PlacedHeight,
                    instance.PlacementSchemaVersion,
                    issues);

                if (!buildingIds.Add(instance.BuildingId))
                {
                    AddError(issues, "building.id.duplicate", $"Building id {instance.BuildingId} is duplicated.");
                }

                if (!BuildingStructuralStatuses.IsKnown(instance.StructuralStatus))
                {
                    AddError(issues, "building.structural_status.invalid", $"Building {instance.BuildingId} has unknown structural status {instance.StructuralStatus}.");
                }

                if (instance.StructuralGraceDeadlineTick < 0)
                {
                    AddError(issues, "building.structural_deadline.negative", $"Building {instance.BuildingId} has a negative structural grace deadline.");
                }

                if (StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Normal) &&
                    instance.StructuralGraceDeadlineTick != 0)
                {
                    AddError(issues, "building.structural_deadline.unexpected", $"Stable building {instance.BuildingId} cannot retain a structural grace deadline.");
                }

                CheckPaidBuildCost(instance.BuildingId, instance.PaidBuildCost, issues);
                CheckLocalInventory(instance, issues);

            }

            foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
            {
                if (task == null)
                {
                    AddError(issues, "construction.task.null", "Construction task cannot be null.");
                    continue;
                }

                if (!StableId.IsValid(task.TaskId))
                {
                    AddError(issues, "construction.task_id.invalid", $"Construction task id {task.TaskId} must use namespace:type:id format.");
                }

                CheckBuildingIdentity(task.BuildingId, task.DefinitionId, definitions, issues);
                CheckPlacement(
                    task.TaskId,
                    task.Layer,
                    task.BaseLayer,
                    task.RotationQuarterTurns,
                    task.PlacedWidth,
                    task.PlacedDepth,
                    task.PlacedHeight,
                    task.PlacementSchemaVersion,
                    issues);

                if (!buildingIds.Add(task.BuildingId))
                {
                    AddError(issues, "building.id.duplicate", $"Building id {task.BuildingId} is duplicated.");
                }

                CheckPaidBuildCost(task.TaskId, task.PaidBuildCost, issues);

            }

            CheckSpatialOccupancy(state, issues);
            CheckStructuralSupport(state, definitions, issues);
        }

        private static void CheckPaidBuildCost(string objectId, Dictionary<string, int> paidBuildCost, List<DiagnosticIssue> issues)
        {
            if (paidBuildCost == null)
            {
                AddError(issues, "building.paid_cost.null", $"Object {objectId} paid build cost cannot be null.");
                return;
            }

            foreach (KeyValuePair<string, int> pair in paidBuildCost)
            {
                if (!StableId.IsValid(pair.Key) || pair.Value < 0)
                {
                    AddError(issues, "building.paid_cost.invalid", $"Object {objectId} has an invalid paid build cost entry.");
                }
            }
        }

        private static void CheckLocalInventory(BuildingInstanceState building, List<DiagnosticIssue> issues)
        {
            if (building.LocalInventory == null || building.LocalInventoryCapacity < 0)
            {
                AddError(issues, "building.local_inventory.invalid", $"Building {building.BuildingId} local inventory is invalid.");
                return;
            }

            long total = 0;
            foreach (KeyValuePair<string, LocalResourceStack> pair in building.LocalInventory)
            {
                LocalResourceStack stack = pair.Value;
                if (!StableId.IsValid(pair.Key) || stack == null ||
                    !StringComparer.Ordinal.Equals(pair.Key, stack.ResourceId) ||
                    stack.Amount < 0 || stack.LockedAmount < 0 || stack.LockedAmount > stack.Amount)
                {
                    AddError(issues, "building.local_inventory.stack_invalid", $"Building {building.BuildingId} has invalid local resource {pair.Key}.");
                    continue;
                }
                total += stack.Amount;
            }

            if (total > building.LocalInventoryCapacity)
            {
                AddError(
                    issues,
                    "building.local_inventory.over_capacity",
                    $"Building {building.BuildingId} local inventory exceeds capacity.",
                    new[] { building.BuildingId },
                    85,
                    "building_inventory");
            }
        }

        private static void CheckProduction(GameState state, DefinitionRegistry definitions, List<DiagnosticIssue> issues)
        {
            if (state.Production == null || state.Production.SlotsByBuildingId == null)
            {
                AddError(issues, "production.state.null", "Production runtime state cannot be null.");
                return;
            }

            foreach (KeyValuePair<string, ProductionSlotState> pair in state.Production.SlotsByBuildingId)
            {
                ProductionSlotState slot = pair.Value;
                if (slot == null || !StringComparer.Ordinal.Equals(pair.Key, slot.BuildingId) ||
                    !ProductionSlotStatuses.IsKnown(slot.Status) ||
                    !state.Buildings.Instances.TryGetValue(slot.BuildingId, out BuildingInstanceState building) || building == null)
                {
                    AddError(issues, "production.slot.invalid", $"Production slot {pair.Key} is invalid.");
                    continue;
                }

                if (!string.IsNullOrEmpty(slot.RecipeId) &&
                    (!definitions.TryGetRecipe(slot.RecipeId, out RecipeDefinition recipe) ||
                     !StringComparer.Ordinal.Equals(recipe.BuildingDefinitionId, building.DefinitionId)))
                {
                    AddError(issues, "production.recipe.invalid", $"Production slot {pair.Key} has an invalid recipe.");
                }

                if (slot.LockedGlobalInputs == null || slot.LockedLocalInputs == null || slot.OutputBuffer == null ||
                    slot.ProgressWorkTicks < 0 || slot.RequiredWorkTicks < 0 ||
                    slot.EfficiencyRemainderBasisPointTicks < 0 ||
                    slot.EfficiencyRemainderBasisPointTicks >= WasteEffectRules.BasisPointsPerWhole ||
                    (slot.HasActiveBatch && (slot.RequiredWorkTicks == 0 || slot.ProgressWorkTicks > slot.RequiredWorkTicks)) ||
                    (!slot.HasActiveBatch && (slot.RequiredWorkTicks != 0 || slot.ProgressWorkTicks != 0)))
                {
                    AddError(issues, "production.batch.invalid", $"Production slot {pair.Key} has an invalid batch state.");
                }

                if (slot.HasBufferedOutput && !StringComparer.Ordinal.Equals(slot.Status, ProductionSlotStatuses.OutputPending))
                {
                    AddError(issues, "production.output.status_invalid", $"Production slot {pair.Key} buffer requires output-pending status.");
                }
            }
        }

        private static void CheckContinuousProduction(
            GameState state,
            DefinitionRegistry definitions,
            List<DiagnosticIssue> issues)
        {
            if (state.ContinuousProduction == null || state.ContinuousProduction.Buildings == null)
            {
                AddError(issues, "continuous_production.state.null",
                    "Continuous production runtime state cannot be null.");
                return;
            }

            foreach (KeyValuePair<string, ContinuousProductionBuildingState> pair in
                state.ContinuousProduction.Buildings)
            {
                ContinuousProductionBuildingState runtime = pair.Value;
                bool hasBuilding = state.Buildings.Instances.TryGetValue(
                    pair.Key, out BuildingInstanceState building);
                bool hasDefinition = hasBuilding && building != null &&
                    definitions.TryGetContinuousProduction(building.DefinitionId, out _);
                bool invalid = runtime == null || !StringComparer.Ordinal.Equals(pair.Key, runtime.BuildingId) ||
                    !hasBuilding || building == null || building.IsDestroyed ||
                    !hasDefinition ||
                    runtime.ProgressUnits < 0 || runtime.ProgressUnits >= GameTime.TicksPerGameDay ||
                    runtime.EfficiencyRemainderBasisPointTicks < 0 ||
                    runtime.EfficiencyRemainderBasisPointTicks >= WasteEffectRules.BasisPointsPerWhole ||
                    runtime.FertilizerBaseOutputRemaining < 0 ||
                    runtime.FertilizerBonusHalfUnitRemainder < 0 ||
                    runtime.FertilizerBonusHalfUnitRemainder > 1 ||
                    (runtime.FertilizerBaseOutputRemaining == 0 && runtime.FertilizerBonusHalfUnitRemainder != 0) ||
                    runtime.InputCoverageTicks < 0 || runtime.InputCoverageTicks > GameTime.TicksPerGameDay ||
                    runtime.PendingOutputAmount < 0 || !ContinuousProductionStatuses.IsKnown(runtime.Status) ||
                    runtime.AdditionalProgressUnits == null || runtime.AdditionalPendingOutputs == null ||
                    runtime.AdditionalProgressUnits.Any(item => !StableId.IsValid(item.Key) || item.Value < 0 || item.Value >= GameTime.TicksPerGameDay) ||
                    runtime.AdditionalPendingOutputs.Any(item => !StableId.IsValid(item.Key) || item.Value < 0) ||
                    (runtime.PendingOutputAmount > 0 &&
                     !StringComparer.Ordinal.Equals(runtime.Status, ContinuousProductionStatuses.OutputPending)) ||
                    (runtime.AdditionalPendingOutputs.Values.Any(value => value > 0) &&
                     !StringComparer.Ordinal.Equals(runtime.Status, ContinuousProductionStatuses.OutputPending));
                if (invalid)
                {
                    AddError(issues, "continuous_production.building.invalid",
                        $"Continuous production state {pair.Key} is invalid.");
                    continue;
                }

                if (StringComparer.Ordinal.Equals(building.DefinitionId, CoreBuildingIds.Farm) &&
                    StringComparer.Ordinal.Equals(runtime.Status, ContinuousProductionStatuses.PausedNoLight))
                {
                    AddInfo(issues, "continuous_production.farm.no_light",
                        $"Farm {pair.Key} is paused because required agricultural light is missing.",
                        new[] { pair.Key },
                        100,
                        "continuous_production");
                }
                else if (StringComparer.Ordinal.Equals(runtime.Status, ContinuousProductionStatuses.OutputPending))
                {
                    AddInfo(issues, "continuous_production.output_pending",
                        $"Continuous production building {pair.Key} has output waiting for transfer.",
                        new[] { pair.Key },
                        80,
                        "continuous_production");
                }
                else if (StringComparer.Ordinal.Equals(runtime.Status, ContinuousProductionStatuses.PausedInput))
                {
                    AddInfo(issues, "continuous_production.input_unavailable",
                        $"Continuous production building {pair.Key} is paused because input resources are unavailable.",
                        new[] { pair.Key },
                        70,
                        "continuous_production");
                }
                else if (StringComparer.Ordinal.Equals(runtime.Status, ContinuousProductionStatuses.PausedNoWorkers))
                {
                    AddInfo(issues, "continuous_production.no_workers",
                        $"Continuous production building {pair.Key} is paused because no workers are assigned.",
                        new[] { pair.Key },
                        60,
                        "continuous_production");
                }
            }
        }

        private static void CheckSunlamps(GameState state, List<DiagnosticIssue> issues)
        {
            if (state.Sunlamps == null || state.Sunlamps.Buildings == null)
            {
                AddError(issues, "sunlamp.state.null", "Sunlamp runtime state cannot be null.");
                return;
            }

            foreach (KeyValuePair<string, SunlampBuildingState> pair in state.Sunlamps.Buildings)
            {
                SunlampBuildingState runtime = pair.Value;
                bool hasBuilding = state.Buildings.Instances.TryGetValue(
                    pair.Key, out BuildingInstanceState building);
                bool invalid = runtime == null ||
                    !StringComparer.Ordinal.Equals(pair.Key, runtime.BuildingId) ||
                    !StableId.IsValid(runtime.BuildingId) ||
                    !hasBuilding ||
                    building == null ||
                    !StringComparer.Ordinal.Equals(building.DefinitionId, CoreBuildingIds.Sunlamp) ||
                    runtime.FuelCoverageTicks < 0 ||
                    runtime.FuelCoverageTicks > GameTime.TicksPerGameDay;
                if (invalid)
                {
                    AddError(issues, "sunlamp.state.invalid",
                        $"Sunlamp runtime state {pair.Key} is invalid.");
                    continue;
                }

                if (BuildingOperationalRules.IsOperational(building) && runtime.FuelCoverageTicks == 0)
                {
                    AddInfo(issues, "sunlamp.fuel.empty",
                        $"Sunlamp {pair.Key} has no prepaid fuel coverage.",
                        new[] { pair.Key },
                        90,
                        "sunlamp");
                }
            }
        }

        private static void CheckLogistics(GameState state, List<DiagnosticIssue> issues)
        {
            if (state.Logistics == null || state.Logistics.ActiveTasks == null || state.Logistics.Routes == null ||
                state.Logistics.ConstructionTasks == null || state.Logistics.Connectors == null)
            {
                AddError(issues, "logistics.state.null", "Logistics runtime state cannot be null.");
                return;
            }


            foreach (KeyValuePair<string, LogisticsConnectorConstructionState> pair in state.Logistics.ConstructionTasks)
            {
                LogisticsConnectorConstructionState task = pair.Value;
                if (task == null || !StringComparer.Ordinal.Equals(pair.Key, task.TaskId) ||
                    !StableId.IsValid(task.ConnectorId) || !StableId.IsValid(task.DefinitionId) ||
                    !StableId.IsValid(task.ResourceId) || task.AutoTransferAmount <= 0 ||
                    task.RequiredTicks <= 0 || task.ProgressTicks < 0 || task.ProgressTicks > task.RequiredTicks ||
                    !state.Buildings.Instances.ContainsKey(task.LowerBuildingId) ||
                    !state.Buildings.Instances.ContainsKey(task.UpperBuildingId))
                {
                    AddError(issues, "logistics.connector_construction.invalid",
                        $"Connector construction task {pair.Key} is invalid.");
                }
            }

            foreach (KeyValuePair<string, LogisticsConnectorInstanceState> pair in state.Logistics.Connectors)
            {
                LogisticsConnectorInstanceState connector = pair.Value;
                if (connector == null || !StringComparer.Ordinal.Equals(pair.Key, connector.ConnectorId) ||
                    !StableId.IsValid(connector.DefinitionId) || !StableId.IsValid(connector.ResourceId) ||
                    !StableId.IsValid(connector.RouteId) || connector.AutoTransferAmount <= 0 ||
                    connector.Durability < 0 || !state.Buildings.Instances.ContainsKey(connector.LowerBuildingId) ||
                    !state.Buildings.Instances.ContainsKey(connector.UpperBuildingId) ||
                    (!connector.IsDestroyed && !state.Logistics.Routes.ContainsKey(connector.RouteId)))
                {
                    AddError(issues, "logistics.connector.invalid", $"Logistics connector {pair.Key} is invalid.");
                }
            }

            foreach (KeyValuePair<string, LogisticsRouteState> pair in state.Logistics.Routes)
            {
                LogisticsRouteState route = pair.Value;
                if (route == null || !StringComparer.Ordinal.Equals(pair.Key, route.RouteId) ||
                    !state.Buildings.Instances.ContainsKey(route.FirstBuildingId) ||
                    !state.Buildings.Instances.ContainsKey(route.SecondBuildingId) ||
                    StringComparer.Ordinal.Equals(route.FirstBuildingId, route.SecondBuildingId))
                {
                    AddError(issues, "logistics.route.invalid", $"Logistics route {pair.Key} is invalid.");
                }
            }

            foreach (KeyValuePair<string, TransportTaskState> pair in state.Logistics.ActiveTasks)
            {
                TransportTaskState task = pair.Value;
                if (task == null || !StringComparer.Ordinal.Equals(pair.Key, task.TaskId) ||
                    !LogisticsEndpointKinds.IsKnown(task.SourceKind) || !LogisticsEndpointKinds.IsKnown(task.TargetKind) ||
                    !StableId.IsValid(task.ResourceId) || task.Amount <= 0 ||
                    task.CreatedTick < 0 || task.CompletionTick < task.CreatedTick ||
                    !IsDiagnosticEndpointValid(state, task.SourceKind, task.SourceBuildingId) ||
                    !IsDiagnosticEndpointValid(state, task.TargetKind, task.TargetBuildingId))
                {
                    AddError(issues, "logistics.task.invalid", $"Transport task {pair.Key} is invalid.");
                    continue;
                }

                if (!string.IsNullOrEmpty(task.RouteId) && !state.Logistics.Routes.ContainsKey(task.RouteId))
                {
                    AddError(issues, "logistics.task.route_missing", $"Transport task {pair.Key} references a missing route.");
                }
            }

            foreach (ResourceStack stack in state.Resources.Items.Values)
            {
                if (stack != null && (stack.IncomingReservedAmount < 0 ||
                    (stack.IncomingReservedAmount > 0 && stack.Amount + stack.IncomingReservedAmount > stack.Capacity)))
                {
                    AddError(issues, "logistics.global_reservation.invalid", $"Resource {stack.ResourceId} has an invalid incoming reservation.");
                }
            }

            foreach (BuildingInstanceState building in state.Buildings.Instances.Values)
            {
                if (building != null && (building.LocalInventoryReservedAmount < 0 ||
                    building.LocalInventory.Values.Sum(stack => stack.Amount) + building.LocalInventoryReservedAmount > building.LocalInventoryCapacity))
                {
                    AddError(issues, "logistics.local_reservation.invalid", $"Building {building.BuildingId} has an invalid local reservation.");
                }
            }
        }

        private static bool IsDiagnosticEndpointValid(GameState state, string kind, string buildingId)
        {
            return StringComparer.Ordinal.Equals(kind, LogisticsEndpointKinds.Global)
                ? string.IsNullOrEmpty(buildingId)
                : state.Buildings.Instances.ContainsKey(buildingId);
        }

        private static void CheckBuildingIdentity(string buildingId, string definitionId, DefinitionRegistry definitions, List<DiagnosticIssue> issues)
        {
            if (!StableId.IsValid(buildingId))
            {
                AddError(issues, "building.id.invalid", $"Building id {buildingId} must use namespace:type:id format.");
            }

            if (!StableId.IsValid(definitionId))
            {
                AddError(issues, "building.definition_id.invalid", $"Building definition id {definitionId} must use namespace:type:id format.");
            }
            else if (!definitions.TryGetBuilding(definitionId, out BuildingDefinition _))
            {
                AddError(issues, "building.definition.missing", $"Building definition {definitionId} is missing.");
            }
        }

        private static void CheckCommands(GameState state, List<DiagnosticIssue> issues)
        {
            foreach (string commandId in state.Commands.ProcessedCommandIds)
            {
                if (!StableId.IsValid(commandId))
                {
                    AddError(issues, "command.id.invalid", $"Processed command id {commandId} must use namespace:type:id format.");
                }
            }

            foreach (KeyValuePair<string, long> pair in state.Commands.LastAcceptedSequenceByPlayer)
            {
                if (!StableId.IsValid(pair.Key))
                {
                    AddError(issues, "command.player_id.invalid", $"Command player id {pair.Key} must use namespace:type:id format.");
                }

                if (pair.Value < 0)
                {
                    AddError(issues, "command.sequence.negative", $"Command sequence for {pair.Key} cannot be negative.");
                }
            }
        }

        private static void CheckNpcs(GameState state, DefinitionRegistry definitions, List<DiagnosticIssue> issues)
        {
            if (state.Npcs == null || state.Npcs.Instances == null || state.Npcs.WorkAssignments == null)
            {
                AddError(issues, "npc.state.null", "NPC runtime state and collections cannot be null.");
                return;
            }

            foreach (KeyValuePair<string, NpcInstanceState> pair in state.Npcs.Instances)
            {
                NpcInstanceState npc = pair.Value;
                if (npc == null || !StableId.IsValid(pair.Key) ||
                    !StringComparer.Ordinal.Equals(pair.Key, npc.NpcId) ||
                    !StableId.IsValid(npc.OwnerPlayerId) || npc.CreationSequence < 0 ||
                    npc.BaseSatisfactionBasisPoints < 0 ||
                    npc.BaseSatisfactionBasisPoints > WasteEffectRules.BasisPointsPerWhole)
                {
                    AddError(issues, "npc.instance.invalid", $"NPC state {pair.Key} is invalid.");
                    continue;
                }

                long minimumLifespan = NpcLifecycleSystem.MinimumAdultLifespanDays * GameTime.TicksPerGameDay;
                long maximumLifespan = NpcLifecycleSystem.MaximumAdultLifespanDays * GameTime.TicksPerGameDay;
                bool lifespanInvalid = npc.AdultLifespanTicks == 0
                    ? npc.LifeStageElapsedTicks != 0 || npc.AdultTransitionTick != 0 || npc.DeathTick != 0
                    : npc.AdultLifespanTicks < minimumLifespan || npc.AdultLifespanTicks > maximumLifespan;
                if (lifespanInvalid || npc.LifeStageElapsedTicks < 0 ||
                    (npc.IsAdult ? npc.LifeStageElapsedTicks > npc.AdultLifespanTicks : npc.LifeStageElapsedTicks >= NpcLifecycleSystem.InfantGrowthTicks) ||
                    npc.AdultTransitionTick < 0 || npc.AdultTransitionTick > state.SimulationTick ||
                    npc.DeathTick < 0 || npc.DeathTick > state.SimulationTick ||
                    (npc.AdultTransitionTick > 0 && !npc.IsAdult) || (npc.DeathTick > 0 && npc.IsAlive))
                    AddError(issues, "npc.lifecycle.invalid", $"NPC lifecycle state {pair.Key} is invalid.");
            }

            HashSet<string> occupiedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, WorkAssignmentState> pair in state.Npcs.WorkAssignments)
            {
                WorkAssignmentState assignment = pair.Value;
                if (assignment == null || !StringComparer.Ordinal.Equals(pair.Key, assignment.NpcId) ||
                    !state.Npcs.Instances.TryGetValue(assignment.NpcId, out NpcInstanceState npc) || npc == null)
                {
                    AddError(issues, "npc.assignment.invalid", $"Work assignment {pair.Key} has an invalid NPC reference.");
                    continue;
                }

                if (!NpcOperationalRules.CanHoldWorkAssignment(npc))
                {
                    AddError(issues, "npc.assignment.npc_unavailable", $"NPC {assignment.NpcId} cannot retain a work assignment.");
                }

                if (!state.Buildings.Instances.TryGetValue(assignment.BuildingId, out BuildingInstanceState building) || building == null)
                {
                    AddError(issues, "npc.assignment.building_missing", $"Work assignment {pair.Key} references missing building {assignment.BuildingId}.");
                    continue;
                }

                if (!BuildingOperationalRules.CanAcceptWorkers(building))
                {
                    AddError(issues, "npc.assignment.building_inoperable", $"Building {assignment.BuildingId} cannot retain workers.");
                }

                if (!definitions.TryGetBuilding(building.DefinitionId, out BuildingDefinition definition) ||
                    assignment.SlotIndex < 0 || assignment.SlotIndex >= definition.WorkerSlotCount)
                {
                    AddError(issues, "npc.assignment.slot_invalid", $"Work assignment {pair.Key} has an invalid slot.");
                    continue;
                }

                string slotKey = assignment.BuildingId + "\n" + assignment.SlotIndex;
                if (!occupiedSlots.Add(slotKey))
                {
                    AddError(issues, "npc.assignment.slot_conflict", $"Worker slot {slotKey} is assigned more than once.");
                }
            }
        }

        private static void CheckHousing(GameState state, DefinitionRegistry definitions, List<DiagnosticIssue> issues)
        {
            if (state.Housing == null || state.Housing.AssignmentsByNpcId == null || state.Housing.HomelessAdultNpcIds == null)
            {
                AddError(issues, "housing.state.null", "Housing runtime state and collections cannot be null.");
                return;
            }

            HashSet<string> occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, HousingAssignmentState> pair in state.Housing.AssignmentsByNpcId)
            {
                HousingAssignmentState assignment = pair.Value;
                if (assignment == null || !StringComparer.Ordinal.Equals(pair.Key, assignment.NpcId) ||
                    !state.Npcs.Instances.TryGetValue(assignment.NpcId, out NpcInstanceState npc) || !HousingSystem.IsEligible(npc))
                {
                    AddError(issues, "housing.assignment.npc_invalid", $"Housing assignment {pair.Key} has an invalid NPC.");
                    continue;
                }
                if (!state.Buildings.Instances.TryGetValue(assignment.BuildingId, out BuildingInstanceState building) ||
                    !BuildingOperationalRules.IsOperational(building) ||
                    !definitions.TryGetBuilding(building.DefinitionId, out BuildingDefinition definition))
                {
                    AddError(issues, "housing.assignment.building_invalid", $"Housing assignment {pair.Key} has an unavailable building.");
                    continue;
                }
                int slotCount;
                try { slotCount = HousingSystem.ResolveBedSlotCount(building, definition); }
                catch (OverflowException)
                {
                    AddError(issues, "housing.assignment.slot_overflow", $"Housing assignment {pair.Key} has an overflowing bed count.");
                    continue;
                }
                if (assignment.BedSlotIndex < 0 || assignment.BedSlotIndex >= slotCount)
                {
                    AddError(issues, "housing.assignment.slot_invalid", $"Housing assignment {pair.Key} has an invalid bed slot.");
                    continue;
                }
                if (!occupied.Add(assignment.BuildingId + "\n" + assignment.BedSlotIndex))
                    AddError(issues, "housing.assignment.slot_conflict", $"Housing bed {assignment.BuildingId}:{assignment.BedSlotIndex} is assigned more than once.");
            }

            foreach (string npcId in state.Housing.HomelessAdultNpcIds)
            {
                if (!state.Npcs.Instances.TryGetValue(npcId, out NpcInstanceState npc) || !HousingSystem.IsEligible(npc) ||
                    state.Housing.AssignmentsByNpcId.ContainsKey(npcId))
                    AddError(issues, "housing.homeless.invalid", $"Homeless NPC state {npcId} is invalid.");
            }
        }

        private static void CheckSurvival(GameState state, List<DiagnosticIssue> issues)
        {
            SurvivalRuntimeState survival = state.Survival;
            if (survival == null)
            {
                AddError(issues, "survival.state.null", "NPC survival runtime state cannot be null.");
                return;
            }
            if (survival.NextSettlementTick <= state.SimulationTick || survival.LastSettlementTick < 0 ||
                survival.LastSettlementTick > state.SimulationTick)
                AddError(issues, "survival.settlement_tick.invalid", "NPC survival settlement ticks are invalid.");
            if (survival.FoodRemainderQuarterUnits < 0 || survival.FoodRemainderQuarterUnits >= NpcSurvivalSystem.QuarterUnitsPerResource ||
                survival.WaterRemainderQuarterUnits < 0 || survival.WaterRemainderQuarterUnits >= NpcSurvivalSystem.QuarterUnitsPerResource)
                AddError(issues, "survival.remainder.invalid", "NPC survival fractional remainders are invalid.");
            if (!ValidSurvivalAmounts(survival.LastFoodRequired, survival.LastFoodConsumed, survival.LastFoodShortage) ||
                !ValidSurvivalAmounts(survival.LastWaterRequired, survival.LastWaterConsumed, survival.LastWaterShortage) ||
                survival.ConsecutiveFoodShortageDays < 0 || survival.ConsecutiveWaterShortageDays < 0)
                AddError(issues, "survival.amount.invalid", "NPC survival consumption or shortage amounts are invalid.");
        }

        private static bool ValidSurvivalAmounts(int required, int consumed, int shortage)
        {
            return required >= 0 && consumed >= 0 && consumed <= required && shortage == required - consumed;
        }

        private static void CheckWaste(GameState state, List<DiagnosticIssue> issues)
        {
            WasteRuntimeState waste = state.Waste;
            if (waste == null)
            {
                AddError(issues, "waste.state.null", "Waste runtime state cannot be null.");
                return;
            }
            if (waste.NextSettlementTick <= state.SimulationTick || waste.LastSettlementTick < 0 ||
                waste.LastSettlementTick > state.SimulationTick)
                AddError(issues, "waste.settlement_tick.invalid", "Waste settlement ticks are invalid.");
            if (waste.NpcHalfUnitRemainder < 0 || waste.NpcHalfUnitRemainder > 1)
                AddError(issues, "waste.remainder.invalid", "Waste NPC half-unit remainder is invalid.");
            if (waste.TotalGeneratedAmount < 0 || waste.TotalDiscardedAmount < 0 ||
                waste.TotalDiscardedAmount > waste.TotalGeneratedAmount ||
                waste.LastSettlementGeneratedAmount < 0 || waste.LastSettlementDiscardedAmount < 0 ||
                waste.LastSettlementDiscardedAmount > waste.LastSettlementGeneratedAmount ||
                waste.LastActiveBuildingCount < 0 || waste.LastLivingNpcCount < 0 ||
                waste.LastDiseaseExposureCount < 0 || waste.LastDiseaseTriggeredCount < 0 ||
                waste.LastDiseaseTriggeredCount > waste.LastDiseaseExposureCount ||
                waste.TotalDiseaseTriggeredCount < waste.LastDiseaseTriggeredCount)
                AddError(issues, "waste.amount.invalid", "Waste generation or discard amounts are invalid.");
            if (waste.AccumulatedSatisfactionPenaltyBasisPoints < 0 ||
                waste.AccumulatedSatisfactionPenaltyBasisPoints > WasteGenerationSystem.MaximumSatisfactionPenaltyBasisPoints ||
                waste.AccumulatedSatisfactionPenaltyBasisPoints % 100 != 0 ||
                (waste.DiseaseChanceBonusBasisPoints != 0 &&
                 waste.DiseaseChanceBonusBasisPoints != WasteGenerationSystem.DiseaseChanceBonusBasisPoints))
                AddError(issues, "waste.penalty.invalid", "Waste penalty state is invalid.");
        }

        private static void CheckPlacement(
            string objectId,
            int legacyLayer,
            int baseLayer,
            int rotationQuarterTurns,
            int placedWidth,
            int placedDepth,
            int placedHeight,
            int schemaVersion,
            List<DiagnosticIssue> issues)
        {
            if (schemaVersion != SpatialPlacementSchema.CurrentVersion)
            {
                AddError(issues, "placement.schema.invalid", $"Placement {objectId} must use the current spatial schema.");
                return;
            }

            if (legacyLayer != baseLayer)
            {
                AddError(issues, "placement.layer.conflict", $"Placement {objectId} has conflicting legacy and base layers.");
            }

            if (rotationQuarterTurns < 0 || rotationQuarterTurns > 3)
            {
                AddError(issues, "placement.rotation.invalid", $"Placement {objectId} has an invalid quarter-turn rotation.");
            }

            if (placedWidth <= 0 || placedDepth <= 0 || placedHeight <= 0)
            {
                AddError(issues, "placement.footprint.invalid", $"Placement {objectId} has invalid footprint dimensions.");
            }

        }

        private static void CheckSpatialOccupancy(GameState state, List<DiagnosticIssue> issues)
        {
            Dictionary<string, List<SpatialPlacement>> placementsByPlot = new Dictionary<string, List<SpatialPlacement>>(StringComparer.Ordinal);
            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance != null && !instance.IsDestroyed)
                {
                    AddPlacement(placementsByPlot, instance.PlotId, new SpatialPlacement(
                        instance.BuildingId,
                        instance.AnchorX,
                        instance.AnchorY,
                        instance.BaseLayer,
                        instance.PlacedWidth,
                        instance.PlacedDepth,
                        instance.PlacedHeight,
                        instance.RotationQuarterTurns));
                }
            }

            foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
            {
                if (task != null)
                {
                    AddPlacement(placementsByPlot, task.PlotId, new SpatialPlacement(
                        task.TaskId,
                        task.AnchorX,
                        task.AnchorY,
                        task.BaseLayer,
                        task.PlacedWidth,
                        task.PlacedDepth,
                        task.PlacedHeight,
                        task.RotationQuarterTurns));
                }
            }

            foreach (KeyValuePair<string, List<SpatialPlacement>> pair in placementsByPlot)
            {
                if (!state.World.Plots.TryGetValue(pair.Key, out PlotState plot) || plot == null)
                {
                    AddError(issues, "placement.plot.missing", $"Spatial placements reference missing plot {pair.Key}.");
                    continue;
                }

                if (plot.Width <= 0 || plot.Depth <= 0 || plot.MaxStackLayers <= 0)
                {
                    AddError(issues, "placement.plot.bounds_invalid", $"Plot {pair.Key} has invalid spatial bounds.");
                    continue;
                }

                pair.Value.Sort((left, right) => StringComparer.Ordinal.Compare(left.ObjectId, right.ObjectId));
                SpatialBounds bounds = new SpatialBounds(plot.X, plot.Y, 0, plot.Width, plot.Depth, plot.MaxStackLayers);
                List<SpatialPlacement> accepted = new List<SpatialPlacement>();
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    SpatialPlacementResult result = SpatialOccupancy.ValidatePlacement(pair.Value[i], bounds, accepted);
                    if (!result.Accepted)
                    {
                        AddError(issues, MapSpatialDiagnosticCode(result.Code), result.Reason);
                        continue;
                    }

                    accepted.Add(pair.Value[i]);
                }
            }
        }

        private static void AddPlacement(
            Dictionary<string, List<SpatialPlacement>> placementsByPlot,
            string plotId,
            SpatialPlacement placement)
        {
            string key = plotId ?? string.Empty;
            if (!placementsByPlot.TryGetValue(key, out List<SpatialPlacement> placements))
            {
                placements = new List<SpatialPlacement>();
                placementsByPlot[key] = placements;
            }

            placements.Add(placement);
        }

        private static string MapSpatialDiagnosticCode(SpatialPlacementIssueCode code)
        {
            switch (code)
            {
                case SpatialPlacementIssueCode.MissingObjectId:
                    return "placement.spatial.object_id_invalid";
                case SpatialPlacementIssueCode.InvalidDimensions:
                    return "placement.spatial.dimensions_invalid";
                case SpatialPlacementIssueCode.InvalidRotation:
                    return "placement.spatial.rotation_invalid";
                case SpatialPlacementIssueCode.FootprintTooLarge:
                    return "placement.spatial.footprint_too_large";
                case SpatialPlacementIssueCode.CoordinateOverflow:
                    return "placement.spatial.coordinate_overflow";
                case SpatialPlacementIssueCode.OutOfBounds:
                    return "placement.spatial.out_of_bounds";
                case SpatialPlacementIssueCode.Overlap:
                    return "building.layer.occupied";
                default:
                    return "placement.spatial.invalid";
            }
        }

        private static void CheckStructuralSupport(
            GameState state,
            DefinitionRegistry definitions,
            List<DiagnosticIssue> issues)
        {
            Dictionary<string, List<StructuralNode>> nodesByPlot = new Dictionary<string, List<StructuralNode>>(StringComparer.Ordinal);
            HashSet<string> invalidPlots = new HashSet<string>(StringComparer.Ordinal);

            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance == null || instance.IsDestroyed ||
                    !StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Normal))
                {
                    continue;
                }

                if (!definitions.TryGetBuilding(instance.DefinitionId, out BuildingDefinition definition))
                {
                    invalidPlots.Add(instance.PlotId ?? string.Empty);
                    continue;
                }

                AddStructuralNode(nodesByPlot, instance.PlotId, new StructuralNode(
                    instance.BuildingId,
                    new SpatialPlacement(
                        instance.BuildingId,
                        instance.AnchorX,
                        instance.AnchorY,
                        instance.BaseLayer,
                        instance.PlacedWidth,
                        instance.PlacedDepth,
                        instance.PlacedHeight,
                        instance.RotationQuarterTurns),
                    definition.Weight,
                    definition.CarryCapacity,
                    definition.CarryCapacity > 0));
            }

            foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
            {
                if (task == null)
                {
                    continue;
                }

                if (!definitions.TryGetBuilding(task.DefinitionId, out BuildingDefinition definition))
                {
                    invalidPlots.Add(task.PlotId ?? string.Empty);
                    continue;
                }

                AddStructuralNode(nodesByPlot, task.PlotId, new StructuralNode(
                    task.TaskId,
                    new SpatialPlacement(
                        task.TaskId,
                        task.AnchorX,
                        task.AnchorY,
                        task.BaseLayer,
                        task.PlacedWidth,
                        task.PlacedDepth,
                        task.PlacedHeight,
                        task.RotationQuarterTurns),
                    definition.Weight,
                    definition.CarryCapacity,
                    false));
            }

            foreach (KeyValuePair<string, List<StructuralNode>> pair in nodesByPlot)
            {
                if (invalidPlots.Contains(pair.Key))
                {
                    continue;
                }

                StructuralSupportResult result = StructuralSupport.Validate(pair.Value);
                if (!result.Accepted)
                {
                    AddError(
                        issues,
                        MapStructuralDiagnosticCode(result.Code),
                        result.Reason,
                        GetStructuralTargetIds(result),
                        95,
                        "structural_support");
                }
            }
        }

        private static IEnumerable<string> GetStructuralTargetIds(StructuralSupportResult result)
        {
            if (result == null)
            {
                return Array.Empty<string>();
            }

            List<string> ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(result.ObjectId))
            {
                ids.Add(result.ObjectId);
            }
            if (!string.IsNullOrWhiteSpace(result.SupporterId) &&
                !ids.Contains(result.SupporterId, StringComparer.Ordinal))
            {
                ids.Add(result.SupporterId);
            }

            return ids;
        }

        private static void AddStructuralNode(
            Dictionary<string, List<StructuralNode>> nodesByPlot,
            string plotId,
            StructuralNode node)
        {
            string key = plotId ?? string.Empty;
            if (!nodesByPlot.TryGetValue(key, out List<StructuralNode> nodes))
            {
                nodes = new List<StructuralNode>();
                nodesByPlot[key] = nodes;
            }

            nodes.Add(node);
        }

        private static string MapStructuralDiagnosticCode(StructuralSupportIssueCode code)
        {
            switch (code)
            {
                case StructuralSupportIssueCode.Unsupported:
                    return "structure.unsupported";
                case StructuralSupportIssueCode.InsufficientContact:
                    return "structure.contact.insufficient";
                case StructuralSupportIssueCode.CapacityExceeded:
                    return "structure.capacity.exceeded";
                case StructuralSupportIssueCode.TooManyNodes:
                    return "structure.limit.exceeded";
                case StructuralSupportIssueCode.ArithmeticOverflow:
                    return "structure.arithmetic.overflow";
                default:
                    return "structure.state.invalid";
            }
        }

        private static void AddError(
            List<DiagnosticIssue> issues,
            string code,
            string message,
            IEnumerable<string> targetIds = null,
            int? priority = null,
            string sourceSystem = null)
        {
            issues.Add(CreateIssue(DiagnosticSeverity.Error, code, message, targetIds, priority, sourceSystem));
        }

        private static void AddWarning(
            List<DiagnosticIssue> issues,
            string code,
            string message,
            IEnumerable<string> targetIds = null,
            int? priority = null,
            string sourceSystem = null)
        {
            issues.Add(CreateIssue(DiagnosticSeverity.Warning, code, message, targetIds, priority, sourceSystem));
        }

        private static void AddInfo(
            List<DiagnosticIssue> issues,
            string code,
            string message,
            IEnumerable<string> targetIds = null,
            int? priority = null,
            string sourceSystem = null)
        {
            issues.Add(CreateIssue(DiagnosticSeverity.Info, code, message, targetIds, priority, sourceSystem));
        }

        private static DiagnosticIssue CreateIssue(
            DiagnosticSeverity severity,
            string code,
            string message,
            IEnumerable<string> targetIds,
            int? priority,
            string sourceSystem)
        {
            return new DiagnosticIssue
            {
                Severity = severity,
                Code = code,
                Message = message,
                TargetIds = targetIds == null ? new List<string>() : targetIds.ToList(),
                Priority = priority,
                SourceSystem = sourceSystem ?? string.Empty
            };
        }

        private static string ToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
