using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;

using ImprovedHordes.Horde.Wandering.AI.Commands;

using static ImprovedHordes.Utils.Logger;
using static ImprovedHordes.Utils.Math;

namespace ImprovedHordes.Horde.Wandering
{
    public class WanderingHordeSpawner
    {
        private static readonly WanderingHordeGenerator HORDE_GENERATOR = new WanderingHordeGenerator();
        public readonly WanderingHorde horde;

        public WanderingHordeSpawner(WanderingHorde horde)
        {
            this.horde = horde;
        }

        private List<EntityPlayer> GetNearbyPlayerGroup(EntityPlayer player)
        {
            List<EntityPlayer> players = new List<EntityPlayer>();

            for(int i = 0; i < this.horde.manager.players.Count; i++)
            {
                int playerId = this.horde.manager.players[i];

                if (playerId == player.entityId)
                    continue;

                EntityPlayer nearbyPlayer = this.horde.manager.world.GetEntity(playerId) as EntityPlayer;

                if (Vector3.Distance(player.position, nearbyPlayer.position) > GamePrefs.GetInt(EnumGamePrefs.PartySharedKillRange) * 4)
                    continue;

                players.Add(nearbyPlayer);
            }

            return players;
        }

        private List<PlayerHordeGroup> DetermineHordeGroups()
        {
            List<int> grouped = new List<int>();
            List<PlayerHordeGroup> groups = new List<PlayerHordeGroup>();

            foreach (var playerId in this.horde.manager.players)
            {
                if (grouped.Contains(playerId))
                    continue;

                var player = this.horde.manager.world.GetEntity(playerId) as EntityPlayer;
                
                var group = GetNearbyPlayerGroup(player);
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
                    var leaderOfGroupIndex = this.horde.manager.random.RandomRange(0, group.Count - 1);
                    var leaderOfGroup = group[leaderOfGroupIndex];
                    
                    PlayerHordeGroup hordeGroup = new PlayerHordeGroup(leaderOfGroup, group);
                    groups.Add(hordeGroup);
                }
            }

            return groups;
        }

        public void SpawnWanderingHordes()
        {
            this.horde.state = WanderingHorde.EHordeState.Spawning;

            List<PlayerHordeGroup> playerHordeGroups = DetermineHordeGroups();

            #region Spawning Log Info
            Log("[Wandering Horde] Occurance {0} Spawning", this.horde.schedule.currentOccurance + 1);
            StringBuilder leads = new StringBuilder();

            for(int i = 0; i < playerHordeGroups.Count; i++)
            {
                var group = playerHordeGroups[i];
                leads.Append(group.leader.EntityName);

                if (i < playerHordeGroups.Count - 1)
                    leads.Append(", ");
            }

            Log("[Wandering Horde] Player Horde Group Leads {0}", leads.ToString());
            #endregion

            foreach (var group in playerHordeGroups)
            {
                var averageGroupPosition = CalculateAverageGroupPosition(group);

                if (!CalculateWanderingHordePositions(averageGroupPosition, out Vector3 startPos, out Vector3 endPos))
                {
                    Error("[Wandering Horde] Invalid spawn position for wandering horde.");
                    return;
                }

                Horde horde = HORDE_GENERATOR.GenerateHorde(group);

#if DEBUG
                Log("Horde Group: {0}", horde.group.name);
                Log("GS: {0}", group.GetGroupGamestage());
                Log("Start Pos: " + startPos.ToString());
                Log("End Pos: " + endPos.ToString());
                Log("Horde size: " + horde.count);
#endif

                Dictionary<string, int> lastEntityIds = new Dictionary<string, int>();

                for (int i = 0; i < horde.count; i++)
                {
                    int entityId = horde.entities[i];
                    
                    if (!this.horde.manager.world.GetRandomSpawnPositionMinMaxToPosition(startPos, 2, 20, 2, true, out Vector3 randomStartPos))
                    {
                        // Failed to find a random spawn near position, so just assign default spawn position for horde.
                        randomStartPos = startPos;
                    }

                    EntityAlive entity = EntityFactory.CreateEntity(entityId, randomStartPos) as EntityAlive;
                    this.horde.manager.world.SpawnEntityInWorld(entity);

                    entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);
                    
                    if(entity is EntityEnemy enemy)
                        enemy.IsHordeZombie = true;

                    entity.bIsChunkObserver = true;
                    entity.IsBloodMoon = false;

                    List<HordeAICommand> commands = new List<HordeAICommand>();
                    const int DEST_RADIUS = 10;

                    if (horde.feral)
                    {
                        commands.Add(new HordeAICommandDestinationMoving(() => CalculateAverageGroupPosition(group), DEST_RADIUS));
                        commands.Add(new HordeAICommandWander(50f));
                    }

                    commands.Add(new HordeAICommandDestination(GetRandomNearbyPosition(endPos, DEST_RADIUS), DEST_RADIUS));
                    
                    this.horde.manager.aiManager.Add(entity, horde, true, commands);

                    // Add to pathfinder manager.
                    AstarManager.Instance.AddLocationLine(randomStartPos, endPos, 64);
                }

                this.horde.hordes.Add(horde);

                this.horde.schedule.AddWeeklyOccurancesForGroup(group.GetAllPlayers(), horde.group);

                lastEntityIds.Clear();
            }

            this.horde.state = WanderingHorde.EHordeState.StillAlive;
        }

        private Vector3 CalculateAverageGroupPosition(PlayerHordeGroup playerHordeGroup)
        {
            List<EntityPlayer> players = playerHordeGroup.GetAllPlayers();

            Vector3 avg = Vector3.zero;

            foreach(var player in players)
            {
                avg += player.position;
            }

            avg /= players.Count;

            if(!GetSpawnableY(ref avg))
            {
                // Testing this.
                Error("Failed to get spawnable Y.");
            }

            return avg;
        }

        private Vector3 GetRandomNearbyPosition(Vector3 target, float radius)
        {
            Vector2 random = this.horde.manager.random.RandomOnUnitCircle;

            float x = target.x + random.x * radius;
            float z = target.z + random.y * radius;

            return new Vector3(x, target.y, z);
        }

        public bool CalculateWanderingHordePositions(Vector3 commonPos, out Vector3 startPos, out Vector3 endPos)
        {
            var random = this.horde.manager.random;

            var radius = random.RandomRange(80, 12 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance)); // TODO: Make XML setting.
            startPos = GetSpawnableCircleFromPos(commonPos, radius);

            this.horde.manager.world.GetRandomSpawnPositionMinMaxToPosition(commonPos, 20, 40, 20, true, out Vector3 randomPos);

            var intersections = FindLineCircleIntersections(randomPos.x, randomPos.z, radius, startPos, commonPos, out _, out Vector2 intEndPos);

            endPos = new Vector3(intEndPos.x, 0, intEndPos.y);
            var result = GetSpawnableY(ref endPos);

            if(!result)
            {
                return CalculateWanderingHordePositions(commonPos, out startPos, out endPos);
            }

            if (intersections < 2)
            {
                Warning("[Wandering Horde] Only 1 intersection was found.");

                return false;
            }

            return true;
        }

        public bool GetSpawnableY(ref Vector3 pos)
        {
            //int y = Utils.Fastfloor(playerY - 1f);
            int y = (int)byte.MaxValue;
            int x = global::Utils.Fastfloor(pos.x);
            int z = global::Utils.Fastfloor(pos.z);

            if(this.horde.manager.world.GetWorldExtent(out Vector3i minSize, out Vector3i maxSize))
            {
                x = Mathf.Clamp(x, minSize.x, maxSize.x);
                z = Mathf.Clamp(z, minSize.z, maxSize.z);
            }
            while(this.horde.manager.world.GetBlock(x, y, z).type == 0)
            {
                if (--y < 0)
                    return false;
            }

            pos.x = (float)x;
            pos.y = (float)(y + 1);
            pos.z = z;
            return true;
        }

        public Vector3 GetSpawnableCircleFromPos(Vector3 playerPos, float radius)
        {
            Vector2 startCircle = this.horde.manager.random.RandomOnUnitCircle;

            float x = (startCircle.x * radius) + playerPos.x;
            float z = (startCircle.y * radius) + playerPos.z;

            Vector3 circleFromPlayer = new Vector3(x, 0, z);
            bool result = GetSpawnableY(ref circleFromPlayer);

            if(!result)
            {
                Log("[Wandering Horde] Failed to find spawnable circle from pos. X" + x + " Z " + z);
                return GetSpawnableCircleFromPos(playerPos, radius);
            }

            return circleFromPlayer;
        }
        private sealed class WanderingHordeGenerator : HordeGenerator
        {
            public WanderingHordeGenerator() : base("wandering")
            { }

            public override Horde GenerateHorde(PlayerHordeGroup playerHordeGroup)
            {
                int gamestage = playerHordeGroup.GetGroupGamestage();
                var groupLeaderPlayer = playerHordeGroup.leader;

                var wanderingHorde = ImprovedHordesMod.manager.wanderingHorde;
                var occurance = wanderingHorde.schedule.occurances[wanderingHorde.schedule.currentOccurance];

                var groups = Hordes.hordes[this.type].Values;
                List<HordeGroup> groupsToPick = new List<HordeGroup>();

                foreach (var group in groups)
                {
                    if (group.MaxWeeklyOccurances != null)
                    {
                        var maxWeeklyOccurances = group.MaxWeeklyOccurances.Evaluate();
                        var weeklyOccurancesForPlayer = wanderingHorde.schedule.GetWeeklyOccurancesForPlayer(groupLeaderPlayer, group);

                        if (weeklyOccurancesForPlayer >= maxWeeklyOccurances)
                            continue;

                        if (maxWeeklyOccurances > 0)
                        {
                            float diminishedChance = (float)Math.Pow(1 / maxWeeklyOccurances, weeklyOccurancesForPlayer);

                            if (wanderingHorde.manager.random.RandomFloat > diminishedChance)
                                continue;
                        }
                    }

                    if (group.PrefWeekDays != null)
                    {
                        var prefWeekDays = group.PrefWeekDays.Evaluate();
                        var weekDay = wanderingHorde.GetCurrentWeekDay();

                        // RNG whether to still spawn this horde, adds variation.
                        bool randomChance = wanderingHorde.manager.random.RandomFloat >= 0.5f;

                        if (!randomChance && !prefWeekDays.Contains(weekDay))
                            continue;
                    }

                    int groupsThatMatchGS = 0;
                    foreach (var entities in group.entities)
                    {
                        if (entities.gs != null)
                        {
                            GS gs = entities.gs;

                            if (gs.min != null && gamestage < gs.min.Evaluate())
                                continue;

                            if (gs.max != null && gamestage > gs.max.Evaluate())
                                continue;
                        }

                        groupsThatMatchGS++;
                    }

                    if (groupsThatMatchGS == 0)
                        continue;

                    groupsToPick.Add(group);
                }

                if (groupsToPick.Count == 0)
                    groupsToPick.AddRange(groups);

                HordeGroup randomGroup = groupsToPick[wanderingHorde.manager.random.RandomRange(0, groupsToPick.Count - 1)];
                Dictionary<HordeGroupEntity, int> entitiesToSpawn = new Dictionary<HordeGroupEntity, int>();

                foreach (var entity in randomGroup.entities)
                {
                    if (entity.chance != null && entity.chance.Evaluate() < wanderingHorde.manager.random.RandomFloat)
                        continue;

                    entitiesToSpawn.Add(entity, 0);

                    int minCount = entity.minCount != null ? entity.minCount.Evaluate() : 0;
                    int maxCount = entity.maxCount != null ? entity.maxCount.Evaluate() : -1;

                    GS gs = entity.gs;
                    int minGS = gs != null && gs.min != null ? gs.min.Evaluate() : 0;
                    int maxGS = gs != null && gs.max != null ? gs.max.Evaluate() : -1;

                    if (gs != null) // Keep an eye on.
                    {
                        if (gamestage < minGS)
                            continue;

                        if (maxGS > 0 && gamestage > maxGS)
                            continue;
                    }

                    int count;

                    if (gs == null || gs.countIncPerGS == null)
                    {
                        if (maxCount > 0)
                            count = wanderingHorde.manager.random.RandomRange(minCount, maxCount);
                        else
                        {
                            Error("Cannot calculate count of entity/entitygroup {0} in group {1} because no gamestage or maximum count has been specified.", entity.name ?? entity.group, randomGroup.name);
                            count = 0;
                        }
                    }
                    else
                    {
                        float countIncPerGS = gs.countIncPerGS.Evaluate();
                    
                        int toSpawn = minCount + (int)Math.Floor(countIncPerGS * (gamestage - minGS));
                        int countDecGS = 0;

                        if (gs.countDecGS != null && gamestage > (countDecGS = gs.countDecGS.Evaluate()) && gs.countDecPerPostGS != null)
                        {
                            float countDecPerPostGS = gs.countDecPerPostGS.Evaluate();

                            int decGSSpawn = (int)Math.Floor(countDecPerPostGS * (gamestage - countDecGS));

                            if (decGSSpawn > 0)
                                toSpawn -= decGSSpawn;
                        }

                        // TODO.
                        if (toSpawn < 0)
                            toSpawn = 0;

                        count = toSpawn;
                    }

                    if (maxCount >= 0 && count > maxCount)
                        count = maxCount;

                    entitiesToSpawn[entity] = count;

#if DEBUG
                    Log("[Wandering Horde] Spawning {0} of {1}", count, entity.name ?? entity.group);
#endif
                }

                List<int> entityIds = new List<int>();
                int totalCount = 0;
                foreach (var entitySet in entitiesToSpawn)
                {
                    HordeGroupEntity ent = entitySet.Key;
                    int count = entitySet.Value;

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
                            int entityId = EntityGroups.GetRandomFromGroup(ent.group, ref lastEntityId, wanderingHorde.manager.random);

                            entityIds.Add(entityId);
                        }

                        totalCount += count;
                    }
                    else
                    {
                        Error("[Wandering Horde] Horde entity in group {0} has no name or group. Skipping.", randomGroup.name);
                        continue;
                    }
                }

                return new Horde(playerHordeGroup, randomGroup, totalCount, occurance.feral, entityIds.ToArray());
            }
        }
    }
}