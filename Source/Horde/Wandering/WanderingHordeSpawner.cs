using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;

using ImprovedHordes.Horde.Wandering.AI.Commands;

using static ImprovedHordes.IHLog;

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

        private int CalculateNearbyGameStages(EntityPlayer player)
        {
            List<int> playerGameStages = new List<int>
            {
                player.gameStage // Add player.
            };

            foreach (var nearbyPlayer in GetNearbyPlayerGroup(player))
            {
                playerGameStages.Add(nearbyPlayer.gameStage);
            }

            return GameStageDefinition.CalcPartyLevel(playerGameStages);
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

        private List<EntityPlayer> DetermineHordeLeads()
        {
            List<int> grouped = new List<int>();
            List<EntityPlayer> leads = new List<EntityPlayer>();

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
                    leads.Add(group[leaderOfGroupIndex]);
                }
            }

            return leads;
        }

        public void SpawnWanderingHordes()
        {
            this.horde.state = WanderingHorde.EHordeState.Spawning;

            List<EntityPlayer> hordeLeads = DetermineHordeLeads();

            #region Spawning Log Info
            Log("Weekly Wandering Horde {0} Spawning", this.horde.schedule.currentOccurance + 1);
            StringBuilder leads = new StringBuilder();

            for(int i = 0; i < hordeLeads.Count; i++)
            {
                var lead = hordeLeads[i];
                leads.Append(lead.EntityName);

                if (i < hordeLeads.Count - 1)
                    leads.Append(", ");
            }

            Log("Leads {0}", leads.ToString());
            #endregion

            foreach (var player in hordeLeads)
            {
                if (!CalculateWanderingHordePositions(out Vector3 startPos, out Vector3 endPos, out Vector3 playerPos, player))
                {
                    Error("Invalid spawn position for wandering horde.");
                    return;
                }

                Horde horde = HORDE_GENERATOR.GenerateHordeFromGameStage(player, CalculateNearbyGameStages(player));

#if DEBUG
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
                        //Error("Invalid spawn position. " + randomStartPos.ToString() + " relative to " + startPos.ToString());
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

                    if (horde.feral)
                    {
                        HordeAICommandDestinationPlayer wTPCommand = new HordeAICommandDestinationPlayer(player);
                        commands.Add(wTPCommand);

                        HordeAICommandWander wanderCommand = new HordeAICommandWander(30);
                    }

                    HordeAICommandDestination wTDCommand = new HordeAICommandDestination(endPos, 6);
                    commands.Add(wTDCommand);

                    this.horde.manager.aiManager.Add(entity, horde, commands);
                }

                this.horde.hordes.Add(horde);

                lastEntityIds.Clear();
            }

            this.horde.state = WanderingHorde.EHordeState.StillAlive;
        }

        public bool CalculateWanderingHordePositions(out Vector3 startPos, out Vector3 endPos, out Vector3 playerPos, EntityPlayer player)
        {
            var random = this.horde.manager.random;

            //var radius = 80 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance);
            //var radius = 80f;
            var radius = random.RandomRange(80, 12 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance));
            playerPos = player.position;
            startPos = GetSpawnableCircleFromPos(playerPos, radius);

            this.horde.manager.world.GetRandomSpawnPositionMinMaxToPosition(playerPos, 20, 40, 20, true, out Vector3 randomPos);

            var intersections = FindLineCircleIntersections(randomPos.x, randomPos.z, radius, startPos, playerPos, out _, out Vector2 intEndPos);

            endPos = new Vector3(intEndPos.x, 0, intEndPos.y);
            var result = GetSpawnableY(ref endPos);

            if(!result)
            {
                return CalculateWanderingHordePositions(out startPos, out endPos, out playerPos, player);
            }

            if (intersections < 2)
            {
                Warning("Only 1 intersection was found.");

                return false;
            }

            return true;
        }

        public int FindLineCircleIntersections(float centerX, float centerY, float radius, Vector2 point1, Vector2 point2, out Vector2 int1, out Vector2 int2)
        {
            float dx, dy, A, B, C, det, t;

            dx = point2.x - point1.x;
            dy = point2.y - point1.y;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (point1.x - centerX) + dy * (point1.y - centerY));
            C = (point1.x - centerX) * (point1.x - centerX) + (point1.y - centerY) * (point1.y - centerY) - radius * radius;

            det = B * B - 4 * A * C;

            if(A <= 0.0000001 || det < 0)
            {
                // No solutions.
                int1 = Vector2.zero;
                int2 = Vector2.zero;
                
                return 0;
            }
            else if(det == 0)
            {
                // One solution.
                t = -B / (2 * A);

                int1 = new Vector2(point1.x + t * dx, point1.y + t * dy);
                int2 = Vector2.zero;
                
                return 1;
            }
            else
            {
                // Two solutions.
                t = (float)((-B + Math.Sqrt(det)) / (2 * A));
                int1 = new Vector2(point1.x + t * dx, point1.y + t * dy);

                t = (float)((-B - Math.Sqrt(det)) / (2 * A));
                int2 = new Vector2(point1.x + t * dx, point1.y + t * dy);

                return 2;
            }
        }

        public bool GetSpawnableY(ref Vector3 pos)
        {
            //int y = Utils.Fastfloor(playerY - 1f);
            int y = (int)byte.MaxValue;
            int x = Utils.Fastfloor(pos.x);
            int z = Utils.Fastfloor(pos.z);

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
                Log("Failed to find spawnable circle from pos. X" + x + " Z " + z);
                return GetSpawnableCircleFromPos(playerPos, radius);
            }

            return circleFromPlayer;
        }
        private sealed class WanderingHordeGenerator : HordeGenerator
        {
            public WanderingHordeGenerator() : base("wandering")
            { }

            public override Horde GenerateHordeFromGameStage(EntityPlayer player, int gamestage)
            {
                var wanderingHorde = ImprovedHordesMod.manager.wanderingHorde;
                var occurance = wanderingHorde.schedule.occurances[wanderingHorde.schedule.currentOccurance];

                var groups = Hordes.hordes[this.type].Values;
                List<HordeGroup> groupsToPick = new List<HordeGroup>();

                foreach (var group in groups)
                {
                    if (group.MaxWeeklyOccurances != null)
                    {
                        var maxWeeklyOccurances = group.MaxWeeklyOccurances.Evaluate();
                        var weeklyOccurancesForPlayer = wanderingHorde.schedule.GetWeeklyOccurancesForPlayer(player, group);

                        if (weeklyOccurancesForPlayer >= maxWeeklyOccurances)
                            continue;
                    }

                    if (group.PrefWeekDays != null)
                    {
                        var prefWeekDays = group.PrefWeekDays.Evaluate();
                        var weekDay = wanderingHorde.GetCurrentWeekDay();

                        if (!prefWeekDays.Contains(weekDay))
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
                    entitiesToSpawn.Add(entity, 0);

                    int minCount = entity.minCount != null ? entity.minCount.Evaluate() : 0;
                    int maxCount = entity.maxCount != null ? entity.maxCount.Evaluate() : -1;

                    GS gs = entity.gs;
                    int minGS = gs != null && gs.min != null ? gs.min.Evaluate() : 0;
                    int maxGS = gs != null && gs.max != null ? gs.max.Evaluate() : -1;
                    int countDecGS = gs != null && gs.countDecGS != null ? gs.countDecGS.Evaluate() : -1;

                    float countIncPerGS = gs != null && gs.countIncPerGS != null ? gs.countIncPerGS.Evaluate() : 0;
                    float countDecPerPostGS = gs != null && gs.countDecPerPostGS != null ? gs.countDecPerPostGS.Evaluate() : 0;

                    if (gs != null) // Keep an eye on.
                    {
                        if (gamestage < minGS)
                            continue;

                        if (maxGS > 0 && gamestage > maxGS)
                            continue;
                    }

                    //var count = minCount + (int)Math.Floor(countIncPerGS * gamestage);
                    int count;

                    if (gs == null || countIncPerGS == 0)
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
                        int toSpawn = minCount + (int)Math.Floor(countIncPerGS * gamestage);

                        if (countDecGS > 0)
                        {
                            int decGSSpawn = (int)Math.Floor(countDecPerPostGS * gamestage);

                            if (decGSSpawn > 0)
                                toSpawn -= decGSSpawn;
                        }

                        count = toSpawn;
                    }

                    if (maxCount > 0 && count > maxCount)
                        count = maxCount;

                    entitiesToSpawn[entity] = count;

#if DEBUG
                    Log("Spawning {0} of {1}", count, entity.name ?? entity.group);
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
                        Error("Horde entity in group {0} has no name or group. Skipping.", randomGroup.name);
                        continue;
                    }
                }

                return new Horde(randomGroup, totalCount, occurance.feral, entityIds.ToArray());
            }
        }
    }
}