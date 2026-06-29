using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class ContinuousProductionSystem : ISimulationSystem
    {
        public const string ApplyFertilizerCommand = "command:core:apply_fertilizer";
        public const string OutputStoredEvent = "event:core:continuous_output_stored";
        public const string OutputLostEvent = "event:core:continuous_output_lost";
        public const string FertilizerAppliedEvent = "event:core:fertilizer_applied";
        public const string FertilizerBonusProducedEvent = "event:core:fertilizer_bonus_produced";

        private readonly JsonSerializerOptions _jsonOptions;

        public ContinuousProductionSystem(JsonSerializerOptions jsonOptions = null)
        {
            _jsonOptions = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
        }

        public void RegisterCommands(CommandBus commandBus)
        {
            if (commandBus == null) throw new ArgumentNullException(nameof(commandBus));
            commandBus.Register(new ApplyFertilizerHandler(_jsonOptions));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            List<GameEvent> events = new List<GameEvent>();
            ReconcileStates(context);
            Dictionary<string, long> sunlampConsumedUntilTick = new Dictionary<string, long>(StringComparer.Ordinal);

            foreach (string buildingId in context.State.ContinuousProduction.Buildings.Keys
                .OrderBy(id => id, StringComparer.Ordinal).ToArray())
            {
                ContinuousProductionBuildingState runtime = context.State.ContinuousProduction.Buildings[buildingId];
                if (!context.State.Buildings.Instances.TryGetValue(buildingId, out BuildingInstanceState building) ||
                    building == null || building.IsDestroyed ||
                    !context.Definitions.TryGetContinuousProduction(
                        building.DefinitionId, out ContinuousProductionDefinition definition))
                {
                    context.State.ContinuousProduction.Buildings.Remove(buildingId);
                    continue;
                }

                if (runtime.PendingOutputAmount > 0)
                {
                    int stored = StoreOutput(context.State, building, definition.OutputResourceId, runtime.PendingOutputAmount);
                    runtime.PendingOutputAmount -= stored;
                    EmitStored(context, events, buildingId, definition.OutputResourceId, stored, runtime.PendingOutputAmount);
                    if (runtime.PendingOutputAmount > 0)
                    {
                        runtime.Status = ContinuousProductionStatuses.OutputPending;
                        continue;
                    }
                }

                foreach (string resourceId in runtime.AdditionalPendingOutputs.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray())
                {
                    int pending = runtime.AdditionalPendingOutputs[resourceId];
                    int stored = StoreOutput(context.State, building, resourceId, pending);
                    pending -= stored;
                    if (pending > 0) runtime.AdditionalPendingOutputs[resourceId] = pending;
                    else runtime.AdditionalPendingOutputs.Remove(resourceId);
                    EmitStored(context, events, buildingId, resourceId, stored, pending);
                }
                if (HasAdditionalPending(runtime))
                {
                    runtime.Status = ContinuousProductionStatuses.OutputPending;
                    continue;
                }

                if (!BuildingOperationalRules.CanProduce(building))
                {
                    runtime.Status = ContinuousProductionStatuses.PausedBuilding;
                    continue;
                }

                int workerCount = CountWorkers(context.State, buildingId);
                if (workerCount <= 0)
                {
                    runtime.Status = ContinuousProductionStatuses.PausedNoWorkers;
                    continue;
                }

                if (StringComparer.Ordinal.Equals(building.DefinitionId, CoreBuildingIds.Farm))
                {
                    ProcessFarmProductionSegments(
                        context, events, buildingId, building, definition, runtime, workerCount, deltaTicks,
                        sunlampConsumedUntilTick);
                    continue;
                }

                ProcessContinuousProductionTicks(
                    context, events, buildingId, building, definition, runtime, workerCount, deltaTicks);
            }

            return events;
        }

        private static void ProcessFarmProductionSegments(
            SimulationContext context,
            List<GameEvent> events,
            string buildingId,
            BuildingInstanceState building,
            ContinuousProductionDefinition definition,
            ContinuousProductionBuildingState runtime,
            int workerCount,
            long deltaTicks,
            Dictionary<string, long> sunlampConsumedUntilTick)
        {
            long endTick = context.State.SimulationTick;
            long currentTick = checked(endTick - deltaTicks);

            while (currentTick < endTick)
            {
                long segmentEnd = GetNextDayNightBoundary(currentTick, endTick);
                long segmentTicks = checked(segmentEnd - currentTick);
                if (segmentTicks <= 0) break;

                long lightAvailableTicks = ResolveAgriculturalLightTicks(
                    context.State, building, currentTick, segmentTicks, sunlampConsumedUntilTick);
                if (lightAvailableTicks <= 0)
                {
                    runtime.Status = ContinuousProductionStatuses.PausedNoLight;
                    currentTick = segmentEnd;
                    continue;
                }

                bool completedSegment = ProcessContinuousProductionTicks(
                    context, events, buildingId, building, definition, runtime, workerCount, lightAvailableTicks);
                if (!completedSegment)
                {
                    return;
                }

                currentTick = checked(currentTick + lightAvailableTicks);
            }
        }

        private static long GetNextDayNightBoundary(long currentTick, long endTick)
        {
            long tickWithinDay = DayNightCycle.GetTickWithinDay(currentTick);
            long ticksToBoundary = tickWithinDay < DayNightCycle.TicksPerHalfDay
                ? DayNightCycle.TicksPerHalfDay - tickWithinDay
                : GameTime.TicksPerGameDay - tickWithinDay;
            long boundary = checked(currentTick + ticksToBoundary);
            return Math.Min(boundary, endTick);
        }

        private static bool ProcessContinuousProductionTicks(
            SimulationContext context,
            List<GameEvent> events,
            string buildingId,
            BuildingInstanceState building,
            ContinuousProductionDefinition definition,
            ContinuousProductionBuildingState runtime,
            int workerCount,
            long deltaTicks)
        {
            int efficiencyBasisPoints = WasteEffectRules.ResolveWorkEfficiencyBasisPoints(context.State);
            long rateUnitsPerTick = checked((long)workerCount * definition.OutputPerWorkerPerDay);
            int freeCapacity = GetOutputCapacity(context.State, building, definition.OutputResourceId);
            long effectiveTicksToBlockedOutput = TicksToGenerate(
                checked((long)freeCapacity + 1), runtime.ProgressUnits, rateUnitsPerTick);
            long ticksToBlockedOutput = WasteEffectRules.ActualTicksToReachEffectiveTicks(
                effectiveTicksToBlockedOutput,
                efficiencyBasisPoints,
                runtime.EfficiencyRemainderBasisPointTicks);
            long availableTicks = Math.Min(deltaTicks, ticksToBlockedOutput);
            foreach (KeyValuePair<string, int> output in definition.AdditionalOutputsPerWorkerPerDay)
            {
                long progress = runtime.AdditionalProgressUnits.TryGetValue(output.Key, out long value) ? value : 0;
                long rate = checked((long)workerCount * output.Value);
                int capacity = GetOutputCapacity(context.State, building, output.Key);
                long effectiveTicks = TicksToGenerate(checked((long)capacity + 1), progress, rate);
                availableTicks = Math.Min(availableTicks,
                    WasteEffectRules.ActualTicksToReachEffectiveTicks(
                        effectiveTicks,
                        efficiencyBasisPoints,
                        runtime.EfficiencyRemainderBasisPointTicks));
            }

            if (definition.OperatingInputPerDay > 0)
            {
                long inputCycles = GetAvailableInput(context.State, building, definition.OperatingInputResourceId) /
                                   definition.OperatingInputPerDay;
                long maximumInputTicks = checked(runtime.InputCoverageTicks +
                    checked(inputCycles * GameTime.TicksPerGameDay));
                availableTicks = Math.Min(availableTicks, maximumInputTicks);
            }

            if (availableTicks <= 0)
            {
                runtime.Status = definition.OperatingInputPerDay > 0
                    ? ContinuousProductionStatuses.PausedInput
                    : ContinuousProductionStatuses.OutputPending;
                return false;
            }

            if (definition.OperatingInputPerDay > 0)
            {
                long uncoveredTicks = Math.Max(0, availableTicks - runtime.InputCoverageTicks);
                long cycles = DivideRoundUp(uncoveredTicks, GameTime.TicksPerGameDay);
                int inputAmount = checked((int)(cycles * definition.OperatingInputPerDay));
                if (inputAmount > 0)
                {
                    ConsumeInput(context.State, building, definition.OperatingInputResourceId, inputAmount);
                    runtime.InputCoverageTicks = checked(runtime.InputCoverageTicks +
                        checked(cycles * GameTime.TicksPerGameDay));
                }
                runtime.InputCoverageTicks -= availableTicks;
            }

            int efficiencyRemainder = runtime.EfficiencyRemainderBasisPointTicks;
            long effectiveAvailableTicks = WasteEffectRules.ApplyEfficiency(
                availableTicks,
                efficiencyBasisPoints,
                ref efficiencyRemainder);
            runtime.EfficiencyRemainderBasisPointTicks = efficiencyRemainder;
            long totalUnits = checked(runtime.ProgressUnits + checked(effectiveAvailableTicks * rateUnitsPerTick));
            int generated = checked((int)(totalUnits / GameTime.TicksPerGameDay));
            runtime.ProgressUnits = totalUnits % GameTime.TicksPerGameDay;
            int fertilizerBonus = ApplyFertilizerBonus(runtime, generated);
            generated = checked(generated + fertilizerBonus);
            if (fertilizerBonus > 0)
            {
                events.Add(context.Events.Create(FertilizerBonusProducedEvent, "system:core:continuous_production",
                    new FertilizerBonusPayload
                    {
                        BuildingId = buildingId,
                        BonusAmount = fertilizerBonus,
                        BaseOutputRemaining = runtime.FertilizerBaseOutputRemaining
                    }));
            }
            int transferred = StoreOutput(context.State, building, definition.OutputResourceId, generated);
            runtime.PendingOutputAmount = generated - transferred;
            EmitStored(context, events, buildingId, definition.OutputResourceId, transferred, runtime.PendingOutputAmount);
            foreach (KeyValuePair<string, int> output in definition.AdditionalOutputsPerWorkerPerDay.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                long progress = runtime.AdditionalProgressUnits.TryGetValue(output.Key, out long value) ? value : 0;
                long outputTotal = checked(progress + checked(effectiveAvailableTicks * workerCount * (long)output.Value));
                int outputGenerated = checked((int)(outputTotal / GameTime.TicksPerGameDay));
                runtime.AdditionalProgressUnits[output.Key] = outputTotal % GameTime.TicksPerGameDay;
                int outputStored = StoreOutput(context.State, building, output.Key, outputGenerated);
                int outputPending = outputGenerated - outputStored;
                if (outputPending > 0) runtime.AdditionalPendingOutputs[output.Key] = outputPending;
                else runtime.AdditionalPendingOutputs.Remove(output.Key);
                EmitStored(context, events, buildingId, output.Key, outputStored, outputPending);
            }
            runtime.Status = runtime.PendingOutputAmount > 0 || HasAdditionalPending(runtime)
                ? ContinuousProductionStatuses.OutputPending
                : availableTicks < deltaTicks && definition.OperatingInputPerDay > 0
                    ? ContinuousProductionStatuses.PausedInput
                    : ContinuousProductionStatuses.Running;
            return availableTicks >= deltaTicks && StringComparer.Ordinal.Equals(
                runtime.Status, ContinuousProductionStatuses.Running);
        }

        private static long ResolveAgriculturalLightTicks(
            GameState state,
            BuildingInstanceState farm,
            long simulationTick,
            long segmentTicks,
            Dictionary<string, long> sunlampConsumedUntilTick)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (farm == null) throw new ArgumentNullException(nameof(farm));
            if (sunlampConsumedUntilTick == null) throw new ArgumentNullException(nameof(sunlampConsumedUntilTick));
            if (!state.World.Plots.TryGetValue(farm.PlotId, out PlotState plot) || plot == null)
            {
                return segmentTicks;
            }

            List<BuildingInstanceState> activeSunlamps = state.Buildings.Instances.Values
                .Where(instance => instance != null &&
                    StringComparer.Ordinal.Equals(instance.DefinitionId, CoreBuildingIds.Sunlamp) &&
                    BuildingOperationalRules.IsOperational(instance))
                .ToList();

            if (DayNightCycle.GetPhase(simulationTick) == DayNightPhase.Night)
            {
                IReadOnlyList<string> sunlampIds = AgriculturalLightRules.SelectSunlampIdsForFullCoverage(
                    farm,
                    activeSunlamps,
                    plot.X,
                    plot.Y,
                    plot.Width,
                    plot.Depth,
                    plot.MaxStackLayers);
                return ReserveSunlampCoverageTicks(state, sunlampIds, simulationTick, segmentTicks, sunlampConsumedUntilTick);
            }

            if (!AgriculturalLightRules.IsPhysicallyOccluded(
                farm,
                state.Buildings.Instances.Values))
            {
                return segmentTicks;
            }

            IReadOnlyList<string> selectedSunlampIds = AgriculturalLightRules.SelectSunlampIdsForFullCoverage(
                farm,
                activeSunlamps,
                plot.X,
                plot.Y,
                plot.Width,
                plot.Depth,
                plot.MaxStackLayers);
            return ReserveSunlampCoverageTicks(state, selectedSunlampIds, simulationTick, segmentTicks, sunlampConsumedUntilTick);
        }

        private static long ReserveSunlampCoverageTicks(
            GameState state,
            IReadOnlyList<string> sunlampIds,
            long segmentStartTick,
            long segmentTicks,
            Dictionary<string, long> sunlampConsumedUntilTick)
        {
            if (sunlampIds == null || sunlampIds.Count == 0) return 0;

            long availableTicks = segmentTicks;
            foreach (string sunlampId in sunlampIds)
            {
                if (!state.Buildings.Instances.TryGetValue(sunlampId, out BuildingInstanceState sunlamp) ||
                    sunlamp == null ||
                    !BuildingOperationalRules.IsOperational(sunlamp))
                {
                    return 0;
                }

                state.Sunlamps.Buildings.TryGetValue(sunlampId, out SunlampBuildingState runtime);
                long consumedUntil = sunlampConsumedUntilTick.TryGetValue(sunlampId, out long value)
                    ? Math.Max(value, segmentStartTick)
                    : segmentStartTick;
                long ticksAlreadyReserved = Math.Max(0, consumedUntil - segmentStartTick);
                long ticksToReserve = Math.Max(0, segmentTicks - ticksAlreadyReserved);
                if (ticksToReserve <= 0) continue;

                if ((runtime == null || runtime.FuelCoverageTicks <= 0) && !TryConsumeSunlampFuel(state, sunlamp))
                {
                    return ticksAlreadyReserved;
                }

                runtime = GetOrCreateSunlampRuntime(state, sunlampId);
                availableTicks = Math.Min(availableTicks, checked(ticksAlreadyReserved + runtime.FuelCoverageTicks));
            }

            if (availableTicks <= 0) return 0;
            foreach (string sunlampId in sunlampIds)
            {
                SunlampBuildingState runtime = GetOrCreateSunlampRuntime(state, sunlampId);
                long consumedUntil = sunlampConsumedUntilTick.TryGetValue(sunlampId, out long value)
                    ? Math.Max(value, segmentStartTick)
                    : segmentStartTick;
                long ticksAlreadyReserved = Math.Max(0, consumedUntil - segmentStartTick);
                long ticksToReserve = Math.Max(0, availableTicks - ticksAlreadyReserved);
                if (ticksToReserve <= 0) continue;
                runtime.FuelCoverageTicks -= ticksToReserve;
                sunlampConsumedUntilTick[sunlampId] = checked(segmentStartTick + availableTicks);
            }

            return availableTicks;
        }

        private static SunlampBuildingState GetOrCreateSunlampRuntime(GameState state, string sunlampId)
        {
            if (!state.Sunlamps.Buildings.TryGetValue(sunlampId, out SunlampBuildingState runtime))
            {
                runtime = new SunlampBuildingState { BuildingId = sunlampId };
                state.Sunlamps.Buildings.Add(sunlampId, runtime);
            }
            return runtime;
        }

        private static bool TryConsumeSunlampFuel(GameState state, BuildingInstanceState sunlamp)
        {
            if (GetAvailableInput(state, sunlamp, CoreResourceIds.Fuel) < 1)
            {
                return false;
            }

            ConsumeInput(state, sunlamp, CoreResourceIds.Fuel, 1);
            GetOrCreateSunlampRuntime(state, sunlamp.BuildingId).FuelCoverageTicks = GameTime.TicksPerGameDay;
            return true;
        }

        public static ValidationResult ValidateDemolition(GameState state, string buildingId)
        {
            if (state.ContinuousProduction != null &&
                state.ContinuousProduction.Buildings.TryGetValue(buildingId, out ContinuousProductionBuildingState runtime) &&
                runtime != null && (runtime.PendingOutputAmount > 0 || HasAdditionalPending(runtime)))
            {
                return ValidationResult.Invalid(
                    $"Building {buildingId} has pending continuous-production output.",
                    CommandErrorCodes.ProductionOutputPending);
            }
            return ValidationResult.Valid();
        }

        public static void HandleBuildingDestroyed(
            SimulationContext context,
            string buildingId,
            List<GameEvent> events)
        {
            if (context.State.ContinuousProduction.Buildings.TryGetValue(
                buildingId, out ContinuousProductionBuildingState runtime))
            {
                if (runtime.PendingOutputAmount > 0 &&
                    context.State.Buildings.Instances.TryGetValue(buildingId, out BuildingInstanceState building) &&
                    context.Definitions.TryGetContinuousProduction(
                        building.DefinitionId, out ContinuousProductionDefinition definition))
                {
                    events.Add(context.Events.Create(OutputLostEvent, "system:core:continuous_production",
                        new ContinuousProductionOutputPayload
                        {
                            BuildingId = buildingId,
                            ResourceId = definition.OutputResourceId,
                            Amount = 0,
                            PendingAmount = runtime.PendingOutputAmount
                        }));
                }
                foreach (KeyValuePair<string, int> pending in runtime.AdditionalPendingOutputs)
                {
                    if (pending.Value <= 0) continue;
                    events.Add(context.Events.Create(OutputLostEvent, "system:core:continuous_production",
                        new ContinuousProductionOutputPayload
                        {
                            BuildingId = buildingId,
                            ResourceId = pending.Key,
                            Amount = 0,
                            PendingAmount = pending.Value
                        }));
                }
                context.State.ContinuousProduction.Buildings.Remove(buildingId);
            }
        }

        private static void ReconcileStates(SimulationContext context)
        {
            foreach (BuildingInstanceState building in context.State.Buildings.Instances.Values
                .Where(value => value != null && !value.IsDestroyed)
                .OrderBy(value => value.BuildingId, StringComparer.Ordinal))
            {
                if (context.Definitions.TryGetContinuousProduction(building.DefinitionId, out _) &&
                    !context.State.ContinuousProduction.Buildings.ContainsKey(building.BuildingId))
                {
                    context.State.ContinuousProduction.Buildings.Add(building.BuildingId,
                        new ContinuousProductionBuildingState { BuildingId = building.BuildingId });
                }
            }
        }

        private static int CountWorkers(GameState state, string buildingId)
        {
            int count = 0;
            foreach (WorkAssignmentState assignment in state.Npcs.WorkAssignments.Values)
            {
                if (assignment != null && StringComparer.Ordinal.Equals(assignment.BuildingId, buildingId)) count++;
            }
            return count;
        }

        private static int ApplyFertilizerBonus(ContinuousProductionBuildingState runtime, int generated)
        {
            if (generated <= 0 || runtime.FertilizerBaseOutputRemaining <= 0) return 0;
            int covered = Math.Min(generated, runtime.FertilizerBaseOutputRemaining);
            int halfUnits = checked(runtime.FertilizerBonusHalfUnitRemainder + covered);
            int bonus = halfUnits / 2;
            runtime.FertilizerBonusHalfUnitRemainder = halfUnits % 2;
            runtime.FertilizerBaseOutputRemaining -= covered;
            if (runtime.FertilizerBaseOutputRemaining == 0)
                runtime.FertilizerBonusHalfUnitRemainder = 0;
            return bonus;
        }

        private sealed class ApplyFertilizerHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public ApplyFertilizerHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return ApplyFertilizerCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                ApplyFertilizerPayload payload = Deserialize<ApplyFertilizerPayload>(command.Payload, _options);
                if (!context.State.Buildings.Instances.TryGetValue(payload.BuildingId, out BuildingInstanceState building))
                    return ValidationResult.Invalid("Unknown farm.", CommandErrorCodes.BuildingNotFound);
                if (!StringComparer.Ordinal.Equals(building.DefinitionId, CoreBuildingIds.Farm) ||
                    !BuildingOperationalRules.CanProduce(building) ||
                    !context.Definitions.TryGetContinuousProduction(building.DefinitionId, out _))
                    return ValidationResult.Invalid("Fertilizer can only be applied to an operational farm.",
                        CommandErrorCodes.BuildingNotOperational);
                if (context.State.ContinuousProduction.Buildings.TryGetValue(payload.BuildingId,
                        out ContinuousProductionBuildingState runtime) && runtime.FertilizerBaseOutputRemaining > 0)
                    return ValidationResult.Invalid("This farm already has an active fertilizer cycle.",
                        CommandErrorCodes.ProductionBatchActive);
                if (CountWorkers(context.State, payload.BuildingId) <= 0)
                    return ValidationResult.Invalid("A staffed farm is required before applying fertilizer.",
                        CommandErrorCodes.NpcUnavailable);
                if (GetAvailableInput(context.State, building, CoreResourceIds.Fertilizer) < 1)
                    return ValidationResult.Invalid("No fertilizer is available.",
                        CommandErrorCodes.ProductionInputUnavailable);
                return ValidationResult.Valid();
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                ApplyFertilizerPayload payload = Deserialize<ApplyFertilizerPayload>(command.Payload, _options);
                BuildingInstanceState building = context.State.Buildings.Instances[payload.BuildingId];
                int workers = CountWorkers(context.State, payload.BuildingId);
                context.Definitions.TryGetContinuousProduction(building.DefinitionId,
                    out ContinuousProductionDefinition definition);
                ConsumeInput(context.State, building, CoreResourceIds.Fertilizer, 1);
                if (!context.State.ContinuousProduction.Buildings.TryGetValue(payload.BuildingId,
                    out ContinuousProductionBuildingState runtime))
                {
                    runtime = new ContinuousProductionBuildingState { BuildingId = payload.BuildingId };
                    context.State.ContinuousProduction.Buildings[payload.BuildingId] = runtime;
                }
                runtime.FertilizerBaseOutputRemaining = checked(workers * definition.OutputPerWorkerPerDay);
                runtime.FertilizerBonusHalfUnitRemainder = 0;
                return new[]
                {
                    context.Events.Create(FertilizerAppliedEvent, command.CommandId, new FertilizerAppliedPayload
                    {
                        BuildingId = payload.BuildingId,
                        WorkerCount = workers,
                        BaseOutputCovered = runtime.FertilizerBaseOutputRemaining
                    })
                };
            }
        }

        private static T Deserialize<T>(JsonElement payload, JsonSerializerOptions options)
        {
            T value = payload.Deserialize<T>(options);
            if (value == null) throw new InvalidOperationException("Command payload is required.");
            return value;
        }

        private static bool HasAdditionalPending(ContinuousProductionBuildingState runtime)
        {
            return runtime.AdditionalPendingOutputs != null && runtime.AdditionalPendingOutputs.Values.Any(value => value > 0);
        }

        private static long TicksToGenerate(long amount, long progressUnits, long rateUnitsPerTick)
        {
            return DivideRoundUp(checked(amount * GameTime.TicksPerGameDay - progressUnits), rateUnitsPerTick);
        }

        private static long DivideRoundUp(long value, long divisor)
        {
            if (value <= 0) return 0;
            return checked((value + divisor - 1) / divisor);
        }

        private static int GetAvailableInput(GameState state, BuildingInstanceState building, string resourceId)
        {
            int available = 0;
            if (state.Resources.Items.TryGetValue(resourceId, out ResourceStack global))
                available = checked(available + Math.Max(0, global.Amount - global.LockedAmount));
            if (building.LocalInventory.TryGetValue(resourceId, out LocalResourceStack local))
                available = checked(available + Math.Max(0, local.Amount - local.LockedAmount));
            return available;
        }

        private static void ConsumeInput(
            GameState state, BuildingInstanceState building, string resourceId, int amount)
        {
            int remaining = amount;
            if (state.Resources.Items.TryGetValue(resourceId, out ResourceStack global))
            {
                int consumed = Math.Min(remaining, Math.Max(0, global.Amount - global.LockedAmount));
                global.Amount -= consumed;
                remaining -= consumed;
            }
            if (remaining > 0 && building.LocalInventory.TryGetValue(resourceId, out LocalResourceStack local))
            {
                int consumed = Math.Min(remaining, Math.Max(0, local.Amount - local.LockedAmount));
                local.Amount -= consumed;
                remaining -= consumed;
            }
            if (remaining != 0) throw new InvalidOperationException("Continuous production input plan became invalid.");
        }

        private static int GetOutputCapacity(GameState state, BuildingInstanceState building, string resourceId)
        {
            int localUsed = building.LocalInventory.Values.Where(stack => stack != null).Sum(stack => stack.Amount);
            int localFree = Math.Max(0, building.LocalInventoryCapacity - building.LocalInventoryReservedAmount - localUsed);
            int globalFree = 0;
            if (state.Resources.Items.TryGetValue(resourceId, out ResourceStack global))
                globalFree = StorageCapacityRules.ResourceFreeCapacity(state.Resources, global);
            return checked(localFree + globalFree);
        }

        private static int StoreOutput(
            GameState state, BuildingInstanceState building, string resourceId, int amount)
        {
            if (amount <= 0) return 0;
            int remaining = amount;
            int localUsed = building.LocalInventory.Values.Where(stack => stack != null).Sum(stack => stack.Amount);
            int localFree = Math.Max(0, building.LocalInventoryCapacity - building.LocalInventoryReservedAmount - localUsed);
            int localAmount = Math.Min(remaining, localFree);
            if (localAmount > 0)
            {
                if (!building.LocalInventory.TryGetValue(resourceId, out LocalResourceStack local))
                {
                    local = new LocalResourceStack { ResourceId = resourceId };
                    building.LocalInventory.Add(resourceId, local);
                }
                local.Amount += localAmount;
                remaining -= localAmount;
            }
            if (remaining > 0 && state.Resources.Items.TryGetValue(resourceId, out ResourceStack global))
            {
                int globalAmount = Math.Min(remaining,
                    StorageCapacityRules.ResourceFreeCapacity(state.Resources, global));
                global.Amount += globalAmount;
                remaining -= globalAmount;
            }
            return amount - remaining;
        }

        private static void EmitStored(
            SimulationContext context,
            List<GameEvent> events,
            string buildingId,
            string resourceId,
            int amount,
            int pending)
        {
            if (amount <= 0) return;
            events.Add(context.Events.Create(OutputStoredEvent, "system:core:continuous_production",
                new ContinuousProductionOutputPayload
                {
                    BuildingId = buildingId,
                    ResourceId = resourceId,
                    Amount = amount,
                    PendingAmount = pending
                }));
        }
    }

    public sealed class ContinuousProductionOutputPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int PendingAmount { get; set; }
    }

    public sealed class ApplyFertilizerPayload
    {
        public string BuildingId { get; set; } = string.Empty;
    }

    public sealed class FertilizerAppliedPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public int WorkerCount { get; set; }
        public int BaseOutputCovered { get; set; }
    }

    public sealed class FertilizerBonusPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public int BonusAmount { get; set; }
        public int BaseOutputRemaining { get; set; }
    }
}
