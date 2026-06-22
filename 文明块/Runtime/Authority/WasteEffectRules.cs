using System;

namespace WenMingBlocks.Runtime.Authority
{
    public static class WasteEffectRules
    {
        public const int BasisPointsPerWhole = 10000;

        public static int ResolveWorkEfficiencyBasisPoints(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            int penalty = state.Waste == null ? 0 : state.Waste.AccumulatedSatisfactionPenaltyBasisPoints;
            return Math.Max(0, BasisPointsPerWhole - penalty);
        }

        public static int ResolveNpcSatisfactionBasisPoints(GameState state, NpcInstanceState npc)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (npc == null) throw new ArgumentNullException(nameof(npc));
            int penalty = state.Waste == null ? 0 : state.Waste.AccumulatedSatisfactionPenaltyBasisPoints;
            return Math.Max(0, npc.BaseSatisfactionBasisPoints - penalty);
        }

        public static long ApplyEfficiency(
            long elapsedTicks,
            int efficiencyBasisPoints,
            ref int remainderBasisPointTicks)
        {
            if (elapsedTicks < 0) throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
            if (efficiencyBasisPoints < 0 || efficiencyBasisPoints > BasisPointsPerWhole)
                throw new ArgumentOutOfRangeException(nameof(efficiencyBasisPoints));
            if (remainderBasisPointTicks < 0 || remainderBasisPointTicks >= BasisPointsPerWhole)
                throw new ArgumentOutOfRangeException(nameof(remainderBasisPointTicks));

            long numerator = checked(checked(elapsedTicks * efficiencyBasisPoints) + remainderBasisPointTicks);
            long effectiveTicks = numerator / BasisPointsPerWhole;
            remainderBasisPointTicks = (int)(numerator % BasisPointsPerWhole);
            return effectiveTicks;
        }

        public static long ActualTicksToReachEffectiveTicks(
            long effectiveTicks,
            int efficiencyBasisPoints,
            int remainderBasisPointTicks)
        {
            if (effectiveTicks <= 0) return 0;
            if (efficiencyBasisPoints <= 0) return long.MaxValue;
            long needed = checked(checked(effectiveTicks * BasisPointsPerWhole) - remainderBasisPointTicks);
            if (needed <= 0) return 0;
            return checked((needed + efficiencyBasisPoints - 1) / efficiencyBasisPoints);
        }
    }
}
