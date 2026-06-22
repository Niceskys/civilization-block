using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class CommandBus
    {
        private readonly Dictionary<string, ICommandHandler> _handlers = new Dictionary<string, ICommandHandler>(StringComparer.Ordinal);

        public void Register(ICommandHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!StableId.IsValid(handler.CommandType))
            {
                throw new ArgumentException("Command type must use namespace:type:id format.", nameof(handler));
            }

            _handlers[handler.CommandType] = handler;
        }

        public CommandResult Execute(SimulationContext context, CommandEnvelope command)
        {
            ValidationResult envelopeValidation = ValidateEnvelope(context, command);
            if (!envelopeValidation.IsValid)
            {
                return CommandResult.Reject(envelopeValidation.Reason, envelopeValidation.Code);
            }

            if (!_handlers.TryGetValue(command.Type, out ICommandHandler handler))
            {
                return CommandResult.Reject($"No command handler registered for {command.Type}.", CommandErrorCodes.UnknownHandler);
            }

            ValidationResult validation;
            try
            {
                validation = handler.Validate(context, command);
            }
            catch (JsonException exception)
            {
                return CommandResult.Reject($"Invalid command payload: {exception.Message}", CommandErrorCodes.InvalidPayload);
            }
            catch (InvalidOperationException exception)
            {
                return CommandResult.Reject($"Invalid command payload: {exception.Message}", CommandErrorCodes.InvalidPayload);
            }

            if (!validation.IsValid)
            {
                return CommandResult.Reject(validation.Reason, validation.Code);
            }

            IReadOnlyList<GameEvent> events;
            try
            {
                events = handler.Execute(context, command);
            }
            catch (JsonException exception)
            {
                return CommandResult.Reject($"Invalid command payload: {exception.Message}", CommandErrorCodes.InvalidPayload);
            }
            catch (InvalidOperationException exception)
            {
                return CommandResult.Reject($"Command execution failed: {exception.Message}", CommandErrorCodes.ExecutionFailed);
            }

            context.State.Commands.ProcessedCommandIds.Add(command.CommandId);
            context.State.Commands.LastAcceptedSequenceByPlayer[command.PlayerId] = command.Sequence;
            return CommandResult.Accept(events);
        }

        private static ValidationResult ValidateEnvelope(SimulationContext context, CommandEnvelope command)
        {
            if (context == null)
            {
                return ValidationResult.Invalid("Missing simulation context.");
            }

            if (command == null)
            {
                return ValidationResult.Invalid("Missing command.");
            }

            if (!StableId.IsValid(command.CommandId))
            {
                return ValidationResult.Invalid("Command id must use namespace:type:id format.");
            }

            if (!StableId.IsValid(command.PlayerId))
            {
                return ValidationResult.Invalid("Player id must use namespace:type:id format.");
            }

            if (!StableId.IsValid(command.Type))
            {
                return ValidationResult.Invalid("Command type must use namespace:type:id format.");
            }

            if (context.State.Commands.ProcessedCommandIds.Contains(command.CommandId))
            {
                return ValidationResult.Invalid("Command has already been processed.");
            }

            if (context.State.Commands.LastAcceptedSequenceByPlayer.TryGetValue(command.PlayerId, out long lastSequence) &&
                command.Sequence <= lastSequence)
            {
                return ValidationResult.Invalid("Command sequence must be greater than the last accepted player sequence.");
            }

            return ValidationResult.Valid();
        }
    }
}
