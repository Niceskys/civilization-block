using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class Simulation
    {
        private readonly List<ISimulationSystem> _systems = new List<ISimulationSystem>();
        private readonly HashSet<Type> _systemTypes = new HashSet<Type>();
        private readonly CommandBus _commandBus = new CommandBus();
        private readonly EventStream _eventStream = new EventStream();
        private readonly DefinitionRegistry _definitions;
        private readonly JsonSerializerOptions _jsonOptions;

        public GameState State { get; }
        public EventStream Events
        {
            get { return _eventStream; }
        }
        public int SystemCount
        {
            get { return _systems.Count; }
        }

        public Simulation(GameState initialState, DefinitionRegistry definitions, JsonSerializerOptions jsonOptions = null)
        {
            State = initialState ?? throw new ArgumentNullException(nameof(initialState));
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _definitions.Seal();
            _jsonOptions = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
        }

        public void AddSystem(ISimulationSystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            Type systemType = system.GetType();
            if (!_systemTypes.Add(systemType))
            {
                throw new InvalidOperationException($"Simulation system {systemType.FullName} is already registered.");
            }

            _systems.Add(system);
            system.RegisterCommands(_commandBus);
        }

        public CommandResult ExecuteCommand(CommandEnvelope command)
        {
            SimulationContext context = CreateContext();
            CommandResult result = _commandBus.Execute(context, command);
            if (!result.Accepted)
            {
                return result;
            }

            Publish(result.Events);
            return result;
        }

        public IReadOnlyList<GameEvent> Tick(long deltaTicks)
        {
            if (deltaTicks <= 0)
            {
                return Array.Empty<GameEvent>();
            }

            State.SimulationTick += deltaTicks;
            SimulationContext context = CreateContext();
            List<GameEvent> emitted = new List<GameEvent>();

            for (int i = 0; i < _systems.Count; i++)
            {
                IReadOnlyList<GameEvent> systemEvents = _systems[i].Tick(context, deltaTicks);
                for (int eventIndex = 0; eventIndex < systemEvents.Count; eventIndex++)
                {
                    emitted.Add(systemEvents[eventIndex]);
                }
            }

            Publish(emitted);
            return emitted;
        }

        private SimulationContext CreateContext()
        {
            return new SimulationContext(State, _definitions, new EventFactory(State, _jsonOptions));
        }

        private void Publish(IReadOnlyList<GameEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                _eventStream.Publish(State, events[i]);
            }
        }
    }
}
