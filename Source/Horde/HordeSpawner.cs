using System.Collections.Generic;

using ImprovedHordes.Horde.AI;

using UnityEngine;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde
{
    public abstract class HordeSpawner
    {
        private static int s_max_alive_per_horde_player;

        private static int MAX_ALIVE_PER_HORDE_PLAYER
        {
            get
            {
                return s_max_alive_per_horde_player;
            }
        }

        private readonly Dictionary<PlayerHordeGroup, SpawningHorde> hordesSpawning = new Dictionary<PlayerHordeGroup, SpawningHorde>();
        private readonly HordeGenerator hordeGenerator;

        private readonly ImprovedHordesManager manager;

        public HordeSpawner(ImprovedHordesManager manager, HordeGenerator hordeGenerator)
        {
            this.manager = manager;
            this.hordeGenerator = hordeGenerator;
        }

        public static void ReadSettings(Settings settings)
        {
            s_max_alive_per_horde_player = settings.GetInt("max_alive_per_horde_player");
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

            Vector2 opposite = new Vector2(commonPos.x, commonPos.z) - (centerStart - new Vector2(commonPos.x, commonPos.z));

            endPos = new Vector3(opposite.x, 0, opposite.y);
            
            Utils.GetSpawnableY(ref endPos);

            return true;
        }

        private void FindAllFarthestDirectionalSpawnsFromGroup(PlayerHordeGroup group, int count, Vector3 centerPos, out Vector2 centerStart, out Queue<Vector3> startPositions)
        {
            startPositions = new Queue<Vector3>(count);

            Vector3 farthestPlayerPosition = GetFarthestPlayerPosition(group, centerPos);
            Vector3 direction = farthestPlayerPosition - centerPos;
            direction.y = 0.0f;

            float theta = Mathf.Atan2(direction.z, direction.x) + Mathf.PI;
            float thetaRange = Mathf.PI / 2;

            float minThetaRange = theta - thetaRange;
            float maxThetaRange = theta + thetaRange;

            var maxDistance = 16 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance);

            for (int i = 0; i < count; i++)
            {
                float thetaRandomnessFactor = this.manager.Random.RandomRange(minThetaRange, maxThetaRange);

                float distance = maxDistance - Mathf.Sqrt(Mathf.Abs(this.manager.Random.RandomFloat) * 16);
                Vector3 newDirection = new Vector3(distance * Mathf.Cos(thetaRandomnessFactor), 0, distance * Mathf.Sin(thetaRandomnessFactor));

                Vector3 spawnPosition = farthestPlayerPosition + newDirection;
                Utils.GetSpawnableY(ref spawnPosition);

                startPositions.Enqueue(spawnPosition);
            }

            centerStart = new Vector2(farthestPlayerPosition.x + maxDistance * Mathf.Cos(theta), farthestPlayerPosition.z + maxDistance * Mathf.Sin(theta));
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
            Vector3 farthest = playerHordeGroup.members[0].position;

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

            return horde.entityIndex < horde.horde.entities.Count;
        }

        protected bool Spawn(PlayerHordeGroup group, SpawningHorde horde)
        {
            int entityId = horde.horde.entities[horde.entityIndex++];

            EntityAlive entity = EntityFactory.CreateEntity(entityId, horde.DetermineRandomSpawnPosition()) as EntityAlive;
            ImprovedHordesManager.Instance.World.SpawnEntityInWorld(entity);

            entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);

            this.SetAttributes(entity);
            this.OnSpawn(entity, group, horde);

            // returns true if spawned all entities to signal that spawning is complete
            // returns false if more will be spawned.
            return horde.entityIndex >= horde.horde.entities.Count;
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

        private List<EntityPlayer> GetNearbyPlayers(EntityPlayer player)
        {
            return GetNearbyPlayers(player.position);
        }

        private List<EntityPlayer> GetNearbyPlayers(Vector3 position)
        {
            List<EntityPlayer> players = new List<EntityPlayer>();

            foreach (var playerId in ImprovedHordesManager.Instance.Players)
            {
                EntityPlayer player = ImprovedHordesManager.Instance.World.GetEntity(playerId) as EntityPlayer;

                if (Vector3.Distance(position, player.position) > GetGroupDistance())
                    continue;

                players.Add(player);
            }

            return players;
        }

        public List<PlayerHordeGroup> GetAllHordeGroups()
        {
            List<int> grouped = new List<int>();
            List<PlayerHordeGroup> groups = new List<PlayerHordeGroup>();

            foreach (var playerId in ImprovedHordesManager.Instance.Players)
            {
                if (grouped.Contains(playerId))
                    continue;

                var player = ImprovedHordesManager.Instance.World.GetEntity(playerId) as EntityPlayer;

                var group = GetNearbyPlayers(player);
                group.Add(player); // Group includes surrounding players and player.

                for (int i = 0; i < group.Count; i++)
                {
                    var groupedPlayer = group[i];

                    if (grouped.Contains(groupedPlayer.entityId))
                    {
                        group.RemoveAt(i);
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

            public Vector3 DetermineRandomSpawnPosition()
            {
                Vector3 spawnPosition = Vector3.zero;

                if (spawnPositions.Count == 0)
                {
                    Warning("[Spawning Horde] More spawned enemies than calculated spawn positions. Did something go wrong?");
                }
                else
                    spawnPosition = spawnPositions.Dequeue();

                if (!ImprovedHordesManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(spawnPosition, 2, 20, 2, true, out Vector3 randomStartPos))
                {
                    // Failed to find a random spawn near position, so just assign default spawn position for horde.
                    randomStartPos = spawnPosition;
                }

                return randomStartPos;
            }
        }
    }
}