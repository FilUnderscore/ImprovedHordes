using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.IO;

using static ImprovedHordes.IHLog;

namespace ImprovedHordes.Horde
{
    class WanderingHordeManager
    {
        public HordeManager manager;
        public readonly WanderingHordes hordes = new WanderingHordes();
        
        public WanderingHordeManager(HordeManager manager)
        {
            this.manager = manager;

            RuntimeEvalRegistry.RegisterVariable("week", this.GetCurrentWeek);

            RuntimeEvalRegistry.RegisterVariable("weekDay", this.GetCurrentWeekDay);

            RuntimeEvalRegistry.RegisterVariable("test", () => true);
        }

        public void Load(BinaryReader reader)
        {
            this.hordes.schedule.Load(reader);
        }

        public void Save(BinaryWriter writer)
        {
            this.hordes.schedule.Save(writer);
        }

        public void Update()
        {
            if (CheckIfNeedsReset())
            {
                ulong nextResetTime = this.GenerateNewResetTime();
#if DEBUG
                var (Days, Hours, Minutes) = GameUtils.WorldTimeToElements(nextResetTime);
                Log("[Wandering Horde] Resetting wandering horde. Next reset at Day {0} {1:D2}:{2:D2}", Days, Hours, Minutes);
#endif

                this.GenerateNewSchedule(nextResetTime);
            }

            if (this.manager.players.Count == 0)
                return;

            if (ShouldSpawnWanderingHorde())
            {
                this.SpawnWanderingHordes();
            }

            this.UpdateWanderingHorde(Time.fixedDeltaTime);
        }

        // Spawn on weekly basis # of hordes
        // Spaced apart a set number of days.
        public bool ShouldSpawnWanderingHorde()
        {
            return this.hordes.state == WanderingHordes.EHordeState.Finished && 
                this.hordes.schedule.currentOccurance < this.hordes.schedule.occurances.Count && 
                this.manager.GetWorldTime() >= this.hordes.schedule.occurances[this.hordes.schedule.currentOccurance].worldTime;
        }

        public bool CheckIfNeedsReset()
        {
            return this.manager.GetWorldTime() > this.hordes.schedule.nextResetTime;
        }

        public ulong GenerateNewResetTime()
        {
            int dayLightLength = GamePrefs.GetInt(EnumGamePrefs.DayLightLength);
            int night = 22;

            int nextDayAfterHordeReset = GetTotalDayRelativeToNextWeek(1);

            return GameUtils.DayTimeToWorldTime(nextDayAfterHordeReset, night - dayLightLength, 0);
        }

        /// <summary>
        /// Gets the relative day to the current week, e.g. Day 8 is Day 1 of Week 2. etc.
        /// </summary>
        /// <param name="day">Day in week (1-7).</param>
        /// <returns>Total day accounting for weeks.</returns>
        public int GetTotalDayRelativeToNextWeek(int day)
        {
            int currentDays = GameUtils.WorldTimeToDays(this.manager.GetWorldTime());
            int dayInWeek = (int)Math.Ceiling(currentDays / (float)7) * 7 + day;

            return dayInWeek;
        }

        public int GetTotalDayRelativeToThisWeek(int day)
        {
            int currentDays = GameUtils.WorldTimeToDays(this.manager.GetWorldTime());
            int dayInWeek = (int)Math.Floor((currentDays - 1) / (float)7) * 7 + day;

            return dayInWeek;
        }

        public int GetCurrentWeekDay()
        {
            int totalDay = GameUtils.WorldTimeToDays(this.manager.GetWorldTime());
            int modulo = totalDay % 7;
            return modulo == 0 ? 7 : modulo;
        }

        public int GetCurrentWeek()
        {
            int totalDay = GameUtils.WorldTimeToDays(this.manager.GetWorldTime());

            return (int)Math.Floor(totalDay / (float)7) + 1;
        }

        public void GenerateNewSchedule(ulong nextResetTime)
        {
            var random = this.manager.random;

            var schedule = new WanderingHordes.Schedule();

            int maxOccurances = random.RandomRange(WanderingHordes.MIN_OCCURANCES, WanderingHordes.MAX_OCCURANCES);

            int possibleOccurances = 0;
            bool possible = false;
            ulong lastOccurance = 0UL;
            for(int i = 0; i < maxOccurances; i++)
            {
                ulong nextOccurance = GenerateNextOccurance(i, maxOccurances, out possible, random, lastOccurance);
                lastOccurance = nextOccurance;

                if(!possible)
                {
                    if (possibleOccurances == 0)
                        Log("No occurances will be scheduled for the remainder of the week.");

                    break;
                }

                bool feral = random.RandomRange(0, 10) >= 5;
                schedule.occurances.Add(i, new WanderingHordes.Occurance(nextOccurance, feral));
                possibleOccurances++;
                
#if DEBUG
                var (Days, Hours, Minutes) = GameUtils.WorldTimeToElements(nextOccurance);
                Log("[Wandering Horde] Occurance {0} at Day {1} {2}:{3}", i, Days, Hours, Minutes);
#endif
            }

#if DEBUG
            Log("[Wandering Horde] Possible occurances this week: {0}", possibleOccurances);
#endif

            schedule.nextResetTime = nextResetTime;
            this.hordes.schedule = schedule;
        }

        public ulong GenerateNextOccurance(int occurance, int occurances, out bool canHaveNext, GameRandom random, ulong lastOccurance)
        {
            var worldTime = this.manager.world.GetWorldTime();
            var maxWorldTime = GameUtils.DaysToWorldTime(this.GetTotalDayRelativeToThisWeek(1)) + WanderingHordes.HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX * 1000;

            var deltaWorldTime = maxWorldTime - (lastOccurance > 0 ? lastOccurance : worldTime);

            if (occurance == 0)
            {
                int randomHr = random.RandomRange(WanderingHordes.HOURS_TO_FIRST_OCCURANCE_MIN, (int)(Math.Floor((float)(WanderingHordes.HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX - 12) / 24) / (float)occurances) * 24 + 12);

                ulong randomHrToWorldTime = (ulong)randomHr * 1000UL + worldTime;

                if (randomHrToWorldTime > maxWorldTime)
                {
                    canHaveNext = false;
                    return maxWorldTime - deltaWorldTime / 2;
                }
                else
                {
                    canHaveNext = true;
                    return randomHrToWorldTime;
                }
            }
            else
            {
                canHaveNext = true;
                return lastOccurance + (deltaWorldTime / (ulong)occurances);
            }
        }

        public void UpdateWanderingHorde(double dt)
        {
            if (this.hordes.state == WanderingHordes.EHordeState.StillAlive)
            {
                for(int hordeIndex = 0; hordeIndex < this.hordes.hordes.Count; hordeIndex++)
                {
                    WanderingHorde horde = this.hordes.hordes[hordeIndex];

                    if (horde.commandsList.Count > 0)
                    {
                        if (this.hordes.schedule.currentOccurance < this.hordes.schedule.occurances.Count - 1 && this.manager.GetWorldTime() >= this.hordes.schedule.occurances[this.hordes.schedule.currentOccurance + 1].worldTime)
                        {
                            for (int i = 0; i < horde.commandsList.Count; i++)
                            {
                                WanderingHorde.Command command = horde.commandsList[i];

                                if(command.entity is EntityEnemy enemy)
                                    enemy.IsHordeZombie = false;

                                command.entity.bIsChunkObserver = false;

                                horde.commandsList.RemoveAt(i);
                            }

                            Log("[Wandering Horde] Horde did not end on time, so it has been forcefully ended.");
                        }
                        else
                        {
                            for (int i = 0; i < horde.commandsList.Count; i++)
                            {
                                WanderingHorde.Command command = horde.commandsList[i];

                                if(command == null)
                                {
                                    Warning("[Wandering Horde] Command is null.");
                                    horde.commandsList.RemoveAt(i);
                                    continue;
                                }

                                if(command.entity == null)
                                {
                                    Warning("[Wandering Horde] Command entity is null.");
                                    horde.commandsList.RemoveAt(i);
                                    continue;
                                }

                                if (command.entity.GetAttackTarget() != null)
                                    continue;

                                if (command.entity.IsDead())
                                {
                                    Log("[Wandering Horde] Horde entity was killed.");

                                    if(command.entity is EntityEnemy enemy)
                                        enemy.IsHordeZombie = false;

                                    command.entity.bIsChunkObserver = false;

                                    horde.commandsList.RemoveAt(i);

                                    continue;
                                }

                                if (command.state != WanderingHorde.ZombieState.Wandering)
                                {
                                    if (command.entity.HasInvestigatePosition)
                                    {
                                        if (command.entity.InvestigatePosition != command.currentTarget)
                                        {
                                            Log("[Wandering Horde] Horde entity was killed or investigating something.");

                                            if(command.entity is EntityEnemy enemy)
                                                enemy.IsHordeZombie = false;

                                            command.entity.bIsChunkObserver = false;

                                            horde.commandsList.RemoveAt(i);
                                            continue;
                                        }
                                        else
                                        {
                                            command.entity.SetInvestigatePosition(command.currentTarget, 6000, false);
                                        }
                                    }
                                    else if (command.state == WanderingHorde.ZombieState.PlayerWander)
                                    {
                                        command.state = WanderingHorde.ZombieState.Wandering;
                                        command.PlayerWanderTime = (float)(90.0f + this.manager.random.RandomFloat * 4.0);

                                        Log("[Wandering Horde] Horde entities are wandering around player position.");
                                    }
                                    else
                                    {
                                        Log("[Wandering Horde] No longer investigating.");

                                        horde.commandsList.RemoveAt(i);
                                        continue;
                                    }
                                }
                                else
                                {
                                    command.PlayerWanderTime -= (float)dt;
                                    command.entity.ResetDespawnTime();

                                    if (command.PlayerWanderTime <= 0.0 && command.entity.GetAttackTarget() == null)
                                    {
                                        command.state = WanderingHorde.ZombieState.End;
                                        command.currentTarget = command.endTarget;
                                        command.entity.SetInvestigatePosition(command.currentTarget, 6000, false);
                                        
                                        if(command.entity is EntityEnemy enemy)
                                            enemy.IsHordeZombie = false;

                                        Log("[Wandering Horde] Horde entity wandered enough, heading to next stop.");
                                    }
                                }
                            }
                        }
                    }

                    if (horde.commandsList.Count == 0)
                    {
                        Log("[Wandering Horde] Horde {0} has ended, all Zombies have either reached their destination or have been killed.", hordeIndex + 1);
                        
                        this.hordes.hordes.RemoveAt(hordeIndex);
                        continue;
                    }
                }

                if(this.hordes.hordes.Count == 0)
                {
                    Log("[Wandering Horde] Hordes for all groups have ended.");

                    this.hordes.schedule.currentOccurance++;
                    this.hordes.state = WanderingHordes.EHordeState.Finished;
                }
            }
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

            for(int i = 0; i < this.manager.players.Count; i++)
            {
                int playerId = this.manager.players[i];

                if (playerId == player.entityId)
                    continue;

                EntityPlayer nearbyPlayer = this.manager.world.GetEntity(playerId) as EntityPlayer;

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

            foreach (var playerId in this.manager.players)
            {
                if (grouped.Contains(playerId))
                    continue;

                var player = this.manager.world.GetEntity(playerId) as EntityPlayer;
                
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
                    var leaderOfGroupIndex = this.manager.random.RandomRange(0, group.Count - 1);
                    leads.Add(group[leaderOfGroupIndex]);
                }
            }

            return leads;
        }

        public void SpawnWanderingHordes()
        {
            this.hordes.state = WanderingHordes.EHordeState.Spawning;

            List<EntityPlayer> hordeLeads = DetermineHordeLeads();

            #region Spawning Log Info
            Log("Weekly Wandering Horde {0} Spawning", this.hordes.schedule.currentOccurance + 1);
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

                WanderingHorde horde = HordeGenerators.WanderingHordeGenerator.GenerateHordeFromGameStage(player, CalculateNearbyGameStages(player));

#if DEBUG
                Log("Start Pos: " + startPos.ToString());
                Log("End Pos: " + endPos.ToString());
                Log("Horde size: " + horde.count);
#endif

                Dictionary<string, int> lastEntityIds = new Dictionary<string, int>();

                for (int i = 0; i < horde.count; i++)
                {
                    int entityId = horde.entities[i];
                    
                    if (!this.manager.world.GetRandomSpawnPositionMinMaxToPosition(startPos, 2, 20, 2, true, out Vector3 randomStartPos))
                    {
                        //Error("Invalid spawn position. " + randomStartPos.ToString() + " relative to " + startPos.ToString());
                        randomStartPos = startPos;
                    }

                    EntityAlive entity = EntityFactory.CreateEntity(entityId, randomStartPos) as EntityAlive;
                    this.manager.world.SpawnEntityInWorld(entity);

                    entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);
                    
                    if(entity is EntityEnemy enemy)
                        enemy.IsHordeZombie = true;

                    entity.bIsChunkObserver = true;
                    entity.IsBloodMoon = false;

                    Vector2 randomOnCircle = this.manager.random.RandomOnUnitCircle * 6f;
                    Vector3 targetPlayerPos = playerPos + new Vector3(randomOnCircle.x, 0, randomOnCircle.y);
                    Vector3 targetEndPos = endPos + new Vector3(randomOnCircle.x, 0, randomOnCircle.y);

                    WanderingHorde.Command command = new WanderingHorde.Command()
                    {
                        entity = entity,
                        playerTarget = targetPlayerPos,
                        endTarget = targetEndPos,
                        currentTarget = horde.feral ? targetPlayerPos : targetEndPos,
                        state = horde.feral ? WanderingHorde.ZombieState.PlayerWander : WanderingHorde.ZombieState.End
                    };

                    horde.commandsList.Add(command);

                    entity.SetInvestigatePosition(command.currentTarget, 6000, false);
                }

                this.hordes.hordes.Add(horde);

                lastEntityIds.Clear();
            }

            this.hordes.state = WanderingHordes.EHordeState.StillAlive;
        }

        public bool CalculateWanderingHordePositions(out Vector3 startPos, out Vector3 endPos, out Vector3 playerPos, EntityPlayer player)
        {
            var random = this.manager.random;

            //var radius = 80 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance);
            //var radius = 80f;
            var radius = random.RandomRange(80, 12 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance));
            playerPos = player.position;
            startPos = GetSpawnableCircleFromPos(playerPos, radius);

            manager.world.GetRandomSpawnPositionMinMaxToPosition(playerPos, 20, 40, 20, true, out Vector3 randomPos);

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

            if(this.manager.world.GetWorldExtent(out Vector3i minSize, out Vector3i maxSize))
            {
                x = Mathf.Clamp(x, minSize.x, maxSize.x);
                z = Mathf.Clamp(z, minSize.z, maxSize.z);
            }
            while(this.manager.world.GetBlock(x, y, z).type == 0)
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
            Vector2 startCircle = this.manager.random.RandomOnUnitCircle;

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
    }
}