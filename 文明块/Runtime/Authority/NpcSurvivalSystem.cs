using System;
using System.Collections.Generic;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class NpcSurvivalSystem : ISimulationSystem
    {
        public const string SurvivalSettledEvent = "event:core:npc_survival_settled";
        public const int QuarterUnitsPerResource = 4;
        public const int AdultFoodQuarterUnitsPerDay = 2;
        public const int InfantFoodQuarterUnitsPerDay = 1;
        public const int WaterQuarterUnitsPerDay = 2;

        public void RegisterCommands(CommandBus commandBus)
        {
            if (commandBus == null) throw new ArgumentNullException(nameof(commandBus));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            List<GameEvent> events = new List<GameEvent>();
            SurvivalRuntimeState survival = context.State.Survival;
            if (survival.NextSettlementTick <= 0)
                survival.NextSettlementTick = NextDayBoundary(context.State.SimulationTick - deltaTicks);

            while (survival.NextSettlementTick <= context.State.SimulationTick)
            {
                SettleDay(context, survival.NextSettlementTick, events);
                survival.NextSettlementTick = checked(survival.NextSettlementTick + GameTime.TicksPerGameDay);
            }
            return events;
        }

        public static bool RequiresResources(NpcInstanceState npc)
        {
            return npc != null && npc.IsAlive && !npc.IsPermanentlyDeparted;
        }

        public static long NextDayBoundary(long tick)
        {
            if (tick < 0) tick = 0;
            return checked((tick / GameTime.TicksPerGameDay + 1) * GameTime.TicksPerGameDay);
        }

        private static void SettleDay(SimulationContext context, long settlementTick, List<GameEvent> events)
        {
            SurvivalRuntimeState survival = context.State.Survival;
            int foodQuarters = survival.FoodRemainderQuarterUnits;
            int waterQuarters = survival.WaterRemainderQuarterUnits;
            int population = 0;
            foreach (NpcInstanceState npc in context.State.Npcs.Instances.Values)
            {
                if (!RequiresResourcesAtTick(npc, settlementTick)) continue;
                population++;
                foodQuarters = checked(foodQuarters + (IsAdultAtTick(npc, settlementTick)
                    ? AdultFoodQuarterUnitsPerDay : InfantFoodQuarterUnitsPerDay));
                waterQuarters = checked(waterQuarters + WaterQuarterUnitsPerDay);
            }

            int foodRequired = foodQuarters / QuarterUnitsPerResource;
            int waterRequired = waterQuarters / QuarterUnitsPerResource;
            survival.FoodRemainderQuarterUnits = foodQuarters % QuarterUnitsPerResource;
            survival.WaterRemainderQuarterUnits = waterQuarters % QuarterUnitsPerResource;
            int foodConsumed = ConsumeUnlocked(context.State.Resources, CoreResourceIds.Food, foodRequired);
            int waterConsumed = ConsumeUnlocked(context.State.Resources, CoreResourceIds.Water, waterRequired);

            survival.LastSettlementTick = settlementTick;
            survival.LastFoodRequired = foodRequired;
            survival.LastFoodConsumed = foodConsumed;
            survival.LastFoodShortage = foodRequired - foodConsumed;
            survival.LastWaterRequired = waterRequired;
            survival.LastWaterConsumed = waterConsumed;
            survival.LastWaterShortage = waterRequired - waterConsumed;
            survival.ConsecutiveFoodShortageDays = survival.LastFoodShortage > 0
                ? checked(survival.ConsecutiveFoodShortageDays + 1) : 0;
            survival.ConsecutiveWaterShortageDays = survival.LastWaterShortage > 0
                ? checked(survival.ConsecutiveWaterShortageDays + 1) : 0;

            events.Add(context.Events.Create(SurvivalSettledEvent, "system:core:npc_survival", new NpcSurvivalSettledPayload
            {
                SettlementTick = settlementTick,
                Population = population,
                FoodRequired = foodRequired,
                FoodConsumed = foodConsumed,
                FoodShortage = survival.LastFoodShortage,
                WaterRequired = waterRequired,
                WaterConsumed = waterConsumed,
                WaterShortage = survival.LastWaterShortage,
                FoodRemainderQuarterUnits = survival.FoodRemainderQuarterUnits,
                WaterRemainderQuarterUnits = survival.WaterRemainderQuarterUnits
            }));
        }

        private static bool RequiresResourcesAtTick(NpcInstanceState npc, long settlementTick)
        {
            if (npc == null || npc.IsPermanentlyDeparted) return false;
            return npc.IsAlive || (npc.DeathTick > 0 && settlementTick < npc.DeathTick);
        }

        private static bool IsAdultAtTick(NpcInstanceState npc, long settlementTick)
        {
            return npc.AdultTransitionTick > 0 ? settlementTick >= npc.AdultTransitionTick : npc.IsAdult;
        }

        private static int ConsumeUnlocked(ResourceState resources, string resourceId, int required)
        {
            if (required <= 0 || resources == null || resources.Items == null ||
                !resources.Items.TryGetValue(resourceId, out ResourceStack stack) || stack == null)
                return 0;
            int consumed = Math.Min(required, Math.Max(0, stack.Amount - stack.LockedAmount));
            stack.Amount -= consumed;
            return consumed;
        }
    }

    public sealed class NpcSurvivalSettledPayload
    {
        public long SettlementTick { get; set; }
        public int Population { get; set; }
        public int FoodRequired { get; set; }
        public int FoodConsumed { get; set; }
        public int FoodShortage { get; set; }
        public int WaterRequired { get; set; }
        public int WaterConsumed { get; set; }
        public int WaterShortage { get; set; }
        public int FoodRemainderQuarterUnits { get; set; }
        public int WaterRemainderQuarterUnits { get; set; }
    }
}
