using System;
using System.Collections.Generic;

namespace WenMingBlocks.Runtime.Authority
{
    public enum OccupancyKind
    {
        Solid,
        Attachment,
        Connector
    }

    public enum ResourceCategory
    {
        Basic,
        Mineral,
        Rare,
        Extended
    }

    public sealed class DefinitionRegistry
    {
        private readonly Dictionary<string, BuildingDefinition> _buildings = new Dictionary<string, BuildingDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, RecipeDefinition> _recipes = new Dictionary<string, RecipeDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, LogisticsConnectorDefinition> _logisticsConnectors = new Dictionary<string, LogisticsConnectorDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, ResourceDefinition> _resources = new Dictionary<string, ResourceDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, ContinuousProductionDefinition> _continuousProduction = new Dictionary<string, ContinuousProductionDefinition>(StringComparer.Ordinal);

        public bool IsSealed { get; private set; }
        public int ResourceCount
        {
            get { return _resources.Count; }
        }

        public void Seal()
        {
            IsSealed = true;
        }

        public void ValidateReferencesAndSeal()
        {
            EnsureMutable();

            foreach (BuildingDefinition building in _buildings.Values)
            {
                ValidateKnownResources(building.BuildCost.Keys, $"Building {building.DefinitionId} cost");
            }
            foreach (RecipeDefinition recipe in _recipes.Values)
            {
                if (!_buildings.ContainsKey(recipe.BuildingDefinitionId))
                {
                    throw new InvalidOperationException(
                        $"Recipe {recipe.RecipeId} references unknown building {recipe.BuildingDefinitionId}.");
                }
                ValidateKnownResources(recipe.Inputs.Keys, $"Recipe {recipe.RecipeId} input");
                ValidateKnownResources(recipe.Outputs.Keys, $"Recipe {recipe.RecipeId} output");
                ValidateKnownResources(recipe.WeightedOutputWeights.Keys, $"Recipe {recipe.RecipeId} weighted output");
            }
            foreach (LogisticsConnectorDefinition connector in _logisticsConnectors.Values)
            {
                ValidateKnownResources(connector.BuildCost.Keys, $"Connector {connector.DefinitionId} cost");
            }
            foreach (ContinuousProductionDefinition production in _continuousProduction.Values)
            {
                if (!_buildings.ContainsKey(production.BuildingDefinitionId))
                {
                    throw new InvalidOperationException($"Continuous production references unknown building {production.BuildingDefinitionId}.");
                }
                ValidateKnownResources(new[] { production.OutputResourceId },
                    $"Continuous production {production.BuildingDefinitionId} output");
                ValidateKnownResources(production.AdditionalOutputsPerWorkerPerDay.Keys,
                    $"Continuous production {production.BuildingDefinitionId} additional output");
                if (!string.IsNullOrEmpty(production.OperatingInputResourceId))
                {
                    ValidateKnownResources(new[] { production.OperatingInputResourceId },
                        $"Continuous production {production.BuildingDefinitionId} input");
                }
            }

            IsSealed = true;
        }

        public void RegisterResource(ResourceDefinition definition)
        {
            EnsureMutable();
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (!StableId.IsValid(definition.ResourceId))
            {
                throw new ArgumentException("Resource definition id must use namespace:type:id format.", nameof(definition));
            }
            if (_resources.ContainsKey(definition.ResourceId))
            {
                throw new InvalidOperationException($"Resource definition {definition.ResourceId} is already registered.");
            }

            _resources.Add(definition.ResourceId, CloneResource(definition));
        }

        public bool TryGetResource(string resourceId, out ResourceDefinition definition)
        {
            if (_resources.TryGetValue(resourceId, out ResourceDefinition registered))
            {
                definition = CloneResource(registered);
                return true;
            }
            definition = null;
            return false;
        }

        public void RegisterContinuousProduction(ContinuousProductionDefinition definition)
        {
            EnsureMutable();
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (!StableId.IsValid(definition.BuildingDefinitionId) ||
                !StableId.IsValid(definition.OutputResourceId) ||
                definition.OutputPerWorkerPerDay <= 0 ||
                definition.AdditionalOutputsPerWorkerPerDay == null ||
                definition.OperatingInputPerDay < 0 ||
                (definition.OperatingInputPerDay > 0 && !StableId.IsValid(definition.OperatingInputResourceId)) ||
                (definition.OperatingInputPerDay == 0 && !string.IsNullOrEmpty(definition.OperatingInputResourceId)))
            {
                throw new ArgumentException("Continuous production definition is invalid.", nameof(definition));
            }
            foreach (KeyValuePair<string, int> output in definition.AdditionalOutputsPerWorkerPerDay)
            {
                if (!StableId.IsValid(output.Key) || output.Value <= 0 ||
                    StringComparer.Ordinal.Equals(output.Key, definition.OutputResourceId))
                    throw new ArgumentException("Additional continuous outputs are invalid.", nameof(definition));
            }
            if (_continuousProduction.ContainsKey(definition.BuildingDefinitionId))
            {
                throw new InvalidOperationException(
                    $"Continuous production for {definition.BuildingDefinitionId} is already registered.");
            }
            _continuousProduction.Add(definition.BuildingDefinitionId, CloneContinuousProduction(definition));
        }

        public bool TryGetContinuousProduction(string buildingDefinitionId, out ContinuousProductionDefinition definition)
        {
            if (_continuousProduction.TryGetValue(buildingDefinitionId, out ContinuousProductionDefinition registered))
            {
                definition = CloneContinuousProduction(registered);
                return true;
            }
            definition = null;
            return false;
        }

        public void RegisterBuilding(BuildingDefinition definition)
        {
            EnsureMutable();
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!StableId.IsValid(definition.DefinitionId))
            {
                throw new ArgumentException("Building definition id must use namespace:type:id format.", nameof(definition));
            }

            if (definition.FootprintWidth <= 0 || definition.FootprintDepth <= 0 || definition.FootprintHeight <= 0)
            {
                throw new ArgumentException("Building footprint dimensions must be positive.", nameof(definition));
            }

            if (definition.OccupancyKind != OccupancyKind.Solid)
            {
                throw new NotSupportedException("Attachment and connector occupancy are not enabled until their dedicated migration phase.");
            }

            if (definition.BuildCost == null)
            {
                throw new ArgumentException("Building cost cannot be null.", nameof(definition));
            }

            if (definition.WorkerSlotCount < 0)
            {
                throw new ArgumentException("Worker slot count cannot be negative.", nameof(definition));
            }

            if (definition.LocalInventoryCapacity < 0)
            {
                throw new ArgumentException("Local inventory capacity cannot be negative.", nameof(definition));
            }

            if (definition.GlobalStorageCapacityBonus < 0)
            {
                throw new ArgumentException("Global storage capacity bonus cannot be negative.", nameof(definition));
            }

            if (definition.BedSlotCount < 0 || definition.AdditionalBedSlotsPerLevel < 0)
            {
                throw new ArgumentException("Bed slot counts cannot be negative.", nameof(definition));
            }

            foreach (KeyValuePair<string, int> cost in definition.BuildCost)
            {
                if (!StableId.IsValid(cost.Key) || cost.Value < 0)
                {
                    throw new ArgumentException("Building costs require valid resource ids and non-negative amounts.", nameof(definition));
                }
            }

            long occupiedCells = (long)definition.FootprintWidth * definition.FootprintDepth * definition.FootprintHeight;
            if (definition.FootprintWidth > SpatialOccupancy.MaxFootprintDimension ||
                definition.FootprintDepth > SpatialOccupancy.MaxFootprintDimension ||
                definition.FootprintHeight > SpatialOccupancy.MaxFootprintDimension ||
                occupiedCells > SpatialOccupancy.MaxFootprintCells)
            {
                throw new ArgumentException("Building footprint exceeds spatial occupancy limits.", nameof(definition));
            }

            if (_buildings.ContainsKey(definition.DefinitionId))
            {
                throw new InvalidOperationException($"Building definition {definition.DefinitionId} is already registered.");
            }

            _buildings.Add(definition.DefinitionId, CloneBuilding(definition));
        }

        public bool TryGetBuilding(string definitionId, out BuildingDefinition definition)
        {
            if (_buildings.TryGetValue(definitionId, out BuildingDefinition registered))
            {
                definition = CloneBuilding(registered);
                return true;
            }
            definition = null;
            return false;
        }

        public void RegisterRecipe(RecipeDefinition recipe)
        {
            EnsureMutable();
            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (!StableId.IsValid(recipe.RecipeId) || !StableId.IsValid(recipe.BuildingDefinitionId))
            {
                throw new ArgumentException("Recipe and building ids must use namespace:type:id format.", nameof(recipe));
            }

            if (recipe.RequiredWorkTicks <= 0 || recipe.MinimumWorkers <= 0 ||
                recipe.Inputs == null || recipe.Outputs == null || recipe.WeightedOutputWeights == null ||
                recipe.WeightedOutputRolls < 0 ||
                (recipe.Outputs.Count == 0 && recipe.WeightedOutputRolls == 0 && !recipe.DiscardsInputs) ||
                (recipe.WeightedOutputRolls > 0 && recipe.WeightedOutputWeights.Count == 0) ||
                (recipe.WeightedOutputRolls == 0 && recipe.WeightedOutputWeights.Count > 0))
            {
                throw new ArgumentException("Recipe work, workers, inputs, and outputs are invalid.", nameof(recipe));
            }

            ValidateRecipeResources(recipe.Inputs, true, recipe);
            ValidateRecipeResources(recipe.Outputs, recipe.DiscardsInputs || recipe.WeightedOutputRolls > 0, recipe);
            ValidateRecipeResources(recipe.WeightedOutputWeights, recipe.WeightedOutputRolls == 0, recipe);
            if (_recipes.ContainsKey(recipe.RecipeId))
            {
                throw new InvalidOperationException($"Recipe definition {recipe.RecipeId} is already registered.");
            }

            _recipes.Add(recipe.RecipeId, CloneRecipe(recipe));
        }

        public bool TryGetRecipe(string recipeId, out RecipeDefinition recipe)
        {
            if (_recipes.TryGetValue(recipeId, out RecipeDefinition registered))
            {
                recipe = CloneRecipe(registered);
                return true;
            }
            recipe = null;
            return false;
        }

        public void RegisterLogisticsConnector(LogisticsConnectorDefinition definition)
        {
            EnsureMutable();
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!StableId.IsValid(definition.DefinitionId) || definition.ConstructionTicks <= 0 ||
                definition.MaxDurability <= 0 || definition.BuildCost == null)
            {
                throw new ArgumentException("Logistics connector definition is invalid.", nameof(definition));
            }

            foreach (KeyValuePair<string, int> cost in definition.BuildCost)
            {
                if (!StableId.IsValid(cost.Key) || cost.Value < 0)
                {
                    throw new ArgumentException("Connector costs require valid resource ids and non-negative amounts.", nameof(definition));
                }
            }

            if (_logisticsConnectors.ContainsKey(definition.DefinitionId))
            {
                throw new InvalidOperationException($"Logistics connector definition {definition.DefinitionId} is already registered.");
            }

            _logisticsConnectors.Add(definition.DefinitionId, CloneConnector(definition));
        }

        public bool TryGetLogisticsConnector(string definitionId, out LogisticsConnectorDefinition definition)
        {
            if (_logisticsConnectors.TryGetValue(definitionId, out LogisticsConnectorDefinition registered))
            {
                definition = CloneConnector(registered);
                return true;
            }
            definition = null;
            return false;
        }

        private void EnsureMutable()
        {
            if (IsSealed)
            {
                throw new InvalidOperationException("Definition registry is sealed after simulation composition.");
            }
        }

        private void ValidateKnownResources(IEnumerable<string> resourceIds, string owner)
        {
            foreach (string resourceId in resourceIds)
            {
                if (!_resources.ContainsKey(resourceId))
                {
                    throw new InvalidOperationException($"{owner} references unknown resource {resourceId}.");
                }
            }
        }

        private static ResourceDefinition CloneResource(ResourceDefinition definition)
        {
            return new ResourceDefinition
            {
                ResourceId = definition.ResourceId,
                Category = definition.Category,
                IsStorable = definition.IsStorable,
                IsCurrency = definition.IsCurrency
            };
        }

        private static ContinuousProductionDefinition CloneContinuousProduction(ContinuousProductionDefinition definition)
        {
            return new ContinuousProductionDefinition
            {
                BuildingDefinitionId = definition.BuildingDefinitionId,
                OutputResourceId = definition.OutputResourceId,
                OutputPerWorkerPerDay = definition.OutputPerWorkerPerDay,
                OperatingInputResourceId = definition.OperatingInputResourceId,
                OperatingInputPerDay = definition.OperatingInputPerDay,
                AdditionalOutputsPerWorkerPerDay = new Dictionary<string, int>(
                    definition.AdditionalOutputsPerWorkerPerDay, StringComparer.Ordinal)
            };
        }

        private static BuildingDefinition CloneBuilding(BuildingDefinition definition)
        {
            return new BuildingDefinition
            {
                DefinitionId = definition.DefinitionId,
                Category = definition.Category,
                MaxDurability = definition.MaxDurability,
                CarryCapacity = definition.CarryCapacity,
                Weight = definition.Weight,
                ConstructionTicks = definition.ConstructionTicks,
                FirstBuildBonusTicks = definition.FirstBuildBonusTicks,
                IsHome = definition.IsHome,
                IsBasicProduction = definition.IsBasicProduction,
                FootprintWidth = definition.FootprintWidth,
                FootprintDepth = definition.FootprintDepth,
                FootprintHeight = definition.FootprintHeight,
                OccupancyKind = definition.OccupancyKind,
                BuildCost = new Dictionary<string, int>(definition.BuildCost, StringComparer.Ordinal),
                WorkerSlotCount = definition.WorkerSlotCount,
                LocalInventoryCapacity = definition.LocalInventoryCapacity,
                GlobalStorageCapacityBonus = definition.GlobalStorageCapacityBonus,
                BedSlotCount = definition.BedSlotCount,
                AdditionalBedSlotsPerLevel = definition.AdditionalBedSlotsPerLevel
            };
        }

        private static RecipeDefinition CloneRecipe(RecipeDefinition recipe)
        {
            return new RecipeDefinition
            {
                RecipeId = recipe.RecipeId,
                BuildingDefinitionId = recipe.BuildingDefinitionId,
                RequiredWorkTicks = recipe.RequiredWorkTicks,
                MinimumWorkers = recipe.MinimumWorkers,
                Inputs = new Dictionary<string, int>(recipe.Inputs, StringComparer.Ordinal),
                Outputs = new Dictionary<string, int>(recipe.Outputs, StringComparer.Ordinal),
                DiscardsInputs = recipe.DiscardsInputs,
                WeightedOutputRolls = recipe.WeightedOutputRolls,
                WeightedOutputWeights = new Dictionary<string, int>(recipe.WeightedOutputWeights, StringComparer.Ordinal)
            };
        }

        private static LogisticsConnectorDefinition CloneConnector(LogisticsConnectorDefinition definition)
        {
            return new LogisticsConnectorDefinition
            {
                DefinitionId = definition.DefinitionId,
                ConstructionTicks = definition.ConstructionTicks,
                MaxDurability = definition.MaxDurability,
                BuildCost = new Dictionary<string, int>(definition.BuildCost, StringComparer.Ordinal)
            };
        }

        private static void ValidateRecipeResources(Dictionary<string, int> resources, bool allowEmpty, RecipeDefinition recipe)
        {
            if (!allowEmpty && resources.Count == 0)
            {
                throw new ArgumentException("Recipe outputs cannot be empty.", nameof(recipe));
            }

            foreach (KeyValuePair<string, int> pair in resources)
            {
                if (!StableId.IsValid(pair.Key) || pair.Value <= 0)
                {
                    throw new ArgumentException("Recipe resources require valid ids and positive amounts.", nameof(recipe));
                }
            }
        }
    }

    public sealed class BuildingDefinition
    {
        public string DefinitionId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int MaxDurability { get; set; } = 100;
        public int CarryCapacity { get; set; }
        public int Weight { get; set; } = 1;
        public long ConstructionTicks { get; set; }
        public long FirstBuildBonusTicks { get; set; }
        public bool IsHome { get; set; }
        public bool IsBasicProduction { get; set; }
        public int FootprintWidth { get; set; } = 1;
        public int FootprintDepth { get; set; } = 1;
        public int FootprintHeight { get; set; } = 1;
        public OccupancyKind OccupancyKind { get; set; } = OccupancyKind.Solid;
        public Dictionary<string, int> BuildCost { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public int WorkerSlotCount { get; set; }
        public int LocalInventoryCapacity { get; set; }
        public int GlobalStorageCapacityBonus { get; set; }
        public int BedSlotCount { get; set; }
        public int AdditionalBedSlotsPerLevel { get; set; }
    }

    public sealed class ResourceDefinition
    {
        public string ResourceId { get; set; } = string.Empty;
        public ResourceCategory Category { get; set; }
        public bool IsStorable { get; set; } = true;
        public bool IsCurrency { get; set; }
    }

    public sealed class ContinuousProductionDefinition
    {
        public string BuildingDefinitionId { get; set; } = string.Empty;
        public string OutputResourceId { get; set; } = string.Empty;
        public int OutputPerWorkerPerDay { get; set; }
        public string OperatingInputResourceId { get; set; } = string.Empty;
        public int OperatingInputPerDay { get; set; }
        public Dictionary<string, int> AdditionalOutputsPerWorkerPerDay { get; set; } =
            new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class RecipeDefinition
    {
        public string RecipeId { get; set; } = string.Empty;
        public string BuildingDefinitionId { get; set; } = string.Empty;
        public long RequiredWorkTicks { get; set; }
        public int MinimumWorkers { get; set; } = 1;
        public Dictionary<string, int> Inputs { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> Outputs { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public bool DiscardsInputs { get; set; }
        public int WeightedOutputRolls { get; set; }
        public Dictionary<string, int> WeightedOutputWeights { get; set; } =
            new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class LogisticsConnectorDefinition
    {
        public string DefinitionId { get; set; } = string.Empty;
        public long ConstructionTicks { get; set; } = 1;
        public int MaxDurability { get; set; } = 200;
        public Dictionary<string, int> BuildCost { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);
    }
}
