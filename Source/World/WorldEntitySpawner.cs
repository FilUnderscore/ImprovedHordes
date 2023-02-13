using HarmonyLib;
using System;
using ImprovedHordes.Horde;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace ImprovedHordes.World
{
    public sealed class WorldEntitySpawner : IManager
    {
        private readonly ImprovedHordesManager manager;

        private readonly Dictionary<Vector3i, ChunkAreaWorldEntitySpawnData.SaveData> worldEntitySpawnDataSavedEntityCounts = new Dictionary<Vector3i, ChunkAreaWorldEntitySpawnData.SaveData>();
        private readonly Dictionary<ChunkAreaBiomeSpawnData, ChunkAreaWorldEntitySpawnData> worldEntitySpawnData = new Dictionary<ChunkAreaBiomeSpawnData, ChunkAreaWorldEntitySpawnData>();

        private class ChunkAreaWorldEntitySpawnData
        {
            public class SaveData
            {
                public readonly Dictionary<WorldEntityType, int> entitiesSpawned;
                public readonly Dictionary<WorldEntityType, int> entitiesSpawnedMax;
                public readonly Dictionary<WorldEntityType, ulong> entitiesRespawnTime;

                private SaveData(Dictionary<WorldEntityType, int> entitiesSpawned, Dictionary<WorldEntityType, int> entitiesSpawnedMax, Dictionary<WorldEntityType, ulong> entitiesRespawnTime)
                {
                    this.entitiesSpawned = entitiesSpawned;
                    this.entitiesSpawnedMax = entitiesSpawnedMax;
                    this.entitiesRespawnTime = entitiesRespawnTime;
                }

                public SaveData()
                {
                    this.entitiesSpawned = new Dictionary<WorldEntityType, int>();
                    this.entitiesSpawnedMax = new Dictionary<WorldEntityType, int>();
                    this.entitiesRespawnTime = new Dictionary<WorldEntityType, ulong>();

                    foreach (WorldEntityType worldEntityType in Enum.GetValues(typeof(WorldEntityType)))
                    {
                        entitiesSpawned.Add(worldEntityType, 0);
                        entitiesSpawnedMax.Add(worldEntityType, 1);
                        this.entitiesRespawnTime.Add(worldEntityType, 0UL);
                    }
                }

                public static SaveData Deserialize(BinaryReader reader)
                {
                    Dictionary<WorldEntityType, int> entitiesSpawned = new Dictionary<WorldEntityType, int>();
                    Dictionary<WorldEntityType, int> entitiesSpawnedMax = new Dictionary<WorldEntityType, int>();
                    Dictionary<WorldEntityType, ulong> entitiesRespawnTime = new Dictionary<WorldEntityType, ulong>();

                    int typeCount = reader.ReadInt32();
                    for (int j = 0; j < typeCount; j++)
                    {
                        WorldEntityType type = (WorldEntityType)reader.ReadInt32();
                        int entityCount = reader.ReadInt32();
                        int entityCountMax = reader.ReadInt32();
                        ulong entityRespawnTime = reader.ReadUInt64();

                        entitiesSpawned[type] = entityCount;
                        entitiesSpawnedMax[type] = entityCountMax;
                        entitiesRespawnTime[type] = entityRespawnTime;
                    }

                    return new SaveData(entitiesSpawned, entitiesSpawnedMax, entitiesRespawnTime);
                }

                public void Serialize(BinaryWriter writer)
                {
                    writer.Write(this.entitiesSpawned.Count);
                    foreach (WorldEntityType worldEntityType in Enum.GetValues(typeof(WorldEntityType)))
                    {
                        writer.Write((int)worldEntityType);
                        writer.Write(entitiesSpawned[worldEntityType]);
                        writer.Write(entitiesSpawnedMax[worldEntityType]);
                        writer.Write(entitiesRespawnTime[worldEntityType]);
                    }
                }
            }

            public static int MAX_ANIMALS_PER_AREA = 0, MAX_ENEMIES_PER_AREA = 0;

            private readonly ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData;
            private readonly SaveData saveData;

            public ChunkAreaWorldEntitySpawnData(ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData)
            {
                this.chunkAreaBiomeSpawnData = chunkAreaBiomeSpawnData;

                if (!ImprovedHordesManager.Instance.WorldEntitySpawner.worldEntitySpawnDataSavedEntityCounts.TryGetValue(this.chunkAreaBiomeSpawnData.chunk.ChunkPos, out this.saveData))
                    this.saveData = new SaveData();
            }

            public void Serialize(BinaryWriter writer)
            {
                this.saveData.Serialize(writer);
            }

            public void IncreaseEntityCount(WorldEntityType worldEntityType)
            {
                this.saveData.entitiesSpawned[worldEntityType]++;
            }

            public void DecreaseEntityCount(WorldEntityType worldEntityType)
            {
                int entityCount = this.saveData.entitiesSpawned[worldEntityType]--;
                long remainingRespawnTime = (long)(this.saveData.entitiesRespawnTime[worldEntityType] - ImprovedHordesManager.Instance.World.worldTime);
                remainingRespawnTime = Math.Max(0, remainingRespawnTime);

                this.saveData.entitiesRespawnTime[worldEntityType] = ImprovedHordesManager.Instance.World.worldTime + (ulong)remainingRespawnTime + (ulong)(entityCount * 1000);
            }

            public int GetEntityCount(WorldEntityType worldEntityType)
            {
                return this.saveData.entitiesSpawned[worldEntityType];
            }

            public int GetMaxEntityCount(WorldEntityType worldEntityType)
            {
                return this.saveData.entitiesSpawnedMax[worldEntityType];
            }

            public bool IsSpawnNeeded()
            {
                return IsSpawnNeeded(WorldEntityType.Animal) || IsSpawnNeeded(WorldEntityType.Enemy);
            }

            public bool IsSpawnNeeded(WorldEntityType worldEntityType)
            {
                if (!this.saveData.entitiesRespawnTime.TryGetValue(worldEntityType, out ulong respawnTime) || respawnTime > ImprovedHordesManager.Instance.World.worldTime)
                    return false;

                return GetEntityCount(worldEntityType) < GetMaxEntityCount(worldEntityType);
            }

            public PlayerHordeGroup GetPlayerHordeGroup()
            {
                HashSet<EntityPlayer> playersNearby = new HashSet<EntityPlayer>();
                foreach (EntityPlayer player in ImprovedHordesManager.Instance.World.GetPlayers())
                {
                    if (player.Spawned && new Rect(player.position.x - 40f, player.position.z - 40f, 80f, 80f).Overlaps(chunkAreaBiomeSpawnData.area))
                    {
                        playersNearby.Add(player);
                    }
                }

                if (playersNearby.Count == 0)
                    return null;

                PlayerHordeGroup playerHordeGroup = new PlayerHordeGroup(playersNearby);

                int gs = playerHordeGroup.GetGroupGamestage();
                int playerCount = playerHordeGroup.members.Count;

                // Recalculate max area entity counts.
                int maxAnimalCount = DetermineMaxEntityCount(gs, playerCount, WorldEntityType.Animal);
                int maxEnemyCount = DetermineMaxEntityCount(gs, playerCount, WorldEntityType.Enemy);

                this.saveData.entitiesSpawnedMax[WorldEntityType.Animal] = maxAnimalCount;
                this.saveData.entitiesSpawnedMax[WorldEntityType.Enemy] = maxEnemyCount;

                return playerHordeGroup;
            }

            private int DetermineMaxEntityCount(int gs, int playerCount, WorldEntityType worldEntityType)
            {
                int count = (worldEntityType == WorldEntityType.Animal ? MAX_ANIMALS_PER_AREA : MAX_ENEMIES_PER_AREA) * playerCount;
                //Log.Out($"Type {worldEntityType} count {count} gs {gs} mined {Math.Min(count, (int)Mathf.Pow(count * gs, 0.25f))}");

                return Math.Min(count, (int)Mathf.Pow(count * gs, 0.25f));
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(this.worldEntitySpawnData.Count);

            foreach(KeyValuePair<ChunkAreaBiomeSpawnData, ChunkAreaWorldEntitySpawnData> entry in this.worldEntitySpawnData)
            {
                Vector3i chunkPos = entry.Key.chunk.ChunkPos;
                writer.Write(chunkPos.x);
                writer.Write(chunkPos.y);
                writer.Write(chunkPos.z);

                entry.Value.Serialize(writer);
            }
        }

        public void Load(BinaryReader reader) 
        {
            int count = reader.ReadInt32();

            for(int i = 0; i < count; i++)
            {
                Vector3i chunkPos = new Vector3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

                if (!this.worldEntitySpawnDataSavedEntityCounts.ContainsKey(chunkPos))
                    this.worldEntitySpawnDataSavedEntityCounts.Add(chunkPos, ChunkAreaWorldEntitySpawnData.SaveData.Deserialize(reader));
                else
                    this.worldEntitySpawnDataSavedEntityCounts[chunkPos] = ChunkAreaWorldEntitySpawnData.SaveData.Deserialize(reader);
            }
        }

        private void FillIfNotFound(ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData)
        {
            if (worldEntitySpawnData.ContainsKey(chunkAreaBiomeSpawnData))
                return;

            worldEntitySpawnData.Add(chunkAreaBiomeSpawnData, new ChunkAreaWorldEntitySpawnData(chunkAreaBiomeSpawnData));
        }

        private ChunkAreaWorldEntitySpawnData GetWorldEntitySpawnData(ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData)
        {
            return worldEntitySpawnData[chunkAreaBiomeSpawnData];
        }

        public WorldEntitySpawner(ImprovedHordesManager manager)
        {
            this.manager = manager;
        }

        public void Init()
        {
            this.manager.World.EntityUnloadedDelegates += OnEntityUnloaded;

            int activeChunkCountPerPlayer = (GameStats.GetInt(EnumGameStats.AllowedViewDistance) * GameStats.GetInt(EnumGameStats.AllowedViewDistance)) / 5;
            ChunkAreaWorldEntitySpawnData.MAX_ANIMALS_PER_AREA = (GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedAnimals) / activeChunkCountPerPlayer) / 2;
            ChunkAreaWorldEntitySpawnData.MAX_ENEMIES_PER_AREA = GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedZombies) / activeChunkCountPerPlayer;
            
            Log.Out($"Active chunk count per player: {activeChunkCountPerPlayer} MAX ANIMALS PER AREA {ChunkAreaWorldEntitySpawnData.MAX_ANIMALS_PER_AREA} MAX ENEMIES {ChunkAreaWorldEntitySpawnData.MAX_ENEMIES_PER_AREA}");
        }

        public void Shutdown()
        {
            this.manager.World.EntityUnloadedDelegates -= OnEntityUnloaded;

            this.worldEntitySpawnData.Clear();
            this.worldEntitySpawnDataSavedEntityCounts.Clear();
        }

        private void OnEntityUnloaded(Entity entity, EnumRemoveEntityReason _reason)
        {
            if (_reason == EnumRemoveEntityReason.Undef || _reason == EnumRemoveEntityReason.Unloaded || entity.GetSpawnerSource() != EnumSpawnerSource.Biome || entity is EntityZombie && this.manager.World.worldTime > ((EntityZombie)entity).timeToDie)
                return;
            Chunk chunkSync = (Chunk)this.manager.World.GetChunkSync(entity.GetSpawnerSourceChunkKey());
            if (chunkSync == null)
                return;
            ChunkAreaBiomeSpawnData chunkBiomeSpawnData = chunkSync.GetChunkBiomeSpawnData();
            if (chunkBiomeSpawnData == null)
                return;

            FillIfNotFound(chunkBiomeSpawnData);
            ChunkAreaWorldEntitySpawnData chunkAreaWorldEntitySpawnData = GetWorldEntitySpawnData(chunkBiomeSpawnData);
            switch (_reason)
            {
                case EnumRemoveEntityReason.Killed:
                case EnumRemoveEntityReason.Despawned:
                    if (!(entity is EntityAlive))
                        break;

                    chunkAreaWorldEntitySpawnData.DecreaseEntityCount(WorldEntityDefinition.DetermineWorldEntityType(entity as EntityAlive));
                    break;
            }
        }

        private void Update(bool canSpawnEnemy, ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData)
        {
            if (chunkAreaBiomeSpawnData == null)
                return;

            if ((canSpawnEnemy && GameStats.GetInt(EnumGameStats.EnemyCount) >= GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedZombies)) ||
                this.manager.World.aiDirector.BloodMoonComponent.BloodMoonActive)
                canSpawnEnemy = false;

            if (!canSpawnEnemy && GameStats.GetInt(EnumGameStats.AnimalCount) >= GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedAnimals))
                return;

            ChunkAreaWorldEntitySpawnData chunkAreaWorldEntitySpawnData = GetWorldEntitySpawnData(chunkAreaBiomeSpawnData);
            bool animalSpawnNeeded = chunkAreaWorldEntitySpawnData.IsSpawnNeeded(WorldEntityType.Animal);
            bool enemySpawnNeeded = chunkAreaWorldEntitySpawnData.IsSpawnNeeded(WorldEntityType.Enemy);

            WorldEntityType type;
            if(canSpawnEnemy && enemySpawnNeeded)
            {
                type = WorldEntityType.Enemy;
            }
            else if(animalSpawnNeeded)
            {
                type = WorldEntityType.Animal;
            }
            else
            {
                return;
            }

            if (!this.manager.World.GetRandomSpawnPositionInAreaMinMaxToPlayers(chunkAreaBiomeSpawnData.area, 20, -1, true, out Vector3 spawnPosition))
                return;

            PlayerHordeGroup playerHordeGroup = chunkAreaWorldEntitySpawnData.GetPlayerHordeGroup();
            if (playerHordeGroup == null)
                return;

            if (!WorldEntityGenerator.GenerateEntity(type, playerHordeGroup.GetGroupGamestage(), new Vector3i(spawnPosition), out int? entityClassId))
                return;

            Entity entity = EntityFactory.CreateEntity(entityClassId.Value, spawnPosition);
            entity.SetSpawnerSource(EnumSpawnerSource.Biome, chunkAreaBiomeSpawnData.chunk.Key, "");
            this.manager.World.SpawnEntityInWorld(entity);
            this.GetWorldEntitySpawnData(chunkAreaBiomeSpawnData).IncreaseEntityCount(type);
        }

        [HarmonyPatch(typeof(SpawnManagerBiomes), "SpawnUpdate")]
        class SpawnManagerBiomes_SpawnUpdate_Patch
        {
            static bool Prefix(bool _isSpawnEnemy, ChunkAreaBiomeSpawnData _chunkBiomeSpawnData)
            {
                if (!ImprovedHordesMod.IsHost())
                    return true;

                ImprovedHordesManager.Instance.WorldEntitySpawner.Update(_isSpawnEnemy, _chunkBiomeSpawnData);

                return false;
            }
        }

        [HarmonyPatch(typeof(ChunkAreaBiomeSpawnData), "IsSpawnNeeded")]
        class ChunkAreaBiomeSpawnData_IsSpawnNeeded_Patch
        {
            static bool Prefix(ChunkAreaBiomeSpawnData __instance, ref bool __result)
            {
                if (!ImprovedHordesMod.IsHost())
                    return true;

                ImprovedHordesManager.Instance.WorldEntitySpawner.FillIfNotFound(__instance);
                ChunkAreaWorldEntitySpawnData chunkAreaWorldEntitySpawnData = ImprovedHordesManager.Instance.WorldEntitySpawner.GetWorldEntitySpawnData(__instance);

                __result = chunkAreaWorldEntitySpawnData.IsSpawnNeeded();

                return false;
            }
        }

        [HarmonyPatch(typeof(SpawnManagerBiomes), "OnEntityUnloaded")]
        class SpawnManagerBiomes_OnEntityUnloaded_Patch
        {
            static bool Prefix()
            {
                return !ImprovedHordesMod.IsHost();
            }
        }
    }
}
