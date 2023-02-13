using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using ImprovedHordes.Horde.Data;

using static ImprovedHordes.Utils.Logger;

using CustomModManager.API;

namespace ImprovedHordes.Horde
{
    public abstract class HordeGenerator
    {
        private static bool s_horde_per_player = false;

        private static bool HORDE_PER_PLAYER
        {
            get
            {
                return s_horde_per_player;
            }
        }

        protected string type;
        public HordeGenerator(string type)
        {
            this.type = type;
        }

        public static void ReadSettings(Settings settings)
        {
            s_horde_per_player = settings.GetBool("horde_per_player", false);
        }

        public static void HookSettings(ModManagerAPI.ModSettings modSettings)
        {
            modSettings.Hook<bool>("hordeGeneralHordePerPlayer", "IHxuiHordeGeneralHordePerPlayerModSetting", value => s_horde_per_player = value, () => s_horde_per_player, toStr => (toStr.ToString(), toStr ? "Yes" : "No"), str =>
            {
                bool success = bool.TryParse(str, out bool val);
                return (val, success);
            }).SetAllowedValues(new bool[] { false, true }).SetTab("hordeGeneralSettingsTab");
        }

        private static BiomeDefinition GetBiomeDefinition(Vector3 hordeSpawnPosition)
        {
            BiomeDefinition biomeDef = ImprovedHordesManager.Instance.World.GetBiome(global::Utils.Fastfloor(hordeSpawnPosition.x), global::Utils.Fastfloor(hordeSpawnPosition.z));

            if (biomeDef == null)
            {
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        biomeDef = ImprovedHordesManager.Instance.World.GetBiome(global::Utils.Fastfloor(hordeSpawnPosition.x) + i, global::Utils.Fastfloor(hordeSpawnPosition.z) + j);

                        if (biomeDef != null)
                            break;
                    }

                    if (biomeDef != null)
                        break;
                }
            }

            return biomeDef;
        }

        private static ChunkAreaBiomeSpawnData GetChunkAreaBiomeSpawnData(Vector3 hordeSpawnPosition)
        {
            IChunk chunk = GameManager.Instance.World.GetChunkSync(Chunk.ToAreaMasterChunkPos(new Vector3i(global::Utils.Fastfloor(hordeSpawnPosition.x), global::Utils.Fastfloor(hordeSpawnPosition.y), global::Utils.Fastfloor(hordeSpawnPosition.z))));
            ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData = chunk != null ? ((Chunk)chunk).GetChunkBiomeSpawnData() : null;

            if (chunkAreaBiomeSpawnData != null)
                Utils.CheckPOITags(chunkAreaBiomeSpawnData);

            return chunkAreaBiomeSpawnData;
        }

        public class HordeGenerationData
        {
            public readonly PlayerHordeGroup PlayerGroup;
            public readonly int PlayerGamestage;
            public readonly bool Feral;
            public readonly Vector3 HordeSpawnPosition;
            public readonly BiomeDefinition HordeSpawnBiome;
            public readonly ChunkAreaBiomeSpawnData HordeSpawnChunkAreaBiomeSpawnData;
            public readonly bool IsDay;

            public readonly HordeGroup HordeGroup;

            public HordeGenerationData(PlayerHordeGroup playerGroup, bool feral, Vector3 hordeSpawnPosition)
            {
                this.PlayerGroup = playerGroup;
                this.Feral = feral;
                this.HordeSpawnPosition = hordeSpawnPosition;

                this.HordeSpawnBiome = GetBiomeDefinition(hordeSpawnPosition);
                this.HordeSpawnChunkAreaBiomeSpawnData = GetChunkAreaBiomeSpawnData(hordeSpawnPosition);

                this.PlayerGamestage = PlayerGroup.GetGroupGamestage(hordeSpawnPosition);
                this.IsDay = ImprovedHordesManager.Instance.World.IsDaytime();
            }

            public HordeGenerationData(PlayerHordeGroup playerGroup, bool feral, Vector3 hordeSpawnPosition, HordeGroup hordeGroup) : this(playerGroup, feral, hordeSpawnPosition)
            {
                this.HordeGroup = hordeGroup;
            }
        }

        protected class HordeGroupEntityPool
        {
            public readonly HordeGroup HordeGroup;
            public readonly List<HordeGroupEntity> Entities;

            public HordeGroupEntityPool(HordeGroup hordeGroup, HordeGenerationData hordeGenerationData, Func<HordeGenerationData, HordeGroup, bool> extraSort)
            {
                this.HordeGroup = hordeGroup;
                this.Entities = new List<HordeGroupEntity>();

                this.Sort(hordeGenerationData, extraSort);
            }

            private void Sort(HordeGenerationData hordeGenerationData, Func<HordeGenerationData, HordeGroup, bool> extraSort)
            {
                foreach (var entity in this.HordeGroup.entities)
                {
                    if (entity.biomes != null)
                    {
                        // Biome specific spawns.
                        HashSet<string> biomes = entity.biomes.Evaluate();

                        if (!biomes.Contains(hordeGenerationData.HordeSpawnBiome.m_sBiomeName))
                        {
                            continue;
                        }
                    }

                    if (entity.timeOfDay != null)
                    {
                        // Time of day specific spawns.
                        ETimeOfDay timeOfDay = entity.timeOfDay.Evaluate();
                        bool isDay = hordeGenerationData.IsDay;

                        if ((timeOfDay == ETimeOfDay.Night && isDay) || (timeOfDay == ETimeOfDay.Day && !isDay))
                            continue;
                    }

                    if (entity.tags != null)
                    {
                        POITags tags = entity.tags.Evaluate();
                        ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData = hordeGenerationData.HordeSpawnChunkAreaBiomeSpawnData;

                        if (chunkAreaBiomeSpawnData == null ||
                            !chunkAreaBiomeSpawnData.checkedPOITags ||
                            (!chunkAreaBiomeSpawnData.poiTags.IsEmpty && !chunkAreaBiomeSpawnData.poiTags.Test_AnySet(tags)))
                            continue;
                    }

                    if (entity.gs != null)
                    {
                        GS gs = entity.gs;
                        int gamestage = hordeGenerationData.PlayerGamestage;

                        if (gs.min != null && gamestage < gs.min.Evaluate())
                            continue;

                        if (gs.max != null && gamestage >= gs.max.Evaluate())
                            continue;
                    }

                    if (extraSort != null && !extraSort.Invoke(hordeGenerationData, this.HordeGroup))
                        continue;

                    this.Entities.Add(entity);
                }
            }
        }

        protected virtual bool CanHordeGroupBePicked(HordeGenerationData hordeGenerationData, HordeGroup group)
        {
            return true;
        }

        public bool GenerateHorde(HordeGenerationData hordeGenerationData, out Horde horde)
        {
            HordeGroup group = hordeGenerationData.HordeGroup;
            HordeGroupEntityPool pool;

            if (group != null)
            {
                pool = new HordeGroupEntityPool(group, hordeGenerationData, this.CanHordeGroupBePicked);
            }
            else
            {
                var groups = HordesList.hordes[this.type].hordes;
                List<HordeGroupEntityPool> pools = new List<HordeGroupEntityPool>();

                foreach (var groupEntry in groups.Values)
                {
                    HordeGroupEntityPool poolEntry = new HordeGroupEntityPool(groupEntry, hordeGenerationData, this.CanHordeGroupBePicked);

                    if(poolEntry.Entities.Count > 0)
                        pools.Add(poolEntry);
                }

                if (pools.Count == 0)
                {
                    // No groups to select.
                    horde = null;
                    return false;
                }

                GameRandom random = ImprovedHordesManager.Instance.Random;
                pool = RandomPool(pools, random);
            }

            return GenerateHorde(hordeGenerationData, pool, out horde);
        }

        private bool GenerateHorde(HordeGenerationData hordeGenerationData, HordeGroupEntityPool pool, out Horde horde)
        {
            GameRandom random = ImprovedHordesManager.Instance.Random;
            Dictionary<HordeGroupEntity, int> entitiesToSpawn = new Dictionary<HordeGroupEntity, int>();

            EvaluateEntitiesInPool(pool, ref entitiesToSpawn, hordeGenerationData);

            if (entitiesToSpawn.Count == 0)
            {
                // No entities to spawn.
                horde = null;
                return false;
            }

            List<int> entityIds = new List<int>();
            int totalCount = 0;

            foreach (var entitySet in entitiesToSpawn)
            {
                HordeGroupEntity ent = entitySet.Key;
                int count = entitySet.Value;

                if (HORDE_PER_PLAYER)
                    count *= hordeGenerationData.PlayerGroup.members.Count;

                if (ent.name != null)
                {
                    int entityId = EntityClass.FromString(ent.name);

                    for (var i = 0; i < count; i++)
                        entityIds.Add(entityId);

                    totalCount += count;
                }
                else if (ent.group != null)
                {
                    int lastEntityId = -1;

                    for (var i = 0; i < count; i++)
                    {
                        int entityId = EntityGroups.GetRandomFromGroup(ent.group, ref lastEntityId, random);

                        entityIds.Add(entityId);
                    }

                    totalCount += count;
                }
                else
                {
                    Error("[{0}] Horde entity in group {1} has no name or group. Skipping.", this.GetType().FullName, hordeGenerationData.HordeGroup.name);
                    continue;
                }
            }

            entityIds.Randomize();

            horde = new Horde(hordeGenerationData.PlayerGroup, hordeGenerationData.HordeGroup, hordeGenerationData.PlayerGamestage, totalCount, hordeGenerationData.Feral, entityIds);
            return true;
        }

        private HordeGroupEntityPool RandomPool(List<HordeGroupEntityPool> pools, GameRandom random)
        {
            HordeGroupEntityPool pickedPool = null;

            float totalWeight = 0.0f;
            Dictionary<HordeGroupEntityPool, WeightRange> weightedPools = new Dictionary<HordeGroupEntityPool, WeightRange>();

            foreach(var pool in pools)
            {
                float weight = pool.HordeGroup.Weight != null ? pool.HordeGroup.Weight.Evaluate() : 1.0f;
                
                if(pool.HordeGroup.PrefWeekDays != null)
                {
                    HashSet<int> prefDays = pool.HordeGroup.PrefWeekDays.Evaluate();

                    if(prefDays.Contains(RuntimeEval.Registry.GetVariable<int>("weekDay")))
                    {
                        weight += (weight < 1.0 ? 1 / weight : weight) * prefDays.Count;
                    }
                }

                weightedPools.Add(pool, new WeightRange
                {
                    Min = totalWeight,
                    Max = totalWeight + weight
                });

                totalWeight += weight;
            }

            float pickedWeight = random.RandomRange(totalWeight);

            foreach(var poolEntry in weightedPools)
            {
                var pool = poolEntry.Key;
                var weights = poolEntry.Value;

                if(weights.InRange(pickedWeight))
                {
                    pickedPool = pool;
                    break;
                }
            }

            return pickedPool;
        }

        struct WeightRange
        {
            public float Min;
            public float Max;

            public bool InRange(float value)
            {
                return value >= Min && value < Max;
            }
        }

        private void EvaluateEntitiesInPool(HordeGroupEntityPool randomGroupEntityPool, ref Dictionary<HordeGroupEntity, int> entitiesToSpawn, HordeGenerationData hordeGenerationData)
        {
            GameRandom random = ImprovedHordesManager.Instance.Random;
            bool isDay = ImprovedHordesManager.Instance.World.IsDaytime();

            foreach (var entity in randomGroupEntityPool.Entities)
            {
                int minCount = entity.minCount != null ? entity.minCount.Evaluate() : 0;
                int maxCount = entity.maxCount != null ? entity.maxCount.Evaluate() : -1;

                GS gs = entity.gs;
                int minGS = gs != null && gs.min != null ? gs.min.Evaluate() : 0;
                int maxGS = gs != null && gs.max != null ? gs.max.Evaluate() : -1;

                int count;

                if (entity.horde == null)
                {
                    if (gs == null || gs.countIncPerGS == null)
                    {
                        if (maxCount > 0)
                            count = random.RandomRange(minCount, maxCount + 1);
                        else
                        {
                            Error("Cannot calculate count of entity/entitygroup {0} in group {1} because no gamestage or maximum count has been specified.", entity.name ?? entity.group, randomGroupEntityPool.HordeGroup.name);
                            count = 0;
                        }
                    }
                    else
                    {
                        float countIncPerGS = gs.countIncPerGS.Evaluate();

                        int toSpawn = minCount + (int)Math.Floor(countIncPerGS * (hordeGenerationData.PlayerGamestage - minGS));
                        int countDecGS = gs.countDecGS != null ? gs.countDecGS.Evaluate() : 0;

                        if (maxCount >= 0 && toSpawn > maxCount)
                            toSpawn = maxCount;

                        if (countDecGS > 0 && hordeGenerationData.PlayerGamestage > countDecGS)
                        {
                            float countDecPerPostGS;

                            if (gs.countDecPerPostGS != null)
                                countDecPerPostGS = gs.countDecPerPostGS.Evaluate();
                            else if (gs.countDecPerPostGS == null && gs.max != null)
                                countDecPerPostGS = toSpawn / (maxGS - countDecGS);
                            else
                            {
                                Error("[{0}] Unable to calculate entity decrease after GS {1} for entity {2} in group {3}.", this.GetType().FullName, countDecGS, entity.name ?? entity.group, randomGroupEntityPool.HordeGroup.name);
                                countDecPerPostGS = 0.0f;
                            }

                            int decGSSpawn = (int)Math.Floor(countDecPerPostGS * (hordeGenerationData.PlayerGamestage - countDecGS));

                            if (decGSSpawn > 0)
                                toSpawn -= decGSSpawn;
                        }

                        if (toSpawn < 0)
                            toSpawn = 0;

                        count = toSpawn;
                    }

                    if (!entitiesToSpawn.ContainsKey(entity))
                        entitiesToSpawn.Add(entity, 0);

                    entitiesToSpawn[entity] += count;

#if DEBUG
                    Log("[{0}] Spawning {1} of {2}", this.GetType().FullName, count, entity.name ?? entity.group);
#endif
                }
                else
                {
                    if (!HordesList.hordes.ContainsKey(entity.horde))
                    {
                        throw new NullReferenceException($"No such horde with type {entity.horde} exists.");
                    }

                    if (entity.group == null)
                    {
                        var hordesDict = HordesList.hordes[entity.horde].hordes;

                        var randomHordeGroup = hordesDict.Values.ToList().ElementAt(random.RandomRange(0, hordesDict.Count));
                        EvaluateEntitiesInPool(new HordeGroupEntityPool(randomHordeGroup, hordeGenerationData, this.CanHordeGroupBePicked), ref entitiesToSpawn, hordeGenerationData);
                    }
                    else
                    {
                        if (!HordesList.hordes[entity.horde].hordes.ContainsKey(entity.group))
                        {
                            throw new NullReferenceException($"Horde type {entity.group} does not exist in horde type {entity.horde}.");
                        }

                        var subGroup = HordesList.hordes[entity.horde].hordes[entity.group];
                        EvaluateEntitiesInPool(new HordeGroupEntityPool(subGroup, hordeGenerationData, this.CanHordeGroupBePicked), ref entitiesToSpawn, hordeGenerationData);
                    }
                }
            }
        }
    }
}
