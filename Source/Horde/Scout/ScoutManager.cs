using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using ImprovedHordes.Horde.AI.Events;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutManager
    {
        public const int CHUNK_RADIUS = 3; // TODO: Make a xml value

        private static readonly HordeGenerator SCOUT_HORDE_GENERATOR = new ScoutHordeGenerator(); // Horde spawned by Scouts.

        private readonly List<Scout> scouts = new List<Scout>();

        public HordeManager manager;
        private ScoutSpawner spawner;

        public ScoutManager(HordeManager manager)
        {
            this.manager = manager;
            this.spawner = new ScoutSpawner(this);

            this.manager.AIManager.OnHordeAIEntitySpawned += OnScoutEntitySpawned;
        }

        public void OnScoutEntitySpawned(object sender, HordeAIEntitySpawnedEvent e)
        {
            if(e.horde.GetHordeInstance().group.list.type.EqualsCaseInsensitive("Scouts"))
            {
                return;
            }

            // Screamers are always scouts.
            if(e.entity.entity.entityClass == EntityClass.FromString("zombieScreamer")
                || e.entity.entity.entityClass == EntityClass.FromString("zombieScreamerFeral")
                || e.entity.entity.entityClass == EntityClass.FromString("zombieScreamerRadiated"))
            {
                Scout scout = new Scout(e.entity);

                this.scouts.Add(scout);
            }
        }

        public void SpawnScouts(Vector3 targetPos)
        {
            // TODO Scout Spawner
            EntityPlayer closest = this.manager.World.GetClosestPlayer(targetPos, -1, false);
            this.spawner.StartSpawningFor(new PlayerHordeGroup(closest), false); // TODO Feral?
        }

        public void NotifyScoutsNear(Vector3i targetBlockPos)
        {
            // TODO Notify scouts near chunk about new target.
        }

        public void Update()
        {
            this.spawner.Update();
        }

        public Scout GetScoutsNearChunk(Vector3 targetPos)
        {


            return null;
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
