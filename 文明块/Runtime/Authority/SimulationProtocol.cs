using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class CommandEnvelope
    {
        public string CommandId { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public JsonElement Payload { get; set; }
        public long ClientTimestamp { get; set; }
        public long Sequence { get; set; }
    }

    public sealed class GameEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public long SimulationTick { get; set; }
        public int Version { get; set; } = 1;
        public JsonElement Payload { get; set; }
    }

    public sealed class CommandResult
    {
        public bool Accepted { get; }
        public string Code { get; }
        public string Reason { get; }
        public IReadOnlyList<GameEvent> Events { get; }

        private CommandResult(bool accepted, string code, string reason, IReadOnlyList<GameEvent> events)
        {
            Accepted = accepted;
            Code = code;
            Reason = reason;
            Events = events;
        }

        public static CommandResult Reject(string reason, string code = CommandErrorCodes.ValidationFailed)
        {
            return new CommandResult(false, code, reason, Array.Empty<GameEvent>());
        }

        public static CommandResult Accept(IReadOnlyList<GameEvent> events)
        {
            return new CommandResult(true, string.Empty, string.Empty, events);
        }
    }

    public readonly struct ValidationResult
    {
        public bool IsValid { get; }
        public string Code { get; }
        public string Reason { get; }

        private ValidationResult(bool isValid, string code, string reason)
        {
            IsValid = isValid;
            Code = code;
            Reason = reason;
        }

        public static ValidationResult Valid()
        {
            return new ValidationResult(true, string.Empty, string.Empty);
        }

        public static ValidationResult Invalid(string reason, string code = CommandErrorCodes.ValidationFailed)
        {
            return new ValidationResult(false, code, reason);
        }
    }

    public static class CommandErrorCodes
    {
        public const string ValidationFailed = "error:core:validation_failed";
        public const string UnknownHandler = "error:core:unknown_command";
        public const string InvalidPayload = "error:core:invalid_payload";
        public const string ExecutionFailed = "error:core:execution_failed";
        public const string SpatialInvalidObject = "error:core:spatial_invalid_object";
        public const string SpatialInvalidDimensions = "error:core:spatial_invalid_dimensions";
        public const string SpatialInvalidRotation = "error:core:spatial_invalid_rotation";
        public const string SpatialInvalidCoordinates = "error:core:spatial_invalid_coordinates";
        public const string SpatialFootprintTooLarge = "error:core:spatial_footprint_too_large";
        public const string SpatialCoordinateOverflow = "error:core:spatial_coordinate_overflow";
        public const string SpatialOutOfBounds = "error:core:spatial_out_of_bounds";
        public const string SpatialOverlap = "error:core:spatial_overlap";
        public const string SpatialStateInvalid = "error:core:spatial_state_invalid";
        public const string StructuralUnsupported = "error:core:structural_unsupported";
        public const string StructuralInsufficientContact = "error:core:structural_insufficient_contact";
        public const string StructuralCapacityExceeded = "error:core:structural_capacity_exceeded";
        public const string StructuralStateInvalid = "error:core:structural_state_invalid";
        public const string StructuralLimitExceeded = "error:core:structural_limit_exceeded";
        public const string StructuralArithmeticOverflow = "error:core:structural_arithmetic_overflow";
        public const string BuildingNotFound = "error:core:building_not_found";
        public const string BuildingAlreadyDestroyed = "error:core:building_already_destroyed";
        public const string BuildingNotOperational = "error:core:building_not_operational";
        public const string StorageCapacityRequired = "error:core:storage_capacity_required";
        public const string NpcNotFound = "error:core:npc_not_found";
        public const string NpcUnavailable = "error:core:npc_unavailable";
        public const string NpcAssignmentUnauthorized = "error:core:npc_assignment_unauthorized";
        public const string NpcAlreadyAssigned = "error:core:npc_already_assigned";
        public const string NpcNotAssigned = "error:core:npc_not_assigned";
        public const string WorkerSlotsFull = "error:core:worker_slots_full";
        public const string ProductionRecipeNotFound = "error:core:production_recipe_not_found";
        public const string ProductionRecipeIncompatible = "error:core:production_recipe_incompatible";
        public const string ProductionBatchActive = "error:core:production_batch_active";
        public const string ProductionOutputPending = "error:core:production_output_pending";
        public const string ProductionInputUnavailable = "error:core:production_input_unavailable";
        public const string ProductionNotConfigured = "error:core:production_not_configured";
        public const string LogisticsEndpointInvalid = "error:core:logistics_endpoint_invalid";
        public const string LogisticsResourceUnavailable = "error:core:logistics_resource_unavailable";
        public const string LogisticsCapacityUnavailable = "error:core:logistics_capacity_unavailable";
        public const string LogisticsRouteUnavailable = "error:core:logistics_route_unavailable";
        public const string LogisticsTaskNotFound = "error:core:logistics_task_not_found";
        public const string LogisticsBuildingBusy = "error:core:logistics_building_busy";
        public const string LogisticsConnectorNotFound = "error:core:logistics_connector_not_found";
        public const string LogisticsConnectorDuplicate = "error:core:logistics_connector_duplicate";
        public const string LogisticsConnectorPlacementInvalid = "error:core:logistics_connector_placement_invalid";
        public const string HousingNpcIneligible = "error:core:housing_npc_ineligible";
        public const string HousingUnavailable = "error:core:housing_unavailable";
        public const string HousingAlreadyAssigned = "error:core:housing_already_assigned";
    }

    public interface ICommandHandler
    {
        string CommandType { get; }
        ValidationResult Validate(SimulationContext context, CommandEnvelope command);
        IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command);
    }

    public interface ISimulationSystem
    {
        void RegisterCommands(CommandBus commandBus);
        IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks);
    }

    public sealed class SimulationContext
    {
        public GameState State { get; }
        public DefinitionRegistry Definitions { get; }
        public EventFactory Events { get; }

        public SimulationContext(GameState state, DefinitionRegistry definitions, EventFactory events)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }
    }
}
