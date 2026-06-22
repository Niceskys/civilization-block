using System;
using System.Collections.Generic;

namespace WenMingBlocks.Runtime.Authority
{
    public enum GameSessionMode
    {
        Local,
        Server,
        Remote
    }

    public enum GameConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting
    }

    public interface IGameSession
    {
        GameSessionMode Mode { get; }
        GameConnectionState ConnectionState { get; }
        bool CanAdvanceSimulation { get; }
        GameState CurrentState { get; }
        EventStream Events { get; }
        CommandResult SendCommand(CommandEnvelope command);
        IReadOnlyList<GameEvent> Tick(long deltaTicks);
    }

    public sealed class LocalGameSession : IGameSession
    {
        private readonly Simulation _simulation;

        public LocalGameSession(Simulation simulation)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        }

        public GameSessionMode Mode
        {
            get { return GameSessionMode.Local; }
        }

        public GameConnectionState ConnectionState
        {
            get { return GameConnectionState.Connected; }
        }

        public bool CanAdvanceSimulation
        {
            get { return true; }
        }

        public GameState CurrentState
        {
            get { return _simulation.State; }
        }

        public EventStream Events
        {
            get { return _simulation.Events; }
        }

        public CommandResult SendCommand(CommandEnvelope command)
        {
            return _simulation.ExecuteCommand(command);
        }

        public IReadOnlyList<GameEvent> Tick(long deltaTicks)
        {
            return _simulation.Tick(deltaTicks);
        }
    }

    public sealed class ServerGameSession : IGameSession
    {
        private readonly Simulation _simulation;
        private readonly HashSet<string> _authorizedPlayers = new HashSet<string>(StringComparer.Ordinal);

        public ServerGameSession(Simulation simulation, IEnumerable<string> authorizedPlayerIds)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            if (authorizedPlayerIds == null)
            {
                throw new ArgumentNullException(nameof(authorizedPlayerIds));
            }

            foreach (string playerId in authorizedPlayerIds)
            {
                AuthorizePlayer(playerId);
            }
        }

        public GameSessionMode Mode
        {
            get { return GameSessionMode.Server; }
        }

        public GameConnectionState ConnectionState
        {
            get { return GameConnectionState.Connected; }
        }

        public bool CanAdvanceSimulation
        {
            get { return true; }
        }

        public GameState CurrentState
        {
            get { return _simulation.State; }
        }

        public EventStream Events
        {
            get { return _simulation.Events; }
        }

        public void AuthorizePlayer(string playerId)
        {
            if (!StableId.IsValid(playerId))
            {
                throw new ArgumentException("Player id must use namespace:type:id format.", nameof(playerId));
            }

            _authorizedPlayers.Add(playerId);
        }

        public bool RevokePlayer(string playerId)
        {
            return _authorizedPlayers.Remove(playerId);
        }

        public CommandResult SendCommand(CommandEnvelope command)
        {
            if (command == null)
            {
                return CommandResult.Reject("Missing command.");
            }

            if (!_authorizedPlayers.Contains(command.PlayerId))
            {
                return CommandResult.Reject($"Player {command.PlayerId} is not authorized for this session.");
            }

            return _simulation.ExecuteCommand(command);
        }

        public IReadOnlyList<GameEvent> Tick(long deltaTicks)
        {
            return _simulation.Tick(deltaTicks);
        }
    }

    public sealed class SessionTransportResponse
    {
        public GameState State { get; }
        public IReadOnlyList<GameEvent> Events { get; }
        public long EventCursor { get; }
        public CommandResult CommandResult { get; }
        public string AuthoritativeStateHash { get; }
        public bool StateRepairRequired { get; }

        public SessionTransportResponse(
            GameState state,
            IReadOnlyList<GameEvent> events,
            long eventCursor,
            CommandResult commandResult,
            string authoritativeStateHash,
            bool stateRepairRequired)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            EventCursor = eventCursor;
            CommandResult = commandResult;
            AuthoritativeStateHash = string.IsNullOrWhiteSpace(authoritativeStateHash)
                ? throw new ArgumentException("Authoritative state hash cannot be empty.", nameof(authoritativeStateHash))
                : authoritativeStateHash;
            StateRepairRequired = stateRepairRequired;
        }
    }

    public interface IGameSessionTransport
    {
        GameConnectionState ConnectionState { get; }
        CompatibilityReport Handshake(SessionCompatibilityProfile clientProfile);
        CompatibilityReport Reconnect(SessionCompatibilityProfile clientProfile);
        SessionTransportResponse Submit(CommandEnvelope command, long eventCursor, string clientStateHash);
        SessionTransportResponse Synchronize(long eventCursor, string clientStateHash);
        SessionTransportResponse RequestAuthoritativeSnapshot(long eventCursor);
    }

    public sealed class LoopbackGameSessionTransport : IGameSessionTransport
    {
        private readonly ServerGameSession _server;
        private readonly SaveSystem _saveSystem;
        private readonly SessionCompatibilityProfile _serverProfile;
        private GameConnectionState _connectionState = GameConnectionState.Connecting;

        public LoopbackGameSessionTransport(
            ServerGameSession server,
            SessionCompatibilityProfile serverProfile,
            SaveSystem saveSystem = null)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _serverProfile = serverProfile ?? throw new ArgumentNullException(nameof(serverProfile));
            _saveSystem = saveSystem ?? new SaveSystem();
        }

        public GameConnectionState ConnectionState
        {
            get { return _connectionState; }
        }

        public CompatibilityReport Handshake(SessionCompatibilityProfile clientProfile)
        {
            _connectionState = GameConnectionState.Connecting;
            return CompleteHandshake(clientProfile);
        }

        public CompatibilityReport Reconnect(SessionCompatibilityProfile clientProfile)
        {
            if (_connectionState != GameConnectionState.Disconnected)
            {
                throw new InvalidOperationException("Transport can reconnect only from the disconnected state.");
            }

            _connectionState = GameConnectionState.Reconnecting;
            return CompleteHandshake(clientProfile);
        }

        public void Disconnect()
        {
            _connectionState = GameConnectionState.Disconnected;
        }

        private CompatibilityReport CompleteHandshake(SessionCompatibilityProfile clientProfile)
        {
            CompatibilityReport report = ModCompatibility.Compare(_serverProfile, clientProfile);
            _connectionState = report.Compatible
                ? GameConnectionState.Connected
                : GameConnectionState.Disconnected;
            return report;
        }

        public SessionTransportResponse Submit(CommandEnvelope command, long eventCursor, string clientStateHash)
        {
            EnsureConnected();
            bool stateRepairRequired = HasStateDrift(clientStateHash);
            CommandResult result = _server.SendCommand(command);
            return CreateResponse(eventCursor, result, stateRepairRequired);
        }

        public SessionTransportResponse Synchronize(long eventCursor, string clientStateHash)
        {
            EnsureConnected();
            return CreateResponse(eventCursor, null, HasStateDrift(clientStateHash));
        }

        public SessionTransportResponse RequestAuthoritativeSnapshot(long eventCursor)
        {
            EnsureConnected();
            return CreateResponse(eventCursor, null, true);
        }

        private void EnsureConnected()
        {
            if (_connectionState != GameConnectionState.Connected)
            {
                throw new InvalidOperationException("Session transport cannot exchange state before a compatible handshake.");
            }
        }

        private SessionTransportResponse CreateResponse(
            long eventCursor,
            CommandResult serverResult,
            bool stateRepairRequired)
        {
            GameState snapshot = _saveSystem.Deserialize(_saveSystem.Serialize(_server.CurrentState));
            string authoritativeStateHash = StateDiagnostics.CalculateStateHash(snapshot);
            int eventCount = snapshot.Events.Events.Count;
            int startIndex = eventCursor < 0 || eventCursor > eventCount ? 0 : (int)eventCursor;
            List<GameEvent> events = new List<GameEvent>(eventCount - startIndex);
            for (int i = startIndex; i < eventCount; i++)
            {
                events.Add(snapshot.Events.Events[i]);
            }

            CommandResult commandResult = null;
            if (serverResult != null)
            {
                if (!serverResult.Accepted)
                {
                    commandResult = CommandResult.Reject(serverResult.Reason, serverResult.Code);
                }
                else
                {
                    int commandEventCount = serverResult.Events.Count;
                    int commandStart = Math.Max(0, eventCount - commandEventCount);
                    List<GameEvent> commandEvents = new List<GameEvent>(commandEventCount);
                    for (int i = commandStart; i < eventCount; i++)
                    {
                        commandEvents.Add(snapshot.Events.Events[i]);
                    }

                    commandResult = CommandResult.Accept(commandEvents);
                }
            }

            return new SessionTransportResponse(
                snapshot,
                events,
                eventCount,
                commandResult,
                authoritativeStateHash,
                stateRepairRequired);
        }

        private bool HasStateDrift(string clientStateHash)
        {
            return !string.IsNullOrWhiteSpace(clientStateHash) &&
                !StringComparer.Ordinal.Equals(
                    clientStateHash,
                    StateDiagnostics.CalculateStateHash(_server.CurrentState));
        }
    }

    public sealed class RemoteGameSession : IGameSession
    {
        private readonly IGameSessionTransport _transport;
        private readonly SessionCompatibilityProfile _clientProfile;
        private readonly EventStream _events = new EventStream();
        private GameState _currentState;
        private long _eventCursor;

        public bool LastSynchronizationRepaired { get; private set; }
        public int StateRepairCount { get; private set; }
        public int ReconnectCount { get; private set; }

        public RemoteGameSession(
            IGameSessionTransport transport,
            SessionCompatibilityProfile clientProfile)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _clientProfile = clientProfile ?? throw new ArgumentNullException(nameof(clientProfile));
            CompatibilityReport report = _transport.Handshake(_clientProfile);
            if (!report.Compatible)
            {
                throw new SessionCompatibilityException(report);
            }

            Apply(ValidateOrRepair(_transport.Synchronize(0, string.Empty)));
        }

        public GameSessionMode Mode
        {
            get { return GameSessionMode.Remote; }
        }

        public GameConnectionState ConnectionState
        {
            get { return _transport.ConnectionState; }
        }

        public bool CanAdvanceSimulation
        {
            get { return false; }
        }

        public GameState CurrentState
        {
            get { return _currentState; }
        }

        public EventStream Events
        {
            get { return _events; }
        }

        public CommandResult SendCommand(CommandEnvelope command)
        {
            SessionTransportResponse response = ValidateOrRepair(
                _transport.Submit(command, _eventCursor, CalculateCurrentStateHash()));
            Apply(response);
            return response.CommandResult ?? CommandResult.Reject("Transport did not return a command result.");
        }

        public IReadOnlyList<GameEvent> Tick(long deltaTicks)
        {
            SessionTransportResponse response = ValidateOrRepair(
                _transport.Synchronize(_eventCursor, CalculateCurrentStateHash()));
            Apply(response);
            return response.Events;
        }

        public IReadOnlyList<GameEvent> Reconnect()
        {
            if (_transport.ConnectionState != GameConnectionState.Disconnected)
            {
                throw new InvalidOperationException("Remote session can reconnect only while disconnected.");
            }

            CompatibilityReport report = _transport.Reconnect(_clientProfile);
            if (!report.Compatible)
            {
                throw new SessionCompatibilityException(report);
            }

            SessionTransportResponse response = ValidateOrRepair(
                _transport.Synchronize(_eventCursor, CalculateCurrentStateHash()));
            Apply(response);
            ReconnectCount++;
            return response.Events;
        }

        private string CalculateCurrentStateHash()
        {
            return _currentState == null ? string.Empty : StateDiagnostics.CalculateStateHash(_currentState);
        }

        private SessionTransportResponse ValidateOrRepair(SessionTransportResponse response)
        {
            ValidateResponse(response);
            string receivedHash = StateDiagnostics.CalculateStateHash(response.State);
            if (StringComparer.Ordinal.Equals(receivedHash, response.AuthoritativeStateHash))
            {
                return response;
            }

            SessionTransportResponse repair = _transport.RequestAuthoritativeSnapshot(_eventCursor);
            ValidateResponse(repair);
            string repairHash = StateDiagnostics.CalculateStateHash(repair.State);
            if (!StringComparer.Ordinal.Equals(repairHash, repair.AuthoritativeStateHash))
            {
                throw new StateSynchronizationException(
                    "Authoritative snapshot hash mismatch after one repair attempt.");
            }

            return new SessionTransportResponse(
                repair.State,
                repair.Events,
                repair.EventCursor,
                response.CommandResult,
                repair.AuthoritativeStateHash,
                true);
        }

        private static void ValidateResponse(SessionTransportResponse response)
        {
            if (response == null)
            {
                throw new StateSynchronizationException("Transport returned no synchronization response.");
            }
        }

        private void Apply(SessionTransportResponse response)
        {
            _currentState = response.State;
            _eventCursor = response.EventCursor;
            LastSynchronizationRepaired = response.StateRepairRequired;
            if (LastSynchronizationRepaired)
            {
                StateRepairCount++;
            }

            for (int i = 0; i < response.Events.Count; i++)
            {
                _events.Notify(response.Events[i]);
            }
        }
    }

    public sealed class StateSynchronizationException : InvalidOperationException
    {
        public StateSynchronizationException(string message)
            : base(message)
        {
        }
    }
}
