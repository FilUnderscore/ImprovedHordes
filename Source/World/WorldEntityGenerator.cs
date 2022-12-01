using ImprovedHordes.Horde;
using ImprovedHordes.Horde.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.World
{
    public static class WorldEntityGenerator
    {
        public static bool GenerateEntity(WorldEntityType worldEntityType, PlayerHordeGroup playerHordeGroup, out int? entityClassId)
        {
            return GenerateEntity(worldEntityType, playerHordeGroup.GetGroupGamestage(), new Vector3i(playerHordeGroup.CalculateAverageGroupPosition(true)), out entityClassId);
        }

        public static bool GenerateEntity(WorldEntityType worldEntityType, int gs, Vector3i position, out int? entityClassId)
        {
            List<WorldEntityDefinition> defs = new List<WorldEntityDefinition>(WorldEntitiesList.WorldEntities[worldEntityType]);

            BiomeDefinition biomeDef = ImprovedHordesManager.Instance.World.GetBiome(position.x, position.z);

            if (biomeDef == null)
            {
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        biomeDef = ImprovedHordesManager.Instance.World.GetBiome(position.x + i, position.z + j);

                        if (biomeDef != null)
                            break;
                    }

                    if (biomeDef != null)
                        break;
                }

                if (biomeDef == null)
                {
                    defs.RemoveAll(def => def.entities.Count(entity => entity.biomes == null) == 0);

                    if(defs.Count == 0)
                    {
                        entityClassId = null;
                        return false;
                    }
                }
            }

            string biomeAtPosition = biomeDef != null ? biomeDef.m_sBiomeName : "";
            
            IChunk chunk = GameManager.Instance.World.GetChunkSync(Chunk.ToAreaMasterChunkPos(new Vector3i(position.x, position.y, position.z)));
            ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData = chunk != null ? ((Chunk)chunk).GetChunkBiomeSpawnData() : null;

            if (chunkAreaBiomeSpawnData != null)
                Utils.CheckPOITags(chunkAreaBiomeSpawnData);

            GameRandom random = ImprovedHordesManager.Instance.Random;
            defs.RemoveAll(def => !CanWorldEntityBePicked(gs, def, biomeAtPosition, chunkAreaBiomeSpawnData, random));

            if(defs.Count == 0)
            {
                entityClassId = null;
                return false;
            }

            WorldEntityDefinition randomWorldEntityDef = defs[random.RandomRange(defs.Count)];
            List<WorldEntityDefinition.Entity> entities = PurgeUnmetConditions(randomWorldEntityDef, gs, biomeAtPosition, chunkAreaBiomeSpawnData);

            if(entities.Count == 0)
            {
                entityClassId = null;
                return false;
            }

            entityClassId = RandomWorldEntity(entities, random, out WorldEntityDefinition.Entity selectedEntity);

            return true;
        }

        private static bool CanWorldEntityEntryBeSelected(WorldEntityDefinition def, WorldEntityDefinition.Entity entity, int gamestage, string biomeAtPosition, bool isDay, ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData)
        {
            if (entity.biomes != null)
            {
                // Biome specific spawns.
                HashSet<string> biomes = entity.biomes.Evaluate();

                if (!biomes.Contains(biomeAtPosition))
                {
                    return false;
                }
            }

            if (entity.tags != null)
            {
                POITags tags = entity.tags.Evaluate();

                if (chunkAreaBiomeSpawnData == null ||
                    !chunkAreaBiomeSpawnData.checkedPOITags ||
                    (!chunkAreaBiomeSpawnData.poiTags.IsEmpty && !chunkAreaBiomeSpawnData.poiTags.Test_AnySet(tags)))
                    return false;
            }

            if (entity.chance != null && entity.chance.Evaluate() < ImprovedHordesManager.Instance.Random.RandomFloat)
                return false;

            if (entity.gs != null)
            {
                GS gs = entity.gs;

                if (gs.min != null && gamestage < gs.min.Evaluate())
                    return false;

                if (gs.max != null && gamestage >= gs.max.Evaluate())
                    return false;
            }

            return true;
        }

        private static List<WorldEntityDefinition.Entity> PurgeUnmetConditions(WorldEntityDefinition def, int gamestage, string biomeAtPosition, ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData)
        {
            List<WorldEntityDefinition.Entity> entities = new List<WorldEntityDefinition.Entity>();
            bool isDay = ImprovedHordesManager.Instance.World.IsDaytime();

            foreach(var entity in def.entities)
            {
                if(CanWorldEntityEntryBeSelected(def, entity, gamestage, biomeAtPosition, isDay, chunkAreaBiomeSpawnData))
                    entities.Add(entity);
            }

            return entities;
        }

        private static bool CanWorldEntityBePicked(int gamestage, WorldEntityDefinition def, string biomeAtPosition, ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData, GameRandom random)
        {
            if(def.chance != null && def.chance.Evaluate() < random.RandomFloat)
            {
                return false;
            }

            bool isDay = ImprovedHordesManager.Instance.World.IsDaytime();

            int entitiesThatMatchGS = 0;
            foreach(var entity in def.entities)
            {
               if(CanWorldEntityEntryBeSelected(def, entity, gamestage, biomeAtPosition, isDay, chunkAreaBiomeSpawnData))
                    entitiesThatMatchGS++;
            }

            if (entitiesThatMatchGS == 0)
                return false;

            return true;
        }

        private static int RandomWorldEntity(List<WorldEntityDefinition.Entity> entities, GameRandom random, out WorldEntityDefinition.Entity entity)
        {
            int index = random.RandomRange(entities.Count);
            entity = entities[index];

            if(entity.EntityGroup != null)
            {
                int lastEntityId = -1;
                return EntityGroups.GetRandomFromGroup(entity.EntityGroup, ref lastEntityId, random);
            }
            else if(entity.EntityName != null)
            {
                return EntityClass.FromString(entity.EntityName);
            }

            throw new InvalidOperationException("[Improved Hordes] Defined World Entity does not have a name/group!");
        }
    }
}