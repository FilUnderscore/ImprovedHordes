using System;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using ImprovedHordes.Horde.AI.Events;
using ImprovedHordes.Horde.Scout.AI.Commands;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutManager
    {
        public const int CHUNK_RADIUS = 3; // TODO: Make a xml value

        private readonly List<Scout> scouts = new List<Scout>();

        public readonly HordeManager manager;
        private readonly ScoutSpawner spawner;

        public ScoutManager(HordeManager manager)
        {
            this.manager = manager;
            this.spawner = new ScoutSpawner(this);

            this.manager.AIManager.OnHordeAIEntitySpawned += OnScoutEntitySpawned;
        }

        public void OnScoutEntitySpawned(object sender, HordeEntitySpawnedEvent e)
        {
            if(!e.horde.GetHordeInstance().group.list.type.EqualsCaseInsensitive("Scouts") &&
                e.entity.entity.entityClass != EntityClass.FromString("zombieScreamer") // Screamers are always scouts.
                    && e.entity.entity.entityClass != EntityClass.FromString("zombieScreamerFeral")
                    && e.entity.entity.entityClass != EntityClass.FromString("zombieScreamerRadiated"))
                return;

            Scout scout = new Scout(e.entity);
            this.scouts.Add(scout);

            // TODO test. 
            // TODO Add scout command for non heatmap scout.

            if (e.horde.GetHordeInstance().group.list.type.EqualsCaseInsensitive("Scouts"))
            {
                Log("Heatmap scout spawned.");
            }
            else
            {
                Log("Horde scout spawned.");
            }

            e.entity.commands.Add(new HordeAICommandScout(e.entity));

            e.entity.OnHordeEntityKilled += OnScoutEntityKilled;
        }

        public void OnScoutEntityKilled(object sender, HordeEntityKilledEvent e)
        {
            // No need for checking because it only applies to scouts.
            Scout storedScout = null;

            foreach(var scout in scouts)
            {
                if(scout.aiEntity == e.entity)
                {
                    storedScout = scout;
                }
            }

            if (storedScout == null)
                throw new NullReferenceException("Stored scout is null, perhaps the scout entity was not cleaned up properly?");

            this.scouts.Remove(storedScout);
        }

        public void SpawnScouts(Vector3 targetPos)
        {
            // TODO Scout Spawner
            EntityPlayer closest = this.manager.World.GetClosestPlayer(targetPos, -1, false);
            this.spawner.StartSpawningFor(new PlayerHordeGroup(closest), false); // TODO Feral?
        }

        public void NotifyScoutsNear(Vector3i targetBlockPos)
        {
            Vector3 targetPos = new Vector3(targetBlockPos.x, targetBlockPos.y, targetBlockPos.z);
            // TODO Notify scouts near chunk about new target.

            foreach (var scout in GetScoutsNear(targetPos))
            {
                scout.Interrupt(targetPos);
            }
        }

        public void Update()
        {
            if (this.manager.Players.Count == 0)
                return;

            this.spawner.Update();
        }

        public List<Scout> GetScoutsNear(Vector3 targetPos)
        {
            const int chunkDist = 16 * CHUNK_RADIUS;

            List<Scout> nearby = new List<Scout>();

            foreach(var scout in scouts)
            {
                Vector3 scoutPos = scout.aiEntity.entity.position;

                if(Vector2.Distance(new Vector2(scoutPos.x, scoutPos.z), new Vector2(targetPos.x, targetPos.z)) <= chunkDist)
                {
                    nearby.Add(scout);
                }
            }

            return nearby;
        }

        private sealed class ScoutHordeGenerator : HordeGenerator
        {
            public ScoutHordeGenerator() : base("scout")
            {
            }
        }

        class HarmonyPatches
        {
            [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
            [HarmonyPatch("SpawnScouts")]
            class SpawnScoutsHook
            {
                static bool Prefix(Vector3 targetPos)
                {
                    var scoutManager = HordeManager.Instance.ScoutManager;
                    scoutManager.SpawnScouts(targetPos);

                    // Prevent default scout horde from spawning.
                    return false;
                }
            }

            [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
            [HarmonyPatch("NotifyEvent")]
            class NotifyEventHook
            {
                static void Postfix(AIDirectorChunkEvent _chunkEvent)
                {
                    // Notify scouts in chunk of the new event to investigate.
                    if(_chunkEvent.Value >= 2.0f && _chunkEvent.EventType != EnumAIDirectorChunkEvent.Torch)
                    {
                        var scoutManager = HordeManager.Instance.ScoutManager;
                        scoutManager.NotifyScoutsNear(_chunkEvent.Position);
                    }
                }
            }
        }
    }
}
