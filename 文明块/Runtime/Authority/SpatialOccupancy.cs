using System;
using System.Collections.Generic;

namespace WenMingBlocks.Runtime.Authority
{
    public readonly struct SpatialGridCell : IEquatable<SpatialGridCell>
    {
        public int X { get; }
        public int Y { get; }
        public int Layer { get; }

        public SpatialGridCell(int x, int y, int layer)
        {
            X = x;
            Y = y;
            Layer = layer;
        }

        public bool Equals(SpatialGridCell other)
        {
            return X == other.X && Y == other.Y && Layer == other.Layer;
        }

        public override bool Equals(object obj)
        {
            return obj is SpatialGridCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + X;
                hash = (hash * 31) + Y;
                hash = (hash * 31) + Layer;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({X},{Y},{Layer})";
        }
    }

    public sealed class SpatialBounds
    {
        public int MinX { get; }
        public int MinY { get; }
        public int MinLayer { get; }
        public int Width { get; }
        public int Depth { get; }
        public int Height { get; }

        public SpatialBounds(int minX, int minY, int minLayer, int width, int depth, int height)
        {
            if (width <= 0 || depth <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Spatial bounds dimensions must be positive.");
            }

            MinX = minX;
            MinY = minY;
            MinLayer = minLayer;
            Width = width;
            Depth = depth;
            Height = height;
        }

        public bool Contains(SpatialGridCell cell)
        {
            return (long)cell.X >= MinX && (long)cell.X < (long)MinX + Width &&
                (long)cell.Y >= MinY && (long)cell.Y < (long)MinY + Depth &&
                (long)cell.Layer >= MinLayer && (long)cell.Layer < (long)MinLayer + Height;
        }
    }

    public sealed class SpatialPlacement
    {
        public string ObjectId { get; }
        public int AnchorX { get; }
        public int AnchorY { get; }
        public int BaseLayer { get; }
        public int Width { get; }
        public int Depth { get; }
        public int Height { get; }
        public int RotationQuarterTurns { get; }

        public SpatialPlacement(
            string objectId,
            int anchorX,
            int anchorY,
            int baseLayer,
            int width,
            int depth,
            int height,
            int rotationQuarterTurns)
        {
            ObjectId = objectId ?? string.Empty;
            AnchorX = anchorX;
            AnchorY = anchorY;
            BaseLayer = baseLayer;
            Width = width;
            Depth = depth;
            Height = height;
            RotationQuarterTurns = rotationQuarterTurns;
        }
    }

    public enum SpatialPlacementIssueCode
    {
        None,
        MissingObjectId,
        InvalidDimensions,
        InvalidRotation,
        FootprintTooLarge,
        CoordinateOverflow,
        OutOfBounds,
        Overlap
    }

    public sealed class SpatialPlacementResult
    {
        public bool Accepted { get; }
        public SpatialPlacementIssueCode Code { get; }
        public string Reason { get; }
        public SpatialGridCell? Cell { get; }
        public string ConflictingObjectId { get; }

        private SpatialPlacementResult(
            bool accepted,
            SpatialPlacementIssueCode code,
            string reason,
            SpatialGridCell? cell,
            string conflictingObjectId)
        {
            Accepted = accepted;
            Code = code;
            Reason = reason;
            Cell = cell;
            ConflictingObjectId = conflictingObjectId ?? string.Empty;
        }

        public static SpatialPlacementResult Accept()
        {
            return new SpatialPlacementResult(true, SpatialPlacementIssueCode.None, string.Empty, null, string.Empty);
        }

        public static SpatialPlacementResult Reject(
            SpatialPlacementIssueCode code,
            string reason,
            SpatialGridCell? cell = null,
            string conflictingObjectId = "")
        {
            if (code == SpatialPlacementIssueCode.None)
            {
                throw new ArgumentException("Rejected placement must have an issue code.", nameof(code));
            }

            return new SpatialPlacementResult(false, code, reason, cell, conflictingObjectId);
        }
    }

    public static class SpatialOccupancy
    {
        public const int MaxFootprintDimension = 256;
        public const int MaxFootprintCells = 65536;

        public static IReadOnlyList<SpatialGridCell> GetOccupiedCells(SpatialPlacement placement)
        {
            SpatialPlacementResult validation = ValidateShape(placement);
            if (!validation.Accepted)
            {
                throw new ArgumentException(validation.Reason, nameof(placement));
            }

            ResolveRotatedSize(placement, out int rotatedWidth, out int rotatedDepth);
            List<SpatialGridCell> cells = new List<SpatialGridCell>(rotatedWidth * rotatedDepth * placement.Height);
            for (int layerOffset = 0; layerOffset < placement.Height; layerOffset++)
            {
                for (int yOffset = 0; yOffset < rotatedDepth; yOffset++)
                {
                    for (int xOffset = 0; xOffset < rotatedWidth; xOffset++)
                    {
                        cells.Add(new SpatialGridCell(
                            checked(placement.AnchorX + xOffset),
                            checked(placement.AnchorY + yOffset),
                            checked(placement.BaseLayer + layerOffset)));
                    }
                }
            }

            return cells.AsReadOnly();
        }

        public static SpatialPlacementResult ValidatePlacement(
            SpatialPlacement candidate,
            SpatialBounds bounds,
            IEnumerable<SpatialPlacement> existingPlacements)
        {
            if (bounds == null)
            {
                throw new ArgumentNullException(nameof(bounds));
            }

            if (existingPlacements == null)
            {
                throw new ArgumentNullException(nameof(existingPlacements));
            }

            SpatialPlacementResult shapeValidation = ValidateShape(candidate);
            if (!shapeValidation.Accepted)
            {
                return shapeValidation;
            }

            IReadOnlyList<SpatialGridCell> candidateCells;
            try
            {
                candidateCells = GetOccupiedCells(candidate);
            }
            catch (OverflowException)
            {
                return SpatialPlacementResult.Reject(
                    SpatialPlacementIssueCode.CoordinateOverflow,
                    $"Placement {candidate.ObjectId} exceeds the integer coordinate range.");
            }

            for (int i = 0; i < candidateCells.Count; i++)
            {
                if (!bounds.Contains(candidateCells[i]))
                {
                    return SpatialPlacementResult.Reject(
                        SpatialPlacementIssueCode.OutOfBounds,
                        $"Placement {candidate.ObjectId} exceeds spatial bounds at {candidateCells[i]}.",
                        candidateCells[i]);
                }
            }

            Dictionary<SpatialGridCell, string> occupied = BuildExistingIndex(bounds, existingPlacements);
            for (int i = 0; i < candidateCells.Count; i++)
            {
                if (occupied.TryGetValue(candidateCells[i], out string conflictingObjectId))
                {
                    return SpatialPlacementResult.Reject(
                        SpatialPlacementIssueCode.Overlap,
                        $"Placement {candidate.ObjectId} overlaps {conflictingObjectId} at {candidateCells[i]}.",
                        candidateCells[i],
                        conflictingObjectId);
                }
            }

            return SpatialPlacementResult.Accept();
        }

        private static SpatialPlacementResult ValidateShape(SpatialPlacement placement)
        {
            if (placement == null)
            {
                throw new ArgumentNullException(nameof(placement));
            }

            if (string.IsNullOrWhiteSpace(placement.ObjectId))
            {
                return SpatialPlacementResult.Reject(
                    SpatialPlacementIssueCode.MissingObjectId,
                    "Spatial placement object id cannot be empty.");
            }

            if (placement.Width <= 0 || placement.Depth <= 0 || placement.Height <= 0)
            {
                return SpatialPlacementResult.Reject(
                    SpatialPlacementIssueCode.InvalidDimensions,
                    $"Placement {placement.ObjectId} dimensions must be positive.");
            }

            if (placement.RotationQuarterTurns < 0 || placement.RotationQuarterTurns > 3)
            {
                return SpatialPlacementResult.Reject(
                    SpatialPlacementIssueCode.InvalidRotation,
                    $"Placement {placement.ObjectId} rotation must be between 0 and 3 quarter turns.");
            }

            if (placement.Width > MaxFootprintDimension ||
                placement.Depth > MaxFootprintDimension ||
                placement.Height > MaxFootprintDimension)
            {
                return SpatialPlacementResult.Reject(
                    SpatialPlacementIssueCode.FootprintTooLarge,
                    $"Placement {placement.ObjectId} exceeds the maximum footprint dimension.");
            }

            long cellCount = (long)placement.Width * placement.Depth * placement.Height;
            if (cellCount > MaxFootprintCells)
            {
                return SpatialPlacementResult.Reject(
                    SpatialPlacementIssueCode.FootprintTooLarge,
                    $"Placement {placement.ObjectId} exceeds the maximum occupied cell count.");
            }

            return SpatialPlacementResult.Accept();
        }

        private static Dictionary<SpatialGridCell, string> BuildExistingIndex(
            SpatialBounds bounds,
            IEnumerable<SpatialPlacement> existingPlacements)
        {
            Dictionary<SpatialGridCell, string> occupied = new Dictionary<SpatialGridCell, string>();
            foreach (SpatialPlacement existing in existingPlacements)
            {
                SpatialPlacementResult validation = ValidateShape(existing);
                if (!validation.Accepted)
                {
                    throw new InvalidOperationException($"Existing spatial state is invalid: {validation.Reason}");
                }

                IReadOnlyList<SpatialGridCell> cells;
                try
                {
                    cells = GetOccupiedCells(existing);
                }
                catch (OverflowException exception)
                {
                    throw new InvalidOperationException(
                        $"Existing placement {existing.ObjectId} exceeds the integer coordinate range.",
                        exception);
                }

                for (int i = 0; i < cells.Count; i++)
                {
                    if (!bounds.Contains(cells[i]))
                    {
                        throw new InvalidOperationException(
                            $"Existing placement {existing.ObjectId} exceeds spatial bounds at {cells[i]}.");
                    }

                    if (!occupied.TryAdd(cells[i], existing.ObjectId))
                    {
                        throw new InvalidOperationException(
                            $"Existing placements {occupied[cells[i]]} and {existing.ObjectId} overlap at {cells[i]}.");
                    }
                }
            }

            return occupied;
        }

        private static void ResolveRotatedSize(SpatialPlacement placement, out int width, out int depth)
        {
            bool swapsAxes = placement.RotationQuarterTurns == 1 || placement.RotationQuarterTurns == 3;
            width = swapsAxes ? placement.Depth : placement.Width;
            depth = swapsAxes ? placement.Width : placement.Depth;
        }
    }
}
