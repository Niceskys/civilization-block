using System;
using System.Collections.Generic;

namespace WenMingBlocks.Runtime.Authority
{
    public sealed class ModManifest
    {
        public string ModId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string MinGameVersion { get; set; } = string.Empty;
        public Dictionary<string, string> Dependencies { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
        public List<string> ContentTypes { get; set; } = new List<string>();
        public string Checksum { get; set; } = string.Empty;
    }

    public sealed class LoadedMod
    {
        public ModManifest Manifest { get; }
        public int LoadOrder { get; }

        public LoadedMod(ModManifest manifest, int loadOrder)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            LoadOrder = loadOrder;
        }
    }

    public sealed class ModLoadResult
    {
        public bool Accepted { get; }
        public string Reason { get; }

        private ModLoadResult(bool accepted, string reason)
        {
            Accepted = accepted;
            Reason = reason;
        }

        public static ModLoadResult Accept()
        {
            return new ModLoadResult(true, string.Empty);
        }

        public static ModLoadResult Reject(string reason)
        {
            return new ModLoadResult(false, reason);
        }
    }

    public sealed class ModRegistry
    {
        private readonly Dictionary<string, LoadedMod> _loadedMods = new Dictionary<string, LoadedMod>(StringComparer.Ordinal);
        private readonly List<LoadedMod> _loadOrder = new List<LoadedMod>();

        public IReadOnlyList<LoadedMod> LoadedMods
        {
            get { return _loadOrder; }
        }

        public ModLoadResult RegisterManifest(ModManifest manifest)
        {
            ModLoadResult validation = ValidateManifest(manifest);
            if (!validation.Accepted)
            {
                return validation;
            }

            if (_loadedMods.ContainsKey(manifest.ModId))
            {
                return ModLoadResult.Reject($"Mod {manifest.ModId} is already loaded.");
            }

            foreach (KeyValuePair<string, string> dependency in manifest.Dependencies)
            {
                if (!_loadedMods.ContainsKey(dependency.Key))
                {
                    return ModLoadResult.Reject($"Missing dependency {dependency.Key} for mod {manifest.ModId}.");
                }
            }

            ModManifest manifestSnapshot = CloneManifest(manifest);
            LoadedMod loadedMod = new LoadedMod(manifestSnapshot, _loadOrder.Count);
            _loadedMods[manifestSnapshot.ModId] = loadedMod;
            _loadOrder.Add(loadedMod);
            return ModLoadResult.Accept();
        }

        public bool IsLoaded(string modId)
        {
            return _loadedMods.ContainsKey(modId);
        }

        public bool TryGetLoadedMod(string modId, out LoadedMod loadedMod)
        {
            return _loadedMods.TryGetValue(modId, out loadedMod);
        }

        public SessionCompatibilityProfile CreateCompatibilityProfile(string protocolVersion, string gameVersion)
        {
            List<ModFingerprint> mods = new List<ModFingerprint>(_loadOrder.Count);
            for (int i = 0; i < _loadOrder.Count; i++)
            {
                LoadedMod loadedMod = _loadOrder[i];
                mods.Add(new ModFingerprint(
                    loadedMod.Manifest.ModId,
                    loadedMod.Manifest.Version,
                    loadedMod.Manifest.Checksum,
                    loadedMod.LoadOrder,
                    loadedMod.Manifest.Dependencies));
            }

            return new SessionCompatibilityProfile(protocolVersion, gameVersion, mods);
        }

        public static ModLoadResult ValidateManifest(ModManifest manifest)
        {
            if (manifest == null)
            {
                return ModLoadResult.Reject("Missing mod manifest.");
            }

            if (!IsValidNamespace(manifest.ModId))
            {
                return ModLoadResult.Reject("Mod id must be a non-empty namespace and cannot be core.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Name))
            {
                return ModLoadResult.Reject("Mod name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Version))
            {
                return ModLoadResult.Reject("Mod version cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(manifest.MinGameVersion))
            {
                return ModLoadResult.Reject("Mod min game version cannot be empty.");
            }

            if (manifest.Dependencies == null)
            {
                return ModLoadResult.Reject("Mod dependencies cannot be null.");
            }

            if (manifest.ContentTypes == null || manifest.ContentTypes.Count == 0)
            {
                return ModLoadResult.Reject("Mod must declare at least one content type.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Checksum))
            {
                return ModLoadResult.Reject("Mod checksum cannot be empty.");
            }

            for (int i = 0; i < manifest.ContentTypes.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(manifest.ContentTypes[i]))
                {
                    return ModLoadResult.Reject("Mod content type cannot be empty.");
                }
            }

            foreach (string dependencyId in manifest.Dependencies.Keys)
            {
                if (!IsValidNamespace(dependencyId))
                {
                    return ModLoadResult.Reject($"Invalid dependency id {dependencyId}.");
                }
            }

            return ModLoadResult.Accept();
        }

        private static bool IsValidNamespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || StringComparer.Ordinal.Equals(value, "core"))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static ModManifest CloneManifest(ModManifest manifest)
        {
            return new ModManifest
            {
                ModId = manifest.ModId,
                Name = manifest.Name,
                Version = manifest.Version,
                MinGameVersion = manifest.MinGameVersion,
                Dependencies = new Dictionary<string, string>(manifest.Dependencies, StringComparer.Ordinal),
                ContentTypes = new List<string>(manifest.ContentTypes),
                Checksum = manifest.Checksum
            };
        }
    }
}
