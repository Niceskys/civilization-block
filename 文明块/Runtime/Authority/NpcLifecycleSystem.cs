using System;
using System.Collections.Generic;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class NpcLifecycleSystem : ISimulationSystem
    {
        public const string NpcBecameAdultEvent = "event:core:npc_became_adult";
        public const string NpcDiedEvent = "event:core:npc_died";
        public const int MinimumAdultLifespanDays = 20;
        public const int MaximumAdultLifespanDays = 40;
        public static readonly long InfantGrowthTicks = GameTime.TicksPerGameDay * 5 / 2;

        public void RegisterCommands(CommandBus commandBus)
        {
            if (commandBus == null) throw new ArgumentNullException(nameof(commandBus));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            List<GameEvent> events = new List<GameEvent>();
            List<PendingLifecycleEvent> pendingEvents = new List<PendingLifecycleEvent>();
            List<NpcInstanceState> npcs = new List<NpcInstanceState>(context.State.Npcs.Instances.Values);
            npcs.RemoveAll(npc => npc == null);
            npcs.Sort(CompareNpcs);
            long intervalStart = checked(context.State.SimulationTick - deltaTicks);

            for (int index = 0; index < npcs.Count; index++)
            {
                NpcInstanceState npc = npcs[index];
                EnsureLifespan(context.State, npc);
                if (!npc.IsAlive || npc.IsPermanentlyDeparted) continue;

                long remaining = deltaTicks;
                long cursor = intervalStart;
                if (!npc.IsAdult)
                {
                    long untilAdult = InfantGrowthTicks - npc.LifeStageElapsedTicks;
                    if (remaining < untilAdult)
                    {
                        npc.LifeStageElapsedTicks = checked(npc.LifeStageElapsedTicks + remaining);
                        continue;
                    }

                    npc.IsAdult = true;
                    npc.LifeStageElapsedTicks = 0;
                    npc.AdultTransitionTick = checked(cursor + untilAdult);
                    cursor = npc.AdultTransitionTick;
                    remaining -= untilAdult;
                    pendingEvents.Add(new PendingLifecycleEvent
                    {
                        EventType = NpcBecameAdultEvent,
                        Payload = new NpcLifecyclePayload
                        {
                            NpcId = npc.NpcId,
                            TransitionTick = npc.AdultTransitionTick,
                            AdultLifespanTicks = npc.AdultLifespanTicks
                        }
                    });
                }

                long untilDeath = npc.AdultLifespanTicks - npc.LifeStageElapsedTicks;
                if (remaining < untilDeath)
                {
                    npc.LifeStageElapsedTicks = checked(npc.LifeStageElapsedTicks + remaining);
                    continue;
                }

                npc.LifeStageElapsedTicks = npc.AdultLifespanTicks;
                npc.IsAlive = false;
                npc.DeathTick = checked(cursor + untilDeath);
                pendingEvents.Add(new PendingLifecycleEvent
                {
                    EventType = NpcDiedEvent,
                    Payload = new NpcLifecyclePayload
                    {
                        NpcId = npc.NpcId,
                        TransitionTick = npc.DeathTick,
                        AdultLifespanTicks = npc.AdultLifespanTicks
                    }
                });
            }

            pendingEvents.Sort((left, right) =>
            {
                int tick = left.Payload.TransitionTick.CompareTo(right.Payload.TransitionTick);
                if (tick != 0) return tick;
                int type = StringComparer.Ordinal.Compare(left.EventType, right.EventType);
                return type != 0 ? type : StringComparer.Ordinal.Compare(left.Payload.NpcId, right.Payload.NpcId);
            });
            for (int index = 0; index < pendingEvents.Count; index++)
            {
                PendingLifecycleEvent pending = pendingEvents[index];
                events.Add(context.Events.Create(pending.EventType, "system:core:npc_lifecycle", pending.Payload));
                if (StringComparer.Ordinal.Equals(pending.EventType, NpcDiedEvent))
                    WasteGenerationSystem.AddDeathWaste(context, pending.Payload.NpcId, pending.Payload.TransitionTick, events);
            }

            HousingSystem.ReconcileAssignments(context, events);
            WorkerAssignmentSystem.ReconcileAssignments(context, events);
            return events;
        }

        public static void EnsureLifespan(GameState state, NpcInstanceState npc)
        {
            if (npc.AdultLifespanTicks <= 0)
                npc.AdultLifespanTicks = ResolveAdultLifespanTicks(state.World == null ? string.Empty : state.World.Seed, npc.NpcId);
        }

        public static long ResolveAdultLifespanTicks(string worldSeed, string npcId)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            string value = (worldSeed ?? string.Empty) + "\n" + (npcId ?? string.Empty);
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= prime;
            }
            int dayRange = MaximumAdultLifespanDays - MinimumAdultLifespanDays + 1;
            int days = MinimumAdultLifespanDays + (int)(hash % (uint)dayRange);
            return checked(days * GameTime.TicksPerGameDay);
        }

        private static int CompareNpcs(NpcInstanceState left, NpcInstanceState right)
        {
            int sequence = left.CreationSequence.CompareTo(right.CreationSequence);
            return sequence != 0 ? sequence : StringComparer.Ordinal.Compare(left.NpcId, right.NpcId);
        }

        private sealed class PendingLifecycleEvent
        {
            public string EventType { get; set; } = string.Empty;
            public NpcLifecyclePayload Payload { get; set; }
        }
    }

    public sealed class NpcLifecyclePayload
    {
        public string NpcId { get; set; } = string.Empty;
        public long TransitionTick { get; set; }
        public long AdultLifespanTicks { get; set; }
    }
}
