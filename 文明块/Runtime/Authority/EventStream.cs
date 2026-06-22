using System;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class EventFactory
    {
        private readonly GameState _state;
        private readonly JsonSerializerOptions _jsonOptions;

        public EventFactory(GameState state, JsonSerializerOptions jsonOptions)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        public GameEvent Create(string eventType, string source, object payload)
        {
            if (!StableId.IsValid(eventType))
            {
                throw new ArgumentException("Event type must use namespace:type:id format.", nameof(eventType));
            }

            string eventId = StableId.Create("event", "core", _state.NextEventSequence.ToString("D12")).ToString();
            _state.NextEventSequence++;

            return new GameEvent
            {
                EventId = eventId,
                EventType = eventType,
                Source = source,
                SimulationTick = _state.SimulationTick,
                Version = 1,
                Payload = JsonSerializer.SerializeToElement(payload, _jsonOptions)
            };
        }
    }

    public sealed class EventStream
    {
        public event Action<GameEvent> EventEmitted;

        public void Publish(GameState state, GameEvent gameEvent)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (gameEvent == null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            state.Events.Events.Add(gameEvent);
            Notify(gameEvent);
        }

        public void Notify(GameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            EventEmitted?.Invoke(gameEvent);
        }
    }
}
