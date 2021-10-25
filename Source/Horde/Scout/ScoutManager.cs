using System;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Events;
using ImprovedHordes.Horde.Scout.AI.Commands;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutManager
    {
        public const int CHUNK_RADIUS = 3; // TODO: Make a xml value

        private readonly Dictionary<HordeAIEntity, Scout> scouts = new Dictionary<HordeAIEntity, Scout>();

        public readonly HordeManager manager;
        private readonly ScoutSpawner spawner;
        private readonly ScoutHordeSpawner hordeSpawner;

        public ScoutManager(HordeManager manager)
        {
            this.manager = manager;
            this.spawner = new ScoutSpawner(this);
            this.hordeSpawner = new ScoutHordeSpawner(this);

            this.manager.AIManager.OnHordeAIEntitySpawned += OnScoutEntitySpawned;
        }

        public void OnScoutEntitySpawned(object sender, HordeEntitySpawnedEvent e)
        {
            Log("Spawned");

            if(!e.horde.GetHordeInstance().group.list.type.EqualsCaseInsensitive("Scouts") &&
                e.entity.entity.entityClass != EntityClass.FromString("zombieScreamer") // Screamers are always scouts.
                    && e.entity.entity.entityClass != EntityClass.FromString("zombieScreamerFeral")
                    && e.entity.entity.entityClass != EntityClass.FromString("zombieScreamerRadiated"))
                return;

            Log("Set");

            Scout scout = new Scout(e.entity);
            this.scouts.Add(e.entity, scout);

            e.entity.commands.Add(new HordeAICommandScout(this, e.entity));
            e.entity.commands.RemoveRange(0, e.entity.commands.Count - 1); // Make Scout command the only command.

            e.entity.OnHordeEntityKilled += OnScoutEntityKilled;
            e.entity.OnHordeEntityDespawned += OnScoutEntityDespawned;
        }

        public void OnScoutEntityKilled(object sender, HordeEntityKilledEvent e)
        {
            this.RemoveScout(e.entity);
        }

        public void OnScoutEntityDespawned(object sender, HordeEntityDespawnedEvent e)
        {
            this.RemoveScout(e.entity);
        }

        private void RemoveScout(HordeAIEntity entity)
        {
            // No need for checking because it only applies to scouts.
            if (!this.scouts.ContainsKey(entity))
            {
                Warning("[Scout] Entity {0} was already removed from scouts.", entity.GetEntityId());
                return;
            }

            this.scouts.Remove(entity);
        }

        public void SpawnScouts(Vector3 targetPos)
        {
            // TODO Scout Spawner
            EntityPlayer closest = this.manager.World.GetClosestPlayer(targetPos, -1, false);
            this.spawner.StartSpawningFor(closest, false, targetPos); // TODO Feral?
        }

        public void SpawnScoutHorde(EntityPlayer target)
        {
            this.hordeSpawner.StartSpawningFor(target, false); // TODO Feral?
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
            this.hordeSpawner.Update();
        }

        public List<Scout> GetScoutsNear(Vector3 targetPos)
        {
            const int chunkDist = 16 * CHUNK_RADIUS;

            List<Scout> nearby = new List<Scout>();

            foreach(var scoutEntry in scouts)
            {
                var entity = scoutEntry.Key;
                var scout = scoutEntry.Value;

                Vector3 scoutPos = entity.entity.position;

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
