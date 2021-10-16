using HarmonyLib;
using UnityEngine;

namespace ImprovedHordes.Horde.Scout
{
    class ScoutHordeManager
    {
        public static void SpawnScouts(Vector3 targetPos)
        {
            // TODO Pass to scout spawner.
        }

        public class ScoutsHordeGenerator : HordeGenerator
        {
            public ScoutsHordeGenerator() : base("scouts")
            {
            }

            public override Horde GenerateHorde(PlayerHordeGroup group)
            {
                // TODO.
                return null;
            }
        }

        public class ScoutHordeGenerator : HordeGenerator
        {
            public ScoutHordeGenerator() : base("scout")
            {
            }

            public override Horde GenerateHorde(PlayerHordeGroup group)
            {
                return null;
            }
        }

        public class HarmonyPatches
        {
            [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
            [HarmonyPatch("SpawnScouts")]
            class SpawnScoutsHook
            {
                static bool Prefix(Vector3 targetPos)
                {
                    // Prevent default scout horde from spawning.
                    SpawnScouts(targetPos);

                    return false;
                }
            }
        }
    }
}
