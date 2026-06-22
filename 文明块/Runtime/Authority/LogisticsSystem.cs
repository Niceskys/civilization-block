using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class LogisticsSystem : ISimulationSystem
    {
        public const string CreateTransportCommand = "command:core:create_transport";
        public const string CancelTransportCommand = "command:core:cancel_transport";
        public const string BuildConnectorCommand = "command:core:build_logistics_connector";
        public const string CancelConnectorConstructionCommand = "command:core:cancel_logistics_connector_construction";
        public const string DemolishConnectorCommand = "command:core:demolish_logistics_connector";
        public const string ConfigureConnectorCommand = "command:core:configure_logistics_connector";
        public const string TransportCreatedEvent = "event:core:transport_created";
        public const string TransportCompletedEvent = "event:core:transport_completed";
        public const string TransportCancelledEvent = "event:core:transport_cancelled";
        public const string ConnectorConstructionStartedEvent = "event:core:logistics_connector_construction_started";
        public const string ConnectorConstructionProgressedEvent = "event:core:logistics_connector_construction_progressed";
        public const string ConnectorConstructionCompletedEvent = "event:core:logistics_connector_construction_completed";
        public const string ConnectorConstructionCancelledEvent = "event:core:logistics_connector_construction_cancelled";
        public const string ConnectorConfiguredEvent = "event:core:logistics_connector_configured";
        public const string ConnectorDestroyedEvent = "event:core:logistics_connector_destroyed";

        private readonly JsonSerializerOptions _jsonOptions;

        public LogisticsSystem(JsonSerializerOptions jsonOptions = null)
        {
            _jsonOptions = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
        }

        public void RegisterCommands(CommandBus commandBus)
        {
            commandBus.Register(new CreateTransportHandler(_jsonOptions));
            commandBus.Register(new CancelTransportHandler(_jsonOptions));
            commandBus.Register(new BuildConnectorHandler(_jsonOptions));
            commandBus.Register(new CancelConnectorConstructionHandler(_jsonOptions));
            commandBus.Register(new DemolishConnectorHandler(_jsonOptions));
            commandBus.Register(new ConfigureConnectorHandler(_jsonOptions));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            List<GameEvent> events = new List<GameEvent>();
            ProgressConnectorConstruction(context, deltaTicks, events);
            ReconcileConnectors(context, events);
            List<TransportTaskState> tasks = context.State.Logistics.ActiveTasks.Values
                .Where(task => task != null)
                .OrderBy(task => task.CompletionTick)
                .ThenBy(task => task.TaskId, StringComparer.Ordinal)
                .ToList();

            foreach (TransportTaskState task in tasks)
            {
                if (!AreEndpointsOperational(context.State, task))
                {
                    CancelTask(context, task, "endpoint_unavailable", events);
                    continue;
                }

                if (context.State.SimulationTick >= task.CompletionTick)
                {
                    CompleteTask(context, task, events);
                }
            }

            ScheduleAutomaticTransfers(context, events);

            return events;
        }

        public static ValidationResult ValidateDemolition(GameState state, string buildingId)
        {
            bool busy = state.Logistics.ActiveTasks.Values.Any(task => task != null &&
                ((StringComparer.Ordinal.Equals(task.SourceKind, LogisticsEndpointKinds.Building) &&
                  StringComparer.Ordinal.Equals(task.SourceBuildingId, buildingId)) ||
                 (StringComparer.Ordinal.Equals(task.TargetKind, LogisticsEndpointKinds.Building) &&
                  StringComparer.Ordinal.Equals(task.TargetBuildingId, buildingId))));
            busy = busy || state.Logistics.ConstructionTasks.Values.Any(task => task != null &&
                (StringComparer.Ordinal.Equals(task.LowerBuildingId, buildingId) ||
                 StringComparer.Ordinal.Equals(task.UpperBuildingId, buildingId))) ||
                state.Logistics.Connectors.Values.Any(connector => connector != null && !connector.IsDestroyed &&
                (StringComparer.Ordinal.Equals(connector.LowerBuildingId, buildingId) ||
                 StringComparer.Ordinal.Equals(connector.UpperBuildingId, buildingId)));
            return busy
                ? ValidationResult.Invalid("Building is participating in an active transport task.", CommandErrorCodes.LogisticsBuildingBusy)
                : ValidationResult.Valid();
        }

        public static void HandleBuildingDestroyed(SimulationContext context, string buildingId, List<GameEvent> events)
        {
            List<TransportTaskState> affected = context.State.Logistics.ActiveTasks.Values
                .Where(task => task != null &&
                    (StringComparer.Ordinal.Equals(task.SourceBuildingId, buildingId) ||
                     StringComparer.Ordinal.Equals(task.TargetBuildingId, buildingId)))
                .OrderBy(task => task.TaskId, StringComparer.Ordinal)
                .ToList();
            foreach (TransportTaskState task in affected)
            {
                CancelTask(context, task, "building_destroyed", events);
            }


            List<LogisticsConnectorConstructionState> constructionTasks = context.State.Logistics.ConstructionTasks.Values
                .Where(task => task != null &&
                    (StringComparer.Ordinal.Equals(task.LowerBuildingId, buildingId) ||
                     StringComparer.Ordinal.Equals(task.UpperBuildingId, buildingId)))
                .OrderBy(task => task.TaskId, StringComparer.Ordinal)
                .ToList();
            foreach (LogisticsConnectorConstructionState task in constructionTasks)
            {
                context.State.Logistics.ConstructionTasks.Remove(task.TaskId);
                events.Add(context.Events.Create(ConnectorConstructionCancelledEvent, "system:core:logistics", new ConnectorLifecyclePayload
                {
                    ConnectorId = task.ConnectorId,
                    TaskId = task.TaskId,
                    LowerBuildingId = task.LowerBuildingId,
                    UpperBuildingId = task.UpperBuildingId,
                    ResourceId = task.ResourceId,
                    Reason = "endpoint_destroyed"
                }));
            }

            List<LogisticsConnectorInstanceState> connectors = context.State.Logistics.Connectors.Values
                .Where(connector => connector != null && !connector.IsDestroyed &&
                    (StringComparer.Ordinal.Equals(connector.LowerBuildingId, buildingId) ||
                     StringComparer.Ordinal.Equals(connector.UpperBuildingId, buildingId)))
                .OrderBy(connector => connector.ConnectorId, StringComparer.Ordinal)
                .ToList();
            foreach (LogisticsConnectorInstanceState connector in connectors)
            {
                DestroyConnector(context, connector, "endpoint_destroyed", events);
            }

            List<string> routes = context.State.Logistics.Routes
                .Where(pair => pair.Value != null &&
                    (StringComparer.Ordinal.Equals(pair.Value.FirstBuildingId, buildingId) ||
                     StringComparer.Ordinal.Equals(pair.Value.SecondBuildingId, buildingId)))
                .Select(pair => pair.Key)
                .OrderBy(routeId => routeId, StringComparer.Ordinal)
                .ToList();
            foreach (string routeId in routes)
            {
                context.State.Logistics.Routes.Remove(routeId);
            }
        }

        private static void ProgressConnectorConstruction(
            SimulationContext context,
            long deltaTicks,
            List<GameEvent> events)
        {
            List<LogisticsConnectorConstructionState> tasks = context.State.Logistics.ConstructionTasks.Values
                .Where(task => task != null)
                .OrderBy(task => task.TaskId, StringComparer.Ordinal)
                .ToList();
            foreach (LogisticsConnectorConstructionState task in tasks)
            {
                if (!TryGetConnectorEndpointInstances(context.State, task.LowerBuildingId, task.UpperBuildingId,
                    out BuildingInstanceState lower, out BuildingInstanceState upper) || lower.IsDestroyed || upper.IsDestroyed)
                {
                    context.State.Logistics.ConstructionTasks.Remove(task.TaskId);
                    events.Add(context.Events.Create(ConnectorConstructionCancelledEvent, "system:core:logistics", new ConnectorLifecyclePayload
                    {
                        TaskId = task.TaskId,
                        ConnectorId = task.ConnectorId,
                        LowerBuildingId = task.LowerBuildingId,
                        UpperBuildingId = task.UpperBuildingId,
                        ResourceId = task.ResourceId,
                        Reason = "endpoint_unavailable"
                    }));
                    continue;
                }

                if (!BuildingOperationalRules.CanTransferInventory(lower) || !BuildingOperationalRules.CanTransferInventory(upper))
                {
                    continue;
                }

                task.ProgressTicks = Math.Min(task.RequiredTicks, checked(task.ProgressTicks + deltaTicks));
                events.Add(context.Events.Create(ConnectorConstructionProgressedEvent, "system:core:logistics", new ConnectorProgressPayload
                {
                    TaskId = task.TaskId,
                    ConnectorId = task.ConnectorId,
                    ProgressTicks = task.ProgressTicks,
                    RequiredTicks = task.RequiredTicks
                }));
                if (task.ProgressTicks < task.RequiredTicks)
                {
                    continue;
                }

                LogisticsConnectorDefinition definition = context.Definitions.TryGetLogisticsConnector(
                    task.DefinitionId, out LogisticsConnectorDefinition found) ? found : null;
                if (definition == null)
                {
                    context.State.Logistics.ConstructionTasks.Remove(task.TaskId);
                    events.Add(context.Events.Create(ConnectorConstructionCancelledEvent, "system:core:logistics", new ConnectorLifecyclePayload
                    {
                        TaskId = task.TaskId,
                        ConnectorId = task.ConnectorId,
                        LowerBuildingId = task.LowerBuildingId,
                        UpperBuildingId = task.UpperBuildingId,
                        ResourceId = task.ResourceId,
                        Reason = "definition_missing"
                    }));
                    continue;
                }

                string routeId = StableId.Create("route", "core", GetStableIdValue(task.ConnectorId)).ToString();
                LogisticsConnectorInstanceState connector = new LogisticsConnectorInstanceState
                {
                    ConnectorId = task.ConnectorId,
                    DefinitionId = task.DefinitionId,
                    PlotId = task.PlotId,
                    LowerBuildingId = task.LowerBuildingId,
                    UpperBuildingId = task.UpperBuildingId,
                    ResourceId = task.ResourceId,
                    RouteId = routeId,
                    Durability = definition.MaxDurability,
                    AutoTransferAmount = task.AutoTransferAmount,
                    CompletedTick = context.State.SimulationTick,
                    PaidBuildCost = new Dictionary<string, int>(task.PaidBuildCost, StringComparer.Ordinal)
                };
                context.State.Logistics.Connectors[connector.ConnectorId] = connector;
                context.State.Logistics.Routes[routeId] = new LogisticsRouteState
                {
                    RouteId = routeId,
                    FirstBuildingId = connector.LowerBuildingId,
                    SecondBuildingId = connector.UpperBuildingId,
                    ConnectorId = connector.ConnectorId,
                    ResourceId = connector.ResourceId,
                    IsBidirectional = false
                };
                context.State.Logistics.ConstructionTasks.Remove(task.TaskId);
                events.Add(context.Events.Create(ConnectorConstructionCompletedEvent, "system:core:logistics", new ConnectorLifecyclePayload
                {
                    TaskId = task.TaskId,
                    ConnectorId = connector.ConnectorId,
                    LowerBuildingId = connector.LowerBuildingId,
                    UpperBuildingId = connector.UpperBuildingId,
                    ResourceId = connector.ResourceId,
                    RouteId = routeId
                }));
            }
        }

        private static void ReconcileConnectors(SimulationContext context, List<GameEvent> events)
        {
            List<LogisticsConnectorInstanceState> invalid = context.State.Logistics.Connectors.Values
                .Where(connector => connector != null && !connector.IsDestroyed &&
                    (!TryGetConnectorEndpointInstances(context.State, connector.LowerBuildingId, connector.UpperBuildingId,
                        out BuildingInstanceState lower, out BuildingInstanceState upper) || lower.IsDestroyed || upper.IsDestroyed))
                .OrderBy(connector => connector.ConnectorId, StringComparer.Ordinal)
                .ToList();
            foreach (LogisticsConnectorInstanceState connector in invalid)
            {
                DestroyConnector(context, connector, "endpoint_unavailable", events);
            }
        }

        private static void ScheduleAutomaticTransfers(SimulationContext context, List<GameEvent> events)
        {
            List<LogisticsConnectorInstanceState> connectors = context.State.Logistics.Connectors.Values
                .Where(connector => connector != null && !connector.IsDestroyed && connector.AutoTransferEnabled)
                .OrderBy(connector => connector.ConnectorId, StringComparer.Ordinal)
                .ToList();
            foreach (LogisticsConnectorInstanceState connector in connectors)
            {
                if (!context.State.Logistics.Routes.TryGetValue(connector.RouteId, out LogisticsRouteState route) ||
                    !route.IsEnabled || context.State.Logistics.ActiveTasks.Values.Any(task =>
                        task != null && StringComparer.Ordinal.Equals(task.RouteId, route.RouteId)))
                {
                    continue;
                }

                if (!TryGetConnectorEndpoints(context.State, connector.LowerBuildingId, connector.UpperBuildingId,
                    out BuildingInstanceState lower, out BuildingInstanceState upper) ||
                    !lower.LocalInventory.TryGetValue(connector.ResourceId, out LocalResourceStack source))
                {
                    continue;
                }

                int available = Math.Max(0, source.Amount - source.LockedAmount);
                int free = Math.Max(0, upper.LocalInventoryCapacity -
                    upper.LocalInventory.Values.Sum(stack => stack.Amount) - upper.LocalInventoryReservedAmount);
                int amount = Math.Min(connector.AutoTransferAmount, Math.Min(available, free));
                if (amount <= 0)
                {
                    continue;
                }

                CreateTransportPayload payload = new CreateTransportPayload
                {
                    SourceKind = LogisticsEndpointKinds.Building,
                    SourceBuildingId = lower.BuildingId,
                    TargetKind = LogisticsEndpointKinds.Building,
                    TargetBuildingId = upper.BuildingId,
                    ResourceId = connector.ResourceId,
                    Amount = amount
                };
                ValidationResult validation = ValidateCreate(context, payload, out string routeId, out long completionTick);
                if (validation.IsValid)
                {
                    CreateTransportTask(context, payload, routeId, completionTick, "system:core:logistics", events);
                }
            }
        }

        private static TransportTaskState CreateTransportTask(
            SimulationContext context,
            CreateTransportPayload payload,
            string routeId,
            long completionTick,
            string sourceEventId,
            List<GameEvent> events)
        {
            TransportTaskState task = new TransportTaskState
            {
                TaskId = StableId.Create("transport", "core", context.State.NextTransportTaskSequence.ToString("D12")).ToString(),
                SourceKind = payload.SourceKind,
                SourceBuildingId = payload.SourceBuildingId,
                TargetKind = payload.TargetKind,
                TargetBuildingId = payload.TargetBuildingId,
                ResourceId = payload.ResourceId,
                Amount = payload.Amount,
                CreatedTick = context.State.SimulationTick,
                CompletionTick = completionTick,
                RouteId = routeId
            };
            context.State.NextTransportTaskSequence++;
            LockSource(context.State, task);
            ReserveTarget(context.State, task);
            context.State.Logistics.ActiveTasks[task.TaskId] = task;
            events.Add(context.Events.Create(TransportCreatedEvent, sourceEventId, ToPayload(task, string.Empty)));
            return task;
        }

        private static void DestroyConnector(
            SimulationContext context,
            LogisticsConnectorInstanceState connector,
            string reason,
            List<GameEvent> events)
        {
            List<TransportTaskState> tasks = context.State.Logistics.ActiveTasks.Values
                .Where(task => task != null && StringComparer.Ordinal.Equals(task.RouteId, connector.RouteId))
                .OrderBy(task => task.TaskId, StringComparer.Ordinal)
                .ToList();
            foreach (TransportTaskState task in tasks)
            {
                CancelTask(context, task, "connector_destroyed", events);
            }

            context.State.Logistics.Routes.Remove(connector.RouteId);
            connector.IsDestroyed = true;
            connector.Durability = 0;
            connector.AutoTransferEnabled = false;
            events.Add(context.Events.Create(ConnectorDestroyedEvent, "system:core:logistics", new ConnectorLifecyclePayload
            {
                ConnectorId = connector.ConnectorId,
                LowerBuildingId = connector.LowerBuildingId,
                UpperBuildingId = connector.UpperBuildingId,
                ResourceId = connector.ResourceId,
                RouteId = connector.RouteId,
                Reason = reason
            }));
        }

        private static bool TryGetConnectorEndpoints(
            GameState state,
            string lowerBuildingId,
            string upperBuildingId,
            out BuildingInstanceState lower,
            out BuildingInstanceState upper)
        {
            bool found = TryGetConnectorEndpointInstances(state, lowerBuildingId, upperBuildingId, out lower, out upper);
            return found && BuildingOperationalRules.CanTransferInventory(lower) &&
                   BuildingOperationalRules.CanTransferInventory(upper);
        }

        private static bool TryGetConnectorEndpointInstances(
            GameState state,
            string lowerBuildingId,
            string upperBuildingId,
            out BuildingInstanceState lower,
            out BuildingInstanceState upper)
        {
            lower = null;
            upper = null;
            bool hasLower = state.Buildings.Instances.TryGetValue(lowerBuildingId, out lower);
            bool hasUpper = state.Buildings.Instances.TryGetValue(upperBuildingId, out upper);
            return hasLower && hasUpper;
        }

        private static bool IsValidConnectorPlacement(BuildingInstanceState lower, BuildingInstanceState upper)
        {
            if (lower == null || upper == null ||
                !StringComparer.Ordinal.Equals(lower.PlotId, upper.PlotId) ||
                upper.BaseLayer != lower.BaseLayer + lower.PlacedHeight)
            {
                return false;
            }

            long lowerMaxX = (long)lower.AnchorX + lower.PlacedWidth;
            long lowerMaxY = (long)lower.AnchorY + lower.PlacedDepth;
            long upperMaxX = (long)upper.AnchorX + upper.PlacedWidth;
            long upperMaxY = (long)upper.AnchorY + upper.PlacedDepth;
            return lower.AnchorX < upperMaxX && upper.AnchorX < lowerMaxX &&
                   lower.AnchorY < upperMaxY && upper.AnchorY < lowerMaxY;
        }

        private static string GetStableIdValue(string stableId)
        {
            string[] parts = stableId.Split(':');
            return parts.Length == 3 ? parts[2] : throw new InvalidOperationException("Stable id is malformed.");
        }

        private static void CompleteTask(SimulationContext context, TransportTaskState task, List<GameEvent> events)
        {
            RemoveSource(context.State, task);
            AddTarget(context.State, task);
            context.State.Logistics.ActiveTasks.Remove(task.TaskId);
            events.Add(context.Events.Create(TransportCompletedEvent, "system:core:logistics", ToPayload(task, string.Empty)));
        }

        private static void CancelTask(SimulationContext context, TransportTaskState task, string reason, List<GameEvent> events)
        {
            ReleaseSourceLock(context.State, task);
            ReleaseTargetReservation(context.State, task);
            context.State.Logistics.ActiveTasks.Remove(task.TaskId);
            events.Add(context.Events.Create(TransportCancelledEvent, "system:core:logistics", ToPayload(task, reason)));
        }

        private static bool AreEndpointsOperational(GameState state, TransportTaskState task)
        {
            bool routeAvailable = string.IsNullOrEmpty(task.RouteId) ||
                (state.Logistics.Routes.TryGetValue(task.RouteId, out LogisticsRouteState route) && route.IsEnabled);
            return routeAvailable &&
                   IsEndpointOperational(state, task.SourceKind, task.SourceBuildingId) &&
                   IsEndpointOperational(state, task.TargetKind, task.TargetBuildingId);
        }

        private static bool IsEndpointOperational(GameState state, string kind, string buildingId)
        {
            if (StringComparer.Ordinal.Equals(kind, LogisticsEndpointKinds.Global))
            {
                return true;
            }

            return state.Buildings.Instances.TryGetValue(buildingId, out BuildingInstanceState building) &&
                   BuildingOperationalRules.CanTransferInventory(building);
        }

        private static void LockSource(GameState state, TransportTaskState task)
        {
            if (StringComparer.Ordinal.Equals(task.SourceKind, LogisticsEndpointKinds.Global))
            {
                state.Resources.Items[task.ResourceId].LockedAmount = checked(
                    state.Resources.Items[task.ResourceId].LockedAmount + task.Amount);
                return;
            }

            LocalResourceStack stack = state.Buildings.Instances[task.SourceBuildingId].LocalInventory[task.ResourceId];
            stack.LockedAmount = checked(stack.LockedAmount + task.Amount);
        }

        private static void ReserveTarget(GameState state, TransportTaskState task)
        {
            if (StringComparer.Ordinal.Equals(task.TargetKind, LogisticsEndpointKinds.Global))
            {
                state.Resources.Items[task.ResourceId].IncomingReservedAmount = checked(
                    state.Resources.Items[task.ResourceId].IncomingReservedAmount + task.Amount);
                return;
            }

            BuildingInstanceState building = state.Buildings.Instances[task.TargetBuildingId];
            building.LocalInventoryReservedAmount = checked(building.LocalInventoryReservedAmount + task.Amount);
        }

        private static void RemoveSource(GameState state, TransportTaskState task)
        {
            if (StringComparer.Ordinal.Equals(task.SourceKind, LogisticsEndpointKinds.Global))
            {
                ResourceStack stack = state.Resources.Items[task.ResourceId];
                stack.Amount = checked(stack.Amount - task.Amount);
                stack.LockedAmount = checked(stack.LockedAmount - task.Amount);
                return;
            }

            LocalResourceStack local = state.Buildings.Instances[task.SourceBuildingId].LocalInventory[task.ResourceId];
            local.Amount = checked(local.Amount - task.Amount);
            local.LockedAmount = checked(local.LockedAmount - task.Amount);
        }

        private static void AddTarget(GameState state, TransportTaskState task)
        {
            if (StringComparer.Ordinal.Equals(task.TargetKind, LogisticsEndpointKinds.Global))
            {
                ResourceStack stack = state.Resources.Items[task.ResourceId];
                stack.IncomingReservedAmount = checked(stack.IncomingReservedAmount - task.Amount);
                stack.Amount = checked(stack.Amount + task.Amount);
                return;
            }

            BuildingInstanceState building = state.Buildings.Instances[task.TargetBuildingId];
            building.LocalInventoryReservedAmount = checked(building.LocalInventoryReservedAmount - task.Amount);
            if (!building.LocalInventory.TryGetValue(task.ResourceId, out LocalResourceStack local))
            {
                local = new LocalResourceStack { ResourceId = task.ResourceId };
                building.LocalInventory[task.ResourceId] = local;
            }
            local.Amount = checked(local.Amount + task.Amount);
        }

        private static void ReleaseSourceLock(GameState state, TransportTaskState task)
        {
            if (StringComparer.Ordinal.Equals(task.SourceKind, LogisticsEndpointKinds.Global))
            {
                state.Resources.Items[task.ResourceId].LockedAmount = checked(
                    state.Resources.Items[task.ResourceId].LockedAmount - task.Amount);
                return;
            }

            state.Buildings.Instances[task.SourceBuildingId].LocalInventory[task.ResourceId].LockedAmount = checked(
                state.Buildings.Instances[task.SourceBuildingId].LocalInventory[task.ResourceId].LockedAmount - task.Amount);
        }

        private static void ReleaseTargetReservation(GameState state, TransportTaskState task)
        {
            if (StringComparer.Ordinal.Equals(task.TargetKind, LogisticsEndpointKinds.Global))
            {
                state.Resources.Items[task.ResourceId].IncomingReservedAmount = checked(
                    state.Resources.Items[task.ResourceId].IncomingReservedAmount - task.Amount);
                return;
            }

            BuildingInstanceState building = state.Buildings.Instances[task.TargetBuildingId];
            building.LocalInventoryReservedAmount = checked(building.LocalInventoryReservedAmount - task.Amount);
        }

        private static TransportTaskPayload ToPayload(TransportTaskState task, string reason)
        {
            return new TransportTaskPayload
            {
                TaskId = task.TaskId,
                SourceKind = task.SourceKind,
                SourceBuildingId = task.SourceBuildingId,
                TargetKind = task.TargetKind,
                TargetBuildingId = task.TargetBuildingId,
                ResourceId = task.ResourceId,
                Amount = task.Amount,
                CompletionTick = task.CompletionTick,
                RouteId = task.RouteId,
                Reason = reason
            };
        }

        private static ValidationResult ValidateCreate(
            SimulationContext context,
            CreateTransportPayload payload,
            out string routeId,
            out long completionTick)
        {
            routeId = string.Empty;
            completionTick = context.State.SimulationTick;
            if (payload == null || !LogisticsEndpointKinds.IsKnown(payload.SourceKind) ||
                !LogisticsEndpointKinds.IsKnown(payload.TargetKind) || !StableId.IsValid(payload.ResourceId) || payload.Amount <= 0)
            {
                return ValidationResult.Invalid("Transport payload is invalid.", CommandErrorCodes.InvalidPayload);
            }

            if (!TryResolveEndpoint(context.State, payload.SourceKind, payload.SourceBuildingId, out BuildingInstanceState source) ||
                !TryResolveEndpoint(context.State, payload.TargetKind, payload.TargetBuildingId, out BuildingInstanceState target) ||
                (StringComparer.Ordinal.Equals(payload.SourceKind, payload.TargetKind) &&
                 StringComparer.Ordinal.Equals(payload.SourceBuildingId, payload.TargetBuildingId)))
            {
                return ValidationResult.Invalid("Transport endpoint is invalid.", CommandErrorCodes.LogisticsEndpointInvalid);
            }

            int available = StringComparer.Ordinal.Equals(payload.SourceKind, LogisticsEndpointKinds.Global)
                ? (context.State.Resources.Items.TryGetValue(payload.ResourceId, out ResourceStack global)
                    ? global.Amount - global.LockedAmount : 0)
                : (source.LocalInventory.TryGetValue(payload.ResourceId, out LocalResourceStack local)
                    ? local.Amount - local.LockedAmount : 0);
            if (available < payload.Amount)
            {
                return ValidationResult.Invalid("Transport source does not have enough available resources.",
                    CommandErrorCodes.LogisticsResourceUnavailable);
            }

            int free = StringComparer.Ordinal.Equals(payload.TargetKind, LogisticsEndpointKinds.Global)
                ? (context.State.Resources.Items.TryGetValue(payload.ResourceId, out ResourceStack targetGlobal)
                    ? StorageCapacityRules.ResourceFreeCapacity(context.State.Resources, targetGlobal) : 0)
                : target.LocalInventoryCapacity - target.LocalInventory.Values.Sum(stack => stack.Amount) - target.LocalInventoryReservedAmount;
            if (free < payload.Amount)
            {
                return ValidationResult.Invalid("Transport target does not have enough unreserved capacity.",
                    CommandErrorCodes.LogisticsCapacityUnavailable);
            }

            if (source != null && target != null && source.BaseLayer != target.BaseLayer)
            {
                LogisticsRouteState route = context.State.Logistics.Routes.Values
                    .Where(candidate => RouteAllows(candidate, source.BuildingId, target.BuildingId, payload.ResourceId))
                    .OrderBy(candidate => candidate.RouteId, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (route == null)
                {
                    return ValidationResult.Invalid("Cross-layer transport requires an enabled logistics route.",
                        CommandErrorCodes.LogisticsRouteUnavailable);
                }

                routeId = route.RouteId;
                completionTick = checked(context.State.SimulationTick + Math.Abs((long)source.BaseLayer - target.BaseLayer));
            }

            return ValidationResult.Valid();
        }

        private static bool RouteAllows(
            LogisticsRouteState route,
            string sourceBuildingId,
            string targetBuildingId,
            string resourceId)
        {
            if (route == null || !route.IsEnabled ||
                (!string.IsNullOrEmpty(route.ResourceId) && !StringComparer.Ordinal.Equals(route.ResourceId, resourceId)))
            {
                return false;
            }

            bool forward = StringComparer.Ordinal.Equals(route.FirstBuildingId, sourceBuildingId) &&
                           StringComparer.Ordinal.Equals(route.SecondBuildingId, targetBuildingId);
            bool reverse = route.IsBidirectional &&
                           StringComparer.Ordinal.Equals(route.FirstBuildingId, targetBuildingId) &&
                           StringComparer.Ordinal.Equals(route.SecondBuildingId, sourceBuildingId);
            return forward || reverse;
        }

        private static bool TryResolveEndpoint(
            GameState state,
            string kind,
            string buildingId,
            out BuildingInstanceState building)
        {
            building = null;
            if (StringComparer.Ordinal.Equals(kind, LogisticsEndpointKinds.Global))
            {
                return string.IsNullOrEmpty(buildingId);
            }

            return StableId.IsValid(buildingId) &&
                   state.Buildings.Instances.TryGetValue(buildingId, out building) &&
                   BuildingOperationalRules.CanTransferInventory(building);
        }

        private sealed class CreateTransportHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public CreateTransportHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return CreateTransportCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                CreateTransportPayload payload = Deserialize<CreateTransportPayload>(command.Payload, _options);
                return ValidateCreate(context, payload, out _, out _);
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                CreateTransportPayload payload = Deserialize<CreateTransportPayload>(command.Payload, _options);
                ValidationResult validation = ValidateCreate(context, payload, out string routeId, out long completionTick);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException(validation.Reason);
                }

                List<GameEvent> events = new List<GameEvent>();
                CreateTransportTask(context, payload, routeId, completionTick, command.CommandId, events);
                return events;
            }
        }

        private sealed class CancelTransportHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public CancelTransportHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return CancelTransportCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                CancelTransportPayload payload = Deserialize<CancelTransportPayload>(command.Payload, _options);
                return payload != null && context.State.Logistics.ActiveTasks.ContainsKey(payload.TaskId)
                    ? ValidationResult.Valid()
                    : ValidationResult.Invalid("Transport task was not found.", CommandErrorCodes.LogisticsTaskNotFound);
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                CancelTransportPayload payload = Deserialize<CancelTransportPayload>(command.Payload, _options);
                List<GameEvent> events = new List<GameEvent>();
                CancelTask(context, context.State.Logistics.ActiveTasks[payload.TaskId], "player_cancelled", events);
                return events;
            }
        }

        private static ValidationResult ValidateConnectorBuild(
            SimulationContext context,
            BuildConnectorPayload payload,
            out LogisticsConnectorDefinition definition,
            out BuildingInstanceState lower,
            out BuildingInstanceState upper)
        {
            definition = null;
            lower = null;
            upper = null;
            if (payload == null || !StableId.IsValid(payload.DefinitionId) ||
                !StableId.IsValid(payload.LowerBuildingId) || !StableId.IsValid(payload.UpperBuildingId) ||
                !StableId.IsValid(payload.ResourceId) || payload.AutoTransferAmount <= 0)
            {
                return ValidationResult.Invalid("Connector build payload is invalid.", CommandErrorCodes.InvalidPayload);
            }

            if (!context.Definitions.TryGetLogisticsConnector(payload.DefinitionId, out definition))
            {
                return ValidationResult.Invalid("Connector definition was not found.", CommandErrorCodes.LogisticsConnectorNotFound);
            }

            if (!TryGetConnectorEndpoints(context.State, payload.LowerBuildingId, payload.UpperBuildingId, out lower, out upper) ||
                !IsValidConnectorPlacement(lower, upper))
            {
                return ValidationResult.Invalid("Connector endpoints must be overlapping buildings on directly adjacent layers.",
                    CommandErrorCodes.LogisticsConnectorPlacementInvalid);
            }

            bool duplicate = context.State.Logistics.Connectors.Values.Any(connector => connector != null && !connector.IsDestroyed &&
                    StringComparer.Ordinal.Equals(connector.LowerBuildingId, payload.LowerBuildingId) &&
                    StringComparer.Ordinal.Equals(connector.UpperBuildingId, payload.UpperBuildingId)) ||
                context.State.Logistics.ConstructionTasks.Values.Any(task => task != null &&
                    StringComparer.Ordinal.Equals(task.LowerBuildingId, payload.LowerBuildingId) &&
                    StringComparer.Ordinal.Equals(task.UpperBuildingId, payload.UpperBuildingId));
            if (duplicate)
            {
                return ValidationResult.Invalid("A connector already occupies these endpoints.",
                    CommandErrorCodes.LogisticsConnectorDuplicate);
            }

            if (!context.State.Resources.Items.ContainsKey(payload.ResourceId))
            {
                return ValidationResult.Invalid("Selected connector resource is unknown.", CommandErrorCodes.LogisticsResourceUnavailable);
            }

            foreach (KeyValuePair<string, int> cost in definition.BuildCost)
            {
                int available = context.State.Resources.Items.TryGetValue(cost.Key, out ResourceStack stack)
                    ? stack.Amount - stack.LockedAmount
                    : 0;
                if (available < cost.Value)
                {
                    return ValidationResult.Invalid("Connector construction resources are unavailable.",
                        CommandErrorCodes.LogisticsResourceUnavailable);
                }
            }

            return ValidationResult.Valid();
        }

        private sealed class BuildConnectorHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public BuildConnectorHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return BuildConnectorCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                BuildConnectorPayload payload = Deserialize<BuildConnectorPayload>(command.Payload, _options);
                return ValidateConnectorBuild(context, payload, out _, out _, out _);
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                BuildConnectorPayload payload = Deserialize<BuildConnectorPayload>(command.Payload, _options);
                ValidationResult validation = ValidateConnectorBuild(
                    context, payload, out LogisticsConnectorDefinition definition, out BuildingInstanceState lower, out _);
                if (!validation.IsValid) throw new InvalidOperationException(validation.Reason);

                Dictionary<string, int> paid = new Dictionary<string, int>(definition.BuildCost, StringComparer.Ordinal);
                foreach (KeyValuePair<string, int> cost in paid)
                {
                    context.State.Resources.Items[cost.Key].Amount = checked(context.State.Resources.Items[cost.Key].Amount - cost.Value);
                }

                string connectorId = StableId.Create(
                    "connector", "core", context.State.NextConnectorSequence.ToString("D12")).ToString();
                string taskId = StableId.Create(
                    "connectorconstruction", "core", context.State.NextConnectorConstructionSequence.ToString("D12")).ToString();
                context.State.NextConnectorSequence++;
                context.State.NextConnectorConstructionSequence++;
                LogisticsConnectorConstructionState task = new LogisticsConnectorConstructionState
                {
                    TaskId = taskId,
                    ConnectorId = connectorId,
                    DefinitionId = payload.DefinitionId,
                    PlotId = lower.PlotId,
                    LowerBuildingId = payload.LowerBuildingId,
                    UpperBuildingId = payload.UpperBuildingId,
                    ResourceId = payload.ResourceId,
                    AutoTransferAmount = payload.AutoTransferAmount,
                    CreatedTick = context.State.SimulationTick,
                    RequiredTicks = definition.ConstructionTicks,
                    PaidBuildCost = paid
                };
                context.State.Logistics.ConstructionTasks[task.TaskId] = task;
                return new[]
                {
                    context.Events.Create(ConnectorConstructionStartedEvent, command.CommandId, new ConnectorLifecyclePayload
                    {
                        TaskId = task.TaskId,
                        ConnectorId = task.ConnectorId,
                        LowerBuildingId = task.LowerBuildingId,
                        UpperBuildingId = task.UpperBuildingId,
                        ResourceId = task.ResourceId
                    })
                };
            }
        }

        private sealed class CancelConnectorConstructionHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public CancelConnectorConstructionHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return CancelConnectorConstructionCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                CancelConnectorConstructionPayload payload = Deserialize<CancelConnectorConstructionPayload>(command.Payload, _options);
                return context.State.Logistics.ConstructionTasks.ContainsKey(payload.TaskId)
                    ? ValidationResult.Valid()
                    : ValidationResult.Invalid("Connector construction task was not found.", CommandErrorCodes.LogisticsConnectorNotFound);
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                CancelConnectorConstructionPayload payload = Deserialize<CancelConnectorConstructionPayload>(command.Payload, _options);
                LogisticsConnectorConstructionState task = context.State.Logistics.ConstructionTasks[payload.TaskId];
                context.State.Logistics.ConstructionTasks.Remove(task.TaskId);
                return new[]
                {
                    context.Events.Create(ConnectorConstructionCancelledEvent, command.CommandId, new ConnectorLifecyclePayload
                    {
                        TaskId = task.TaskId,
                        ConnectorId = task.ConnectorId,
                        LowerBuildingId = task.LowerBuildingId,
                        UpperBuildingId = task.UpperBuildingId,
                        ResourceId = task.ResourceId,
                        Reason = "player_cancelled"
                    })
                };
            }
        }

        private sealed class DemolishConnectorHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public DemolishConnectorHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return DemolishConnectorCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                ConnectorIdPayload payload = Deserialize<ConnectorIdPayload>(command.Payload, _options);
                return context.State.Logistics.Connectors.TryGetValue(payload.ConnectorId, out LogisticsConnectorInstanceState connector) &&
                    connector != null && !connector.IsDestroyed
                    ? ValidationResult.Valid()
                    : ValidationResult.Invalid("Connector was not found.", CommandErrorCodes.LogisticsConnectorNotFound);
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                ConnectorIdPayload payload = Deserialize<ConnectorIdPayload>(command.Payload, _options);
                List<GameEvent> events = new List<GameEvent>();
                DestroyConnector(context, context.State.Logistics.Connectors[payload.ConnectorId], "player_demolished", events);
                return events;
            }
        }

        private sealed class ConfigureConnectorHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _options;
            public ConfigureConnectorHandler(JsonSerializerOptions options) { _options = options; }
            public string CommandType { get { return ConfigureConnectorCommand; } }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                ConfigureConnectorPayload payload = Deserialize<ConfigureConnectorPayload>(command.Payload, _options);
                if (payload.AutoTransferAmount <= 0)
                {
                    return ValidationResult.Invalid("Auto-transfer amount must be positive.", CommandErrorCodes.InvalidPayload);
                }
                return context.State.Logistics.Connectors.TryGetValue(payload.ConnectorId, out LogisticsConnectorInstanceState connector) &&
                    connector != null && !connector.IsDestroyed
                    ? ValidationResult.Valid()
                    : ValidationResult.Invalid("Connector was not found.", CommandErrorCodes.LogisticsConnectorNotFound);
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                ConfigureConnectorPayload payload = Deserialize<ConfigureConnectorPayload>(command.Payload, _options);
                LogisticsConnectorInstanceState connector = context.State.Logistics.Connectors[payload.ConnectorId];
                connector.AutoTransferEnabled = payload.AutoTransferEnabled;
                connector.AutoTransferAmount = payload.AutoTransferAmount;
                return new[]
                {
                    context.Events.Create(ConnectorConfiguredEvent, command.CommandId, new ConnectorConfiguredPayload
                    {
                        ConnectorId = connector.ConnectorId,
                        AutoTransferEnabled = connector.AutoTransferEnabled,
                        AutoTransferAmount = connector.AutoTransferAmount
                    })
                };
            }
        }

        private static T Deserialize<T>(JsonElement payload, JsonSerializerOptions options)
        {
            T result = payload.Deserialize<T>(options);
            if (result == null) throw new InvalidOperationException("Command payload could not be deserialized.");
            return result;
        }
    }

    public sealed class CreateTransportPayload
    {
        public string SourceKind { get; set; } = LogisticsEndpointKinds.Global;
        public string SourceBuildingId { get; set; } = string.Empty;
        public string TargetKind { get; set; } = LogisticsEndpointKinds.Global;
        public string TargetBuildingId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    public sealed class CancelTransportPayload
    {
        public string TaskId { get; set; } = string.Empty;
    }

    public sealed class BuildConnectorPayload
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string LowerBuildingId { get; set; } = string.Empty;
        public string UpperBuildingId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public int AutoTransferAmount { get; set; } = 1;
    }

    public sealed class CancelConnectorConstructionPayload
    {
        public string TaskId { get; set; } = string.Empty;
    }

    public sealed class ConnectorIdPayload
    {
        public string ConnectorId { get; set; } = string.Empty;
    }

    public sealed class ConfigureConnectorPayload
    {
        public string ConnectorId { get; set; } = string.Empty;
        public bool AutoTransferEnabled { get; set; }
        public int AutoTransferAmount { get; set; } = 1;
    }

    public sealed class ConnectorLifecyclePayload
    {
        public string TaskId { get; set; } = string.Empty;
        public string ConnectorId { get; set; } = string.Empty;
        public string LowerBuildingId { get; set; } = string.Empty;
        public string UpperBuildingId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class ConnectorProgressPayload
    {
        public string TaskId { get; set; } = string.Empty;
        public string ConnectorId { get; set; } = string.Empty;
        public long ProgressTicks { get; set; }
        public long RequiredTicks { get; set; }
    }

    public sealed class ConnectorConfiguredPayload
    {
        public string ConnectorId { get; set; } = string.Empty;
        public bool AutoTransferEnabled { get; set; }
        public int AutoTransferAmount { get; set; }
    }

    public sealed class TransportTaskPayload
    {
        public string TaskId { get; set; } = string.Empty;
        public string SourceKind { get; set; } = string.Empty;
        public string SourceBuildingId { get; set; } = string.Empty;
        public string TargetKind { get; set; } = string.Empty;
        public string TargetBuildingId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public long CompletionTick { get; set; }
        public string RouteId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
