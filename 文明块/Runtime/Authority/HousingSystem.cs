using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class HousingSystem : ISimulationSystem
    {
        public const string AssignHousingCommand = "command:core:assign_housing";
        public const string HousingAssignedEvent = "event:core:housing_assigned";
        public const string HousingReleasedEvent = "event:core:housing_released";

        private readonly JsonSerializerOptions _jsonOptions;

        public HousingSystem(JsonSerializerOptions jsonOptions = null)
        {
            _jsonOptions = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
        }

        public void RegisterCommands(CommandBus commandBus)
        {
            commandBus.Register(new AssignHousingCommandHandler(_jsonOptions));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            List<GameEvent> events = new List<GameEvent>();
            ReconcileAssignments(context, events);
            return events;
        }

        public static bool IsEligible(NpcInstanceState npc)
        {
            return npc != null && npc.IsAlive && npc.IsAdult && !npc.IsPermanentlyDeparted;
        }

        public static int ResolveBedSlotCount(BuildingInstanceState building, BuildingDefinition definition)
        {
            int extraLevels = Math.Max(0, building.Level - 1);
            return checked(definition.BedSlotCount + extraLevels * definition.AdditionalBedSlotsPerLevel);
        }

        public static void ReconcileAssignments(SimulationContext context, List<GameEvent> events)
        {
            List<HousingAssignmentState> assignments = new List<HousingAssignmentState>(context.State.Housing.AssignmentsByNpcId.Values);
            assignments.RemoveAll(item => item == null);
            assignments.Sort(CompareAssignments);
            HashSet<string> occupied = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < assignments.Count; index++)
            {
                HousingAssignmentState assignment = assignments[index];
                string reason = GetInvalidReason(context, assignment, occupied);
                if (string.IsNullOrEmpty(reason))
                {
                    occupied.Add(SlotKey(assignment.BuildingId, assignment.BedSlotIndex));
                    continue;
                }

                if (context.State.Housing.AssignmentsByNpcId.Remove(assignment.NpcId))
                {
                    AddReleasedEvent(context, events, assignment, reason, "system:core:housing");
                }
            }

            List<BuildingInstanceState> houses = GetOrderedOperationalHouses(context);
            List<NpcInstanceState> npcs = new List<NpcInstanceState>(context.State.Npcs.Instances.Values);
            npcs.RemoveAll(npc => !IsEligible(npc) || context.State.Housing.AssignmentsByNpcId.ContainsKey(npc.NpcId));
            npcs.Sort(CompareNpcs);

            int houseIndex = 0;
            int slotIndex = 0;
            for (int npcIndex = 0; npcIndex < npcs.Count; npcIndex++)
            {
                while (houseIndex < houses.Count)
                {
                    BuildingInstanceState house = houses[houseIndex];
                    BuildingDefinition definition = context.Definitions.TryGetBuilding(house.DefinitionId, out BuildingDefinition found)
                        ? found : null;
                    int slotCount = definition == null ? 0 : ResolveBedSlotCount(house, definition);
                    while (slotIndex < slotCount && occupied.Contains(SlotKey(house.BuildingId, slotIndex)))
                    {
                        slotIndex++;
                    }
                    if (slotIndex < slotCount)
                    {
                        HousingAssignmentState assignment = new HousingAssignmentState
                        {
                            NpcId = npcs[npcIndex].NpcId,
                            BuildingId = house.BuildingId,
                            BedSlotIndex = slotIndex,
                            AssignedTick = context.State.SimulationTick
                        };
                        context.State.Housing.AssignmentsByNpcId[assignment.NpcId] = assignment;
                        occupied.Add(SlotKey(assignment.BuildingId, assignment.BedSlotIndex));
                        AddAssignedEvent(context, events, assignment, "system:core:housing");
                        slotIndex++;
                        break;
                    }
                    houseIndex++;
                    slotIndex = 0;
                }
            }

            context.State.Housing.HomelessAdultNpcIds.Clear();
            foreach (NpcInstanceState npc in context.State.Npcs.Instances.Values)
            {
                if (IsEligible(npc) && !context.State.Housing.AssignmentsByNpcId.ContainsKey(npc.NpcId))
                {
                    context.State.Housing.HomelessAdultNpcIds.Add(npc.NpcId);
                }
            }
        }

        private sealed class AssignHousingCommandHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _jsonOptions;

            public AssignHousingCommandHandler(JsonSerializerOptions jsonOptions) { _jsonOptions = jsonOptions; }
            public string CommandType { get { return AssignHousingCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                AssignHousingPayload payload = Deserialize<AssignHousingPayload>(command.Payload, _jsonOptions);
                if (!StableId.IsValid(payload.NpcId) || !StableId.IsValid(payload.BuildingId))
                    return ValidationResult.Invalid("NPC and building ids must use namespace:type:id format.");
                if (!context.State.Npcs.Instances.TryGetValue(payload.NpcId, out NpcInstanceState npc))
                    return ValidationResult.Invalid($"Unknown NPC {payload.NpcId}.", CommandErrorCodes.NpcNotFound);
                if (!StringComparer.Ordinal.Equals(npc.OwnerPlayerId, command.PlayerId))
                    return ValidationResult.Invalid($"Player cannot house NPC {payload.NpcId}.", CommandErrorCodes.NpcAssignmentUnauthorized);
                if (!IsEligible(npc))
                    return ValidationResult.Invalid($"NPC {payload.NpcId} is not eligible for housing.", CommandErrorCodes.HousingNpcIneligible);
                if (!TryGetHouse(context, payload.BuildingId, out BuildingInstanceState house, out BuildingDefinition definition))
                    return context.State.Buildings.Instances.ContainsKey(payload.BuildingId)
                        ? ValidationResult.Invalid($"Building {payload.BuildingId} cannot provide housing.", CommandErrorCodes.HousingUnavailable)
                        : ValidationResult.Invalid($"Unknown building {payload.BuildingId}.", CommandErrorCodes.BuildingNotFound);

                int slotCount = ResolveBedSlotCount(house, definition);
                if (payload.BedSlotIndex.HasValue && (payload.BedSlotIndex.Value < 0 || payload.BedSlotIndex.Value >= slotCount))
                    return ValidationResult.Invalid("Requested bed slot is invalid.", CommandErrorCodes.HousingUnavailable);
                if (context.State.Housing.AssignmentsByNpcId.TryGetValue(payload.NpcId, out HousingAssignmentState existing) &&
                    StringComparer.Ordinal.Equals(existing.BuildingId, payload.BuildingId) &&
                    (!payload.BedSlotIndex.HasValue || payload.BedSlotIndex.Value == existing.BedSlotIndex))
                    return ValidationResult.Invalid($"NPC {payload.NpcId} already has this housing assignment.", CommandErrorCodes.HousingAlreadyAssigned);
                if (FindAvailableSlot(context.State, payload.BuildingId, slotCount, payload.NpcId, payload.BedSlotIndex) < 0)
                    return ValidationResult.Invalid($"Building {payload.BuildingId} has no available bed.", CommandErrorCodes.HousingUnavailable);
                return ValidationResult.Valid();
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                AssignHousingPayload payload = Deserialize<AssignHousingPayload>(command.Payload, _jsonOptions);
                TryGetHouse(context, payload.BuildingId, out BuildingInstanceState house, out BuildingDefinition definition);
                int slot = FindAvailableSlot(context.State, payload.BuildingId, ResolveBedSlotCount(house, definition), payload.NpcId, payload.BedSlotIndex);
                List<GameEvent> events = new List<GameEvent>();
                if (context.State.Housing.AssignmentsByNpcId.TryGetValue(payload.NpcId, out HousingAssignmentState old))
                {
                    context.State.Housing.AssignmentsByNpcId.Remove(payload.NpcId);
                    AddReleasedEvent(context, events, old, HousingReleaseReasons.ManualReassignment, command.CommandId);
                }
                HousingAssignmentState assignment = new HousingAssignmentState
                {
                    NpcId = payload.NpcId, BuildingId = payload.BuildingId, BedSlotIndex = slot,
                    IsManual = true, AssignedTick = context.State.SimulationTick
                };
                context.State.Housing.AssignmentsByNpcId[assignment.NpcId] = assignment;
                AddAssignedEvent(context, events, assignment, command.CommandId);
                ReconcileAssignments(context, events);
                return events;
            }
        }

        private static bool TryGetHouse(SimulationContext context, string buildingId, out BuildingInstanceState house, out BuildingDefinition definition)
        {
            definition = null;
            return context.State.Buildings.Instances.TryGetValue(buildingId, out house) &&
                   BuildingOperationalRules.IsOperational(house) &&
                   context.Definitions.TryGetBuilding(house.DefinitionId, out definition) &&
                   ResolveBedSlotCount(house, definition) > 0;
        }

        private static List<BuildingInstanceState> GetOrderedOperationalHouses(SimulationContext context)
        {
            List<BuildingInstanceState> houses = new List<BuildingInstanceState>();
            foreach (BuildingInstanceState building in context.State.Buildings.Instances.Values)
                if (TryGetHouse(context, building.BuildingId, out _, out _)) houses.Add(building);
            houses.Sort((left, right) => left.ConstructionSequence != right.ConstructionSequence
                ? left.ConstructionSequence.CompareTo(right.ConstructionSequence)
                : StringComparer.Ordinal.Compare(left.BuildingId, right.BuildingId));
            return houses;
        }

        private static string GetInvalidReason(SimulationContext context, HousingAssignmentState assignment, HashSet<string> occupied)
        {
            if (!context.State.Npcs.Instances.TryGetValue(assignment.NpcId, out NpcInstanceState npc) || !IsEligible(npc)) return HousingReleaseReasons.NpcUnavailable;
            if (!context.State.Buildings.Instances.ContainsKey(assignment.BuildingId)) return HousingReleaseReasons.BuildingMissing;
            if (!TryGetHouse(context, assignment.BuildingId, out BuildingInstanceState house, out BuildingDefinition definition)) return HousingReleaseReasons.BuildingInoperable;
            if (assignment.BedSlotIndex < 0 || assignment.BedSlotIndex >= ResolveBedSlotCount(house, definition)) return HousingReleaseReasons.SlotInvalid;
            return occupied.Contains(SlotKey(assignment.BuildingId, assignment.BedSlotIndex)) ? HousingReleaseReasons.SlotConflict : string.Empty;
        }

        private static int FindAvailableSlot(GameState state, string buildingId, int slotCount, string ignoredNpcId, int? requested)
        {
            bool[] occupied = new bool[slotCount];
            foreach (HousingAssignmentState assignment in state.Housing.AssignmentsByNpcId.Values)
                if (assignment != null && !StringComparer.Ordinal.Equals(assignment.NpcId, ignoredNpcId) &&
                    StringComparer.Ordinal.Equals(assignment.BuildingId, buildingId) && assignment.BedSlotIndex >= 0 && assignment.BedSlotIndex < slotCount)
                    occupied[assignment.BedSlotIndex] = true;
            if (requested.HasValue) return occupied[requested.Value] ? -1 : requested.Value;
            for (int index = 0; index < occupied.Length; index++) if (!occupied[index]) return index;
            return -1;
        }

        private static int CompareAssignments(HousingAssignmentState left, HousingAssignmentState right)
        {
            int manual = right.IsManual.CompareTo(left.IsManual);
            return manual != 0 ? manual : StringComparer.Ordinal.Compare(left.NpcId, right.NpcId);
        }

        private static int CompareNpcs(NpcInstanceState left, NpcInstanceState right)
        {
            int sequence = left.CreationSequence.CompareTo(right.CreationSequence);
            return sequence != 0 ? sequence : StringComparer.Ordinal.Compare(left.NpcId, right.NpcId);
        }

        private static string SlotKey(string buildingId, int slot) { return buildingId + "\n" + slot; }
        private static T Deserialize<T>(JsonElement payload, JsonSerializerOptions options)
        {
            T value = payload.Deserialize<T>(options);
            return value == null ? throw new InvalidOperationException("Command payload could not be deserialized.") : value;
        }

        private static void AddAssignedEvent(SimulationContext context, List<GameEvent> events, HousingAssignmentState assignment, string source)
        {
            events.Add(context.Events.Create(HousingAssignedEvent, source, new HousingAssignedPayload
            { NpcId = assignment.NpcId, BuildingId = assignment.BuildingId, BedSlotIndex = assignment.BedSlotIndex, IsManual = assignment.IsManual }));
        }

        private static void AddReleasedEvent(SimulationContext context, List<GameEvent> events, HousingAssignmentState assignment, string reason, string source)
        {
            events.Add(context.Events.Create(HousingReleasedEvent, source, new HousingReleasedPayload
            { NpcId = assignment.NpcId, BuildingId = assignment.BuildingId, BedSlotIndex = assignment.BedSlotIndex, Reason = reason }));
        }
    }

    public static class HousingReleaseReasons
    {
        public const string ManualReassignment = "manual_reassignment";
        public const string NpcUnavailable = "npc_unavailable";
        public const string BuildingMissing = "building_missing";
        public const string BuildingInoperable = "building_inoperable";
        public const string SlotInvalid = "slot_invalid";
        public const string SlotConflict = "slot_conflict";
    }

    public sealed class AssignHousingPayload
    {
        public string NpcId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public int? BedSlotIndex { get; set; }
    }

    public sealed class HousingAssignedPayload
    {
        public string NpcId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public int BedSlotIndex { get; set; }
        public bool IsManual { get; set; }
    }

    public sealed class HousingReleasedPayload
    {
        public string NpcId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public int BedSlotIndex { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
