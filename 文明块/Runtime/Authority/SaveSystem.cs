using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class SaveSystem
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public SaveSystem(JsonSerializerOptions jsonOptions = null)
        {
            _jsonOptions = jsonOptions ?? CreateDefaultJsonOptions();
        }

        public string Serialize(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return JsonSerializer.Serialize(state, _jsonOptions);
        }

        public GameState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Save json cannot be empty.", nameof(json));
            }

            GameState state = JsonSerializer.Deserialize<GameState>(json, _jsonOptions);
            if (state == null)
            {
                throw new InvalidOperationException("Save json could not be deserialized.");
            }

            SaveMigration.RepairAfterLoad(state);
            return state;
        }

        public static JsonSerializerOptions CreateDefaultJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }
    }

    public static class SaveMigration
    {
        public static void RepairAfterLoad(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            bool migrateSurvival = string.IsNullOrWhiteSpace(state.SaveVersion) ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.0") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.1") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.2") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.3") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.4") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.5") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.6") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.7") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.8") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "1.9") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "2.0") ||
                StringComparer.Ordinal.Equals(state.SaveVersion, "2.1");
            bool migrateLifecycle = migrateSurvival || StringComparer.Ordinal.Equals(state.SaveVersion, "2.2");
            bool migrateContinuousAdditional = migrateLifecycle || StringComparer.Ordinal.Equals(state.SaveVersion, "2.3");
            bool migrateSharedStorage = migrateContinuousAdditional || StringComparer.Ordinal.Equals(state.SaveVersion, "2.4");
            bool migrateWaste = migrateSharedStorage || StringComparer.Ordinal.Equals(state.SaveVersion, "2.5");
            bool migrateWasteEffects = migrateWaste || StringComparer.Ordinal.Equals(state.SaveVersion, "2.6");
            bool migrateFertilizer = migrateWasteEffects || StringComparer.Ordinal.Equals(state.SaveVersion, "2.7");
            if (migrateFertilizer)
            {
                state.SaveVersion = "2.8";
            }
            state.PlayerId = string.IsNullOrWhiteSpace(state.PlayerId) ? "player:core:local" : state.PlayerId;

            if (state.World == null)
            {
                state.World = new WorldState();
            }

            if (state.Resources == null)
            {
                state.Resources = new ResourceState();
            }
            if (migrateSharedStorage)
            {
                state.Resources.SharedCapacity = StorageCapacityRules.BaseSharedCapacity;
            }

            if (state.Buildings == null)
            {
                state.Buildings = new BuildingRuntimeState();
            }

            if (state.Difficulty == null)
            {
                state.Difficulty = new DifficultyState();
            }

            if (state.Npcs == null)
            {
                state.Npcs = new NpcRuntimeState();
            }

            if (state.Housing == null)
            {
                state.Housing = new HousingRuntimeState();
            }

            if (state.Survival == null || migrateSurvival)
            {
                state.Survival = new SurvivalRuntimeState
                {
                    NextSettlementTick = NpcSurvivalSystem.NextDayBoundary(state.SimulationTick)
                };
            }

            if (state.Waste == null || migrateWaste)
            {
                state.Waste = new WasteRuntimeState
                {
                    NextSettlementTick = NpcSurvivalSystem.NextDayBoundary(state.SimulationTick)
                };
            }

            if (state.Production == null)
            {
                state.Production = new ProductionRuntimeState();
            }

            if (state.ContinuousProduction == null)
            {
                state.ContinuousProduction = new ContinuousProductionRuntimeState();
            }

            if (state.Logistics == null)
            {
                state.Logistics = new LogisticsRuntimeState();
            }

            if (state.Commands == null)
            {
                state.Commands = new CommandHistoryState();
            }

            if (state.Events == null)
            {
                state.Events = new EventLogState();
            }

            if (state.World.Plots == null)
            {
                state.World.Plots = new Dictionary<string, PlotState>(StringComparer.Ordinal);
            }

            if (state.Resources.Items == null)
            {
                state.Resources.Items = new Dictionary<string, ResourceStack>(StringComparer.Ordinal);
            }

            if (state.Buildings.Instances == null)
            {
                state.Buildings.Instances = new Dictionary<string, BuildingInstanceState>(StringComparer.Ordinal);
            }

            if (state.Buildings.ConstructionTasks == null)
            {
                state.Buildings.ConstructionTasks = new Dictionary<string, ConstructionTaskState>(StringComparer.Ordinal);
            }

            if (state.Buildings.FirstHomeBonus == null)
            {
                state.Buildings.FirstHomeBonus = new FirstBuildBonusState();
            }

            if (state.Buildings.FirstBasicProductionBonus == null)
            {
                state.Buildings.FirstBasicProductionBonus = new FirstBuildBonusState();
            }

            if (state.Npcs.Instances == null)
            {
                state.Npcs.Instances = new Dictionary<string, NpcInstanceState>(StringComparer.Ordinal);
            }

            if (state.Npcs.WorkAssignments == null)
            {
                state.Npcs.WorkAssignments = new Dictionary<string, WorkAssignmentState>(StringComparer.Ordinal);
            }
            if (migrateWasteEffects)
            {
                foreach (NpcInstanceState npc in state.Npcs.Instances.Values)
                {
                    if (npc != null) npc.BaseSatisfactionBasisPoints = WasteEffectRules.BasisPointsPerWhole;
                }
            }

            if (state.Housing.AssignmentsByNpcId == null)
            {
                state.Housing.AssignmentsByNpcId = new Dictionary<string, HousingAssignmentState>(StringComparer.Ordinal);
            }

            if (state.Housing.HomelessAdultNpcIds == null)
            {
                state.Housing.HomelessAdultNpcIds = new HashSet<string>(StringComparer.Ordinal);
            }

            if (state.Production.SlotsByBuildingId == null)
            {
                state.Production.SlotsByBuildingId = new Dictionary<string, ProductionSlotState>(StringComparer.Ordinal);
            }

            if (state.ContinuousProduction.Buildings == null)
            {
                state.ContinuousProduction.Buildings =
                    new Dictionary<string, ContinuousProductionBuildingState>(StringComparer.Ordinal);
            }
            foreach (ContinuousProductionBuildingState runtime in state.ContinuousProduction.Buildings.Values)
            {
                if (runtime == null) continue;
                if (runtime.AdditionalProgressUnits == null)
                    runtime.AdditionalProgressUnits = new Dictionary<string, long>(StringComparer.Ordinal);
                if (runtime.AdditionalPendingOutputs == null)
                    runtime.AdditionalPendingOutputs = new Dictionary<string, int>(StringComparer.Ordinal);
            }

            if (state.Logistics.ActiveTasks == null)
            {
                state.Logistics.ActiveTasks = new Dictionary<string, TransportTaskState>(StringComparer.Ordinal);
            }

            if (state.Logistics.Routes == null)
            {
                state.Logistics.Routes = new Dictionary<string, LogisticsRouteState>(StringComparer.Ordinal);
            }

            if (state.Logistics.ConstructionTasks == null)
            {
                state.Logistics.ConstructionTasks = new Dictionary<string, LogisticsConnectorConstructionState>(StringComparer.Ordinal);
            }

            if (state.Logistics.Connectors == null)
            {
                state.Logistics.Connectors = new Dictionary<string, LogisticsConnectorInstanceState>(StringComparer.Ordinal);
            }

            if (state.Commands.ProcessedCommandIds == null)
            {
                state.Commands.ProcessedCommandIds = new HashSet<string>(StringComparer.Ordinal);
            }

            if (state.Commands.LastAcceptedSequenceByPlayer == null)
            {
                state.Commands.LastAcceptedSequenceByPlayer = new Dictionary<string, long>(StringComparer.Ordinal);
            }

            if (state.Events.Events == null)
            {
                state.Events.Events = new List<GameEvent>();
            }

            if (state.Survival.NextSettlementTick <= 0)
            {
                state.Survival.NextSettlementTick = NpcSurvivalSystem.NextDayBoundary(state.SimulationTick);
            }
            if (state.Waste != null && state.Waste.NextSettlementTick <= 0)
            {
                state.Waste.NextSettlementTick = NpcSurvivalSystem.NextDayBoundary(state.SimulationTick);
            }

            RepairSpatialPlacements(state);
            RepairStructuralIncidentState(state);
            RepairNpcLifecycleState(state, migrateLifecycle);
            ValidateNpcState(state);
            ValidateHousingState(state);
            ValidateSurvivalState(state);
            ValidateWasteState(state);
            ValidateProductionState(state);
            ValidateContinuousProductionState(state);
            DifficultyProfiles.ResolveStructuralFailure(state.Difficulty);
            ValidateSpatialOccupancy(state);
            RepairSequences(state);
            RepairFirstBuildBonus(state.Buildings.FirstHomeBonus, state);
            RepairFirstBuildBonus(state.Buildings.FirstBasicProductionBonus, state);
        }

        private static void ValidateHousingState(GameState state)
        {
            HashSet<string> occupiedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, HousingAssignmentState> pair in state.Housing.AssignmentsByNpcId)
            {
                HousingAssignmentState assignment = pair.Value;
                if (assignment == null || !StringComparer.Ordinal.Equals(pair.Key, assignment.NpcId) ||
                    !StableId.IsValid(assignment.NpcId) || !StableId.IsValid(assignment.BuildingId) ||
                    !state.Npcs.Instances.TryGetValue(assignment.NpcId, out NpcInstanceState npc) ||
                    !HousingSystem.IsEligible(npc) ||
                    !state.Buildings.Instances.TryGetValue(assignment.BuildingId, out BuildingInstanceState building) ||
                    building == null || building.IsDestroyed || assignment.BedSlotIndex < 0 || assignment.AssignedTick < 0 ||
                    !occupiedSlots.Add(assignment.BuildingId + "\n" + assignment.BedSlotIndex))
                {
                    throw new InvalidOperationException($"Housing assignment {pair.Key} is invalid.");
                }
            }

            foreach (string npcId in state.Housing.HomelessAdultNpcIds)
            {
                if (!StableId.IsValid(npcId) || !state.Npcs.Instances.TryGetValue(npcId, out NpcInstanceState npc) ||
                    !HousingSystem.IsEligible(npc) || state.Housing.AssignmentsByNpcId.ContainsKey(npcId))
                {
                    throw new InvalidOperationException($"Homeless NPC state {npcId} is invalid.");
                }
            }
        }

        private static void ValidateSurvivalState(GameState state)
        {
            SurvivalRuntimeState survival = state.Survival;
            if (survival.NextSettlementTick <= state.SimulationTick || survival.LastSettlementTick < 0 ||
                survival.LastSettlementTick > state.SimulationTick ||
                survival.FoodRemainderQuarterUnits < 0 || survival.FoodRemainderQuarterUnits >= NpcSurvivalSystem.QuarterUnitsPerResource ||
                survival.WaterRemainderQuarterUnits < 0 || survival.WaterRemainderQuarterUnits >= NpcSurvivalSystem.QuarterUnitsPerResource ||
                !ValidSettlementAmounts(survival.LastFoodRequired, survival.LastFoodConsumed, survival.LastFoodShortage) ||
                !ValidSettlementAmounts(survival.LastWaterRequired, survival.LastWaterConsumed, survival.LastWaterShortage) ||
                survival.ConsecutiveFoodShortageDays < 0 || survival.ConsecutiveWaterShortageDays < 0)
            {
                throw new InvalidOperationException("NPC survival state is invalid.");
            }
        }

        private static bool ValidSettlementAmounts(int required, int consumed, int shortage)
        {
            return required >= 0 && consumed >= 0 && consumed <= required && shortage == required - consumed;
        }

        private static void ValidateProductionState(GameState state)
        {
            Dictionary<string, int> expectedGlobalLocks = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, Dictionary<string, int>> expectedLocalLocks = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            Dictionary<string, int> expectedGlobalReservations = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, int> expectedLocalReservations = new Dictionary<string, int>(StringComparer.Ordinal);

            if (state.Resources.SharedCapacity < 0)
            {
                throw new InvalidOperationException("Shared resource capacity cannot be negative.");
            }

            foreach (ResourceStack stack in state.Resources.Items.Values)
            {
                if (stack == null || stack.LockedAmount < 0 || stack.LockedAmount > stack.Amount ||
                    stack.IncomingReservedAmount < 0 ||
                    (stack.IncomingReservedAmount > 0 && stack.Amount + stack.IncomingReservedAmount > stack.Capacity))
                {
                    throw new InvalidOperationException("Global resource stack has an invalid locked amount.");
                }
            }

            foreach (BuildingInstanceState building in state.Buildings.Instances.Values)
            {
                if (building == null)
                {
                    continue;
                }

                if (building.LocalInventory == null)
                {
                    building.LocalInventory = new Dictionary<string, LocalResourceStack>(StringComparer.Ordinal);
                }

                if (building.LocalInventoryCapacity < 0)
                {
                    throw new InvalidOperationException($"Building {building.BuildingId} has negative local inventory capacity.");
                }

                long total = 0;
                foreach (KeyValuePair<string, LocalResourceStack> pair in building.LocalInventory)
                {
                    LocalResourceStack stack = pair.Value;
                    if (!StableId.IsValid(pair.Key) || stack == null ||
                        !StringComparer.Ordinal.Equals(pair.Key, stack.ResourceId) ||
                        stack.Amount < 0 || stack.LockedAmount < 0 || stack.LockedAmount > stack.Amount)
                    {
                        throw new InvalidOperationException($"Building {building.BuildingId} has invalid local inventory.");
                    }

                    total += stack.Amount;
                }

                if (building.LocalInventoryReservedAmount < 0 ||
                    total + building.LocalInventoryReservedAmount > building.LocalInventoryCapacity)
                {
                    throw new InvalidOperationException($"Building {building.BuildingId} local inventory exceeds capacity.");
                }
            }

            foreach (KeyValuePair<string, ProductionSlotState> pair in state.Production.SlotsByBuildingId)
            {
                ProductionSlotState slot = pair.Value;
                if (slot == null || !StringComparer.Ordinal.Equals(pair.Key, slot.BuildingId) ||
                    !state.Buildings.Instances.ContainsKey(slot.BuildingId) || !ProductionSlotStatuses.IsKnown(slot.Status))
                {
                    throw new InvalidOperationException($"Production slot {pair.Key} is invalid.");
                }

                slot.LockedGlobalInputs ??= new Dictionary<string, int>(StringComparer.Ordinal);
                slot.LockedLocalInputs ??= new Dictionary<string, int>(StringComparer.Ordinal);
                slot.OutputBuffer ??= new Dictionary<string, int>(StringComparer.Ordinal);
                bool active = slot.HasActiveBatch;
                if (active != (slot.RequiredWorkTicks > 0) || slot.ProgressWorkTicks < 0 ||
                    slot.EfficiencyRemainderBasisPointTicks < 0 ||
                    slot.EfficiencyRemainderBasisPointTicks >= WasteEffectRules.BasisPointsPerWhole ||
                    (active && slot.ProgressWorkTicks > slot.RequiredWorkTicks) ||
                    (!active && (slot.ProgressWorkTicks != 0 || slot.LockedGlobalInputs.Count > 0 || slot.LockedLocalInputs.Count > 0)))
                {
                    throw new InvalidOperationException($"Production slot {pair.Key} has an inconsistent active batch.");
                }

                if (slot.HasBufferedOutput)
                {
                    slot.Status = ProductionSlotStatuses.OutputPending;
                }

                AccumulateLocks(slot.LockedGlobalInputs, expectedGlobalLocks, slot.BuildingId);
                if (!expectedLocalLocks.TryGetValue(slot.BuildingId, out Dictionary<string, int> localLocks))
                {
                    localLocks = new Dictionary<string, int>(StringComparer.Ordinal);
                    expectedLocalLocks[slot.BuildingId] = localLocks;
                }
                AccumulateLocks(slot.LockedLocalInputs, localLocks, slot.BuildingId);
            }

            ValidateLogisticsState(
                state,
                expectedGlobalLocks,
                expectedLocalLocks,
                expectedGlobalReservations,
                expectedLocalReservations);

            foreach (KeyValuePair<string, ResourceStack> pair in state.Resources.Items)
            {
                int expected = expectedGlobalLocks.TryGetValue(pair.Key, out int amount) ? amount : 0;
                if (pair.Value.LockedAmount != expected)
                {
                    throw new InvalidOperationException($"Global resource lock {pair.Key} does not match active production batches.");
                }


                int expectedReservation = expectedGlobalReservations.TryGetValue(pair.Key, out int reserved) ? reserved : 0;
                if (pair.Value.IncomingReservedAmount != expectedReservation)
                {
                    throw new InvalidOperationException($"Global resource reservation {pair.Key} does not match active transport tasks.");
                }
            }

            foreach (KeyValuePair<string, int> pair in expectedGlobalLocks)
            {
                if (!state.Resources.Items.ContainsKey(pair.Key))
                {
                    throw new InvalidOperationException($"Production lock references missing global resource {pair.Key}.");
                }
            }

            foreach (BuildingInstanceState building in state.Buildings.Instances.Values)
            {
                if (building == null) continue;
                expectedLocalLocks.TryGetValue(building.BuildingId, out Dictionary<string, int> localLocks);
                foreach (KeyValuePair<string, LocalResourceStack> pair in building.LocalInventory)
                {
                    int expected = localLocks != null && localLocks.TryGetValue(pair.Key, out int amount) ? amount : 0;
                    if (pair.Value.LockedAmount != expected)
                    {
                        throw new InvalidOperationException($"Local resource lock {building.BuildingId}/{pair.Key} does not match active production batches.");
                    }
                }

                if (localLocks != null)
                {
                    foreach (string resourceId in localLocks.Keys)
                    {
                        if (!building.LocalInventory.ContainsKey(resourceId))
                        {
                            throw new InvalidOperationException($"Production lock references missing local resource {building.BuildingId}/{resourceId}.");
                        }
                    }
                }


                int expectedReservation = expectedLocalReservations.TryGetValue(building.BuildingId, out int reserved) ? reserved : 0;
                if (building.LocalInventoryReservedAmount != expectedReservation)
                {
                    throw new InvalidOperationException($"Local capacity reservation {building.BuildingId} does not match active transport tasks.");
                }
            }
        }

        private static void ValidateWasteState(GameState state)
        {
            WasteRuntimeState waste = state.Waste;
            if (waste == null || waste.NextSettlementTick <= state.SimulationTick ||
                waste.LastSettlementTick < 0 || waste.LastSettlementTick > state.SimulationTick ||
                waste.NpcHalfUnitRemainder < 0 || waste.NpcHalfUnitRemainder > 1 ||
                waste.TotalGeneratedAmount < 0 || waste.TotalDiscardedAmount < 0 ||
                waste.TotalDiscardedAmount > waste.TotalGeneratedAmount ||
                waste.LastSettlementGeneratedAmount < 0 || waste.LastSettlementDiscardedAmount < 0 ||
                waste.LastSettlementDiscardedAmount > waste.LastSettlementGeneratedAmount ||
                waste.LastActiveBuildingCount < 0 || waste.LastLivingNpcCount < 0 ||
                waste.LastDiseaseExposureCount < 0 || waste.LastDiseaseTriggeredCount < 0 ||
                waste.LastDiseaseTriggeredCount > waste.LastDiseaseExposureCount ||
                waste.TotalDiseaseTriggeredCount < waste.LastDiseaseTriggeredCount ||
                waste.AccumulatedSatisfactionPenaltyBasisPoints < 0 ||
                waste.AccumulatedSatisfactionPenaltyBasisPoints > WasteGenerationSystem.MaximumSatisfactionPenaltyBasisPoints ||
                waste.AccumulatedSatisfactionPenaltyBasisPoints % 100 != 0 ||
                (waste.DiseaseChanceBonusBasisPoints != 0 &&
                 waste.DiseaseChanceBonusBasisPoints != WasteGenerationSystem.DiseaseChanceBonusBasisPoints))
            {
                throw new InvalidOperationException("Waste runtime state is invalid.");
            }
        }

        private static void ValidateContinuousProductionState(GameState state)
        {
            foreach (KeyValuePair<string, ContinuousProductionBuildingState> pair in
                state.ContinuousProduction.Buildings)
            {
                ContinuousProductionBuildingState runtime = pair.Value;
                if (runtime == null || !StringComparer.Ordinal.Equals(pair.Key, runtime.BuildingId) ||
                    !StableId.IsValid(runtime.BuildingId) ||
                    !state.Buildings.Instances.TryGetValue(runtime.BuildingId, out BuildingInstanceState building) ||
                    building == null || building.IsDestroyed ||
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
                     !StringComparer.Ordinal.Equals(runtime.Status, ContinuousProductionStatuses.OutputPending)))
                {
                    throw new InvalidOperationException($"Continuous production state {pair.Key} is invalid.");
                }
            }
        }

        private static void ValidateLogisticsState(
            GameState state,
            Dictionary<string, int> expectedGlobalLocks,
            Dictionary<string, Dictionary<string, int>> expectedLocalLocks,
            Dictionary<string, int> expectedGlobalReservations,
            Dictionary<string, int> expectedLocalReservations)
        {
            HashSet<string> occupiedConnectorEndpoints = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, LogisticsConnectorConstructionState> pair in state.Logistics.ConstructionTasks)
            {
                LogisticsConnectorConstructionState task = pair.Value;
                if (task == null || !StringComparer.Ordinal.Equals(pair.Key, task.TaskId) ||
                    !StableId.IsValid(task.TaskId) || !StableId.IsValid(task.ConnectorId) ||
                    !StableId.IsValid(task.DefinitionId) || !StableId.IsValid(task.ResourceId) ||
                    !ValidateConnectorEndpoints(state, task.PlotId, task.LowerBuildingId, task.UpperBuildingId) ||
                    state.Buildings.Instances[task.LowerBuildingId].IsDestroyed ||
                    state.Buildings.Instances[task.UpperBuildingId].IsDestroyed ||
                    task.AutoTransferAmount <= 0 || task.CreatedTick < 0 || task.RequiredTicks <= 0 ||
                    task.ProgressTicks < 0 || task.ProgressTicks > task.RequiredTicks || task.PaidBuildCost == null)
                {
                    throw new InvalidOperationException($"Connector construction task {pair.Key} is invalid.");
                }
                ValidatePaidBuildCost(task.TaskId, task.PaidBuildCost);
                if (!occupiedConnectorEndpoints.Add(task.LowerBuildingId + "\n" + task.UpperBuildingId))
                {
                    throw new InvalidOperationException($"Connector endpoints for {pair.Key} are duplicated.");
                }
            }

            foreach (KeyValuePair<string, LogisticsConnectorInstanceState> pair in state.Logistics.Connectors)
            {
                LogisticsConnectorInstanceState connector = pair.Value;
                if (connector == null || !StringComparer.Ordinal.Equals(pair.Key, connector.ConnectorId) ||
                    !StableId.IsValid(connector.ConnectorId) || !StableId.IsValid(connector.DefinitionId) ||
                    !StableId.IsValid(connector.ResourceId) || !StableId.IsValid(connector.RouteId) ||
                    !ValidateConnectorEndpoints(state, connector.PlotId, connector.LowerBuildingId, connector.UpperBuildingId) ||
                    (!connector.IsDestroyed &&
                     (state.Buildings.Instances[connector.LowerBuildingId].IsDestroyed ||
                      state.Buildings.Instances[connector.UpperBuildingId].IsDestroyed)) ||
                    connector.AutoTransferAmount <= 0 || connector.Durability < 0 || connector.CompletedTick < 0 ||
                    connector.PaidBuildCost == null || (!connector.IsDestroyed && connector.Durability <= 0))
                {
                    throw new InvalidOperationException($"Logistics connector {pair.Key} is invalid.");
                }
                ValidatePaidBuildCost(connector.ConnectorId, connector.PaidBuildCost);
                if (!connector.IsDestroyed && !occupiedConnectorEndpoints.Add(
                    connector.LowerBuildingId + "\n" + connector.UpperBuildingId))
                {
                    throw new InvalidOperationException($"Connector endpoints for {pair.Key} are duplicated.");
                }
                if (connector.IsDestroyed && state.Logistics.Routes.ContainsKey(connector.RouteId))
                {
                    throw new InvalidOperationException($"Destroyed connector {pair.Key} still has a route.");
                }
            }

            foreach (KeyValuePair<string, LogisticsRouteState> pair in state.Logistics.Routes)
            {
                LogisticsRouteState route = pair.Value;
                if (route == null || !StableId.IsValid(pair.Key) ||
                    !StringComparer.Ordinal.Equals(pair.Key, route.RouteId) ||
                    !state.Buildings.Instances.ContainsKey(route.FirstBuildingId) ||
                    !state.Buildings.Instances.ContainsKey(route.SecondBuildingId) ||
                    StringComparer.Ordinal.Equals(route.FirstBuildingId, route.SecondBuildingId))
                {
                    throw new InvalidOperationException($"Logistics route {pair.Key} is invalid.");
                }

                if (!string.IsNullOrEmpty(route.ResourceId) && !StableId.IsValid(route.ResourceId))
                {
                    throw new InvalidOperationException($"Logistics route {pair.Key} has an invalid resource.");
                }
                if (!string.IsNullOrEmpty(route.ConnectorId))
                {
                    if (!state.Logistics.Connectors.TryGetValue(route.ConnectorId, out LogisticsConnectorInstanceState connector) ||
                        connector.IsDestroyed || !StringComparer.Ordinal.Equals(connector.RouteId, route.RouteId) ||
                        !StringComparer.Ordinal.Equals(connector.LowerBuildingId, route.FirstBuildingId) ||
                        !StringComparer.Ordinal.Equals(connector.UpperBuildingId, route.SecondBuildingId) ||
                        !StringComparer.Ordinal.Equals(connector.ResourceId, route.ResourceId) || route.IsBidirectional)
                    {
                        throw new InvalidOperationException($"Logistics route {pair.Key} does not match its connector.");
                    }
                }
            }

            foreach (LogisticsConnectorInstanceState connector in state.Logistics.Connectors.Values)
            {
                if (connector != null && !connector.IsDestroyed && !state.Logistics.Routes.ContainsKey(connector.RouteId))
                {
                    throw new InvalidOperationException($"Logistics connector {connector.ConnectorId} has no route.");
                }
            }

            foreach (KeyValuePair<string, TransportTaskState> pair in state.Logistics.ActiveTasks)
            {
                TransportTaskState task = pair.Value;
                if (task == null || !StableId.IsValid(pair.Key) ||
                    !StringComparer.Ordinal.Equals(pair.Key, task.TaskId) ||
                    !LogisticsEndpointKinds.IsKnown(task.SourceKind) ||
                    !LogisticsEndpointKinds.IsKnown(task.TargetKind) ||
                    !StableId.IsValid(task.ResourceId) || task.Amount <= 0 ||
                    task.CreatedTick < 0 || task.CompletionTick < task.CreatedTick ||
                    !ValidateLogisticsEndpoint(state, task.SourceKind, task.SourceBuildingId) ||
                    !ValidateLogisticsEndpoint(state, task.TargetKind, task.TargetBuildingId) ||
                    (StringComparer.Ordinal.Equals(task.SourceKind, task.TargetKind) &&
                     StringComparer.Ordinal.Equals(task.SourceBuildingId, task.TargetBuildingId)))
                {
                    throw new InvalidOperationException($"Transport task {pair.Key} is invalid.");
                }

                if (!string.IsNullOrEmpty(task.RouteId) && !state.Logistics.Routes.ContainsKey(task.RouteId))
                {
                    throw new InvalidOperationException($"Transport task {pair.Key} references a missing route.");
                }

                if (StringComparer.Ordinal.Equals(task.SourceKind, LogisticsEndpointKinds.Building) &&
                    StringComparer.Ordinal.Equals(task.TargetKind, LogisticsEndpointKinds.Building))
                {
                    BuildingInstanceState source = state.Buildings.Instances[task.SourceBuildingId];
                    BuildingInstanceState target = state.Buildings.Instances[task.TargetBuildingId];
                    if (source.BaseLayer != target.BaseLayer &&
                        (string.IsNullOrEmpty(task.RouteId) ||
                         !RouteConnects(state.Logistics.Routes[task.RouteId], source.BuildingId, target.BuildingId, task.ResourceId)))
                    {
                        throw new InvalidOperationException($"Cross-layer transport task {pair.Key} has no matching route.");
                    }
                }

                if (StringComparer.Ordinal.Equals(task.SourceKind, LogisticsEndpointKinds.Global))
                {
                    expectedGlobalLocks[task.ResourceId] = checked(
                        (expectedGlobalLocks.TryGetValue(task.ResourceId, out int amount) ? amount : 0) + task.Amount);
                }
                else
                {
                    if (!expectedLocalLocks.TryGetValue(task.SourceBuildingId, out Dictionary<string, int> localLocks))
                    {
                        localLocks = new Dictionary<string, int>(StringComparer.Ordinal);
                        expectedLocalLocks[task.SourceBuildingId] = localLocks;
                    }
                    localLocks[task.ResourceId] = checked(
                        (localLocks.TryGetValue(task.ResourceId, out int amount) ? amount : 0) + task.Amount);
                }

                if (StringComparer.Ordinal.Equals(task.TargetKind, LogisticsEndpointKinds.Global))
                {
                    expectedGlobalReservations[task.ResourceId] = checked(
                        (expectedGlobalReservations.TryGetValue(task.ResourceId, out int amount) ? amount : 0) + task.Amount);
                }
                else
                {
                    expectedLocalReservations[task.TargetBuildingId] = checked(
                        (expectedLocalReservations.TryGetValue(task.TargetBuildingId, out int amount) ? amount : 0) + task.Amount);
                }
            }


            foreach (string resourceId in expectedGlobalReservations.Keys)
            {
                if (!state.Resources.Items.ContainsKey(resourceId))
                {
                    throw new InvalidOperationException($"Transport reservation references missing global resource {resourceId}.");
                }
            }
        }

        private static bool ValidateConnectorEndpoints(
            GameState state,
            string plotId,
            string lowerBuildingId,
            string upperBuildingId)
        {
            if (!StableId.IsValid(plotId) ||
                !state.Buildings.Instances.TryGetValue(lowerBuildingId, out BuildingInstanceState lower) ||
                !state.Buildings.Instances.TryGetValue(upperBuildingId, out BuildingInstanceState upper) ||
                !StringComparer.Ordinal.Equals(plotId, lower.PlotId) ||
                !StringComparer.Ordinal.Equals(plotId, upper.PlotId) ||
                upper.BaseLayer != lower.BaseLayer + lower.PlacedHeight)
            {
                return false;
            }

            return lower.AnchorX < (long)upper.AnchorX + upper.PlacedWidth &&
                   upper.AnchorX < (long)lower.AnchorX + lower.PlacedWidth &&
                   lower.AnchorY < (long)upper.AnchorY + upper.PlacedDepth &&
                   upper.AnchorY < (long)lower.AnchorY + lower.PlacedDepth;
        }

        private static bool RouteConnects(
            LogisticsRouteState route,
            string sourceBuildingId,
            string targetBuildingId,
            string resourceId)
        {
            if (route == null ||
                (!string.IsNullOrEmpty(route.ResourceId) && !StringComparer.Ordinal.Equals(route.ResourceId, resourceId)))
            {
                return false;
            }
            bool forward = StringComparer.Ordinal.Equals(route.FirstBuildingId, sourceBuildingId) &&
                           StringComparer.Ordinal.Equals(route.SecondBuildingId, targetBuildingId);
            bool reverse = route.IsBidirectional &&
                           StringComparer.Ordinal.Equals(route.FirstBuildingId, targetBuildingId) &&
                           StringComparer.Ordinal.Equals(route.SecondBuildingId, sourceBuildingId);
            return forward || reverse;
        }

        private static bool ValidateLogisticsEndpoint(GameState state, string kind, string buildingId)
        {
            return StringComparer.Ordinal.Equals(kind, LogisticsEndpointKinds.Global)
                ? string.IsNullOrEmpty(buildingId)
                : StableId.IsValid(buildingId) && state.Buildings.Instances.ContainsKey(buildingId);
        }

        private static void AccumulateLocks(
            Dictionary<string, int> source,
            Dictionary<string, int> target,
            string ownerId)
        {
            foreach (KeyValuePair<string, int> pair in source)
            {
                if (!StableId.IsValid(pair.Key) || pair.Value <= 0)
                {
                    throw new InvalidOperationException($"Production slot {ownerId} has an invalid resource lock.");
                }

                target[pair.Key] = checked((target.TryGetValue(pair.Key, out int amount) ? amount : 0) + pair.Value);
            }
        }

        private static void ValidateNpcState(GameState state)
        {
            foreach (KeyValuePair<string, NpcInstanceState> pair in state.Npcs.Instances)
            {
                if (pair.Value == null || !StableId.IsValid(pair.Key) ||
                    !StringComparer.Ordinal.Equals(pair.Key, pair.Value.NpcId) ||
                    !StableId.IsValid(pair.Value.OwnerPlayerId) || pair.Value.CreationSequence < 0 ||
                    pair.Value.BaseSatisfactionBasisPoints < 0 ||
                    pair.Value.BaseSatisfactionBasisPoints > WasteEffectRules.BasisPointsPerWhole ||
                    !IsValidNpcLifecycle(pair.Value, state.SimulationTick))
                {
                    throw new InvalidOperationException($"NPC state {pair.Key} is invalid.");
                }
            }

            HashSet<string> occupiedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, WorkAssignmentState> pair in state.Npcs.WorkAssignments)
            {
                WorkAssignmentState assignment = pair.Value;
                if (assignment == null || !StringComparer.Ordinal.Equals(pair.Key, assignment.NpcId) ||
                    !state.Npcs.Instances.ContainsKey(assignment.NpcId) ||
                    !state.Buildings.Instances.ContainsKey(assignment.BuildingId) ||
                    assignment.SlotIndex < 0 || assignment.AssignedTick < 0)
                {
                    throw new InvalidOperationException($"Work assignment {pair.Key} is invalid.");
                }

                string slotKey = assignment.BuildingId + "\n" + assignment.SlotIndex;
                if (!occupiedSlots.Add(slotKey))
                {
                    throw new InvalidOperationException($"Work assignment slot {slotKey} is duplicated.");
                }
            }
        }

        private static void RepairNpcLifecycleState(GameState state, bool migrating)
        {
            foreach (NpcInstanceState npc in state.Npcs.Instances.Values)
            {
                if (npc == null) continue;
                if (migrating)
                {
                    npc.LifeStageElapsedTicks = 0;
                    npc.AdultTransitionTick = 0;
                    npc.DeathTick = 0;
                }
                if (migrating) NpcLifecycleSystem.EnsureLifespan(state, npc);
            }
        }

        private static bool IsValidNpcLifecycle(NpcInstanceState npc, long simulationTick)
        {
            long minimum = NpcLifecycleSystem.MinimumAdultLifespanDays * GameTime.TicksPerGameDay;
            long maximum = NpcLifecycleSystem.MaximumAdultLifespanDays * GameTime.TicksPerGameDay;
            bool lifespanValid = npc.AdultLifespanTicks == 0
                ? npc.LifeStageElapsedTicks == 0 && npc.AdultTransitionTick == 0 && npc.DeathTick == 0
                : npc.AdultLifespanTicks >= minimum && npc.AdultLifespanTicks <= maximum;
            return lifespanValid && npc.LifeStageElapsedTicks >= 0 &&
                   (npc.IsAdult ? npc.LifeStageElapsedTicks <= npc.AdultLifespanTicks : npc.LifeStageElapsedTicks < NpcLifecycleSystem.InfantGrowthTicks) &&
                   npc.AdultTransitionTick >= 0 && npc.AdultTransitionTick <= simulationTick &&
                   npc.DeathTick >= 0 && npc.DeathTick <= simulationTick &&
                   (npc.AdultTransitionTick == 0 || npc.IsAdult) &&
                   (npc.DeathTick == 0 || !npc.IsAlive);
        }

        private static void RepairStructuralIncidentState(GameState state)
        {
            if (state.Buildings.NextStructuralCollapseTick < 0)
            {
                throw new InvalidOperationException("Next structural collapse tick cannot be negative.");
            }

            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(instance.StructuralStatus))
                {
                    instance.StructuralStatus = BuildingStructuralStatuses.Normal;
                }

                if (instance.PaidBuildCost == null)
                {
                    instance.PaidBuildCost = new Dictionary<string, int>(StringComparer.Ordinal);
                }

                ValidatePaidBuildCost(instance.BuildingId, instance.PaidBuildCost);

                if (!BuildingStructuralStatuses.IsKnown(instance.StructuralStatus))
                {
                    throw new InvalidOperationException($"Building {instance.BuildingId} has unknown structural status {instance.StructuralStatus}.");
                }

                if (instance.StructuralGraceDeadlineTick < 0)
                {
                    throw new InvalidOperationException($"Building {instance.BuildingId} has a negative structural grace deadline.");
                }

                if (StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Normal))
                {
                    instance.StructuralGraceDeadlineTick = 0;
                }
            }


            foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
            {
                if (task == null)
                {
                    continue;
                }

                if (task.PaidBuildCost == null)
                {
                    task.PaidBuildCost = new Dictionary<string, int>(StringComparer.Ordinal);
                }

                ValidatePaidBuildCost(task.TaskId, task.PaidBuildCost);
            }
        }

        private static void ValidatePaidBuildCost(string objectId, Dictionary<string, int> paidBuildCost)
        {
            foreach (KeyValuePair<string, int> pair in paidBuildCost)
            {
                if (!StableId.IsValid(pair.Key) || pair.Value < 0)
                {
                    throw new InvalidOperationException($"Object {objectId} has an invalid paid build cost entry.");
                }
            }
        }

        private static void RepairSpatialPlacements(GameState state)
        {
            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance != null)
                {
                    int anchorX = instance.AnchorX;
                    int anchorY = instance.AnchorY;
                    int baseLayer = instance.BaseLayer;
                    int rotationQuarterTurns = instance.RotationQuarterTurns;
                    int placedWidth = instance.PlacedWidth;
                    int placedDepth = instance.PlacedDepth;
                    int placedHeight = instance.PlacedHeight;
                    int placementSchemaVersion = instance.PlacementSchemaVersion;
                    RepairPlacement(
                        instance.BuildingId,
                        instance.PlotId,
                        instance.Layer,
                        ref anchorX,
                        ref anchorY,
                        ref baseLayer,
                        ref rotationQuarterTurns,
                        ref placedWidth,
                        ref placedDepth,
                        ref placedHeight,
                        ref placementSchemaVersion,
                        state.World.Plots);
                    instance.AnchorX = anchorX;
                    instance.AnchorY = anchorY;
                    instance.BaseLayer = baseLayer;
                    instance.RotationQuarterTurns = rotationQuarterTurns;
                    instance.PlacedWidth = placedWidth;
                    instance.PlacedDepth = placedDepth;
                    instance.PlacedHeight = placedHeight;
                    instance.PlacementSchemaVersion = placementSchemaVersion;
                }
            }

            foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
            {
                if (task != null)
                {
                    int anchorX = task.AnchorX;
                    int anchorY = task.AnchorY;
                    int baseLayer = task.BaseLayer;
                    int rotationQuarterTurns = task.RotationQuarterTurns;
                    int placedWidth = task.PlacedWidth;
                    int placedDepth = task.PlacedDepth;
                    int placedHeight = task.PlacedHeight;
                    int placementSchemaVersion = task.PlacementSchemaVersion;
                    RepairPlacement(
                        task.TaskId,
                        task.PlotId,
                        task.Layer,
                        ref anchorX,
                        ref anchorY,
                        ref baseLayer,
                        ref rotationQuarterTurns,
                        ref placedWidth,
                        ref placedDepth,
                        ref placedHeight,
                        ref placementSchemaVersion,
                        state.World.Plots);
                    task.AnchorX = anchorX;
                    task.AnchorY = anchorY;
                    task.BaseLayer = baseLayer;
                    task.RotationQuarterTurns = rotationQuarterTurns;
                    task.PlacedWidth = placedWidth;
                    task.PlacedDepth = placedDepth;
                    task.PlacedHeight = placedHeight;
                    task.PlacementSchemaVersion = placementSchemaVersion;
                }
            }
        }

        private static void RepairPlacement(
            string objectId,
            string plotId,
            int legacyLayer,
            ref int anchorX,
            ref int anchorY,
            ref int baseLayer,
            ref int rotationQuarterTurns,
            ref int placedWidth,
            ref int placedDepth,
            ref int placedHeight,
            ref int placementSchemaVersion,
            IReadOnlyDictionary<string, PlotState> plots)
        {
            if (placementSchemaVersion < 0)
            {
                throw new InvalidOperationException($"Placement {objectId} has invalid schema version {placementSchemaVersion}.");
            }

            if (!plots.TryGetValue(plotId, out PlotState plot) || plot == null)
            {
                throw new InvalidOperationException($"Placement {objectId} references missing plot {plotId}.");
            }

            if (plot.Width <= 0 || plot.Depth <= 0 || plot.MaxStackLayers <= 0)
            {
                throw new InvalidOperationException($"Placement {objectId} references plot {plotId} with invalid spatial bounds.");
            }

            if (legacyLayer < 0 || legacyLayer >= plot.MaxStackLayers)
            {
                throw new InvalidOperationException($"Placement {objectId} is outside the plot stack range.");
            }

            if (placementSchemaVersion == 0)
            {
                anchorX = plot.X;
                anchorY = plot.Y;
                baseLayer = legacyLayer;
                rotationQuarterTurns = 0;
                placedWidth = 1;
                placedDepth = 1;
                placedHeight = 1;
                placementSchemaVersion = SpatialPlacementSchema.CurrentVersion;
                return;
            }

            if (placementSchemaVersion > SpatialPlacementSchema.CurrentVersion)
            {
                throw new InvalidOperationException($"Placement {objectId} uses unsupported schema version {placementSchemaVersion}.");
            }

            if (baseLayer != legacyLayer)
            {
                throw new InvalidOperationException($"Placement {objectId} has conflicting legacy and base layers.");
            }

            SpatialPlacement placement = new SpatialPlacement(
                objectId,
                anchorX,
                anchorY,
                baseLayer,
                placedWidth,
                placedDepth,
                placedHeight,
                rotationQuarterTurns);
            SpatialPlacementResult result = SpatialOccupancy.ValidatePlacement(
                placement,
                new SpatialBounds(plot.X, plot.Y, 0, plot.Width, plot.Depth, plot.MaxStackLayers),
                Array.Empty<SpatialPlacement>());
            if (!result.Accepted)
            {
                throw new InvalidOperationException($"Placement {objectId} is invalid: {result.Reason}");
            }
        }

        private static void ValidateSpatialOccupancy(GameState state)
        {
            foreach (KeyValuePair<string, PlotState> plotPair in state.World.Plots)
            {
                PlotState plot = plotPair.Value;
                if (plot == null || plot.Width <= 0 || plot.Depth <= 0 || plot.MaxStackLayers <= 0)
                {
                    continue;
                }

                List<SpatialPlacement> candidates = new List<SpatialPlacement>();
                foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
                {
                    if (instance != null && !instance.IsDestroyed && StringComparer.Ordinal.Equals(instance.PlotId, plotPair.Key))
                    {
                        candidates.Add(CreateSpatialPlacement(instance));
                    }
                }

                foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
                {
                    if (task != null && StringComparer.Ordinal.Equals(task.PlotId, plotPair.Key))
                    {
                        candidates.Add(CreateSpatialPlacement(task));
                    }
                }

                candidates.Sort((left, right) => StringComparer.Ordinal.Compare(left.ObjectId, right.ObjectId));
                List<SpatialPlacement> accepted = new List<SpatialPlacement>();
                SpatialBounds bounds = new SpatialBounds(plot.X, plot.Y, 0, plot.Width, plot.Depth, plot.MaxStackLayers);
                for (int i = 0; i < candidates.Count; i++)
                {
                    SpatialPlacementResult result = SpatialOccupancy.ValidatePlacement(candidates[i], bounds, accepted);
                    if (!result.Accepted)
                    {
                        throw new InvalidOperationException($"Spatial state is invalid: {result.Reason}");
                    }

                    accepted.Add(candidates[i]);
                }
            }
        }

        private static SpatialPlacement CreateSpatialPlacement(BuildingInstanceState instance)
        {
            return new SpatialPlacement(
                instance.BuildingId,
                instance.AnchorX,
                instance.AnchorY,
                instance.BaseLayer,
                instance.PlacedWidth,
                instance.PlacedDepth,
                instance.PlacedHeight,
                instance.RotationQuarterTurns);
        }

        private static SpatialPlacement CreateSpatialPlacement(ConstructionTaskState task)
        {
            return new SpatialPlacement(
                task.TaskId,
                task.AnchorX,
                task.AnchorY,
                task.BaseLayer,
                task.PlacedWidth,
                task.PlacedDepth,
                task.PlacedHeight,
                task.RotationQuarterTurns);
        }

        private static void RepairSequences(GameState state)
        {
            state.NextInstanceSequence = Math.Max(1, state.NextInstanceSequence);
            state.NextEventSequence = Math.Max(1, state.NextEventSequence);
            state.NextConstructionSequence = Math.Max(1, state.NextConstructionSequence);
            state.NextProductionBatchSequence = Math.Max(1, state.NextProductionBatchSequence);
            state.NextTransportTaskSequence = Math.Max(1, state.NextTransportTaskSequence);
            state.NextConnectorSequence = Math.Max(1, state.NextConnectorSequence);
            state.NextConnectorConstructionSequence = Math.Max(1, state.NextConnectorConstructionSequence);

            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                state.NextInstanceSequence = Math.Max(state.NextInstanceSequence, ExtractSequence(instance.BuildingId) + 1);
                state.NextConstructionSequence = Math.Max(state.NextConstructionSequence, instance.ConstructionSequence + 1);
            }

            foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
            {
                state.NextInstanceSequence = Math.Max(state.NextInstanceSequence, ExtractSequence(task.BuildingId) + 1);
                state.NextConstructionSequence = Math.Max(state.NextConstructionSequence, ExtractSequence(task.TaskId) + 1);
                state.NextConstructionSequence = Math.Max(state.NextConstructionSequence, task.ConstructionSequence + 1);
            }

            foreach (ProductionSlotState slot in state.Production.SlotsByBuildingId.Values)
            {
                if (slot != null && slot.HasActiveBatch)
                {
                    state.NextProductionBatchSequence = Math.Max(
                        state.NextProductionBatchSequence,
                        ExtractSequence(slot.ActiveBatchId) + 1);
                }
            }

            foreach (TransportTaskState task in state.Logistics.ActiveTasks.Values)
            {
                if (task != null)
                {
                    state.NextTransportTaskSequence = Math.Max(
                        state.NextTransportTaskSequence,
                        ExtractSequence(task.TaskId) + 1);
                }
            }

            foreach (LogisticsConnectorConstructionState task in state.Logistics.ConstructionTasks.Values)
            {
                if (task == null) continue;
                state.NextConnectorSequence = Math.Max(state.NextConnectorSequence, ExtractSequence(task.ConnectorId) + 1);
                state.NextConnectorConstructionSequence = Math.Max(
                    state.NextConnectorConstructionSequence, ExtractSequence(task.TaskId) + 1);
            }

            foreach (LogisticsConnectorInstanceState connector in state.Logistics.Connectors.Values)
            {
                if (connector != null)
                {
                    state.NextConnectorSequence = Math.Max(
                        state.NextConnectorSequence, ExtractSequence(connector.ConnectorId) + 1);
                }
            }

            for (int i = 0; i < state.Events.Events.Count; i++)
            {
                state.NextEventSequence = Math.Max(state.NextEventSequence, ExtractSequence(state.Events.Events[i].EventId) + 1);
            }
        }

        private static long ExtractSequence(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                return 0;
            }

            int lastSeparator = stableId.LastIndexOf(':');
            string tail = lastSeparator >= 0 ? stableId.Substring(lastSeparator + 1) : stableId;
            return long.TryParse(tail, out long sequence) ? sequence : 0;
        }

        private static void RepairFirstBuildBonus(FirstBuildBonusState bonus, GameState state)
        {
            if (bonus.Consumed)
            {
                bonus.ReservedTaskId = string.Empty;
                bonus.ReservedDefinitionId = string.Empty;
                return;
            }

            if (!string.IsNullOrEmpty(bonus.ReservedTaskId) &&
                !state.Buildings.ConstructionTasks.ContainsKey(bonus.ReservedTaskId))
            {
                bonus.ReservedTaskId = string.Empty;
                bonus.ReservedDefinitionId = string.Empty;
            }
        }
    }
}
