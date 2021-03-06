using System.Collections.Generic;
using System.Linq;

using ImprovedHordes.Horde.AI;

using UnityEngine;

using static ImprovedHordes.Utils.Logger;

using CustomModManager.API;

namespace ImprovedHordes.Horde
{
    public abstract class HordeSpawner
    {
        private static int s_max_alive_per_horde_player = 10;

        private static int MAX_ALIVE_PER_HORDE_PLAYER
        {
            get
            {
                return s_max_alive_per_horde_player;
            }
        }

        private readonly Dictionary<PlayerHordeGroup, SpawningHorde> hordesSpawning = new Dictionary<PlayerHordeGroup, SpawningHorde>();
        private readonly HordeGenerator hordeGenerator;

        protected readonly ImprovedHordesManager manager;

        public HordeSpawner(ImprovedHordesManager manager, HordeGenerator hordeGenerator)
        {
            this.manager = manager;
            this.hordeGenerator = hordeGenerator;
        }

        public static void ReadSettings(Settings settings)
        {
            s_max_alive_per_horde_player = settings.GetInt("max_alive_per_horde_player", 8);
        }

        public static void HookSettings(ModManagerAPI.ModSettings modSettings)
        {
            modSettings.Hook<int>("hordeGeneralMaxAlivePerHordePlayer", "IHxuiHordeGeneralMaxAlivePerHordePlayerModSetting", value => s_max_alive_per_horde_player = value, () => s_max_alive_per_horde_player, toStr => (toStr.ToString(), toStr > -1 ? toStr.ToString() + " Zombie" + (toStr != 1 ? "s" : "") : "Unlimited"), str =>
            {
                bool success = int.TryParse(str, out int val);
                return (val, success);
            }).SetTab("hordeGeneralSettingsTab");
        }

        public bool IsStillSpawningFor(PlayerHordeGroup playerHordeGroup)
        {
            return hordesSpawning.ContainsKey(playerHordeGroup);
        }

        protected virtual void SetAttributes(EntityAlive entity) 
        {
            if (entity is EntityEnemy enemy)
                enemy.IsHordeZombie = true;

            entity.bIsChunkObserver = true;
            entity.IsBloodMoon = false;
        }

        public void StopAllSpawning()
        {
            hordesSpawning.Clear();
        }

        protected abstract void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde);

        public void StartSpawningFor(List<PlayerHordeGroup> groups, bool feral)
        {
            foreach(var group in groups)
            {
                StartSpawningFor(group, feral);
            }
        }

        public void StartSpawningFor(PlayerHordeGroup group, bool feral)
        {
            if (hordesSpawning.ContainsKey(group))
                return;

            if(!this.hordeGenerator.GenerateHorde(group, feral, out Horde horde))
            {
                Log("No horde spawned for group {0}, as there were no matching horde groups.", group);
                return;
            }

            if (!GetSpawnPositions(group, horde.count, out Queue<Vector3> spawnPositions, out Vector3 targetPosition))
            {
                Error("Invalid horde spawn position for group: {0}", group.ToString());
                return;
            }

            SpawningHorde spawningHorde = new SpawningHorde(horde, spawnPositions, targetPosition);

            hordesSpawning.Add(group, spawningHorde);
            ImprovedHordesManager.Instance.HordeManager.RegisterHorde(horde);

            PreSpawn(group, spawningHorde);
        }

        private readonly List<PlayerHordeGroup> toRemove = new List<PlayerHordeGroup>();
        public void Update()
        {
            if (hordesSpawning.Count == 0)
                return;

            foreach (var playerHordeEntry in hordesSpawning)
            {
                var playerGroup = playerHordeEntry.Key;
                var horde = playerHordeEntry.Value;

                if (CanSpawn(horde))
                {
                    if (Spawn(playerGroup, horde))
                    {
                        toRemove.Add(playerGroup);
                        PostSpawn(playerGroup, horde);
                    }
                }
            }

            foreach (var playerHordeGroup in toRemove)
            {
                hordesSpawning.Remove(playerHordeGroup);
            }

            if(toRemove.Count > 0)
                toRemove.Clear();
        }

        protected virtual void PreSpawn(PlayerHordeGroup playerHordeGroup, SpawningHorde horde) { }
        
        protected virtual void PostSpawn(PlayerHordeGroup playerHordeGroup, SpawningHorde horde) { }

        public virtual bool GetSpawnPositions(PlayerHordeGroup playerHordeGroup, int count, out Queue<Vector3> spawnPositions, out Vector3 targetPosition)
        {
            return CalculateHordePositions(playerHordeGroup, count, out spawnPositions, out targetPosition);
        }
        protected Vector3 GetRandomNearbyPosition(Vector3 target, int radius)
        {
            Vector2 randomInCircle = this.manager.Random.RandomInsideUnitCircle * radius;

            Vector3 pos = new Vector3(randomInCircle.x, 0, randomInCircle.y) + target;
            Utils.GetSpawnableY(ref pos);

            return pos;
        }

        public bool CalculateHordePositions(PlayerHordeGroup playerHordeGroup, int count, out Queue<Vector3> startPositions, out Vector3 endPos)
        {
            Vector3 commonPos = playerHordeGroup.CalculateAverageGroupPosition(true);
            FindAllFarthestDirectionalSpawnsFromGroup(playerHordeGroup, count, commonPos, out Vector2 centerStart, out startPositions);

            Vector2 direction = centerStart - commonPos.ToXZ();
            float theta = Mathf.Atan2(direction.y, direction.x);
            float mag = direction.magnitude;

            float minThetaRange = theta + Mathf.PI / 2;
            float maxThetaRange = 2 * Mathf.PI + theta - Mathf.PI / 2;

            float thetaRandomnessFactor = this.manager.Random.RandomRange(minThetaRange, maxThetaRange);
            
            endPos = commonPos + new Vector3(mag * Mathf.Cos(thetaRandomnessFactor), 0, mag * Mathf.Sin(thetaRandomnessFactor));
            
            Utils.GetSpawnableY(ref endPos);

            return true;
        }

        private void FindAllFarthestDirectionalSpawnsFromGroup(PlayerHordeGroup group, int count, Vector3 centerPos, out Vector2 centerStart, out Queue<Vector3> startPositions)
        {
            startPositions = new Queue<Vector3>(count);

            Vector3 farthestPlayerPosition = GetFarthestPlayerPosition(group, centerPos);
            Vector3 direction = farthestPlayerPosition - centerPos;
            direction.y = 0.0f;

            float theta = Mathf.Atan2(direction.z, direction.x);
            float thetaRange = group.members.Count > 1 ? Mathf.PI / 2 : Mathf.PI;

            float minThetaRange = theta - thetaRange;
            float maxThetaRange = theta + thetaRange;

            float thetaRandomnessFactor = this.manager.Random.RandomRange(minThetaRange, maxThetaRange); // Center of horde.

            for (int i = 0; i < count; i++)
            {
                float distance = GetMaxSpawnDistance() - Mathf.Sqrt(Mathf.Abs(this.manager.Random.RandomFloat) * 16f);
                Vector3 newDirection = new Vector3(distance * Mathf.Cos(thetaRandomnessFactor), 0, distance * Mathf.Sin(thetaRandomnessFactor));
                
                Vector2 relativeToCenter = this.manager.Random.RandomOnUnitCircle;
                Vector3 spawnPosition = newDirection + new Vector3(relativeToCenter.x, 0, relativeToCenter.y);

                startPositions.Enqueue(spawnPosition);
            }

            centerStart = new Vector2(farthestPlayerPosition.x + GetMaxSpawnDistance() * Mathf.Cos(thetaRandomnessFactor), farthestPlayerPosition.z + GetMaxSpawnDistance() * Mathf.Sin(thetaRandomnessFactor));
        }

        private float GetMaxSpawnDistance()
        {
            return 6 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance); // Too far of a spawn distance results in Horde AI not working in unloaded chunks.
        }

        public bool GetSpawnableCircleFromPos(Vector3 playerPos, float radius, out Vector3 spawnablePos)
        {
            Vector2 startCircle = this.manager.Random.RandomOnUnitCircle;

            float x = (startCircle.x * radius) + playerPos.x;
            float z = (startCircle.y * radius) + playerPos.z;

            Vector3 circleFromPlayer = new Vector3(x, 0, z);
            Utils.GetSpawnableY(ref circleFromPlayer);

            spawnablePos = circleFromPlayer;
            return true;
        }

        private Vector3 GetFarthestPlayerPosition(PlayerHordeGroup playerHordeGroup, Vector3 center)
        {
            float distance = 0.0f;
            Vector3 farthest = playerHordeGroup.members.First().position;

            foreach(var player in playerHordeGroup.members)
            {
                float newDistance = Vector3.Distance(player.position, center);

                if(newDistance > distance)
                {
                    distance = newDistance;
                    farthest = player.position;
                }
            }

            return farthest;
        }

        protected bool CanSpawn(SpawningHorde horde)
        {
            int playerCount = horde.horde.playerGroup.members.Count;

            if (MAX_ALIVE_PER_HORDE_PLAYER > -1 && horde.aiHorde.GetStat(EHordeAIStats.TOTAL_ALIVE) >= MAX_ALIVE_PER_HORDE_PLAYER * playerCount)
                return false;

            if (GameStats.GetInt(EnumGameStats.EnemyCount) >= GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedZombies))
                return false;

            return horde.entityIndex < horde.horde.entityIds.Count;
        }

        protected bool Spawn(PlayerHordeGroup group, SpawningHorde horde)
        {
            int entityId = horde.horde.entityIds[horde.entityIndex++];

            if (!horde.DetermineRandomSpawnPosition(out Vector3 spawnPosition, GetFarthestPlayerPosition(group, group.CalculateAverageGroupPosition(true))))
                return true; // End spawning since we don't have any more spawns left and will loop until crash occurs. This scenario should never realistically occur.

            EntityAlive entity = EntityFactory.CreateEntity(entityId, spawnPosition) as EntityAlive;
            ImprovedHordesManager.Instance.World.SpawnEntityInWorld(entity);

            entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);

            this.SetAttributes(entity);
            this.OnSpawn(entity, group, horde);

#if DEBUG
            entity.AddNavObject("ih_horde_zombie_debug", "");
#endif
            // returns true if spawned all entities to signal that spawning is complete
            // returns false if more will be spawned.
            return horde.entityIndex >= horde.horde.entityIds.Count;
        }

        public abstract int GetGroupDistance();

        public PlayerHordeGroup GetHordeGroupNearLocation(Vector3 position)
        {
            return new PlayerHordeGroup(GetNearbyPlayers(position));
        }

        public PlayerHordeGroup GetHordeGroupNearPlayer(EntityPlayer player)
        {
            return GetHordeGroupNearLocation(player.position);
        }

        private HashSet<EntityPlayer> GetNearbyPlayers(EntityPlayer player)
        {
            return GetNearbyPlayers(player.position);
        }

        private HashSet<EntityPlayer> GetNearbyPlayers(Vector3 position)
        {
            HashSet<EntityPlayer> players = new HashSet<EntityPlayer>();

            foreach (var playerId in ImprovedHordesManager.Instance.PlayerManager.GetPlayers())
            {
                EntityPlayer player = ImprovedHordesManager.Instance.World.GetEntity(playerId) as EntityPlayer;

                if (player == null || Vector3.Distance(position, player.position) > GetGroupDistance())
                    continue;

                players.Add(player);
            }

            return players;
        }

        public List<PlayerHordeGroup> GetAllHordeGroups()
        {
            List<int> grouped = new List<int>();
            List<PlayerHordeGroup> groups = new List<PlayerHordeGroup>();

            foreach (var playerId in ImprovedHordesManager.Instance.PlayerManager.GetPlayers())
            {
                if (grouped.Contains(playerId))
                    continue;

                var player = ImprovedHordesManager.Instance.World.GetEntity(playerId) as EntityPlayer;

                if (player == null)
                    continue;

                var group = GetNearbyPlayers(player);
                group.Add(player); // Group includes surrounding players and player.

                HashSet<EntityPlayer> groupCopy = new HashSet<EntityPlayer>(group);

                foreach(var groupedPlayer in groupCopy)
                {
                    if (grouped.Contains(groupedPlayer.entityId))
                    {
                        group.Remove(groupedPlayer);
                    }
                    else
                    {
                        grouped.Add(groupedPlayer.entityId);
                    }
                }

                if (group.Count > 0)
                {
                    PlayerHordeGroup hordeGroup = new PlayerHordeGroup(group);
                    groups.Add(hordeGroup);
                }
            }

            return groups;
        }

        protected class SpawningHorde
        {
            public Horde horde;
            public HordeAIHorde aiHorde;
            public Queue<Vector3> spawnPositions;
            public Vector3 targetPosition;
            public int entityIndex = 0;

            public SpawningHorde(Horde horde, Queue<Vector3> spawnPositions, Vector3 targetPosition)
            {
                this.horde = horde;
                this.aiHorde = ImprovedHordesManager.Instance.AIManager.GetAsAIHorde(horde);
                this.spawnPositions = spawnPositions;
                this.targetPosition = targetPosition;
            }

            public bool DetermineRandomSpawnPosition(out Vector3 spawnPosition, Vector3 farthestPlayerLocation)
            {
                if (spawnPositions.Count == 0)
                {
                    Warning("[Spawning Horde] More spawned enemies than calculated spawn positions. Did something go wrong?");

                    spawnPosition = Vector3.zero;
                    return false;
                }
                else
                    spawnPosition = spawnPositions.Dequeue();

                spawnPosition += farthestPlayerLocation;
                Utils.GetSpawnableY(ref spawnPosition);

                if (ImprovedHordesManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(spawnPosition, 2, 20, 2, true, out Vector3 randomStartPos))
                {
                    spawnPosition = randomStartPos;
                }

                return true;
            }
        }
    }
}