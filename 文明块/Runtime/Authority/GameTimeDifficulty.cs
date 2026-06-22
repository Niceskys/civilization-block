using System;

namespace WenMingBlocks.Runtime.Authority
{
    public static class GameTime
    {
        public const long TicksPerGameSecond = 1;
        public const long TicksPerGameMinute = 60;
        public const long TicksPerGameDay = 24 * 60 * TicksPerGameMinute;
        public const int RealSecondsPerGameDayAtNormalSpeed = 20 * 60;
        public const int NormalSpeedTicksPerRealSecond = (int)(TicksPerGameDay / RealSecondsPerGameDayAtNormalSpeed);

        public static long TicksFromRealMinutesAtNormalSpeed(int realMinutes)
        {
            if (realMinutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(realMinutes));
            }

            return checked((long)realMinutes * 60 * NormalSpeedTicksPerRealSecond);
        }

        public static long TicksFromRealSecondsAtNormalSpeed(int realSeconds)
        {
            if (realSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(realSeconds));
            }

            return checked((long)realSeconds * NormalSpeedTicksPerRealSecond);
        }

        public static long ResolveElapsedTicks(long realSeconds, int speedMultiplier)
        {
            if (realSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(realSeconds));
            }

            if (speedMultiplier < 0 || speedMultiplier > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(speedMultiplier), "Time speed must be pause, 1x, or 2x.");
            }

            return checked(realSeconds * NormalSpeedTicksPerRealSecond * speedMultiplier);
        }
    }

    public static class DifficultyIds
    {
        public const string Easy = "difficulty:core:easy";
        public const string Normal = "difficulty:core:normal";
        public const string Hard = "difficulty:core:hard";
        public const string Extreme = "difficulty:core:extreme";
        public const string Custom = "difficulty:core:custom";
    }

    public static class StructuralFailureModes
    {
        public const string AutomaticCollapse = "automatic_collapse";
        public const string DisableOnly = "disable_only";
    }

    public sealed class DifficultyState
    {
        public string DifficultyId { get; set; } = DifficultyIds.Normal;
        public long CustomStructuralGraceTicks { get; set; } = GameTime.TicksFromRealMinutesAtNormalSpeed(30);
        public long CustomCollapseIntervalTicks { get; set; } = GameTime.TicksFromRealSecondsAtNormalSpeed(5);
        public string CustomStructuralFailureMode { get; set; } = StructuralFailureModes.AutomaticCollapse;
    }

    public sealed class StructuralFailurePolicy
    {
        public string DifficultyId { get; }
        public long GraceTicks { get; }
        public long CollapseIntervalTicks { get; }
        public string FailureMode { get; }

        public StructuralFailurePolicy(
            string difficultyId,
            long graceTicks,
            long collapseIntervalTicks,
            string failureMode)
        {
            DifficultyId = difficultyId;
            GraceTicks = graceTicks;
            CollapseIntervalTicks = collapseIntervalTicks;
            FailureMode = failureMode;
        }
    }

    public static class DifficultyProfiles
    {
        public static readonly long MaximumCustomGraceTicks = GameTime.TicksFromRealMinutesAtNormalSpeed(120);
        public static readonly long MinimumCustomCollapseIntervalTicks = GameTime.TicksFromRealSecondsAtNormalSpeed(1);
        public static readonly long MaximumCustomCollapseIntervalTicks = GameTime.TicksFromRealSecondsAtNormalSpeed(10);

        public static StructuralFailurePolicy ResolveStructuralFailure(DifficultyState difficulty)
        {
            if (difficulty == null)
            {
                throw new ArgumentNullException(nameof(difficulty));
            }

            switch (difficulty.DifficultyId)
            {
                case DifficultyIds.Easy:
                    return CreateAutomatic(DifficultyIds.Easy, 60, 10);
                case DifficultyIds.Normal:
                    return CreateAutomatic(DifficultyIds.Normal, 30, 5);
                case DifficultyIds.Hard:
                    return CreateAutomatic(DifficultyIds.Hard, 15, 3);
                case DifficultyIds.Extreme:
                    return CreateAutomatic(DifficultyIds.Extreme, 5, 1);
                case DifficultyIds.Custom:
                    ValidateCustom(difficulty);
                    return new StructuralFailurePolicy(
                        DifficultyIds.Custom,
                        difficulty.CustomStructuralGraceTicks,
                        difficulty.CustomCollapseIntervalTicks,
                        difficulty.CustomStructuralFailureMode);
                default:
                    throw new InvalidOperationException($"Unknown difficulty id {difficulty.DifficultyId}.");
            }
        }

        public static void ValidateCustom(DifficultyState difficulty)
        {
            if (difficulty.CustomStructuralGraceTicks < 0 ||
                difficulty.CustomStructuralGraceTicks > MaximumCustomGraceTicks)
            {
                throw new InvalidOperationException("Custom structural grace must be between 0 and 120 normal-speed real minutes.");
            }

            if (difficulty.CustomCollapseIntervalTicks < MinimumCustomCollapseIntervalTicks ||
                difficulty.CustomCollapseIntervalTicks > MaximumCustomCollapseIntervalTicks)
            {
                throw new InvalidOperationException("Custom collapse interval must be between 1 and 10 normal-speed real seconds.");
            }

            if (!StringComparer.Ordinal.Equals(difficulty.CustomStructuralFailureMode, StructuralFailureModes.AutomaticCollapse) &&
                !StringComparer.Ordinal.Equals(difficulty.CustomStructuralFailureMode, StructuralFailureModes.DisableOnly))
            {
                throw new InvalidOperationException($"Unknown structural failure mode {difficulty.CustomStructuralFailureMode}.");
            }
        }

        private static StructuralFailurePolicy CreateAutomatic(
            string difficultyId,
            int graceRealMinutes,
            int collapseRealSeconds)
        {
            return new StructuralFailurePolicy(
                difficultyId,
                GameTime.TicksFromRealMinutesAtNormalSpeed(graceRealMinutes),
                GameTime.TicksFromRealSecondsAtNormalSpeed(collapseRealSeconds),
                StructuralFailureModes.AutomaticCollapse);
        }
    }

    public static class StructuralGraceClock
    {
        public static long CreateDeadline(long currentTick, StructuralFailurePolicy policy)
        {
            if (currentTick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentTick));
            }

            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            return checked(currentTick + policy.GraceTicks);
        }

        public static bool IsExpired(long currentTick, long deadlineTick)
        {
            if (currentTick < 0 || deadlineTick < 0)
            {
                throw new ArgumentOutOfRangeException(currentTick < 0 ? nameof(currentTick) : nameof(deadlineTick));
            }

            return currentTick >= deadlineTick;
        }
    }
}
