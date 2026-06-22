using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class WasteGenerationSystem : ISimulationSystem
    {
        public const string WasteSettledEvent = "event:core:waste_settled";
        public const string WasteGeneratedByDeathEvent = "event:core:waste_generated_by_death";
        public const string WasteOverflowDiscardedEvent = "event:core:waste_overflow_discarded";
        public const string WasteThresholdChangedEvent = "event:core:waste_threshold_changed";
        public const string WasteDiseaseExposureSettledEvent = "event:core:waste_disease_exposure_settled";
        public const int WasteCapacity = 500;
        public const int WarningThreshold = 200;
        public const int CriticalThreshold = 400;
        public const int MaximumSatisfactionPenaltyBasisPoints = 2000;
        public const int DiseaseChanceBonusBasisPoints = 500;

        public void RegisterCommands(CommandBus commandBus)
        {
            if (commandBus == null) throw new ArgumentNullException(nameof(commandBus));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            List<GameEvent> events = new List<GameEvent>();
            WasteRuntimeState waste = context.State.Waste;
            if (waste.NextSettlementTick <= 0)
                waste.NextSettlementTick = NpcSurvivalSystem.NextDayBoundary(context.State.SimulationTick - deltaTicks);

            ClearSatisfactionPenaltyWhenEmpty(context, events);
            while (waste.NextSettlementTick <= context.State.SimulationTick)
            {
                SettleDay(context, waste.NextSettlementTick, events);
                waste.NextSettlementTick = checked(waste.NextSettlementTick + GameTime.TicksPerGameDay);
            }
            return events;
        }

        public static void AddDeathWaste(
            SimulationContext context,
            string npcId,
            long transitionTick,
            List<GameEvent> events)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (events == null) throw new ArgumentNullException(nameof(events));

            WasteRuntimeState runtime = context.State.Waste;
            int stored = StoreWaste(context.State, 1);
            int discarded = 1 - stored;
            runtime.TotalGeneratedAmount = checked(runtime.TotalGeneratedAmount + 1);
            runtime.TotalDiscardedAmount = checked(runtime.TotalDiscardedAmount + discarded);
            events.Add(context.Events.Create(WasteGeneratedByDeathEvent, "system:core:waste", new WasteDeathPayload
            {
                NpcId = npcId,
                TransitionTick = transitionTick,
                StoredAmount = stored,
                DiscardedAmount = discarded
            }));
            if (discarded > 0)
                AddOverflowEvent(context, "npc_death", discarded, events);
        }

        private static void SettleDay(SimulationContext context, long settlementTick, List<GameEvent> events)
        {
            WasteRuntimeState runtime = context.State.Waste;
            int activeBuildings = CountActiveBuildings(context.State);
            int livingNpcs = CountLivingNpcs(context.State, settlementTick);
            int halfUnits = checked(activeBuildings * 2 + livingNpcs + runtime.NpcHalfUnitRemainder);
            int generated = halfUnits / 2;
            runtime.NpcHalfUnitRemainder = halfUnits % 2;
            int stored = StoreWaste(context.State, generated);
            int discarded = generated - stored;

            runtime.LastSettlementTick = settlementTick;
            runtime.LastSettlementGeneratedAmount = generated;
            runtime.LastSettlementDiscardedAmount = discarded;
            runtime.LastActiveBuildingCount = activeBuildings;
            runtime.LastLivingNpcCount = livingNpcs;
            runtime.TotalGeneratedAmount = checked(runtime.TotalGeneratedAmount + generated);
            runtime.TotalDiscardedAmount = checked(runtime.TotalDiscardedAmount + discarded);

            int oldSatisfaction = runtime.AccumulatedSatisfactionPenaltyBasisPoints;
            int oldDisease = runtime.DiseaseChanceBonusBasisPoints;
            int wasteAmount = EnsureWasteStack(context.State).Amount;
            if (wasteAmount > CriticalThreshold)
            {
                runtime.AccumulatedSatisfactionPenaltyBasisPoints = Math.Min(
                    MaximumSatisfactionPenaltyBasisPoints,
                    runtime.AccumulatedSatisfactionPenaltyBasisPoints + 200);
                runtime.DiseaseChanceBonusBasisPoints = DiseaseChanceBonusBasisPoints;
            }
            else
            {
                if (wasteAmount > WarningThreshold)
                    runtime.AccumulatedSatisfactionPenaltyBasisPoints = Math.Min(
                        MaximumSatisfactionPenaltyBasisPoints,
                        runtime.AccumulatedSatisfactionPenaltyBasisPoints + 100);
                else if (wasteAmount == 0)
                    runtime.AccumulatedSatisfactionPenaltyBasisPoints = 0;
                runtime.DiseaseChanceBonusBasisPoints = 0;
            }
            SettleDiseaseExposure(context, settlementTick, events);

            events.Add(context.Events.Create(WasteSettledEvent, "system:core:waste", new WasteSettlementPayload
            {
                SettlementTick = settlementTick,
                ActiveBuildingCount = activeBuildings,
                LivingNpcCount = livingNpcs,
                GeneratedAmount = generated,
                StoredAmount = stored,
                DiscardedAmount = discarded,
                WasteAmount = wasteAmount,
                NpcHalfUnitRemainder = runtime.NpcHalfUnitRemainder
            }));
            if (discarded > 0)
                AddOverflowEvent(context, "daily_settlement", discarded, events);
            AddThresholdEventIfChanged(context, oldSatisfaction, oldDisease, events);
        }

        private static void ClearSatisfactionPenaltyWhenEmpty(SimulationContext context, List<GameEvent> events)
        {
            WasteRuntimeState runtime = context.State.Waste;
            if (runtime.AccumulatedSatisfactionPenaltyBasisPoints == 0 || EnsureWasteStack(context.State).Amount != 0)
                return;
            int oldSatisfaction = runtime.AccumulatedSatisfactionPenaltyBasisPoints;
            runtime.AccumulatedSatisfactionPenaltyBasisPoints = 0;
            AddThresholdEventIfChanged(context, oldSatisfaction, runtime.DiseaseChanceBonusBasisPoints, events);
        }

        private static int CountActiveBuildings(GameState state)
        {
            HashSet<string> active = new HashSet<string>(StringComparer.Ordinal);
            foreach (WorkAssignmentState assignment in state.Npcs.WorkAssignments.Values)
            {
                if (assignment == null || !state.Npcs.Instances.TryGetValue(assignment.NpcId, out NpcInstanceState npc) ||
                    !NpcOperationalRules.CanHoldWorkAssignment(npc) ||
                    !state.Buildings.Instances.TryGetValue(assignment.BuildingId, out BuildingInstanceState building) ||
                    !BuildingOperationalRules.IsOperational(building))
                    continue;
                active.Add(assignment.BuildingId);
            }
            return active.Count;
        }

        private static int CountLivingNpcs(GameState state, long settlementTick)
        {
            int count = 0;
            foreach (NpcInstanceState npc in state.Npcs.Instances.Values)
            {
                if (IsLivingAtTick(npc, settlementTick)) count++;
            }
            return count;
        }

        private static bool IsLivingAtTick(NpcInstanceState npc, long settlementTick)
        {
            return npc != null && !npc.IsPermanentlyDeparted &&
                   (npc.IsAlive || (npc.DeathTick > 0 && settlementTick < npc.DeathTick));
        }

        private static void SettleDiseaseExposure(
            SimulationContext context,
            long settlementTick,
            List<GameEvent> events)
        {
            WasteRuntimeState runtime = context.State.Waste;
            int chance = runtime.DiseaseChanceBonusBasisPoints;
            string[] exposedNpcIds = context.State.Npcs.Instances.Values
                .Where(npc => IsLivingAtTick(npc, settlementTick))
                .Select(npc => npc.NpcId)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
            runtime.LastDiseaseExposureCount = chance > 0 ? exposedNpcIds.Length : 0;
            runtime.LastDiseaseTriggeredCount = 0;
            if (chance <= 0) return;

            List<string> triggeredNpcIds = new List<string>();
            for (int index = 0; index < exposedNpcIds.Length; index++)
            {
                string key = $"{context.State.RngSeed}|{settlementTick}|{exposedNpcIds[index]}|waste_disease";
                byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(key));
                uint roll = ((uint)digest[0] << 24) | ((uint)digest[1] << 16) |
                            ((uint)digest[2] << 8) | digest[3];
                if (roll % WasteEffectRules.BasisPointsPerWhole < chance)
                    triggeredNpcIds.Add(exposedNpcIds[index]);
            }
            runtime.LastDiseaseTriggeredCount = triggeredNpcIds.Count;
            runtime.TotalDiseaseTriggeredCount = checked(runtime.TotalDiseaseTriggeredCount + triggeredNpcIds.Count);
            events.Add(context.Events.Create(WasteDiseaseExposureSettledEvent, "system:core:waste", new WasteDiseaseExposurePayload
            {
                SettlementTick = settlementTick,
                ChanceBasisPoints = chance,
                ExposureCount = exposedNpcIds.Length,
                TriggeredNpcIds = triggeredNpcIds
            }));
        }

        private static int StoreWaste(GameState state, int amount)
        {
            if (amount <= 0) return 0;
            ResourceStack stack = EnsureWasteStack(state);
            int stored = Math.Min(amount, StorageCapacityRules.ResourceFreeCapacity(state.Resources, stack));
            stack.Amount = checked(stack.Amount + stored);
            return stored;
        }

        private static ResourceStack EnsureWasteStack(GameState state)
        {
            if (!state.Resources.Items.TryGetValue(CoreResourceIds.Waste, out ResourceStack stack) || stack == null)
            {
                stack = new ResourceStack { ResourceId = CoreResourceIds.Waste, Capacity = WasteCapacity };
                state.Resources.Items[CoreResourceIds.Waste] = stack;
            }
            stack.Capacity = WasteCapacity;
            return stack;
        }

        private static void AddOverflowEvent(SimulationContext context, string sourceKind, int discarded, List<GameEvent> events)
        {
            events.Add(context.Events.Create(WasteOverflowDiscardedEvent, "system:core:waste", new WasteOverflowPayload
            {
                SourceKind = sourceKind,
                DiscardedAmount = discarded,
                TotalDiscardedAmount = context.State.Waste.TotalDiscardedAmount
            }));
        }

        private static void AddThresholdEventIfChanged(
            SimulationContext context,
            int oldSatisfaction,
            int oldDisease,
            List<GameEvent> events)
        {
            WasteRuntimeState runtime = context.State.Waste;
            if (oldSatisfaction == runtime.AccumulatedSatisfactionPenaltyBasisPoints &&
                oldDisease == runtime.DiseaseChanceBonusBasisPoints)
                return;
            events.Add(context.Events.Create(WasteThresholdChangedEvent, "system:core:waste", new WasteThresholdPayload
            {
                WasteAmount = EnsureWasteStack(context.State).Amount,
                PreviousSatisfactionPenaltyBasisPoints = oldSatisfaction,
                SatisfactionPenaltyBasisPoints = runtime.AccumulatedSatisfactionPenaltyBasisPoints,
                PreviousDiseaseChanceBonusBasisPoints = oldDisease,
                DiseaseChanceBonusBasisPoints = runtime.DiseaseChanceBonusBasisPoints
            }));
        }
    }

    public sealed class WasteSettlementPayload
    {
        public long SettlementTick { get; set; }
        public int ActiveBuildingCount { get; set; }
        public int LivingNpcCount { get; set; }
        public int GeneratedAmount { get; set; }
        public int StoredAmount { get; set; }
        public int DiscardedAmount { get; set; }
        public int WasteAmount { get; set; }
        public int NpcHalfUnitRemainder { get; set; }
    }

    public sealed class WasteDeathPayload
    {
        public string NpcId { get; set; } = string.Empty;
        public long TransitionTick { get; set; }
        public int StoredAmount { get; set; }
        public int DiscardedAmount { get; set; }
    }

    public sealed class WasteOverflowPayload
    {
        public string SourceKind { get; set; } = string.Empty;
        public int DiscardedAmount { get; set; }
        public long TotalDiscardedAmount { get; set; }
    }

    public sealed class WasteThresholdPayload
    {
        public int WasteAmount { get; set; }
        public int PreviousSatisfactionPenaltyBasisPoints { get; set; }
        public int SatisfactionPenaltyBasisPoints { get; set; }
        public int PreviousDiseaseChanceBonusBasisPoints { get; set; }
        public int DiseaseChanceBonusBasisPoints { get; set; }
    }

    public sealed class WasteDiseaseExposurePayload
    {
        public long SettlementTick { get; set; }
        public int ChanceBasisPoints { get; set; }
        public int ExposureCount { get; set; }
        public List<string> TriggeredNpcIds { get; set; } = new List<string>();
    }
}
