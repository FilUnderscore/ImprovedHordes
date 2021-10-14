using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace ImprovedHordes
{
    class HarmonyModPatches
    {
        [HarmonyPatch(typeof(AIDirectorWanderingHordeComponent))]
        [HarmonyPatch("Tick")]
        class WanderingHordeSpawnHook
        {
            static bool Prefix(double _dt)
            {
                // Prevent hordes from spawning at all.
                return false;
            }
        }
    }
}
