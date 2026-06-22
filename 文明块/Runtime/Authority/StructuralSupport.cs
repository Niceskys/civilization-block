using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class StructuralNode
    {
        public string ObjectId { get; }
        public SpatialPlacement Placement { get; }
        public int Weight { get; }
        public int CarryCapacity { get; }
        public bool CanSupport { get; }

        public StructuralNode(
            string objectId,
            SpatialPlacement placement,
            int weight,
            int carryCapacity,
            bool canSupport)
        {
            ObjectId = objectId ?? string.Empty;
            Placement = placement ?? throw new ArgumentNullException(nameof(placement));
            Weight = weight;
            CarryCapacity = carryCapacity;
            CanSupport = canSupport;
        }
    }

    public enum StructuralSupportIssueCode
    {
        None,
        TooManyNodes,
        DuplicateObjectId,
        InvalidWeight,
        InvalidCapacity,
        SpatialStateInvalid,
        Unsupported,
        InsufficientContact,
        CapacityExceeded,
        ArithmeticOverflow
    }

    public sealed class StructuralSupportEdge
    {
        public string SupportedObjectId { get; }
        public string SupporterObjectId { get; }
        public int ContactCells { get; }
        public long LoadUnits { get; }

        public StructuralSupportEdge(
            string supportedObjectId,
            string supporterObjectId,
            int contactCells,
            long loadUnits)
        {
            SupportedObjectId = supportedObjectId;
            SupporterObjectId = supporterObjectId;
            ContactCells = contactCells;
            LoadUnits = loadUnits;
        }
    }

    public sealed class StructuralSupportResult
    {
        private readonly ReadOnlyCollection<StructuralSupportEdge> _edges;
        private readonly ReadOnlyDictionary<string, long> _receivedLoadUnits;

        public bool Accepted { get; }
        public StructuralSupportIssueCode Code { get; }
        public string Reason { get; }
        public string ObjectId { get; }
        public string SupporterId { get; }
        public int RequiredContactCells { get; }
        public int ActualContactCells { get; }
        public long LoadUnits { get; }
        public long CapacityUnits { get; }
        public IReadOnlyList<StructuralSupportEdge> Edges
        {
            get { return _edges; }
        }

        public IReadOnlyDictionary<string, long> ReceivedLoadUnits
        {
            get { return _receivedLoadUnits; }
        }

        private StructuralSupportResult(
            bool accepted,
            StructuralSupportIssueCode code,
            string reason,
            string objectId,
            string supporterId,
            int requiredContactCells,
            int actualContactCells,
            long loadUnits,
            long capacityUnits,
            IEnumerable<StructuralSupportEdge> edges,
            IDictionary<string, long> receivedLoadUnits)
        {
            Accepted = accepted;
            Code = code;
            Reason = reason ?? string.Empty;
            ObjectId = objectId ?? string.Empty;
            SupporterId = supporterId ?? string.Empty;
            RequiredContactCells = requiredContactCells;
            ActualContactCells = actualContactCells;
            LoadUnits = loadUnits;
            CapacityUnits = capacityUnits;
            _edges = new List<StructuralSupportEdge>(edges ?? Array.Empty<StructuralSupportEdge>()).AsReadOnly();
            _receivedLoadUnits = new ReadOnlyDictionary<string, long>(
                new Dictionary<string, long>(receivedLoadUnits ?? new Dictionary<string, long>(), StringComparer.Ordinal));
        }

        internal static StructuralSupportResult Accept(
            IEnumerable<StructuralSupportEdge> edges,
            IDictionary<string, long> receivedLoadUnits)
        {
            return new StructuralSupportResult(
                true,
                StructuralSupportIssueCode.None,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                0,
                edges,
                receivedLoadUnits);
        }

        internal static StructuralSupportResult Reject(
            StructuralSupportIssueCode code,
            string reason,
            string objectId = "",
            string supporterId = "",
            int requiredContactCells = 0,
            int actualContactCells = 0,
            long loadUnits = 0,
            long capacityUnits = 0)
        {
            return new StructuralSupportResult(
                false,
                code,
                reason,
                objectId,
                supporterId,
                requiredContactCells,
                actualContactCells,
                loadUnits,
                capacityUnits,
                Array.Empty<StructuralSupportEdge>(),
                new Dictionary<string, long>(StringComparer.Ordinal));
        }
    }

    public static class StructuralSupport
    {
        public const int LoadScale = 1000;
        public const int MinimumSupportBasisPoints = 5000;
        public const int BasisPointScale = 10000;
        public const int MaxNodes = 10000;
        public const int MaxTotalOccupiedCells = 1000000;

        public static StructuralSupportResult Validate(IReadOnlyList<StructuralNode> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (nodes.Count > MaxNodes)
            {
                return StructuralSupportResult.Reject(
                    StructuralSupportIssueCode.TooManyNodes,
                    $"Structural graph exceeds the maximum node count of {MaxNodes}.");
            }

            List<StructuralNode> orderedNodes = new List<StructuralNode>(nodes.Count);
            Dictionary<string, StructuralNode> nodesById = new Dictionary<string, StructuralNode>(StringComparer.Ordinal);
            Dictionary<SpatialGridCell, string> occupiedByCell = new Dictionary<SpatialGridCell, string>();
            Dictionary<string, IReadOnlyList<SpatialGridCell>> cellsByNode = new Dictionary<string, IReadOnlyList<SpatialGridCell>>(StringComparer.Ordinal);
            long totalOccupiedCells = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                StructuralNode node = nodes[i];
                StructuralSupportResult nodeValidation = ValidateNode(node);
                if (!nodeValidation.Accepted)
                {
                    return nodeValidation;
                }

                if (!nodesById.TryAdd(node.ObjectId, node))
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.DuplicateObjectId,
                        $"Structural node id {node.ObjectId} is duplicated.",
                        node.ObjectId);
                }

                IReadOnlyList<SpatialGridCell> cells;
                try
                {
                    cells = SpatialOccupancy.GetOccupiedCells(node.Placement);
                }
                catch (Exception exception) when (exception is ArgumentException || exception is OverflowException)
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.SpatialStateInvalid,
                        $"Structural node {node.ObjectId} has invalid placement: {exception.Message}",
                        node.ObjectId);
                }

                totalOccupiedCells += cells.Count;
                if (totalOccupiedCells > MaxTotalOccupiedCells)
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.TooManyNodes,
                        $"Structural graph exceeds the maximum occupied cell count of {MaxTotalOccupiedCells}.",
                        node.ObjectId);
                }

                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                {
                    if (!occupiedByCell.TryAdd(cells[cellIndex], node.ObjectId))
                    {
                        return StructuralSupportResult.Reject(
                            StructuralSupportIssueCode.SpatialStateInvalid,
                            $"Structural nodes {occupiedByCell[cells[cellIndex]]} and {node.ObjectId} overlap at {cells[cellIndex]}.",
                            node.ObjectId,
                            occupiedByCell[cells[cellIndex]]);
                    }
                }

                orderedNodes.Add(node);
                cellsByNode[node.ObjectId] = cells;
            }

            orderedNodes.Sort(ComparePropagationOrder);
            Dictionary<string, long> receivedLoadUnits = orderedNodes.ToDictionary(
                node => node.ObjectId,
                _ => 0L,
                StringComparer.Ordinal);
            List<StructuralSupportEdge> edges = new List<StructuralSupportEdge>();

            for (int i = 0; i < orderedNodes.Count; i++)
            {
                StructuralNode node = orderedNodes[i];
                long capacityUnits;
                long totalLoadUnits;
                try
                {
                    capacityUnits = checked((long)node.CarryCapacity * LoadScale);
                    totalLoadUnits = checked(((long)node.Weight * LoadScale) + receivedLoadUnits[node.ObjectId]);
                }
                catch (OverflowException)
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.ArithmeticOverflow,
                        $"Structural load arithmetic overflowed for {node.ObjectId}.",
                        node.ObjectId);
                }

                if (receivedLoadUnits[node.ObjectId] > capacityUnits)
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.CapacityExceeded,
                        $"Structural node {node.ObjectId} receives {receivedLoadUnits[node.ObjectId]} load units but can carry {capacityUnits}.",
                        node.ObjectId,
                        node.ObjectId,
                        loadUnits: receivedLoadUnits[node.ObjectId],
                        capacityUnits: capacityUnits);
                }

                if (node.Placement.BaseLayer == 0)
                {
                    continue;
                }

                if (node.Placement.BaseLayer < 0)
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.SpatialStateInvalid,
                        $"Structural node {node.ObjectId} cannot be placed below the ground layer.",
                        node.ObjectId);
                }

                Dictionary<string, int> contactCellsBySupporter = FindSupportContacts(
                    node,
                    cellsByNode[node.ObjectId],
                    nodesById,
                    occupiedByCell);
                int bottomCellCount = CountBottomCells(node, cellsByNode[node.ObjectId]);
                int actualContactCells = contactCellsBySupporter.Values.Sum();
                int requiredContactCells = DivideRoundUp(
                    checked(bottomCellCount * MinimumSupportBasisPoints),
                    BasisPointScale);

                if (actualContactCells == 0)
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.Unsupported,
                        $"Structural node {node.ObjectId} has no valid support contact.",
                        node.ObjectId,
                        requiredContactCells: requiredContactCells,
                        actualContactCells: 0);
                }

                if (actualContactCells < requiredContactCells)
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.InsufficientContact,
                        $"Structural node {node.ObjectId} has {actualContactCells} support cells but requires {requiredContactCells}.",
                        node.ObjectId,
                        requiredContactCells: requiredContactCells,
                        actualContactCells: actualContactCells);
                }

                IReadOnlyList<LoadShare> shares;
                try
                {
                    shares = DistributeLoad(totalLoadUnits, contactCellsBySupporter);
                }
                catch (OverflowException)
                {
                    return StructuralSupportResult.Reject(
                        StructuralSupportIssueCode.ArithmeticOverflow,
                        $"Structural load distribution overflowed for {node.ObjectId}.",
                        node.ObjectId,
                        loadUnits: totalLoadUnits);
                }
                for (int shareIndex = 0; shareIndex < shares.Count; shareIndex++)
                {
                    LoadShare share = shares[shareIndex];
                    try
                    {
                        receivedLoadUnits[share.SupporterId] = checked(receivedLoadUnits[share.SupporterId] + share.LoadUnits);
                    }
                    catch (OverflowException)
                    {
                        return StructuralSupportResult.Reject(
                            StructuralSupportIssueCode.ArithmeticOverflow,
                            $"Accumulated structural load overflowed for {share.SupporterId}.",
                            node.ObjectId,
                            share.SupporterId);
                    }

                    edges.Add(new StructuralSupportEdge(
                        node.ObjectId,
                        share.SupporterId,
                        share.ContactCells,
                        share.LoadUnits));
                }
            }

            return StructuralSupportResult.Accept(edges, receivedLoadUnits);
        }

        private static StructuralSupportResult ValidateNode(StructuralNode node)
        {
            if (node == null)
            {
                return StructuralSupportResult.Reject(
                    StructuralSupportIssueCode.SpatialStateInvalid,
                    "Structural graph cannot contain a null node.");
            }

            if (string.IsNullOrWhiteSpace(node.ObjectId) || !StringComparer.Ordinal.Equals(node.ObjectId, node.Placement.ObjectId))
            {
                return StructuralSupportResult.Reject(
                    StructuralSupportIssueCode.SpatialStateInvalid,
                    "Structural node id must be non-empty and match its placement id.",
                    node.ObjectId);
            }

            if (node.Weight < 0)
            {
                return StructuralSupportResult.Reject(
                    StructuralSupportIssueCode.InvalidWeight,
                    $"Structural node {node.ObjectId} cannot have negative weight.",
                    node.ObjectId);
            }

            if (node.CarryCapacity < 0)
            {
                return StructuralSupportResult.Reject(
                    StructuralSupportIssueCode.InvalidCapacity,
                    $"Structural node {node.ObjectId} cannot have negative carry capacity.",
                    node.ObjectId);
            }

            return StructuralSupportResult.Accept(Array.Empty<StructuralSupportEdge>(), new Dictionary<string, long>());
        }

        private static int ComparePropagationOrder(StructuralNode left, StructuralNode right)
        {
            int layerComparison = right.Placement.BaseLayer.CompareTo(left.Placement.BaseLayer);
            return layerComparison != 0
                ? layerComparison
                : StringComparer.Ordinal.Compare(left.ObjectId, right.ObjectId);
        }

        private static Dictionary<string, int> FindSupportContacts(
            StructuralNode node,
            IReadOnlyList<SpatialGridCell> nodeCells,
            IReadOnlyDictionary<string, StructuralNode> nodesById,
            IReadOnlyDictionary<SpatialGridCell, string> occupiedByCell)
        {
            Dictionary<string, int> contacts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < nodeCells.Count; i++)
            {
                SpatialGridCell cell = nodeCells[i];
                if (cell.Layer != node.Placement.BaseLayer)
                {
                    continue;
                }

                SpatialGridCell below = new SpatialGridCell(cell.X, cell.Y, cell.Layer - 1);
                if (!occupiedByCell.TryGetValue(below, out string supporterId))
                {
                    continue;
                }

                StructuralNode supporter = nodesById[supporterId];
                int supporterTopLayer = checked(supporter.Placement.BaseLayer + supporter.Placement.Height - 1);
                if (!supporter.CanSupport || supporterTopLayer != below.Layer)
                {
                    continue;
                }

                contacts[supporterId] = contacts.TryGetValue(supporterId, out int count) ? count + 1 : 1;
            }

            return contacts;
        }

        private static int CountBottomCells(StructuralNode node, IReadOnlyList<SpatialGridCell> cells)
        {
            int count = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Layer == node.Placement.BaseLayer)
                {
                    count++;
                }
            }

            return count;
        }

        private static IReadOnlyList<LoadShare> DistributeLoad(
            long totalLoadUnits,
            IReadOnlyDictionary<string, int> contactCellsBySupporter)
        {
            int totalContacts = contactCellsBySupporter.Values.Sum();
            List<LoadShare> shares = new List<LoadShare>(contactCellsBySupporter.Count);
            long allocated = 0;
            foreach (KeyValuePair<string, int> pair in contactCellsBySupporter)
            {
                long weightedLoad = checked(totalLoadUnits * pair.Value);
                long load = weightedLoad / totalContacts;
                long remainder = weightedLoad % totalContacts;
                allocated = checked(allocated + load);
                shares.Add(new LoadShare(pair.Key, pair.Value, load, remainder));
            }

            long remaining = totalLoadUnits - allocated;
            shares.Sort((left, right) =>
            {
                int remainderComparison = right.Remainder.CompareTo(left.Remainder);
                return remainderComparison != 0
                    ? remainderComparison
                    : StringComparer.Ordinal.Compare(left.SupporterId, right.SupporterId);
            });
            for (int i = 0; i < remaining; i++)
            {
                shares[i] = shares[i].WithAdditionalLoadUnit();
            }

            shares.Sort((left, right) => StringComparer.Ordinal.Compare(left.SupporterId, right.SupporterId));
            return shares;
        }

        private static int DivideRoundUp(int value, int divisor)
        {
            return (value + divisor - 1) / divisor;
        }

        private readonly struct LoadShare
        {
            public string SupporterId { get; }
            public int ContactCells { get; }
            public long LoadUnits { get; }
            public long Remainder { get; }

            public LoadShare(string supporterId, int contactCells, long loadUnits, long remainder)
            {
                SupporterId = supporterId;
                ContactCells = contactCells;
                LoadUnits = loadUnits;
                Remainder = remainder;
            }

            public LoadShare WithAdditionalLoadUnit()
            {
                return new LoadShare(SupporterId, ContactCells, LoadUnits + 1, Remainder);
            }
        }
    }
}
