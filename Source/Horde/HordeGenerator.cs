using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static ImprovedHordes.IHLog;

namespace ImprovedHordes.Horde
{
    class HordeGenerators
    {
        public static readonly HordeGenerator<WanderingHorde> WanderingHordeGenerator = new WanderingHordeGenerator();
    }

    abstract class HordeGenerator<T> where T : Horde
    {
        protected string type;
        public HordeGenerator(string type)
        {
            this.type = type;
        }

        public abstract T GenerateHordeFromGameStage(EntityPlayer player, int gamestage);
    }

    sealed class WanderingHordeGenerator : HordeGenerator<WanderingHorde>
    {
        public WanderingHordeGenerator() : base("wandering")
        { }

        public override WanderingHorde GenerateHordeFromGameStage(EntityPlayer player, int gamestage)
        {
            var manager = ImprovedHordesMod.manager.wanderingHorde;
            var occurance = manager.hordes.schedule.occurances[manager.hordes.schedule.currentOccurance];

            var groups = Hordes.hordes[this.type].Values;
            List<HordeGroup> groupsToPick = new List<HordeGroup>();

            foreach (var group in groups)
            {
                if (group.MaxWeeklyOccurances != null)
                {
                    var maxWeeklyOccurances = group.MaxWeeklyOccurances.Evaluate();
                    var weeklyOccurancesForPlayer = manager.hordes.schedule.GetWeeklyOccurancesForPlayer(player, group);

                    if (weeklyOccurancesForPlayer >= maxWeeklyOccurances)
                        continue;
                }

                if (group.PrefWeekDays != null)
                {
                    var prefWeekDays = group.PrefWeekDays.Evaluate();
                    var weekDay = manager.GetCurrentWeekDay();

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

            HordeGroup randomGroup = groupsToPick[manager.manager.random.RandomRange(0, groupsToPick.Count - 1)];
            Dictionary<HordeGroupEntity, int> entitiesToSpawn = new Dictionary<HordeGroupEntity, int>();

            foreach(var entity in randomGroup.entities)
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

                if(gs == null || countIncPerGS == 0)
                {
                    if (maxCount > 0)
                        count = manager.manager.random.RandomRange(minCount, maxCount);
                    else
                    {
                        Error("Cannot calculate count of entity/entitygroup {0} in group {1} because no gamestage or maximum count has been specified.", entity.name != null ? entity.name : entity.group, randomGroup.name);
                        count = 0;
                    }
                }
                else
                {
                    int toSpawn = minCount + (int)Math.Floor(countIncPerGS * gamestage);

                    if(countDecGS > 0)
                    {
                        int decGSSpawn = (int)Math.Floor(countDecPerPostGS * gamestage);

                        if(decGSSpawn > 0)
                            toSpawn -= decGSSpawn;
                    }

                    count = toSpawn;
                }

                if (maxCount > 0 && count > maxCount)
                    count = maxCount;

                entitiesToSpawn[entity] = count;

#if DEBUG
                Log("Spawning {0} of {1}", count, entity.name != null ? entity.name : entity.group);
#endif
            }

            List<int> entityIds = new List<int>();
            int totalCount = 0;
            foreach(var entitySet in entitiesToSpawn)
            {
                HordeGroupEntity ent = entitySet.Key;
                int count = entitySet.Value;
                
                if (ent.name != null)
                {
                    int entityId = EntityClass.FromString(ent.name);
                    
                    for(var i = 0; i < count; i++)
                        entityIds.Add(entityId);

                    totalCount += count;
                }
                else if (ent.group != null)
                {
                    int lastEntityId = -1;

                    for(var i = 0; i < count; i++)
                    {
                        int entityId = EntityGroups.GetRandomFromGroup(ent.group, ref lastEntityId, manager.manager.random);

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

            WanderingHorde wanderingHorde = new WanderingHorde(randomGroup, totalCount, occurance.feral, entityIds.ToArray());

            return wanderingHorde;
        }
    }
}
