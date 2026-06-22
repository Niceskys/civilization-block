using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WenMingBlocks.Runtime.Authority
{
    public interface IDefinitionModule
    {
        string ModuleId { get; }
        void RegisterDefinitions(DefinitionRegistry registry);
    }

    public sealed class CoreContentDefinitionModule : IDefinitionModule
    {
        public const string Id = "definitions:core:runtime";
        public const string PipeDefinitionId = "connector:core:pipe";

        public string ModuleId
        {
            get { return Id; }
        }

        public void RegisterDefinitions(DefinitionRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            RegisterResources(registry);
            RegisterInitialBuildings(registry);
            RegisterContinuousProduction(registry);
            RegisterInitialRecipes(registry);
            registry.RegisterLogisticsConnector(new LogisticsConnectorDefinition
            {
                DefinitionId = PipeDefinitionId,
                ConstructionTicks = GameTime.TicksPerGameDay * 3 / 10,
                MaxDurability = 200,
                BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    [CoreResourceIds.Wood] = 5,
                    [CoreResourceIds.IronIngot] = 2
                }
            });
        }

        private static void RegisterInitialBuildings(DefinitionRegistry registry)
        {
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = CoreBuildingIds.House,
                Category = "housing",
                MaxDurability = 500,
                CarryCapacity = 3,
                Weight = 1,
                ConstructionTicks = GameTime.TicksPerGameDay / 2,
                FirstBuildBonusTicks = GameTime.TicksPerGameDay / 4,
                IsHome = true,
                BedSlotCount = 2,
                AdditionalBedSlotsPerLevel = 1,
                BuildCost = Cost((CoreResourceIds.Wood, 10)),
                LocalInventoryCapacity = 20
            });
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = CoreBuildingIds.Farm,
                Category = "production",
                MaxDurability = 500,
                CarryCapacity = 3,
                Weight = 1,
                ConstructionTicks = GameTime.TicksPerGameDay,
                FirstBuildBonusTicks = GameTime.TicksPerGameDay * 2 / 5,
                IsBasicProduction = true,
                BuildCost = Cost((CoreResourceIds.Wood, 5), (CoreResourceIds.Water, 1)),
                WorkerSlotCount = 2,
                LocalInventoryCapacity = 20
            });
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = CoreBuildingIds.Well,
                Category = "production",
                MaxDurability = 500,
                CarryCapacity = 3,
                Weight = 2,
                ConstructionTicks = GameTime.TicksPerGameDay,
                FirstBuildBonusTicks = GameTime.TicksPerGameDay * 2 / 5,
                IsBasicProduction = true,
                BuildCost = Cost((CoreResourceIds.Stone, 10)),
                WorkerSlotCount = 2,
                LocalInventoryCapacity = 20
            });
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = CoreBuildingIds.TreeFarm,
                Category = "production",
                MaxDurability = 500,
                CarryCapacity = 3,
                Weight = 1,
                ConstructionTicks = GameTime.TicksPerGameDay,
                FirstBuildBonusTicks = GameTime.TicksPerGameDay * 2 / 5,
                IsBasicProduction = true,
                BuildCost = Cost((CoreResourceIds.Wood, 10)),
                WorkerSlotCount = 2,
                LocalInventoryCapacity = 20
            });
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = CoreBuildingIds.ExcavationSite,
                Category = "production",
                MaxDurability = 500,
                CarryCapacity = 5,
                Weight = 3,
                ConstructionTicks = GameTime.TicksPerGameDay,
                BuildCost = Cost((CoreResourceIds.Stone, 15)),
                WorkerSlotCount = 2,
                LocalInventoryCapacity = 30
            });
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = CoreBuildingIds.Smelter,
                Category = "production",
                MaxDurability = 600,
                CarryCapacity = 5,
                Weight = 4,
                ConstructionTicks = GameTime.TicksPerGameDay * 2,
                BuildCost = Cost((CoreResourceIds.Stone, 15), (CoreResourceIds.IronIngot, 5)),
                WorkerSlotCount = 2,
                LocalInventoryCapacity = 30
            });
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = CoreBuildingIds.Warehouse,
                Category = "storage",
                MaxDurability = 400,
                CarryCapacity = 4,
                Weight = 4,
                ConstructionTicks = GameTime.TicksPerGameDay / 2,
                BuildCost = Cost((CoreResourceIds.Wood, 15), (CoreResourceIds.Stone, 5)),
                GlobalStorageCapacityBonus = 200
            });
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = CoreBuildingIds.WasteProcessor,
                Category = "utility",
                MaxDurability = 500,
                CarryCapacity = 4,
                Weight = 4,
                ConstructionTicks = GameTime.TicksPerGameDay,
                BuildCost = Cost((CoreResourceIds.Wood, 10), (CoreResourceIds.Stone, 10)),
                WorkerSlotCount = 1,
                LocalInventoryCapacity = 30
            });
        }

        private static void RegisterInitialRecipes(DefinitionRegistry registry)
        {
            registry.RegisterRecipe(new RecipeDefinition
            {
                RecipeId = CoreRecipeIds.EmergencyCharcoal,
                BuildingDefinitionId = CoreBuildingIds.TreeFarm,
                RequiredWorkTicks = GameTime.TicksPerGameDay,
                MinimumWorkers = 1,
                Inputs = Cost((CoreResourceIds.Wood, 2)),
                Outputs = Cost((CoreResourceIds.Fuel, 1))
            });
            registry.RegisterRecipe(new RecipeDefinition
            {
                RecipeId = CoreRecipeIds.SmeltIron,
                BuildingDefinitionId = CoreBuildingIds.Smelter,
                RequiredWorkTicks = GameTime.TicksPerGameDay,
                MinimumWorkers = 1,
                Inputs = Cost((CoreResourceIds.IronOre, 1), (CoreResourceIds.Fuel, 1)),
                Outputs = Cost((CoreResourceIds.IronIngot, 1))
            });
            registry.RegisterRecipe(new RecipeDefinition
            {
                RecipeId = CoreRecipeIds.SmeltCopper,
                BuildingDefinitionId = CoreBuildingIds.Smelter,
                RequiredWorkTicks = GameTime.TicksPerGameDay,
                MinimumWorkers = 1,
                Inputs = Cost((CoreResourceIds.CopperOre, 2), (CoreResourceIds.Fuel, 1)),
                Outputs = Cost((CoreResourceIds.CopperIngot, 1))
            });
            registry.RegisterRecipe(new RecipeDefinition
            {
                RecipeId = CoreRecipeIds.DestroyWaste,
                BuildingDefinitionId = CoreBuildingIds.WasteProcessor,
                RequiredWorkTicks = GameTime.TicksPerGameDay,
                MinimumWorkers = 1,
                Inputs = Cost((CoreResourceIds.Waste, 10)),
                DiscardsInputs = true
            });
            registry.RegisterRecipe(new RecipeDefinition
            {
                RecipeId = CoreRecipeIds.RecycleWaste,
                BuildingDefinitionId = CoreBuildingIds.WasteProcessor,
                RequiredWorkTicks = GameTime.TicksPerGameDay,
                MinimumWorkers = 1,
                Inputs = Cost((CoreResourceIds.Waste, 6)),
                WeightedOutputRolls = 6,
                WeightedOutputWeights = Cost(
                    (CoreResourceIds.Wood, 40),
                    (CoreResourceIds.Stone, 40),
                    (CoreResourceIds.IronIngot, 20))
            });
            registry.RegisterRecipe(new RecipeDefinition
            {
                RecipeId = CoreRecipeIds.CompostWaste,
                BuildingDefinitionId = CoreBuildingIds.WasteProcessor,
                RequiredWorkTicks = GameTime.TicksPerGameDay,
                MinimumWorkers = 1,
                Inputs = Cost((CoreResourceIds.Waste, 6)),
                Outputs = Cost((CoreResourceIds.Fertilizer, 6))
            });
            registry.RegisterRecipe(new RecipeDefinition
            {
                RecipeId = CoreRecipeIds.RefineWasteFuel,
                BuildingDefinitionId = CoreBuildingIds.WasteProcessor,
                RequiredWorkTicks = GameTime.TicksPerGameDay * 5 / 3,
                MinimumWorkers = 1,
                Inputs = Cost((CoreResourceIds.Waste, 10)),
                Outputs = Cost((CoreResourceIds.Fuel, 3))
            });
        }

        private static void RegisterContinuousProduction(DefinitionRegistry registry)
        {
            registry.RegisterContinuousProduction(new ContinuousProductionDefinition
            {
                BuildingDefinitionId = CoreBuildingIds.Farm,
                OutputResourceId = CoreResourceIds.Food,
                OutputPerWorkerPerDay = 4,
                OperatingInputResourceId = CoreResourceIds.Water,
                OperatingInputPerDay = 1
            });
            registry.RegisterContinuousProduction(new ContinuousProductionDefinition
            {
                BuildingDefinitionId = CoreBuildingIds.Well,
                OutputResourceId = CoreResourceIds.Water,
                OutputPerWorkerPerDay = 4
            });
            registry.RegisterContinuousProduction(new ContinuousProductionDefinition
            {
                BuildingDefinitionId = CoreBuildingIds.TreeFarm,
                OutputResourceId = CoreResourceIds.Wood,
                OutputPerWorkerPerDay = 3
            });
            registry.RegisterContinuousProduction(new ContinuousProductionDefinition
            {
                BuildingDefinitionId = CoreBuildingIds.ExcavationSite,
                OutputResourceId = CoreResourceIds.IronOre,
                OutputPerWorkerPerDay = 2,
                AdditionalOutputsPerWorkerPerDay = Cost((CoreResourceIds.Stone, 1))
            });
        }

        private static Dictionary<string, int> Cost(params (string ResourceId, int Amount)[] entries)
        {
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < entries.Length; index++)
            {
                result.Add(entries[index].ResourceId, entries[index].Amount);
            }
            return result;
        }

        private static void RegisterResources(DefinitionRegistry registry)
        {
            RegisterResource(registry, CoreResourceIds.Food, ResourceCategory.Basic);
            RegisterResource(registry, CoreResourceIds.Water, ResourceCategory.Basic);
            RegisterResource(registry, CoreResourceIds.Wood, ResourceCategory.Basic);
            RegisterResource(registry, CoreResourceIds.Stone, ResourceCategory.Basic);
            RegisterResource(registry, CoreResourceIds.Waste, ResourceCategory.Basic);
            RegisterResource(registry, CoreResourceIds.IronOre, ResourceCategory.Mineral);
            RegisterResource(registry, CoreResourceIds.CopperOre, ResourceCategory.Mineral);
            RegisterResource(registry, CoreResourceIds.CrystalOre, ResourceCategory.Mineral);
            RegisterResource(registry, CoreResourceIds.IronIngot, ResourceCategory.Mineral);
            RegisterResource(registry, CoreResourceIds.CopperIngot, ResourceCategory.Mineral);
            RegisterResource(registry, CoreResourceIds.CrystalShard, ResourceCategory.Rare);
            RegisterResource(registry, CoreResourceIds.CrystalEnergyGem, ResourceCategory.Rare);
            RegisterResource(registry, CoreResourceIds.Starlight, ResourceCategory.Extended, true, true);
            RegisterResource(registry, CoreResourceIds.Fertilizer, ResourceCategory.Extended);
            RegisterResource(registry, CoreResourceIds.Fuel, ResourceCategory.Extended);
            RegisterResource(registry, CoreResourceIds.TechnologyPoint, ResourceCategory.Extended, false, false);
        }

        private static void RegisterResource(
            DefinitionRegistry registry,
            string resourceId,
            ResourceCategory category,
            bool isStorable = true,
            bool isCurrency = false)
        {
            registry.RegisterResource(new ResourceDefinition
            {
                ResourceId = resourceId,
                Category = category,
                IsStorable = isStorable,
                IsCurrency = isCurrency
            });
        }
    }

    public static class CoreResourceIds
    {
        public const string Food = "resource:core:food";
        public const string Water = "resource:core:water";
        public const string Wood = "resource:core:wood";
        public const string Stone = "resource:core:stone";
        public const string Waste = "resource:core:waste";
        public const string IronOre = "resource:core:iron_ore";
        public const string CopperOre = "resource:core:copper_ore";
        public const string CrystalOre = "resource:core:crystal_ore";
        public const string IronIngot = "resource:core:iron_ingot";
        public const string CopperIngot = "resource:core:copper_ingot";
        public const string CrystalShard = "resource:core:crystal_shard";
        public const string CrystalEnergyGem = "resource:core:crystal_energy_gem";
        public const string Starlight = "resource:core:starlight";
        public const string Fertilizer = "resource:core:fertilizer";
        public const string Fuel = "resource:core:fuel";
        public const string TechnologyPoint = "resource:core:technology_point";
    }

    public static class CoreBuildingIds
    {
        public const string House = "building:core:house";
        public const string Farm = "building:core:farm";
        public const string Well = "building:core:well";
        public const string TreeFarm = "building:core:tree_farm";
        public const string ExcavationSite = "building:core:excavation_site";
        public const string Smelter = "building:core:smelter";
        public const string Warehouse = "building:core:warehouse";
        public const string WasteProcessor = "building:core:waste_processor";
    }

    public static class CoreRecipeIds
    {
        public const string EmergencyCharcoal = "recipe:core:emergency_charcoal";
        public const string SmeltIron = "recipe:core:smelt_iron";
        public const string SmeltCopper = "recipe:core:smelt_copper";
        public const string DestroyWaste = "recipe:core:destroy_waste";
        public const string RecycleWaste = "recipe:core:recycle_waste";
        public const string CompostWaste = "recipe:core:compost_waste";
        public const string RefineWasteFuel = "recipe:core:refine_waste_fuel";
    }

    public static class RuntimeComposition
    {
        public static DefinitionRegistry CreateDefinitions(IEnumerable<IDefinitionModule> additionalModules = null)
        {
            DefinitionRegistry registry = new DefinitionRegistry();
            HashSet<string> moduleIds = new HashSet<string>(StringComparer.Ordinal);
            RegisterModule(registry, moduleIds, new CoreContentDefinitionModule());

            if (additionalModules != null)
            {
                foreach (IDefinitionModule module in additionalModules)
                {
                    RegisterModule(registry, moduleIds, module);
                }
            }

            registry.ValidateReferencesAndSeal();
            return registry;
        }

        public static Simulation CreateSimulation(
            GameState state,
            DefinitionRegistry definitions,
            JsonSerializerOptions jsonOptions = null)
        {
            JsonSerializerOptions options = jsonOptions ?? SaveSystem.CreateDefaultJsonOptions();
            Simulation simulation = new Simulation(state, definitions, options);
            simulation.AddSystem(new BuildingSystem(options));
            simulation.AddSystem(new NpcLifecycleSystem());
            simulation.AddSystem(new HousingSystem(options));
            simulation.AddSystem(new WorkerAssignmentSystem(options));
            simulation.AddSystem(new ContinuousProductionSystem());
            simulation.AddSystem(new NpcSurvivalSystem());
            simulation.AddSystem(new ProductionSystem(options));
            simulation.AddSystem(new WasteGenerationSystem());
            simulation.AddSystem(new LogisticsSystem(options));
            return simulation;
        }

        public static LocalGameSession CreateLocalSession(
            GameState state,
            DefinitionRegistry definitions,
            JsonSerializerOptions jsonOptions = null)
        {
            return new LocalGameSession(CreateSimulation(state, definitions, jsonOptions));
        }

        public static ServerGameSession CreateServerSession(
            GameState state,
            DefinitionRegistry definitions,
            IEnumerable<string> authorizedPlayerIds,
            JsonSerializerOptions jsonOptions = null)
        {
            return new ServerGameSession(
                CreateSimulation(state, definitions, jsonOptions),
                authorizedPlayerIds);
        }

        private static void RegisterModule(
            DefinitionRegistry registry,
            HashSet<string> moduleIds,
            IDefinitionModule module)
        {
            if (module == null)
            {
                throw new ArgumentException("Definition modules cannot contain null entries.", nameof(module));
            }
            if (!StableId.IsValid(module.ModuleId))
            {
                throw new ArgumentException("Definition module id must use namespace:type:id format.", nameof(module));
            }
            if (!moduleIds.Add(module.ModuleId))
            {
                throw new InvalidOperationException($"Definition module {module.ModuleId} is already registered.");
            }

            module.RegisterDefinitions(registry);
        }
    }
}
