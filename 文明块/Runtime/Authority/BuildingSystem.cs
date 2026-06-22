using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class BuildingSystem : ISimulationSystem
    {
        public const int DemolitionRefundBasisPoints = 7500;
        public const int RefundBasisPointScale = 10000;
        public const string BuildCommand = "command:core:build";
        public const string CancelConstructionCommand = "command:core:cancel_construction";
        public const string DemolishBuildingCommand = "command:core:demolish_building";
        public const string BuildingPlacedEvent = "event:core:building_placed";
        public const string ConstructionProgressedEvent = "event:core:construction_progressed";
        public const string ConstructionCompletedEvent = "event:core:construction_completed";
        public const string ConstructionCancelledEvent = "event:core:construction_cancelled";
        public const string BuildingDemolishedEvent = "event:core:building_demolished";
        public const string StructuralGraceStartedEvent = "event:core:structural_grace_started";
        public const string StructuralStabilityRestoredEvent = "event:core:structural_stability_restored";
        public const string BuildingCollapsedEvent = "event:core:building_collapsed";
        public const string BuildingStructurallyDisabledEvent = "event:core:building_structurally_disabled";

        private readonly JsonSerializerOptions _jsonOptions;

        public BuildingSystem(JsonSerializerOptions jsonOptions = null)
        {
            _jsonOptions = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
        }

        public void RegisterCommands(CommandBus commandBus)
        {
            commandBus.Register(new BuildCommandHandler(_jsonOptions));
            commandBus.Register(new CancelConstructionCommandHandler(_jsonOptions));
            commandBus.Register(new DemolishBuildingCommandHandler(_jsonOptions));
        }

        public IReadOnlyList<GameEvent> Tick(SimulationContext context, long deltaTicks)
        {
            List<GameEvent> events = new List<GameEvent>();
            List<ConstructionTaskState> completedTasks = new List<ConstructionTaskState>();

            foreach (ConstructionTaskState task in context.State.Buildings.ConstructionTasks.Values)
            {
                task.ProgressTicks = Math.Min(task.RequiredTicks, task.ProgressTicks + deltaTicks);
                events.Add(context.Events.Create(ConstructionProgressedEvent, "system:core:building", new ConstructionProgressedPayload
                {
                    TaskId = task.TaskId,
                    BuildingId = task.BuildingId,
                    ProgressTicks = task.ProgressTicks,
                    RequiredTicks = task.RequiredTicks
                }));

                if (task.ProgressTicks >= task.RequiredTicks)
                {
                    completedTasks.Add(task);
                }
            }

            completedTasks.Sort(CompareConstructionCompletionOrder);
            for (int i = 0; i < completedTasks.Count; i++)
            {
                if (context.State.Buildings.ConstructionTasks.TryGetValue(completedTasks[i].TaskId, out ConstructionTaskState task))
                {
                    CompleteConstruction(context, task, events);
                }
            }

            ReconcileStructuralIncidents(context, events);
            ProcessStructuralFailureDeadlines(context, events);
            HousingSystem.ReconcileAssignments(context, events);
            WorkerAssignmentSystem.ReconcileAssignments(context, events);

            return events;
        }

        private static int CompareConstructionCompletionOrder(ConstructionTaskState left, ConstructionTaskState right)
        {
            int sequenceComparison = left.ConstructionSequence.CompareTo(right.ConstructionSequence);
            if (sequenceComparison != 0)
            {
                return sequenceComparison;
            }

            return StringComparer.Ordinal.Compare(left.TaskId, right.TaskId);
        }

        private static void CompleteConstruction(SimulationContext context, ConstructionTaskState task, List<GameEvent> events)
        {
            BuildingDefinition definition = context.Definitions.TryGetBuilding(task.DefinitionId, out BuildingDefinition found)
                ? found
                : null;

            BuildingInstanceState instance = new BuildingInstanceState
            {
                BuildingId = task.BuildingId,
                DefinitionId = task.DefinitionId,
                PlotId = task.PlotId,
                Layer = task.Layer,
                AnchorX = task.AnchorX,
                AnchorY = task.AnchorY,
                BaseLayer = task.BaseLayer,
                RotationQuarterTurns = task.RotationQuarterTurns,
                PlacedWidth = task.PlacedWidth,
                PlacedDepth = task.PlacedDepth,
                PlacedHeight = task.PlacedHeight,
                PlacementSchemaVersion = task.PlacementSchemaVersion,
                Durability = definition != null ? definition.MaxDurability : 100,
                CompletedTick = context.State.SimulationTick,
                ConstructionSequence = task.ConstructionSequence,
                PaidBuildCost = new Dictionary<string, int>(task.PaidBuildCost, StringComparer.Ordinal),
                LocalInventoryCapacity = definition != null ? definition.LocalInventoryCapacity : 0
            };

            context.State.Buildings.Instances[instance.BuildingId] = instance;
            context.State.Buildings.ConstructionTasks.Remove(task.TaskId);
            if (definition != null)
            {
                StorageCapacityRules.AddBonus(context.State.Resources, definition.GlobalStorageCapacityBonus);
            }

            if (task.UsesFirstBuildBonus)
            {
                FirstBuildBonusState bonus = GetBonusState(context.State, task.FirstBuildBonusKind);
                bonus.Consumed = true;
                bonus.ReservedTaskId = string.Empty;
                bonus.ReservedDefinitionId = string.Empty;
            }

            events.Add(context.Events.Create(ConstructionCompletedEvent, "system:core:building", new ConstructionCompletedPayload
            {
                TaskId = task.TaskId,
                BuildingId = task.BuildingId,
                DefinitionId = task.DefinitionId,
                PlotId = task.PlotId,
                Layer = task.Layer,
                AnchorX = task.AnchorX,
                AnchorY = task.AnchorY,
                BaseLayer = task.BaseLayer,
                RotationQuarterTurns = task.RotationQuarterTurns,
                PlacedWidth = task.PlacedWidth,
                PlacedDepth = task.PlacedDepth,
                PlacedHeight = task.PlacedHeight,
                PlacementSchemaVersion = task.PlacementSchemaVersion,
                UsesFirstBuildBonus = task.UsesFirstBuildBonus,
                FirstBuildBonusKind = task.FirstBuildBonusKind
            }));
        }

        private static FirstBuildBonusState GetBonusState(GameState state, string kind)
        {
            if (StringComparer.Ordinal.Equals(kind, FirstBuildBonusKinds.Home))
            {
                return state.Buildings.FirstHomeBonus;
            }

            return state.Buildings.FirstBasicProductionBonus;
        }

        private sealed class BuildCommandHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _jsonOptions;

            public BuildCommandHandler(JsonSerializerOptions jsonOptions)
            {
                _jsonOptions = jsonOptions;
            }

            public string CommandType
            {
                get { return BuildCommand; }
            }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                BuildCommandPayload payload = Deserialize<BuildCommandPayload>(command.Payload, _jsonOptions);

                if (!StableId.IsValid(payload.DefinitionId))
                {
                    return ValidationResult.Invalid("Building definition id must use namespace:type:id format.");
                }

                if (!StableId.IsValid(payload.PlotId))
                {
                    return ValidationResult.Invalid("Plot id must use namespace:type:id format.");
                }

                if (!context.Definitions.TryGetBuilding(payload.DefinitionId, out BuildingDefinition definition))
                {
                    return ValidationResult.Invalid($"Unknown building definition {payload.DefinitionId}.");
                }

                if (!context.State.World.Plots.TryGetValue(payload.PlotId, out PlotState plot))
                {
                    return ValidationResult.Invalid($"Unknown plot {payload.PlotId}.");
                }

                if (payload.AnchorX.HasValue != payload.AnchorY.HasValue)
                {
                    return ValidationResult.Invalid(
                        "Building anchor X and Y must be provided together.",
                        CommandErrorCodes.SpatialInvalidCoordinates);
                }

                if (plot.Width <= 0 || plot.Depth <= 0 || plot.MaxStackLayers <= 0)
                {
                    return ValidationResult.Invalid("Target plot has invalid spatial bounds.", CommandErrorCodes.SpatialStateInvalid);
                }

                SpatialPlacement placement = ResolveBuildPlacement(payload, plot, definition, command.CommandId);
                SpatialPlacementResult spatialResult;
                try
                {
                    spatialResult = SpatialOccupancy.ValidatePlacement(
                        placement,
                        CreatePlotBounds(plot),
                        GetExistingPlacements(context.State, payload.PlotId));
                }
                catch (InvalidOperationException exception)
                {
                    return ValidationResult.Invalid(
                        $"Authoritative spatial state is invalid: {exception.Message}",
                        CommandErrorCodes.SpatialStateInvalid);
                }

                if (!spatialResult.Accepted)
                {
                    return ValidationResult.Invalid(spatialResult.Reason, MapSpatialErrorCode(spatialResult.Code));
                }

                bool firstBuildBonusWouldApply = WouldUseFirstBuildBonus(context.State, definition);
                bool extraAccelerationWouldApply = payload.UseExtraResourceAcceleration && !firstBuildBonusWouldApply;

                if (!HasEnoughResources(context.State.Resources, definition.BuildCost, extraAccelerationWouldApply))
                {
                    return ValidationResult.Invalid("Not enough resources.");
                }

                if (!TryCreateStructuralNodes(
                    context.State,
                    context.Definitions,
                    payload.PlotId,
                    new StructuralNode(command.CommandId, placement, definition.Weight, definition.CarryCapacity, false),
                    out List<StructuralNode> structuralNodes,
                    out string structuralStateError))
                {
                    return ValidationResult.Invalid(structuralStateError, CommandErrorCodes.StructuralStateInvalid);
                }

                StructuralSupportResult structuralResult = StructuralSupport.Validate(structuralNodes);
                if (!structuralResult.Accepted)
                {
                    return ValidationResult.Invalid(structuralResult.Reason, MapStructuralErrorCode(structuralResult.Code));
                }

                return ValidationResult.Valid();
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                BuildCommandPayload payload = Deserialize<BuildCommandPayload>(command.Payload, _jsonOptions);
                BuildingDefinition definition = context.Definitions.TryGetBuilding(payload.DefinitionId, out BuildingDefinition found)
                    ? found
                    : throw new InvalidOperationException("Definition disappeared between validation and execution.");
                PlotState plot = context.State.World.Plots[payload.PlotId];
                SpatialPlacement placement = ResolveBuildPlacement(payload, plot, definition, command.CommandId);

                bool usesFirstBuildBonus = TryReserveFirstBuildBonus(context.State, definition, out string bonusKind);
                bool usesExtraAcceleration = payload.UseExtraResourceAcceleration && !usesFirstBuildBonus;
                long requiredTicks = ResolveConstructionTicks(definition, usesFirstBuildBonus, usesExtraAcceleration);

                Dictionary<string, int> paidBuildCost = CreatePaidBuildCost(definition.BuildCost, usesExtraAcceleration);
                SpendResources(context.State.Resources, paidBuildCost);

                string buildingId = StableId.Create("building", "core", context.State.NextInstanceSequence.ToString("D12")).ToString();
                context.State.NextInstanceSequence++;

                string taskId = StableId.Create("construction", "core", context.State.NextConstructionSequence.ToString("D12")).ToString();
                long constructionSequence = context.State.NextConstructionSequence;
                context.State.NextConstructionSequence++;

                ConstructionTaskState task = new ConstructionTaskState
                {
                    TaskId = taskId,
                    BuildingId = buildingId,
                    DefinitionId = payload.DefinitionId,
                    PlotId = payload.PlotId,
                    Layer = placement.BaseLayer,
                    AnchorX = placement.AnchorX,
                    AnchorY = placement.AnchorY,
                    BaseLayer = placement.BaseLayer,
                    RotationQuarterTurns = placement.RotationQuarterTurns,
                    PlacedWidth = placement.Width,
                    PlacedDepth = placement.Depth,
                    PlacedHeight = placement.Height,
                    PlacementSchemaVersion = SpatialPlacementSchema.CurrentVersion,
                    CreatedTick = context.State.SimulationTick,
                    RequiredTicks = requiredTicks,
                    ConstructionSequence = constructionSequence,
                    UsesFirstBuildBonus = usesFirstBuildBonus,
                    FirstBuildBonusKind = bonusKind,
                    UsesExtraResourceAcceleration = usesExtraAcceleration,
                    PaidBuildCost = paidBuildCost
                };

                context.State.Buildings.ConstructionTasks[task.TaskId] = task;

                if (usesFirstBuildBonus)
                {
                    FirstBuildBonusState bonus = GetBonusState(context.State, bonusKind);
                    bonus.ReservedTaskId = task.TaskId;
                    bonus.ReservedDefinitionId = task.DefinitionId;
                }

                return new[]
                {
                    context.Events.Create(BuildingPlacedEvent, command.CommandId, new BuildingPlacedPayload
                    {
                        TaskId = task.TaskId,
                        BuildingId = task.BuildingId,
                        DefinitionId = task.DefinitionId,
                        PlotId = task.PlotId,
                        Layer = task.Layer,
                        AnchorX = task.AnchorX,
                        AnchorY = task.AnchorY,
                        BaseLayer = task.BaseLayer,
                        RotationQuarterTurns = task.RotationQuarterTurns,
                        PlacedWidth = task.PlacedWidth,
                        PlacedDepth = task.PlacedDepth,
                        PlacedHeight = task.PlacedHeight,
                        PlacementSchemaVersion = task.PlacementSchemaVersion,
                        RequiredTicks = task.RequiredTicks,
                        UsesFirstBuildBonus = task.UsesFirstBuildBonus,
                        FirstBuildBonusKind = task.FirstBuildBonusKind,
                        UsesExtraResourceAcceleration = task.UsesExtraResourceAcceleration
                    })
                };
            }
        }

        private sealed class CancelConstructionCommandHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _jsonOptions;

            public CancelConstructionCommandHandler(JsonSerializerOptions jsonOptions)
            {
                _jsonOptions = jsonOptions;
            }

            public string CommandType
            {
                get { return CancelConstructionCommand; }
            }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                CancelConstructionPayload payload = Deserialize<CancelConstructionPayload>(command.Payload, _jsonOptions);
                if (!StableId.IsValid(payload.TaskId))
                {
                    return ValidationResult.Invalid("Construction task id must use namespace:type:id format.");
                }

                if (!context.State.Buildings.ConstructionTasks.ContainsKey(payload.TaskId))
                {
                    return ValidationResult.Invalid($"Unknown construction task {payload.TaskId}.");
                }

                return ValidationResult.Valid();
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                CancelConstructionPayload payload = Deserialize<CancelConstructionPayload>(command.Payload, _jsonOptions);
                ConstructionTaskState task = context.State.Buildings.ConstructionTasks[payload.TaskId];
                context.State.Buildings.ConstructionTasks.Remove(task.TaskId);

                if (task.UsesFirstBuildBonus)
                {
                    FirstBuildBonusState bonus = GetBonusState(context.State, task.FirstBuildBonusKind);
                    bonus.ReservedTaskId = string.Empty;
                    bonus.ReservedDefinitionId = string.Empty;
                }

                return new[]
                {
                    context.Events.Create(ConstructionCancelledEvent, command.CommandId, new ConstructionCancelledPayload
                    {
                        TaskId = task.TaskId,
                        BuildingId = task.BuildingId,
                        DefinitionId = task.DefinitionId,
                        RestoresFirstBuildBonus = task.UsesFirstBuildBonus,
                        FirstBuildBonusKind = task.FirstBuildBonusKind
                    })
                };
            }
        }

        private sealed class DemolishBuildingCommandHandler : ICommandHandler
        {
            private readonly JsonSerializerOptions _jsonOptions;

            public DemolishBuildingCommandHandler(JsonSerializerOptions jsonOptions)
            {
                _jsonOptions = jsonOptions;
            }

            public string CommandType
            {
                get { return DemolishBuildingCommand; }
            }

            public ValidationResult Validate(SimulationContext context, CommandEnvelope command)
            {
                DemolishBuildingPayload payload = Deserialize<DemolishBuildingPayload>(command.Payload, _jsonOptions);
                if (!StableId.IsValid(payload.BuildingId))
                {
                    return ValidationResult.Invalid("Building id must use namespace:type:id format.");
                }

                if (!context.State.Buildings.Instances.TryGetValue(payload.BuildingId, out BuildingInstanceState instance))
                {
                    return ValidationResult.Invalid($"Unknown building {payload.BuildingId}.", CommandErrorCodes.BuildingNotFound);
                }

                if (instance.IsDestroyed)
                {
                    return ValidationResult.Invalid($"Building {payload.BuildingId} is already destroyed.", CommandErrorCodes.BuildingAlreadyDestroyed);
                }

                ValidationResult logisticsValidation = LogisticsSystem.ValidateDemolition(context.State, payload.BuildingId);
                if (!logisticsValidation.IsValid)
                {
                    return logisticsValidation;
                }

                ValidationResult productionValidation = ProductionSystem.ValidateDemolition(context.State, payload.BuildingId);
                if (!productionValidation.IsValid)
                {
                    return productionValidation;
                }

                ValidationResult continuousValidation = ContinuousProductionSystem.ValidateDemolition(
                    context.State, payload.BuildingId);
                if (!continuousValidation.IsValid)
                {
                    return continuousValidation;
                }

                if (context.Definitions.TryGetBuilding(instance.DefinitionId, out BuildingDefinition definition) &&
                    !StorageCapacityRules.CanRemoveBonus(
                        context.State.Resources, definition.GlobalStorageCapacityBonus))
                {
                    return ValidationResult.Invalid(
                        $"Building {payload.BuildingId} provides storage capacity still required by global resources.",
                        CommandErrorCodes.StorageCapacityRequired);
                }

                return ValidationResult.Valid();
            }

            public IReadOnlyList<GameEvent> Execute(SimulationContext context, CommandEnvelope command)
            {
                DemolishBuildingPayload payload = Deserialize<DemolishBuildingPayload>(command.Payload, _jsonOptions);
                BuildingInstanceState instance = context.State.Buildings.Instances[payload.BuildingId];
                Dictionary<string, int> refundBasis = ResolveRefundBasis(instance, context.Definitions);
                instance.IsDestroyed = true;
                instance.StructuralStatus = BuildingStructuralStatuses.Normal;
                instance.StructuralGraceDeadlineTick = 0;
                if (context.Definitions.TryGetBuilding(instance.DefinitionId, out BuildingDefinition definition))
                {
                    StorageCapacityRules.RemoveBonus(
                        context.State.Resources, definition.GlobalStorageCapacityBonus);
                }
                ResourceRefundResult refund = RefundResources(context.State.Resources, refundBasis);

                List<GameEvent> events = new List<GameEvent>
                {
                    context.Events.Create(BuildingDemolishedEvent, command.CommandId, new BuildingRemovedPayload
                    {
                        BuildingId = instance.BuildingId,
                        PlotId = instance.PlotId,
                        Cause = "player_demolition",
                        RequestedRefund = refund.Requested,
                        CreditedRefund = refund.Credited,
                        OverflowRefund = refund.Overflow
                    })
                };
                LogisticsSystem.HandleBuildingDestroyed(context, instance.BuildingId, events);
                ProductionSystem.HandleBuildingDestroyed(context, instance.BuildingId, events);
                ContinuousProductionSystem.HandleBuildingDestroyed(context, instance.BuildingId, events);
                ReconcileStructuralIncidents(context, events);
                ProcessStructuralFailureDeadlines(context, events);
                HousingSystem.ReconcileAssignments(context, events);
                WorkerAssignmentSystem.ReconcileAssignments(context, events);
                return events;
            }
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

        private static bool TryReserveFirstBuildBonus(GameState state, BuildingDefinition definition, out string bonusKind)
        {
            bonusKind = string.Empty;

            if (definition.IsHome && !state.Buildings.FirstHomeBonus.Consumed && !state.Buildings.FirstHomeBonus.IsReserved)
            {
                bonusKind = FirstBuildBonusKinds.Home;
                return true;
            }

            if (definition.IsBasicProduction &&
                !state.Buildings.FirstBasicProductionBonus.Consumed &&
                !state.Buildings.FirstBasicProductionBonus.IsReserved)
            {
                bonusKind = FirstBuildBonusKinds.BasicProduction;
                return true;
            }

            return false;
        }

        private static bool WouldUseFirstBuildBonus(GameState state, BuildingDefinition definition)
        {
            return (definition.IsHome && !state.Buildings.FirstHomeBonus.Consumed && !state.Buildings.FirstHomeBonus.IsReserved) ||
                   (definition.IsBasicProduction &&
                    !state.Buildings.FirstBasicProductionBonus.Consumed &&
                    !state.Buildings.FirstBasicProductionBonus.IsReserved);
        }

        private static long ResolveConstructionTicks(BuildingDefinition definition, bool usesFirstBuildBonus, bool usesExtraAcceleration)
        {
            if (usesFirstBuildBonus && definition.FirstBuildBonusTicks > 0)
            {
                return definition.FirstBuildBonusTicks;
            }

            if (usesExtraAcceleration)
            {
                return Math.Max(1, definition.ConstructionTicks / 2);
            }

            return Math.Max(1, definition.ConstructionTicks);
        }

        private static SpatialPlacement ResolveBuildPlacement(
            BuildCommandPayload payload,
            PlotState plot,
            BuildingDefinition definition,
            string objectId)
        {
            return new SpatialPlacement(
                objectId,
                payload.AnchorX ?? plot.X,
                payload.AnchorY ?? plot.Y,
                payload.BaseLayer ?? payload.Layer,
                definition.FootprintWidth,
                definition.FootprintDepth,
                definition.FootprintHeight,
                payload.RotationQuarterTurns);
        }

        private static SpatialBounds CreatePlotBounds(PlotState plot)
        {
            return new SpatialBounds(plot.X, plot.Y, 0, plot.Width, plot.Depth, plot.MaxStackLayers);
        }

        private static IReadOnlyList<SpatialPlacement> GetExistingPlacements(GameState state, string plotId)
        {
            List<SpatialPlacement> placements = new List<SpatialPlacement>();
            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (!instance.IsDestroyed && StringComparer.Ordinal.Equals(instance.PlotId, plotId))
                {
                    placements.Add(new SpatialPlacement(
                        instance.BuildingId,
                        instance.AnchorX,
                        instance.AnchorY,
                        instance.BaseLayer,
                        instance.PlacedWidth,
                        instance.PlacedDepth,
                        instance.PlacedHeight,
                        instance.RotationQuarterTurns));
                }
            }

            foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
            {
                if (StringComparer.Ordinal.Equals(task.PlotId, plotId))
                {
                    placements.Add(new SpatialPlacement(
                        task.TaskId,
                        task.AnchorX,
                        task.AnchorY,
                        task.BaseLayer,
                        task.PlacedWidth,
                        task.PlacedDepth,
                        task.PlacedHeight,
                        task.RotationQuarterTurns));
                }
            }

            placements.Sort((left, right) => StringComparer.Ordinal.Compare(left.ObjectId, right.ObjectId));
            return placements;
        }

        private static string MapSpatialErrorCode(SpatialPlacementIssueCode code)
        {
            switch (code)
            {
                case SpatialPlacementIssueCode.MissingObjectId:
                    return CommandErrorCodes.SpatialInvalidObject;
                case SpatialPlacementIssueCode.InvalidDimensions:
                    return CommandErrorCodes.SpatialInvalidDimensions;
                case SpatialPlacementIssueCode.InvalidRotation:
                    return CommandErrorCodes.SpatialInvalidRotation;
                case SpatialPlacementIssueCode.FootprintTooLarge:
                    return CommandErrorCodes.SpatialFootprintTooLarge;
                case SpatialPlacementIssueCode.CoordinateOverflow:
                    return CommandErrorCodes.SpatialCoordinateOverflow;
                case SpatialPlacementIssueCode.OutOfBounds:
                    return CommandErrorCodes.SpatialOutOfBounds;
                case SpatialPlacementIssueCode.Overlap:
                    return CommandErrorCodes.SpatialOverlap;
                default:
                    return CommandErrorCodes.ValidationFailed;
            }
        }

        private static bool TryCreateStructuralNodes(
            GameState state,
            DefinitionRegistry definitions,
            string plotId,
            StructuralNode candidate,
            out List<StructuralNode> nodes,
            out string error)
        {
            nodes = new List<StructuralNode>();
            error = string.Empty;

            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance.IsDestroyed ||
                    !StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Normal) ||
                    !StringComparer.Ordinal.Equals(instance.PlotId, plotId))
                {
                    continue;
                }

                if (!definitions.TryGetBuilding(instance.DefinitionId, out BuildingDefinition definition))
                {
                    error = $"Building {instance.BuildingId} references missing definition {instance.DefinitionId}.";
                    return false;
                }

                nodes.Add(new StructuralNode(
                    instance.BuildingId,
                    CreateSpatialPlacement(instance),
                    definition.Weight,
                    definition.CarryCapacity,
                    definition.CarryCapacity > 0));
            }

            foreach (ConstructionTaskState task in state.Buildings.ConstructionTasks.Values)
            {
                if (!StringComparer.Ordinal.Equals(task.PlotId, plotId))
                {
                    continue;
                }

                if (!definitions.TryGetBuilding(task.DefinitionId, out BuildingDefinition definition))
                {
                    error = $"Construction task {task.TaskId} references missing definition {task.DefinitionId}.";
                    return false;
                }

                nodes.Add(new StructuralNode(
                    task.TaskId,
                    CreateSpatialPlacement(task),
                    definition.Weight,
                    definition.CarryCapacity,
                    false));
            }

            nodes.Add(candidate);
            return true;
        }

        private static void ReconcileStructuralIncidents(SimulationContext context, List<GameEvent> events)
        {
            StructuralFailurePolicy policy = DifficultyProfiles.ResolveStructuralFailure(context.State.Difficulty);
            Dictionary<string, List<BuildingInstanceState>> instancesByPlot = new Dictionary<string, List<BuildingInstanceState>>(StringComparer.Ordinal);
            foreach (BuildingInstanceState instance in context.State.Buildings.Instances.Values)
            {
                if (instance == null || instance.IsDestroyed)
                {
                    continue;
                }

                string plotId = instance.PlotId ?? string.Empty;
                if (!instancesByPlot.TryGetValue(plotId, out List<BuildingInstanceState> plotInstances))
                {
                    plotInstances = new List<BuildingInstanceState>();
                    instancesByPlot[plotId] = plotInstances;
                }

                plotInstances.Add(instance);
            }

            HashSet<string> unstableIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (List<BuildingInstanceState> plotInstances in instancesByPlot.Values)
            {
                FindUnstableBuildings(plotInstances, context.Definitions, unstableIds);
            }

            List<BuildingInstanceState> ordered = new List<BuildingInstanceState>(context.State.Buildings.Instances.Values);
            ordered.RemoveAll(instance => instance == null || instance.IsDestroyed);
            ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.BuildingId, right.BuildingId));
            for (int i = 0; i < ordered.Count; i++)
            {
                BuildingInstanceState instance = ordered[i];
                if (unstableIds.Contains(instance.BuildingId))
                {
                    if (StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Normal))
                    {
                        instance.StructuralStatus = BuildingStructuralStatuses.Grace;
                        instance.StructuralGraceDeadlineTick = StructuralGraceClock.CreateDeadline(context.State.SimulationTick, policy);
                        events.Add(context.Events.Create(StructuralGraceStartedEvent, "system:core:building", new StructuralStatusChangedPayload
                        {
                            BuildingId = instance.BuildingId,
                            Status = instance.StructuralStatus,
                            DeadlineTick = instance.StructuralGraceDeadlineTick
                        }));
                    }
                }
                else if (!StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Normal))
                {
                    instance.StructuralStatus = BuildingStructuralStatuses.Normal;
                    instance.StructuralGraceDeadlineTick = 0;
                    events.Add(context.Events.Create(StructuralStabilityRestoredEvent, "system:core:building", new StructuralStatusChangedPayload
                    {
                        BuildingId = instance.BuildingId,
                        Status = instance.StructuralStatus
                    }));
                }
            }

            ResetCollapseScheduleIfIdle(context.State);
        }

        private static void FindUnstableBuildings(
            List<BuildingInstanceState> instances,
            DefinitionRegistry definitions,
            HashSet<string> unstableIds)
        {
            List<StructuralNode> remaining = new List<StructuralNode>();
            for (int i = 0; i < instances.Count; i++)
            {
                BuildingInstanceState instance = instances[i];
                if (!definitions.TryGetBuilding(instance.DefinitionId, out BuildingDefinition definition))
                {
                    unstableIds.Add(instance.BuildingId);
                    continue;
                }

                remaining.Add(new StructuralNode(
                    instance.BuildingId,
                    CreateSpatialPlacement(instance),
                    definition.Weight,
                    definition.CarryCapacity,
                    definition.CarryCapacity > 0));
            }

            while (remaining.Count > 0)
            {
                StructuralSupportResult result = StructuralSupport.Validate(remaining);
                if (result.Accepted)
                {
                    return;
                }

                string failedId = result.ObjectId;
                int failedIndex = remaining.FindIndex(node => StringComparer.Ordinal.Equals(node.ObjectId, failedId));
                if (failedIndex < 0)
                {
                    throw new InvalidOperationException($"Structural incident evaluation failed without a removable object: {result.Reason}");
                }

                unstableIds.Add(failedId);
                remaining.RemoveAt(failedIndex);
            }
        }

        private static void ProcessStructuralFailureDeadlines(SimulationContext context, List<GameEvent> events)
        {
            StructuralFailurePolicy policy = DifficultyProfiles.ResolveStructuralFailure(context.State.Difficulty);
            if (StringComparer.Ordinal.Equals(policy.FailureMode, StructuralFailureModes.DisableOnly))
            {
                DisableExpiredBuildings(context, events);
                context.State.Buildings.NextStructuralCollapseTick = 0;
                return;
            }

            long nextTick = ResolveNextCollapseTick(context.State);
            while (nextTick > 0 && nextTick <= context.State.SimulationTick)
            {
                BuildingInstanceState target = SelectCollapseTarget(context.State, nextTick);
                if (target == null)
                {
                    context.State.Buildings.NextStructuralCollapseTick = 0;
                    nextTick = ResolveNextCollapseTick(context.State);
                    continue;
                }

                target.IsDestroyed = true;
                target.StructuralStatus = BuildingStructuralStatuses.Normal;
                target.StructuralGraceDeadlineTick = 0;
                if (context.Definitions.TryGetBuilding(target.DefinitionId, out BuildingDefinition definition))
                {
                    StorageCapacityRules.RemoveBonus(
                        context.State.Resources, definition.GlobalStorageCapacityBonus);
                }
                LogisticsSystem.HandleBuildingDestroyed(context, target.BuildingId, events);
                ProductionSystem.HandleBuildingDestroyed(context, target.BuildingId, events);
                ContinuousProductionSystem.HandleBuildingDestroyed(context, target.BuildingId, events);
                HousingSystem.ReconcileAssignments(context, events);
                events.Add(context.Events.Create(BuildingCollapsedEvent, "system:core:building", new BuildingRemovedPayload
                {
                    BuildingId = target.BuildingId,
                    PlotId = target.PlotId,
                    Cause = "structural_failure"
                }));

                context.State.Buildings.NextStructuralCollapseTick = checked(nextTick + policy.CollapseIntervalTicks);
                ReconcileStructuralIncidents(context, events);
                nextTick = context.State.Buildings.NextStructuralCollapseTick;
            }
        }

        private static void DisableExpiredBuildings(SimulationContext context, List<GameEvent> events)
        {
            List<BuildingInstanceState> expired = new List<BuildingInstanceState>();
            foreach (BuildingInstanceState instance in context.State.Buildings.Instances.Values)
            {
                if (instance != null && !instance.IsDestroyed &&
                    StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Grace) &&
                    StructuralGraceClock.IsExpired(context.State.SimulationTick, instance.StructuralGraceDeadlineTick))
                {
                    expired.Add(instance);
                }
            }

            expired.Sort(CompareCollapseOrder);
            for (int i = 0; i < expired.Count; i++)
            {
                BuildingInstanceState instance = expired[i];
                instance.StructuralStatus = BuildingStructuralStatuses.Disabled;
                events.Add(context.Events.Create(BuildingStructurallyDisabledEvent, "system:core:building", new StructuralStatusChangedPayload
                {
                    BuildingId = instance.BuildingId,
                    Status = instance.StructuralStatus,
                    DeadlineTick = instance.StructuralGraceDeadlineTick
                }));
            }
        }

        private static long ResolveNextCollapseTick(GameState state)
        {
            if (state.Buildings.NextStructuralCollapseTick > 0)
            {
                return state.Buildings.NextStructuralCollapseTick;
            }

            long earliest = 0;
            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance != null && !instance.IsDestroyed &&
                    StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Grace) &&
                    (earliest == 0 || instance.StructuralGraceDeadlineTick < earliest))
                {
                    earliest = instance.StructuralGraceDeadlineTick;
                }
            }

            state.Buildings.NextStructuralCollapseTick = earliest;
            return earliest;
        }

        private static BuildingInstanceState SelectCollapseTarget(GameState state, long dueTick)
        {
            List<BuildingInstanceState> candidates = new List<BuildingInstanceState>();
            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance != null && !instance.IsDestroyed &&
                    StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Grace) &&
                    instance.StructuralGraceDeadlineTick <= dueTick)
                {
                    candidates.Add(instance);
                }
            }

            candidates.Sort(CompareCollapseOrder);
            return candidates.Count == 0 ? null : candidates[0];
        }

        private static int CompareCollapseOrder(BuildingInstanceState left, BuildingInstanceState right)
        {
            int leftTop = checked(left.BaseLayer + left.PlacedHeight - 1);
            int rightTop = checked(right.BaseLayer + right.PlacedHeight - 1);
            int layerComparison = rightTop.CompareTo(leftTop);
            return layerComparison != 0
                ? layerComparison
                : StringComparer.Ordinal.Compare(left.BuildingId, right.BuildingId);
        }

        private static void ResetCollapseScheduleIfIdle(GameState state)
        {
            foreach (BuildingInstanceState instance in state.Buildings.Instances.Values)
            {
                if (instance != null && !instance.IsDestroyed &&
                    StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Grace))
                {
                    return;
                }
            }

            state.Buildings.NextStructuralCollapseTick = 0;
        }

        private static SpatialPlacement CreateSpatialPlacement(BuildingInstanceState instance)
        {
            return new SpatialPlacement(
                instance.BuildingId,
                instance.AnchorX,
                instance.AnchorY,
                instance.BaseLayer,
                instance.PlacedWidth,
                instance.PlacedDepth,
                instance.PlacedHeight,
                instance.RotationQuarterTurns);
        }

        private static SpatialPlacement CreateSpatialPlacement(ConstructionTaskState task)
        {
            return new SpatialPlacement(
                task.TaskId,
                task.AnchorX,
                task.AnchorY,
                task.BaseLayer,
                task.PlacedWidth,
                task.PlacedDepth,
                task.PlacedHeight,
                task.RotationQuarterTurns);
        }

        private static string MapStructuralErrorCode(StructuralSupportIssueCode code)
        {
            switch (code)
            {
                case StructuralSupportIssueCode.Unsupported:
                    return CommandErrorCodes.StructuralUnsupported;
                case StructuralSupportIssueCode.InsufficientContact:
                    return CommandErrorCodes.StructuralInsufficientContact;
                case StructuralSupportIssueCode.CapacityExceeded:
                    return CommandErrorCodes.StructuralCapacityExceeded;
                case StructuralSupportIssueCode.TooManyNodes:
                    return CommandErrorCodes.StructuralLimitExceeded;
                case StructuralSupportIssueCode.ArithmeticOverflow:
                    return CommandErrorCodes.StructuralArithmeticOverflow;
                default:
                    return CommandErrorCodes.StructuralStateInvalid;
            }
        }

        private static bool HasEnoughResources(ResourceState resources, Dictionary<string, int> cost, bool doubleCost)
        {
            foreach (KeyValuePair<string, int> pair in cost)
            {
                long required = (long)pair.Value * (doubleCost ? 2 : 1);
                if (!resources.Items.TryGetValue(pair.Key, out ResourceStack stack) ||
                    (long)stack.Amount - stack.LockedAmount < required)
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, int> CreatePaidBuildCost(Dictionary<string, int> cost, bool doubleCost)
        {
            Dictionary<string, int> paid = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in cost)
            {
                paid[pair.Key] = checked(pair.Value * (doubleCost ? 2 : 1));
            }

            return paid;
        }

        private static void SpendResources(ResourceState resources, Dictionary<string, int> paidCost)
        {
            foreach (KeyValuePair<string, int> pair in paidCost)
            {
                resources.Items[pair.Key].Amount -= pair.Value;
            }
        }

        private static Dictionary<string, int> ResolveRefundBasis(
            BuildingInstanceState instance,
            DefinitionRegistry definitions)
        {
            if (instance.PaidBuildCost != null && instance.PaidBuildCost.Count > 0)
            {
                return instance.PaidBuildCost;
            }

            if (definitions.TryGetBuilding(instance.DefinitionId, out BuildingDefinition definition))
            {
                return definition.BuildCost;
            }

            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        private static ResourceRefundResult RefundResources(ResourceState resources, Dictionary<string, int> paidCost)
        {
            ResourceRefundResult result = new ResourceRefundResult();
            foreach (KeyValuePair<string, int> pair in paidCost)
            {
                int requested = checked((int)(((long)pair.Value * DemolitionRefundBasisPoints) / RefundBasisPointScale));
                int credited = 0;
                if (resources.Items.TryGetValue(pair.Key, out ResourceStack stack))
                {
                    int availableCapacity = StorageCapacityRules.ResourceFreeCapacity(resources, stack);
                    credited = Math.Min(requested, availableCapacity);
                    stack.Amount = checked(stack.Amount + credited);
                }

                result.Requested[pair.Key] = requested;
                result.Credited[pair.Key] = credited;
                result.Overflow[pair.Key] = requested - credited;
            }

            return result;
        }

    }

    public static class FirstBuildBonusKinds
    {
        public const string Home = "home";
        public const string BasicProduction = "basic_production";
    }

    public sealed class BuildCommandPayload
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string PlotId { get; set; } = string.Empty;
        public int Layer { get; set; }
        public int? AnchorX { get; set; }
        public int? AnchorY { get; set; }
        public int? BaseLayer { get; set; }
        public int RotationQuarterTurns { get; set; }
        public bool UseExtraResourceAcceleration { get; set; }
    }

    public sealed class CancelConstructionPayload
    {
        public string TaskId { get; set; } = string.Empty;
    }

    public sealed class DemolishBuildingPayload
    {
        public string BuildingId { get; set; } = string.Empty;
    }

    public sealed class BuildingRemovedPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public string PlotId { get; set; } = string.Empty;
        public string Cause { get; set; } = string.Empty;
        public Dictionary<string, int> RequestedRefund { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> CreditedRefund { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> OverflowRefund { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    internal sealed class ResourceRefundResult
    {
        public Dictionary<string, int> Requested { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> Credited { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> Overflow { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class StructuralStatusChangedPayload
    {
        public string BuildingId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long DeadlineTick { get; set; }
    }

    public sealed class BuildingPlacedPayload
    {
        public string TaskId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string PlotId { get; set; } = string.Empty;
        public int Layer { get; set; }
        public int AnchorX { get; set; }
        public int AnchorY { get; set; }
        public int BaseLayer { get; set; }
        public int RotationQuarterTurns { get; set; }
        public int PlacedWidth { get; set; }
        public int PlacedDepth { get; set; }
        public int PlacedHeight { get; set; }
        public int PlacementSchemaVersion { get; set; }
        public long RequiredTicks { get; set; }
        public bool UsesFirstBuildBonus { get; set; }
        public string FirstBuildBonusKind { get; set; } = string.Empty;
        public bool UsesExtraResourceAcceleration { get; set; }
    }

    public sealed class ConstructionProgressedPayload
    {
        public string TaskId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public long ProgressTicks { get; set; }
        public long RequiredTicks { get; set; }
    }

    public sealed class ConstructionCompletedPayload
    {
        public string TaskId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string PlotId { get; set; } = string.Empty;
        public int Layer { get; set; }
        public int AnchorX { get; set; }
        public int AnchorY { get; set; }
        public int BaseLayer { get; set; }
        public int RotationQuarterTurns { get; set; }
        public int PlacedWidth { get; set; }
        public int PlacedDepth { get; set; }
        public int PlacedHeight { get; set; }
        public int PlacementSchemaVersion { get; set; }
        public bool UsesFirstBuildBonus { get; set; }
        public string FirstBuildBonusKind { get; set; } = string.Empty;
    }

    public sealed class ConstructionCancelledPayload
    {
        public string TaskId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public bool RestoresFirstBuildBonus { get; set; }
        public string FirstBuildBonusKind { get; set; } = string.Empty;
    }
}
