using System;
using System.Collections.Generic;

namespace WenMingBlocks.Runtime.Authority
{
    public static class SpatialPlacementSchema
    {
        public const int CurrentVersion = 1;
    }

    public sealed class GameState
    {
        public string SaveVersion { get; set; } = "2.9";
        public string PlayerId { get; set; } = "player:core:local";
        public long SimulationTick { get; set; }
        public long RngSeed { get; set; }
        public long NextInstanceSequence { get; set; } = 1;
        public long NextEventSequence { get; set; } = 1;
        public long NextConstructionSequence { get; set; } = 1;
        public long NextProductionBatchSequence { get; set; } = 1;
        public long NextTransportTaskSequence { get; set; } = 1;
        public long NextConnectorSequence { get; set; } = 1;
        public long NextConnectorConstructionSequence { get; set; } = 1;
        public WorldState World { get; set; } = new WorldState();
        public ResourceState Resources { get; set; } = new ResourceState();
        public BuildingRuntimeState Buildings { get; set; } = new BuildingRuntimeState();
        public NpcRuntimeState Npcs { get; set; } = new NpcRuntimeState();
        public HousingRuntimeState Housing { get; set; } = new HousingRuntimeState();
        public SurvivalRuntimeState Survival { get; set; } = new SurvivalRuntimeState();
        public WasteRuntimeState Waste { get; set; } = new WasteRuntimeState();
        public ProductionRuntimeState Production { get; set; } = new ProductionRuntimeState();
        public ContinuousProductionRuntimeState ContinuousProduction { get; set; } = new ContinuousProductionRuntimeState();
        public SunlampRuntimeState Sunlamps { get; set; } = new SunlampRuntimeState();
        public LogisticsRuntimeState Logistics { get; set; } = new LogisticsRuntimeState();
        public DifficultyState Difficulty { get; set; } = new DifficultyState();
        public CommandHistoryState Commands { get; set; } = new CommandHistoryState();
        public EventLogState Events { get; set; } = new EventLogState();
    }

    public sealed class WorldState
    {
        public string WorldId { get; set; } = "world:core:default";
        public string Seed { get; set; } = string.Empty;
        public Dictionary<string, PlotState> Plots { get; set; } = new Dictionary<string, PlotState>(StringComparer.Ordinal);
    }

    public sealed class PlotState
    {
        public string PlotId { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 1;
        public int Depth { get; set; } = 1;
        public int MaxStackLayers { get; set; } = 64;
    }

    public sealed class ResourceState
    {
        public int SharedCapacity { get; set; } = StorageCapacityRules.BaseSharedCapacity;
        public Dictionary<string, ResourceStack> Items { get; set; } = new Dictionary<string, ResourceStack>(StringComparer.Ordinal);
    }

    public sealed class ResourceStack
    {
        public string ResourceId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int Capacity { get; set; }
        public int LockedAmount { get; set; }
        public int IncomingReservedAmount { get; set; }
    }

    public sealed class BuildingRuntimeState
    {
        public Dictionary<string, BuildingInstanceState> Instances { get; set; } = new Dictionary<string, BuildingInstanceState>(StringComparer.Ordinal);
        public Dictionary<string, ConstructionTaskState> ConstructionTasks { get; set; } = new Dictionary<string, ConstructionTaskState>(StringComparer.Ordinal);
        public FirstBuildBonusState FirstHomeBonus { get; set; } = new FirstBuildBonusState();
        public FirstBuildBonusState FirstBasicProductionBonus { get; set; } = new FirstBuildBonusState();
        public long NextStructuralCollapseTick { get; set; }
    }

    public sealed class BuildingInstanceState
    {
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
        public int Level { get; set; } = 1;
        public int Durability { get; set; }
        public bool IsDestroyed { get; set; }
        public string StructuralStatus { get; set; } = BuildingStructuralStatuses.Normal;
        public long StructuralGraceDeadlineTick { get; set; }
        public long CompletedTick { get; set; }
        public long ConstructionSequence { get; set; }
        public Dictionary<string, int> PaidBuildCost { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public int LocalInventoryCapacity { get; set; }
        public int LocalInventoryReservedAmount { get; set; }
        public Dictionary<string, LocalResourceStack> LocalInventory { get; set; } = new Dictionary<string, LocalResourceStack>(StringComparer.Ordinal);
    }

    public sealed class LocalResourceStack
    {
        public string ResourceId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int LockedAmount { get; set; }
    }

    public static class BuildingStructuralStatuses
    {
        public const string Normal = "normal";
        public const string Grace = "grace";
        public const string Disabled = "disabled";

        public static bool IsKnown(string status)
        {
            return StringComparer.Ordinal.Equals(status, Normal) ||
                   StringComparer.Ordinal.Equals(status, Grace) ||
                   StringComparer.Ordinal.Equals(status, Disabled);
        }
    }

    public static class BuildingOperationalRules
    {
        public static bool IsOperational(BuildingInstanceState instance)
        {
            return instance != null &&
                   !instance.IsDestroyed &&
                   instance.Durability > 0 &&
                   StringComparer.Ordinal.Equals(instance.StructuralStatus, BuildingStructuralStatuses.Normal);
        }

        public static bool CanAcceptWorkers(BuildingInstanceState instance)
        {
            return IsOperational(instance);
        }

        public static bool CanProduce(BuildingInstanceState instance)
        {
            return IsOperational(instance);
        }

        public static bool CanTransferInventory(BuildingInstanceState instance)
        {
            return IsOperational(instance);
        }
    }

    public sealed class NpcRuntimeState
    {
        public Dictionary<string, NpcInstanceState> Instances { get; set; } = new Dictionary<string, NpcInstanceState>(StringComparer.Ordinal);
        public Dictionary<string, WorkAssignmentState> WorkAssignments { get; set; } = new Dictionary<string, WorkAssignmentState>(StringComparer.Ordinal);
    }

    public sealed class HousingRuntimeState
    {
        public Dictionary<string, HousingAssignmentState> AssignmentsByNpcId { get; set; } =
            new Dictionary<string, HousingAssignmentState>(StringComparer.Ordinal);
        public HashSet<string> HomelessAdultNpcIds { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class HousingAssignmentState
    {
        public string NpcId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public int BedSlotIndex { get; set; }
        public bool IsManual { get; set; }
        public long AssignedTick { get; set; }
    }

    public sealed class SurvivalRuntimeState
    {
        public long NextSettlementTick { get; set; } = GameTime.TicksPerGameDay;
        public long LastSettlementTick { get; set; }
        public int FoodRemainderQuarterUnits { get; set; }
        public int WaterRemainderQuarterUnits { get; set; }
        public int LastFoodRequired { get; set; }
        public int LastFoodConsumed { get; set; }
        public int LastFoodShortage { get; set; }
        public int LastWaterRequired { get; set; }
        public int LastWaterConsumed { get; set; }
        public int LastWaterShortage { get; set; }
        public int ConsecutiveFoodShortageDays { get; set; }
        public int ConsecutiveWaterShortageDays { get; set; }
    }

    public sealed class WasteRuntimeState
    {
        public long NextSettlementTick { get; set; } = GameTime.TicksPerGameDay;
        public long LastSettlementTick { get; set; }
        public int NpcHalfUnitRemainder { get; set; }
        public long TotalGeneratedAmount { get; set; }
        public long TotalDiscardedAmount { get; set; }
        public int AccumulatedSatisfactionPenaltyBasisPoints { get; set; }
        public int DiseaseChanceBonusBasisPoints { get; set; }
        public int LastSettlementGeneratedAmount { get; set; }
        public int LastSettlementDiscardedAmount { get; set; }
        public int LastActiveBuildingCount { get; set; }
        public int LastLivingNpcCount { get; set; }
        public int LastDiseaseExposureCount { get; set; }
        public int LastDiseaseTriggeredCount { get; set; }
        public long TotalDiseaseTriggeredCount { get; set; }
    }

    public sealed class NpcInstanceState
    {
        public string NpcId { get; set; } = string.Empty;
        public string OwnerPlayerId { get; set; } = "player:core:local";
        public long CreationSequence { get; set; }
        public bool IsAlive { get; set; } = true;
        public bool IsAdult { get; set; } = true;
        public bool IsPermanentlyDeparted { get; set; }
        public bool IsUnconscious { get; set; }
        public bool HasUncancellableTaskLock { get; set; }
        public int BaseSatisfactionBasisPoints { get; set; } = 10000;
        public long LifeStageElapsedTicks { get; set; }
        public long AdultLifespanTicks { get; set; }
        public long AdultTransitionTick { get; set; }
        public long DeathTick { get; set; }
    }

    public sealed class WorkAssignmentState
    {
        public string NpcId { get; set; } = string.Empty;
        public string BuildingId { get; set; } = string.Empty;
        public int SlotIndex { get; set; }
        public long AssignedTick { get; set; }
    }

    public static class NpcOperationalRules
    {
        public static bool CanHoldWorkAssignment(NpcInstanceState npc)
        {
            return npc != null &&
                   npc.IsAlive &&
                   npc.IsAdult &&
                   !npc.IsPermanentlyDeparted &&
                   !npc.IsUnconscious;
        }

        public static bool CanReceiveWorkAssignment(NpcInstanceState npc)
        {
            return CanHoldWorkAssignment(npc) && !npc.HasUncancellableTaskLock;
        }
    }

    public sealed class ProductionRuntimeState
    {
        public Dictionary<string, ProductionSlotState> SlotsByBuildingId { get; set; } = new Dictionary<string, ProductionSlotState>(StringComparer.Ordinal);
    }

    public sealed class ContinuousProductionRuntimeState
    {
        public Dictionary<string, ContinuousProductionBuildingState> Buildings { get; set; } =
            new Dictionary<string, ContinuousProductionBuildingState>(StringComparer.Ordinal);
    }

    public sealed class SunlampRuntimeState
    {
        public Dictionary<string, SunlampBuildingState> Buildings { get; set; } =
            new Dictionary<string, SunlampBuildingState>(StringComparer.Ordinal);
    }

    public sealed class SunlampBuildingState
    {
        public string BuildingId { get; set; } = string.Empty;
        public long FuelCoverageTicks { get; set; }
    }

    public sealed class ContinuousProductionBuildingState
    {
        public string BuildingId { get; set; } = string.Empty;
        public long ProgressUnits { get; set; }
        public long InputCoverageTicks { get; set; }
        public int EfficiencyRemainderBasisPointTicks { get; set; }
        public int FertilizerBaseOutputRemaining { get; set; }
        public int FertilizerBonusHalfUnitRemainder { get; set; }
        public int PendingOutputAmount { get; set; }
        public Dictionary<string, long> AdditionalProgressUnits { get; set; } =
            new Dictionary<string, long>(StringComparer.Ordinal);
        public Dictionary<string, int> AdditionalPendingOutputs { get; set; } =
            new Dictionary<string, int>(StringComparer.Ordinal);
        public string Status { get; set; } = ContinuousProductionStatuses.PausedNoWorkers;
    }

    public static class ContinuousProductionStatuses
    {
        public const string Running = "running";
        public const string PausedBuilding = "paused_building";
        public const string PausedNoWorkers = "paused_no_workers";
        public const string PausedNoLight = "paused_no_light";
        public const string PausedInput = "paused_input";
        public const string OutputPending = "output_pending";

        public static bool IsKnown(string status)
        {
            return StringComparer.Ordinal.Equals(status, Running) ||
                   StringComparer.Ordinal.Equals(status, PausedBuilding) ||
                   StringComparer.Ordinal.Equals(status, PausedNoWorkers) ||
                   StringComparer.Ordinal.Equals(status, PausedNoLight) ||
                   StringComparer.Ordinal.Equals(status, PausedInput) ||
                   StringComparer.Ordinal.Equals(status, OutputPending);
        }
    }

    public sealed class ProductionSlotState
    {
        public string BuildingId { get; set; } = string.Empty;
        public string RecipeId { get; set; } = string.Empty;
        public string Status { get; set; } = ProductionSlotStatuses.Unconfigured;
        public bool Continuous { get; set; }
        public string ActiveBatchId { get; set; } = string.Empty;
        public long RequiredWorkTicks { get; set; }
        public long ProgressWorkTicks { get; set; }
        public int EfficiencyRemainderBasisPointTicks { get; set; }
        public Dictionary<string, int> LockedGlobalInputs { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> LockedLocalInputs { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> OutputBuffer { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        public bool HasActiveBatch
        {
            get { return !string.IsNullOrEmpty(ActiveBatchId); }
        }

        public bool HasBufferedOutput
        {
            get { return OutputBuffer.Count > 0; }
        }
    }

    public static class ProductionSlotStatuses
    {
        public const string Unconfigured = "unconfigured";
        public const string Idle = "idle";
        public const string Waiting = "waiting";
        public const string Producing = "producing";
        public const string Paused = "paused";
        public const string OutputPending = "output_pending";

        public static bool IsKnown(string status)
        {
            return StringComparer.Ordinal.Equals(status, Unconfigured) ||
                   StringComparer.Ordinal.Equals(status, Idle) ||
                   StringComparer.Ordinal.Equals(status, Waiting) ||
                   StringComparer.Ordinal.Equals(status, Producing) ||
                   StringComparer.Ordinal.Equals(status, Paused) ||
                   StringComparer.Ordinal.Equals(status, OutputPending);
        }
    }

    public sealed class LogisticsRuntimeState
    {
        public Dictionary<string, TransportTaskState> ActiveTasks { get; set; } = new Dictionary<string, TransportTaskState>(StringComparer.Ordinal);
        public Dictionary<string, LogisticsRouteState> Routes { get; set; } = new Dictionary<string, LogisticsRouteState>(StringComparer.Ordinal);
        public Dictionary<string, LogisticsConnectorConstructionState> ConstructionTasks { get; set; } = new Dictionary<string, LogisticsConnectorConstructionState>(StringComparer.Ordinal);
        public Dictionary<string, LogisticsConnectorInstanceState> Connectors { get; set; } = new Dictionary<string, LogisticsConnectorInstanceState>(StringComparer.Ordinal);
    }

    public sealed class TransportTaskState
    {
        public string TaskId { get; set; } = string.Empty;
        public string SourceKind { get; set; } = LogisticsEndpointKinds.Global;
        public string SourceBuildingId { get; set; } = string.Empty;
        public string TargetKind { get; set; } = LogisticsEndpointKinds.Global;
        public string TargetBuildingId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public long CreatedTick { get; set; }
        public long CompletionTick { get; set; }
        public string RouteId { get; set; } = string.Empty;
    }

    public sealed class LogisticsRouteState
    {
        public string RouteId { get; set; } = string.Empty;
        public string FirstBuildingId { get; set; } = string.Empty;
        public string SecondBuildingId { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string ConnectorId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public bool IsBidirectional { get; set; } = true;
    }

    public sealed class LogisticsConnectorConstructionState
    {
        public string TaskId { get; set; } = string.Empty;
        public string ConnectorId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string PlotId { get; set; } = string.Empty;
        public string LowerBuildingId { get; set; } = string.Empty;
        public string UpperBuildingId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public int AutoTransferAmount { get; set; } = 1;
        public long CreatedTick { get; set; }
        public long RequiredTicks { get; set; }
        public long ProgressTicks { get; set; }
        public Dictionary<string, int> PaidBuildCost { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class LogisticsConnectorInstanceState
    {
        public string ConnectorId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public string PlotId { get; set; } = string.Empty;
        public string LowerBuildingId { get; set; } = string.Empty;
        public string UpperBuildingId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public int Durability { get; set; }
        public bool IsDestroyed { get; set; }
        public bool AutoTransferEnabled { get; set; } = true;
        public int AutoTransferAmount { get; set; } = 1;
        public long CompletedTick { get; set; }
        public Dictionary<string, int> PaidBuildCost { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public static class LogisticsEndpointKinds
    {
        public const string Global = "global";
        public const string Building = "building";

        public static bool IsKnown(string kind)
        {
            return StringComparer.Ordinal.Equals(kind, Global) || StringComparer.Ordinal.Equals(kind, Building);
        }
    }

    public sealed class ConstructionTaskState
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
        public long CreatedTick { get; set; }
        public long RequiredTicks { get; set; }
        public long ProgressTicks { get; set; }
        public long ConstructionSequence { get; set; }
        public bool UsesFirstBuildBonus { get; set; }
        public string FirstBuildBonusKind { get; set; } = string.Empty;
        public bool UsesExtraResourceAcceleration { get; set; }
        public Dictionary<string, int> PaidBuildCost { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class FirstBuildBonusState
    {
        public bool Consumed { get; set; }
        public string ReservedTaskId { get; set; } = string.Empty;
        public string ReservedDefinitionId { get; set; } = string.Empty;

        public bool IsReserved
        {
            get { return !string.IsNullOrEmpty(ReservedTaskId); }
        }
    }

    public sealed class CommandHistoryState
    {
        public HashSet<string> ProcessedCommandIds { get; set; } = new HashSet<string>(StringComparer.Ordinal);
        public Dictionary<string, long> LastAcceptedSequenceByPlayer { get; set; } = new Dictionary<string, long>(StringComparer.Ordinal);
    }

    public sealed class EventLogState
    {
        public List<GameEvent> Events { get; set; } = new List<GameEvent>();
    }
}
