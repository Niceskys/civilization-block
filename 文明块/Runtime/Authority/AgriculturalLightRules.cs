using System;
using System.Collections.Generic;
using System.Linq;

namespace WenMingBlocks.Runtime.Authority
{
    public static class AgriculturalLightRules
    {
        private static readonly int[] HorizontalDx = { 0, 1, -1, 0, 0 };
        private static readonly int[] HorizontalDy = { 0, 0, 0, 1, -1 };

        /// <summary>
        /// Gets the coverage cells for a Lv1 sunlamp.
        /// Vertical coverage layers: BaseLayer, BaseLayer-1, BaseLayer-2 (downward).
        /// Horizontal coverage per layer: (0,0), (+1,0), (-1,0), (0,+1), (0,-1).
        /// Cells below layer 0, outside plot bounds, or at/above MaxStackLayers are ignored.
        /// </summary>
        public static IReadOnlyList<SpatialGridCell> GetSunlampCoverageCells(
            int anchorX, int anchorY, int baseLayer,
            int plotOriginX, int plotOriginY,
            int plotWidth, int plotDepth, int maxStackLayers)
        {
            if (plotWidth <= 0) throw new ArgumentOutOfRangeException(nameof(plotWidth), "Plot width must be positive.");
            if (plotDepth <= 0) throw new ArgumentOutOfRangeException(nameof(plotDepth), "Plot depth must be positive.");
            if (maxStackLayers <= 0) throw new ArgumentOutOfRangeException(nameof(maxStackLayers), "Max stack layers must be positive.");

            long plotOriginXLong = plotOriginX;
            long plotOriginYLong = plotOriginY;
            long maxExclusiveX = plotOriginXLong + (long)plotWidth;
            long maxExclusiveY = plotOriginYLong + (long)plotDepth;

            List<SpatialGridCell> cells = new List<SpatialGridCell>(15);

            for (int layerOffset = 0; layerOffset >= -2; layerOffset--)
            {
                long layerLong = (long)baseLayer + layerOffset;
                if (layerLong < 0) continue;
                if (layerLong >= maxStackLayers) continue;
                int layer = (int)layerLong;

                for (int i = 0; i < 5; i++)
                {
                    long x = (long)anchorX + HorizontalDx[i];
                    long y = (long)anchorY + HorizontalDy[i];

                    if (x < plotOriginXLong || x >= maxExclusiveX) continue;
                    if (y < plotOriginYLong || y >= maxExclusiveY) continue;

                    cells.Add(new SpatialGridCell((int)x, (int)y, layer));
                }
            }

            return cells.AsReadOnly();
        }

        /// <summary>
        /// Gets all footprint cells for the farm base layer.
        /// Uses SpatialOccupancy for authoritative occupied-cell calculation, supporting rotation.
        /// </summary>
        public static IReadOnlyList<SpatialGridCell> GetFarmFootprintCells(
            BuildingInstanceState farm)
        {
            if (farm == null) throw new ArgumentNullException(nameof(farm));
            if (string.IsNullOrEmpty(farm.BuildingId)) throw new ArgumentException("Farm BuildingId cannot be null or empty.", nameof(farm));
            if (farm.PlacedWidth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedWidth), "Farm PlacedWidth must be positive.");
            if (farm.PlacedDepth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedDepth), "Farm PlacedDepth must be positive.");

            SpatialPlacement placement = new SpatialPlacement(
                farm.BuildingId,
                farm.AnchorX,
                farm.AnchorY,
                farm.BaseLayer,
                farm.PlacedWidth,
                farm.PlacedDepth,
                1,
                farm.RotationQuarterTurns);

            return SpatialOccupancy.GetOccupiedCells(placement);
        }

        /// <summary>
        /// Determines whether the farm is physically occluded.
        /// For each farm footprint cell, checks whether the cell directly above
        /// (Farm.BaseLayer + Farm.PlacedHeight) is occupied by a completed,
        /// non-destroyed building in the same plot. Excludes the farm itself by BuildingId.
        /// Destroyed buildings, construction tasks, logistics connectors, and buildings
        /// from different plots do not occlude.
        /// Buildings with StructuralStatus Normal/Grace/Disabled still occlude.
        /// If any footprint cell is occluded, the farm is considered physically occluded.
        /// </summary>
        public static bool IsPhysicallyOccluded(
            BuildingInstanceState farm,
            IEnumerable<BuildingInstanceState> buildings)
        {
            if (farm == null) throw new ArgumentNullException(nameof(farm));
            if (string.IsNullOrEmpty(farm.BuildingId)) throw new ArgumentException("Farm BuildingId cannot be null or empty.", nameof(farm));
            if (farm.PlacedWidth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedWidth));
            if (farm.PlacedDepth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedDepth));
            if (farm.PlacedHeight <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedHeight));

            List<BuildingInstanceState> buildingSnapshot = ValidateAndSnapshotBuildings(buildings);
            return IsPhysicallyOccludedCore(farm, buildingSnapshot);
        }

        /// <summary>
        /// Determines whether the farm has full active sunlamp coverage.
        /// Every farm footprint cell must be covered by at least one active sunlamp.
        /// Sunlamps from different plots are excluded; destroyed sunlamps do not participate.
        /// </summary>
        public static bool HasFullActiveSunlampCoverage(
            BuildingInstanceState farm,
            IEnumerable<BuildingInstanceState> activeSunlamps,
            int plotOriginX, int plotOriginY,
            int plotWidth, int plotDepth, int maxStackLayers)
        {
            if (farm == null) throw new ArgumentNullException(nameof(farm));
            if (string.IsNullOrEmpty(farm.BuildingId)) throw new ArgumentException("Farm BuildingId cannot be null or empty.", nameof(farm));
            if (plotWidth <= 0) throw new ArgumentOutOfRangeException(nameof(plotWidth));
            if (plotDepth <= 0) throw new ArgumentOutOfRangeException(nameof(plotDepth));
            if (maxStackLayers <= 0) throw new ArgumentOutOfRangeException(nameof(maxStackLayers));
            if (farm.PlacedWidth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedWidth));
            if (farm.PlacedDepth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedDepth));

            List<BuildingInstanceState> sunlampSnapshot = ValidateAndSnapshotSunlamps(activeSunlamps);
            return HasFullActiveSunlampCoverageCore(farm, sunlampSnapshot, plotOriginX, plotOriginY, plotWidth, plotDepth, maxStackLayers);
        }

        /// <summary>
        /// Selects a deterministic set of active sunlamps whose combined coverage covers every farm footprint cell.
        /// The method is pure geometry selection and does not consume fuel or mutate state.
        /// </summary>
        public static IReadOnlyList<string> SelectSunlampIdsForFullCoverage(
            BuildingInstanceState farm,
            IEnumerable<BuildingInstanceState> activeSunlamps,
            int plotOriginX, int plotOriginY,
            int plotWidth, int plotDepth, int maxStackLayers)
        {
            if (farm == null) throw new ArgumentNullException(nameof(farm));
            if (string.IsNullOrEmpty(farm.BuildingId)) throw new ArgumentException("Farm BuildingId cannot be null or empty.", nameof(farm));
            if (plotWidth <= 0) throw new ArgumentOutOfRangeException(nameof(plotWidth));
            if (plotDepth <= 0) throw new ArgumentOutOfRangeException(nameof(plotDepth));
            if (maxStackLayers <= 0) throw new ArgumentOutOfRangeException(nameof(maxStackLayers));
            if (farm.PlacedWidth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedWidth));
            if (farm.PlacedDepth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedDepth));

            List<BuildingInstanceState> sunlampSnapshot = ValidateAndSnapshotSunlamps(activeSunlamps);
            return SelectSunlampIdsForFullCoverageCore(
                farm, sunlampSnapshot, plotOriginX, plotOriginY, plotWidth, plotDepth, maxStackLayers);
        }

        /// <summary>
        /// Determines whether the farm has the required light level.
        /// Not occluded: has light.
        /// Occluded and fully covered by active sunlamps: has light.
        /// Occluded but incomplete coverage: no light.
        /// Both collections are fully validated before any short-circuit return.
        /// </summary>
        public static bool HasRequiredLight(
            BuildingInstanceState farm,
            IEnumerable<BuildingInstanceState> buildings,
            IEnumerable<BuildingInstanceState> activeSunlamps,
            int plotOriginX, int plotOriginY,
            int plotWidth, int plotDepth, int maxStackLayers)
        {
            if (farm == null) throw new ArgumentNullException(nameof(farm));
            if (string.IsNullOrEmpty(farm.BuildingId)) throw new ArgumentException("Farm BuildingId cannot be null or empty.", nameof(farm));
            if (plotWidth <= 0) throw new ArgumentOutOfRangeException(nameof(plotWidth));
            if (plotDepth <= 0) throw new ArgumentOutOfRangeException(nameof(plotDepth));
            if (maxStackLayers <= 0) throw new ArgumentOutOfRangeException(nameof(maxStackLayers));
            if (farm.PlacedWidth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedWidth));
            if (farm.PlacedDepth <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedDepth));
            if (farm.PlacedHeight <= 0) throw new ArgumentOutOfRangeException(nameof(farm.PlacedHeight));

            // Snapshot and validate both collections before any short-circuit.
            List<BuildingInstanceState> buildingSnapshot = ValidateAndSnapshotBuildings(buildings);
            List<BuildingInstanceState> sunlampSnapshot = ValidateAndSnapshotSunlamps(activeSunlamps);

            if (!IsPhysicallyOccludedCore(farm, buildingSnapshot))
            {
                return true;
            }

            return HasFullActiveSunlampCoverageCore(
                farm, sunlampSnapshot,
                plotOriginX, plotOriginY,
                plotWidth, plotDepth, maxStackLayers);
        }

        /// <summary>
        /// Validates that the buildings collection is non-null and contains no null entries,
        /// then returns a stable snapshot list preserving original order.
        /// </summary>
        private static List<BuildingInstanceState> ValidateAndSnapshotBuildings(
            IEnumerable<BuildingInstanceState> buildings)
        {
            if (buildings == null) throw new ArgumentNullException(nameof(buildings));
            List<BuildingInstanceState> snapshot = new List<BuildingInstanceState>(buildings);
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i] == null)
                {
                    throw new ArgumentException("Buildings collection contains a null entry.", nameof(buildings));
                }
            }
            return snapshot;
        }

        /// <summary>
        /// Validates that the activeSunlamps collection is non-null and contains no null entries,
        /// then returns a stable snapshot list preserving original order.
        /// </summary>
        private static List<BuildingInstanceState> ValidateAndSnapshotSunlamps(
            IEnumerable<BuildingInstanceState> activeSunlamps)
        {
            if (activeSunlamps == null) throw new ArgumentNullException(nameof(activeSunlamps));
            List<BuildingInstanceState> snapshot = new List<BuildingInstanceState>(activeSunlamps);
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i] == null)
                {
                    throw new ArgumentException("Active sunlamps collection contains a null entry.", nameof(activeSunlamps));
                }
            }
            return snapshot;
        }

        /// <summary>
        /// Core occlusion logic operating on a pre-validated building snapshot.
        /// </summary>
        private static bool IsPhysicallyOccludedCore(
            BuildingInstanceState farm,
            List<BuildingInstanceState> buildings)
        {
            IReadOnlyList<SpatialGridCell> farmCells = GetFarmFootprintCells(farm);

            for (int fc = 0; fc < farmCells.Count; fc++)
            {
                SpatialGridCell farmCell = farmCells[fc];
                int occlusionLayer = checked(farm.BaseLayer + farm.PlacedHeight);
                SpatialGridCell occlusionCell = new SpatialGridCell(
                    farmCell.X, farmCell.Y, occlusionLayer);

                for (int bi = 0; bi < buildings.Count; bi++)
                {
                    BuildingInstanceState building = buildings[bi];
                    if (building.IsDestroyed) continue;
                    if (!string.Equals(building.PlotId, farm.PlotId, StringComparison.Ordinal)) continue;
                    if (string.Equals(building.BuildingId, farm.BuildingId, StringComparison.Ordinal)) continue;

                    SpatialPlacement buildingPlacement = new SpatialPlacement(
                        building.BuildingId,
                        building.AnchorX,
                        building.AnchorY,
                        building.BaseLayer,
                        building.PlacedWidth,
                        building.PlacedDepth,
                        building.PlacedHeight,
                        building.RotationQuarterTurns);

                    IReadOnlyList<SpatialGridCell> buildingCells =
                        SpatialOccupancy.GetOccupiedCells(buildingPlacement);

                    for (int i = 0; i < buildingCells.Count; i++)
                    {
                        if (buildingCells[i].Equals(occlusionCell))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Core coverage logic operating on a pre-validated sunlamp snapshot.
        /// </summary>
        private static bool HasFullActiveSunlampCoverageCore(
            BuildingInstanceState farm,
            List<BuildingInstanceState> activeSunlamps,
            int plotOriginX, int plotOriginY,
            int plotWidth, int plotDepth, int maxStackLayers)
        {
            IReadOnlyList<SpatialGridCell> farmCells = GetFarmFootprintCells(farm);

            for (int fc = 0; fc < farmCells.Count; fc++)
            {
                SpatialGridCell farmCell = farmCells[fc];
                bool isCovered = false;

                for (int si = 0; si < activeSunlamps.Count; si++)
                {
                    BuildingInstanceState sunlamp = activeSunlamps[si];
                    if (!string.Equals(sunlamp.PlotId, farm.PlotId, StringComparison.Ordinal)) continue;
                    if (sunlamp.IsDestroyed) continue;

                    IReadOnlyList<SpatialGridCell> sunlampCells = GetSunlampCoverageCells(
                        sunlamp.AnchorX, sunlamp.AnchorY, sunlamp.BaseLayer,
                        plotOriginX, plotOriginY,
                        plotWidth, plotDepth, maxStackLayers);

                    for (int i = 0; i < sunlampCells.Count; i++)
                    {
                        if (sunlampCells[i].Equals(farmCell))
                        {
                            isCovered = true;
                            break;
                        }
                    }

                    if (isCovered) break;
                }

                if (!isCovered) return false;
            }

            return true;
        }

        private static IReadOnlyList<string> SelectSunlampIdsForFullCoverageCore(
            BuildingInstanceState farm,
            List<BuildingInstanceState> activeSunlamps,
            int plotOriginX, int plotOriginY,
            int plotWidth, int plotDepth, int maxStackLayers)
        {
            HashSet<SpatialGridCell> uncovered = new HashSet<SpatialGridCell>(GetFarmFootprintCells(farm));
            List<string> selectedIds = new List<string>();
            foreach (BuildingInstanceState sunlamp in activeSunlamps
                .Where(candidate => string.Equals(candidate.PlotId, farm.PlotId, StringComparison.Ordinal) &&
                    !candidate.IsDestroyed)
                .OrderBy(candidate => candidate.BuildingId, StringComparer.Ordinal))
            {
                IReadOnlyList<SpatialGridCell> sunlampCells = GetSunlampCoverageCells(
                    sunlamp.AnchorX, sunlamp.AnchorY, sunlamp.BaseLayer,
                    plotOriginX, plotOriginY, plotWidth, plotDepth, maxStackLayers);
                bool used = false;
                for (int i = 0; i < sunlampCells.Count; i++)
                {
                    if (uncovered.Remove(sunlampCells[i]))
                    {
                        used = true;
                    }
                }

                if (used) selectedIds.Add(sunlamp.BuildingId);
                if (uncovered.Count == 0) return selectedIds;
            }

            return Array.Empty<string>();
        }
    }
}
