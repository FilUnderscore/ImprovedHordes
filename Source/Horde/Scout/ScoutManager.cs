using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutManager
    {
        private const int CHUNK_RADIUS = 3; // TODO: Make a xml value

        private static readonly HordeGenerator SCOUTS_HORDE_GENERATOR = new ScoutsHordeGenerator(); // Scouts - e.g. Screamer
        private static readonly HordeGenerator SCOUT_HORDE_GENERATOR = new ScoutHordeGenerator(); // Horde spawned by Scouts.

        private readonly List<Scout> scouts = new List<Scout>();

        private HordeManager manager;

        public ScoutManager(HordeManager manager)
        {
            this.manager = manager;
        }

        public void SpawnScouts(Vector3 targetPos)
        {
            // TODO Scout Spawner
        }

        public void NotifyScoutsNear(Vector3i targetBlockPos)
        {
            // TODO Notify scouts near chunk about new target.
        }

        public Scout GetScoutsNearChunk(Vector3 targetPos)
        {


            return null;
        }

        class ScoutsHordeGenerator : HordeGenerator
        {
            public ScoutsHordeGenerator() : base("scouts")
            {
            }
        }

        class ScoutHordeGenerator : HordeGenerator
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
