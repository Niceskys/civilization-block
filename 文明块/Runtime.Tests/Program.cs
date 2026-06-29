using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WenMingBlocks.Runtime.Authority;

namespace WenMingBlocks.Runtime.Tests
{
    internal static class Program
    {
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            Run("StableId validates namespace:type:id", StableIdValidation);
            Run("StableId rejects empty, whitespace, and missing namespace", StableIdRejectsEdgeCases);
            Run("ModRegistry rejects invalid manifests", ModRegistryRejectsInvalidManifest);
            Run("ModRegistry accepts dependencies in load order", ModRegistryAcceptsDependencies);
            Run("ModRegistry rejects duplicate ModId", ModRegistryRejectsDuplicateModId);
            Run("Mod compatibility accepts identical profiles", ModCompatibilityAcceptsIdenticalProfiles);
            Run("Mod compatibility reports protocol and game version mismatch", ModCompatibilityReportsRuntimeMismatch);
            Run("Mod compatibility reports missing and unexpected mods", ModCompatibilityReportsMissingAndUnexpectedMods);
            Run("Mod compatibility reports mod detail mismatches", ModCompatibilityReportsModDetailMismatches);
            Run("Mod registry compatibility profile is a snapshot", ModRegistryCompatibilityProfileIsSnapshot);
            Run("Transport blocks state exchange before compatible handshake", TransportBlocksStateBeforeHandshake);
            Run("Remote session rejects incompatible handshake", RemoteSessionRejectsIncompatibleHandshake);
            Run("StateDiagnostics reports negative resources", StateDiagnosticsReportsNegativeResources);
            Run("StateDiagnostics reports resource storage targets", StateDiagnosticsReportsResourceStorageTargets);
            Run("StateDiagnostics detects duplicate building layers", StateDiagnosticsDetectsDuplicateBuildingLayers);
            Run("StateDiagnostics handles empty state without exception", StateDiagnosticsHandlesEmptyState);
            Run("StateDiagnostics reports local inventory target", StateDiagnosticsReportsLocalInventoryTarget);
            Run("Game time resolves pause and speed deterministically", GameTimeResolvesPauseAndSpeedDeterministically);
            Run("Day night cycle derives phase from simulation tick", DayNightCycleDerivesPhaseFromSimulationTick);
            Run("Difficulty profiles resolve structural timings", DifficultyProfilesResolveStructuralTimings);
            Run("Custom difficulty supports disable-only failure mode", CustomDifficultySupportsDisableOnlyMode);
            Run("Structural grace deadline follows simulation speed", StructuralGraceDeadlineFollowsSimulationSpeed);
            Run("Save migration defaults legacy difficulty to normal", SaveMigrationDefaultsLegacyDifficultyToNormal);
            Run("Invalid custom difficulty fails save load and diagnostics", InvalidCustomDifficultyFailsLoadAndDiagnostics);
            Run("LocalGameSession sends commands through Simulation", LocalGameSessionSendsCommandThroughSimulation);
            Run("DefinitionRegistry rejects null building", DefinitionRegistryRejectsNullBuilding);
            Run("DefinitionRegistry rejects invalid building DefinitionId", DefinitionRegistryRejectsInvalidId);
            Run("DefinitionRegistry registers and retrieves valid building", DefinitionRegistryRegistersAndRetrievesValidBuilding);
            Run("DefinitionRegistry accepts bounded solid footprints", DefinitionRegistryAcceptsBoundedSolidFootprints);
            Run("DefinitionRegistry rejects unsupported occupancy kinds", DefinitionRegistryRejectsUnsupportedOccupancyKinds);
            Run("DefinitionRegistry snapshots, seals, and rejects duplicates", DefinitionRegistrySnapshotsSealsAndRejectsDuplicates);
            Run("Core content registers authoritative pipe definition", CoreContentRegistersPipeDefinition);
            Run("Core content registers all authoritative resources", CoreContentRegistersResources);
            Run("Core content registers initial building definitions", CoreContentRegistersInitialBuildings);
            Run("Core content registers emergency charcoal recipe", CoreContentRegistersEmergencyCharcoal);
            Run("Core content registers excavation and smelter chain", CoreContentRegistersExcavationAndSmelterChain);
            Run("Core content registers warehouse capacity", CoreContentRegistersWarehouseCapacity);
            Run("Core content registers waste processor modes", CoreContentRegistersWasteProcessorModes);
            Run("Core initial buildings apply first-build bonuses", CoreInitialBuildingsApplyFirstBuildBonuses);
            Run("Warehouse construction and demolition update shared capacity", WarehouseLifecycleUpdatesSharedCapacity);
            Run("Warehouse demolition rejects required capacity", WarehouseDemolitionRejectsRequiredCapacity);
            Run("Shared capacity blocks continuous global output", SharedCapacityBlocksContinuousGlobalOutput);
            Run("Shared capacity migration defaults legacy saves", SharedCapacityMigrationDefaultsLegacySaves);
            Run("Shared overcapacity survives save and reports warning", SharedOvercapacitySurvivesSaveAndReportsWarning);
            Run("Remote warehouse capacity uses server authority", RemoteWarehouseCapacityUsesServerAuthority);
            Run("Continuous production yields exact daily output", ContinuousProductionYieldsExactDailyOutput);
            Run("Farm continuous production consumes irrigation", FarmContinuousProductionConsumesIrrigation);
            Run("Farm light occlusion pauses continuous production", FarmLightOcclusionPausesContinuousProduction);
            Run("Farm light sunlamp restores continuous production", FarmLightSunlampRestoresContinuousProduction);
            Run("Farm light destroyed sunlamp does not restore production", FarmLightDestroyedSunlampDoesNotRestoreProduction);
            Run("Farm light paused status survives save round trip", FarmLightPausedStatusSurvivesSaveRoundTrip);
            Run("Farm light restoration does not backfill paused ticks", FarmLightRestorationDoesNotBackfillPausedTicks);
            Run("Farm light night without sunlamp pauses continuous production", FarmLightNightWithoutSunlampPausesContinuousProduction);
            Run("Farm light day night split only produces daylight", FarmLightDayNightSplitOnlyProducesDaylight);
            Run("Farm light night day split does not backfill night", FarmLightNightDaySplitDoesNotBackfillNight);
            Run("Farm light night sunlamp supports production", FarmLightNightSunlampSupportsProduction);
            Run("Farm light sunlamp consumes fuel coverage", FarmLightSunlampConsumesFuelCoverage);
            Run("Farm light shared sunlamp charges by time not farm count", FarmLightSharedSunlampChargesByTimeNotFarmCount);
            Run("Farm light unfueled sunlamp does not restore night production", FarmLightUnfueledSunlampDoesNotRestoreNightProduction);
            Run("Farm light sunlamp fuel survives save round trip", FarmLightSunlampFuelSurvivesSaveRoundTrip);
            Run("Agricultural light only affects farm production", AgriculturalLightOnlyAffectsFarmProduction);
            Run("Continuous production pauses without conditions", ContinuousProductionPausesWithoutConditions);
            Run("Continuous production preserves pending output", ContinuousProductionPreservesPendingOutput);
            Run("Continuous production survives save round trip", ContinuousProductionSurvivesSaveRoundTrip);
            Run("Excavation produces iron ore and stone", ExcavationProducesIronOreAndStone);
            Run("Excavation preserves blocked stone byproduct", ExcavationPreservesBlockedStoneByproduct);
            Run("Excavation byproduct survives save round trip", ExcavationByproductSurvivesSaveRoundTrip);
            Run("Save migration initializes continuous byproducts", SaveMigrationInitializesContinuousByproducts);
            Run("Remote continuous production uses server authority", RemoteContinuousProductionUsesServerAuthority);
            Run("Remote farm light uses server authority", RemoteFarmLightUsesServerAuthority);
            Run("Remote farm night light uses server authority", RemoteFarmNightLightUsesServerAuthority);
            Run("Remote farm sunlamp fuel uses server authority", RemoteFarmSunlampFuelUsesServerAuthority);
            Run("Definition sealing rejects broken cross references", DefinitionSealingRejectsBrokenReferences);
            Run("Definition sealing accepts complete content module", DefinitionSealingAcceptsCompleteModule);
            Run("Runtime composition registers every core command system", RuntimeCompositionRegistersCoreSystems);
            Run("Runtime composition creates local and server sessions", RuntimeCompositionCreatesSessions);
            Run("Save migration upgrades legacy placement snapshots", SaveMigrationUpgradesLegacyPlacementSnapshots);
            Run("Save migration rejects unsupported placement schema", SaveMigrationRejectsUnsupportedPlacementSchema);
            Run("Save migration rejects footprint outside plot", SaveMigrationRejectsFootprintOutsidePlot);
            Run("Spatial placement survives save round trip and hash", SpatialPlacementSurvivesSaveRoundTrip);
            Run("Spatial occupancy emits deterministic multi-layer cells", SpatialOccupancyEmitsDeterministicCells);
            Run("Spatial occupancy rotates rectangular footprints", SpatialOccupancyRotatesRectangularFootprints);
            Run("Spatial occupancy accepts adjacent placement", SpatialOccupancyAcceptsAdjacentPlacement);
            Run("Spatial occupancy reports overlap details", SpatialOccupancyReportsOverlapDetails);
            Run("Spatial occupancy treats construction reservations as occupied", SpatialOccupancyTreatsReservationsAsOccupied);
            Run("Spatial occupancy rejects bounds violations", SpatialOccupancyRejectsBoundsViolations);
            Run("Spatial occupancy rejects invalid shape and rotation", SpatialOccupancyRejectsInvalidShapeAndRotation);
            Run("Spatial occupancy rejects excessive footprints", SpatialOccupancyRejectsExcessiveFootprints);
            Run("Spatial occupancy rejects coordinate overflow", SpatialOccupancyRejectsCoordinateOverflow);
            Run("Spatial occupancy rejects corrupt existing state", SpatialOccupancyRejectsCorruptExistingState);
            Run("Structural support accepts grounded node", StructuralSupportAcceptsGroundedNode);
            Run("Structural support rejects unsupported node", StructuralSupportRejectsUnsupportedNode);
            Run("Structural support accepts exact half contact", StructuralSupportAcceptsExactHalfContact);
            Run("Structural support rejects contact below threshold", StructuralSupportRejectsContactBelowThreshold);
            Run("Structural support distributes common load deterministically", StructuralSupportDistributesCommonLoadDeterministically);
            Run("Structural support propagates multi-layer load", StructuralSupportPropagatesMultiLayerLoad);
            Run("Structural support rejects exceeded capacity", StructuralSupportRejectsExceededCapacity);
            Run("Structural support does not use construction as support", StructuralSupportDoesNotUseConstructionAsSupport);
            Run("Structural support rejects overlapping nodes", StructuralSupportRejectsOverlappingNodes);
            Run("Structural support ignores registration order", StructuralSupportIgnoresRegistrationOrder);
            Run("Building system places rotated variable footprint", BuildingSystemPlacesRotatedVariableFootprint);
            Run("Building system rejects overlap with stable code", BuildingSystemRejectsOverlapWithStableCode);
            Run("Building system rejects plot boundary crossing", BuildingSystemRejectsPlotBoundaryCrossing);
            Run("Building system rejects partial anchor coordinates", BuildingSystemRejectsPartialAnchorCoordinates);
            Run("Construction completion preserves spatial snapshot", ConstructionCompletionPreservesSpatialSnapshot);
            Run("Save migration accepts valid variable footprint", SaveMigrationAcceptsValidVariableFootprint);
            Run("Save migration rejects overlapping spatial state", SaveMigrationRejectsOverlappingSpatialState);
            Run("State diagnostics accepts adjacent same-layer buildings", StateDiagnosticsAcceptsAdjacentSameLayerBuildings);
            Run("Remote building uses authoritative spatial validation", RemoteBuildingUsesAuthoritativeSpatialValidation);
            Run("Building rejects support from unfinished construction", BuildingRejectsSupportFromUnfinishedConstruction);
            Run("Building accepts support after construction completes", BuildingAcceptsSupportAfterConstructionCompletes);
            Run("Building accepts deterministic common support", BuildingAcceptsDeterministicCommonSupport);
            Run("Building rejects structural capacity overflow", BuildingRejectsStructuralCapacityOverflow);
            Run("Demolition starts structural grace", DemolitionStartsStructuralGrace);
            Run("Repair before deadline restores structural stability", RepairBeforeDeadlineRestoresStability);
            Run("Automatic collapse uses deterministic highest-first order", AutomaticCollapseUsesDeterministicOrder);
            Run("Disable-only mode preserves expired buildings", DisableOnlyModePreservesBuildings);
            Run("Large tick processes structural collapse intervals", LargeTickProcessesCollapseIntervals);
            Run("Structural incident survives save round trip", StructuralIncidentSurvivesSaveRoundTrip);
            Run("Remote demolition synchronizes structural grace", RemoteDemolitionSynchronizesGrace);
            Run("Demolition refunds 75 percent of paid cost", DemolitionRefundsPaidCost);
            Run("Accelerated construction refunds actual doubled cost", AcceleratedConstructionRefundsActualCost);
            Run("Demolition reports refund overflow", DemolitionReportsRefundOverflow);
            Run("Structural collapse never refunds resources", StructuralCollapseNeverRefundsResources);
            Run("Operational rules consistently block unusable buildings", OperationalRulesBlockUnusableBuildings);
            Run("Worker assignment uses deterministic slots", WorkerAssignmentUsesDeterministicSlots);
            Run("Worker reassignment is atomic", WorkerReassignmentIsAtomic);
            Run("Worker assignment rejects unavailable NPC and full building", WorkerAssignmentRejectsInvalidCandidates);
            Run("Building demolition immediately releases workers", BuildingDemolitionReleasesWorkers);
            Run("Worker assignment survives save round trip", WorkerAssignmentSurvivesSaveRoundTrip);
            Run("Remote worker assignment uses server authority", RemoteWorkerAssignmentUsesServerAuthority);
            Run("Housing assigns adults in deterministic order", HousingAssignsAdultsDeterministically);
            Run("Housing excludes infants and tracks homeless adults", HousingExcludesInfantsAndTracksHomeless);
            Run("Manual housing reassignment is preserved", ManualHousingReassignmentIsPreserved);
            Run("Inoperable housing relocates residents", InoperableHousingRelocatesResidents);
            Run("Housing survives save round trip", HousingSurvivesSaveRoundTrip);
            Run("NPC infant grows at exact lifecycle boundary", NpcInfantGrowsAtExactBoundary);
            Run("NPC lifespan is deterministic and bounded", NpcLifespanIsDeterministicAndBounded);
            Run("NPC adult dies at natural lifespan", NpcAdultDiesAtNaturalLifespan);
            Run("NPC lifecycle processes growth and death in large tick", NpcLifecycleProcessesGrowthAndDeathInLargeTick);
            Run("NPC death releases housing and work", NpcDeathReleasesHousingAndWork);
            Run("NPC lifecycle keeps survival tick splitting invariant", NpcLifecycleKeepsSurvivalTickSplittingInvariant);
            Run("NPC lifecycle survives save round trip", NpcLifecycleSurvivesSaveRoundTrip);
            Run("Remote NPC lifecycle uses server authority", RemoteNpcLifecycleUsesServerAuthority);
            Run("NPC survival consumes adult daily needs", NpcSurvivalConsumesAdultNeeds);
            Run("NPC survival carries fractional needs", NpcSurvivalCarriesFractionalNeeds);
            Run("NPC survival includes infant needs", NpcSurvivalIncludesInfantNeeds);
            Run("NPC survival respects resource locks and shortages", NpcSurvivalRespectsLocksAndShortages);
            Run("NPC survival processes large ticks deterministically", NpcSurvivalProcessesLargeTicks);
            Run("NPC survival survives save round trip", NpcSurvivalSurvivesSaveRoundTrip);
            Run("NPC survival migration starts after current day", NpcSurvivalMigrationStartsAfterCurrentDay);
            Run("Remote NPC survival uses server authority", RemoteNpcSurvivalUsesServerAuthority);
            Run("Production locks inputs and blocks resource reuse", ProductionLocksInputsAndBlocksReuse);
            Run("Production pauses and resumes with workers", ProductionPausesAndResumesWithWorkers);
            Run("Production cancel unlocks without consuming", ProductionCancelUnlocksWithoutConsuming);
            Run("Production output waits for whole-batch storage", ProductionOutputWaitsForWholeBatchStorage);
            Run("Continuous production starts exactly one next batch", ContinuousProductionStartsOneNextBatch);
            Run("Production combines global and local inputs", ProductionCombinesGlobalAndLocalInputs);
            Run("Production survives save round trip", ProductionSurvivesSaveRoundTrip);
            Run("Formal smelter consumes ore and fuel atomically", FormalSmelterConsumesOreAndFuelAtomically);
            Run("Waste mode A destroys ten waste", WasteModeADestroysTenWaste);
            Run("Waste mode B recovery is deterministic", WasteModeBRecoveryIsDeterministic);
            Run("Waste mode C produces fertilizer", WasteModeCProducesFertilizer);
            Run("Waste mode D preserves fuel ratio", WasteModeDPreservesFuelRatio);
            Run("Waste processor modes are mutually exclusive", WasteProcessorModesAreMutuallyExclusive);
            Run("Remote waste recovery uses server authority", RemoteWasteRecoveryUsesServerAuthority);
            Run("Waste generation carries NPC half units exactly", WasteGenerationCarriesNpcHalfUnits);
            Run("Idle buildings do not generate waste", IdleBuildingsDoNotGenerateWaste);
            Run("Waste overflow respects category and shared capacity", WasteOverflowRespectsCapacity);
            Run("Waste thresholds accumulate and clear on schedule", WasteThresholdsAccumulateAndClear);
            Run("NPC death generates one immediate waste", NpcDeathGeneratesImmediateWaste);
            Run("Waste state migrates and survives save round trip", WasteStateMigratesAndSurvivesSave);
            Run("Remote waste generation uses server authority", RemoteWasteGenerationUsesServerAuthority);
            Run("Waste penalty resolves NPC satisfaction", WastePenaltyResolvesNpcSatisfaction);
            Run("Waste penalty slows batch production exactly", WastePenaltySlowsBatchProductionExactly);
            Run("Waste penalty slows continuous production exactly", WastePenaltySlowsContinuousProductionExactly);
            Run("Waste disease exposure is deterministic", WasteDiseaseExposureIsDeterministic);
            Run("Waste effects migrate and synchronize remotely", WasteEffectsMigrateAndSynchronizeRemotely);
            Run("Fertilizer boosts one farm cycle", FertilizerBoostsOneFarmCycle);
            Run("Fertilizer is invariant to tick splitting", FertilizerIsInvariantToTickSplitting);
            Run("Fertilizer rejects invalid application", FertilizerRejectsInvalidApplication);
            Run("Fertilizer state migrates and synchronizes remotely", FertilizerStateMigratesAndSynchronizesRemotely);
            Run("Remote production uses server authority", RemoteProductionUsesServerAuthority);
            Run("Logistics locks source and reserves target capacity", LogisticsLocksAndReservesCapacity);
            Run("Logistics transfers from global warehouse to building", LogisticsTransfersGlobalToBuilding);
            Run("Logistics reservation blocks competing task", LogisticsReservationBlocksCompetition);
            Run("Logistics cancel releases lock and reservation", LogisticsCancelReleasesState);
            Run("Cross-layer logistics requires route and exact delay", CrossLayerLogisticsRequiresRoute);
            Run("Logistics building failure cancels active task", LogisticsFailureCancelsTask);
            Run("Active logistics blocks normal demolition", ActiveLogisticsBlocksDemolition);
            Run("Logistics survives save round trip", LogisticsSurvivesSaveRoundTrip);
            Run("Remote logistics uses server authority", RemoteLogisticsUsesServerAuthority);
            Run("Connector construction creates directed resource route", ConnectorConstructionCreatesRoute);
            Run("Connector placement rejects invalid and duplicate endpoints", ConnectorPlacementRejectsInvalidAndDuplicate);
            Run("Connector automatically transfers one configured batch", ConnectorAutomaticallyTransfersConfiguredBatch);
            Run("Connector route rejects reverse and wrong-resource transport", ConnectorRouteRejectsInvalidTransport);
            Run("Connector demolition cancels transport and removes route", ConnectorDemolitionCleansRuntimeState);
            Run("Connector pauses and resumes with endpoint operation", ConnectorPausesAndResumesWithEndpoint);
            Run("Connector survives save round trip", ConnectorSurvivesSaveRoundTrip);
            Run("Remote connector construction uses server authority", RemoteConnectorConstructionUsesServerAuthority);
            Run("State diagnostics reports unsupported structure", StateDiagnosticsReportsUnsupportedStructure);
            Run("State diagnostics reports structural overload", StateDiagnosticsReportsStructuralOverload);
            Run("State diagnostics reports farm missing light", StateDiagnosticsReportsFarmMissingLight);
            Run("State diagnostics reports sunlamp fuel target", StateDiagnosticsReportsSunlampFuelTarget);
            Run("State diagnostics reports continuous production targets", StateDiagnosticsReportsContinuousProductionTargets);
            Run("State diagnostics reports batch production targets", StateDiagnosticsReportsBatchProductionTargets);
            Run("State diagnostics reports logistics targets", StateDiagnosticsReportsLogisticsTargets);
            Run("Remote preserves structural rejection code", RemotePreservesStructuralRejectionCode);
            Run("EventStream publishes event and triggers callback", EventStreamPublishesEventAndTriggersCallback);
            Run("Server session rejects unauthorized player", ServerSessionRejectsUnauthorizedPlayer);
            Run("Remote session submits command through server authority", RemoteSessionSubmitsThroughServerAuthority);
            Run("Remote session tick synchronizes without advancing server", RemoteSessionTickDoesNotAdvanceServer);
            Run("Remote snapshot is isolated and events are not replayed", RemoteSnapshotIsIsolatedAndEventsAreNotReplayed);
            Run("Remote detects local state drift and repairs from server", RemoteDetectsAndRepairsLocalStateDrift);
            Run("Remote normal synchronization does not report repair", RemoteNormalSynchronizationDoesNotReportRepair);
            Run("Remote retries one authoritative snapshot after transport corruption", RemoteRetriesAfterTransportCorruption);
            Run("Remote reconnect resumes cursor without replay", RemoteReconnectResumesCursorWithoutReplay);
            Run("Remote reconnect preserves player command sequence", RemoteReconnectPreservesCommandSequence);
            Run("Server session revoke rejects command and preserves state", ServerSessionRevokeRejectsCommandAndPreservesState);
            Run("Remote session rejects unauthorized player without side effects", RemoteSessionRejectsUnauthorizedWithoutSideEffects);
            Run("Remote command events isolated from backlog, ordered, not replayed", RemoteCommandEventsIsolatedFromBacklog);

            Run("Agricultural light Lv1 sunlamp generates 15 coverage cells", AgriculturalLightLv1SunlampGenerates15CoverageCells);
            Run("Agricultural light coverage extends downward not upward", AgriculturalLightCoverageExtendsDownwardNotUpward);
            Run("Agricultural light clips negative layers at base layer zero", AgriculturalLightClipsNegativeLayersAtBaseLayerZero);
            Run("Agricultural light clips horizontal range at plot edge", AgriculturalLightClipsHorizontalRangeAtPlotEdge);
            Run("Agricultural light completed building occludes farm above", AgriculturalLightCompletedBuildingOccludesFarmAbove);
            Run("Agricultural light destroyed building does not occlude", AgriculturalLightDestroyedBuildingDoesNotOcclude);
            Run("Agricultural light grace building still occludes", AgriculturalLightGraceBuildingStillOccludes);
            Run("Agricultural light disabled building still occludes", AgriculturalLightDisabledBuildingStillOccludes);
            Run("Agricultural light construction task does not occlude", AgriculturalLightConstructionTaskDoesNotOcclude);
            Run("Agricultural light occluded farm recovers with active sunlamp coverage", AgriculturalLightOccludedFarmRecoversWithActiveSunlampCoverage);
            Run("Agricultural light occluded farm without active sunlamp has no light", AgriculturalLightOccludedFarmWithoutActiveSunlampHasNoLight);
            Run("Agricultural light sunlamp in different plot does not cover", AgriculturalLightSunlampInDifferentPlotDoesNotCover);
            Run("Agricultural light overlapping multiple sunlamps do not duplicate", AgriculturalLightOverlappingMultipleSunlampsDoNotDuplicate);
            Run("Agricultural light multi-cell farm partial coverage still no light", AgriculturalLightMultiCellFarmPartialCoverageStillNoLight);
            Run("Agricultural light multi-cell farm full coverage has light", AgriculturalLightMultiCellFarmFullCoverageHasLight);
            Run("Agricultural light sunlamp self-occludes but provides full coverage has light", AgriculturalLightSunlampSelfOccludesButProvidesFullCoverageHasLight);
            Run("Agricultural light rotated multi-cell farm uses authoritative footprint", AgriculturalLightRotatedMultiCellFarmUsesAuthoritativeFootprint);
            Run("Agricultural light excludes farm itself by building id", AgriculturalLightExcludesFarmItselfByBuildingId);
            Run("Agricultural light rejects null building entry explicitly", AgriculturalLightRejectsNullBuildingEntryExplicitly);
            Run("Agricultural light rejects null active sunlamp entry explicitly", AgriculturalLightRejectsNullActiveSunlampEntryExplicitly);
            Run("Agricultural light coverage avoids coordinate overflow at plot maximum", AgriculturalLightCoverageAvoidsCoordinateOverflowAtPlotMaximum);
            Run("Agricultural light rejects trailing null building after occluder", AgriculturalLightRejectsTrailingNullBuildingAfterOccluder);
            Run("Agricultural light rejects trailing null sunlamp after coverage", AgriculturalLightRejectsTrailingNullSunlampAfterCoverage);
            Run("Agricultural light final query validates sunlamps before daylight short circuit", AgriculturalLightFinalQueryValidatesSunlampsBeforeDaylightShortCircuit);

            if (Failures.Count == 0)
            {
                Console.WriteLine("All runtime smoke tests passed.");
                return 0;
            }

            Console.WriteLine("Runtime smoke tests failed:");
            for (int i = 0; i < Failures.Count; i++)
            {
                Console.WriteLine("- " + Failures[i]);
            }

            return 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception exception)
            {
                Failures.Add(name + ": " + exception.Message);
                Console.WriteLine("[FAIL] " + name);
            }
        }

        private static void StableIdValidation()
        {
            AssertTrue(StableId.IsValid("building:core:farm"), "Expected valid stable id.");
            AssertFalse(StableId.IsValid("building:farm"), "Expected missing segment to be invalid.");
            AssertFalse(StableId.IsValid("building::farm"), "Expected empty segment to be invalid.");
        }

        private static void ModRegistryRejectsInvalidManifest()
        {
            ModLoadResult coreNamespace = ModRegistry.ValidateManifest(new ModManifest
            {
                ModId = "core",
                Name = "Bad Mod",
                Version = "1.0.0",
                MinGameVersion = "0.1.0",
                ContentTypes = new List<string> { "buildings" },
                Checksum = "abc"
            });

            AssertFalse(coreNamespace.Accepted, "Core namespace must be reserved.");

            ModLoadResult missingChecksum = ModRegistry.ValidateManifest(new ModManifest
            {
                ModId = "example_mod",
                Name = "Example",
                Version = "1.0.0",
                MinGameVersion = "0.1.0",
                ContentTypes = new List<string> { "buildings" }
            });

            AssertFalse(missingChecksum.Accepted, "Manifest without checksum must be rejected.");
        }

        private static void ModRegistryAcceptsDependencies()
        {
            ModRegistry registry = new ModRegistry();
            ModLoadResult libraryResult = registry.RegisterManifest(new ModManifest
            {
                ModId = "library_mod",
                Name = "Library",
                Version = "1.0.0",
                MinGameVersion = "0.1.0",
                ContentTypes = new List<string> { "items" },
                Checksum = "library-checksum"
            });

            AssertTrue(libraryResult.Accepted, libraryResult.Reason);

            ModLoadResult dependentResult = registry.RegisterManifest(new ModManifest
            {
                ModId = "dependent_mod",
                Name = "Dependent",
                Version = "1.0.0",
                MinGameVersion = "0.1.0",
                Dependencies = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "library_mod", ">=1.0.0" }
                },
                ContentTypes = new List<string> { "buildings" },
                Checksum = "dependent-checksum"
            });

            AssertTrue(dependentResult.Accepted, dependentResult.Reason);
            AssertEqual(2, registry.LoadedMods.Count, "Expected two loaded mods.");
            AssertEqual("dependent_mod", registry.LoadedMods[1].Manifest.ModId, "Expected dependent mod to load second.");
        }

        private static void StateDiagnosticsReportsNegativeResources()
        {
            GameState state = new GameState();
            state.Resources.Items["item:core:wood"] = new ResourceStack
            {
                ResourceId = "item:core:wood",
                Amount = -1,
                Capacity = 10
            };

            IReadOnlyList<DiagnosticIssue> issues = StateDiagnostics.CheckInvariants(state, new DefinitionRegistry());
            AssertTrue(issues.Any(issue => issue.Code == "resource.amount.negative"), "Expected negative resource diagnostic.");
            AssertFalse(string.IsNullOrWhiteSpace(StateDiagnostics.CalculateStateHash(state)), "Expected state hash.");
        }

        private static void StateDiagnosticsReportsResourceStorageTargets()
        {
            GameState state = new GameState();
            state.Resources.SharedCapacity = 1;
            state.Resources.Items[CoreResourceIds.Wood] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Wood,
                Amount = 2,
                Capacity = 1
            };

            string beforeHash = StateDiagnostics.CalculateStateHash(state);
            IReadOnlyList<DiagnosticIssue> issues =
                StateDiagnostics.CheckInvariants(state, RuntimeComposition.CreateDefinitions());
            string afterHash = StateDiagnostics.CalculateStateHash(state);

            DiagnosticIssue resourceIssue = issues.Single(item => item.Code == "resource.amount.over_capacity");
            AssertEqual(CoreResourceIds.Wood, resourceIssue.TargetIds.Single(),
                "Resource over-capacity diagnostic must expose the resource target id.");
            AssertEqual(85, resourceIssue.Priority.GetValueOrDefault(),
                "Resource over-capacity diagnostic must expose stable display priority.");
            AssertEqual("resource_storage", resourceIssue.SourceSystem,
                "Resource over-capacity diagnostic must expose its source system.");

            DiagnosticIssue sharedIssue = issues.Single(item => item.Code == "resource.shared_capacity.exceeded");
            AssertEqual(0, sharedIssue.TargetIds.Count,
                "Shared-capacity diagnostic must not invent a building target.");
            AssertEqual(85, sharedIssue.Priority.GetValueOrDefault(),
                "Shared-capacity diagnostic must expose stable display priority.");
            AssertEqual("resource_storage", sharedIssue.SourceSystem,
                "Shared-capacity diagnostic must expose its source system.");
            AssertEqual(beforeHash, afterHash,
                "Diagnostic display metadata must not mutate authoritative state hash.");
        }

        private static void StateDiagnosticsDetectsDuplicateBuildingLayers()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:farm",
                Category = "production"
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:000001"] = new PlotState
            {
                PlotId = "plot:core:000001"
            };
            state.Buildings.Instances["building:core:000001"] = new BuildingInstanceState
            {
                BuildingId = "building:core:000001",
                DefinitionId = "building:core:farm",
                PlotId = "plot:core:000001",
                Layer = 0,
                PlacedWidth = 1,
                PlacedDepth = 1,
                PlacedHeight = 1,
                PlacementSchemaVersion = SpatialPlacementSchema.CurrentVersion
            };
            state.Buildings.ConstructionTasks["construction:core:000001"] = new ConstructionTaskState
            {
                TaskId = "construction:core:000001",
                BuildingId = "building:core:000002",
                DefinitionId = "building:core:farm",
                PlotId = "plot:core:000001",
                Layer = 0,
                PlacedWidth = 1,
                PlacedDepth = 1,
                PlacedHeight = 1,
                PlacementSchemaVersion = SpatialPlacementSchema.CurrentVersion
            };

            IReadOnlyList<DiagnosticIssue> issues = StateDiagnostics.CheckInvariants(state, definitions);
            AssertTrue(issues.Any(issue => issue.Code == "building.layer.occupied"), "Expected duplicate layer diagnostic.");
        }

        private static void LocalGameSessionSendsCommandThroughSimulation()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:farm",
                Category = "production",
                ConstructionTicks = 1,
                BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:000001"] = new PlotState
            {
                PlotId = "plot:core:000001",
                X = 4,
                Y = -2,
                MaxStackLayers = 2
            };

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new BuildingSystem());
            LocalGameSession session = new LocalGameSession(simulation);

            CommandEnvelope command = new CommandEnvelope
            {
                CommandId = "command:core:000001",
                PlayerId = "player:core:local",
                Type = BuildingSystem.BuildCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildCommandPayload
                {
                    DefinitionId = "building:core:farm",
                    PlotId = "plot:core:000001",
                    Layer = 0
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = 1
            };

            CommandResult result = session.SendCommand(command);
            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(1, state.Buildings.ConstructionTasks.Count, "Expected construction task to be created.");
            ConstructionTaskState task = state.Buildings.ConstructionTasks.Values.Single();
            AssertEqual(4, task.AnchorX, "Task anchor X must come from the legacy plot coordinate.");
            AssertEqual(-2, task.AnchorY, "Task anchor Y must come from the legacy plot coordinate.");
            AssertEqual(0, task.BaseLayer, "Task base layer must mirror the legacy layer in Phase A.");
            AssertEqual(1, task.PlacedWidth, "Phase A task must snapshot a 1x1x1 footprint.");
            AssertEqual(SpatialPlacementSchema.CurrentVersion, task.PlacementSchemaVersion, "Task must use current placement schema.");
            AssertEqual(GameSessionMode.Local, session.Mode, "Expected local session mode.");
        }

        private static void StableIdRejectsEdgeCases()
        {
            AssertFalse(StableId.IsValid(""), "Expected empty string to be invalid.");
            AssertFalse(StableId.IsValid(" "), "Expected whitespace to be invalid.");
            AssertFalse(StableId.IsValid(":core:farm"), "Expected missing namespace to be invalid.");
        }

        private static void ModRegistryRejectsDuplicateModId()
        {
            ModRegistry registry = new ModRegistry();
            ModLoadResult first = registry.RegisterManifest(new ModManifest
            {
                ModId = "test_mod",
                Name = "Test",
                Version = "1.0.0",
                MinGameVersion = "0.1.0",
                ContentTypes = new List<string> { "buildings" },
                Checksum = "test-checksum"
            });

            AssertTrue(first.Accepted, first.Reason);
            AssertEqual(1, registry.LoadedMods.Count, "Expected one loaded mod after first registration.");

            ModLoadResult second = registry.RegisterManifest(new ModManifest
            {
                ModId = "test_mod",
                Name = "Test Duplicate",
                Version = "2.0.0",
                MinGameVersion = "0.1.0",
                ContentTypes = new List<string> { "items" },
                Checksum = "test-checksum-2"
            });

            AssertFalse(second.Accepted, "Expected duplicate ModId to be rejected.");
            AssertEqual(1, registry.LoadedMods.Count, "Expected loaded mod count to remain unchanged after duplicate.");
        }

        private static void StateDiagnosticsHandlesEmptyState()
        {
            GameState state = new GameState();
            DefinitionRegistry definitions = new DefinitionRegistry();

            IReadOnlyList<DiagnosticIssue> issues = StateDiagnostics.CheckInvariants(state, definitions);
            AssertTrue(issues != null, "Expected non-null diagnostic issues list from empty state.");

            string hash = StateDiagnostics.CalculateStateHash(state);
            AssertFalse(string.IsNullOrWhiteSpace(hash), "Expected non-empty state hash for empty state.");
        }

        private static void StateDiagnosticsReportsLocalInventoryTarget()
        {
            GameState state = new GameState();
            state.World.Plots["plot:test:inventory"] = new PlotState
            {
                PlotId = "plot:test:inventory",
                Width = 1,
                Depth = 1,
                MaxStackLayers = 1
            };
            BuildingInstanceState building = CreateBuildingStateWithDefinition(
                "building:test:inventory_full",
                CoreBuildingIds.Warehouse,
                "plot:test:inventory",
                0,
                0,
                0,
                1,
                1,
                1);
            building.Durability = 400;
            building.LocalInventoryCapacity = 1;
            building.LocalInventory[CoreResourceIds.Wood] = new LocalResourceStack
            {
                ResourceId = CoreResourceIds.Wood,
                Amount = 2
            };
            state.Buildings.Instances[building.BuildingId] = building;

            string beforeHash = StateDiagnostics.CalculateStateHash(state);
            IReadOnlyList<DiagnosticIssue> issues =
                StateDiagnostics.CheckInvariants(state, RuntimeComposition.CreateDefinitions());
            string afterHash = StateDiagnostics.CalculateStateHash(state);

            DiagnosticIssue issue = issues.Single(item => item.Code == "building.local_inventory.over_capacity");
            AssertEqual("building:test:inventory_full", issue.TargetIds.Single(),
                "Local inventory over-capacity diagnostic must expose the building target id.");
            AssertEqual(85, issue.Priority.GetValueOrDefault(),
                "Local inventory over-capacity diagnostic must expose stable display priority.");
            AssertEqual("building_inventory", issue.SourceSystem,
                "Local inventory over-capacity diagnostic must expose its source system.");
            AssertEqual(beforeHash, afterHash,
                "Diagnostic display metadata must not mutate authoritative state hash.");
        }

        private static void GameTimeResolvesPauseAndSpeedDeterministically()
        {
            AssertEqual(72, GameTime.NormalSpeedTicksPerRealSecond, "One normal-speed real second must advance 72 game seconds.");
            AssertEqual(0L, GameTime.ResolveElapsedTicks(10, 0), "Pause must not advance simulation ticks.");
            AssertEqual(720L, GameTime.ResolveElapsedTicks(10, 1), "Normal speed must use the authoritative tick ratio.");
            AssertEqual(1440L, GameTime.ResolveElapsedTicks(10, 2), "Double speed must advance twice as many ticks.");
            AssertEqual(GameTime.TicksPerGameDay, GameTime.ResolveElapsedTicks(20 * 60, 1), "Twenty normal-speed real minutes must equal one game day.");
        }

        private static void DayNightCycleDerivesPhaseFromSimulationTick()
        {
            AssertEqual(GameTime.TicksPerGameDay / 2, DayNightCycle.TicksPerHalfDay,
                "Day and night must each occupy half of one game day.");
            AssertEqual(DayNightPhase.Day, DayNightCycle.GetPhase(0),
                "Tick zero must be the start of daytime.");
            AssertEqual(DayNightPhase.Day, DayNightCycle.GetPhase(DayNightCycle.TicksPerHalfDay - 1),
                "The tick before the half-day boundary must still be daytime.");
            AssertEqual(DayNightPhase.Night, DayNightCycle.GetPhase(DayNightCycle.TicksPerHalfDay),
                "The half-day boundary must enter night.");
            AssertEqual(DayNightPhase.Night, DayNightCycle.GetPhase(GameTime.TicksPerGameDay - 1),
                "The tick before the next day must still be night.");
            AssertEqual(DayNightPhase.Day, DayNightCycle.GetPhase(GameTime.TicksPerGameDay),
                "The next day boundary must return to daytime.");
            AssertEqual(DayNightPhase.Night,
                DayNightCycle.GetPhase(GameTime.TicksPerGameDay + DayNightCycle.TicksPerHalfDay + 7),
                "Later days must derive the phase from tick modulo one game day.");
            AssertEqual(DayNightPhase.Day, DayNightCycle.GetPhase(GameTime.ResolveElapsedTicks(100, 0)),
                "Paused elapsed time must not move the phase.");
            AssertThrows<ArgumentOutOfRangeException>(
                () => DayNightCycle.GetPhase(-1),
                "Negative simulation ticks must be rejected.");
        }

        private static void DifficultyProfilesResolveStructuralTimings()
        {
            StructuralFailurePolicy easy = DifficultyProfiles.ResolveStructuralFailure(new DifficultyState { DifficultyId = DifficultyIds.Easy });
            StructuralFailurePolicy normal = DifficultyProfiles.ResolveStructuralFailure(new DifficultyState { DifficultyId = DifficultyIds.Normal });
            StructuralFailurePolicy hard = DifficultyProfiles.ResolveStructuralFailure(new DifficultyState { DifficultyId = DifficultyIds.Hard });
            StructuralFailurePolicy extreme = DifficultyProfiles.ResolveStructuralFailure(new DifficultyState { DifficultyId = DifficultyIds.Extreme });

            AssertEqual(GameTime.TicksFromRealMinutesAtNormalSpeed(60), easy.GraceTicks, "Easy grace must equal 60 minutes at normal speed.");
            AssertEqual(GameTime.TicksFromRealMinutesAtNormalSpeed(30), normal.GraceTicks, "Normal grace must equal 30 minutes at normal speed.");
            AssertEqual(GameTime.TicksFromRealMinutesAtNormalSpeed(15), hard.GraceTicks, "Hard grace must equal 15 minutes at normal speed.");
            AssertEqual(GameTime.TicksFromRealMinutesAtNormalSpeed(5), extreme.GraceTicks, "Extreme grace must equal 5 minutes at normal speed.");
            AssertEqual(GameTime.TicksFromRealSecondsAtNormalSpeed(10), easy.CollapseIntervalTicks, "Easy collapse interval must equal 10 seconds at normal speed.");
            AssertEqual(GameTime.TicksFromRealSecondsAtNormalSpeed(1), extreme.CollapseIntervalTicks, "Extreme collapse interval must equal 1 second at normal speed.");
        }

        private static void CustomDifficultySupportsDisableOnlyMode()
        {
            DifficultyState difficulty = new DifficultyState
            {
                DifficultyId = DifficultyIds.Custom,
                CustomStructuralGraceTicks = GameTime.TicksFromRealMinutesAtNormalSpeed(45),
                CustomCollapseIntervalTicks = GameTime.TicksFromRealSecondsAtNormalSpeed(7),
                CustomStructuralFailureMode = StructuralFailureModes.DisableOnly
            };

            StructuralFailurePolicy policy = DifficultyProfiles.ResolveStructuralFailure(difficulty);

            AssertEqual(difficulty.CustomStructuralGraceTicks, policy.GraceTicks, "Custom grace must be preserved exactly.");
            AssertEqual(difficulty.CustomCollapseIntervalTicks, policy.CollapseIntervalTicks, "Custom collapse interval must be preserved exactly.");
            AssertEqual(StructuralFailureModes.DisableOnly, policy.FailureMode, "Custom difficulty must support non-destructive structural failure.");
        }

        private static void StructuralGraceDeadlineFollowsSimulationSpeed()
        {
            StructuralFailurePolicy policy = DifficultyProfiles.ResolveStructuralFailure(new DifficultyState());
            long deadline = StructuralGraceClock.CreateDeadline(0, policy);
            long pausedTick = GameTime.ResolveElapsedTicks(60 * 60, 0);
            long normalHalf = GameTime.ResolveElapsedTicks(15 * 60, 1);
            long doubleComplete = GameTime.ResolveElapsedTicks(15 * 60, 2);

            AssertFalse(StructuralGraceClock.IsExpired(pausedTick, deadline), "Paused real time must not consume structural grace.");
            AssertFalse(StructuralGraceClock.IsExpired(normalHalf, deadline), "Fifteen normal-speed minutes must leave half the normal grace.");
            AssertTrue(StructuralGraceClock.IsExpired(doubleComplete, deadline), "Fifteen double-speed minutes must consume normal grace.");
        }

        private static void SaveMigrationDefaultsLegacyDifficultyToNormal()
        {
            GameState legacy = new GameState
            {
                SaveVersion = "1.2",
                Difficulty = null
            };
            SaveSystem saves = new SaveSystem();

            GameState migrated = saves.Deserialize(saves.Serialize(legacy));

            AssertEqual("2.9", migrated.SaveVersion, "Legacy difficulty save must upgrade to 2.9.");
            AssertEqual(DifficultyIds.Normal, migrated.Difficulty.DifficultyId, "Legacy save must default to normal difficulty.");
        }

        private static void InvalidCustomDifficultyFailsLoadAndDiagnostics()
        {
            GameState state = new GameState
            {
                Difficulty = new DifficultyState
                {
                    DifficultyId = DifficultyIds.Custom,
                    CustomStructuralGraceTicks = DifficultyProfiles.MaximumCustomGraceTicks + 1
                }
            };
            SaveSystem saves = new SaveSystem();

            AssertThrows<InvalidOperationException>(
                () => saves.Deserialize(saves.Serialize(state)),
                "Invalid custom structural policy must fail save loading.");
            AssertTrue(
                StateDiagnostics.CheckInvariants(state, new DefinitionRegistry())
                    .Any(issue => issue.Code == "difficulty.structural_policy.invalid"),
                "Diagnostics must report invalid custom structural policy.");
        }

        private static void DefinitionRegistryRejectsNullBuilding()
        {
            DefinitionRegistry registry = new DefinitionRegistry();
            try
            {
                registry.RegisterBuilding(null);
                throw new InvalidOperationException("Expected ArgumentNullException for null building definition.");
            }
            catch (ArgumentNullException)
            {
                // expected
            }
        }

        private static void DefinitionRegistryRejectsInvalidId()
        {
            DefinitionRegistry registry = new DefinitionRegistry();
            try
            {
                registry.RegisterBuilding(new BuildingDefinition
                {
                    DefinitionId = "building:farm"
                });
                throw new InvalidOperationException("Expected ArgumentException for invalid building DefinitionId.");
            }
            catch (ArgumentException)
            {
                // expected
            }
        }

        private static void DefinitionRegistryRegistersAndRetrievesValidBuilding()
        {
            DefinitionRegistry registry = new DefinitionRegistry();
            BuildingDefinition def = new BuildingDefinition
            {
                DefinitionId = "building:core:farm",
                Category = "production",
                MaxDurability = 200,
                CarryCapacity = 10,
                ConstructionTicks = 5
            };
            registry.RegisterBuilding(def);

            AssertTrue(registry.TryGetBuilding("building:core:farm", out BuildingDefinition retrieved),
                "Expected TryGetBuilding to return true for registered definition.");
            AssertTrue(retrieved != null, "Expected retrieved definition to be non-null.");
            AssertEqual("building:core:farm", retrieved.DefinitionId, "Expected DefinitionId to match.");
            AssertEqual("production", retrieved.Category, "Expected Category to match.");
        }

        private static void DefinitionRegistryAcceptsBoundedSolidFootprints()
        {
            DefinitionRegistry registry = new DefinitionRegistry();
            registry.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:future_room",
                FootprintWidth = 3,
                FootprintDepth = 2,
                FootprintHeight = 2
            });

            AssertTrue(registry.TryGetBuilding("building:core:future_room", out BuildingDefinition definition), "Expected solid room definition to register.");
            AssertEqual(3, definition.FootprintWidth, "Expected registered footprint width.");
        }

        private static void DefinitionRegistryRejectsUnsupportedOccupancyKinds()
        {
            DefinitionRegistry registry = new DefinitionRegistry();
            AssertThrows<NotSupportedException>(
                () => registry.RegisterBuilding(new BuildingDefinition
                {
                    DefinitionId = "building:core:future_connector",
                    OccupancyKind = OccupancyKind.Connector
                }),
                "Connector occupancy must remain closed until its dedicated migration.");
        }

        private static void DefinitionRegistrySnapshotsSealsAndRejectsDuplicates()
        {
            DefinitionRegistry registry = new DefinitionRegistry();
            BuildingDefinition source = new BuildingDefinition
            {
                DefinitionId = "building:core:snapshot",
                Category = "original",
                BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["resource:core:wood"] = 3
                }
            };
            registry.RegisterBuilding(source);
            source.Category = "mutated";
            source.BuildCost["resource:core:wood"] = 99;

            AssertTrue(registry.TryGetBuilding(source.DefinitionId, out BuildingDefinition first),
                "Expected snapshotted definition.");
            AssertEqual("original", first.Category, "Registration must snapshot scalar values.");
            AssertEqual(3, first.BuildCost["resource:core:wood"], "Registration must snapshot collections.");

            first.Category = "external_mutation";
            AssertTrue(registry.TryGetBuilding(source.DefinitionId, out BuildingDefinition second),
                "Expected definition after external mutation.");
            AssertEqual("original", second.Category, "Lookup must not expose mutable registry state.");

            AssertThrows<InvalidOperationException>(
                () => registry.RegisterBuilding(new BuildingDefinition { DefinitionId = source.DefinitionId }),
                "Duplicate definitions must be rejected instead of overwritten.");

            _ = new Simulation(new GameState(), registry);
            AssertTrue(registry.IsSealed, "Simulation construction must seal definitions.");
            AssertThrows<InvalidOperationException>(
                () => registry.RegisterBuilding(new BuildingDefinition { DefinitionId = "building:core:late" }),
                "Definitions must not change after simulation composition.");
        }

        private static void CoreContentRegistersPipeDefinition()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            AssertTrue(definitions.IsSealed, "Composed definitions must be sealed.");
            AssertTrue(definitions.TryGetLogisticsConnector(
                CoreContentDefinitionModule.PipeDefinitionId,
                out LogisticsConnectorDefinition pipe),
                "Core content must register the formal pipe.");
            AssertEqual(GameTime.TicksPerGameDay * 3 / 10, pipe.ConstructionTicks,
                "Pipe construction must be 0.3 game day.");
            AssertEqual(200, pipe.MaxDurability, "Pipe durability must match 6.1.");
            AssertEqual(5, pipe.BuildCost[CoreResourceIds.Wood],
                "Pipe wood cost must match 6.1.");
            AssertEqual(2, pipe.BuildCost[CoreResourceIds.IronIngot],
                "Pipe iron ingot cost must match 6.1.");
        }

        private static void CoreContentRegistersResources()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            AssertEqual(16, definitions.ResourceCount, "Core content must register all resources from 4.1.");
            AssertTrue(definitions.TryGetResource(CoreResourceIds.IronOre, out ResourceDefinition ironOre),
                "Expected formal iron ore resource.");
            AssertEqual(ResourceCategory.Mineral, ironOre.Category, "Iron ore must be a mineral resource.");
            AssertTrue(definitions.TryGetResource(CoreResourceIds.Starlight, out ResourceDefinition starlight),
                "Expected formal starlight resource.");
            AssertTrue(starlight.IsCurrency, "Starlight must be marked as currency.");
            AssertTrue(definitions.TryGetResource(CoreResourceIds.TechnologyPoint, out ResourceDefinition technologyPoint),
                "Expected formal technology point resource.");
            AssertTrue(!technologyPoint.IsStorable, "Technology points are not stored inventory.");
        }

        private static void CoreContentRegistersInitialBuildings()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            AssertCoreBuilding(definitions, CoreBuildingIds.House, 500, 3, 1, 0, 20, GameTime.TicksPerGameDay / 2);
            AssertCoreBuilding(definitions, CoreBuildingIds.Farm, 500, 3, 1, 2, 20, GameTime.TicksPerGameDay);
            AssertCoreBuilding(definitions, CoreBuildingIds.Well, 500, 3, 2, 2, 20, GameTime.TicksPerGameDay);
            AssertCoreBuilding(definitions, CoreBuildingIds.TreeFarm, 500, 3, 1, 2, 20, GameTime.TicksPerGameDay);
            AssertCoreBuilding(definitions, CoreBuildingIds.Sunlamp, 400, 5, 5, 0, 0, GameTime.TicksPerGameDay);

            definitions.TryGetBuilding(CoreBuildingIds.House, out BuildingDefinition house);
            definitions.TryGetBuilding(CoreBuildingIds.Farm, out BuildingDefinition farm);
            definitions.TryGetBuilding(CoreBuildingIds.Sunlamp, out BuildingDefinition sunlamp);
            AssertTrue(house.IsHome && !house.IsBasicProduction, "House must own the independent home bonus.");
            AssertTrue(farm.IsBasicProduction && !farm.IsHome, "Farm must share the basic-production bonus.");
            AssertEqual(10, house.BuildCost[CoreResourceIds.Wood], "House wood cost must match 6.1.");
            AssertEqual(5, farm.BuildCost[CoreResourceIds.Wood], "Farm wood cost must match 6.1.");
            AssertEqual(1, farm.BuildCost[CoreResourceIds.Water], "Farm water cost must match 6.1.");
            AssertEqual(10, sunlamp.BuildCost[CoreResourceIds.Stone], "Sunlamp stone cost must match 6.1.");
            AssertEqual(5, sunlamp.BuildCost[CoreResourceIds.IronIngot], "Sunlamp iron-ingot cost must match 6.1.");
        }

        private static void CoreContentRegistersEmergencyCharcoal()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            AssertTrue(definitions.TryGetRecipe(CoreRecipeIds.EmergencyCharcoal, out RecipeDefinition recipe),
                "Expected tree farm emergency charcoal recipe.");
            AssertEqual(CoreBuildingIds.TreeFarm, recipe.BuildingDefinitionId, "Charcoal must belong to tree farm.");
            AssertEqual(GameTime.TicksPerGameDay, recipe.RequiredWorkTicks, "Charcoal work must be one game day.");
            AssertEqual(1, recipe.MinimumWorkers, "Charcoal requires one worker.");
            AssertEqual(2, recipe.Inputs[CoreResourceIds.Wood], "Charcoal input must be two wood.");
            AssertEqual(1, recipe.Outputs[CoreResourceIds.Fuel], "Charcoal output must be one fuel.");
        }

        private static void CoreContentRegistersExcavationAndSmelterChain()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            AssertCoreBuilding(definitions, CoreBuildingIds.ExcavationSite, 500, 5, 3, 2, 30,
                GameTime.TicksPerGameDay);
            AssertCoreBuilding(definitions, CoreBuildingIds.Smelter, 600, 5, 4, 2, 30,
                GameTime.TicksPerGameDay * 2);

            definitions.TryGetBuilding(CoreBuildingIds.ExcavationSite, out BuildingDefinition excavation);
            definitions.TryGetBuilding(CoreBuildingIds.Smelter, out BuildingDefinition smelter);
            AssertEqual(15, excavation.BuildCost[CoreResourceIds.Stone],
                "Excavation stone cost must match 6.1.");
            AssertEqual(15, smelter.BuildCost[CoreResourceIds.Stone],
                "Smelter stone cost must match 6.1.");
            AssertEqual(5, smelter.BuildCost[CoreResourceIds.IronIngot],
                "Smelter iron-ingot cost must match 6.1.");

            AssertTrue(definitions.TryGetContinuousProduction(
                CoreBuildingIds.ExcavationSite, out ContinuousProductionDefinition production),
                "Excavation must register continuous production.");
            AssertEqual(CoreResourceIds.IronOre, production.OutputResourceId,
                "Excavation primary output must be iron ore.");
            AssertEqual(2, production.OutputPerWorkerPerDay,
                "Excavation must produce two iron ore per worker-day.");
            AssertEqual(1, production.AdditionalOutputsPerWorkerPerDay[CoreResourceIds.Stone],
                "Excavation must produce one stone byproduct per worker-day.");

            AssertTrue(definitions.TryGetRecipe(CoreRecipeIds.SmeltIron, out RecipeDefinition iron),
                "Expected formal iron-smelting recipe.");
            AssertTrue(definitions.TryGetRecipe(CoreRecipeIds.SmeltCopper, out RecipeDefinition copper),
                "Expected formal copper-smelting recipe.");
            AssertEqual(CoreBuildingIds.Smelter, iron.BuildingDefinitionId,
                "Iron recipe must belong to the smelter.");
            AssertEqual(1, iron.Inputs[CoreResourceIds.IronOre], "Iron recipe must consume one ore.");
            AssertEqual(1, iron.Inputs[CoreResourceIds.Fuel], "Iron recipe must consume one fuel.");
            AssertEqual(2, copper.Inputs[CoreResourceIds.CopperOre], "Copper recipe must consume two ore.");
            AssertEqual(1, copper.Inputs[CoreResourceIds.Fuel], "Copper recipe must consume one fuel.");
        }

        private static void CoreContentRegistersWarehouseCapacity()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            AssertCoreBuilding(definitions, CoreBuildingIds.Warehouse, 400, 4, 4, 0, 0,
                GameTime.TicksPerGameDay / 2);
            definitions.TryGetBuilding(CoreBuildingIds.Warehouse, out BuildingDefinition warehouse);
            AssertEqual(15, warehouse.BuildCost[CoreResourceIds.Wood],
                "Warehouse wood cost must match 6.1.");
            AssertEqual(5, warehouse.BuildCost[CoreResourceIds.Stone],
                "Warehouse stone cost must match 6.1.");
            AssertEqual(200, warehouse.GlobalStorageCapacityBonus,
                "Warehouse must add 200 shared capacity.");
        }

        private static void CoreContentRegistersWasteProcessorModes()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            AssertCoreBuilding(definitions, CoreBuildingIds.WasteProcessor, 500, 4, 4, 1, 30,
                GameTime.TicksPerGameDay);
            definitions.TryGetBuilding(CoreBuildingIds.WasteProcessor, out BuildingDefinition building);
            AssertEqual(10, building.BuildCost[CoreResourceIds.Wood], "Waste processor wood cost must match 6.1.");
            AssertEqual(10, building.BuildCost[CoreResourceIds.Stone], "Waste processor stone cost must match 6.1.");

            AssertTrue(definitions.TryGetRecipe(CoreRecipeIds.DestroyWaste, out RecipeDefinition destroy),
                "Expected mode A recipe.");
            AssertTrue(definitions.TryGetRecipe(CoreRecipeIds.RecycleWaste, out RecipeDefinition recycle),
                "Expected mode B recipe.");
            AssertTrue(definitions.TryGetRecipe(CoreRecipeIds.CompostWaste, out RecipeDefinition compost),
                "Expected mode C recipe.");
            AssertTrue(definitions.TryGetRecipe(CoreRecipeIds.RefineWasteFuel, out RecipeDefinition refine),
                "Expected mode D recipe.");
            AssertTrue(destroy.DiscardsInputs && destroy.Outputs.Count == 0,
                "Mode A must explicitly discard inputs without fake output.");
            AssertEqual(6, recycle.WeightedOutputRolls, "Mode B must resolve one recovery per waste.");
            AssertEqual(40, recycle.WeightedOutputWeights[CoreResourceIds.Wood], "Mode B wood weight must be 40.");
            AssertEqual(40, recycle.WeightedOutputWeights[CoreResourceIds.Stone], "Mode B stone weight must be 40.");
            AssertEqual(20, recycle.WeightedOutputWeights[CoreResourceIds.IronIngot], "Mode B metal weight must be 20.");
            AssertEqual(6, compost.Outputs[CoreResourceIds.Fertilizer], "Mode C must produce six fertilizer.");
            AssertEqual(10, refine.Inputs[CoreResourceIds.Waste], "Mode D minimum integer batch must consume ten waste.");
            AssertEqual(3, refine.Outputs[CoreResourceIds.Fuel], "Mode D minimum integer batch must produce three fuel.");
        }

        private static void WarehouseLifecycleUpdatesSharedCapacity()
        {
            Simulation simulation = CreateWarehouseSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateWarehouseBuildCommand(
                "command:test:build_warehouse", 1)).Accepted, "Expected warehouse construction.");
            AssertEqual(2000, simulation.State.Resources.SharedCapacity,
                "Capacity bonus must not apply before completion.");
            session.Tick(GameTime.TicksPerGameDay / 2);
            AssertEqual(2200, simulation.State.Resources.SharedCapacity,
                "Completed warehouse must add 200 capacity.");

            string warehouseId = simulation.State.Buildings.Instances.Values.Single().BuildingId;
            CommandResult demolished = session.SendCommand(CreateDemolitionCommand(
                "command:test:demolish_warehouse", 2, warehouseId, "player:test:warehouse"));
            AssertTrue(demolished.Accepted, demolished.Reason);
            AssertEqual(2000, simulation.State.Resources.SharedCapacity,
                "Demolished warehouse must remove its capacity bonus.");
        }

        private static void WarehouseDemolitionRejectsRequiredCapacity()
        {
            Simulation simulation = CreateWarehouseSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateWarehouseBuildCommand(
                "command:test:build_required_warehouse", 1)).Accepted, "Expected warehouse construction.");
            session.Tick(GameTime.TicksPerGameDay / 2);
            simulation.State.Resources.Items[CoreResourceIds.Wood].Amount = 2050;
            simulation.State.Resources.Items[CoreResourceIds.Wood].Capacity = 3000;
            string warehouseId = simulation.State.Buildings.Instances.Values.Single().BuildingId;

            CommandResult demolition = session.SendCommand(CreateDemolitionCommand(
                "command:test:demolish_required_warehouse", 2, warehouseId, "player:test:warehouse"));
            AssertFalse(demolition.Accepted, "Required warehouse capacity must block demolition.");
            AssertEqual(CommandErrorCodes.StorageCapacityRequired, demolition.Code,
                "Expected stable storage-capacity rejection code.");
            AssertEqual(2200, simulation.State.Resources.SharedCapacity,
                "Rejected demolition must preserve shared capacity.");
        }

        private static void SharedCapacityBlocksContinuousGlobalOutput()
        {
            Simulation simulation = CreateContinuousProductionSimulation(
                CoreBuildingIds.TreeFarm, 1, 0, 100, 0, 0);
            simulation.State.Resources.SharedCapacity = 0;
            simulation.Tick(GameTime.TicksPerGameDay / 3);
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings["building:test:continuous"];
            AssertEqual(1, runtime.PendingOutputAmount,
                "Full shared storage must preserve one pending output.");
            AssertEqual(0, simulation.State.Resources.Items[CoreResourceIds.Wood].Amount,
                "Shared-capacity overflow must not enter global storage.");
        }

        private static void SharedCapacityMigrationDefaultsLegacySaves()
        {
            GameState legacy = new GameState { SaveVersion = "2.4" };
            legacy.Resources.SharedCapacity = 0;
            JsonSerializerOptions options = SaveSystem.CreateDefaultJsonOptions();
            GameState migrated = new SaveSystem(options).Deserialize(JsonSerializer.Serialize(legacy, options));
            AssertEqual("2.9", migrated.SaveVersion, "Version 2.4 must migrate to 2.9.");
            AssertEqual(2000, migrated.Resources.SharedCapacity,
                "Legacy saves must receive the authoritative base shared capacity.");
        }

        private static void SharedOvercapacitySurvivesSaveAndReportsWarning()
        {
            GameState state = new GameState();
            state.Resources.SharedCapacity = 100;
            state.Resources.Items[CoreResourceIds.Wood] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Wood, Amount = 101, Capacity = 200
            };
            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(state));
            AssertEqual(101, loaded.Resources.Items[CoreResourceIds.Wood].Amount,
                "Overcapacity caused by destruction must not delete resources during save load.");
            AssertTrue(StateDiagnostics.CheckInvariants(loaded, new DefinitionRegistry())
                .Any(issue => issue.Code == "resource.shared_capacity.exceeded"),
                "Diagnostics must report recoverable shared overcapacity.");
        }

        private static void RemoteWarehouseCapacityUsesServerAuthority()
        {
            ServerGameSession server = new ServerGameSession(
                CreateWarehouseSimulation(), new[] { "player:test:warehouse" });
            RemoteGameSession remote = CreateRemoteSession(server);
            AssertTrue(remote.SendCommand(CreateWarehouseBuildCommand(
                "command:test:remote_warehouse", 1)).Accepted, "Expected remote warehouse construction.");
            remote.Tick(999);
            AssertEqual(2000, remote.CurrentState.Resources.SharedCapacity,
                "Remote tick must not complete warehouse construction.");
            server.Tick(GameTime.TicksPerGameDay / 2);
            remote.Tick(999);
            AssertEqual(2200, remote.CurrentState.Resources.SharedCapacity,
                "Remote must synchronize the server capacity bonus.");
        }

        private static void CoreInitialBuildingsApplyFirstBuildBonuses()
        {
            GameState state = new GameState();
            state.World.Plots["plot:core:initial_content"] = new PlotState
            {
                PlotId = "plot:core:initial_content",
                Width = 4,
                Depth = 1,
                MaxStackLayers = 2
            };
            state.Resources.Items[CoreResourceIds.Wood] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Wood,
                Amount = 100,
                Capacity = 200
            };
            state.Resources.Items[CoreResourceIds.Water] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Water,
                Amount = 10,
                Capacity = 100
            };

            LocalGameSession session = RuntimeComposition.CreateLocalSession(
                state, RuntimeComposition.CreateDefinitions());
            AssertTrue(session.SendCommand(CreateFormalBuildCommand(
                "command:test:first_house", 1, CoreBuildingIds.House, 0)).Accepted,
                "Expected first formal house construction.");
            ConstructionTaskState firstHouse = state.Buildings.ConstructionTasks.Values.Single();
            AssertEqual(GameTime.TicksPerGameDay / 4, firstHouse.RequiredTicks,
                "First house must use its independent 0.25-day bonus.");

            session.Tick(firstHouse.RequiredTicks);
            AssertTrue(session.SendCommand(CreateFormalBuildCommand(
                "command:test:second_house", 2, CoreBuildingIds.House, 1)).Accepted,
                "Expected second formal house construction.");
            ConstructionTaskState secondHouse = state.Buildings.ConstructionTasks.Values.Single();
            AssertEqual(GameTime.TicksPerGameDay / 2, secondHouse.RequiredTicks,
                "Second house must use normal construction time.");

            AssertTrue(session.SendCommand(CreateFormalBuildCommand(
                "command:test:first_farm", 3, CoreBuildingIds.Farm, 2)).Accepted,
                "Expected first formal farm construction.");
            ConstructionTaskState firstFarm = state.Buildings.ConstructionTasks.Values.Single(
                task => task.DefinitionId == CoreBuildingIds.Farm);
            AssertEqual(GameTime.TicksPerGameDay * 2 / 5, firstFarm.RequiredTicks,
                "First basic production building must use shared 0.4-day bonus.");
        }

        private static void AssertCoreBuilding(
            DefinitionRegistry definitions,
            string definitionId,
            int durability,
            int carryCapacity,
            int weight,
            int workerSlots,
            int localCapacity,
            long constructionTicks)
        {
            AssertTrue(definitions.TryGetBuilding(definitionId, out BuildingDefinition building),
                $"Expected core building {definitionId}.");
            AssertEqual(durability, building.MaxDurability, "Unexpected durability.");
            AssertEqual(carryCapacity, building.CarryCapacity, "Unexpected carry capacity.");
            AssertEqual(weight, building.Weight, "Unexpected derived weight.");
            AssertEqual(workerSlots, building.WorkerSlotCount, "Unexpected worker slots.");
            AssertEqual(localCapacity, building.LocalInventoryCapacity, "Unexpected local inventory capacity.");
            AssertEqual(constructionTicks, building.ConstructionTicks, "Unexpected construction time.");
            AssertEqual(1, building.FootprintWidth, "Initial building width must be one standard cell.");
            AssertEqual(1, building.FootprintDepth, "Initial building depth must be one standard cell.");
            AssertEqual(1, building.FootprintHeight, "Initial building height must be one standard cell.");
        }

        private static void ContinuousProductionYieldsExactDailyOutput()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.TreeFarm, 1, 20, 100, 0, 0);
            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState building = simulation.State.Buildings.Instances["building:test:continuous"];
            AssertEqual(3, building.LocalInventory[CoreResourceIds.Wood].Amount,
                "One tree-farm worker must produce exactly three wood per day.");
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[building.BuildingId];
            AssertEqual(0L, runtime.ProgressUnits, "A complete day must leave no fractional progress.");
            AssertEqual(ContinuousProductionStatuses.Running, runtime.Status, "Expected running status.");
        }

        private static void FarmContinuousProductionConsumesIrrigation()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false);
            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            AssertEqual(8, farm.LocalInventory[CoreResourceIds.Food].Amount,
                "Two farm workers must produce eight food per day.");
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Water].Amount,
                "Farm irrigation must consume one water per operating day, independent of worker count.");
            AssertEqual(0L, simulation.State.ContinuousProduction.Buildings[farm.BuildingId].InputCoverageTicks,
                "A full operating day must consume its irrigation coverage.");
        }

        private static void FarmLightOcclusionPausesContinuousProduction()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            ExtendContinuousPlotForLight(simulation);
            AddLightOccluder(simulation);

            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.PausedNoLight, runtime.Status,
                "Occluded farm without sunlamp coverage must pause for missing light.");
            AssertEqual(0, GetLocalAmount(farm, CoreResourceIds.Food),
                "Missing light must block farm output.");
            AssertEqual(2, simulation.State.Resources.Items[CoreResourceIds.Water].Amount,
                "Missing light must pause before irrigation is consumed.");
            AssertEqual(0L, runtime.ProgressUnits, "Missing light must preserve production progress.");
        }

        private static void FarmLightSunlampRestoresContinuousProduction()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            ExtendContinuousPlotForLight(simulation);
            AddLightOccluder(simulation);
            AddSunlamp(simulation, false);

            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.Running, runtime.Status,
                "Covered farm must resume normal continuous production.");
            AssertEqual(8, farm.LocalInventory[CoreResourceIds.Food].Amount,
                "Sunlamp-covered farm must produce normally.");
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Water].Amount,
                "Running farm must consume one irrigation water per operating day.");
        }

        private static void FarmLightDestroyedSunlampDoesNotRestoreProduction()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            ExtendContinuousPlotForLight(simulation);
            AddLightOccluder(simulation);
            AddSunlamp(simulation, true);

            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.PausedNoLight, runtime.Status,
                "Destroyed sunlamp must not restore farm light.");
            AssertEqual(0, GetLocalAmount(farm, CoreResourceIds.Food),
                "Destroyed sunlamp must not allow occluded farm output.");
        }

        private static void FarmLightPausedStatusSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            ExtendContinuousPlotForLight(simulation);
            AddLightOccluder(simulation);

            simulation.Tick(GameTime.TicksPerGameDay);

            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            ContinuousProductionBuildingState runtime =
                loaded.ContinuousProduction.Buildings["building:test:continuous"];

            AssertEqual(ContinuousProductionStatuses.PausedNoLight, runtime.Status,
                "No-light continuous-production status must survive save load.");
            AssertTrue(ContinuousProductionStatuses.IsKnown(runtime.Status),
                "No-light status must remain a known continuous-production state after save load.");
            AssertEqual(0L, runtime.ProgressUnits,
                "No-light save round trip must not invent farm progress.");
            AssertEqual(2, loaded.Resources.Items[CoreResourceIds.Water].Amount,
                "No-light save round trip must preserve unconsumed irrigation water.");
        }

        private static void FarmLightRestorationDoesNotBackfillPausedTicks()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 3);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 3;
            ExtendContinuousPlotForLight(simulation);
            AddLightOccluder(simulation);

            simulation.Tick(GameTime.TicksPerGameDay);
            AddSunlamp(simulation, false);
            simulation.Tick(GameTime.TicksPerGameDay / 2);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(4, GetLocalAmount(farm, CoreResourceIds.Food),
                "Restored farm must produce only for ticks after light returns.");
            AssertEqual(2, simulation.State.Resources.Items[CoreResourceIds.Water].Amount,
                "Restored half-day production must consume one operating-day irrigation coverage.");
            AssertEqual(GameTime.TicksPerGameDay / 2, runtime.InputCoverageTicks,
                "Half-day after restore must leave the unused half-day irrigation coverage.");
            AssertEqual(0L, runtime.ProgressUnits,
                "Half-day production at two workers must complete four food without carrying paused progress.");
            AssertEqual(ContinuousProductionStatuses.Running, runtime.Status,
                "Farm must return to running after light is restored.");
        }

        private static void FarmLightNightWithoutSunlampPausesContinuousProduction()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;

            simulation.Tick(GameTime.TicksPerGameDay / 4);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.PausedNoLight, runtime.Status,
                "Night farm without sunlamp coverage must pause for missing light.");
            AssertEqual(0, GetLocalAmount(farm, CoreResourceIds.Food),
                "Night missing light must block farm output.");
            AssertEqual(2, simulation.State.Resources.Items[CoreResourceIds.Water].Amount,
                "Night missing light must not consume irrigation.");
            AssertEqual(0L, runtime.ProgressUnits, "Night missing light must not advance production progress.");
        }

        private static void FarmLightDayNightSplitOnlyProducesDaylight()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;

            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.PausedNoLight, runtime.Status,
                "A day that ends with unlit night work must leave the farm paused for missing light.");
            AssertEqual(4, GetLocalAmount(farm, CoreResourceIds.Food),
                "Unlit night must not backfill the daylight half-day output.");
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Water].Amount,
                "Only the daylight operating segment must consume irrigation coverage.");
            AssertEqual(GameTime.TicksPerGameDay / 2, runtime.InputCoverageTicks,
                "Daylight half-day must leave unused irrigation coverage for later valid light.");
        }

        private static void FarmLightNightDaySplitDoesNotBackfillNight()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;

            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.Running, runtime.Status,
                "After crossing into daylight, the farm must resume without remaining paused.");
            AssertEqual(4, GetLocalAmount(farm, CoreResourceIds.Food),
                "Night-to-day split must only produce for the valid daylight half-day.");
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Water].Amount,
                "Skipped night work must not consume irrigation.");
            AssertEqual(GameTime.TicksPerGameDay / 2, runtime.InputCoverageTicks,
                "Only the daylight half-day should consume prepaid irrigation coverage.");
        }

        private static void FarmLightNightSunlampSupportsProduction()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false);

            simulation.Tick(GameTime.TicksPerGameDay / 2);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.Running, runtime.Status,
                "Night farm with full sunlamp coverage must keep running.");
            AssertEqual(4, GetLocalAmount(farm, CoreResourceIds.Food),
                "Sunlamp-covered night farm must produce for the valid half-day segment.");
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Water].Amount,
                "Sunlamp-covered night production must consume irrigation coverage.");
            AssertEqual(GameTime.TicksPerGameDay / 2, runtime.InputCoverageTicks,
                "Night half-day production must leave the unused half-day irrigation coverage.");
        }

        private static void FarmLightSunlampConsumesFuelCoverage()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false, 1);

            simulation.Tick(GameTime.TicksPerGameDay / 2);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            SunlampBuildingState sunlampRuntime = simulation.State.Sunlamps.Buildings["building:test:sunlamp"];
            AssertEqual(4, GetLocalAmount(farm, CoreResourceIds.Food),
                "Fueled sunlamp must allow night half-day production.");
            AssertEqual(0, simulation.State.Resources.Items[CoreResourceIds.Fuel].Amount,
                "Sunlamp must consume one fuel before providing one game day of coverage.");
            AssertEqual(GameTime.TicksPerGameDay / 2, sunlampRuntime.FuelCoverageTicks,
                "Night half-day coverage must leave half a day of prepaid sunlamp fuel.");
        }

        private static void FarmLightSharedSunlampChargesByTimeNotFarmCount()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 1, 20, 100, 0, 4);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false, 1);

            BuildingInstanceState secondFarm = CreateBuildingStateWithDefinition(
                "building:test:continuous_second",
                CoreBuildingIds.Farm,
                "plot:test:continuous",
                1,
                1,
                0,
                1,
                1,
                1);
            secondFarm.Durability = 500;
            secondFarm.LocalInventoryCapacity = 20;
            simulation.State.Buildings.Instances.Add(secondFarm.BuildingId, secondFarm);
            simulation.State.Npcs.Instances["npc:test:continuous_second"] = new NpcInstanceState
            {
                NpcId = "npc:test:continuous_second",
                OwnerPlayerId = "player:test:continuous",
                CreationSequence = 2
            };
            simulation.State.Npcs.WorkAssignments["npc:test:continuous_second"] = new WorkAssignmentState
            {
                NpcId = "npc:test:continuous_second",
                BuildingId = secondFarm.BuildingId,
                SlotIndex = 0
            };

            simulation.Tick(GameTime.TicksPerGameDay / 2);

            BuildingInstanceState firstFarm = simulation.State.Buildings.Instances["building:test:continuous"];
            SunlampBuildingState sunlampRuntime = simulation.State.Sunlamps.Buildings["building:test:sunlamp"];
            AssertEqual(2, GetLocalAmount(firstFarm, CoreResourceIds.Food),
                "First farm must receive the shared sunlamp coverage for the night half-day.");
            AssertEqual(2, GetLocalAmount(secondFarm, CoreResourceIds.Food),
                "Second farm must receive the same shared sunlamp coverage for the night half-day.");
            AssertEqual(GameTime.TicksPerGameDay / 2, sunlampRuntime.FuelCoverageTicks,
                "One sunlamp covering two farms in the same tick must spend coverage by time, not farm count.");
            AssertEqual(0, simulation.State.Resources.Items[CoreResourceIds.Fuel].Amount,
                "Shared sunlamp coverage must still consume exactly one fuel prepayment.");
        }

        private static void FarmLightUnfueledSunlampDoesNotRestoreNightProduction()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false, 0);

            simulation.Tick(GameTime.TicksPerGameDay / 2);

            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.PausedNoLight, runtime.Status,
                "Unfueled sunlamp must not restore night farm production.");
            AssertEqual(0, GetLocalAmount(farm, CoreResourceIds.Food),
                "Unfueled sunlamp must not allow night output.");
            AssertTrue(!simulation.State.Sunlamps.Buildings.ContainsKey("building:test:sunlamp"),
                "Unfueled unused sunlamp must not create prepaid coverage state.");
        }

        private static void FarmLightSunlampFuelSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false, 1);
            simulation.Tick(GameTime.TicksPerGameDay / 2);

            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));

            AssertEqual("2.9", loaded.SaveVersion, "Sunlamp fuel state requires save version 2.9.");
            AssertEqual(GameTime.TicksPerGameDay / 2,
                loaded.Sunlamps.Buildings["building:test:sunlamp"].FuelCoverageTicks,
                "Sunlamp prepaid fuel coverage must survive save load.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State),
                StateDiagnostics.CalculateStateHash(loaded),
                "Sunlamp fuel state must participate in StateHash.");
        }

        private static void AgriculturalLightOnlyAffectsFarmProduction()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.TreeFarm, 1, 20, 100, 0, 0);
            ExtendContinuousPlotForLight(simulation);
            AddLightOccluder(simulation);

            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState treeFarm = simulation.State.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings[treeFarm.BuildingId];
            AssertEqual(ContinuousProductionStatuses.Running, runtime.Status,
                "Agricultural light gating must not affect non-farm continuous production.");
            AssertEqual(3, treeFarm.LocalInventory[CoreResourceIds.Wood].Amount,
                "Tree farm must keep producing even when physically occluded.");
        }

        private static void ContinuousProductionPausesWithoutConditions()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 0);
            simulation.Tick(GameTime.TicksPerGameDay);
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings["building:test:continuous"];
            AssertEqual(ContinuousProductionStatuses.PausedInput, runtime.Status,
                "Farm without irrigation must pause.");
            AssertEqual(0L, runtime.ProgressUnits, "Missing irrigation must not advance progress.");

            simulation.State.Resources.Items[CoreResourceIds.Water].Amount = 1;
            simulation.Tick(GameTime.TicksPerGameDay / 2);
            long progress = runtime.ProgressUnits;
            long coverage = runtime.InputCoverageTicks;
            simulation.State.Npcs.WorkAssignments.Clear();
            simulation.Tick(GameTime.TicksPerGameDay / 2);
            AssertEqual(ContinuousProductionStatuses.PausedNoWorkers, runtime.Status,
                "Missing workers must pause continuous production.");
            AssertEqual(progress, runtime.ProgressUnits, "Worker pause must preserve progress.");
            AssertEqual(coverage, runtime.InputCoverageTicks, "Worker pause must preserve prepaid irrigation.");
        }

        private static void ContinuousProductionPreservesPendingOutput()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.TreeFarm, 1, 0, 5, 5, 0);
            simulation.Tick(GameTime.TicksPerGameDay / 3);
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings["building:test:continuous"];
            AssertEqual(1, runtime.PendingOutputAmount, "Full storage must preserve exactly one generated unit.");
            AssertEqual(ContinuousProductionStatuses.OutputPending, runtime.Status, "Expected output-pending status.");

            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(1, runtime.PendingOutputAmount, "Blocked production must not accumulate unbounded output.");
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult demolition = session.SendCommand(new CommandEnvelope
            {
                CommandId = "command:test:continuous_demolition",
                PlayerId = "player:test:continuous",
                Type = BuildingSystem.DemolishBuildingCommand,
                Payload = JsonSerializer.SerializeToElement(new DemolishBuildingPayload
                {
                    BuildingId = "building:test:continuous"
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = 1
            });
            AssertFalse(demolition.Accepted, "Pending output must block normal demolition.");
            AssertEqual(CommandErrorCodes.ProductionOutputPending, demolition.Code,
                "Expected stable pending-output error code.");

            simulation.State.Resources.Items[CoreResourceIds.Wood].Amount = 4;
            simulation.Tick(1);
            AssertEqual(0, runtime.PendingOutputAmount, "Freed storage must accept pending output.");
            AssertEqual(5, simulation.State.Resources.Items[CoreResourceIds.Wood].Amount,
                "Pending output must be transferred without duplication.");
        }

        private static void ContinuousProductionSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.TreeFarm, 1, 20, 100, 0, 0);
            simulation.Tick(GameTime.TicksPerGameDay / 2);
            ContinuousProductionBuildingState before =
                simulation.State.ContinuousProduction.Buildings["building:test:continuous"];
            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            ContinuousProductionBuildingState after =
                loaded.ContinuousProduction.Buildings["building:test:continuous"];

            AssertEqual("2.9", loaded.SaveVersion, "Continuous production requires save version 2.9.");
            AssertEqual(before.ProgressUnits, after.ProgressUnits, "Fractional progress must survive save load.");
            AssertEqual(before.PendingOutputAmount, after.PendingOutputAmount, "Pending output must survive save load.");
            AssertEqual(before.Status, after.Status, "Continuous production status must survive save load.");
        }

        private static void ExcavationProducesIronOreAndStone()
        {
            Simulation simulation = CreateContinuousProductionSimulation(
                CoreBuildingIds.ExcavationSite, 2, 20, 100, 0, 0);
            simulation.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState excavation = simulation.State.Buildings.Instances["building:test:continuous"];
            AssertEqual(4, excavation.LocalInventory[CoreResourceIds.IronOre].Amount,
                "Two workers must produce four iron ore per day.");
            AssertEqual(2, excavation.LocalInventory[CoreResourceIds.Stone].Amount,
                "Two workers must produce two stone byproduct per day.");
        }

        private static void ExcavationPreservesBlockedStoneByproduct()
        {
            Simulation simulation = CreateContinuousProductionSimulation(
                CoreBuildingIds.ExcavationSite, 1, 0, 100, 0, 0);
            simulation.State.Resources.Items[CoreResourceIds.Stone].Capacity = 0;
            simulation.Tick(GameTime.TicksPerGameDay);

            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings["building:test:continuous"];
            AssertEqual(2, simulation.State.Resources.Items[CoreResourceIds.IronOre].Amount,
                "Available iron storage must still accept the primary output.");
            AssertEqual(1, runtime.AdditionalPendingOutputs[CoreResourceIds.Stone],
                "Blocked stone must remain as one pending byproduct.");
            AssertEqual(ContinuousProductionStatuses.OutputPending, runtime.Status,
                "Blocked byproduct must pause further excavation.");

            CommandResult demolition = new LocalGameSession(simulation).SendCommand(new CommandEnvelope
            {
                CommandId = "command:test:demolish_blocked_excavation",
                PlayerId = "player:test:continuous",
                Type = BuildingSystem.DemolishBuildingCommand,
                Payload = JsonSerializer.SerializeToElement(new DemolishBuildingPayload
                {
                    BuildingId = "building:test:continuous"
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = 1
            });
            AssertFalse(demolition.Accepted, "Pending stone must block normal demolition.");
            AssertEqual(CommandErrorCodes.ProductionOutputPending, demolition.Code,
                "Byproduct demolition rejection must use the stable production code.");

            simulation.State.Resources.Items[CoreResourceIds.Stone].Capacity = 1;
            simulation.Tick(1);
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Stone].Amount,
                "Freed storage must accept the pending stone exactly once.");
            AssertTrue(!runtime.AdditionalPendingOutputs.ContainsKey(CoreResourceIds.Stone),
                "Transferred stone must clear pending state.");
        }

        private static void ExcavationByproductSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateContinuousProductionSimulation(
                CoreBuildingIds.ExcavationSite, 1, 20, 100, 0, 0);
            simulation.Tick(GameTime.TicksPerGameDay / 2);
            ContinuousProductionBuildingState before =
                simulation.State.ContinuousProduction.Buildings["building:test:continuous"];

            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            ContinuousProductionBuildingState after =
                loaded.ContinuousProduction.Buildings["building:test:continuous"];

            AssertEqual(before.AdditionalProgressUnits[CoreResourceIds.Stone],
                after.AdditionalProgressUnits[CoreResourceIds.Stone],
                "Stone byproduct progress must survive save load.");
            AssertEqual("2.9", loaded.SaveVersion, "Byproduct state requires save version 2.9.");
        }

        private static void SaveMigrationInitializesContinuousByproducts()
        {
            GameState legacyState = new GameState { SaveVersion = "2.3" };
            legacyState.World.Plots["plot:test:legacy"] = new PlotState
            {
                PlotId = "plot:test:legacy", Width = 1, Depth = 1, MaxStackLayers = 1
            };
            BuildingInstanceState building = CreateBuildingStateWithDefinition(
                "building:test:legacy", CoreBuildingIds.ExcavationSite,
                "plot:test:legacy", 0, 0, 0, 1, 1, 1);
            building.Durability = 500;
            legacyState.Buildings.Instances[building.BuildingId] = building;
            legacyState.ContinuousProduction.Buildings["building:test:legacy"] =
                new ContinuousProductionBuildingState
                {
                    BuildingId = "building:test:legacy",
                    ProgressUnits = 5,
                    PendingOutputAmount = 0,
                    Status = ContinuousProductionStatuses.Running,
                    AdditionalProgressUnits = null,
                    AdditionalPendingOutputs = null
                };
            JsonSerializerOptions options = SaveSystem.CreateDefaultJsonOptions();
            string legacy = JsonSerializer.Serialize(legacyState, options);
            GameState migrated = new SaveSystem(options).Deserialize(legacy);
            ContinuousProductionBuildingState runtime =
                migrated.ContinuousProduction.Buildings["building:test:legacy"];

            AssertEqual("2.9", migrated.SaveVersion, "Version 2.3 must migrate to 2.9.");
            AssertEqual(0, runtime.AdditionalProgressUnits.Count,
                "Legacy saves must initialize empty byproduct progress.");
            AssertEqual(0, runtime.AdditionalPendingOutputs.Count,
                "Legacy saves must initialize empty pending byproducts.");
        }

        private static void RemoteContinuousProductionUsesServerAuthority()
        {
            ServerGameSession server = new ServerGameSession(
                CreateContinuousProductionSimulation(CoreBuildingIds.TreeFarm, 1, 20, 100, 0, 0),
                new[] { "player:test:continuous" });
            RemoteGameSession remote = CreateRemoteSession(server);

            remote.Tick(999);
            AssertTrue(!remote.CurrentState.ContinuousProduction.Buildings.ContainsKey("building:test:continuous"),
                "Remote tick must not advance continuous production.");
            server.Tick(GameTime.TicksPerGameDay);
            remote.Tick(999);

            AssertEqual(3,
                remote.CurrentState.Buildings.Instances["building:test:continuous"]
                    .LocalInventory[CoreResourceIds.Wood].Amount,
                "Remote must receive server-produced output.");
            AssertEqual(server.CurrentState.SimulationTick, remote.CurrentState.SimulationTick,
                "Remote continuous production must use authoritative server time.");
        }

        private static void RemoteFarmLightUsesServerAuthority()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            ExtendContinuousPlotForLight(simulation);
            AddLightOccluder(simulation);
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:test:continuous" });
            RemoteGameSession remote = CreateRemoteSession(server);

            remote.Tick(GameTime.TicksPerGameDay);
            AssertTrue(!remote.CurrentState.ContinuousProduction.Buildings.ContainsKey("building:test:continuous"),
                "Remote tick must not advance farm light production locally.");
            server.Tick(GameTime.TicksPerGameDay);
            remote.Tick(1);

            BuildingInstanceState remoteFarm =
                remote.CurrentState.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState remoteRuntime =
                remote.CurrentState.ContinuousProduction.Buildings[remoteFarm.BuildingId];
            AssertEqual(0, GetLocalAmount(remoteFarm, CoreResourceIds.Food),
                "Remote must receive the server-authoritative no-light production result.");
            AssertEqual(ContinuousProductionStatuses.PausedNoLight, remoteRuntime.Status,
                "Remote must synchronize the server-authoritative no-light status.");
            AssertEqual(server.CurrentState.SimulationTick, remote.CurrentState.SimulationTick,
                "Remote farm light synchronization must use authoritative server time.");
        }

        private static void RemoteFarmNightLightUsesServerAuthority()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:test:continuous" });
            RemoteGameSession remote = CreateRemoteSession(server);

            remote.Tick(GameTime.TicksPerGameDay / 2);
            AssertTrue(!remote.CurrentState.ContinuousProduction.Buildings.ContainsKey("building:test:continuous"),
                "Remote tick must not advance night farm production locally.");
            server.Tick(GameTime.TicksPerGameDay / 2);
            remote.Tick(1);

            BuildingInstanceState remoteFarm =
                remote.CurrentState.Buildings.Instances["building:test:continuous"];
            ContinuousProductionBuildingState remoteRuntime =
                remote.CurrentState.ContinuousProduction.Buildings[remoteFarm.BuildingId];
            AssertEqual(0, GetLocalAmount(remoteFarm, CoreResourceIds.Food),
                "Remote must receive the server-authoritative night no-light result.");
            AssertEqual(2, remote.CurrentState.Resources.Items[CoreResourceIds.Water].Amount,
                "Remote must receive the server-authoritative night irrigation result.");
            AssertEqual(ContinuousProductionStatuses.PausedNoLight, remoteRuntime.Status,
                "Remote must synchronize the server-authoritative night no-light status.");
            AssertEqual(server.CurrentState.SimulationTick, remote.CurrentState.SimulationTick,
                "Remote farm night light synchronization must use authoritative server time.");
        }

        private static void RemoteFarmSunlampFuelUsesServerAuthority()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            simulation.State.SimulationTick = DayNightCycle.TicksPerHalfDay;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false, 1);
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:test:continuous" });
            RemoteGameSession remote = CreateRemoteSession(server);

            remote.Tick(GameTime.TicksPerGameDay / 2);
            AssertTrue(!remote.CurrentState.Sunlamps.Buildings.ContainsKey("building:test:sunlamp"),
                "Remote tick must not consume sunlamp fuel locally.");
            server.Tick(GameTime.TicksPerGameDay / 2);
            remote.Tick(1);

            BuildingInstanceState remoteFarm =
                remote.CurrentState.Buildings.Instances["building:test:continuous"];
            AssertEqual(4, GetLocalAmount(remoteFarm, CoreResourceIds.Food),
                "Remote must receive server-authoritative fueled night production.");
            AssertEqual(0, remote.CurrentState.Resources.Items[CoreResourceIds.Fuel].Amount,
                "Remote must receive server-authoritative sunlamp fuel consumption.");
            AssertEqual(GameTime.TicksPerGameDay / 2,
                remote.CurrentState.Sunlamps.Buildings["building:test:sunlamp"].FuelCoverageTicks,
                "Remote must receive server-authoritative sunlamp fuel coverage.");
            AssertEqual(StateDiagnostics.CalculateStateHash(server.CurrentState),
                StateDiagnostics.CalculateStateHash(remote.CurrentState),
                "Sunlamp fuel state must remain server authoritative.");
        }

        private static void DefinitionSealingRejectsBrokenReferences()
        {
            AssertThrows<InvalidOperationException>(
                () => RuntimeComposition.CreateDefinitions(new[]
                {
                    new TestDefinitionModule("definitions:test:unknown_resource", registry =>
                        registry.RegisterBuilding(new BuildingDefinition
                        {
                            DefinitionId = "building:test:broken_cost",
                            BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
                            {
                                ["resource:test:missing"] = 1
                            }
                        }))
                }),
                "Formal composition must reject unknown building cost resources.");

            AssertThrows<InvalidOperationException>(
                () => RuntimeComposition.CreateDefinitions(new[]
                {
                    new TestDefinitionModule("definitions:test:unknown_building", registry =>
                        registry.RegisterRecipe(new RecipeDefinition
                        {
                            RecipeId = "recipe:test:broken_building",
                            BuildingDefinitionId = "building:test:missing",
                            RequiredWorkTicks = 1,
                            Inputs = new Dictionary<string, int>(StringComparer.Ordinal),
                            Outputs = new Dictionary<string, int>(StringComparer.Ordinal)
                            {
                                [CoreResourceIds.Wood] = 1
                            }
                        }))
                }),
                "Formal composition must reject recipes for unknown buildings.");
        }

        private static void DefinitionSealingAcceptsCompleteModule()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions(new[]
            {
                new TestDefinitionModule("definitions:test:complete", registry =>
                {
                    registry.RegisterBuilding(new BuildingDefinition
                    {
                        DefinitionId = "building:test:kiln",
                        BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
                        {
                            [CoreResourceIds.Wood] = 2
                        }
                    });
                    registry.RegisterRecipe(new RecipeDefinition
                    {
                        RecipeId = "recipe:test:fuel",
                        BuildingDefinitionId = "building:test:kiln",
                        RequiredWorkTicks = 1,
                        Inputs = new Dictionary<string, int>(StringComparer.Ordinal)
                        {
                            [CoreResourceIds.Wood] = 2
                        },
                        Outputs = new Dictionary<string, int>(StringComparer.Ordinal)
                        {
                            [CoreResourceIds.Fuel] = 1
                        }
                    });
                })
            });

            AssertTrue(definitions.IsSealed, "Complete content must seal successfully.");
            AssertTrue(definitions.TryGetRecipe("recipe:test:fuel", out RecipeDefinition recipe),
                "Expected valid cross-referenced recipe.");
            AssertEqual("building:test:kiln", recipe.BuildingDefinitionId, "Expected recipe building reference.");
        }

        private static void RuntimeCompositionRegistersCoreSystems()
        {
            Simulation simulation = RuntimeComposition.CreateSimulation(
                new GameState(),
                RuntimeComposition.CreateDefinitions());
            AssertEqual(9, simulation.SystemCount, "Core composition must install exactly nine systems.");

            string[] commandTypes =
            {
                BuildingSystem.BuildCommand,
                BuildingSystem.CancelConstructionCommand,
                BuildingSystem.DemolishBuildingCommand,
                HousingSystem.AssignHousingCommand,
                WorkerAssignmentSystem.AssignWorkerCommand,
                WorkerAssignmentSystem.UnassignWorkerCommand,
                ContinuousProductionSystem.ApplyFertilizerCommand,
                ProductionSystem.ConfigureProductionCommand,
                ProductionSystem.StartProductionCommand,
                ProductionSystem.CancelProductionCommand,
                LogisticsSystem.CreateTransportCommand,
                LogisticsSystem.CancelTransportCommand,
                LogisticsSystem.BuildConnectorCommand,
                LogisticsSystem.CancelConnectorConstructionCommand,
                LogisticsSystem.DemolishConnectorCommand,
                LogisticsSystem.ConfigureConnectorCommand
            };

            for (int index = 0; index < commandTypes.Length; index++)
            {
                CommandResult result = simulation.ExecuteCommand(new CommandEnvelope
                {
                    CommandId = $"command:test:composition_{index}",
                    PlayerId = $"player:test:composition_{index}",
                    Type = commandTypes[index],
                    Payload = JsonSerializer.SerializeToElement(new { }),
                    Sequence = 1
                });
                AssertTrue(result.Code != CommandErrorCodes.UnknownHandler,
                    $"Core composition omitted handler {commandTypes[index]}.");
            }

            AssertThrows<InvalidOperationException>(
                () => simulation.AddSystem(new BuildingSystem()),
                "Simulation must reject duplicate system types.");
        }

        private static void RuntimeCompositionCreatesSessions()
        {
            LocalGameSession local = RuntimeComposition.CreateLocalSession(
                new GameState(), RuntimeComposition.CreateDefinitions());
            ServerGameSession server = RuntimeComposition.CreateServerSession(
                new GameState(), RuntimeComposition.CreateDefinitions(),
                new[] { "player:core:host" });

            AssertEqual(GameSessionMode.Local, local.Mode, "Expected local composition mode.");
            AssertEqual(GameSessionMode.Server, server.Mode, "Expected server composition mode.");
            AssertTrue(local.CanAdvanceSimulation && server.CanAdvanceSimulation,
                "Both authoritative compositions must advance the same simulation runtime.");
        }

        private static void SaveMigrationUpgradesLegacyPlacementSnapshots()
        {
            GameState legacy = new GameState { SaveVersion = "1.0" };
            legacy.World.Plots["plot:core:legacy"] = new PlotState
            {
                PlotId = "plot:core:legacy",
                X = 7,
                Y = -3
            };
            legacy.Buildings.Instances["building:core:legacy"] = new BuildingInstanceState
            {
                BuildingId = "building:core:legacy",
                DefinitionId = "building:core:farm",
                PlotId = "plot:core:legacy",
                Layer = 5
            };

            SaveSystem saves = new SaveSystem();
            GameState migrated = saves.Deserialize(saves.Serialize(legacy));
            BuildingInstanceState instance = migrated.Buildings.Instances["building:core:legacy"];

            AssertEqual("2.9", migrated.SaveVersion, "Legacy save must upgrade to version 2.9.");
            AssertEqual(7, instance.AnchorX, "Legacy placement must inherit plot X.");
            AssertEqual(-3, instance.AnchorY, "Legacy placement must inherit plot Y.");
            AssertEqual(5, instance.BaseLayer, "Legacy layer must migrate to base layer.");
            AssertEqual(1, instance.PlacedWidth, "Legacy placement width must default to one.");
            AssertEqual(1, instance.PlacedDepth, "Legacy placement depth must default to one.");
            AssertEqual(1, instance.PlacedHeight, "Legacy placement height must default to one.");
            AssertEqual(SpatialPlacementSchema.CurrentVersion, instance.PlacementSchemaVersion, "Legacy placement must use current schema after migration.");
        }

        private static void SaveMigrationRejectsUnsupportedPlacementSchema()
        {
            GameState state = new GameState();
            state.Buildings.Instances["building:core:future"] = new BuildingInstanceState
            {
                BuildingId = "building:core:future",
                DefinitionId = "building:core:farm",
                PlotId = "plot:core:future",
                PlacedWidth = 1,
                PlacedDepth = 1,
                PlacedHeight = 1,
                PlacementSchemaVersion = SpatialPlacementSchema.CurrentVersion + 1
            };

            SaveSystem saves = new SaveSystem();
            AssertThrows<InvalidOperationException>(
                () => saves.Deserialize(saves.Serialize(state)),
                "Future placement schema must not be silently accepted.");
        }

        private static void SaveMigrationRejectsFootprintOutsidePlot()
        {
            GameState state = new GameState();
            state.World.Plots["plot:core:crafted"] = new PlotState
            {
                PlotId = "plot:core:crafted",
                X = 1,
                Y = 2
            };
            state.Buildings.Instances["building:core:crafted"] = new BuildingInstanceState
            {
                BuildingId = "building:core:crafted",
                DefinitionId = "building:core:farm",
                PlotId = "plot:core:crafted",
                AnchorX = 1,
                AnchorY = 2,
                PlacedWidth = 3,
                PlacedDepth = 3,
                PlacedHeight = 1,
                PlacementSchemaVersion = SpatialPlacementSchema.CurrentVersion
            };

            SaveSystem saves = new SaveSystem();
            AssertThrows<InvalidOperationException>(
                () => saves.Deserialize(saves.Serialize(state)),
                "Variable footprint outside its plot must be rejected.");
        }

        private static void SpatialPlacementSurvivesSaveRoundTrip()
        {
            GameState state = new GameState();
            state.World.Plots["plot:core:roundtrip"] = new PlotState
            {
                PlotId = "plot:core:roundtrip",
                X = 2,
                Y = 9
            };
            state.Buildings.ConstructionTasks["construction:core:roundtrip"] = new ConstructionTaskState
            {
                TaskId = "construction:core:roundtrip",
                BuildingId = "building:core:roundtrip",
                DefinitionId = "building:core:farm",
                PlotId = "plot:core:roundtrip",
                Layer = 2,
                AnchorX = 2,
                AnchorY = 9,
                BaseLayer = 2,
                PlacedWidth = 1,
                PlacedDepth = 1,
                PlacedHeight = 1,
                PlacementSchemaVersion = SpatialPlacementSchema.CurrentVersion
            };
            SaveSystem saves = new SaveSystem();
            string beforeHash = StateDiagnostics.CalculateStateHash(state);

            GameState restored = saves.Deserialize(saves.Serialize(state));

            AssertEqual(beforeHash, StateDiagnostics.CalculateStateHash(restored), "Valid spatial placement must preserve StateHash after round trip.");
            AssertEqual(2, restored.Buildings.ConstructionTasks["construction:core:roundtrip"].BaseLayer, "Base layer must survive round trip.");
        }

        private static void SpatialOccupancyEmitsDeterministicCells()
        {
            SpatialPlacement placement = CreateSpatialPlacement("building:core:multi", 10, 20, 3, 2, 2, 2);

            IReadOnlyList<SpatialGridCell> cells = SpatialOccupancy.GetOccupiedCells(placement);

            AssertEqual(8, cells.Count, "Expected every occupied cell to be emitted.");
            AssertEqual(new SpatialGridCell(10, 20, 3), cells[0], "Cell order must begin at the minimum coordinate.");
            AssertEqual(new SpatialGridCell(11, 20, 3), cells[1], "X must advance first.");
            AssertEqual(new SpatialGridCell(10, 21, 3), cells[2], "Y must advance after X.");
            AssertEqual(new SpatialGridCell(10, 20, 4), cells[4], "Layer must advance after each XY plane.");
        }

        private static void SpatialOccupancyRotatesRectangularFootprints()
        {
            SpatialPlacement placement = CreateSpatialPlacement("building:core:rotated", 5, 7, 0, 2, 3, 1, 1);

            IReadOnlyList<SpatialGridCell> cells = SpatialOccupancy.GetOccupiedCells(placement);

            AssertEqual(6, cells.Count, "Rotation must preserve occupied cell count.");
            AssertTrue(cells.Contains(new SpatialGridCell(7, 8, 0)), "Quarter turn must swap width and depth around the minimum anchor.");
            AssertFalse(cells.Contains(new SpatialGridCell(5, 9, 0)), "Rotated depth must use the original width.");
        }

        private static void SpatialOccupancyAcceptsAdjacentPlacement()
        {
            SpatialBounds bounds = new SpatialBounds(0, 0, 0, 8, 8, 4);
            SpatialPlacement existing = CreateSpatialPlacement("building:core:left", 0, 0, 0, 2, 2, 1);
            SpatialPlacement candidate = CreateSpatialPlacement("building:core:right", 2, 0, 0, 2, 2, 1);

            SpatialPlacementResult result = SpatialOccupancy.ValidatePlacement(candidate, bounds, new[] { existing });

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(SpatialPlacementIssueCode.None, result.Code, "Accepted placement must not carry an issue code.");
        }

        private static void SpatialOccupancyReportsOverlapDetails()
        {
            SpatialBounds bounds = new SpatialBounds(0, 0, 0, 8, 8, 4);
            SpatialPlacement existing = CreateSpatialPlacement("building:core:existing", 0, 0, 0, 2, 2, 1);
            SpatialPlacement candidate = CreateSpatialPlacement("building:core:candidate", 1, 0, 0, 2, 2, 1);

            SpatialPlacementResult result = SpatialOccupancy.ValidatePlacement(candidate, bounds, new[] { existing });

            AssertFalse(result.Accepted, "Overlapping placement must be rejected.");
            AssertEqual(SpatialPlacementIssueCode.Overlap, result.Code, "Expected overlap issue code.");
            AssertEqual(new SpatialGridCell(1, 0, 0), result.Cell.Value, "Expected first deterministic conflict cell.");
            AssertEqual(existing.ObjectId, result.ConflictingObjectId, "Expected conflicting object id.");
        }

        private static void SpatialOccupancyTreatsReservationsAsOccupied()
        {
            SpatialBounds bounds = new SpatialBounds(0, 0, 0, 8, 8, 4);
            SpatialPlacement reservation = CreateSpatialPlacement("construction:core:reserved", 3, 3, 1, 2, 2, 2);
            SpatialPlacement candidate = CreateSpatialPlacement("building:core:candidate", 4, 4, 2);

            SpatialPlacementResult result = SpatialOccupancy.ValidatePlacement(candidate, bounds, new[] { reservation });

            AssertEqual(SpatialPlacementIssueCode.Overlap, result.Code, "Construction reservations must block every reserved layer.");
            AssertEqual(reservation.ObjectId, result.ConflictingObjectId, "Expected reservation id in conflict details.");
        }

        private static void SpatialOccupancyRejectsBoundsViolations()
        {
            SpatialBounds bounds = new SpatialBounds(0, 0, 0, 4, 4, 4);
            SpatialPlacement negative = CreateSpatialPlacement("building:core:negative", -1, 0, 0);
            SpatialPlacement tooHigh = CreateSpatialPlacement("building:core:high", 0, 0, 3, 1, 1, 2);

            SpatialPlacementResult negativeResult = SpatialOccupancy.ValidatePlacement(negative, bounds, Array.Empty<SpatialPlacement>());
            SpatialPlacementResult highResult = SpatialOccupancy.ValidatePlacement(tooHigh, bounds, Array.Empty<SpatialPlacement>());

            AssertEqual(SpatialPlacementIssueCode.OutOfBounds, negativeResult.Code, "Negative X must be rejected by bounds.");
            AssertEqual(new SpatialGridCell(-1, 0, 0), negativeResult.Cell.Value, "Expected the first out-of-bounds cell.");
            AssertEqual(SpatialPlacementIssueCode.OutOfBounds, highResult.Code, "Placement height must respect upper layer bounds.");
        }

        private static void SpatialOccupancyRejectsInvalidShapeAndRotation()
        {
            SpatialBounds bounds = new SpatialBounds(0, 0, 0, 4, 4, 4);
            SpatialPlacement invalidShape = CreateSpatialPlacement("building:core:shape", 0, 0, 0, 0, 1, 1);
            SpatialPlacement invalidRotation = CreateSpatialPlacement("building:core:rotation", 0, 0, 0, 1, 1, 1, 4);

            AssertEqual(
                SpatialPlacementIssueCode.InvalidDimensions,
                SpatialOccupancy.ValidatePlacement(invalidShape, bounds, Array.Empty<SpatialPlacement>()).Code,
                "Zero-sized placement must be rejected.");
            AssertEqual(
                SpatialPlacementIssueCode.InvalidRotation,
                SpatialOccupancy.ValidatePlacement(invalidRotation, bounds, Array.Empty<SpatialPlacement>()).Code,
                "Rotation outside 0-3 must be rejected.");
            AssertThrows<ArgumentException>(
                () => SpatialOccupancy.GetOccupiedCells(invalidShape),
                "Direct occupancy expansion must reject invalid shape.");
        }

        private static void SpatialOccupancyRejectsExcessiveFootprints()
        {
            SpatialBounds bounds = new SpatialBounds(0, 0, 0, 300, 300, 300);
            SpatialPlacement excessiveDimension = CreateSpatialPlacement("building:core:wide", 0, 0, 0, 257, 1, 1);
            SpatialPlacement excessiveCells = CreateSpatialPlacement("building:core:huge", 0, 0, 0, 256, 256, 2);

            AssertEqual(
                SpatialPlacementIssueCode.FootprintTooLarge,
                SpatialOccupancy.ValidatePlacement(excessiveDimension, bounds, Array.Empty<SpatialPlacement>()).Code,
                "Oversized dimension must be rejected before allocation.");
            AssertEqual(
                SpatialPlacementIssueCode.FootprintTooLarge,
                SpatialOccupancy.ValidatePlacement(excessiveCells, bounds, Array.Empty<SpatialPlacement>()).Code,
                "Oversized cell count must be rejected before allocation.");
        }

        private static void SpatialOccupancyRejectsCoordinateOverflow()
        {
            SpatialBounds bounds = new SpatialBounds(0, 0, 0, 4, 4, 4);
            SpatialPlacement candidate = CreateSpatialPlacement("building:core:overflow", int.MaxValue, 0, 0, 2, 1, 1);

            SpatialPlacementResult result = SpatialOccupancy.ValidatePlacement(candidate, bounds, Array.Empty<SpatialPlacement>());

            AssertEqual(SpatialPlacementIssueCode.CoordinateOverflow, result.Code, "Coordinate overflow must produce a stable rejection code.");
        }

        private static void SpatialOccupancyRejectsCorruptExistingState()
        {
            SpatialBounds bounds = new SpatialBounds(0, 0, 0, 4, 4, 4);
            SpatialPlacement first = CreateSpatialPlacement("building:core:first", 0, 0, 0);
            SpatialPlacement second = CreateSpatialPlacement("building:core:second", 0, 0, 0);
            SpatialPlacement candidate = CreateSpatialPlacement("building:core:candidate", 3, 3, 3);

            AssertThrows<InvalidOperationException>(
                () => SpatialOccupancy.ValidatePlacement(candidate, bounds, new[] { first, second }),
                "Overlapping authoritative state must fail closed.");
        }

        private static void StructuralSupportAcceptsGroundedNode()
        {
            StructuralNode grounded = CreateStructuralNode("building:core:grounded", 0, 0, 0, 2, 2, 1, 4, 0, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { grounded });

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(0, result.Edges.Count, "Ground support must not create a hidden building edge.");
            AssertEqual(0L, result.ReceivedLoadUnits[grounded.ObjectId], "Grounded node must not receive upper load.");
        }

        private static void StructuralSupportRejectsUnsupportedNode()
        {
            StructuralNode floating = CreateStructuralNode("building:core:floating", 0, 0, 1, 1, 1, 1, 1, 0, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { floating });

            AssertEqual(StructuralSupportIssueCode.Unsupported, result.Code, "Floating node must require explicit support.");
            AssertEqual(floating.ObjectId, result.ObjectId, "Rejection must identify the unsupported node.");
        }

        private static void StructuralSupportAcceptsExactHalfContact()
        {
            StructuralNode left = CreateStructuralNode("building:core:left_support", 0, 0, 0, 1, 1, 1, 0, 10, true);
            StructuralNode right = CreateStructuralNode("building:core:right_support", 1, 0, 0, 1, 1, 1, 0, 10, true);
            StructuralNode upper = CreateStructuralNode("building:core:half_supported", 0, 0, 1, 2, 2, 1, 4, 0, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { upper, right, left });

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(2, result.Edges.Count, "Two supporting buildings must create two merged support edges.");
        }

        private static void StructuralSupportRejectsContactBelowThreshold()
        {
            StructuralNode support = CreateStructuralNode("building:core:single_support", 0, 0, 0, 1, 1, 1, 0, 10, true);
            StructuralNode upper = CreateStructuralNode("building:core:under_supported", 0, 0, 1, 2, 2, 1, 4, 0, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { support, upper });

            AssertEqual(StructuralSupportIssueCode.InsufficientContact, result.Code, "One of four bottom cells must be below the 50 percent threshold.");
            AssertEqual(2, result.RequiredContactCells, "Expected two required support cells.");
            AssertEqual(1, result.ActualContactCells, "Expected one actual support cell.");
        }

        private static void StructuralSupportDistributesCommonLoadDeterministically()
        {
            StructuralNode narrow = CreateStructuralNode("building:core:a_narrow", 0, 0, 0, 1, 1, 1, 0, 10, true);
            StructuralNode wide = CreateStructuralNode("building:core:b_wide", 1, 0, 0, 2, 1, 1, 0, 10, true);
            StructuralNode upper = CreateStructuralNode("building:core:upper", 0, 0, 1, 3, 1, 1, 10, 0, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { wide, upper, narrow });

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(3333L, result.ReceivedLoadUnits[narrow.ObjectId], "One contact cell must receive one third rounded down.");
            AssertEqual(6667L, result.ReceivedLoadUnits[wide.ObjectId], "Two contact cells must receive the deterministic remainder.");
            AssertEqual(10000L, result.Edges.Sum(edge => edge.LoadUnits), "Distributed load must be conserved exactly.");
        }

        private static void StructuralSupportPropagatesMultiLayerLoad()
        {
            StructuralNode foundation = CreateStructuralNode("building:core:foundation", 0, 0, 0, 1, 1, 1, 1, 10, true);
            StructuralNode middle = CreateStructuralNode("building:core:middle", 0, 0, 1, 1, 1, 1, 2, 10, true);
            StructuralNode top = CreateStructuralNode("building:core:top", 0, 0, 2, 1, 1, 1, 3, 0, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { middle, foundation, top });

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(3000L, result.ReceivedLoadUnits[middle.ObjectId], "Middle node must receive top weight.");
            AssertEqual(5000L, result.ReceivedLoadUnits[foundation.ObjectId], "Foundation must receive middle weight plus propagated top weight.");
        }

        private static void StructuralSupportRejectsExceededCapacity()
        {
            StructuralNode foundation = CreateStructuralNode("building:core:weak", 0, 0, 0, 1, 1, 1, 1, 4, true);
            StructuralNode upper = CreateStructuralNode("building:core:heavy", 0, 0, 1, 1, 1, 1, 5, 0, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { foundation, upper });

            AssertEqual(StructuralSupportIssueCode.CapacityExceeded, result.Code, "Load above capacity must be rejected.");
            AssertEqual(foundation.ObjectId, result.ObjectId, "Capacity rejection must identify the overloaded supporter.");
            AssertEqual(5000L, result.LoadUnits, "Expected exact received load units.");
            AssertEqual(4000L, result.CapacityUnits, "Expected exact capacity units.");
        }

        private static void StructuralSupportDoesNotUseConstructionAsSupport()
        {
            StructuralNode construction = CreateStructuralNode("construction:core:pending", 0, 0, 0, 1, 1, 1, 1, 10, false);
            StructuralNode upper = CreateStructuralNode("building:core:upper", 0, 0, 1, 1, 1, 1, 1, 0, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { construction, upper });

            AssertEqual(StructuralSupportIssueCode.Unsupported, result.Code, "Construction reservation must not provide finished structural support.");
        }

        private static void StructuralSupportRejectsOverlappingNodes()
        {
            StructuralNode first = CreateStructuralNode("building:core:first_overlap", 0, 0, 0, 1, 1, 1, 1, 1, true);
            StructuralNode second = CreateStructuralNode("building:core:second_overlap", 0, 0, 0, 1, 1, 1, 1, 1, true);

            StructuralSupportResult result = StructuralSupport.Validate(new[] { first, second });

            AssertEqual(StructuralSupportIssueCode.SpatialStateInvalid, result.Code, "Overlapping structural state must fail closed.");
        }

        private static void StructuralSupportIgnoresRegistrationOrder()
        {
            StructuralNode narrow = CreateStructuralNode("building:core:a_order", 0, 0, 0, 1, 1, 1, 0, 10, true);
            StructuralNode wide = CreateStructuralNode("building:core:b_order", 1, 0, 0, 2, 1, 1, 0, 10, true);
            StructuralNode upper = CreateStructuralNode("building:core:z_order", 0, 0, 1, 3, 1, 1, 7, 0, true);

            StructuralSupportResult forward = StructuralSupport.Validate(new[] { narrow, wide, upper });
            StructuralSupportResult reverse = StructuralSupport.Validate(new[] { upper, wide, narrow });

            AssertTrue(forward.Accepted, forward.Reason);
            AssertTrue(reverse.Accepted, reverse.Reason);
            AssertEqual(forward.ReceivedLoadUnits[narrow.ObjectId], reverse.ReceivedLoadUnits[narrow.ObjectId], "Narrow support load must ignore registration order.");
            AssertEqual(forward.ReceivedLoadUnits[wide.ObjectId], reverse.ReceivedLoadUnits[wide.ObjectId], "Wide support load must ignore registration order.");
            AssertEqual(
                string.Join("|", forward.Edges.Select(edge => $"{edge.SupportedObjectId}>{edge.SupporterObjectId}:{edge.LoadUnits}")),
                string.Join("|", reverse.Edges.Select(edge => $"{edge.SupportedObjectId}>{edge.SupporterObjectId}:{edge.LoadUnits}")),
                "Support edge ordering and load must be deterministic.");
        }

        private static StructuralNode CreateStructuralNode(
            string objectId,
            int anchorX,
            int anchorY,
            int baseLayer,
            int width,
            int depth,
            int height,
            int weight,
            int carryCapacity,
            bool canSupport)
        {
            return new StructuralNode(
                objectId,
                CreateSpatialPlacement(objectId, anchorX, anchorY, baseLayer, width, depth, height),
                weight,
                carryCapacity,
                canSupport);
        }

        private static void BuildingSystemPlacesRotatedVariableFootprint()
        {
            Simulation simulation = CreateSpatialBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);

            CommandResult result = session.SendCommand(CreateSpatialBuildCommand(
                "command:core:spatial_build",
                "player:core:local",
                1,
                11,
                21,
                0,
                1));

            AssertTrue(result.Accepted, result.Reason);
            ConstructionTaskState task = simulation.State.Buildings.ConstructionTasks.Values.Single();
            AssertEqual(11, task.AnchorX, "Task must preserve explicit anchor X.");
            AssertEqual(21, task.AnchorY, "Task must preserve explicit anchor Y.");
            AssertEqual(1, task.RotationQuarterTurns, "Task must preserve quarter-turn rotation.");
            AssertEqual(2, task.PlacedWidth, "Task must snapshot unrotated definition width.");
            AssertEqual(3, task.PlacedDepth, "Task must snapshot unrotated definition depth.");

            BuildingPlacedPayload payload = result.Events.Single().Payload.Deserialize<BuildingPlacedPayload>(SaveSystem.CreateDefaultJsonOptions());
            AssertEqual(task.AnchorX, payload.AnchorX, "Placed event must expose authoritative anchor.");
            AssertEqual(task.RotationQuarterTurns, payload.RotationQuarterTurns, "Placed event must expose authoritative rotation.");
        }

        private static void BuildingSystemRejectsOverlapWithStableCode()
        {
            Simulation simulation = CreateSpatialBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(
                session.SendCommand(CreateSpatialBuildCommand("command:core:first_room", "player:core:local", 1, 10, 20, 0, 0)).Accepted,
                "Expected first room reservation to succeed.");

            CommandResult overlap = session.SendCommand(CreateSpatialBuildCommand(
                "command:core:overlap_room",
                "player:core:local",
                2,
                11,
                20,
                0,
                0));

            AssertFalse(overlap.Accepted, "Overlapping construction reservation must be rejected.");
            AssertEqual(CommandErrorCodes.SpatialOverlap, overlap.Code, "Overlap must use a stable command error code.");
            AssertEqual(1, simulation.State.Buildings.ConstructionTasks.Count, "Rejected overlap must not mutate construction state.");
        }

        private static void BuildingSystemRejectsPlotBoundaryCrossing()
        {
            Simulation simulation = CreateSpatialBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);

            CommandResult result = session.SendCommand(CreateSpatialBuildCommand(
                "command:core:cross_boundary",
                "player:core:local",
                1,
                14,
                24,
                0,
                1));

            AssertFalse(result.Accepted, "Footprint crossing the selected plot boundary must be rejected.");
            AssertEqual(CommandErrorCodes.SpatialOutOfBounds, result.Code, "Boundary rejection must use a stable error code.");
            AssertEqual(0, simulation.State.Buildings.ConstructionTasks.Count, "Boundary rejection must not create a task.");
        }

        private static void BuildingSystemRejectsPartialAnchorCoordinates()
        {
            Simulation simulation = CreateSpatialBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            CommandEnvelope command = CreateSpatialBuildCommand(
                "command:core:partial_anchor", "player:core:local", 1, 10, 20, 0, 0);
            BuildCommandPayload payload = command.Payload.Deserialize<BuildCommandPayload>(SaveSystem.CreateDefaultJsonOptions());
            payload.AnchorY = null;
            command.Payload = JsonSerializer.SerializeToElement(payload, SaveSystem.CreateDefaultJsonOptions());

            CommandResult result = session.SendCommand(command);

            AssertFalse(result.Accepted, "Partial anchor coordinates must be rejected.");
            AssertEqual(CommandErrorCodes.SpatialInvalidCoordinates, result.Code, "Partial anchor must use a stable error code.");
        }

        private static void ConstructionCompletionPreservesSpatialSnapshot()
        {
            Simulation simulation = CreateSpatialBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(
                session.SendCommand(CreateSpatialBuildCommand("command:core:complete_room", "player:core:local", 1, 12, 22, 0, 3)).Accepted,
                "Expected room construction to start.");

            IReadOnlyList<GameEvent> events = session.Tick(2);
            GameEvent completed = events.Single(gameEvent => gameEvent.EventType == BuildingSystem.ConstructionCompletedEvent);
            ConstructionCompletedPayload payload = completed.Payload.Deserialize<ConstructionCompletedPayload>(SaveSystem.CreateDefaultJsonOptions());
            BuildingInstanceState instance = simulation.State.Buildings.Instances.Values.Single();

            AssertEqual(12, instance.AnchorX, "Completed instance must preserve task anchor.");
            AssertEqual(3, instance.RotationQuarterTurns, "Completed instance must preserve task rotation.");
            AssertEqual(instance.AnchorX, payload.AnchorX, "Completion event must expose preserved anchor.");
            AssertEqual(instance.PlacedDepth, payload.PlacedDepth, "Completion event must expose preserved footprint snapshot.");
            AssertEqual(0, simulation.State.Buildings.ConstructionTasks.Count, "Completed task must be removed.");
        }

        private static void SaveMigrationAcceptsValidVariableFootprint()
        {
            GameState state = new GameState();
            state.World.Plots["plot:core:room"] = new PlotState
            {
                PlotId = "plot:core:room",
                X = 5,
                Y = 8,
                Width = 5,
                Depth = 5,
                MaxStackLayers = 3
            };
            state.Buildings.Instances["building:core:room"] = CreateBuildingState(
                "building:core:room",
                "plot:core:room",
                6,
                9,
                0,
                2,
                3,
                1,
                1);

            SaveSystem saves = new SaveSystem();
            GameState restored = saves.Deserialize(saves.Serialize(state));

            AssertEqual(2, restored.Buildings.Instances["building:core:room"].PlacedWidth, "Valid variable footprint must survive save load.");
            AssertEqual(1, restored.Buildings.Instances["building:core:room"].RotationQuarterTurns, "Valid rotation must survive save load.");
        }

        private static void SaveMigrationRejectsOverlappingSpatialState()
        {
            GameState state = new GameState();
            state.World.Plots["plot:core:overlap"] = new PlotState
            {
                PlotId = "plot:core:overlap",
                Width = 4,
                Depth = 4,
                MaxStackLayers = 2
            };
            state.Buildings.Instances["building:core:first"] = CreateBuildingState(
                "building:core:first", "plot:core:overlap", 0, 0, 0, 2, 2, 1, 0);
            state.Buildings.Instances["building:core:second"] = CreateBuildingState(
                "building:core:second", "plot:core:overlap", 1, 1, 0, 2, 2, 1, 0);

            SaveSystem saves = new SaveSystem();
            AssertThrows<InvalidOperationException>(
                () => saves.Deserialize(saves.Serialize(state)),
                "Overlapping authoritative save state must fail closed.");
        }

        private static void StateDiagnosticsAcceptsAdjacentSameLayerBuildings()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition { DefinitionId = "building:core:farm" });
            GameState state = new GameState();
            state.World.Plots["plot:core:adjacent"] = new PlotState
            {
                PlotId = "plot:core:adjacent",
                Width = 4,
                Depth = 4
            };
            state.Buildings.Instances["building:core:left"] = CreateBuildingState(
                "building:core:left", "plot:core:adjacent", 0, 0, 0, 1, 1, 1, 0);
            state.Buildings.Instances["building:core:right"] = CreateBuildingState(
                "building:core:right", "plot:core:adjacent", 1, 0, 0, 1, 1, 1, 0);

            IReadOnlyList<DiagnosticIssue> issues = StateDiagnostics.CheckInvariants(state, definitions);

            AssertFalse(issues.Any(issue => issue.Code == "building.layer.occupied"), "Adjacent same-layer buildings must not be diagnosed as overlapping.");
            AssertFalse(issues.Any(issue => issue.Code == "placement.spatial.out_of_bounds"), "Adjacent buildings must remain inside plot bounds.");
        }

        private static void RemoteBuildingUsesAuthoritativeSpatialValidation()
        {
            Simulation simulation = CreateSpatialBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);

            CommandResult first = remote.SendCommand(CreateSpatialBuildCommand(
                "command:core:remote_room", "player:core:remote", 1, 10, 20, 0, 0));
            CommandResult overlap = remote.SendCommand(CreateSpatialBuildCommand(
                "command:core:remote_overlap", "player:core:remote", 2, 11, 20, 0, 0));

            AssertTrue(first.Accepted, first.Reason);
            AssertFalse(overlap.Accepted, "Server must reject remote spatial overlap.");
            AssertEqual(CommandErrorCodes.SpatialOverlap, overlap.Code, "Remote rejection must preserve authoritative spatial error code.");
            AssertEqual(1, server.CurrentState.Buildings.ConstructionTasks.Count, "Server state must contain only the accepted reservation.");
            AssertEqual(1, remote.CurrentState.Buildings.ConstructionTasks.Count, "Remote snapshot must remain synchronized after rejection.");
        }

        private static void BuildingRejectsSupportFromUnfinishedConstruction()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(
                session.SendCommand(CreateStructuralBuildCommand(
                    "command:core:start_pillar", "player:core:local", 1, "building:core:pillar", 0, 0, 0)).Accepted,
                "Expected ground pillar construction to start.");

            CommandResult upper = session.SendCommand(CreateStructuralBuildCommand(
                "command:core:early_upper", "player:core:local", 2, "building:core:light", 0, 0, 1));

            AssertFalse(upper.Accepted, "Unfinished construction must not support an upper building.");
            AssertEqual(CommandErrorCodes.StructuralUnsupported, upper.Code, "Expected stable unsupported error code.");
            AssertEqual(1, simulation.State.Buildings.ConstructionTasks.Count, "Rejected upper building must not create a task.");
        }

        private static void BuildingAcceptsSupportAfterConstructionCompletes()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(
                session.SendCommand(CreateStructuralBuildCommand(
                    "command:core:complete_pillar", "player:core:local", 1, "building:core:pillar", 0, 0, 0)).Accepted,
                "Expected pillar construction to start.");
            session.Tick(1);

            CommandResult upper = session.SendCommand(CreateStructuralBuildCommand(
                "command:core:supported_upper", "player:core:local", 2, "building:core:light", 0, 0, 1));

            AssertTrue(upper.Accepted, upper.Reason);
            AssertEqual(1, simulation.State.Buildings.Instances.Count, "Completed pillar must remain as structural support.");
            AssertEqual(1, simulation.State.Buildings.ConstructionTasks.Count, "Accepted upper building must create one task.");
        }

        private static void BuildingAcceptsDeterministicCommonSupport()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateStructuralBuildCommand(
                "command:core:left_pillar", "player:core:local", 1, "building:core:pillar", 0, 0, 0)).Accepted, "Expected left pillar.");
            session.Tick(1);
            AssertTrue(session.SendCommand(CreateStructuralBuildCommand(
                "command:core:right_pillar", "player:core:local", 2, "building:core:pillar", 1, 0, 0)).Accepted, "Expected right pillar.");
            session.Tick(1);

            CommandResult platform = session.SendCommand(CreateStructuralBuildCommand(
                "command:core:common_platform", "player:core:local", 3, "building:core:platform", 0, 0, 1));

            AssertTrue(platform.Accepted, platform.Reason);
            AssertEqual(2, simulation.State.Buildings.Instances.Count, "Both completed pillars must remain.");
            AssertEqual(1, simulation.State.Buildings.ConstructionTasks.Count, "Platform must be reserved once.");
        }

        private static void BuildingRejectsStructuralCapacityOverflow()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateStructuralBuildCommand(
                "command:core:weak_pillar", "player:core:local", 1, "building:core:pillar", 0, 0, 0)).Accepted, "Expected pillar.");
            session.Tick(1);

            CommandResult heavy = session.SendCommand(CreateStructuralBuildCommand(
                "command:core:heavy_upper", "player:core:local", 2, "building:core:heavy", 0, 0, 1));

            AssertFalse(heavy.Accepted, "Weight above carry capacity must be rejected.");
            AssertEqual(CommandErrorCodes.StructuralCapacityExceeded, heavy.Code, "Expected stable capacity error code.");
        }

        private static void DemolitionStartsStructuralGrace()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            BuildAndComplete(session, 1, "pillar_for_grace", "building:core:pillar", 0);
            BuildAndComplete(session, 2, "upper_for_grace", "building:core:light", 1);

            long demolitionTick = simulation.State.SimulationTick;
            CommandResult result = session.SendCommand(CreateDemolishCommand(
                "command:core:remove_grace_pillar", "player:core:local", 3, "building:core:000000000001"));

            AssertTrue(result.Accepted, result.Reason);
            BuildingInstanceState upper = simulation.State.Buildings.Instances["building:core:000000000002"];
            AssertEqual(BuildingStructuralStatuses.Grace, upper.StructuralStatus, "Unsupported upper building must enter grace.");
            AssertEqual(
                demolitionTick + GameTime.TicksFromRealMinutesAtNormalSpeed(30),
                upper.StructuralGraceDeadlineTick,
                "Normal difficulty must assign a 30-minute deadline.");
            AssertTrue(result.Events.Any(gameEvent => gameEvent.EventType == BuildingSystem.StructuralGraceStartedEvent),
                "Demolition must emit a structural grace event.");
        }

        private static void RepairBeforeDeadlineRestoresStability()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            BuildAndComplete(session, 1, "pillar_for_repair", "building:core:pillar", 0);
            BuildAndComplete(session, 2, "upper_for_repair", "building:core:light", 1);
            AssertTrue(session.SendCommand(CreateDemolishCommand(
                "command:core:remove_repair_pillar", "player:core:local", 3, "building:core:000000000001")).Accepted,
                "Expected support demolition to succeed.");

            CommandResult repair = session.SendCommand(CreateStructuralBuildCommand(
                "command:core:replacement_pillar", "player:core:local", 4, "building:core:pillar", 0, 0, 0));
            AssertTrue(repair.Accepted, repair.Reason);
            IReadOnlyList<GameEvent> completionEvents = session.Tick(1);

            BuildingInstanceState upper = simulation.State.Buildings.Instances["building:core:000000000002"];
            AssertEqual(BuildingStructuralStatuses.Normal, upper.StructuralStatus, "Completed replacement support must restore stability.");
            AssertEqual(0L, upper.StructuralGraceDeadlineTick, "Restored building must clear its deadline.");
            AssertTrue(completionEvents.Any(gameEvent => gameEvent.EventType == BuildingSystem.StructuralStabilityRestoredEvent),
                "Repair completion must emit a stability-restored event.");
        }

        private static void AutomaticCollapseUsesDeterministicOrder()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            simulation.State.Difficulty = new DifficultyState
            {
                DifficultyId = DifficultyIds.Custom,
                CustomStructuralGraceTicks = 0,
                CustomCollapseIntervalTicks = GameTime.TicksFromRealSecondsAtNormalSpeed(1),
                CustomStructuralFailureMode = StructuralFailureModes.AutomaticCollapse
            };
            LocalGameSession session = new LocalGameSession(simulation);
            BuildAndComplete(session, 1, "tower_ground", "building:core:pillar", 0);
            BuildAndComplete(session, 2, "tower_middle", "building:core:pillar", 1);
            BuildAndComplete(session, 3, "tower_top", "building:core:pillar", 2);

            CommandResult result = session.SendCommand(CreateDemolishCommand(
                "command:core:remove_tower_ground", "player:core:local", 4, "building:core:000000000001"));

            AssertTrue(result.Accepted, result.Reason);
            AssertTrue(simulation.State.Buildings.Instances["building:core:000000000003"].IsDestroyed,
                "Highest expired building must collapse first.");
            AssertFalse(simulation.State.Buildings.Instances["building:core:000000000002"].IsDestroyed,
                "Lower building must wait for the next collapse interval.");
            session.Tick(GameTime.TicksFromRealSecondsAtNormalSpeed(1));
            AssertTrue(simulation.State.Buildings.Instances["building:core:000000000002"].IsDestroyed,
                "Next interval must collapse the remaining unstable building.");
        }

        private static void DisableOnlyModePreservesBuildings()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            simulation.State.Difficulty = new DifficultyState
            {
                DifficultyId = DifficultyIds.Custom,
                CustomStructuralGraceTicks = 0,
                CustomCollapseIntervalTicks = GameTime.TicksFromRealSecondsAtNormalSpeed(1),
                CustomStructuralFailureMode = StructuralFailureModes.DisableOnly
            };
            LocalGameSession session = new LocalGameSession(simulation);
            BuildAndComplete(session, 1, "disable_ground", "building:core:pillar", 0);
            BuildAndComplete(session, 2, "disable_upper", "building:core:light", 1);

            CommandResult result = session.SendCommand(CreateDemolishCommand(
                "command:core:disable_remove_ground", "player:core:local", 3, "building:core:000000000001"));

            BuildingInstanceState upper = simulation.State.Buildings.Instances["building:core:000000000002"];
            AssertTrue(result.Accepted, result.Reason);
            AssertFalse(upper.IsDestroyed, "Disable-only mode must preserve the building instance.");
            AssertEqual(BuildingStructuralStatuses.Disabled, upper.StructuralStatus, "Expired building must become disabled.");
        }

        private static void LargeTickProcessesCollapseIntervals()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            simulation.State.Difficulty = new DifficultyState
            {
                DifficultyId = DifficultyIds.Custom,
                CustomStructuralGraceTicks = 10,
                CustomCollapseIntervalTicks = GameTime.TicksFromRealSecondsAtNormalSpeed(1),
                CustomStructuralFailureMode = StructuralFailureModes.AutomaticCollapse
            };
            simulation.State.Buildings.Instances["building:core:ground"] = CreateBuildingStateWithDefinition(
                "building:core:ground", "building:core:pillar", "plot:core:structure", 0, 0, 0, 1, 1, 1);
            simulation.State.Buildings.Instances["building:core:middle"] = CreateBuildingStateWithDefinition(
                "building:core:middle", "building:core:pillar", "plot:core:structure", 0, 0, 1, 1, 1, 1);
            simulation.State.Buildings.Instances["building:core:top"] = CreateBuildingStateWithDefinition(
                "building:core:top", "building:core:pillar", "plot:core:structure", 0, 0, 2, 1, 1, 1);
            simulation.State.Buildings.Instances["building:core:ground"].IsDestroyed = true;
            simulation.State.Buildings.Instances["building:core:middle"].StructuralStatus = BuildingStructuralStatuses.Grace;
            simulation.State.Buildings.Instances["building:core:middle"].StructuralGraceDeadlineTick = 10;
            simulation.State.Buildings.Instances["building:core:top"].StructuralStatus = BuildingStructuralStatuses.Grace;
            simulation.State.Buildings.Instances["building:core:top"].StructuralGraceDeadlineTick = 10;

            simulation.Tick(10 + GameTime.TicksFromRealSecondsAtNormalSpeed(1));

            AssertTrue(simulation.State.Buildings.Instances["building:core:top"].IsDestroyed,
                "Large tick must process the first due collapse.");
            AssertTrue(simulation.State.Buildings.Instances["building:core:middle"].IsDestroyed,
                "Large tick must process every elapsed collapse interval.");
        }

        private static void StructuralIncidentSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            BuildAndComplete(session, 1, "save_ground", "building:core:pillar", 0);
            BuildAndComplete(session, 2, "save_upper", "building:core:light", 1);
            AssertTrue(session.SendCommand(CreateDemolishCommand(
                "command:core:save_remove_ground", "player:core:local", 3, "building:core:000000000001")).Accepted,
                "Expected demolition before save.");

            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            BuildingInstanceState upper = loaded.Buildings.Instances["building:core:000000000002"];

            AssertEqual("2.9", loaded.SaveVersion, "Structural incident save must use version 2.9.");
            AssertEqual(BuildingStructuralStatuses.Grace, upper.StructuralStatus, "Grace status must survive save round trip.");
            AssertEqual(
                simulation.State.Buildings.Instances["building:core:000000000002"].StructuralGraceDeadlineTick,
                upper.StructuralGraceDeadlineTick,
                "Grace deadline must survive save round trip.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State), StateDiagnostics.CalculateStateHash(loaded),
                "Structural incident state hash must survive save round trip.");
        }

        private static void RemoteDemolitionSynchronizesGrace()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            AssertTrue(remote.SendCommand(CreateStructuralBuildCommand(
                "command:core:remote_incident_ground", "player:core:remote", 1, "building:core:pillar", 0, 0, 0)).Accepted,
                "Expected remote ground construction.");
            server.Tick(1);
            remote.Tick(1);
            AssertTrue(remote.SendCommand(CreateStructuralBuildCommand(
                "command:core:remote_incident_upper", "player:core:remote", 2, "building:core:light", 0, 0, 1)).Accepted,
                "Expected remote upper construction.");
            server.Tick(1);
            remote.Tick(1);

            CommandResult result = remote.SendCommand(CreateDemolishCommand(
                "command:core:remote_incident_demolish", "player:core:remote", 3, "building:core:000000000001"));

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(BuildingStructuralStatuses.Grace,
                server.CurrentState.Buildings.Instances["building:core:000000000002"].StructuralStatus,
                "Server must authoritatively enter grace.");
            AssertEqual(BuildingStructuralStatuses.Grace,
                remote.CurrentState.Buildings.Instances["building:core:000000000002"].StructuralStatus,
                "Remote snapshot must synchronize structural grace.");
        }

        private static void DemolitionRefundsPaidCost()
        {
            Simulation simulation = CreateRefundSimulation(100, 100);
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateRefundBuildCommand(
                "command:core:refund_build", "player:core:local", 1, false)).Accepted, "Expected refund test building.");
            session.Tick(1);

            CommandResult result = session.SendCommand(CreateDemolishCommand(
                "command:core:refund_demolish", "player:core:local", 2, "building:core:000000000001"));
            BuildingRemovedPayload payload = result.Events.Single(gameEvent =>
                gameEvent.EventType == BuildingSystem.BuildingDemolishedEvent).Payload.Deserialize<BuildingRemovedPayload>(SaveSystem.CreateDefaultJsonOptions());

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(97, simulation.State.Resources.Items["resource:core:wood"].Amount,
                "Ten paid resources must refund seven after floor rounding.");
            AssertEqual(7, payload.RequestedRefund["resource:core:wood"], "Refund event must report requested amount.");
            AssertEqual(7, payload.CreditedRefund["resource:core:wood"], "Refund event must report credited amount.");
        }

        private static void AcceleratedConstructionRefundsActualCost()
        {
            Simulation simulation = CreateRefundSimulation(100, 100);
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateRefundBuildCommand(
                "command:core:accelerated_refund_build", "player:core:local", 1, true)).Accepted,
                "Expected accelerated refund test building.");
            session.Tick(1);
            BuildingInstanceState instance = simulation.State.Buildings.Instances["building:core:000000000001"];
            AssertEqual(20, instance.PaidBuildCost["resource:core:wood"], "Paid cost snapshot must include acceleration multiplier.");
            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            AssertEqual(20, loaded.Buildings.Instances[instance.BuildingId].PaidBuildCost["resource:core:wood"],
                "Paid cost snapshot must survive save round trip.");

            AssertTrue(session.SendCommand(CreateDemolishCommand(
                "command:core:accelerated_refund_demolish", "player:core:local", 2, instance.BuildingId)).Accepted,
                "Expected accelerated building demolition.");

            AssertEqual(95, simulation.State.Resources.Items["resource:core:wood"].Amount,
                "Twenty paid resources must refund fifteen.");
        }

        private static void DemolitionReportsRefundOverflow()
        {
            Simulation simulation = CreateRefundSimulation(10, 12);
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateRefundBuildCommand(
                "command:core:overflow_build", "player:core:local", 1, false)).Accepted, "Expected overflow test building.");
            session.Tick(1);
            simulation.State.Resources.Items["resource:core:wood"].Amount = 10;

            CommandResult result = session.SendCommand(CreateDemolishCommand(
                "command:core:overflow_demolish", "player:core:local", 2, "building:core:000000000001"));
            BuildingRemovedPayload payload = result.Events.Single(gameEvent =>
                gameEvent.EventType == BuildingSystem.BuildingDemolishedEvent).Payload.Deserialize<BuildingRemovedPayload>(SaveSystem.CreateDefaultJsonOptions());

            AssertEqual(12, simulation.State.Resources.Items["resource:core:wood"].Amount, "Refund must stop at storage capacity.");
            AssertEqual(2, payload.CreditedRefund["resource:core:wood"], "Only available capacity may be credited.");
            AssertEqual(5, payload.OverflowRefund["resource:core:wood"], "Uncredited refund must be explicit.");
        }

        private static void StructuralCollapseNeverRefundsResources()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            simulation.State.Difficulty = new DifficultyState
            {
                DifficultyId = DifficultyIds.Custom,
                CustomStructuralGraceTicks = 0,
                CustomCollapseIntervalTicks = GameTime.TicksFromRealSecondsAtNormalSpeed(1),
                CustomStructuralFailureMode = StructuralFailureModes.AutomaticCollapse
            };
            simulation.State.Resources.Items["resource:core:wood"] = new ResourceStack
            {
                ResourceId = "resource:core:wood",
                Amount = 0,
                Capacity = 100
            };
            LocalGameSession session = new LocalGameSession(simulation);
            BuildAndComplete(session, 1, "collapse_refund_ground", "building:core:pillar", 0);
            BuildAndComplete(session, 2, "collapse_refund_upper", "building:core:light", 1);
            simulation.State.Buildings.Instances["building:core:000000000002"].PaidBuildCost["resource:core:wood"] = 10;

            CommandResult result = session.SendCommand(CreateDemolishCommand(
                "command:core:collapse_refund_remove_ground", "player:core:local", 3, "building:core:000000000001"));
            BuildingRemovedPayload collapse = result.Events.Single(gameEvent =>
                gameEvent.EventType == BuildingSystem.BuildingCollapsedEvent).Payload.Deserialize<BuildingRemovedPayload>(SaveSystem.CreateDefaultJsonOptions());

            AssertEqual(0, simulation.State.Resources.Items["resource:core:wood"].Amount, "Collapse must not refund paid cost.");
            AssertEqual(0, collapse.CreditedRefund.Count, "Collapse event must contain no credited refund.");
        }

        private static void OperationalRulesBlockUnusableBuildings()
        {
            BuildingInstanceState instance = new BuildingInstanceState
            {
                BuildingId = "building:core:operational",
                Durability = 100,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };
            AssertTrue(BuildingOperationalRules.IsOperational(instance), "Stable durable building must be operational.");

            instance.StructuralStatus = BuildingStructuralStatuses.Grace;
            AssertFalse(BuildingOperationalRules.CanAcceptWorkers(instance), "Grace building must reject workers.");
            AssertFalse(BuildingOperationalRules.CanProduce(instance), "Grace building must stop production.");
            AssertFalse(BuildingOperationalRules.CanTransferInventory(instance), "Grace building must stop inventory transfer.");

            instance.StructuralStatus = BuildingStructuralStatuses.Normal;
            instance.Durability = 0;
            AssertFalse(BuildingOperationalRules.IsOperational(instance), "Zero durability building must be inoperable.");
            instance.Durability = 100;
            instance.IsDestroyed = true;
            AssertFalse(BuildingOperationalRules.IsOperational(instance), "Destroyed building must be inoperable.");
        }

        private static void WorkerAssignmentUsesDeterministicSlots()
        {
            Simulation simulation = CreateWorkerSimulation();
            LocalGameSession session = new LocalGameSession(simulation);

            AssertTrue(session.SendCommand(CreateAssignWorkerCommand(
                "command:core:assign_first", "player:core:local", 1, "npc:core:000001", "building:core:work_a")).Accepted,
                "Expected first worker assignment.");
            AssertTrue(session.SendCommand(CreateAssignWorkerCommand(
                "command:core:assign_second", "player:core:local", 2, "npc:core:000002", "building:core:work_a")).Accepted,
                "Expected second worker assignment.");

            AssertEqual(0, simulation.State.Npcs.WorkAssignments["npc:core:000001"].SlotIndex,
                "First worker must occupy the smallest slot.");
            AssertEqual(1, simulation.State.Npcs.WorkAssignments["npc:core:000002"].SlotIndex,
                "Second worker must occupy the next slot.");
        }

        private static void WorkerReassignmentIsAtomic()
        {
            Simulation simulation = CreateWorkerSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateAssignWorkerCommand(
                "command:core:assign_before_move", "player:core:local", 1, "npc:core:000001", "building:core:work_a")).Accepted,
                "Expected initial assignment.");

            CommandResult result = session.SendCommand(CreateAssignWorkerCommand(
                "command:core:assign_after_move", "player:core:local", 2, "npc:core:000001", "building:core:work_b"));

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual("building:core:work_b", simulation.State.Npcs.WorkAssignments["npc:core:000001"].BuildingId,
                "Reassignment must replace the old assignment.");
            AssertEqual(2, result.Events.Count, "Atomic reassignment must emit release then assignment.");
            AssertEqual(WorkerAssignmentSystem.WorkerReleasedEvent, result.Events[0].EventType,
                "Old assignment must be released first.");
            AssertEqual(WorkerAssignmentSystem.WorkerAssignedEvent, result.Events[1].EventType,
                "New assignment must be established second.");
        }

        private static void WorkerAssignmentRejectsInvalidCandidates()
        {
            Simulation simulation = CreateWorkerSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            simulation.State.Npcs.Instances["npc:core:000002"].IsAdult = false;

            CommandResult unavailable = session.SendCommand(CreateAssignWorkerCommand(
                "command:core:assign_child", "player:core:local", 1, "npc:core:000002", "building:core:work_b"));
            AssertFalse(unavailable.Accepted, "Child NPC must not receive a work assignment.");
            AssertEqual(CommandErrorCodes.NpcUnavailable, unavailable.Code, "Expected stable unavailable NPC code.");

            AssertTrue(session.SendCommand(CreateAssignWorkerCommand(
                "command:core:fill_single_slot", "player:core:local", 2, "npc:core:000001", "building:core:work_b")).Accepted,
                "Expected single slot to be filled.");
            simulation.State.Npcs.Instances["npc:core:000002"].IsAdult = true;
            CommandResult full = session.SendCommand(CreateAssignWorkerCommand(
                "command:core:assign_full_slot", "player:core:local", 3, "npc:core:000002", "building:core:work_b"));
            AssertFalse(full.Accepted, "Full building must reject another worker.");
            AssertEqual(CommandErrorCodes.WorkerSlotsFull, full.Code, "Expected stable full-slot code.");
        }

        private static void BuildingDemolitionReleasesWorkers()
        {
            Simulation simulation = CreateWorkerSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateAssignWorkerCommand(
                "command:core:assign_before_demolition", "player:core:local", 1, "npc:core:000001", "building:core:work_a")).Accepted,
                "Expected assignment before demolition.");

            CommandResult result = session.SendCommand(CreateDemolishCommand(
                "command:core:demolish_workplace", "player:core:local", 2, "building:core:work_a"));

            AssertTrue(result.Accepted, result.Reason);
            AssertFalse(simulation.State.Npcs.WorkAssignments.ContainsKey("npc:core:000001"),
                "Demolition must release the assigned worker immediately.");
            WorkerReleasedPayload released = result.Events.Single(gameEvent =>
                gameEvent.EventType == WorkerAssignmentSystem.WorkerReleasedEvent).Payload.Deserialize<WorkerReleasedPayload>(SaveSystem.CreateDefaultJsonOptions());
            AssertEqual(WorkerReleaseReasons.BuildingInoperable, released.Reason,
                "Release event must identify the inoperable building.");
        }

        private static void WorkerAssignmentSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateWorkerSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateAssignWorkerCommand(
                "command:core:assign_before_save", "player:core:local", 1, "npc:core:000001", "building:core:work_a")).Accepted,
                "Expected assignment before save.");

            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));

            AssertEqual("2.9", loaded.SaveVersion, "Worker assignment save must use version 2.9.");
            AssertEqual("building:core:work_a", loaded.Npcs.WorkAssignments["npc:core:000001"].BuildingId,
                "Worker assignment must survive save round trip.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State), StateDiagnostics.CalculateStateHash(loaded),
                "NPC assignment state hash must survive save round trip.");
        }

        private static void RemoteWorkerAssignmentUsesServerAuthority()
        {
            Simulation simulation = CreateWorkerSimulation();
            simulation.State.Npcs.Instances["npc:core:000001"].OwnerPlayerId = "player:core:remote";
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);

            CommandResult result = remote.SendCommand(CreateAssignWorkerCommand(
                "command:core:remote_assign_worker",
                "player:core:remote",
                1,
                "npc:core:000001",
                "building:core:work_a"));

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual("building:core:work_a", server.CurrentState.Npcs.WorkAssignments["npc:core:000001"].BuildingId,
                "Server must own the accepted assignment.");
            AssertEqual("building:core:work_a", remote.CurrentState.Npcs.WorkAssignments["npc:core:000001"].BuildingId,
                "Remote snapshot must synchronize the assignment.");
        }

        private static void HousingAssignsAdultsDeterministically()
        {
            Simulation simulation = CreateHousingSimulation();
            simulation.Tick(1);

            AssertEqual("building:core:home_a", simulation.State.Housing.AssignmentsByNpcId["npc:core:resident_1"].BuildingId,
                "First resident must receive the first constructed home.");
            AssertEqual(0, simulation.State.Housing.AssignmentsByNpcId["npc:core:resident_1"].BedSlotIndex,
                "First resident must receive the smallest bed slot.");
            AssertEqual(1, simulation.State.Housing.AssignmentsByNpcId["npc:core:resident_2"].BedSlotIndex,
                "Second resident must receive the next bed slot.");
            AssertEqual("building:core:home_b", simulation.State.Housing.AssignmentsByNpcId["npc:core:resident_3"].BuildingId,
                "Third resident must continue into the next constructed home.");
        }

        private static void HousingExcludesInfantsAndTracksHomeless()
        {
            Simulation simulation = CreateHousingSimulation();
            simulation.State.Buildings.Instances["building:core:home_b"].IsDestroyed = true;
            simulation.State.Npcs.Instances["npc:core:resident_3"].IsAdult = false;
            simulation.State.Npcs.Instances["npc:core:resident_4"] = new NpcInstanceState
            {
                NpcId = "npc:core:resident_4", OwnerPlayerId = "player:core:local", CreationSequence = 4
            };
            simulation.Tick(1);

            AssertFalse(simulation.State.Housing.AssignmentsByNpcId.ContainsKey("npc:core:resident_3"), "Infant must not occupy a bed.");
            AssertFalse(simulation.State.Housing.HomelessAdultNpcIds.Contains("npc:core:resident_3"), "Infant must not be marked homeless.");
            AssertTrue(simulation.State.Housing.HomelessAdultNpcIds.Contains("npc:core:resident_4"), "Adult without a bed must be tracked as homeless.");
        }

        private static void ManualHousingReassignmentIsPreserved()
        {
            Simulation simulation = CreateHousingSimulation();
            simulation.Tick(1);
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult result = session.SendCommand(CreateAssignHousingCommand(
                "command:core:move_resident", 1, "npc:core:resident_1", "building:core:home_b", 1));

            AssertTrue(result.Accepted, result.Reason);
            HousingAssignmentState assignment = simulation.State.Housing.AssignmentsByNpcId["npc:core:resident_1"];
            AssertEqual("building:core:home_b", assignment.BuildingId, "Manual move must replace automatic housing.");
            AssertEqual(1, assignment.BedSlotIndex, "Manual move must preserve the selected bed.");
            AssertTrue(assignment.IsManual, "Manual housing must remain distinguishable from automatic housing.");
            simulation.Tick(1);
            AssertEqual(1, simulation.State.Housing.AssignmentsByNpcId["npc:core:resident_1"].BedSlotIndex,
                "Automatic reconciliation must not overwrite a valid manual choice.");
        }

        private static void InoperableHousingRelocatesResidents()
        {
            Simulation simulation = CreateHousingSimulation();
            simulation.Tick(1);
            simulation.State.Buildings.Instances["building:core:home_a"].Durability = 0;
            simulation.Tick(1);

            AssertEqual("building:core:home_b", simulation.State.Housing.AssignmentsByNpcId["npc:core:resident_1"].BuildingId,
                "Resident must move to the remaining operational home.");
            AssertTrue(simulation.State.Housing.HomelessAdultNpcIds.Contains("npc:core:resident_2"),
                "Resident without replacement capacity must become homeless.");
        }

        private static void HousingSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateHousingSimulation();
            simulation.Tick(1);
            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));

            AssertEqual("2.9", loaded.SaveVersion, "Housing requires save version 2.9.");
            AssertEqual(3, loaded.Housing.AssignmentsByNpcId.Count, "Housing assignments must survive save load.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State), StateDiagnostics.CalculateStateHash(loaded),
                "Housing state hash must survive save round trip.");
        }

        private static void NpcInfantGrowsAtExactBoundary()
        {
            Simulation simulation = CreateLifecycleSimulation(false);
            NpcInstanceState npc = simulation.State.Npcs.Instances["npc:core:lifecycle"];
            simulation.Tick(NpcLifecycleSystem.InfantGrowthTicks - 1);
            AssertFalse(npc.IsAdult, "Infant must remain an infant before the exact boundary.");
            IReadOnlyList<GameEvent> events = simulation.Tick(1);
            AssertTrue(npc.IsAdult, "Infant must become adult at exactly 2.5 game days.");
            AssertEqual(NpcLifecycleSystem.InfantGrowthTicks, npc.AdultTransitionTick, "Growth tick must be exact.");
            AssertEqual(NpcLifecycleSystem.NpcBecameAdultEvent, events[0].EventType, "Growth must emit a stable event.");
        }

        private static void NpcLifespanIsDeterministicAndBounded()
        {
            long first = NpcLifecycleSystem.ResolveAdultLifespanTicks("seed-a", "npc:core:lifecycle");
            long second = NpcLifecycleSystem.ResolveAdultLifespanTicks("seed-a", "npc:core:lifecycle");
            AssertEqual(first, second, "The same world and NPC must always resolve the same lifespan.");
            AssertTrue(first >= NpcLifecycleSystem.MinimumAdultLifespanDays * GameTime.TicksPerGameDay,
                "Resolved lifespan must respect the minimum.");
            AssertTrue(first <= NpcLifecycleSystem.MaximumAdultLifespanDays * GameTime.TicksPerGameDay,
                "Resolved lifespan must respect the maximum.");
        }

        private static void NpcAdultDiesAtNaturalLifespan()
        {
            Simulation simulation = CreateLifecycleSimulation(true);
            NpcInstanceState npc = simulation.State.Npcs.Instances["npc:core:lifecycle"];
            long lifespan = npc.AdultLifespanTicks;
            simulation.Tick(lifespan - 1);
            AssertTrue(npc.IsAlive, "Adult must remain alive before the lifespan boundary.");
            IReadOnlyList<GameEvent> events = simulation.Tick(1);
            AssertFalse(npc.IsAlive, "Adult must die at the exact natural lifespan.");
            AssertEqual(lifespan, npc.DeathTick, "Natural death tick must be exact.");
            AssertEqual(NpcLifecycleSystem.NpcDiedEvent, events[0].EventType, "Natural death must emit a stable event.");
        }

        private static void NpcLifecycleProcessesGrowthAndDeathInLargeTick()
        {
            Simulation simulation = CreateLifecycleSimulation(false);
            NpcInstanceState npc = simulation.State.Npcs.Instances["npc:core:lifecycle"];
            long total = NpcLifecycleSystem.InfantGrowthTicks + npc.AdultLifespanTicks;
            IReadOnlyList<GameEvent> events = simulation.Tick(total);
            AssertTrue(npc.IsAdult, "Large tick must process infant growth.");
            AssertFalse(npc.IsAlive, "Large tick must continue through adult natural death.");
            AssertEqual(3, events.Count, "Growth, death, and death waste must each emit one event.");
            AssertEqual(NpcLifecycleSystem.InfantGrowthTicks, npc.AdultTransitionTick, "Large tick growth time must remain exact.");
            AssertEqual(total, npc.DeathTick, "Large tick death time must remain exact.");
        }

        private static void NpcDeathReleasesHousingAndWork()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:lifecycle_home", CarryCapacity = 10, BedSlotCount = 1, WorkerSlotCount = 1
            });
            GameState state = new GameState();
            BuildingInstanceState building = new BuildingInstanceState
            {
                BuildingId = "building:core:lifecycle_building", DefinitionId = "building:core:lifecycle_home",
                Durability = 100, StructuralStatus = BuildingStructuralStatuses.Normal
            };
            state.Buildings.Instances[building.BuildingId] = building;
            NpcInstanceState npc = new NpcInstanceState
            {
                NpcId = "npc:core:lifecycle", OwnerPlayerId = "player:core:local", CreationSequence = 1,
                IsAdult = true, AdultLifespanTicks = 20 * GameTime.TicksPerGameDay
            };
            state.Npcs.Instances[npc.NpcId] = npc;
            state.Npcs.WorkAssignments[npc.NpcId] = new WorkAssignmentState
            {
                NpcId = npc.NpcId, BuildingId = building.BuildingId, SlotIndex = 0
            };
            state.Housing.AssignmentsByNpcId[npc.NpcId] = new HousingAssignmentState
            {
                NpcId = npc.NpcId, BuildingId = building.BuildingId, BedSlotIndex = 0
            };
            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new NpcLifecycleSystem());
            simulation.Tick(npc.AdultLifespanTicks);
            AssertFalse(state.Npcs.WorkAssignments.ContainsKey(npc.NpcId), "Death must release work immediately.");
            AssertFalse(state.Housing.AssignmentsByNpcId.ContainsKey(npc.NpcId), "Death must release housing immediately.");
        }

        private static void NpcLifecycleKeepsSurvivalTickSplittingInvariant()
        {
            Simulation large = CreateLifecycleSimulation(false, true);
            Simulation split = CreateLifecycleSimulation(false, true);
            long total = NpcLifecycleSystem.InfantGrowthTicks + 20 * GameTime.TicksPerGameDay;
            large.Tick(total);
            for (int day = 0; day < 22; day++) split.Tick(GameTime.TicksPerGameDay);
            split.Tick(GameTime.TicksPerGameDay / 2);
            AssertEqual(large.State.Resources.Items[CoreResourceIds.Food].Amount,
                split.State.Resources.Items[CoreResourceIds.Food].Amount,
                "Food consumption must not depend on tick splitting across growth and death.");
            AssertEqual(large.State.Npcs.Instances["npc:core:lifecycle"].DeathTick,
                split.State.Npcs.Instances["npc:core:lifecycle"].DeathTick,
                "Death timing must not depend on tick splitting.");
            AssertEqual(large.State.Survival.FoodRemainderQuarterUnits, split.State.Survival.FoodRemainderQuarterUnits,
                "Fractional survival state must not depend on tick splitting.");
            AssertEqual(large.State.Survival.LastSettlementTick, split.State.Survival.LastSettlementTick,
                "Settlement progress must not depend on tick splitting.");
        }

        private static void NpcLifecycleSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateLifecycleSimulation(true);
            simulation.Tick(GameTime.TicksPerGameDay * 3);
            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            AssertEqual("2.9", loaded.SaveVersion, "NPC lifecycle requires save version 2.9.");
            AssertEqual(simulation.State.Npcs.Instances["npc:core:lifecycle"].LifeStageElapsedTicks,
                loaded.Npcs.Instances["npc:core:lifecycle"].LifeStageElapsedTicks,
                "Lifecycle age must survive save load.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State), StateDiagnostics.CalculateStateHash(loaded),
                "Lifecycle state hash must survive save round trip.");
        }

        private static void RemoteNpcLifecycleUsesServerAuthority()
        {
            Simulation simulation = CreateLifecycleSimulation(false);
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            server.Tick(NpcLifecycleSystem.InfantGrowthTicks);
            remote.Tick(1);
            AssertTrue(server.CurrentState.Npcs.Instances["npc:core:lifecycle"].IsAdult, "Server must own lifecycle growth.");
            AssertTrue(remote.CurrentState.Npcs.Instances["npc:core:lifecycle"].IsAdult, "Remote must synchronize lifecycle growth.");
        }

        private static void NpcSurvivalConsumesAdultNeeds()
        {
            Simulation simulation = CreateSurvivalSimulation(2, 0, 10, 10);
            IReadOnlyList<GameEvent> events = simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(9, simulation.State.Resources.Items[CoreResourceIds.Food].Amount, "Two adults must consume one food per day.");
            AssertEqual(9, simulation.State.Resources.Items[CoreResourceIds.Water].Amount, "Two adults must consume one water per day.");
            AssertEqual(1, events.Count, "One elapsed game day must emit one survival settlement.");
            AssertEqual(0, simulation.State.Survival.LastFoodShortage, "Supplied adults must have no food shortage.");
        }

        private static void NpcSurvivalCarriesFractionalNeeds()
        {
            Simulation simulation = CreateSurvivalSimulation(1, 0, 10, 10);
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(10, simulation.State.Resources.Items[CoreResourceIds.Food].Amount, "Half-unit food demand must remain fractional after one day.");
            AssertEqual(2, simulation.State.Survival.FoodRemainderQuarterUnits, "Adult food remainder must retain two quarter units.");
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(9, simulation.State.Resources.Items[CoreResourceIds.Food].Amount, "Two adult-days must consume one whole food.");
            AssertEqual(0, simulation.State.Survival.FoodRemainderQuarterUnits, "Whole consumption must clear the carried remainder.");
        }

        private static void NpcSurvivalIncludesInfantNeeds()
        {
            Simulation simulation = CreateSurvivalSimulation(1, 2, 10, 10);
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(9, simulation.State.Resources.Items[CoreResourceIds.Food].Amount, "One adult plus two infants must consume one food.");
            AssertEqual(9, simulation.State.Resources.Items[CoreResourceIds.Water].Amount, "Population water demand must consume one unit.");
            AssertEqual(2, simulation.State.Survival.WaterRemainderQuarterUnits, "Half-unit water demand must carry forward.");
        }

        private static void NpcSurvivalRespectsLocksAndShortages()
        {
            Simulation simulation = CreateSurvivalSimulation(2, 0, 1, 10);
            simulation.State.Resources.Items[CoreResourceIds.Food].LockedAmount = 1;
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Food].Amount, "Survival must not consume locked food.");
            AssertEqual(1, simulation.State.Survival.LastFoodShortage, "Unavailable food must create an exact shortage.");
            AssertEqual(1, simulation.State.Survival.ConsecutiveFoodShortageDays, "First shortage must start the streak.");
            simulation.State.Resources.Items[CoreResourceIds.Food].LockedAmount = 0;
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(0, simulation.State.Survival.LastFoodShortage, "Restored supply must clear current shortage.");
            AssertEqual(0, simulation.State.Survival.ConsecutiveFoodShortageDays, "Restored supply must reset the streak.");
        }

        private static void NpcSurvivalProcessesLargeTicks()
        {
            Simulation simulation = CreateSurvivalSimulation(2, 0, 10, 10);
            IReadOnlyList<GameEvent> events = simulation.Tick(GameTime.TicksPerGameDay * 3);
            AssertEqual(7, simulation.State.Resources.Items[CoreResourceIds.Food].Amount, "Three elapsed days must settle three food cycles.");
            AssertEqual(7, simulation.State.Resources.Items[CoreResourceIds.Water].Amount, "Three elapsed days must settle three water cycles.");
            AssertEqual(3, events.Count, "Large tick must preserve one event per crossed day boundary.");
        }

        private static void NpcSurvivalSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateSurvivalSimulation(1, 1, 10, 10);
            simulation.Tick(GameTime.TicksPerGameDay);
            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            AssertEqual("2.9", loaded.SaveVersion, "NPC survival requires save version 2.9.");
            AssertEqual(simulation.State.Survival.FoodRemainderQuarterUnits, loaded.Survival.FoodRemainderQuarterUnits,
                "Fractional food demand must survive save load.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State), StateDiagnostics.CalculateStateHash(loaded),
                "NPC survival state hash must survive save round trip.");
        }

        private static void RemoteNpcSurvivalUsesServerAuthority()
        {
            Simulation simulation = CreateSurvivalSimulation(2, 0, 10, 10);
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            server.Tick(GameTime.TicksPerGameDay);
            remote.Tick(1);
            AssertEqual(9, server.CurrentState.Resources.Items[CoreResourceIds.Food].Amount, "Server must own survival consumption.");
            AssertEqual(9, remote.CurrentState.Resources.Items[CoreResourceIds.Food].Amount, "Remote must synchronize survival state.");
            AssertEqual(GameTime.TicksPerGameDay, server.CurrentState.SimulationTick, "Remote synchronization must not advance server time.");
        }

        private static void NpcSurvivalMigrationStartsAfterCurrentDay()
        {
            GameState legacy = new GameState
            {
                SaveVersion = "2.1",
                SimulationTick = GameTime.TicksPerGameDay * 5 + 10,
                Survival = null
            };
            SaveMigration.RepairAfterLoad(legacy);
            AssertEqual(GameTime.TicksPerGameDay * 6, legacy.Survival.NextSettlementTick,
                "Legacy survival must begin at the next day boundary without retroactive consumption.");
            AssertEqual(0L, legacy.Survival.LastSettlementTick, "Migration must not invent a past settlement.");
        }

        private static void ProductionLocksInputsAndBlocksReuse()
        {
            Simulation simulation = CreateProductionSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureAndStartProduction(session, false);

            ResourceStack ore = simulation.State.Resources.Items["resource:core:ore"];
            AssertEqual(10, ore.Amount, "Starting a batch must not consume inputs early.");
            AssertEqual(2, ore.LockedAmount, "Starting a batch must lock its inputs.");

            CommandResult build = session.SendCommand(CreateProductionCostBuildCommand(3));
            AssertFalse(build.Accepted, "Construction must not reuse production-locked resources.");
            AssertEqual(CommandErrorCodes.ValidationFailed, build.Code, "Expected the current resource-validation code.");
            AssertEqual(10, ore.Amount, "Rejected construction must not mutate resources.");
            AssertEqual(2, ore.LockedAmount, "Rejected construction must preserve the production lock.");
        }

        private static void ProductionPausesAndResumesWithWorkers()
        {
            Simulation simulation = CreateProductionSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureAndStartProduction(session, false);
            session.Tick(5);
            ProductionSlotState slot = GetProducerSlot(simulation.State);
            AssertEqual(5L, slot.ProgressWorkTicks, "One worker must contribute one work tick per simulation tick.");

            simulation.State.Npcs.WorkAssignments.Remove("npc:core:producer_worker");
            session.Tick(5);
            AssertEqual(ProductionSlotStatuses.Paused, slot.Status, "Missing workers must pause the active batch.");
            AssertEqual(5L, slot.ProgressWorkTicks, "Paused production must retain progress.");
            AssertEqual(2, simulation.State.Resources.Items["resource:core:ore"].LockedAmount,
                "Paused production must retain input locks.");

            AssertTrue(session.SendCommand(CreateAssignWorkerCommand(
                "command:core:resume_producer", "player:core:local", 3,
                "npc:core:producer_worker", "building:core:producer")).Accepted,
                "Expected worker reassignment to resume production.");
            session.Tick(5);
            AssertFalse(slot.HasActiveBatch, "Completed batch must clear its active batch.");
            AssertEqual(8, simulation.State.Resources.Items["resource:core:ore"].Amount,
                "Completion must consume exactly one input batch.");
            AssertEqual(3, simulation.State.Buildings.Instances["building:core:producer"]
                .LocalInventory["resource:core:ingot"].Amount, "Output must transfer to local storage first.");
        }

        private static void ProductionCancelUnlocksWithoutConsuming()
        {
            Simulation simulation = CreateProductionSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureAndStartProduction(session, true);

            CommandResult cancelled = session.SendCommand(CreateProductionCommand(
                "command:core:cancel_test", 3, ProductionSystem.CancelProductionCommand));
            AssertTrue(cancelled.Accepted, cancelled.Reason);
            ProductionSlotState slot = GetProducerSlot(simulation.State);
            AssertFalse(slot.HasActiveBatch, "Cancel must clear the active batch.");
            AssertFalse(slot.Continuous, "Cancel must disable continuous production.");
            AssertEqual(10, simulation.State.Resources.Items["resource:core:ore"].Amount,
                "Cancel must not consume locked input.");
            AssertEqual(0, simulation.State.Resources.Items["resource:core:ore"].LockedAmount,
                "Cancel must release global locks.");
        }

        private static void ProductionOutputWaitsForWholeBatchStorage()
        {
            Simulation simulation = CreateProductionSimulation(localCapacity: 0, ingotCapacity: 2);
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureAndStartProduction(session, false);
            session.Tick(10);

            ProductionSlotState slot = GetProducerSlot(simulation.State);
            AssertEqual(ProductionSlotStatuses.OutputPending, slot.Status, "Insufficient storage must retain the whole output batch.");
            AssertEqual(3, slot.OutputBuffer["resource:core:ingot"], "Buffered output must remain intact.");
            AssertEqual(0, simulation.State.Resources.Items["resource:core:ingot"].Amount,
                "A partial output transfer is forbidden.");
            CommandResult demolition = session.SendCommand(CreateDemolishCommand(
                "command:core:demolish_output_pending", "player:core:local", 3, "building:core:producer"));
            AssertFalse(demolition.Accepted, "Pending production output must block normal demolition.");
            AssertEqual(CommandErrorCodes.ProductionOutputPending, demolition.Code, "Expected stable pending-output code.");

            simulation.State.Resources.Items["resource:core:ingot"].Capacity = 3;
            session.Tick(1);
            AssertEqual(0, slot.OutputBuffer.Count, "Available storage must release the full buffered batch.");
            AssertEqual(3, simulation.State.Resources.Items["resource:core:ingot"].Amount,
                "The full output batch must transfer atomically.");
        }

        private static void ContinuousProductionStartsOneNextBatch()
        {
            Simulation simulation = CreateProductionSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureAndStartProduction(session, true);
            session.Tick(10);

            ProductionSlotState slot = GetProducerSlot(simulation.State);
            AssertTrue(slot.HasActiveBatch, "Continuous production must start the next batch after transfer.");
            AssertEqual(0L, slot.ProgressWorkTicks, "A newly started batch must not receive the completed tick twice.");
            AssertEqual(8, simulation.State.Resources.Items["resource:core:ore"].Amount,
                "Only the completed batch may consume input.");
            AssertEqual(2, simulation.State.Resources.Items["resource:core:ore"].LockedAmount,
                "The next batch must reserve, but not consume, its inputs.");
        }

        private static void ProductionCombinesGlobalAndLocalInputs()
        {
            Simulation simulation = CreateProductionSimulation(oreAmount: 1);
            BuildingInstanceState building = simulation.State.Buildings.Instances["building:core:producer"];
            building.LocalInventory["resource:core:ore"] = new LocalResourceStack
            {
                ResourceId = "resource:core:ore",
                Amount = 1
            };
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureAndStartProduction(session, false);

            ProductionSlotState slot = GetProducerSlot(simulation.State);
            AssertEqual(1, slot.LockedGlobalInputs["resource:core:ore"], "Global inventory must have first input priority.");
            AssertEqual(1, slot.LockedLocalInputs["resource:core:ore"], "Local inventory must supplement the remainder.");
        }

        private static void ProductionSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateProductionSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureAndStartProduction(session, false);
            session.Tick(4);

            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            ProductionSlotState slot = GetProducerSlot(loaded);
            AssertEqual("2.9", loaded.SaveVersion, "Production save must use version 2.9.");
            AssertEqual(4L, slot.ProgressWorkTicks, "Active production progress must survive save load.");
            AssertEqual(2, loaded.Resources.Items["resource:core:ore"].LockedAmount,
                "Active production locks must survive save load.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State), StateDiagnostics.CalculateStateHash(loaded),
                "Production state hash must survive save round trip.");
        }

        private static void FormalSmelterConsumesOreAndFuelAtomically()
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            GameState state = new GameState();
            state.World.Plots["plot:test:smelter"] = new PlotState
            {
                PlotId = "plot:test:smelter", Width = 1, Depth = 1, MaxStackLayers = 1
            };
            BuildingInstanceState smelter = CreateBuildingStateWithDefinition(
                "building:test:smelter", CoreBuildingIds.Smelter, "plot:test:smelter", 0, 0, 0, 1, 1, 1);
            smelter.Durability = 600;
            smelter.LocalInventoryCapacity = 20;
            state.Buildings.Instances[smelter.BuildingId] = smelter;
            state.Npcs.Instances["npc:test:smelter"] = new NpcInstanceState
            {
                NpcId = "npc:test:smelter", OwnerPlayerId = "player:test:smelter", CreationSequence = 1
            };
            state.Npcs.WorkAssignments["npc:test:smelter"] = new WorkAssignmentState
            {
                NpcId = "npc:test:smelter", BuildingId = smelter.BuildingId, SlotIndex = 0
            };
            state.Resources.Items[CoreResourceIds.IronOre] = new ResourceStack
            {
                ResourceId = CoreResourceIds.IronOre, Amount = 1, Capacity = 100
            };
            state.Resources.Items[CoreResourceIds.Fuel] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Fuel, Amount = 1, Capacity = 100
            };
            state.Resources.Items[CoreResourceIds.IronIngot] = new ResourceStack
            {
                ResourceId = CoreResourceIds.IronIngot, Amount = 0, Capacity = 100
            };

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new ProductionSystem());
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult configured = session.SendCommand(new CommandEnvelope
            {
                CommandId = "command:test:configure_smelter",
                PlayerId = "player:test:smelter",
                Type = ProductionSystem.ConfigureProductionCommand,
                Payload = JsonSerializer.SerializeToElement(new ConfigureProductionPayload
                {
                    BuildingId = smelter.BuildingId,
                    RecipeId = CoreRecipeIds.SmeltIron,
                    Continuous = false
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = 1
            });
            AssertTrue(configured.Accepted, configured.Reason);
            CommandResult started = session.SendCommand(new CommandEnvelope
            {
                CommandId = "command:test:start_smelter",
                PlayerId = "player:test:smelter",
                Type = ProductionSystem.StartProductionCommand,
                Payload = JsonSerializer.SerializeToElement(new ProductionBuildingPayload
                {
                    BuildingId = smelter.BuildingId
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = 2
            });
            AssertTrue(started.Accepted, started.Reason);
            AssertEqual(1, state.Resources.Items[CoreResourceIds.IronOre].LockedAmount,
                "Starting smelting must lock its ore.");
            AssertEqual(1, state.Resources.Items[CoreResourceIds.Fuel].LockedAmount,
                "Starting smelting must lock its fuel in the same batch.");

            session.Tick(GameTime.TicksPerGameDay);
            AssertEqual(0, state.Resources.Items[CoreResourceIds.IronOre].Amount,
                "Completed smelting must consume its ore.");
            AssertEqual(0, state.Resources.Items[CoreResourceIds.Fuel].Amount,
                "Completed smelting must consume its fuel.");
            AssertEqual(1, smelter.LocalInventory[CoreResourceIds.IronIngot].Amount,
                "Completed smelting must store one iron ingot.");
        }

        private static void WasteModeADestroysTenWaste()
        {
            Simulation simulation = CreateWasteProcessingSimulation(101);
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureWasteAndStart(session, CoreRecipeIds.DestroyWaste);
            session.Tick(GameTime.TicksPerGameDay);
            AssertEqual(90, simulation.State.Resources.Items[CoreResourceIds.Waste].Amount,
                "Mode A must consume ten waste per worker-day.");
            AssertEqual(0, simulation.State.Buildings.Instances["building:test:waste_processor"]
                .LocalInventory.Values.Sum(stack => stack.Amount),
                "Mode A must not create placeholder output.");
        }

        private static void WastePenaltyResolvesNpcSatisfaction()
        {
            GameState state = new GameState();
            NpcInstanceState npc = new NpcInstanceState
            {
                NpcId = "npc:test:satisfaction",
                BaseSatisfactionBasisPoints = 9000
            };
            state.Waste.AccumulatedSatisfactionPenaltyBasisPoints = 300;
            AssertEqual(8700, WasteEffectRules.ResolveNpcSatisfactionBasisPoints(state, npc),
                "Pollution must reduce effective NPC satisfaction by the authoritative penalty.");
            AssertEqual(9700, WasteEffectRules.ResolveWorkEfficiencyBasisPoints(state),
                "The same pollution penalty must reduce all work efficiency.");
            state.Waste.AccumulatedSatisfactionPenaltyBasisPoints = 0;
            AssertEqual(9000, WasteEffectRules.ResolveNpcSatisfactionBasisPoints(state, npc),
                "Clearing pollution must reveal the unchanged base satisfaction immediately.");
        }

        private static void FertilizerBoostsOneFarmCycle()
        {
            Simulation simulation = CreateFertilizerSimulation(1);
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult applied = session.SendCommand(CreateApplyFertilizerCommand(
                "command:test:apply_fertilizer", "player:test:fertilizer", 1));
            AssertTrue(applied.Accepted, applied.Reason);
            AssertEqual(0, simulation.State.Resources.Items[CoreResourceIds.Fertilizer].Amount,
                "Applying fertilizer must consume exactly one unlocked unit.");
            session.Tick(GameTime.TicksPerGameDay);
            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            AssertEqual(6, farm.LocalInventory[CoreResourceIds.Food].Amount,
                "One worker-day of four food must receive exactly two bonus food.");
            ContinuousProductionBuildingState runtime = simulation.State.ContinuousProduction.Buildings[farm.BuildingId];
            AssertEqual(0, runtime.FertilizerBaseOutputRemaining, "One complete cycle must consume the fertilizer effect.");
        }

        private static void FertilizerIsInvariantToTickSplitting()
        {
            Simulation large = CreateFertilizerSimulation(1);
            Simulation split = CreateFertilizerSimulation(1);
            AssertTrue(new LocalGameSession(large).SendCommand(CreateApplyFertilizerCommand(
                "command:test:fertilizer_large", "player:test:fertilizer", 1)).Accepted,
                "Expected fertilizer application.");
            AssertTrue(new LocalGameSession(split).SendCommand(CreateApplyFertilizerCommand(
                "command:test:fertilizer_split", "player:test:fertilizer", 1)).Accepted,
                "Expected fertilizer application.");
            large.Tick(GameTime.TicksPerGameDay);
            for (int index = 0; index < 4; index++) split.Tick(GameTime.TicksPerGameDay / 4);
            LocalResourceStack largeFood = large.State.Buildings.Instances["building:test:continuous"]
                .LocalInventory[CoreResourceIds.Food];
            LocalResourceStack splitFood = split.State.Buildings.Instances["building:test:continuous"]
                .LocalInventory[CoreResourceIds.Food];
            AssertEqual(largeFood.Amount, splitFood.Amount,
                "Tick splitting must not change fertilizer bonus output.");
            AssertEqual(large.State.ContinuousProduction.Buildings["building:test:continuous"].FertilizerBaseOutputRemaining,
                split.State.ContinuousProduction.Buildings["building:test:continuous"].FertilizerBaseOutputRemaining,
                "Tick splitting must not change fertilizer coverage state.");
        }

        private static void FertilizerRejectsInvalidApplication()
        {
            Simulation simulation = CreateFertilizerSimulation(1);
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateApplyFertilizerCommand(
                "command:test:fertilizer_first", "player:test:fertilizer", 1)).Accepted,
                "Expected first fertilizer application.");
            simulation.State.Resources.Items[CoreResourceIds.Fertilizer].Amount = 1;
            CommandResult duplicate = session.SendCommand(CreateApplyFertilizerCommand(
                "command:test:fertilizer_duplicate", "player:test:fertilizer", 2));
            AssertFalse(duplicate.Accepted, "An active farm cycle must reject fertilizer stacking.");
            AssertEqual(CommandErrorCodes.ProductionBatchActive, duplicate.Code,
                "Duplicate fertilizer must use a stable rejection code.");
        }

        private static void FertilizerStateMigratesAndSynchronizesRemotely()
        {
            GameState legacy = new GameState { SaveVersion = "2.7" };
            GameState migrated = new SaveSystem().Deserialize(new SaveSystem().Serialize(legacy));
            AssertEqual("2.9", migrated.SaveVersion, "Version 2.7 must migrate to 2.9.");

            ServerGameSession server = new ServerGameSession(
                CreateFertilizerSimulation(1), new[] { "player:test:fertilizer" });
            RemoteGameSession remote = CreateRemoteSession(server);
            AssertTrue(remote.SendCommand(CreateApplyFertilizerCommand(
                "command:test:remote_fertilizer", "player:test:fertilizer", 1)).Accepted,
                "Server must accept remote fertilizer application.");
            server.Tick(GameTime.TicksPerGameDay);
            remote.Tick(1);
            AssertEqual(6, remote.CurrentState.Buildings.Instances["building:test:continuous"]
                .LocalInventory[CoreResourceIds.Food].Amount,
                "Remote snapshot must receive fertilizer bonus output.");
            AssertEqual(StateDiagnostics.CalculateStateHash(server.CurrentState),
                StateDiagnostics.CalculateStateHash(remote.CurrentState),
                "Fertilizer state must remain server authoritative.");
        }

        private static void WastePenaltySlowsBatchProductionExactly()
        {
            Simulation simulation = CreateProductionSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateConfigureProductionCommand(
                "command:test:waste_efficiency_configure", "player:core:local", 1, false)).Accepted,
                "Expected batch configuration.");
            AssertTrue(session.SendCommand(CreateProductionCommand(
                "command:test:waste_efficiency_start", 2, ProductionSystem.StartProductionCommand)).Accepted,
                "Expected batch start.");
            simulation.State.Waste.AccumulatedSatisfactionPenaltyBasisPoints = 2000;
            session.Tick(10);
            AssertEqual(8L, GetProducerSlot(simulation.State).ProgressWorkTicks,
                "A twenty-percent pollution penalty must turn ten work ticks into eight.");
            session.Tick(3);
            AssertFalse(GetProducerSlot(simulation.State).HasActiveBatch,
                "Fractional fixed-point work must complete the batch without permanent loss.");
        }

        private static void WastePenaltySlowsContinuousProductionExactly()
        {
            Simulation simulation = CreateContinuousProductionSimulation(
                CoreBuildingIds.Farm, 1, 20, 100, 0, 100);
            simulation.State.Survival.NextSettlementTick = long.MaxValue;
            simulation.State.Waste.NextSettlementTick = long.MaxValue;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false);
            simulation.State.Waste.AccumulatedSatisfactionPenaltyBasisPoints = 2000;
            simulation.Tick(GameTime.TicksPerGameDay);
            BuildingInstanceState farm = simulation.State.Buildings.Instances["building:test:continuous"];
            AssertEqual(3, farm.LocalInventory[CoreResourceIds.Food].Amount,
                "A farm at eighty-percent efficiency must store three whole food after one day.");
            ContinuousProductionBuildingState runtime = simulation.State.ContinuousProduction.Buildings.Values.Single();
            AssertEqual(GameTime.TicksPerGameDay / 5, runtime.ProgressUnits,
                "The remaining 0.2 food must stay as exact continuous progress.");
        }

        private static void WasteDiseaseExposureIsDeterministic()
        {
            Simulation first = CreateWasteGenerationSimulation(50, false);
            Simulation second = CreateWasteGenerationSimulation(50, false);
            first.State.RngSeed = second.State.RngSeed = 909;
            first.State.Resources.Items[CoreResourceIds.Waste].Amount = 401;
            second.State.Resources.Items[CoreResourceIds.Waste].Amount = 401;
            IReadOnlyList<GameEvent> firstEvents = first.Tick(GameTime.TicksPerGameDay);
            IReadOnlyList<GameEvent> secondEvents = second.Tick(GameTime.TicksPerGameDay);
            AssertEqual(50, first.State.Waste.LastDiseaseExposureCount,
                "Every living NPC must receive one critical-waste exposure roll.");
            AssertEqual(first.State.Waste.LastDiseaseTriggeredCount, second.State.Waste.LastDiseaseTriggeredCount,
                "Equal seed, day, and NPC identities must produce equal disease triggers.");
            GameEvent firstDisease = firstEvents.Single(gameEvent =>
                gameEvent.EventType == WasteGenerationSystem.WasteDiseaseExposureSettledEvent);
            GameEvent secondDisease = secondEvents.Single(gameEvent =>
                gameEvent.EventType == WasteGenerationSystem.WasteDiseaseExposureSettledEvent);
            AssertEqual(firstDisease.Payload.GetRawText(), secondDisease.Payload.GetRawText(),
                "Disease exposure payloads must be byte-for-byte deterministic.");
        }

        private static void WasteEffectsMigrateAndSynchronizeRemotely()
        {
            GameState legacy = new GameState { SaveVersion = "2.6" };
            legacy.Npcs.Instances["npc:test:legacy_satisfaction"] = new NpcInstanceState
            {
                NpcId = "npc:test:legacy_satisfaction",
                BaseSatisfactionBasisPoints = 0
            };
            SaveSystem saves = new SaveSystem();
            GameState migrated = saves.Deserialize(saves.Serialize(legacy));
            AssertEqual("2.9", migrated.SaveVersion, "Version 2.6 must migrate to 2.9.");
            AssertEqual(10000, migrated.Npcs.Instances["npc:test:legacy_satisfaction"].BaseSatisfactionBasisPoints,
                "Legacy NPCs must receive neutral base satisfaction.");

            Simulation simulation = CreateProductionSimulation();
            simulation.State.Waste.AccumulatedSatisfactionPenaltyBasisPoints = 2000;
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            AssertTrue(remote.SendCommand(CreateConfigureProductionCommand(
                "command:test:remote_waste_effect_configure", "player:core:remote", 1, false)).Accepted,
                "Expected remote batch configuration.");
            AssertTrue(remote.SendCommand(CreateProductionCommand(
                "command:test:remote_waste_effect_start", 2,
                ProductionSystem.StartProductionCommand, "player:core:remote")).Accepted,
                "Expected remote batch start.");
            server.Tick(5);
            remote.Tick(1);
            AssertEqual(4L, GetProducerSlot(remote.CurrentState).ProgressWorkTicks,
                "Remote snapshot must expose server-calculated pollution efficiency.");
            AssertEqual(StateDiagnostics.CalculateStateHash(server.CurrentState),
                StateDiagnostics.CalculateStateHash(remote.CurrentState),
                "Waste-effect progress and remainders must synchronize exactly.");
        }

        private static void WasteGenerationCarriesNpcHalfUnits()
        {
            Simulation simulation = CreateWasteGenerationSimulation(3, true);
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(2, simulation.State.Resources.Items[CoreResourceIds.Waste].Amount,
                "One active building and three NPCs must generate 2.5 waste with integer storage.");
            AssertEqual(1, simulation.State.Waste.NpcHalfUnitRemainder,
                "The first half unit must remain authoritative state.");
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(5, simulation.State.Resources.Items[CoreResourceIds.Waste].Amount,
                "Two days must preserve the exact five-waste total.");
            AssertEqual(1, simulation.State.Waste.LastActiveBuildingCount, "Expected one active building.");
            AssertEqual(3, simulation.State.Waste.LastLivingNpcCount, "Infants and adults both count as living NPCs.");
        }

        private static void IdleBuildingsDoNotGenerateWaste()
        {
            Simulation simulation = CreateWasteGenerationSimulation(1, false);
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(0, simulation.State.Resources.Items[CoreResourceIds.Waste].Amount,
                "An idle building must not generate its daily waste.");
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Waste].Amount,
                "One NPC must generate exactly one waste over two days.");
            AssertEqual(0, simulation.State.Waste.LastActiveBuildingCount, "Unstaffed buildings must remain idle.");
        }

        private static void WasteOverflowRespectsCapacity()
        {
            Simulation simulation = CreateWasteGenerationSimulation(2, true);
            simulation.State.Resources.SharedCapacity = 1;
            simulation.State.Resources.Items[CoreResourceIds.Wood] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Wood, Amount = 1, Capacity = 10
            };
            IReadOnlyList<GameEvent> events = simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(0, simulation.State.Resources.Items[CoreResourceIds.Waste].Amount,
                "Waste may not bypass shared storage capacity.");
            AssertEqual(2L, simulation.State.Waste.TotalGeneratedAmount, "Expected two generated waste.");
            AssertEqual(2L, simulation.State.Waste.TotalDiscardedAmount, "Blocked waste must be tracked as discarded.");
            AssertTrue(events.Any(gameEvent => gameEvent.EventType == WasteGenerationSystem.WasteOverflowDiscardedEvent),
                "Waste overflow must emit a stable diagnostic event.");
        }

        private static void WasteThresholdsAccumulateAndClear()
        {
            Simulation simulation = CreateWasteGenerationSimulation(0, false);
            ResourceStack stack = simulation.State.Resources.Items[CoreResourceIds.Waste];
            stack.Amount = 201;
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(100, simulation.State.Waste.AccumulatedSatisfactionPenaltyBasisPoints,
                "Waste above 200 must add one percent daily satisfaction penalty.");
            stack.Amount = 401;
            simulation.Tick(GameTime.TicksPerGameDay);
            AssertEqual(300, simulation.State.Waste.AccumulatedSatisfactionPenaltyBasisPoints,
                "Waste above 400 must add two percent to the existing penalty.");
            AssertEqual(500, simulation.State.Waste.DiseaseChanceBonusBasisPoints,
                "Critical waste must add five percent daily disease chance.");
            stack.Amount = 0;
            simulation.Tick(1);
            AssertEqual(0, simulation.State.Waste.AccumulatedSatisfactionPenaltyBasisPoints,
                "Clearing all waste must remove satisfaction penalty immediately.");
            AssertEqual(500, simulation.State.Waste.DiseaseChanceBonusBasisPoints,
                "Disease chance must remain until the next daily settlement.");
            simulation.Tick(GameTime.TicksPerGameDay - 1);
            AssertEqual(0, simulation.State.Waste.DiseaseChanceBonusBasisPoints,
                "The next day must restore normal disease chance.");
        }

        private static void NpcDeathGeneratesImmediateWaste()
        {
            Simulation simulation = CreateLifecycleSimulation(true);
            simulation.State.Npcs.Instances["npc:core:lifecycle"].AdultLifespanTicks = 1;
            IReadOnlyList<GameEvent> events = simulation.Tick(1);
            AssertEqual(1, simulation.State.Resources.Items[CoreResourceIds.Waste].Amount,
                "Natural death must add exactly one waste immediately.");
            AssertEqual(1L, simulation.State.Waste.TotalGeneratedAmount,
                "Death waste must participate in the authoritative generated total.");
            AssertTrue(events.Any(gameEvent => gameEvent.EventType == WasteGenerationSystem.WasteGeneratedByDeathEvent),
                "Natural death must emit the death-waste event.");
        }

        private static void WasteStateMigratesAndSurvivesSave()
        {
            SaveSystem saves = new SaveSystem();
            GameState legacy = new GameState
            {
                SaveVersion = "2.5",
                SimulationTick = GameTime.TicksPerGameDay + 7,
                Waste = null
            };
            legacy.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            GameState migrated = saves.Deserialize(saves.Serialize(legacy));
            AssertEqual("2.9", migrated.SaveVersion, "Version 2.5 must migrate to 2.9.");
            AssertEqual(GameTime.TicksPerGameDay * 2, migrated.Waste.NextSettlementTick,
                "Migration must schedule waste after the current day boundary.");

            migrated.Waste.TotalGeneratedAmount = 17;
            migrated.Waste.NpcHalfUnitRemainder = 1;
            GameState loaded = saves.Deserialize(saves.Serialize(migrated));
            AssertEqual(StateDiagnostics.CalculateStateHash(migrated), StateDiagnostics.CalculateStateHash(loaded),
                "Waste counters and fractional state must survive save round trip.");
        }

        private static void RemoteWasteGenerationUsesServerAuthority()
        {
            ServerGameSession server = new ServerGameSession(
                CreateWasteGenerationSimulation(2, true), new[] { "player:test:waste" });
            RemoteGameSession remote = CreateRemoteSession(server);
            remote.Tick(GameTime.TicksPerGameDay);
            AssertEqual(0L, remote.CurrentState.Waste.TotalGeneratedAmount,
                "Remote tick must not advance waste generation.");
            server.Tick(GameTime.TicksPerGameDay);
            remote.Tick(1);
            AssertEqual(server.CurrentState.Waste.TotalGeneratedAmount, remote.CurrentState.Waste.TotalGeneratedAmount,
                "Remote must synchronize the server-owned waste total.");
            AssertEqual(StateDiagnostics.CalculateStateHash(server.CurrentState),
                StateDiagnostics.CalculateStateHash(remote.CurrentState),
                "Remote waste state must match the server hash.");
        }

        private static void WasteModeBRecoveryIsDeterministic()
        {
            Simulation first = CreateWasteProcessingSimulation(202);
            Simulation second = CreateWasteProcessingSimulation(202);
            ConfigureWasteAndStart(new LocalGameSession(first), CoreRecipeIds.RecycleWaste);
            ConfigureWasteAndStart(new LocalGameSession(second), CoreRecipeIds.RecycleWaste);
            first.Tick(GameTime.TicksPerGameDay);
            second.Tick(GameTime.TicksPerGameDay);

            BuildingInstanceState firstBuilding = first.State.Buildings.Instances["building:test:waste_processor"];
            BuildingInstanceState secondBuilding = second.State.Buildings.Instances["building:test:waste_processor"];
            AssertEqual(6, firstBuilding.LocalInventory.Values.Sum(stack => stack.Amount),
                "Mode B must recover one resource for each of six waste.");
            AssertEqual(
                JsonSerializer.Serialize(firstBuilding.LocalInventory, SaveSystem.CreateDefaultJsonOptions()),
                JsonSerializer.Serialize(secondBuilding.LocalInventory, SaveSystem.CreateDefaultJsonOptions()),
                "Equal seed and batch identity must produce equal recovery output.");
        }

        private static void WasteModeCProducesFertilizer()
        {
            Simulation simulation = CreateWasteProcessingSimulation(303);
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureWasteAndStart(session, CoreRecipeIds.CompostWaste);
            session.Tick(GameTime.TicksPerGameDay);
            AssertEqual(6, simulation.State.Buildings.Instances["building:test:waste_processor"]
                .LocalInventory[CoreResourceIds.Fertilizer].Amount,
                "Mode C must produce six fertilizer from six waste.");
        }

        private static void WasteModeDPreservesFuelRatio()
        {
            Simulation simulation = CreateWasteProcessingSimulation(404);
            LocalGameSession session = new LocalGameSession(simulation);
            ConfigureWasteAndStart(session, CoreRecipeIds.RefineWasteFuel);
            session.Tick(GameTime.TicksPerGameDay * 5 / 3);
            AssertEqual(90, simulation.State.Resources.Items[CoreResourceIds.Waste].Amount,
                "Mode D batch must consume ten waste.");
            AssertEqual(3, simulation.State.Buildings.Instances["building:test:waste_processor"]
                .LocalInventory[CoreResourceIds.Fuel].Amount,
                "Mode D must preserve the 0.3 fuel per waste ratio.");
        }

        private static void WasteProcessorModesAreMutuallyExclusive()
        {
            Simulation simulation = CreateWasteProcessingSimulation(405);
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateWasteConfigurationCommand(
                "command:test:configure_waste_a", 1, CoreRecipeIds.DestroyWaste)).Accepted,
                "Expected mode A configuration.");
            AssertTrue(session.SendCommand(CreateWasteConfigurationCommand(
                "command:test:configure_waste_c", 2, CoreRecipeIds.CompostWaste)).Accepted,
                "Idle processor must allow mode replacement.");
            ProductionSlotState slot = simulation.State.Production.SlotsByBuildingId["building:test:waste_processor"];
            AssertEqual(CoreRecipeIds.CompostWaste, slot.RecipeId,
                "Only the last selected waste mode may remain configured.");
            AssertEqual(1, simulation.State.Production.SlotsByBuildingId.Count,
                "Waste processor must own one authoritative production slot.");
        }

        private static void RemoteWasteRecoveryUsesServerAuthority()
        {
            ServerGameSession server = new ServerGameSession(
                CreateWasteProcessingSimulation(505), new[] { "player:test:waste" });
            RemoteGameSession remote = CreateRemoteSession(server);
            ConfigureWasteAndStart(remote, CoreRecipeIds.RecycleWaste);
            remote.Tick(999);
            AssertEqual(0, remote.CurrentState.Buildings.Instances["building:test:waste_processor"]
                .LocalInventory.Count, "Remote tick must not resolve recovery output.");
            server.Tick(GameTime.TicksPerGameDay);
            remote.Tick(999);
            AssertEqual(6, remote.CurrentState.Buildings.Instances["building:test:waste_processor"]
                .LocalInventory.Values.Sum(stack => stack.Amount),
                "Remote must receive the server's deterministic recovery result.");
            AssertEqual(
                StateDiagnostics.CalculateStateHash(server.CurrentState),
                StateDiagnostics.CalculateStateHash(remote.CurrentState),
                "Remote recovery state must match the server hash.");
        }

        private static void RemoteProductionUsesServerAuthority()
        {
            Simulation simulation = CreateProductionSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);

            AssertTrue(remote.SendCommand(CreateConfigureProductionCommand(
                "command:core:remote_configure", "player:core:remote", 1, false)).Accepted,
                "Remote configure must be accepted by the server.");
            CommandResult started = remote.SendCommand(CreateProductionCommand(
                "command:core:remote_start", 2, ProductionSystem.StartProductionCommand, "player:core:remote"));
            AssertTrue(started.Accepted, started.Reason);
            AssertEqual(2, server.CurrentState.Resources.Items["resource:core:ore"].LockedAmount,
                "Server state must own production locks.");
            AssertEqual(2, remote.CurrentState.Resources.Items["resource:core:ore"].LockedAmount,
                "Remote snapshot must synchronize production locks.");
            AssertEqual(GetProducerSlot(server.CurrentState).ActiveBatchId, GetProducerSlot(remote.CurrentState).ActiveBatchId,
                "Remote and server must agree on the active batch identity.");
        }

        private static void LogisticsLocksAndReservesCapacity()
        {
            Simulation simulation = CreateLogisticsSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult created = session.SendCommand(CreateTransportCommand(
                "command:core:transport_local", "player:core:local", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 4));
            AssertTrue(created.Accepted, created.Reason);
            AssertEqual(4, GetLocalWood(simulation.State, "building:core:source").LockedAmount,
                "Transport must lock source resources before transit.");
            AssertEqual(4, simulation.State.Buildings.Instances["building:core:target"].LocalInventoryReservedAmount,
                "Transport must reserve target capacity before transit.");

            session.Tick(1);
            AssertEqual(2, GetLocalWood(simulation.State, "building:core:source").Amount,
                "Completion must remove the transported amount from source.");
            AssertEqual(4, GetLocalWood(simulation.State, "building:core:target").Amount,
                "Completion must add the transported amount to target.");
            AssertEqual(0, simulation.State.Logistics.ActiveTasks.Count, "Completed task must leave active state.");
        }

        private static void LogisticsTransfersGlobalToBuilding()
        {
            Simulation simulation = CreateLogisticsSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult created = session.SendCommand(CreateTransportCommand(
                "command:core:global_to_building", "player:core:local", 1,
                LogisticsEndpointKinds.Global, string.Empty,
                LogisticsEndpointKinds.Building, "building:core:target", 3));
            AssertTrue(created.Accepted, created.Reason);
            AssertEqual(3, simulation.State.Resources.Items["resource:core:wood"].LockedAmount,
                "Global source must use the shared resource lock.");
            AssertEqual(3, simulation.State.Buildings.Instances["building:core:target"].LocalInventoryReservedAmount,
                "Building target must reserve shared local capacity.");

            session.Tick(1);
            AssertEqual(3, simulation.State.Resources.Items["resource:core:wood"].Amount,
                "Completion must debit the global warehouse.");
            AssertEqual(3, GetLocalWood(simulation.State, "building:core:target").Amount,
                "Completion must credit the building inventory.");
        }

        private static void LogisticsReservationBlocksCompetition()
        {
            Simulation simulation = CreateLogisticsSimulation(targetCapacity: 5);
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateTransportCommand(
                "command:core:reserve_first", "player:core:local", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 4)).Accepted,
                "Expected first transport reservation.");

            CommandResult competing = session.SendCommand(CreateTransportCommand(
                "command:core:reserve_second", "player:core:local", 2,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 2));
            AssertFalse(competing.Accepted, "Competing task must not overbook target capacity.");
            AssertEqual(CommandErrorCodes.LogisticsCapacityUnavailable, competing.Code,
                "Expected stable capacity-reservation rejection code.");
            AssertEqual(4, GetLocalWood(simulation.State, "building:core:source").LockedAmount,
                "Rejected task must not add a source lock.");
        }

        private static void LogisticsCancelReleasesState()
        {
            Simulation simulation = CreateLogisticsSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult created = session.SendCommand(CreateTransportCommand(
                "command:core:create_cancelled", "player:core:local", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 4));
            string taskId = created.Events.Single().Payload.Deserialize<TransportTaskPayload>(
                SaveSystem.CreateDefaultJsonOptions()).TaskId;
            CommandResult cancelled = session.SendCommand(CreateCancelTransportCommand(
                "command:core:cancel_transport", "player:core:local", 2, taskId));

            AssertTrue(cancelled.Accepted, cancelled.Reason);
            AssertEqual(0, GetLocalWood(simulation.State, "building:core:source").LockedAmount,
                "Cancel must release the source lock.");
            AssertEqual(0, simulation.State.Buildings.Instances["building:core:target"].LocalInventoryReservedAmount,
                "Cancel must release target capacity.");
            AssertEqual(6, GetLocalWood(simulation.State, "building:core:source").Amount,
                "Cancel must not consume source resources.");
        }

        private static void CrossLayerLogisticsRequiresRoute()
        {
            Simulation simulation = CreateLogisticsSimulation(targetLayer: 2);
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult missingRoute = session.SendCommand(CreateTransportCommand(
                "command:core:cross_layer_without_route", "player:core:local", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 2));
            AssertFalse(missingRoute.Accepted, "Cross-layer task must require an authority route.");
            AssertEqual(CommandErrorCodes.LogisticsRouteUnavailable, missingRoute.Code,
                "Expected stable missing-route code.");

            simulation.State.Logistics.Routes["route:core:vertical"] = new LogisticsRouteState
            {
                RouteId = "route:core:vertical",
                FirstBuildingId = "building:core:source",
                SecondBuildingId = "building:core:target"
            };
            AssertTrue(session.SendCommand(CreateTransportCommand(
                "command:core:cross_layer_with_route", "player:core:local", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 2)).Accepted,
                "Enabled route must allow cross-layer transport.");
            session.Tick(1);
            AssertEqual(0, GetLocalWood(simulation.State, "building:core:target").Amount,
                "Two-layer transport must not complete after one tick.");
            session.Tick(1);
            AssertEqual(2, GetLocalWood(simulation.State, "building:core:target").Amount,
                "Two-layer transport must complete after exactly two ticks.");
        }

        private static void LogisticsFailureCancelsTask()
        {
            Simulation simulation = CreateLogisticsSimulation(targetLayer: 2, addRoute: true);
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateTransportCommand(
                "command:core:transport_before_failure", "player:core:local", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 3)).Accepted,
                "Expected active transport before failure.");
            simulation.State.Buildings.Instances["building:core:target"].StructuralStatus = BuildingStructuralStatuses.Disabled;
            IReadOnlyList<GameEvent> events = session.Tick(1);

            AssertEqual(0, simulation.State.Logistics.ActiveTasks.Count, "Endpoint failure must cancel the task.");
            AssertEqual(0, GetLocalWood(simulation.State, "building:core:source").LockedAmount,
                "Endpoint failure must release source lock.");
            AssertEqual(0, simulation.State.Buildings.Instances["building:core:target"].LocalInventoryReservedAmount,
                "Endpoint failure must release target reservation.");
            AssertTrue(events.Any(gameEvent => gameEvent.EventType == LogisticsSystem.TransportCancelledEvent),
                "Endpoint failure must emit a cancellation event.");
        }

        private static void ActiveLogisticsBlocksDemolition()
        {
            Simulation simulation = CreateLogisticsSimulation(targetLayer: 2, addRoute: true);
            simulation.AddSystem(new BuildingSystem());
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateTransportCommand(
                "command:core:transport_before_demolition", "player:core:local", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 2)).Accepted,
                "Expected active transport before demolition.");
            CommandResult demolition = session.SendCommand(CreateDemolishCommand(
                "command:core:demolish_busy_logistics", "player:core:local", 2, "building:core:source"));
            AssertFalse(demolition.Accepted, "Normal demolition must not silently cancel an active transport.");
            AssertEqual(CommandErrorCodes.LogisticsBuildingBusy, demolition.Code,
                "Expected stable busy-logistics code.");
        }

        private static void LogisticsSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateLogisticsSimulation(targetLayer: 2, addRoute: true);
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateTransportCommand(
                "command:core:transport_before_save", "player:core:local", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 3)).Accepted,
                "Expected active transport before save.");

            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            AssertEqual("2.9", loaded.SaveVersion, "Logistics save must use version 2.9.");
            AssertEqual(1, loaded.Logistics.ActiveTasks.Count, "Active transport must survive save load.");
            AssertEqual(3, GetLocalWood(loaded, "building:core:source").LockedAmount,
                "Transport source lock must survive save load.");
            AssertEqual(3, loaded.Buildings.Instances["building:core:target"].LocalInventoryReservedAmount,
                "Transport target reservation must survive save load.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State), StateDiagnostics.CalculateStateHash(loaded),
                "Logistics state hash must survive save round trip.");
        }

        private static void RemoteLogisticsUsesServerAuthority()
        {
            Simulation simulation = CreateLogisticsSimulation(targetLayer: 2, addRoute: true);
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            CommandResult result = remote.SendCommand(CreateTransportCommand(
                "command:core:remote_transport", "player:core:remote", 1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 3));

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(3, GetLocalWood(server.CurrentState, "building:core:source").LockedAmount,
                "Server must own the transport lock.");
            AssertEqual(3, GetLocalWood(remote.CurrentState, "building:core:source").LockedAmount,
                "Remote snapshot must synchronize the transport lock.");
            AssertEqual(server.CurrentState.Logistics.ActiveTasks.Keys.Single(), remote.CurrentState.Logistics.ActiveTasks.Keys.Single(),
                "Remote and server must agree on task identity.");
        }

        private static void ConnectorConstructionCreatesRoute()
        {
            Simulation simulation = CreateConnectorSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateBuildConnectorCommand(
                "command:core:build_connector", "player:core:local", 1, 2)).Accepted,
                "Expected connector construction to start.");
            AssertEqual(8, simulation.State.Resources.Items["resource:core:wood"].Amount,
                "Connector construction must deduct its declared cost once.");

            session.Tick(2);
            LogisticsConnectorInstanceState connector = simulation.State.Logistics.Connectors.Values.Single();
            LogisticsRouteState route = simulation.State.Logistics.Routes[connector.RouteId];
            AssertEqual("building:core:connector_lower", route.FirstBuildingId, "Route source must be the lower building.");
            AssertEqual("building:core:connector_upper", route.SecondBuildingId, "Route target must be the upper building.");
            AssertEqual("resource:core:ore", route.ResourceId, "Route must preserve the selected resource.");
            AssertFalse(route.IsBidirectional, "Formal pipe route must be upward-only.");
            AssertEqual(connector.ConnectorId, route.ConnectorId, "Route must be owned by the completed connector.");
        }

        private static void ConnectorPlacementRejectsInvalidAndDuplicate()
        {
            Simulation invalidSimulation = CreateConnectorSimulation(upperAnchorX: 1);
            LocalGameSession invalidSession = new LocalGameSession(invalidSimulation);
            CommandResult invalid = invalidSession.SendCommand(CreateBuildConnectorCommand(
                "command:core:invalid_connector", "player:core:local", 1, 1));
            AssertFalse(invalid.Accepted, "Non-overlapping endpoints must reject connector placement.");
            AssertEqual(CommandErrorCodes.LogisticsConnectorPlacementInvalid, invalid.Code,
                "Expected stable connector-placement code.");

            Simulation duplicateSimulation = CreateConnectorSimulation();
            LocalGameSession duplicateSession = new LocalGameSession(duplicateSimulation);
            AssertTrue(duplicateSession.SendCommand(CreateBuildConnectorCommand(
                "command:core:first_connector", "player:core:local", 1, 1)).Accepted,
                "Expected first connector construction.");
            CommandResult duplicate = duplicateSession.SendCommand(CreateBuildConnectorCommand(
                "command:core:duplicate_connector", "player:core:local", 2, 1));
            AssertFalse(duplicate.Accepted, "The same endpoint pair must not host a second connector.");
            AssertEqual(CommandErrorCodes.LogisticsConnectorDuplicate, duplicate.Code,
                "Expected stable duplicate-connector code.");
        }

        private static void ConnectorAutomaticallyTransfersConfiguredBatch()
        {
            Simulation simulation = CreateConnectorSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateBuildConnectorCommand(
                "command:core:auto_connector", "player:core:local", 1, 2)).Accepted,
                "Expected automatic connector construction.");
            session.Tick(2);

            AssertEqual(1, simulation.State.Logistics.ActiveTasks.Count,
                "Completion must schedule exactly one automatic transport task.");
            AssertEqual(2, GetConnectorOre(simulation.State, "building:core:connector_lower").LockedAmount,
                "Automatic task must lock the configured batch amount.");
            AssertEqual(2, simulation.State.Buildings.Instances["building:core:connector_upper"].LocalInventoryReservedAmount,
                "Automatic task must reserve the configured batch amount.");

            session.Tick(1);
            AssertEqual(2, GetConnectorOre(simulation.State, "building:core:connector_upper").Amount,
                "Automatic pipe task must arrive after one layer tick.");
            AssertEqual(1, simulation.State.Logistics.ActiveTasks.Count,
                "Continuous auto-transfer may schedule only one next task.");
        }

        private static void ConnectorRouteRejectsInvalidTransport()
        {
            Simulation simulation = CreateConnectorSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateBuildConnectorCommand(
                "command:core:restricted_connector", "player:core:local", 1, 1)).Accepted,
                "Expected connector construction.");
            session.Tick(2);
            LogisticsConnectorInstanceState connector = simulation.State.Logistics.Connectors.Values.Single();
            AssertTrue(session.SendCommand(CreateConfigureConnectorCommand(
                "command:core:disable_connector_auto", "player:core:local", 2, connector.ConnectorId, false, 1)).Accepted,
                "Expected automatic transfer to be disabled.");
            string activeTaskId = simulation.State.Logistics.ActiveTasks.Keys.Single();
            AssertTrue(session.SendCommand(CreateCancelTransportCommand(
                "command:core:cancel_auto_task", "player:core:local", 3, activeTaskId)).Accepted,
                "Expected initial automatic task cancellation.");
            GetConnectorOre(simulation.State, "building:core:connector_upper").Amount = 1;

            CommandResult reverse = session.SendCommand(CreateTransportCommandForResource(
                "command:core:reverse_pipe", "player:core:local", 4,
                "building:core:connector_upper", "building:core:connector_lower", "resource:core:ore", 1));
            AssertFalse(reverse.Accepted, "Formal pipe must reject reverse transport.");
            AssertEqual(CommandErrorCodes.LogisticsRouteUnavailable, reverse.Code, "Expected direction-aware route rejection.");

            CommandResult wrongResource = session.SendCommand(CreateTransportCommandForResource(
                "command:core:wrong_pipe_resource", "player:core:local", 4,
                "building:core:connector_lower", "building:core:connector_upper", "resource:core:water", 1));
            AssertFalse(wrongResource.Accepted, "Formal pipe must reject a resource other than its configured type.");
            AssertEqual(CommandErrorCodes.LogisticsRouteUnavailable, wrongResource.Code,
                "Expected resource-aware route rejection.");
        }

        private static void ConnectorDemolitionCleansRuntimeState()
        {
            Simulation simulation = CreateConnectorSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateBuildConnectorCommand(
                "command:core:connector_before_demolition", "player:core:local", 1, 2)).Accepted,
                "Expected connector construction.");
            session.Tick(2);
            LogisticsConnectorInstanceState connector = simulation.State.Logistics.Connectors.Values.Single();
            AssertEqual(1, simulation.State.Logistics.ActiveTasks.Count, "Expected one automatic task before demolition.");

            CommandResult demolished = session.SendCommand(CreateConnectorIdCommand(
                "command:core:demolish_connector", "player:core:local", 2,
                LogisticsSystem.DemolishConnectorCommand, connector.ConnectorId));
            AssertTrue(demolished.Accepted, demolished.Reason);
            AssertTrue(connector.IsDestroyed, "Demolished connector must remain as destroyed authority state.");
            AssertEqual(0, simulation.State.Logistics.Routes.Count, "Demolition must remove the connector route.");
            AssertEqual(0, simulation.State.Logistics.ActiveTasks.Count, "Demolition must cancel route transport.");
            AssertEqual(0, GetConnectorOre(simulation.State, "building:core:connector_lower").LockedAmount,
                "Demolition must release the route task source lock.");
        }

        private static void ConnectorPausesAndResumesWithEndpoint()
        {
            Simulation simulation = CreateConnectorSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateBuildConnectorCommand(
                "command:core:connector_before_pause", "player:core:local", 1, 2)).Accepted,
                "Expected connector construction.");
            session.Tick(2);
            LogisticsConnectorInstanceState connector = simulation.State.Logistics.Connectors.Values.Single();
            BuildingInstanceState upper = simulation.State.Buildings.Instances["building:core:connector_upper"];
            upper.StructuralStatus = BuildingStructuralStatuses.Grace;
            session.Tick(1);

            AssertFalse(connector.IsDestroyed, "Temporary endpoint failure must not destroy the connector.");
            AssertEqual(1, simulation.State.Logistics.Routes.Count, "Paused connector must retain its route.");
            AssertEqual(0, simulation.State.Logistics.ActiveTasks.Count, "Paused connector must cancel in-flight transport.");
            AssertEqual(0, GetConnectorOre(simulation.State, "building:core:connector_lower").LockedAmount,
                "Paused connector must release the source lock.");

            upper.StructuralStatus = BuildingStructuralStatuses.Normal;
            session.Tick(1);
            AssertEqual(1, simulation.State.Logistics.ActiveTasks.Count,
                "Restored endpoint must allow automatic transport to resume.");
        }

        private static void ConnectorSurvivesSaveRoundTrip()
        {
            Simulation simulation = CreateConnectorSimulation();
            LocalGameSession session = new LocalGameSession(simulation);
            AssertTrue(session.SendCommand(CreateBuildConnectorCommand(
                "command:core:connector_before_save", "player:core:local", 1, 2)).Accepted,
                "Expected connector construction.");
            session.Tick(2);

            SaveSystem saves = new SaveSystem();
            GameState loaded = saves.Deserialize(saves.Serialize(simulation.State));
            AssertEqual("2.9", loaded.SaveVersion, "Connector save must use version 2.9.");
            AssertEqual(1, loaded.Logistics.Connectors.Count, "Completed connector must survive save load.");
            AssertEqual(1, loaded.Logistics.Routes.Count, "Connector route must survive save load.");
            AssertEqual(1, loaded.Logistics.ActiveTasks.Count, "Automatic in-flight task must survive save load.");
            AssertEqual(StateDiagnostics.CalculateStateHash(simulation.State), StateDiagnostics.CalculateStateHash(loaded),
                "Connector and automatic transport state hash must survive save round trip.");
        }

        private static void RemoteConnectorConstructionUsesServerAuthority()
        {
            Simulation simulation = CreateConnectorSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            CommandResult result = remote.SendCommand(CreateBuildConnectorCommand(
                "command:core:remote_connector", "player:core:remote", 1, 1));
            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(1, server.CurrentState.Logistics.ConstructionTasks.Count,
                "Server must own connector construction state.");
            AssertEqual(1, remote.CurrentState.Logistics.ConstructionTasks.Count,
                "Remote snapshot must synchronize connector construction.");
            AssertEqual(server.CurrentState.Logistics.ConstructionTasks.Keys.Single(),
                remote.CurrentState.Logistics.ConstructionTasks.Keys.Single(),
                "Server and remote must agree on connector construction identity.");
        }

        private static void AgriculturalLightLv1SunlampGenerates15CoverageCells()
        {
            IReadOnlyList<SpatialGridCell> cells = AgriculturalLightRules.GetSunlampCoverageCells(
                5, 5, 5, 0, 0, 20, 20, 64);

            AssertEqual(15, cells.Count, "Lv1 sunlamp at interior position must generate exactly 15 coverage cells.");

            bool hasCenter = false;
            bool hasPlusX = false;
            bool hasMinusX = false;
            bool hasPlusY = false;
            bool hasMinusY = false;
            int layer5Count = 0;
            int layer4Count = 0;
            int layer3Count = 0;

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Layer == 5)
                {
                    layer5Count++;
                    if (cells[i].X == 5 && cells[i].Y == 5) hasCenter = true;
                    if (cells[i].X == 6 && cells[i].Y == 5) hasPlusX = true;
                    if (cells[i].X == 4 && cells[i].Y == 5) hasMinusX = true;
                    if (cells[i].X == 5 && cells[i].Y == 6) hasPlusY = true;
                    if (cells[i].X == 5 && cells[i].Y == 4) hasMinusY = true;
                }
                else if (cells[i].Layer == 4) layer4Count++;
                else if (cells[i].Layer == 3) layer3Count++;
            }

            AssertEqual(5, layer5Count, "BaseLayer must have 5 horizontal cells.");
            AssertEqual(5, layer4Count, "BaseLayer-1 must have 5 horizontal cells.");
            AssertEqual(5, layer3Count, "BaseLayer-2 must have 5 horizontal cells.");
            AssertTrue(hasCenter, "Must include (0,0) offset.");
            AssertTrue(hasPlusX, "Must include (+1,0) offset.");
            AssertTrue(hasMinusX, "Must include (-1,0) offset.");
            AssertTrue(hasPlusY, "Must include (0,+1) offset.");
            AssertTrue(hasMinusY, "Must include (0,-1) offset.");
        }

        private static void AgriculturalLightCoverageExtendsDownwardNotUpward()
        {
            IReadOnlyList<SpatialGridCell> cells = AgriculturalLightRules.GetSunlampCoverageCells(
                5, 5, 5, 0, 0, 20, 20, 64);

            bool hasLayer6 = false;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Layer == 6) hasLayer6 = true;
            }

            AssertFalse(hasLayer6, "Sunlamp coverage must NOT include BaseLayer+1 (upward).");
        }

        private static void AgriculturalLightClipsNegativeLayersAtBaseLayerZero()
        {
            IReadOnlyList<SpatialGridCell> cells = AgriculturalLightRules.GetSunlampCoverageCells(
                5, 5, 0, 0, 0, 20, 20, 64);

            AssertEqual(5, cells.Count, "BaseLayer 0 must clip layers -1 and -2, leaving only 5 cells (1 layer x 5).");

            bool hasNegativeLayer = false;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Layer < 0) hasNegativeLayer = true;
            }

            AssertFalse(hasNegativeLayer, "No cell must have negative layer.");
        }

        private static void AgriculturalLightClipsHorizontalRangeAtPlotEdge()
        {
            IReadOnlyList<SpatialGridCell> cells = AgriculturalLightRules.GetSunlampCoverageCells(
                0, 0, 2, 0, 0, 5, 5, 64);

            bool hasMinusX = false;
            bool hasMinusY = false;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].X < 0) hasMinusX = true;
                if (cells[i].Y < 0) hasMinusY = true;
            }

            AssertFalse(hasMinusX, "Cells at plot origin must not extend to negative X.");
            AssertFalse(hasMinusY, "Cells at plot origin must not extend to negative Y.");
            AssertEqual(9, cells.Count, "Sunlamp at (0,0) in 5x5 plot must have 9 cells (3 per layer, 3 layers).");

            IReadOnlyList<SpatialGridCell> cornerCells = AgriculturalLightRules.GetSunlampCoverageCells(
                4, 4, 2, 0, 0, 5, 5, 64);
            AssertEqual(9, cornerCells.Count, "Sunlamp at (4,4) in 5x5 plot must have 9 cells (3 per layer, 3 layers).");
        }

        private static void AgriculturalLightCompletedBuildingOccludesFarmAbove()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:occlusion",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState occluder = new BuildingInstanceState
            {
                BuildingId = "building:test:occluder",
                PlotId = "plot:test:occlusion",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };

            bool occluded = AgriculturalLightRules.IsPhysicallyOccluded(farm, new[] { occluder });

            AssertTrue(occluded, "Completed building at BaseLayer=1 must occlude farm at BaseLayer=0 with height=1.");
        }

        private static void AgriculturalLightDestroyedBuildingDoesNotOcclude()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:destroyed",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState destroyed = new BuildingInstanceState
            {
                BuildingId = "building:test:destroyed",
                PlotId = "plot:test:destroyed",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = true,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };

            bool occluded = AgriculturalLightRules.IsPhysicallyOccluded(farm, new[] { destroyed });

            AssertFalse(occluded, "Destroyed building must not occlude farm.");
        }

        private static void AgriculturalLightGraceBuildingStillOccludes()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:grace",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState grace = new BuildingInstanceState
            {
                BuildingId = "building:test:grace",
                PlotId = "plot:test:grace",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Grace
            };

            bool occluded = AgriculturalLightRules.IsPhysicallyOccluded(farm, new[] { grace });

            AssertTrue(occluded, "Grace-status building must still occlude farm.");
        }

        private static void AgriculturalLightDisabledBuildingStillOccludes()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:disabled",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState disabled = new BuildingInstanceState
            {
                BuildingId = "building:test:disabled",
                PlotId = "plot:test:disabled",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Disabled
            };

            bool occluded = AgriculturalLightRules.IsPhysicallyOccluded(farm, new[] { disabled });

            AssertTrue(occluded, "Disabled-status building must still occlude farm.");
        }

        private static void AgriculturalLightConstructionTaskDoesNotOcclude()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:construction",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };

            bool occluded = AgriculturalLightRules.IsPhysicallyOccluded(farm, Array.Empty<BuildingInstanceState>());

            AssertFalse(occluded, "Construction task must not occlude farm (not passed as buildings).");
        }

        private static void AgriculturalLightOccludedFarmRecoversWithActiveSunlampCoverage()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:sunlamp_recover",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState occluder = new BuildingInstanceState
            {
                BuildingId = "building:test:above",
                PlotId = "plot:test:sunlamp_recover",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };
            BuildingInstanceState sunlamp = new BuildingInstanceState
            {
                BuildingId = "building:test:sunlamp",
                PlotId = "plot:test:sunlamp_recover",
                AnchorX = 5, AnchorY = 5, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };

            bool hasLight = AgriculturalLightRules.HasRequiredLight(
                farm, new[] { occluder }, new[] { sunlamp }, 0, 0, 20, 20, 64);

            AssertTrue(hasLight, "Occluded farm with full active sunlamp coverage must have light.");
        }

        private static void AgriculturalLightOccludedFarmWithoutActiveSunlampHasNoLight()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:no_sunlamp",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState occluder = new BuildingInstanceState
            {
                BuildingId = "building:test:blocker",
                PlotId = "plot:test:no_sunlamp",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };

            bool hasLight = AgriculturalLightRules.HasRequiredLight(
                farm, new[] { occluder }, Array.Empty<BuildingInstanceState>(),
                0, 0, 20, 20, 64);

            AssertFalse(hasLight, "Occluded farm without any active sunlamp must have no light.");
        }

        private static void AgriculturalLightSunlampInDifferentPlotDoesNotCover()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:farm_plot",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState occluder = new BuildingInstanceState
            {
                BuildingId = "building:test:blocker",
                PlotId = "plot:test:farm_plot",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };
            BuildingInstanceState sunlampOtherPlot = new BuildingInstanceState
            {
                BuildingId = "building:test:sunlamp_other",
                PlotId = "plot:test:other_plot",
                AnchorX = 5, AnchorY = 5, BaseLayer = 5,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };

            bool hasLight = AgriculturalLightRules.HasRequiredLight(
                farm, new[] { occluder }, new[] { sunlampOtherPlot },
                0, 0, 20, 20, 64);

            AssertFalse(hasLight, "Sunlamp in different plot must not provide coverage.");
        }

        private static void AgriculturalLightOverlappingMultipleSunlampsDoNotDuplicate()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:overlap",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState occluder = new BuildingInstanceState
            {
                BuildingId = "building:test:blocker",
                PlotId = "plot:test:overlap",
                AnchorX = 5, AnchorY = 5, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };
            BuildingInstanceState sunlampA = new BuildingInstanceState
            {
                BuildingId = "building:test:sunlamp_a",
                PlotId = "plot:test:overlap",
                AnchorX = 5, AnchorY = 5, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };
            BuildingInstanceState sunlampB = new BuildingInstanceState
            {
                BuildingId = "building:test:sunlamp_b",
                PlotId = "plot:test:overlap",
                AnchorX = 5, AnchorY = 5, BaseLayer = 3,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };

            bool hasLight = AgriculturalLightRules.HasRequiredLight(
                farm, new[] { occluder }, new[] { sunlampA, sunlampB },
                0, 0, 20, 20, 64);

            AssertTrue(hasLight, "Two overlapping sunlamps providing full coverage must still result in light (boolean only).");
        }

        private static void AgriculturalLightMultiCellFarmPartialCoverageStillNoLight()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:multi_partial",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 2, PlacedDepth = 2, PlacedHeight = 1
            };
            BuildingInstanceState occluder = new BuildingInstanceState
            {
                BuildingId = "building:test:wide_blocker",
                PlotId = "plot:test:multi_partial",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 2, PlacedDepth = 2, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };
            BuildingInstanceState sunlamp = new BuildingInstanceState
            {
                BuildingId = "building:test:partial_lamp",
                PlotId = "plot:test:multi_partial",
                AnchorX = 5, AnchorY = 5, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };

            bool hasLight = AgriculturalLightRules.HasRequiredLight(
                farm, new[] { occluder }, new[] { sunlamp },
                0, 0, 20, 20, 64);

            AssertFalse(hasLight, "Multi-cell farm with only partial coverage must have no light.");
        }

        private static void AgriculturalLightMultiCellFarmFullCoverageHasLight()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:multi_full",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 2, PlacedDepth = 2, PlacedHeight = 1
            };
            BuildingInstanceState occluder = new BuildingInstanceState
            {
                BuildingId = "building:test:wide_blocker2",
                PlotId = "plot:test:multi_full",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 2, PlacedDepth = 2, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };
            BuildingInstanceState sunlampA = new BuildingInstanceState
            {
                BuildingId = "building:test:lamp_a",
                PlotId = "plot:test:multi_full",
                AnchorX = 5, AnchorY = 5, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };
            BuildingInstanceState sunlampB = new BuildingInstanceState
            {
                BuildingId = "building:test:lamp_b",
                PlotId = "plot:test:multi_full",
                AnchorX = 6, AnchorY = 5, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };
            BuildingInstanceState sunlampC = new BuildingInstanceState
            {
                BuildingId = "building:test:lamp_c",
                PlotId = "plot:test:multi_full",
                AnchorX = 5, AnchorY = 6, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };
            BuildingInstanceState sunlampD = new BuildingInstanceState
            {
                BuildingId = "building:test:lamp_d",
                PlotId = "plot:test:multi_full",
                AnchorX = 6, AnchorY = 6, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };

            bool hasLight = AgriculturalLightRules.HasRequiredLight(
                farm,
                new[] { occluder },
                new[] { sunlampA, sunlampB, sunlampC, sunlampD },
                0, 0, 20, 20, 64);

            AssertTrue(hasLight, "Multi-cell farm with all cells covered by sunlamps must have light.");
        }

        private static void AgriculturalLightSunlampSelfOccludesButProvidesFullCoverageHasLight()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:farm",
                PlotId = "plot:test:self_occlude",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState sunlamp = new BuildingInstanceState
            {
                BuildingId = "building:test:self_lamp",
                PlotId = "plot:test:self_occlude",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };

            bool hasLight = AgriculturalLightRules.HasRequiredLight(
                farm,
                new[] { sunlamp },
                new[] { sunlamp },
                0, 0, 20, 20, 64);

            AssertTrue(hasLight, "Sunlamp that occludes farm but also provides full coverage must still result in light.");
        }

        private static void AgriculturalLightRotatedMultiCellFarmUsesAuthoritativeFootprint()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:rotated_farm",
                PlotId = "plot:test:rotated",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 2, PlacedDepth = 1, PlacedHeight = 1,
                RotationQuarterTurns = 1
            };

            IReadOnlyList<SpatialGridCell> cells = AgriculturalLightRules.GetFarmFootprintCells(farm);

            AssertEqual(2, cells.Count, "2x1 farm must occupy exactly 2 cells.");
            AssertTrue(cells.Contains(new SpatialGridCell(5, 5, 0)), "Rotated farm must include anchor cell (5,5,0).");
            AssertTrue(cells.Contains(new SpatialGridCell(5, 6, 0)), "Quarter-turn rotation must swap width/depth, producing (5,6,0) instead of (6,5,0).");
            AssertFalse(cells.Contains(new SpatialGridCell(6, 5, 0)), "Rotated 2x1 farm must NOT occupy (6,5,0) — that would be unrotated.");
        }

        private static void AgriculturalLightExcludesFarmItselfByBuildingId()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:self_id_farm",
                PlotId = "plot:test:self_id",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };

            bool occluded = AgriculturalLightRules.IsPhysicallyOccluded(
                farm,
                new[] { farm });

            AssertFalse(occluded, "Farm must not be occluded by itself — BuildingId exclusion must work.");
        }

        private static void AgriculturalLightRejectsNullBuildingEntryExplicitly()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:null_entry_farm",
                PlotId = "plot:test:null_entry",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };

            AssertThrows<ArgumentException>(
                () => AgriculturalLightRules.IsPhysicallyOccluded(
                    farm,
                    new BuildingInstanceState[] { null }),
                "Null building entry must throw ArgumentException, not NullReferenceException.");
        }

        private static void AgriculturalLightRejectsNullActiveSunlampEntryExplicitly()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:null_lamp_farm",
                PlotId = "plot:test:null_lamp",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };

            AssertThrows<ArgumentException>(
                () => AgriculturalLightRules.HasFullActiveSunlampCoverage(
                    farm,
                    new BuildingInstanceState[] { null },
                    0, 0, 20, 20, 64),
                "Null active sunlamp entry must throw ArgumentException, not NullReferenceException.");
        }

        private static void AgriculturalLightCoverageAvoidsCoordinateOverflowAtPlotMaximum()
        {
            IReadOnlyList<SpatialGridCell> cells = AgriculturalLightRules.GetSunlampCoverageCells(
                int.MaxValue, int.MaxValue, 2,
                int.MaxValue, int.MaxValue,
                1, 1, 64);

            // Only the center cell (0,0 offset) falls inside the 1x1 plot at int.MaxValue.
            // 3 layers: baseLayer=2, baseLayer-1=1, baseLayer-2=0.
            AssertEqual(3, cells.Count, "Only center cell within plot should be kept across 3 layers.");
            AssertEqual(int.MaxValue, cells[0].X, "First cell X must be int.MaxValue without overflow.");
            AssertEqual(int.MaxValue, cells[0].Y, "First cell Y must be int.MaxValue without overflow.");
            AssertEqual(2, cells[0].Layer, "First cell layer must be baseLayer.");
            AssertEqual(int.MaxValue, cells[1].X, "Second cell X must be int.MaxValue without overflow.");
            AssertEqual(int.MaxValue, cells[1].Y, "Second cell Y must be int.MaxValue without overflow.");
            AssertEqual(1, cells[1].Layer, "Second cell layer must be baseLayer-1.");
            AssertEqual(int.MaxValue, cells[2].X, "Third cell X must be int.MaxValue without overflow.");
            AssertEqual(int.MaxValue, cells[2].Y, "Third cell Y must be int.MaxValue without overflow.");
            AssertEqual(0, cells[2].Layer, "Third cell layer must be baseLayer-2.");
        }

        private static void AgriculturalLightRejectsTrailingNullBuildingAfterOccluder()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:null_after_occluder_farm",
                PlotId = "plot:test:null_after_occluder",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState occluder = new BuildingInstanceState
            {
                BuildingId = "building:test:real_occluder",
                PlotId = "plot:test:null_after_occluder",
                AnchorX = 5, AnchorY = 5, BaseLayer = 1,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };

            AssertThrows<ArgumentException>(
                () => AgriculturalLightRules.IsPhysicallyOccluded(
                    farm,
                    new BuildingInstanceState[] { occluder, null }),
                "Trailing null building must throw ArgumentException even when first building occludes.");
        }

        private static void AgriculturalLightRejectsTrailingNullSunlampAfterCoverage()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:null_after_coverage_farm",
                PlotId = "plot:test:null_after_coverage",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };
            BuildingInstanceState sunlamp = new BuildingInstanceState
            {
                BuildingId = "building:test:valid_lamp",
                PlotId = "plot:test:null_after_coverage",
                AnchorX = 5, AnchorY = 5, BaseLayer = 2,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1,
                IsDestroyed = false
            };

            AssertThrows<ArgumentException>(
                () => AgriculturalLightRules.HasFullActiveSunlampCoverage(
                    farm,
                    new BuildingInstanceState[] { sunlamp, null },
                    0, 0, 20, 20, 64),
                "Trailing null sunlamp must throw ArgumentException even when first lamp provides full coverage.");
        }

        private static void AgriculturalLightFinalQueryValidatesSunlampsBeforeDaylightShortCircuit()
        {
            BuildingInstanceState farm = new BuildingInstanceState
            {
                BuildingId = "building:test:validate_before_shortcircuit",
                PlotId = "plot:test:validate_before_shortcircuit",
                AnchorX = 5, AnchorY = 5, BaseLayer = 0,
                PlacedWidth = 1, PlacedDepth = 1, PlacedHeight = 1
            };

            AssertThrows<ArgumentException>(
                () => AgriculturalLightRules.HasRequiredLight(
                    farm,
                    Array.Empty<BuildingInstanceState>(),
                    new BuildingInstanceState[] { null },
                    0, 0, 20, 20, 64),
                "HasRequiredLight must validate sunlamps even when farm is not occluded.");
        }

        private static void BuildAndComplete(LocalGameSession session, long sequence, string commandSuffix, string definitionId, int layer)
        {
            CommandResult result = session.SendCommand(CreateStructuralBuildCommand(
                "command:core:" + commandSuffix,
                "player:core:local",
                sequence,
                definitionId,
                0,
                0,
                layer));
            AssertTrue(result.Accepted, result.Reason);
            session.Tick(1);
        }

        private static void StateDiagnosticsReportsUnsupportedStructure()
        {
            DefinitionRegistry definitions = CreateStructuralDefinitions();
            GameState state = CreateStructuralState();
            state.Buildings.Instances["building:core:floating"] = CreateBuildingStateWithDefinition(
                "building:core:floating", "building:core:light", "plot:core:structure", 0, 0, 1, 1, 1, 1);

            IReadOnlyList<DiagnosticIssue> issues = StateDiagnostics.CheckInvariants(state, definitions);

            DiagnosticIssue issue = issues.Single(item => item.Code == "structure.unsupported");
            AssertEqual("building:core:floating", issue.TargetIds.Single(),
                "Unsupported structure diagnostic must expose the unsupported building target.");
            AssertEqual(95, issue.Priority.GetValueOrDefault(),
                "Structural diagnostics must expose stable display priority.");
            AssertEqual("structural_support", issue.SourceSystem,
                "Structural diagnostics must expose their source system.");
        }

        private static void StateDiagnosticsReportsStructuralOverload()
        {
            DefinitionRegistry definitions = CreateStructuralDefinitions();
            GameState state = CreateStructuralState();
            state.Buildings.Instances["building:core:diagnostic_pillar"] = CreateBuildingStateWithDefinition(
                "building:core:diagnostic_pillar", "building:core:pillar", "plot:core:structure", 0, 0, 0, 1, 1, 1);
            state.Buildings.Instances["building:core:diagnostic_heavy"] = CreateBuildingStateWithDefinition(
                "building:core:diagnostic_heavy", "building:core:heavy", "plot:core:structure", 0, 0, 1, 1, 1, 1);

            IReadOnlyList<DiagnosticIssue> issues = StateDiagnostics.CheckInvariants(state, definitions);

            DiagnosticIssue issue = issues.Single(item => item.Code == "structure.capacity.exceeded");
            AssertEqual("building:core:diagnostic_pillar", issue.TargetIds.Single(),
                "Structural overload diagnostic must expose the overloaded support target.");
            AssertEqual(95, issue.Priority.GetValueOrDefault(),
                "Structural diagnostics must expose stable display priority.");
            AssertEqual("structural_support", issue.SourceSystem,
                "Structural diagnostics must expose their source system.");
        }

        private static void StateDiagnosticsReportsFarmMissingLight()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            simulation.State.Survival.NextSettlementTick = GameTime.TicksPerGameDay * 2;
            ExtendContinuousPlotForLight(simulation);
            AddLightOccluder(simulation);
            simulation.Tick(GameTime.TicksPerGameDay);

            string beforeHash = StateDiagnostics.CalculateStateHash(simulation.State);
            IReadOnlyList<DiagnosticIssue> issues =
                StateDiagnostics.CheckInvariants(simulation.State, RuntimeComposition.CreateDefinitions());
            string afterHash = StateDiagnostics.CalculateStateHash(simulation.State);

            DiagnosticIssue issue = issues.Single(item =>
                item.Code == "continuous_production.farm.no_light" &&
                item.Severity == DiagnosticSeverity.Info);
            AssertEqual("building:test:continuous", issue.TargetIds.Single(),
                "Farm no-light diagnostic must expose the farm target id.");
            AssertEqual(100, issue.Priority.GetValueOrDefault(),
                "Farm no-light diagnostic must expose stable display priority.");
            AssertEqual("continuous_production", issue.SourceSystem,
                "Farm no-light diagnostic must expose its source system.");
            AssertEqual(beforeHash, afterHash,
                "Diagnostic display metadata must not mutate authoritative state hash.");
        }

        private static void StateDiagnosticsReportsSunlampFuelTarget()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.Farm, 2, 20, 100, 0, 2);
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false, 0);
            simulation.State.Sunlamps.Buildings["building:test:sunlamp"] = new SunlampBuildingState
            {
                BuildingId = "building:test:sunlamp",
                FuelCoverageTicks = 0
            };

            string beforeHash = StateDiagnostics.CalculateStateHash(simulation.State);
            IReadOnlyList<DiagnosticIssue> issues =
                StateDiagnostics.CheckInvariants(simulation.State, RuntimeComposition.CreateDefinitions());
            string afterHash = StateDiagnostics.CalculateStateHash(simulation.State);

            DiagnosticIssue issue = issues.Single(item =>
                item.Code == "sunlamp.fuel.empty" &&
                item.Severity == DiagnosticSeverity.Info);
            AssertEqual("building:test:sunlamp", issue.TargetIds.Single(),
                "Sunlamp empty-fuel diagnostic must expose the sunlamp target id.");
            AssertEqual(90, issue.Priority.GetValueOrDefault(),
                "Sunlamp empty-fuel diagnostic must expose stable display priority.");
            AssertEqual("sunlamp", issue.SourceSystem,
                "Sunlamp empty-fuel diagnostic must expose its source system.");
            AssertEqual(beforeHash, afterHash,
                "Diagnostic display metadata must not mutate authoritative state hash.");
        }

        private static void StateDiagnosticsReportsContinuousProductionTargets()
        {
            Simulation simulation = CreateContinuousProductionSimulation(CoreBuildingIds.TreeFarm, 1, 20, 100, 0, 0);
            simulation.State.ContinuousProduction.Buildings["building:test:continuous"] =
                new ContinuousProductionBuildingState { BuildingId = "building:test:continuous" };
            ContinuousProductionBuildingState runtime =
                simulation.State.ContinuousProduction.Buildings["building:test:continuous"];

            runtime.Status = ContinuousProductionStatuses.OutputPending;
            runtime.PendingOutputAmount = 1;
            AssertContinuousProductionDiagnostic(
                simulation,
                "continuous_production.output_pending",
                80);

            runtime.Status = ContinuousProductionStatuses.PausedInput;
            runtime.PendingOutputAmount = 0;
            AssertContinuousProductionDiagnostic(
                simulation,
                "continuous_production.input_unavailable",
                70);

            runtime.Status = ContinuousProductionStatuses.PausedNoWorkers;
            AssertContinuousProductionDiagnostic(
                simulation,
                "continuous_production.no_workers",
                60);
        }

        private static void AssertContinuousProductionDiagnostic(
            Simulation simulation,
            string expectedCode,
            int expectedPriority)
        {
            string beforeHash = StateDiagnostics.CalculateStateHash(simulation.State);
            IReadOnlyList<DiagnosticIssue> issues =
                StateDiagnostics.CheckInvariants(simulation.State, RuntimeComposition.CreateDefinitions());
            string afterHash = StateDiagnostics.CalculateStateHash(simulation.State);

            DiagnosticIssue issue = issues.Single(item =>
                item.Code == expectedCode &&
                item.Severity == DiagnosticSeverity.Info);
            AssertEqual("building:test:continuous", issue.TargetIds.Single(),
                "Continuous production diagnostic must expose the building target id.");
            AssertEqual(expectedPriority, issue.Priority.GetValueOrDefault(),
                "Continuous production diagnostic must expose stable display priority.");
            AssertEqual("continuous_production", issue.SourceSystem,
                "Continuous production diagnostic must expose its source system.");
            AssertEqual(beforeHash, afterHash,
                "Diagnostic display metadata must not mutate authoritative state hash.");
        }

        private static void StateDiagnosticsReportsBatchProductionTargets()
        {
            Simulation simulation = CreateProductionSimulation();
            ProductionSlotState slot = new ProductionSlotState
            {
                BuildingId = "building:core:producer",
                Status = ProductionSlotStatuses.OutputPending
            };
            slot.OutputBuffer[CoreResourceIds.IronIngot] = 1;
            simulation.State.Production.SlotsByBuildingId[slot.BuildingId] = slot;

            AssertProductionDiagnostic(simulation, "production.output_pending", 80);

            slot.Status = ProductionSlotStatuses.Paused;
            slot.OutputBuffer.Clear();
            slot.ActiveBatchId = "batch:test:paused";
            slot.RequiredWorkTicks = 10;
            slot.ProgressWorkTicks = 5;

            AssertProductionDiagnostic(simulation, "production.paused", 60);
        }

        private static void AssertProductionDiagnostic(
            Simulation simulation,
            string expectedCode,
            int expectedPriority)
        {
            string beforeHash = StateDiagnostics.CalculateStateHash(simulation.State);
            IReadOnlyList<DiagnosticIssue> issues =
                StateDiagnostics.CheckInvariants(simulation.State, new DefinitionRegistry());
            string afterHash = StateDiagnostics.CalculateStateHash(simulation.State);

            DiagnosticIssue issue = issues.Single(item =>
                item.Code == expectedCode &&
                item.Severity == DiagnosticSeverity.Info);
            AssertEqual("building:core:producer", issue.TargetIds.Single(),
                "Production diagnostic must expose the building target id.");
            AssertEqual(expectedPriority, issue.Priority.GetValueOrDefault(),
                "Production diagnostic must expose stable display priority.");
            AssertEqual("production", issue.SourceSystem,
                "Production diagnostic must expose its source system.");
            AssertEqual(beforeHash, afterHash,
                "Diagnostic display metadata must not mutate authoritative state hash.");
        }

        private static void StateDiagnosticsReportsLogisticsTargets()
        {
            Simulation simulation = CreateLogisticsSimulation(targetLayer: 2, addRoute: true);
            LocalGameSession session = new LocalGameSession(simulation);
            CommandResult result = session.SendCommand(CreateTransportCommand(
                "command:core:diagnostic_transport",
                "player:core:local",
                1,
                LogisticsEndpointKinds.Building, "building:core:source",
                LogisticsEndpointKinds.Building, "building:core:target", 3));
            AssertTrue(result.Accepted, "Expected diagnostic transport setup to succeed.");

            TransportTaskState task = simulation.State.Logistics.ActiveTasks.Values.Single();
            simulation.State.Logistics.Routes.Remove(task.RouteId);

            string beforeHash = StateDiagnostics.CalculateStateHash(simulation.State);
            IReadOnlyList<DiagnosticIssue> issues =
                StateDiagnostics.CheckInvariants(simulation.State, RuntimeComposition.CreateDefinitions());
            string afterHash = StateDiagnostics.CalculateStateHash(simulation.State);

            DiagnosticIssue issue = issues.Single(item => item.Code == "logistics.task.route_missing");
            AssertTrue(issue.TargetIds.Contains(task.TaskId),
                "Logistics diagnostic must expose the transport task target id.");
            AssertTrue(issue.TargetIds.Contains(task.RouteId),
                "Logistics diagnostic must expose the missing route target id.");
            AssertTrue(issue.TargetIds.Contains("building:core:source"),
                "Logistics diagnostic must expose the source building target id.");
            AssertTrue(issue.TargetIds.Contains("building:core:target"),
                "Logistics diagnostic must expose the target building target id.");
            AssertTrue(issue.TargetIds.Contains(CoreResourceIds.Wood),
                "Logistics diagnostic must expose the resource target id.");
            AssertEqual(85, issue.Priority.GetValueOrDefault(),
                "Logistics diagnostic must expose stable display priority.");
            AssertEqual("logistics", issue.SourceSystem,
                "Logistics diagnostic must expose its source system.");
            AssertEqual(beforeHash, afterHash,
                "Diagnostic display metadata must not mutate authoritative state hash.");
        }

        private static void RemotePreservesStructuralRejectionCode()
        {
            Simulation simulation = CreateStructuralBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            AssertTrue(remote.SendCommand(CreateStructuralBuildCommand(
                "command:core:remote_pillar", "player:core:remote", 1, "building:core:pillar", 0, 0, 0)).Accepted, "Expected remote pillar.");

            CommandResult upper = remote.SendCommand(CreateStructuralBuildCommand(
                "command:core:remote_early_upper", "player:core:remote", 2, "building:core:light", 0, 0, 1));

            AssertFalse(upper.Accepted, "Remote upper building must be rejected while pillar is unfinished.");
            AssertEqual(CommandErrorCodes.StructuralUnsupported, upper.Code, "Transport must preserve structural error code.");
            AssertEqual(1, remote.CurrentState.Buildings.ConstructionTasks.Count, "Remote snapshot must remain authoritative after rejection.");
        }

        private static BuildingInstanceState CreateBuildingStateWithDefinition(
            string buildingId,
            string definitionId,
            string plotId,
            int anchorX,
            int anchorY,
            int baseLayer,
            int width,
            int depth,
            int height)
        {
            BuildingInstanceState instance = CreateBuildingState(
                buildingId, plotId, anchorX, anchorY, baseLayer, width, depth, height, 0);
            instance.DefinitionId = definitionId;
            return instance;
        }

        private static BuildingInstanceState CreateBuildingState(
            string buildingId,
            string plotId,
            int anchorX,
            int anchorY,
            int baseLayer,
            int width,
            int depth,
            int height,
            int rotationQuarterTurns)
        {
            return new BuildingInstanceState
            {
                BuildingId = buildingId,
                DefinitionId = "building:core:farm",
                PlotId = plotId,
                Layer = baseLayer,
                AnchorX = anchorX,
                AnchorY = anchorY,
                BaseLayer = baseLayer,
                RotationQuarterTurns = rotationQuarterTurns,
                PlacedWidth = width,
                PlacedDepth = depth,
                PlacedHeight = height,
                PlacementSchemaVersion = SpatialPlacementSchema.CurrentVersion
            };
        }

        private static SpatialPlacement CreateSpatialPlacement(
            string objectId,
            int anchorX,
            int anchorY,
            int baseLayer,
            int width = 1,
            int depth = 1,
            int height = 1,
            int rotationQuarterTurns = 0)
        {
            return new SpatialPlacement(
                objectId,
                anchorX,
                anchorY,
                baseLayer,
                width,
                depth,
                height,
                rotationQuarterTurns);
        }

        private static void EventStreamPublishesEventAndTriggersCallback()
        {
            GameState state = new GameState();
            EventStream stream = new EventStream();
            bool callbackInvoked = false;
            GameEvent callbackEvent = null;

            stream.EventEmitted += (e) =>
            {
                callbackInvoked = true;
                callbackEvent = e;
            };

            EventFactory factory = new EventFactory(state, SaveSystem.CreateDefaultJsonOptions());
            GameEvent gameEvent = factory.Create("event:core:test", "test:core:source", new { value = 42 });

            stream.Publish(state, gameEvent);

            AssertEqual(1, state.Events.Events.Count, "Expected one event in state after Publish.");
            AssertTrue(callbackInvoked, "Expected subscription callback to be invoked.");
            AssertEqual(gameEvent.EventId, callbackEvent.EventId, "Expected same event id in callback and state.");
        }

        private static void ServerSessionRejectsUnauthorizedPlayer()
        {
            Simulation simulation = CreateBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:host" });

            CommandResult result = server.SendCommand(CreateBuildCommand("command:core:unauthorized", "player:core:guest", 1));

            AssertFalse(result.Accepted, "Expected unauthorized player command to be rejected.");
            AssertEqual(0, server.CurrentState.Buildings.ConstructionTasks.Count, "Rejected command must not mutate state.");
            AssertEqual(0, server.CurrentState.Commands.ProcessedCommandIds.Count, "Rejected command must not enter command history.");
        }

        private static void RemoteSessionSubmitsThroughServerAuthority()
        {
            Simulation simulation = CreateBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);

            CommandResult result = remote.SendCommand(CreateBuildCommand("command:core:remote_build", "player:core:remote", 1));

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(1, server.CurrentState.Buildings.ConstructionTasks.Count, "Server must own the accepted construction task.");
            AssertEqual(1, remote.CurrentState.Buildings.ConstructionTasks.Count, "Remote snapshot must synchronize accepted state.");
            AssertEqual(GameSessionMode.Remote, remote.Mode, "Expected remote session mode.");
            AssertFalse(remote.CanAdvanceSimulation, "Remote session must not be allowed to advance simulation.");
            AssertTrue(server.CanAdvanceSimulation, "Server session must be allowed to advance simulation.");
            AssertFalse(remote.LastSynchronizationRepaired, "A normal accepted command must not be reported as state repair.");
            AssertEqual(0, remote.StateRepairCount, "A normal accepted command must not increment repair count.");
        }

        private static void RemoteSessionTickDoesNotAdvanceServer()
        {
            Simulation simulation = CreateBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            remote.SendCommand(CreateBuildCommand("command:core:remote_tick_build", "player:core:remote", 1));

            IReadOnlyList<GameEvent> beforeServerTick = remote.Tick(999);
            AssertEqual(0L, server.CurrentState.SimulationTick, "Remote tick must not advance authoritative time.");
            AssertEqual(0, beforeServerTick.Count, "No new server events should be synchronized before server tick.");

            server.Tick(2);
            IReadOnlyList<GameEvent> synchronized = remote.Tick(999);

            AssertEqual(2L, server.CurrentState.SimulationTick, "Server must advance its own authoritative time.");
            AssertEqual(2L, remote.CurrentState.SimulationTick, "Remote snapshot must receive authoritative time.");
            AssertEqual(1, remote.CurrentState.Buildings.Instances.Count, "Remote snapshot must receive completed building.");
            BuildingInstanceState completed = remote.CurrentState.Buildings.Instances.Values.Single();
            AssertEqual(1, completed.PlacedWidth, "Completed building must preserve the task footprint snapshot.");
            AssertEqual(SpatialPlacementSchema.CurrentVersion, completed.PlacementSchemaVersion, "Completed building must preserve placement schema.");
            AssertEqual(2, synchronized.Count, "Expected progress and completion events from authoritative tick.");
        }

        private static void RemoteSnapshotIsIsolatedAndEventsAreNotReplayed()
        {
            Simulation simulation = CreateBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            int observedEvents = 0;
            remote.Events.EventEmitted += gameEvent => observedEvents++;

            remote.SendCommand(CreateBuildCommand("command:core:remote_isolation", "player:core:remote", 1));
            AssertEqual(1, observedEvents, "Expected placed event to be observed once.");

            remote.CurrentState.SimulationTick = 999;
            AssertEqual(0L, server.CurrentState.SimulationTick, "Mutating remote snapshot must not mutate server state.");

            IReadOnlyList<GameEvent> repeated = remote.Tick(1);
            AssertEqual(0, repeated.Count, "Synchronizing without new server events must not replay old events.");
            AssertEqual(1, observedEvents, "Event callback must not receive duplicate events.");
            AssertEqual(0L, remote.CurrentState.SimulationTick, "Synchronization must replace a locally modified snapshot.");
        }

        private static void ServerSessionRevokeRejectsCommandAndPreservesState()
        {
            Simulation simulation = CreateBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:revokable" });

            bool revoked = server.RevokePlayer("player:core:revokable");
            AssertTrue(revoked, "Expected RevokePlayer to return true for authorized player.");

            bool revokedAgain = server.RevokePlayer("player:core:revokable");
            AssertFalse(revokedAgain, "Expected RevokePlayer to return false for already revoked player.");

            CommandResult result = server.SendCommand(CreateBuildCommand("command:core:revoked", "player:core:revokable", 1));

            AssertFalse(result.Accepted, "Expected revoked player command to be rejected.");
            AssertEqual(0, server.CurrentState.Buildings.ConstructionTasks.Count, "Rejected command must not mutate state.");
            AssertEqual(0, server.CurrentState.Commands.ProcessedCommandIds.Count, "Rejected command must not enter command history.");
        }

        private static void RemoteDetectsAndRepairsLocalStateDrift()
        {
            ServerGameSession server = new ServerGameSession(CreateBuildingSimulation(), new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);
            remote.CurrentState.SimulationTick = 999;

            remote.Tick(1);

            AssertTrue(remote.LastSynchronizationRepaired, "Local state drift must be reported as repaired.");
            AssertEqual(1, remote.StateRepairCount, "Expected one state repair.");
            AssertEqual(0L, remote.CurrentState.SimulationTick, "Remote state must be replaced by the authoritative snapshot.");
            AssertEqual(
                StateDiagnostics.CalculateStateHash(server.CurrentState),
                StateDiagnostics.CalculateStateHash(remote.CurrentState),
                "Remote and server state hashes must match after repair.");
        }

        private static void RemoteNormalSynchronizationDoesNotReportRepair()
        {
            ServerGameSession server = new ServerGameSession(CreateBuildingSimulation(), new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);

            remote.Tick(1);

            AssertFalse(remote.LastSynchronizationRepaired, "Matching state must not be reported as repaired.");
            AssertEqual(0, remote.StateRepairCount, "Matching state must not increment repair count.");
        }

        private static void RemoteRetriesAfterTransportCorruption()
        {
            ServerGameSession server = new ServerGameSession(CreateBuildingSimulation(), new[] { "player:core:remote" });
            SessionCompatibilityProfile profile = CreateVanillaProfile();
            CorruptOnceTransport transport = new CorruptOnceTransport(
                new LoopbackGameSessionTransport(server, profile));

            RemoteGameSession remote = new RemoteGameSession(transport, profile);

            AssertEqual(1, transport.RepairRequests, "Corrupted snapshot must trigger exactly one repair request.");
            AssertTrue(remote.LastSynchronizationRepaired, "Transport corruption repair must be observable.");
            AssertEqual(
                StateDiagnostics.CalculateStateHash(server.CurrentState),
                StateDiagnostics.CalculateStateHash(remote.CurrentState),
                "Repaired snapshot must match authoritative server state.");
        }

        private static void RemoteReconnectResumesCursorWithoutReplay()
        {
            ServerGameSession server = new ServerGameSession(CreateBuildingSimulation(), new[] { "player:core:remote" });
            SessionCompatibilityProfile profile = CreateVanillaProfile();
            LoopbackGameSessionTransport transport = new LoopbackGameSessionTransport(server, profile);
            RemoteGameSession remote = new RemoteGameSession(transport, profile);
            AssertTrue(
                remote.SendCommand(CreateBuildCommand("command:core:before_disconnect", "player:core:remote", 1)).Accepted,
                "Expected command before disconnect to succeed.");

            transport.Disconnect();
            AssertEqual(GameConnectionState.Disconnected, remote.ConnectionState, "Remote must expose transport disconnection.");
            AssertThrows<InvalidOperationException>(
                () => remote.Tick(1),
                "Disconnected remote must not synchronize before reconnect.");

            server.Tick(2);
            int observedEvents = 0;
            remote.Events.EventEmitted += gameEvent => observedEvents++;
            IReadOnlyList<GameEvent> recoveredEvents = remote.Reconnect();

            AssertEqual(GameConnectionState.Connected, remote.ConnectionState, "Successful reconnect must restore connected state.");
            AssertEqual(1, remote.ReconnectCount, "Expected one successful reconnect.");
            AssertEqual(2, recoveredEvents.Count, "Reconnect must receive only events created while disconnected.");
            AssertEqual(2, observedEvents, "Recovered events must be published once.");
            AssertEqual(2L, remote.CurrentState.SimulationTick, "Reconnect must receive current authoritative state.");
            AssertEqual(0, remote.Tick(1).Count, "First synchronization after reconnect must not replay recovered events.");
        }

        private static void RemoteReconnectPreservesCommandSequence()
        {
            ServerGameSession server = new ServerGameSession(CreateBuildingSimulation(), new[] { "player:core:remote" });
            SessionCompatibilityProfile profile = CreateVanillaProfile();
            LoopbackGameSessionTransport transport = new LoopbackGameSessionTransport(server, profile);
            RemoteGameSession remote = new RemoteGameSession(transport, profile);
            AssertTrue(
                remote.SendCommand(CreateBuildCommand("command:core:sequence_one", "player:core:remote", 1)).Accepted,
                "Expected first sequence to succeed.");

            server.Tick(2);
            transport.Disconnect();
            remote.Reconnect();

            CommandResult second = remote.SendCommand(
                CreateBuildCommand("command:core:sequence_two", "player:core:remote", 2, 1));
            CommandResult stale = remote.SendCommand(
                CreateBuildCommand("command:core:stale_sequence", "player:core:remote", 1, 1));

            AssertTrue(second.Accepted, second.Reason);
            AssertFalse(stale.Accepted, "Reconnect must not reset the player's accepted command sequence.");
            AssertEqual(
                2L,
                server.CurrentState.Commands.LastAcceptedSequenceByPlayer["player:core:remote"],
                "Server must retain the highest accepted sequence across reconnect.");
            AssertThrows<InvalidOperationException>(
                () => remote.Reconnect(),
                "Connected remote must not start a second reconnect.");
        }

        private static void RemoteSessionRejectsUnauthorizedWithoutSideEffects()
        {
            Simulation simulation = CreateBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:authorized" });
            RemoteGameSession remote = CreateRemoteSession(server);

            CommandResult result = remote.SendCommand(CreateBuildCommand("command:core:unauth_remote", "player:core:guest", 1));

            AssertFalse(result.Accepted, "Expected unauthorized player command to be rejected by remote session.");
            AssertEqual(0, server.CurrentState.Buildings.ConstructionTasks.Count, "Server must not create construction task from unauthorized command.");
            AssertEqual(0, remote.CurrentState.Buildings.ConstructionTasks.Count, "Remote snapshot must not create construction task from unauthorized command.");
        }

        private static void RemoteCommandEventsIsolatedFromBacklog()
        {
            Simulation simulation = CreateBuildingSimulation();
            ServerGameSession server = new ServerGameSession(simulation, new[] { "player:core:remote" });
            RemoteGameSession remote = CreateRemoteSession(server);

            server.SendCommand(CreateBuildCommand("command:core:server_cmd", "player:core:remote", 1));
            server.Tick(2);
            int serverEventsBefore = server.CurrentState.Events.Events.Count;
            string firstServerEventType = server.CurrentState.Events.Events[0].EventType;
            AssertEqual(3, serverEventsBefore, "Expected 3 server events after direct command + tick.");

            List<string> remoteEventTypes = new List<string>();
            remote.Events.EventEmitted += gameEvent => remoteEventTypes.Add(gameEvent.EventType);

            CommandResult result = remote.SendCommand(CreateBuildCommand("command:core:remote_cmd", "player:core:remote", 2, 1));

            AssertTrue(result.Accepted, result.Reason);
            AssertEqual(1, result.Events.Count, "CommandResult.Events must contain only events from this command, not backlog.");

            AssertEqual(serverEventsBefore + 1, remoteEventTypes.Count, "Remote.Events must receive backlog events and command events in order.");
            AssertEqual(firstServerEventType, remoteEventTypes[0], "First Remote.Events notification must be oldest backlog event.");

            IReadOnlyList<GameEvent> nextSync = remote.Tick(1);
            AssertEqual(0, nextSync.Count, "Next synchronization must not replay already received events.");
        }

        private static Simulation CreateBuildingSimulation()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:farm",
                Category = "production",
                ConstructionTicks = 2,
                MaxDurability = 500,
                CarryCapacity = 10,
                BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:000001"] = new PlotState
            {
                PlotId = "plot:core:000001",
                MaxStackLayers = 2
            };

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new BuildingSystem());
            return simulation;
        }

        private static Simulation CreateWarehouseSimulation()
        {
            GameState state = new GameState();
            state.World.Plots["plot:test:warehouse"] = new PlotState
            {
                PlotId = "plot:test:warehouse", Width = 1, Depth = 1, MaxStackLayers = 1
            };
            state.Resources.Items[CoreResourceIds.Wood] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Wood, Amount = 100, Capacity = 3000
            };
            state.Resources.Items[CoreResourceIds.Stone] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Stone, Amount = 100, Capacity = 3000
            };
            return RuntimeComposition.CreateSimulation(state, RuntimeComposition.CreateDefinitions());
        }

        private static CommandEnvelope CreateWarehouseBuildCommand(string commandId, long sequence)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = "player:test:warehouse",
                Type = BuildingSystem.BuildCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildCommandPayload
                {
                    DefinitionId = CoreBuildingIds.Warehouse,
                    PlotId = "plot:test:warehouse",
                    AnchorX = 0,
                    AnchorY = 0,
                    BaseLayer = 0
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateDemolitionCommand(
            string commandId, long sequence, string buildingId, string playerId)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = BuildingSystem.DemolishBuildingCommand,
                Payload = JsonSerializer.SerializeToElement(new DemolishBuildingPayload
                {
                    BuildingId = buildingId
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static Simulation CreateContinuousProductionSimulation(
            string definitionId,
            int workerCount,
            int localCapacity,
            int outputCapacity,
            int outputAmount,
            int waterAmount)
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            GameState state = new GameState();
            state.World.Plots["plot:test:continuous"] = new PlotState
            {
                PlotId = "plot:test:continuous",
                Width = 1,
                Depth = 1,
                MaxStackLayers = 1
            };
            BuildingInstanceState building = CreateBuildingStateWithDefinition(
                "building:test:continuous", definitionId, "plot:test:continuous", 0, 0, 0, 1, 1, 1);
            building.Durability = 500;
            building.LocalInventoryCapacity = localCapacity;
            state.Buildings.Instances.Add(building.BuildingId, building);

            string outputResourceId = definitionId == CoreBuildingIds.Farm
                ? CoreResourceIds.Food
                : definitionId == CoreBuildingIds.Well
                    ? CoreResourceIds.Water
                    : definitionId == CoreBuildingIds.ExcavationSite
                        ? CoreResourceIds.IronOre
                        : CoreResourceIds.Wood;
            state.Resources.Items[outputResourceId] = new ResourceStack
            {
                ResourceId = outputResourceId,
                Amount = outputAmount,
                Capacity = outputCapacity
            };
            if (!state.Resources.Items.ContainsKey(CoreResourceIds.Water))
            {
                state.Resources.Items[CoreResourceIds.Water] = new ResourceStack
                {
                    ResourceId = CoreResourceIds.Water,
                    Amount = waterAmount,
                    Capacity = 100
                };
            }
            else if (definitionId == CoreBuildingIds.Farm)
            {
                state.Resources.Items[CoreResourceIds.Water].Amount = waterAmount;
            }
            if (definitionId == CoreBuildingIds.ExcavationSite)
            {
                state.Resources.Items[CoreResourceIds.Stone] = new ResourceStack
                {
                    ResourceId = CoreResourceIds.Stone,
                    Amount = 0,
                    Capacity = outputCapacity
                };
            }

            for (int index = 0; index < workerCount; index++)
            {
                string npcId = $"npc:test:continuous_{index}";
                state.Npcs.Instances[npcId] = new NpcInstanceState
                {
                    NpcId = npcId,
                    OwnerPlayerId = "player:test:continuous",
                    CreationSequence = index + 1
                };
                state.Npcs.WorkAssignments[npcId] = new WorkAssignmentState
                {
                    NpcId = npcId,
                    BuildingId = building.BuildingId,
                    SlotIndex = index
                };
            }

            return RuntimeComposition.CreateSimulation(state, definitions);
        }

        private static void ExtendContinuousPlotForLight(Simulation simulation)
        {
            PlotState plot = simulation.State.World.Plots["plot:test:continuous"];
            plot.Width = 3;
            plot.Depth = 3;
            plot.MaxStackLayers = 3;
        }

        private static void AddLightOccluder(Simulation simulation)
        {
            BuildingInstanceState occluder = CreateBuildingStateWithDefinition(
                "building:test:light_occluder",
                CoreBuildingIds.House,
                "plot:test:continuous",
                0,
                0,
                1,
                1,
                1,
                1);
            occluder.Durability = 400;
            occluder.StructuralStatus = BuildingStructuralStatuses.Normal;
            simulation.State.Buildings.Instances[occluder.BuildingId] = occluder;
        }

        private static void AddSunlamp(Simulation simulation, bool destroyed, int fuelAmount = 10)
        {
            BuildingInstanceState sunlamp = CreateBuildingStateWithDefinition(
                "building:test:sunlamp",
                CoreBuildingIds.Sunlamp,
                "plot:test:continuous",
                1,
                0,
                0,
                1,
                1,
                1);
            sunlamp.Durability = destroyed ? 0 : 400;
            sunlamp.IsDestroyed = destroyed;
            sunlamp.StructuralStatus = destroyed
                ? BuildingStructuralStatuses.Disabled
                : BuildingStructuralStatuses.Normal;
            simulation.State.Buildings.Instances[sunlamp.BuildingId] = sunlamp;
            if (fuelAmount > 0)
            {
                simulation.State.Resources.Items[CoreResourceIds.Fuel] = new ResourceStack
                {
                    ResourceId = CoreResourceIds.Fuel,
                    Amount = fuelAmount,
                    Capacity = 100
                };
            }
        }

        private static int GetLocalAmount(BuildingInstanceState building, string resourceId)
        {
            return building.LocalInventory.TryGetValue(resourceId, out LocalResourceStack stack)
                ? stack.Amount
                : 0;
        }

        private static Simulation CreateFertilizerSimulation(int workerCount)
        {
            Simulation simulation = CreateContinuousProductionSimulation(
                CoreBuildingIds.Farm, workerCount, 30, 100, 0, 100);
            simulation.State.Survival.NextSettlementTick = long.MaxValue;
            simulation.State.Waste.NextSettlementTick = long.MaxValue;
            ExtendContinuousPlotForLight(simulation);
            AddSunlamp(simulation, false);
            simulation.State.Resources.Items[CoreResourceIds.Fertilizer] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Fertilizer,
                Amount = 1,
                Capacity = 100
            };
            return simulation;
        }

        private static CommandEnvelope CreateApplyFertilizerCommand(
            string commandId, string playerId, long sequence)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = ContinuousProductionSystem.ApplyFertilizerCommand,
                Payload = JsonSerializer.SerializeToElement(new ApplyFertilizerPayload
                {
                    BuildingId = "building:test:continuous"
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static Simulation CreateSpatialBuildingSimulation()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:room",
                Category = "production",
                ConstructionTicks = 2,
                MaxDurability = 500,
                CarryCapacity = 10,
                FootprintWidth = 2,
                FootprintDepth = 3,
                FootprintHeight = 1,
                BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:spatial"] = new PlotState
            {
                PlotId = "plot:core:spatial",
                X = 10,
                Y = 20,
                Width = 6,
                Depth = 6,
                MaxStackLayers = 4
            };

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new BuildingSystem());
            return simulation;
        }

        private static Simulation CreateRefundSimulation(int resourceAmount, int resourceCapacity)
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:refund_test",
                ConstructionTicks = 1,
                MaxDurability = 100,
                CarryCapacity = 10,
                BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    { "resource:core:wood", 10 }
                }
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:refund"] = new PlotState
            {
                PlotId = "plot:core:refund",
                MaxStackLayers = 2
            };
            state.Resources.Items["resource:core:wood"] = new ResourceStack
            {
                ResourceId = "resource:core:wood",
                Amount = resourceAmount,
                Capacity = resourceCapacity
            };

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new BuildingSystem());
            return simulation;
        }

        private static Simulation CreateWorkerSimulation()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:workplace_a",
                CarryCapacity = 10,
                WorkerSlotCount = 2
            });
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:workplace_b",
                CarryCapacity = 10,
                WorkerSlotCount = 1
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:workers"] = new PlotState
            {
                PlotId = "plot:core:workers",
                Width = 2,
                Depth = 1,
                MaxStackLayers = 2
            };
            BuildingInstanceState workA = CreateBuildingStateWithDefinition(
                "building:core:work_a", "building:core:workplace_a", "plot:core:workers", 0, 0, 0, 1, 1, 1);
            workA.Durability = 100;
            BuildingInstanceState workB = CreateBuildingStateWithDefinition(
                "building:core:work_b", "building:core:workplace_b", "plot:core:workers", 1, 0, 0, 1, 1, 1);
            workB.Durability = 100;
            state.Buildings.Instances[workA.BuildingId] = workA;
            state.Buildings.Instances[workB.BuildingId] = workB;
            state.Npcs.Instances["npc:core:000001"] = new NpcInstanceState
            {
                NpcId = "npc:core:000001",
                OwnerPlayerId = "player:core:local",
                CreationSequence = 1
            };
            state.Npcs.Instances["npc:core:000002"] = new NpcInstanceState
            {
                NpcId = "npc:core:000002",
                OwnerPlayerId = "player:core:local",
                CreationSequence = 2
            };

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new BuildingSystem());
            simulation.AddSystem(new WorkerAssignmentSystem());
            return simulation;
        }

        private static Simulation CreateHousingSimulation()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:test_house", CarryCapacity = 10, BedSlotCount = 2,
                AdditionalBedSlotsPerLevel = 1
            });
            GameState state = new GameState();
            state.NextConstructionSequence = 3;
            state.World.Plots["plot:core:housing"] = new PlotState
            {
                PlotId = "plot:core:housing", Width = 2, Depth = 1, MaxStackLayers = 1
            };
            for (int index = 0; index < 2; index++)
            {
                string id = index == 0 ? "building:core:home_a" : "building:core:home_b";
                BuildingInstanceState house = CreateBuildingStateWithDefinition(
                    id, "building:core:test_house", "plot:core:housing", index, 0, 0, 1, 1, 1);
                house.Durability = 100;
                house.ConstructionSequence = index + 1;
                state.Buildings.Instances[id] = house;
            }
            for (int index = 1; index <= 3; index++)
            {
                string id = "npc:core:resident_" + index;
                state.Npcs.Instances[id] = new NpcInstanceState
                {
                    NpcId = id, OwnerPlayerId = "player:core:local", CreationSequence = index
                };
            }
            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new BuildingSystem());
            simulation.AddSystem(new HousingSystem());
            return simulation;
        }

        private static Simulation CreateSurvivalSimulation(int adults, int infants, int food, int water)
        {
            GameState state = new GameState();
            state.Waste.NextSettlementTick = long.MaxValue;
            state.Resources.Items[CoreResourceIds.Food] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Food, Amount = food, Capacity = 100
            };
            state.Resources.Items[CoreResourceIds.Water] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Water, Amount = water, Capacity = 100
            };
            int sequence = 1;
            for (int index = 0; index < adults + infants; index++)
            {
                string npcId = "npc:core:survival_" + sequence;
                state.Npcs.Instances[npcId] = new NpcInstanceState
                {
                    NpcId = npcId,
                    OwnerPlayerId = "player:core:local",
                    CreationSequence = sequence,
                    IsAdult = index < adults
                };
                sequence++;
            }
            Simulation simulation = new Simulation(state, new DefinitionRegistry());
            simulation.AddSystem(new NpcSurvivalSystem());
            return simulation;
        }

        private static Simulation CreateLifecycleSimulation(bool isAdult, bool includeSurvival = false)
        {
            GameState state = new GameState();
            state.Waste.NextSettlementTick = long.MaxValue;
            state.World.Seed = "lifecycle-test-seed";
            state.Resources.Items[CoreResourceIds.Food] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Food, Amount = 100, Capacity = 100
            };
            state.Resources.Items[CoreResourceIds.Water] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Water, Amount = 100, Capacity = 100
            };
            state.Npcs.Instances["npc:core:lifecycle"] = new NpcInstanceState
            {
                NpcId = "npc:core:lifecycle", OwnerPlayerId = "player:core:local", CreationSequence = 1,
                IsAdult = isAdult, AdultLifespanTicks = 20 * GameTime.TicksPerGameDay
            };
            Simulation simulation = new Simulation(state, new DefinitionRegistry());
            simulation.AddSystem(new NpcLifecycleSystem());
            if (includeSurvival)
                simulation.AddSystem(new NpcSurvivalSystem());
            else
                state.Survival.NextSettlementTick = long.MaxValue;
            return simulation;
        }

        private static Simulation CreateWasteGenerationSimulation(int livingNpcCount, bool staffBuilding)
        {
            GameState state = new GameState();
            state.Survival.NextSettlementTick = long.MaxValue;
            state.World.Plots["plot:test:waste"] = new PlotState
            {
                PlotId = "plot:test:waste", Width = 1, Depth = 1, MaxStackLayers = 1
            };
            state.Resources.Items[CoreResourceIds.Waste] = new ResourceStack
            {
                ResourceId = CoreResourceIds.Waste,
                Capacity = WasteGenerationSystem.WasteCapacity
            };
            BuildingInstanceState building = new BuildingInstanceState
            {
                BuildingId = "building:test:waste_source",
                DefinitionId = CoreBuildingIds.Farm,
                PlotId = "plot:test:waste",
                Layer = 0,
                BaseLayer = 0,
                PlacedWidth = 1,
                PlacedDepth = 1,
                PlacedHeight = 1,
                PlacementSchemaVersion = SpatialPlacementSchema.CurrentVersion,
                Durability = 100,
                StructuralStatus = BuildingStructuralStatuses.Normal
            };
            state.Buildings.Instances[building.BuildingId] = building;
            for (int index = 0; index < livingNpcCount; index++)
            {
                string npcId = $"npc:test:waste_{index}";
                state.Npcs.Instances[npcId] = new NpcInstanceState
                {
                    NpcId = npcId,
                    CreationSequence = index + 1,
                    IsAdult = index % 2 == 0
                };
                if (staffBuilding && index == 0)
                {
                    state.Npcs.WorkAssignments[npcId] = new WorkAssignmentState
                    {
                        NpcId = npcId,
                        BuildingId = building.BuildingId,
                        SlotIndex = 0
                    };
                }
            }
            Simulation simulation = new Simulation(state, RuntimeComposition.CreateDefinitions());
            simulation.AddSystem(new WasteGenerationSystem());
            return simulation;
        }

        private static CommandEnvelope CreateAssignHousingCommand(
            string commandId, long sequence, string npcId, string buildingId, int? bedSlotIndex = null)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = "player:core:local",
                Type = HousingSystem.AssignHousingCommand,
                Payload = JsonSerializer.SerializeToElement(new AssignHousingPayload
                {
                    NpcId = npcId, BuildingId = buildingId, BedSlotIndex = bedSlotIndex
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static Simulation CreateProductionSimulation(
            int localCapacity = 5,
            int ingotCapacity = 100,
            int oreAmount = 10)
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:producer_type",
                CarryCapacity = 10,
                WorkerSlotCount = 2,
                LocalInventoryCapacity = localCapacity
            });
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:ore_cost_type",
                CarryCapacity = 10,
                BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["resource:core:ore"] = 9
                }
            });
            definitions.RegisterRecipe(new RecipeDefinition
            {
                RecipeId = "recipe:core:smelt",
                BuildingDefinitionId = "building:core:producer_type",
                RequiredWorkTicks = 10,
                MinimumWorkers = 1,
                Inputs = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["resource:core:ore"] = 2
                },
                Outputs = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["resource:core:ingot"] = 3
                }
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:production"] = new PlotState
            {
                PlotId = "plot:core:production",
                Width = 2,
                Depth = 1,
                MaxStackLayers = 1
            };
            BuildingInstanceState producer = CreateBuildingStateWithDefinition(
                "building:core:producer", "building:core:producer_type", "plot:core:production", 0, 0, 0, 1, 1, 1);
            producer.Durability = 100;
            producer.LocalInventoryCapacity = localCapacity;
            state.Buildings.Instances[producer.BuildingId] = producer;
            state.Npcs.Instances["npc:core:producer_worker"] = new NpcInstanceState
            {
                NpcId = "npc:core:producer_worker",
                OwnerPlayerId = "player:core:local",
                CreationSequence = 1
            };
            state.Npcs.WorkAssignments["npc:core:producer_worker"] = new WorkAssignmentState
            {
                NpcId = "npc:core:producer_worker",
                BuildingId = producer.BuildingId,
                SlotIndex = 0
            };
            state.Resources.Items["resource:core:ore"] = new ResourceStack
            {
                ResourceId = "resource:core:ore",
                Amount = oreAmount,
                Capacity = 100
            };
            state.Resources.Items["resource:core:ingot"] = new ResourceStack
            {
                ResourceId = "resource:core:ingot",
                Amount = 0,
                Capacity = ingotCapacity
            };

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new BuildingSystem());
            simulation.AddSystem(new WorkerAssignmentSystem());
            simulation.AddSystem(new ProductionSystem());
            return simulation;
        }

        private static Simulation CreateWasteProcessingSimulation(long seed)
        {
            DefinitionRegistry definitions = RuntimeComposition.CreateDefinitions();
            GameState state = new GameState { RngSeed = seed };
            state.Survival.NextSettlementTick = long.MaxValue;
            state.Waste.NextSettlementTick = long.MaxValue;
            state.World.Plots["plot:test:waste"] = new PlotState
            {
                PlotId = "plot:test:waste", Width = 1, Depth = 1, MaxStackLayers = 1
            };
            BuildingInstanceState building = CreateBuildingStateWithDefinition(
                "building:test:waste_processor", CoreBuildingIds.WasteProcessor,
                "plot:test:waste", 0, 0, 0, 1, 1, 1);
            building.Durability = 500;
            building.LocalInventoryCapacity = 30;
            state.Buildings.Instances[building.BuildingId] = building;
            state.Npcs.Instances["npc:test:waste_worker"] = new NpcInstanceState
            {
                NpcId = "npc:test:waste_worker", OwnerPlayerId = "player:test:waste", CreationSequence = 1
            };
            state.Npcs.WorkAssignments["npc:test:waste_worker"] = new WorkAssignmentState
            {
                NpcId = "npc:test:waste_worker", BuildingId = building.BuildingId, SlotIndex = 0
            };
            string[] resourceIds =
            {
                CoreResourceIds.Waste, CoreResourceIds.Wood, CoreResourceIds.Stone,
                CoreResourceIds.IronIngot, CoreResourceIds.Fertilizer, CoreResourceIds.Fuel
            };
            foreach (string resourceId in resourceIds)
            {
                state.Resources.Items[resourceId] = new ResourceStack
                {
                    ResourceId = resourceId,
                    Amount = StringComparer.Ordinal.Equals(resourceId, CoreResourceIds.Waste) ? 100 : 0,
                    Capacity = 500
                };
            }
            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new ProductionSystem());
            return simulation;
        }

        private static void ConfigureWasteAndStart(IGameSession session, string recipeId)
        {
            CommandResult configured = session.SendCommand(CreateWasteConfigurationCommand(
                "command:test:configure_waste", 1, recipeId));
            AssertTrue(configured.Accepted, configured.Reason);
            CommandResult started = session.SendCommand(new CommandEnvelope
            {
                CommandId = "command:test:start_waste",
                PlayerId = "player:test:waste",
                Type = ProductionSystem.StartProductionCommand,
                Payload = JsonSerializer.SerializeToElement(new ProductionBuildingPayload
                {
                    BuildingId = "building:test:waste_processor"
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = 2
            });
            AssertTrue(started.Accepted, started.Reason);
        }

        private static CommandEnvelope CreateWasteConfigurationCommand(
            string commandId, long sequence, string recipeId)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = "player:test:waste",
                Type = ProductionSystem.ConfigureProductionCommand,
                Payload = JsonSerializer.SerializeToElement(new ConfigureProductionPayload
                {
                    BuildingId = "building:test:waste_processor",
                    RecipeId = recipeId,
                    Continuous = false
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static void ConfigureAndStartProduction(LocalGameSession session, bool continuous)
        {
            AssertTrue(session.SendCommand(CreateConfigureProductionCommand(
                "command:core:configure_test", "player:core:local", 1, continuous)).Accepted,
                "Expected production configuration.");
            CommandResult started = session.SendCommand(CreateProductionCommand(
                "command:core:start_test", 2, ProductionSystem.StartProductionCommand));
            AssertTrue(started.Accepted, started.Reason);
        }

        private static ProductionSlotState GetProducerSlot(GameState state)
        {
            return state.Production.SlotsByBuildingId["building:core:producer"];
        }

        private static Simulation CreateLogisticsSimulation(
            int targetCapacity = 10,
            int targetLayer = 0,
            bool addRoute = false)
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:logistics_node",
                CarryCapacity = 10,
                LocalInventoryCapacity = 10
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:logistics"] = new PlotState
            {
                PlotId = "plot:core:logistics",
                Width = 2,
                Depth = 1,
                MaxStackLayers = 4
            };
            BuildingInstanceState source = CreateBuildingStateWithDefinition(
                "building:core:source", "building:core:logistics_node", "plot:core:logistics", 0, 0, 0, 1, 1, 1);
            source.Durability = 100;
            source.LocalInventoryCapacity = 10;
            source.LocalInventory["resource:core:wood"] = new LocalResourceStack
            {
                ResourceId = "resource:core:wood",
                Amount = 6
            };
            BuildingInstanceState target = CreateBuildingStateWithDefinition(
                "building:core:target", "building:core:logistics_node", "plot:core:logistics", 1, 0, targetLayer, 1, 1, 1);
            target.Durability = 100;
            target.LocalInventoryCapacity = targetCapacity;
            target.LocalInventory["resource:core:wood"] = new LocalResourceStack
            {
                ResourceId = "resource:core:wood",
                Amount = 0
            };
            state.Buildings.Instances[source.BuildingId] = source;
            state.Buildings.Instances[target.BuildingId] = target;
            state.Resources.Items["resource:core:wood"] = new ResourceStack
            {
                ResourceId = "resource:core:wood",
                Amount = 6,
                Capacity = 20
            };
            if (addRoute)
            {
                state.Logistics.Routes["route:core:vertical"] = new LogisticsRouteState
                {
                    RouteId = "route:core:vertical",
                    FirstBuildingId = source.BuildingId,
                    SecondBuildingId = target.BuildingId
                };
            }

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new LogisticsSystem());
            return simulation;
        }

        private static LocalResourceStack GetLocalWood(GameState state, string buildingId)
        {
            return state.Buildings.Instances[buildingId].LocalInventory["resource:core:wood"];
        }

        private static Simulation CreateConnectorSimulation(int upperAnchorX = 0)
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:connector_endpoint",
                CarryCapacity = 10,
                LocalInventoryCapacity = 10
            });
            definitions.RegisterLogisticsConnector(new LogisticsConnectorDefinition
            {
                DefinitionId = "connector:core:pipe",
                ConstructionTicks = 2,
                MaxDurability = 200,
                BuildCost = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["resource:core:wood"] = 2
                }
            });

            GameState state = new GameState();
            state.World.Plots["plot:core:connector"] = new PlotState
            {
                PlotId = "plot:core:connector",
                Width = 2,
                Depth = 1,
                MaxStackLayers = 3
            };
            BuildingInstanceState lower = CreateBuildingStateWithDefinition(
                "building:core:connector_lower", "building:core:connector_endpoint",
                "plot:core:connector", 0, 0, 0, 1, 1, 1);
            lower.Durability = 100;
            lower.LocalInventoryCapacity = 10;
            lower.LocalInventory["resource:core:ore"] = new LocalResourceStack
            {
                ResourceId = "resource:core:ore",
                Amount = 5
            };
            lower.LocalInventory["resource:core:water"] = new LocalResourceStack
            {
                ResourceId = "resource:core:water",
                Amount = 2
            };
            BuildingInstanceState upper = CreateBuildingStateWithDefinition(
                "building:core:connector_upper", "building:core:connector_endpoint",
                "plot:core:connector", upperAnchorX, 0, 1, 1, 1, 1);
            upper.Durability = 100;
            upper.LocalInventoryCapacity = 10;
            upper.LocalInventory["resource:core:ore"] = new LocalResourceStack
            {
                ResourceId = "resource:core:ore",
                Amount = 0
            };
            upper.LocalInventory["resource:core:water"] = new LocalResourceStack
            {
                ResourceId = "resource:core:water",
                Amount = 0
            };
            state.Buildings.Instances[lower.BuildingId] = lower;
            state.Buildings.Instances[upper.BuildingId] = upper;
            state.Resources.Items["resource:core:wood"] = new ResourceStack
            {
                ResourceId = "resource:core:wood",
                Amount = 10,
                Capacity = 20
            };
            state.Resources.Items["resource:core:ore"] = new ResourceStack
            {
                ResourceId = "resource:core:ore",
                Amount = 0,
                Capacity = 20
            };
            state.Resources.Items["resource:core:water"] = new ResourceStack
            {
                ResourceId = "resource:core:water",
                Amount = 0,
                Capacity = 20
            };

            Simulation simulation = new Simulation(state, definitions);
            simulation.AddSystem(new LogisticsSystem());
            return simulation;
        }

        private static LocalResourceStack GetConnectorOre(GameState state, string buildingId)
        {
            return state.Buildings.Instances[buildingId].LocalInventory["resource:core:ore"];
        }

        private static DefinitionRegistry CreateStructuralDefinitions()
        {
            DefinitionRegistry definitions = new DefinitionRegistry();
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:pillar",
                ConstructionTicks = 1,
                Weight = 1,
                CarryCapacity = 4
            });
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:light",
                ConstructionTicks = 1,
                Weight = 1,
                CarryCapacity = 0
            });
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:heavy",
                ConstructionTicks = 1,
                Weight = 5,
                CarryCapacity = 0
            });
            definitions.RegisterBuilding(new BuildingDefinition
            {
                DefinitionId = "building:core:platform",
                ConstructionTicks = 1,
                Weight = 8,
                CarryCapacity = 0,
                FootprintWidth = 2,
                FootprintDepth = 1,
                FootprintHeight = 1
            });
            return definitions;
        }

        private static GameState CreateStructuralState()
        {
            GameState state = new GameState();
            state.World.Plots["plot:core:structure"] = new PlotState
            {
                PlotId = "plot:core:structure",
                Width = 2,
                Depth = 1,
                MaxStackLayers = 3
            };
            return state;
        }

        private static Simulation CreateStructuralBuildingSimulation()
        {
            Simulation simulation = new Simulation(CreateStructuralState(), CreateStructuralDefinitions());
            simulation.AddSystem(new BuildingSystem());
            return simulation;
        }

        private static void ModCompatibilityAcceptsIdenticalProfiles()
        {
            SessionCompatibilityProfile server = CreateProfile(
                new ModFingerprint("library_mod", "1.0.0", "lib-hash", 0),
                new ModFingerprint(
                    "city_mod",
                    "2.0.0",
                    "city-hash",
                    1,
                    new Dictionary<string, string>(StringComparer.Ordinal) { { "library_mod", ">=1.0.0" } }));
            SessionCompatibilityProfile client = CreateProfile(
                new ModFingerprint("library_mod", "1.0.0", "lib-hash", 0),
                new ModFingerprint(
                    "city_mod",
                    "2.0.0",
                    "city-hash",
                    1,
                    new Dictionary<string, string>(StringComparer.Ordinal) { { "library_mod", ">=1.0.0" } }));

            CompatibilityReport report = ModCompatibility.Compare(server, client);

            AssertTrue(report.Compatible, report.CreateSummary());
            AssertEqual(0, report.Issues.Count, "Compatible profiles must not report issues.");
        }

        private static void ModCompatibilityReportsRuntimeMismatch()
        {
            SessionCompatibilityProfile server = SessionCompatibilityProfile.CreateVanilla("1", "0.1.0");
            SessionCompatibilityProfile client = SessionCompatibilityProfile.CreateVanilla("2", "0.2.0");

            CompatibilityReport report = ModCompatibility.Compare(server, client);

            AssertIssue(report, CompatibilityIssueCode.ProtocolVersionMismatch);
            AssertIssue(report, CompatibilityIssueCode.GameVersionMismatch);
        }

        private static void ModCompatibilityReportsMissingAndUnexpectedMods()
        {
            SessionCompatibilityProfile server = CreateProfile(
                new ModFingerprint("server_mod", "1.0.0", "server-hash", 0));
            SessionCompatibilityProfile client = CreateProfile(
                new ModFingerprint("client_mod", "1.0.0", "client-hash", 0));

            CompatibilityReport report = ModCompatibility.Compare(server, client);

            AssertIssue(report, CompatibilityIssueCode.MissingClientMod, "server_mod");
            AssertIssue(report, CompatibilityIssueCode.UnexpectedClientMod, "client_mod");
        }

        private static void ModCompatibilityReportsModDetailMismatches()
        {
            SessionCompatibilityProfile server = CreateProfile(
                new ModFingerprint(
                    "shared_mod",
                    "1.0.0",
                    "server-hash",
                    0,
                    new Dictionary<string, string>(StringComparer.Ordinal) { { "base_mod", ">=1.0.0" } }));
            SessionCompatibilityProfile client = CreateProfile(
                new ModFingerprint(
                    "shared_mod",
                    "2.0.0",
                    "client-hash",
                    1,
                    new Dictionary<string, string>(StringComparer.Ordinal) { { "base_mod", ">=2.0.0" } }));

            CompatibilityReport report = ModCompatibility.Compare(server, client);

            AssertIssue(report, CompatibilityIssueCode.ModVersionMismatch, "shared_mod");
            AssertIssue(report, CompatibilityIssueCode.ModChecksumMismatch, "shared_mod");
            AssertIssue(report, CompatibilityIssueCode.ModLoadOrderMismatch, "shared_mod");
            AssertIssue(report, CompatibilityIssueCode.ModDependenciesMismatch, "shared_mod");
        }

        private static void ModRegistryCompatibilityProfileIsSnapshot()
        {
            ModManifest manifest = new ModManifest
            {
                ModId = "snapshot_mod",
                Name = "Snapshot",
                Version = "1.0.0",
                MinGameVersion = "0.1.0",
                Dependencies = new Dictionary<string, string>(StringComparer.Ordinal),
                ContentTypes = new List<string> { "items" },
                Checksum = "original-hash"
            };
            ModRegistry registry = new ModRegistry();
            AssertTrue(registry.RegisterManifest(manifest).Accepted, "Expected manifest registration to succeed.");

            manifest.Version = "9.9.9";
            manifest.Checksum = "changed-hash";
            manifest.Dependencies["late_mod"] = ">=1.0.0";
            SessionCompatibilityProfile profile = registry.CreateCompatibilityProfile("1", "0.1.0");

            AssertEqual("1.0.0", profile.Mods[0].Version, "Profile version must be captured at creation time.");
            AssertEqual("original-hash", profile.Mods[0].Checksum, "Profile checksum must be captured at creation time.");
            AssertEqual(0, profile.Mods[0].Dependencies.Count, "Profile dependencies must not follow later manifest mutations.");
        }

        private static void TransportBlocksStateBeforeHandshake()
        {
            ServerGameSession server = new ServerGameSession(CreateBuildingSimulation(), new[] { "player:core:remote" });
            LoopbackGameSessionTransport transport = new LoopbackGameSessionTransport(server, CreateVanillaProfile());

            AssertEqual(GameConnectionState.Connecting, transport.ConnectionState, "Transport must start in connecting state.");
            AssertThrows<InvalidOperationException>(
                () => transport.Synchronize(0, string.Empty),
                "Transport must reject synchronization before handshake.");

            CompatibilityReport report = transport.Handshake(
                SessionCompatibilityProfile.CreateVanilla("different", "0.1.0"));
            AssertFalse(report.Compatible, "Mismatched handshake must be rejected.");
            AssertEqual(GameConnectionState.Disconnected, transport.ConnectionState, "Rejected handshake must disconnect transport.");
            AssertThrows<InvalidOperationException>(
                () => transport.Submit(CreateBuildCommand("command:core:blocked", "player:core:remote", 1), 0, string.Empty),
                "Rejected transport must block commands.");
        }

        private static void RemoteSessionRejectsIncompatibleHandshake()
        {
            ServerGameSession server = new ServerGameSession(CreateBuildingSimulation(), new[] { "player:core:remote" });
            LoopbackGameSessionTransport transport = new LoopbackGameSessionTransport(server, CreateVanillaProfile());

            try
            {
                _ = new RemoteGameSession(
                    transport,
                    SessionCompatibilityProfile.CreateVanilla("1", "different-game"));
                throw new InvalidOperationException("Expected incompatible remote session to be rejected.");
            }
            catch (SessionCompatibilityException exception)
            {
                AssertIssue(exception.Report, CompatibilityIssueCode.GameVersionMismatch);
            }

            AssertEqual(GameConnectionState.Disconnected, transport.ConnectionState, "Rejected remote session must remain disconnected.");
            AssertEqual(0, server.CurrentState.Commands.ProcessedCommandIds.Count, "Rejected connection must not reach server commands.");
        }

        private static RemoteGameSession CreateRemoteSession(ServerGameSession server)
        {
            SessionCompatibilityProfile profile = CreateVanillaProfile();
            return new RemoteGameSession(new LoopbackGameSessionTransport(server, profile), profile);
        }

        private static SessionCompatibilityProfile CreateVanillaProfile()
        {
            return SessionCompatibilityProfile.CreateVanilla(SessionCompatibilityProfile.CurrentProtocolVersion, "0.1.0");
        }

        private static SessionCompatibilityProfile CreateProfile(params ModFingerprint[] mods)
        {
            return new SessionCompatibilityProfile(SessionCompatibilityProfile.CurrentProtocolVersion, "0.1.0", mods);
        }

        private sealed class CorruptOnceTransport : IGameSessionTransport
        {
            private readonly IGameSessionTransport _inner;
            private bool _corruptNextSynchronization = true;

            public CorruptOnceTransport(IGameSessionTransport inner)
            {
                _inner = inner;
            }

            public int RepairRequests { get; private set; }

            public GameConnectionState ConnectionState
            {
                get { return _inner.ConnectionState; }
            }

            public CompatibilityReport Handshake(SessionCompatibilityProfile clientProfile)
            {
                return _inner.Handshake(clientProfile);
            }

            public CompatibilityReport Reconnect(SessionCompatibilityProfile clientProfile)
            {
                return _inner.Reconnect(clientProfile);
            }

            public SessionTransportResponse Submit(CommandEnvelope command, long eventCursor, string clientStateHash)
            {
                return _inner.Submit(command, eventCursor, clientStateHash);
            }

            public SessionTransportResponse Synchronize(long eventCursor, string clientStateHash)
            {
                SessionTransportResponse response = _inner.Synchronize(eventCursor, clientStateHash);
                if (!_corruptNextSynchronization)
                {
                    return response;
                }

                _corruptNextSynchronization = false;
                response.State.SimulationTick++;
                return response;
            }

            public SessionTransportResponse RequestAuthoritativeSnapshot(long eventCursor)
            {
                RepairRequests++;
                return _inner.RequestAuthoritativeSnapshot(eventCursor);
            }
        }

        private static CommandEnvelope CreateBuildCommand(string commandId, string playerId, long sequence, int layer = 0)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = BuildingSystem.BuildCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildCommandPayload
                {
                    DefinitionId = "building:core:farm",
                    PlotId = "plot:core:000001",
                    Layer = layer
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateFormalBuildCommand(
            string commandId,
            long sequence,
            string definitionId,
            int anchorX)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = "player:test:initial_content",
                Type = BuildingSystem.BuildCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildCommandPayload
                {
                    DefinitionId = definitionId,
                    PlotId = "plot:core:initial_content",
                    AnchorX = anchorX,
                    AnchorY = 0,
                    BaseLayer = 0
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateSpatialBuildCommand(
            string commandId,
            string playerId,
            long sequence,
            int anchorX,
            int anchorY,
            int baseLayer,
            int rotationQuarterTurns)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = BuildingSystem.BuildCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildCommandPayload
                {
                    DefinitionId = "building:core:room",
                    PlotId = "plot:core:spatial",
                    Layer = baseLayer,
                    AnchorX = anchorX,
                    AnchorY = anchorY,
                    BaseLayer = baseLayer,
                    RotationQuarterTurns = rotationQuarterTurns
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateStructuralBuildCommand(
            string commandId,
            string playerId,
            long sequence,
            string definitionId,
            int anchorX,
            int anchorY,
            int baseLayer)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = BuildingSystem.BuildCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildCommandPayload
                {
                    DefinitionId = definitionId,
                    PlotId = "plot:core:structure",
                    Layer = baseLayer,
                    AnchorX = anchorX,
                    AnchorY = anchorY,
                    BaseLayer = baseLayer
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateDemolishCommand(
            string commandId,
            string playerId,
            long sequence,
            string buildingId)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = BuildingSystem.DemolishBuildingCommand,
                Payload = JsonSerializer.SerializeToElement(new DemolishBuildingPayload
                {
                    BuildingId = buildingId
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateRefundBuildCommand(
            string commandId,
            string playerId,
            long sequence,
            bool useExtraAcceleration)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = BuildingSystem.BuildCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildCommandPayload
                {
                    DefinitionId = "building:core:refund_test",
                    PlotId = "plot:core:refund",
                    Layer = 0,
                    UseExtraResourceAcceleration = useExtraAcceleration
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateAssignWorkerCommand(
            string commandId,
            string playerId,
            long sequence,
            string npcId,
            string buildingId)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = WorkerAssignmentSystem.AssignWorkerCommand,
                Payload = JsonSerializer.SerializeToElement(new AssignWorkerPayload
                {
                    NpcId = npcId,
                    BuildingId = buildingId
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateConfigureProductionCommand(
            string commandId,
            string playerId,
            long sequence,
            bool continuous)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = ProductionSystem.ConfigureProductionCommand,
                Payload = JsonSerializer.SerializeToElement(new ConfigureProductionPayload
                {
                    BuildingId = "building:core:producer",
                    RecipeId = "recipe:core:smelt",
                    Continuous = continuous
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateProductionCommand(
            string commandId,
            long sequence,
            string commandType,
            string playerId = "player:core:local")
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = commandType,
                Payload = JsonSerializer.SerializeToElement(new ProductionBuildingPayload
                {
                    BuildingId = "building:core:producer"
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateProductionCostBuildCommand(long sequence)
        {
            return new CommandEnvelope
            {
                CommandId = "command:core:production_cost_build",
                PlayerId = "player:core:local",
                Type = BuildingSystem.BuildCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildCommandPayload
                {
                    DefinitionId = "building:core:ore_cost_type",
                    PlotId = "plot:core:production",
                    AnchorX = 1,
                    AnchorY = 0,
                    BaseLayer = 0
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateTransportCommand(
            string commandId,
            string playerId,
            long sequence,
            string sourceKind,
            string sourceBuildingId,
            string targetKind,
            string targetBuildingId,
            int amount)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = LogisticsSystem.CreateTransportCommand,
                Payload = JsonSerializer.SerializeToElement(new CreateTransportPayload
                {
                    SourceKind = sourceKind,
                    SourceBuildingId = sourceBuildingId,
                    TargetKind = targetKind,
                    TargetBuildingId = targetBuildingId,
                    ResourceId = "resource:core:wood",
                    Amount = amount
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateCancelTransportCommand(
            string commandId,
            string playerId,
            long sequence,
            string taskId)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = LogisticsSystem.CancelTransportCommand,
                Payload = JsonSerializer.SerializeToElement(new CancelTransportPayload
                {
                    TaskId = taskId
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateBuildConnectorCommand(
            string commandId,
            string playerId,
            long sequence,
            int autoTransferAmount)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = LogisticsSystem.BuildConnectorCommand,
                Payload = JsonSerializer.SerializeToElement(new BuildConnectorPayload
                {
                    DefinitionId = "connector:core:pipe",
                    LowerBuildingId = "building:core:connector_lower",
                    UpperBuildingId = "building:core:connector_upper",
                    ResourceId = "resource:core:ore",
                    AutoTransferAmount = autoTransferAmount
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateConfigureConnectorCommand(
            string commandId,
            string playerId,
            long sequence,
            string connectorId,
            bool enabled,
            int amount)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = LogisticsSystem.ConfigureConnectorCommand,
                Payload = JsonSerializer.SerializeToElement(new ConfigureConnectorPayload
                {
                    ConnectorId = connectorId,
                    AutoTransferEnabled = enabled,
                    AutoTransferAmount = amount
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateConnectorIdCommand(
            string commandId,
            string playerId,
            long sequence,
            string commandType,
            string connectorId)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = commandType,
                Payload = JsonSerializer.SerializeToElement(new ConnectorIdPayload
                {
                    ConnectorId = connectorId
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private static CommandEnvelope CreateTransportCommandForResource(
            string commandId,
            string playerId,
            long sequence,
            string sourceBuildingId,
            string targetBuildingId,
            string resourceId,
            int amount)
        {
            return new CommandEnvelope
            {
                CommandId = commandId,
                PlayerId = playerId,
                Type = LogisticsSystem.CreateTransportCommand,
                Payload = JsonSerializer.SerializeToElement(new CreateTransportPayload
                {
                    SourceKind = LogisticsEndpointKinds.Building,
                    SourceBuildingId = sourceBuildingId,
                    TargetKind = LogisticsEndpointKinds.Building,
                    TargetBuildingId = targetBuildingId,
                    ResourceId = resourceId,
                    Amount = amount
                }, SaveSystem.CreateDefaultJsonOptions()),
                Sequence = sequence
            };
        }

        private sealed class TestDefinitionModule : IDefinitionModule
        {
            private readonly Action<DefinitionRegistry> _register;

            public string ModuleId { get; }

            public TestDefinitionModule(string moduleId, Action<DefinitionRegistry> register)
            {
                ModuleId = moduleId;
                _register = register ?? throw new ArgumentNullException(nameof(register));
            }

            public void RegisterDefinitions(DefinitionRegistry registry)
            {
                _register(registry);
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertIssue(
            CompatibilityReport report,
            CompatibilityIssueCode code,
            string modId = null)
        {
            AssertTrue(
                report.Issues.Any(issue =>
                    issue.Code == code && (modId == null || StringComparer.Ordinal.Equals(modId, issue.ModId))),
                $"Expected compatibility issue {code} for {modId ?? "the session"}. Actual: {report.CreateSummary()}");
        }

        private static void AssertThrows<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(message + " Expected: " + expected + ", Actual: " + actual + ".");
            }
        }
    }
}
