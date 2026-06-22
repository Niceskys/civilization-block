using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class WorkerAssignmentSystem : ISimulationSystem
    {
        public const string AssignWorkerCommand = "command:core:assign_worker";
        public const string UnassignWorkerCommand = "command:core:unassign_worker";
        public const string WorkerAssignedEvent = "event:core:worker_assigned";
        public const string WorkerReleasedEvent = "event:core:worker_released";

        private readonly JsonSerializerOptions _jsonOptions;

        public WorkerAssignmentSystem(JsonSerializerOptions jsonOptions = null)
        {
            _jsonOptions = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
        }

        public void RegisterCommands(CommandBus commandBus)
        {
            commandBus.Register(new AssignWorkerCommandHandler(_jsonOptions));
            commandBus.Register(new UnassignWorkerCommandHandler(_jsonOptions));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            List<GameEvent> events = new List<GameEvent>();
            ReconcileAssignments(context, events);
            return events;
        }

        public static void ReconcileAssignments(SimulationContext context, List<GameEvent> events)
        {
            List<WorkAssignmentState> ordered = new List<WorkAssignmentState>(context.State.Npcs.WorkAssignments.Values);
            ordered.RemoveAll(assignment => assignment == null);
            ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.NpcId, right.NpcId));
            HashSet<string> occupiedSlots = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < ordered.Count; i++)
            {
                WorkAssignmentState assignment = ordered[i];
                string reason = GetInvalidAssignmentReason(context, assignment, occupiedSlots);
                if (string.IsNullOrEmpty(reason))
                {
                    occupiedSlots.Add(CreateSlotKey(assignment.BuildingId, assignment.SlotIndex));
                    continue;
                }

                if (context.State.Npcs.WorkAssignments.Remove(assignment.NpcId))
                {
                    events.Add(context.Events.Create(WorkerReleasedEvent, "system:core:worker_assignment", new WorkerReleasedPayload
                    {
                        NpcId = assignment.NpcId,
                        BuildingId = assignment.BuildingId,
                        SlotIndex = assignment.SlotIndex,
                        Reason = reason
                    }));
                }
            }
        }

        private static string GetInvalidAssignmentReason(
            SimulationContext context,
            WorkAssignmentState assignment,
            HashSet<string> occupiedSlots)
        {
            if (!context.State.Npcs.Instances.TryGetValue(assignment.NpcId, out NpcInstanceState npc) ||
                !NpcOperationalRules.CanHoldWorkAssignment(npc))
            {
                return WorkerReleaseReasons.NpcUnavailable;
            }

            if (!context.State.Buildings.Instances.TryGetValue(assignment.BuildingId, out BuildingInstanceState building))
            {
                return WorkerReleaseReasons.BuildingMissing;
            }

            if (!BuildingOperationalRules.CanAcceptWorkers(building))
            {
                return WorkerReleaseReasons.BuildingInoperable;
            }

            if (!context.Definitions.TryGetBuilding(building.DefinitionId, out BuildingDefinition definition) ||
                assignment.SlotIndex < 0 || assignment.SlotIndex >= definition.WorkerSlotCount)
            {
                return WorkerReleaseReasons.SlotInvalid;
            }

            return occupiedSlots.Contains(CreateSlotKey(assignment.BuildingId, assignment.SlotIndex))
                ? WorkerReleaseReasons.SlotConflict
                : string.Empty;
        }

        private sealed class AssignWorkerCommandHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _jsonOptions;

            public AssignWorkerCommandHandler(JsonSerializerOptions jsonOptions)
            {
                _jsonOptions = jsonOptions;
            }

            public string CommandType
            {
                get { return AssignWorkerCommand; }
            }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                AssignWorkerPayload payload = Deserialize<AssignWorkerPayload>(command.Payload, _jsonOptions);
                if (!StableId.IsValid(payload.NpcId) || !StableId.IsValid(payload.BuildingId))
                {
                    return ValidationResult.Invalid("NPC and building ids must use namespace:type:id format.");
                }

                if (!context.State.Npcs.Instances.TryGetValue(payload.NpcId, out NpcInstanceState npc))
                {
                    return ValidationResult.Invalid($"Unknown NPC {payload.NpcId}.", CommandErrorCodes.NpcNotFound);
                }

                if (!StringComparer.Ordinal.Equals(npc.OwnerPlayerId, command.PlayerId))
                {
                    return ValidationResult.Invalid($"Player cannot assign NPC {payload.NpcId}.", CommandErrorCodes.NpcAssignmentUnauthorized);
                }

                if (!NpcOperationalRules.CanReceiveWorkAssignment(npc))
                {
                    return ValidationResult.Invalid($"NPC {payload.NpcId} cannot work.", CommandErrorCodes.NpcUnavailable);
                }

                if (!context.State.Buildings.Instances.TryGetValue(payload.BuildingId, out BuildingInstanceState building))
                {
                    return ValidationResult.Invalid($"Unknown building {payload.BuildingId}.", CommandErrorCodes.BuildingNotFound);
                }

                if (!BuildingOperationalRules.CanAcceptWorkers(building))
                {
                    return ValidationResult.Invalid($"Building {payload.BuildingId} cannot accept workers.", CommandErrorCodes.BuildingNotOperational);
                }

                if (!context.Definitions.TryGetBuilding(building.DefinitionId, out BuildingDefinition definition) ||
                    definition.WorkerSlotCount <= 0)
                {
                    return ValidationResult.Invalid($"Building {payload.BuildingId} has no worker slots.", CommandErrorCodes.WorkerSlotsFull);
                }

                if (context.State.Npcs.WorkAssignments.TryGetValue(payload.NpcId, out WorkAssignmentState existing) &&
                    StringComparer.Ordinal.Equals(existing.BuildingId, payload.BuildingId))
                {
                    return ValidationResult.Invalid($"NPC {payload.NpcId} is already assigned to this building.", CommandErrorCodes.NpcAlreadyAssigned);
                }

                if (FindAvailableSlot(context.State, payload.BuildingId, definition.WorkerSlotCount) < 0)
                {
                    return ValidationResult.Invalid($"Building {payload.BuildingId} has no available worker slot.", CommandErrorCodes.WorkerSlotsFull);
                }

                return ValidationResult.Valid();
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                AssignWorkerPayload payload = Deserialize<AssignWorkerPayload>(command.Payload, _jsonOptions);
                BuildingInstanceState building = context.State.Buildings.Instances[payload.BuildingId];
                BuildingDefinition definition = context.Definitions.TryGetBuilding(building.DefinitionId, out BuildingDefinition found)
                    ? found
                    : throw new InvalidOperationException("Building definition disappeared between validation and execution.");
                List<GameEvent> events = new List<GameEvent>();

                if (context.State.Npcs.WorkAssignments.TryGetValue(payload.NpcId, out WorkAssignmentState oldAssignment))
                {
                    context.State.Npcs.WorkAssignments.Remove(payload.NpcId);
                    events.Add(context.Events.Create(WorkerReleasedEvent, command.CommandId, new WorkerReleasedPayload
                    {
                        NpcId = oldAssignment.NpcId,
                        BuildingId = oldAssignment.BuildingId,
                        SlotIndex = oldAssignment.SlotIndex,
                        Reason = WorkerReleaseReasons.Reassigned
                    }));
                }

                int slotIndex = FindAvailableSlot(context.State, payload.BuildingId, definition.WorkerSlotCount);
                if (slotIndex < 0)
                {
                    throw new InvalidOperationException("Worker slot disappeared between validation and execution.");
                }

                WorkAssignmentState assignment = new WorkAssignmentState
                {
                    NpcId = payload.NpcId,
                    BuildingId = payload.BuildingId,
                    SlotIndex = slotIndex,
                    AssignedTick = context.State.SimulationTick
                };
                context.State.Npcs.WorkAssignments[assignment.NpcId] = assignment;
                events.Add(context.Events.Create(WorkerAssignedEvent, command.CommandId, new WorkerAssignedPayload
                {
                    NpcId = assignment.NpcId,
                    BuildingId = assignment.BuildingId,
                    SlotIndex = assignment.SlotIndex
                }));
                return events;
            }
        }

        private sealed class UnassignWorkerCommandHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _jsonOptions;

            public UnassignWorkerCommandHandler(JsonSerializerOptions jsonOptions)
            {
                _jsonOptions = jsonOptions;
            }

            public string CommandType
            {
                get { return UnassignWorkerCommand; }
            }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                UnassignWorkerPayload payload = Deserialize<UnassignWorkerPayload>(command.Payload, _jsonOptions);
                if (!context.State.Npcs.Instances.TryGetValue(payload.NpcId, out NpcInstanceState npc))
                {
                    return ValidationResult.Invalid($"Unknown NPC {payload.NpcId}.", CommandErrorCodes.NpcNotFound);
                }

                if (!StringComparer.Ordinal.Equals(npc.OwnerPlayerId, command.PlayerId))
                {
                    return ValidationResult.Invalid($"Player cannot unassign NPC {payload.NpcId}.", CommandErrorCodes.NpcAssignmentUnauthorized);
                }

                return context.State.Npcs.WorkAssignments.ContainsKey(payload.NpcId)
                    ? ValidationResult.Valid()
                    : ValidationResult.Invalid($"NPC {payload.NpcId} has no work assignment.", CommandErrorCodes.NpcNotAssigned);
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                UnassignWorkerPayload payload = Deserialize<UnassignWorkerPayload>(command.Payload, _jsonOptions);
                WorkAssignmentState assignment = context.State.Npcs.WorkAssignments[payload.NpcId];
                context.State.Npcs.WorkAssignments.Remove(payload.NpcId);
                return new[]
                {
                    context.Events.Create(WorkerReleasedEvent, command.CommandId, new WorkerReleasedPayload
                    {
                        NpcId = assignment.NpcId,
                        BuildingId = assignment.BuildingId,
                        SlotIndex = assignment.SlotIndex,
                        Reason = WorkerReleaseReasons.PlayerUnassigned
                    })
                };
            }
        }

        private static int FindAvailableSlot(GameState state, string buildingId, int slotCount)
        {
            bool[] occupied = new bool[slotCount];
            foreach (WorkAssignmentState assignment in state.Npcs.WorkAssignments.Values)
            {
                if (assignment != null && StringComparer.Ordinal.Equals(assignment.BuildingId, buildingId) &&
                    assignment.SlotIndex >= 0 && assignment.SlotIndex < slotCount)
                {
                    occupied[assignment.SlotIndex] = true;
                }
            }

            for (int i = 0; i < occupied.Length; i++)
            {
                if (!occupied[i])
                {
                    return i;
                }
            }

            return -1;
        }

        private static string CreateSlotKey(string buildingId, int slotIndex)
        {
            return buildingId + "\n" + slotIndex;
        }

        private static T Deserialize<T>(JsonElement payload, JsonSerializerOptions options)
        {
            T value = payload.Deserialize<T>(options);
            if (value == null)
            {
                throw new InvalidOperationException("Command payload could not be deserialized.");
            }

            return value;
        }
    }

    public static class WorkerReleaseReasons
    {
        public const string PlayerUnassigned = "player_unassigned";
        public const string Reassigned = "reassigned";
        public const string NpcUnavailable = "npc_unavailable";
        public const string BuildingMissing = "building_missing";
        public const string BuildingInoperable = "building_inoperable";
        public const string SlotInvalid = "slot_invalid";
        public const string SlotConflict = "slot_conflict";
    }

    public sealed class AssignWorkerPayload
    {
        public string NpcId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
    }

    public sealed class UnassignWorkerPayload
    {
        public string NpcId { get; set; } = string.Empty;
    }

    public sealed class WorkerAssignedPayload
    {
        public string NpcId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public int SlotIndex { get; set; }
    }

    public sealed class WorkerReleasedPayload
    {
        public string NpcId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public int SlotIndex { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
