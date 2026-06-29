using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WenMingBlocks.Runtime.Authority
{
    public enum CompatibilityIssueCode
    {
        ProtocolVersionMismatch,
        GameVersionMismatch,
        DuplicateModId,
        MissingClientMod,
        UnexpectedClientMod,
        ModVersionMismatch,
        ModChecksumMismatch,
        ModLoadOrderMismatch,
        ModDependenciesMismatch
    }

    public sealed class ModFingerprint
    {
        private readonly ReadOnlyDictionary<string, string> _dependencies;

        public string ModId { get; }
        public string Version { get; }
        public string Checksum { get; }
        public int LoadOrder { get; }
        public IReadOnlyDictionary<string, string> Dependencies
        {
            get { return _dependencies; }
        }

        public ModFingerprint(
            string modId,
            string version,
            string checksum,
            int loadOrder,
            IReadOnlyDictionary<string, string> dependencies = null)
        {
            if (string.IsNullOrWhiteSpace(modId))
            {
                throw new ArgumentException("Mod id cannot be empty.", nameof(modId));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("Mod version cannot be empty.", nameof(version));
            }

            if (string.IsNullOrWhiteSpace(checksum))
            {
                throw new ArgumentException("Mod checksum cannot be empty.", nameof(checksum));
            }

            if (loadOrder < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(loadOrder));
            }

            ModId = modId;
            Version = version;
            Checksum = checksum;
            LoadOrder = loadOrder;
            Dictionary<string, string> dependencySnapshot = dependencies == null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : dependencies.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            _dependencies = new ReadOnlyDictionary<string, string>(dependencySnapshot);
        }

        internal string CreateDependencySignature()
        {
            return string.Join("\n", _dependencies
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + "=" + pair.Value));
        }
    }

    public sealed class SessionCompatibilityProfile
    {
        public const string CurrentProtocolVersion = "21";

        private readonly ReadOnlyCollection<ModFingerprint> _mods;

        public string ProtocolVersion { get; }
        public string GameVersion { get; }
        public IReadOnlyList<ModFingerprint> Mods
        {
            get { return _mods; }
        }

        public SessionCompatibilityProfile(
            string protocolVersion,
            string gameVersion,
            IEnumerable<ModFingerprint> mods)
        {
            if (string.IsNullOrWhiteSpace(protocolVersion))
            {
                throw new ArgumentException("Protocol version cannot be empty.", nameof(protocolVersion));
            }

            if (string.IsNullOrWhiteSpace(gameVersion))
            {
                throw new ArgumentException("Game version cannot be empty.", nameof(gameVersion));
            }

            ProtocolVersion = protocolVersion;
            GameVersion = gameVersion;
            List<ModFingerprint> modSnapshot = mods == null
                ? throw new ArgumentNullException(nameof(mods))
                : new List<ModFingerprint>(mods);

            if (modSnapshot.Any(mod => mod == null))
            {
                throw new ArgumentException("Mod list cannot contain null entries.", nameof(mods));
            }

            _mods = modSnapshot.AsReadOnly();
        }

        public static SessionCompatibilityProfile CreateVanilla(string protocolVersion, string gameVersion)
        {
            return new SessionCompatibilityProfile(protocolVersion, gameVersion, Array.Empty<ModFingerprint>());
        }
    }

    public sealed class CompatibilityIssue
    {
        public CompatibilityIssueCode Code { get; }
        public string ModId { get; }
        public string Message { get; }

        public CompatibilityIssue(CompatibilityIssueCode code, string message, string modId = "")
        {
            Code = code;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            ModId = modId ?? string.Empty;
        }
    }

    public sealed class CompatibilityReport
    {
        private readonly ReadOnlyCollection<CompatibilityIssue> _issues;

        public bool Compatible
        {
            get { return _issues.Count == 0; }
        }

        public IReadOnlyList<CompatibilityIssue> Issues
        {
            get { return _issues; }
        }

        public CompatibilityReport(IEnumerable<CompatibilityIssue> issues)
        {
            List<CompatibilityIssue> issueSnapshot = issues == null
                ? throw new ArgumentNullException(nameof(issues))
                : new List<CompatibilityIssue>(issues);
            if (issueSnapshot.Any(issue => issue == null))
            {
                throw new ArgumentException("Compatibility issues cannot contain null entries.", nameof(issues));
            }

            _issues = issueSnapshot.AsReadOnly();
        }

        public string CreateSummary()
        {
            return Compatible
                ? "Compatible."
                : string.Join(" ", _issues.Select(issue => $"[{issue.Code}] {issue.Message}"));
        }
    }

    public static class ModCompatibility
    {
        public static CompatibilityReport Compare(
            SessionCompatibilityProfile server,
            SessionCompatibilityProfile client)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            List<CompatibilityIssue> issues = new List<CompatibilityIssue>();
            if (!StringComparer.Ordinal.Equals(server.ProtocolVersion, client.ProtocolVersion))
            {
                issues.Add(new CompatibilityIssue(
                    CompatibilityIssueCode.ProtocolVersionMismatch,
                    $"Server protocol {server.ProtocolVersion} does not match client protocol {client.ProtocolVersion}."));
            }

            if (!StringComparer.Ordinal.Equals(server.GameVersion, client.GameVersion))
            {
                issues.Add(new CompatibilityIssue(
                    CompatibilityIssueCode.GameVersionMismatch,
                    $"Server game version {server.GameVersion} does not match client game version {client.GameVersion}."));
            }

            Dictionary<string, ModFingerprint> serverMods = IndexMods(server.Mods, "server", issues);
            Dictionary<string, ModFingerprint> clientMods = IndexMods(client.Mods, "client", issues);

            foreach (ModFingerprint serverMod in server.Mods.OrderBy(mod => mod.LoadOrder))
            {
                if (!clientMods.TryGetValue(serverMod.ModId, out ModFingerprint clientMod))
                {
                    issues.Add(new CompatibilityIssue(
                        CompatibilityIssueCode.MissingClientMod,
                        $"Client is missing server mod {serverMod.ModId}.",
                        serverMod.ModId));
                    continue;
                }

                CompareMod(serverMod, clientMod, issues);
            }

            foreach (ModFingerprint clientMod in client.Mods.OrderBy(mod => mod.LoadOrder))
            {
                if (!serverMods.ContainsKey(clientMod.ModId))
                {
                    issues.Add(new CompatibilityIssue(
                        CompatibilityIssueCode.UnexpectedClientMod,
                        $"Client has mod {clientMod.ModId}, which is not loaded by the server.",
                        clientMod.ModId));
                }
            }

            return new CompatibilityReport(issues);
        }

        private static Dictionary<string, ModFingerprint> IndexMods(
            IReadOnlyList<ModFingerprint> mods,
            string side,
            ICollection<CompatibilityIssue> issues)
        {
            Dictionary<string, ModFingerprint> index = new Dictionary<string, ModFingerprint>(StringComparer.Ordinal);
            for (int i = 0; i < mods.Count; i++)
            {
                ModFingerprint mod = mods[i];
                if (!index.TryAdd(mod.ModId, mod))
                {
                    issues.Add(new CompatibilityIssue(
                        CompatibilityIssueCode.DuplicateModId,
                        $"The {side} profile contains duplicate mod id {mod.ModId}.",
                        mod.ModId));
                }
            }

            return index;
        }

        private static void CompareMod(
            ModFingerprint server,
            ModFingerprint client,
            ICollection<CompatibilityIssue> issues)
        {
            if (!StringComparer.Ordinal.Equals(server.Version, client.Version))
            {
                issues.Add(new CompatibilityIssue(
                    CompatibilityIssueCode.ModVersionMismatch,
                    $"Mod {server.ModId} is version {server.Version} on the server and {client.Version} on the client.",
                    server.ModId));
            }

            if (!StringComparer.Ordinal.Equals(server.Checksum, client.Checksum))
            {
                issues.Add(new CompatibilityIssue(
                    CompatibilityIssueCode.ModChecksumMismatch,
                    $"Mod {server.ModId} has a different checksum on the client.",
                    server.ModId));
            }

            if (server.LoadOrder != client.LoadOrder)
            {
                issues.Add(new CompatibilityIssue(
                    CompatibilityIssueCode.ModLoadOrderMismatch,
                    $"Mod {server.ModId} loads at position {server.LoadOrder} on the server and {client.LoadOrder} on the client.",
                    server.ModId));
            }

            if (!StringComparer.Ordinal.Equals(server.CreateDependencySignature(), client.CreateDependencySignature()))
            {
                issues.Add(new CompatibilityIssue(
                    CompatibilityIssueCode.ModDependenciesMismatch,
                    $"Mod {server.ModId} has different dependency declarations on the client.",
                    server.ModId));
            }
        }
    }

    public sealed class SessionCompatibilityException : InvalidOperationException
    {
        public CompatibilityReport Report { get; }

        public SessionCompatibilityException(CompatibilityReport report)
            : base(report?.CreateSummary())
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }
    }
}
