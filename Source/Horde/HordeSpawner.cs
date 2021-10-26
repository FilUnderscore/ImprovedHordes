using System;
using System.Collections.Generic;

using UnityEngine;

using static ImprovedHordes.Utils.Logger;
using static ImprovedHordes.Utils.Math;

namespace ImprovedHordes.Horde
{
    public abstract class HordeSpawner
    {
        private readonly Dictionary<PlayerHordeGroup, SpawningHorde> hordesSpawning = new Dictionary<PlayerHordeGroup, SpawningHorde>();
        private readonly HordeGenerator hordeGenerator;

        private readonly HordeManager manager;

        public HordeSpawner(HordeManager manager, HordeGenerator hordeGenerator)
        {
            this.manager = manager;
            this.hordeGenerator = hordeGenerator;
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

        public void StartSpawningFor(EntityPlayer player, bool feral)
        {
            StartSpawningFor(GetHordeGroupNearLocation(player.position), feral);
        }

        public void StartSpawningFor(PlayerHordeGroup group, bool feral)
        {
            if (hordesSpawning.ContainsKey(group))
                return;

            Horde horde = this.hordeGenerator.GenerateHorde(group, feral);

            if (!GetSpawnPosition(group, out Vector3 spawnPosition, out Vector3 targetPosition))
            {
                Error("Invalid horde spawn position for group: {0}", group.ToString());
                return;
            }

            SpawningHorde spawningHorde = new SpawningHorde(horde, spawnPosition, targetPosition);

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

        public virtual bool GetSpawnPosition(PlayerHordeGroup playerHordeGroup, out Vector3 spawnPosition, out Vector3 targetPosition)
        {
            var averageGroupPosition = CalculateAverageGroupPosition(playerHordeGroup);

            return CalculateWanderingHordePositions(averageGroupPosition, out spawnPosition, out targetPosition);
        }
        protected Vector3 GetRandomNearbyPosition(Vector3 target, float radius)
        {
            Vector2 random = this.manager.Random.RandomOnUnitCircle;

            float x = target.x + random.x * radius;
            float z = target.z + random.y * radius;

            return new Vector3(x, target.y, z);
        }

        public bool CalculateWanderingHordePositions(Vector3 commonPos, out Vector3 startPos, out Vector3 endPos)
        {
            var random = this.manager.Random;

            var maxViewDistance = GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance);
            var maxViewDistanceRadius = maxViewDistance * 12; // prevent smaller maximum than minimum.
            var radius = random.RandomRange(90, maxViewDistanceRadius <= 90 ? 120 : maxViewDistanceRadius); // TODO: Make XML setting.

            if (!GetSpawnableCircleFromPos(commonPos, radius, out startPos))
            {
                endPos = Vector3.zero;
                return false;
            }

            this.manager.World.GetRandomSpawnPositionMinMaxToPosition(commonPos, 20, 40, 20, true, out Vector3 randomPos);

            var intersections = FindLineCircleIntersections(randomPos.x, randomPos.z, radius, startPos, commonPos, out _, out Vector2 intEndPos);

            endPos = new Vector3(intEndPos.x, 0, intEndPos.y);
            var result = Utils.GetSpawnableY(ref endPos);

            if (!result)
            {
                return CalculateWanderingHordePositions(commonPos, out startPos, out endPos);
            }

            if (intersections < 2)
            {
                Warning("[Spawner] Only 1 intersection was found.");

                return false;
            }

            return true;
        }

        public bool GetSpawnableCircleFromPos(Vector3 playerPos, float radius, out Vector3 spawnablePos, int attempt = 0)
        {
            Vector2 startCircle = this.manager.Random.RandomOnUnitCircle;

            float x = (startCircle.x * radius) + playerPos.x;
            float z = (startCircle.y * radius) + playerPos.z;

            Vector3 circleFromPlayer = new Vector3(x, 0, z);
            bool result = Utils.GetSpawnableY(ref circleFromPlayer);

            if (!result)
            {
                if (attempt++ < 10)
                    return GetSpawnableCircleFromPos(playerPos, radius, out spawnablePos, attempt);
                else
                {
                    if (this.manager.World.GetRandomSpawnPositionMinMaxToPosition(playerPos, 20, (int)radius, 20, true, out Vector3 alt))
                    {
                        spawnablePos = alt;
                        return true;
                    }

                    spawnablePos = Vector3.zero;
                    return false;
                }
            }

            spawnablePos = circleFromPlayer;
            return true;
        }

        protected Vector3 CalculateAverageGroupPosition(PlayerHordeGroup playerHordeGroup)
        {
            List<EntityPlayer> players = playerHordeGroup.members;

            Vector3 avg = Vector3.zero;

            foreach (var player in players)
            {
                avg += player.position;
            }

            avg /= players.Count;

            if (!Utils.GetSpawnableY(ref avg))
            {
                // Testing this.
                Error("[Spawner] Failed to get spawnable Y for group average position.");
            }

            return avg;
        }

        protected bool CanSpawn(SpawningHorde horde)
        {
            // TODO Optional Spawning Limit
            if (horde.entityIndex < horde.horde.entities.Count)
                return true;

            return false;
        }

        protected bool Spawn(PlayerHordeGroup group, SpawningHorde horde)
        {
            int entityId = horde.horde.entities[horde.entityIndex++];

            EntityAlive entity = EntityFactory.CreateEntity(entityId, horde.DetermineRandomSpawnPosition()) as EntityAlive;
            HordeManager.Instance.World.SpawnEntityInWorld(entity);

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

        private List<EntityPlayer> GetNearbyPlayers(EntityPlayer player)
        {
            return GetNearbyPlayers(player.position);
        }

        private List<EntityPlayer> GetNearbyPlayers(Vector3 position)
        {
            List<EntityPlayer> players = new List<EntityPlayer>();

            foreach (var playerId in HordeManager.Instance.Players)
            {
                EntityPlayer player = HordeManager.Instance.World.GetEntity(playerId) as EntityPlayer;

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

            foreach (var playerId in HordeManager.Instance.Players)
            {
                if (grouped.Contains(playerId))
                    continue;

                var player = HordeManager.Instance.World.GetEntity(playerId) as EntityPlayer;

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
            public Vector3 spawnPosition;
            public Vector3 targetPosition;
            public int entityIndex = 0;

            public SpawningHorde(Horde horde, Vector3 spawnPosition, Vector3 targetPosition)
            {
                this.horde = horde;
                this.spawnPosition = spawnPosition;
                this.targetPosition = targetPosition;
            }

            public Vector3 DetermineRandomSpawnPosition()
            {
                if (!HordeManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(this.spawnPosition, 2, 20, 2, true, out Vector3 randomStartPos))
                {
                    // Failed to find a random spawn near position, so just assign default spawn position for horde.
                    randomStartPos = this.spawnPosition;
                }

                return randomStartPos;
            }
        }
    }
}