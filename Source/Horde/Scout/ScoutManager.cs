using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Events;
using ImprovedHordes.Horde.Scout.AI.Commands;

using static ImprovedHordes.Utils.Logger;

using CustomModManager.API;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutManager : IManager
    {
        private int s_chunk_radius = 4, s_max_scout_hordes_active_per_player_group = 3;
        private bool s_enabled = true;

        public int CHUNK_RADIUS
        {
            get
            {
                return s_chunk_radius;
            }
        }

        public int MAX_SCOUT_HORDES_ACTIVE_PER_PLAYER_GROUP
        {
            get
            {
                return s_max_scout_hordes_active_per_player_group;
            }
        }

        public bool ENABLED
        {
            get
            {
                return s_enabled;
            }
        }

        private readonly Dictionary<HordeAIHorde, Dictionary<HordeAIEntity, Scout>> scouts = new Dictionary<HordeAIHorde, Dictionary<HordeAIEntity, Scout>>();
        
        public readonly ImprovedHordesManager manager;
        private readonly ScoutSpawner spawner;
        private readonly ScoutHordeSpawner hordeSpawner;

        private readonly Dictionary<HordeAIHorde, Scout> currentScoutZombieHordesSpawned = new Dictionary<HordeAIHorde, Scout>();

        public ScoutManager(ImprovedHordesManager manager)
        {
            this.manager = manager;
            this.spawner = new ScoutSpawner(this);
            this.hordeSpawner = new ScoutHordeSpawner(this);

            this.manager.AIManager.OnHordeAIEntitySpawned += OnScoutEntitySpawned;
            this.manager.AIManager.OnHordeKilled += OnScoutHordeKilled;
            this.manager.AIManager.OnHordeKilled += OnScoutZombieHordeKilled;
        }

        public void ReadSettings(Settings settings)
        {
            this.s_enabled = settings.GetBool("enabled", true);
            this.s_chunk_radius = settings.GetInt("chunk_radius", GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance), true, GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance));
            this.s_max_scout_hordes_active_per_player_group = settings.GetInt("max_scout_hordes_active_per_player_group", 0, false, 3);
        }

        public void HookSettings(ModManagerAPI.ModSettings modSettings)
        {
            modSettings.CreateTab("hordeScoutSettingsTab", "IHxuiHordeScoutSettingsTab");

            modSettings.Hook<bool>("hordeScoutEnabled", "IHxuiHordeEnabledModSetting", value => this.s_enabled = value, () => this.s_enabled, toStr => (toStr.ToString(), toStr.ToString()), str =>
            {
                bool success = bool.TryParse(str, out bool val);
                return (val, success);
            }).SetTab("hordeScoutSettingsTab").SetAllowedValues(new bool[] { true, false });

            modSettings.Hook<int>("hordeScoutChunkRadius", "IHxuiHordeScoutChunkRadiusModSetting", value => this.s_chunk_radius = value, () => this.s_chunk_radius, toStr => (toStr.ToString(), toStr.ToString() + " Chunk" + (toStr > 1 ? "s" : "")), str =>
            {
                bool success = int.TryParse(str, out int val);
                return (val, success);
            }).SetTab("hordeScoutSettingsTab").SetMinimumMaximumAndIncrementValues(1, GamePrefs.GetInt(EnumGamePrefs.OptionsGfxViewDistance), 1);

            modSettings.Hook<int>("hordeScoutMaxScoutHordesActivePerPlayerGroup", "IHxuiHordeScoutMaxScoutHordesActivePerPlayerGroupModSetting", value => this.s_max_scout_hordes_active_per_player_group = value, () => this.s_max_scout_hordes_active_per_player_group, toStr => (toStr.ToString(), toStr.ToString() + " Horde" + (toStr > 1 ? "s" : "")), str =>
            {
                bool success = int.TryParse(str, out int val) && val >= 0;
                return (val, success);
            }).SetTab("hordeScoutSettingsTab");
        }

        public void OnScoutEntitySpawned(object sender, HordeEntitySpawnedEvent e)
        {
            if(!IsScoutHorde(e.horde.GetHordeInstance()) && !IsScoutSpawnedZombieHorde(e.horde.GetHordeInstance()) && !IsScreamerZombie(e.entity.entity))
                return;

            Scout scout = new Scout(e.entity, e.horde);
            bool registerHorde = !this.scouts.ContainsKey(e.horde);

            if (registerHorde)
                this.scouts.Add(e.horde, new Dictionary<HordeAIEntity, Scout>());
            
            this.scouts[e.horde].Add(e.entity, scout);

            e.entity.commands.Add(new HordeAICommandScout(this, scout, IsScreamerZombie(e.entity.entity) || IsScoutHorde(e.horde.GetHordeInstance())));
            e.entity.commands.RemoveRange(0, e.entity.commands.Count - 1); // Make Scout command the only command.
        
            if(registerHorde)
            {
                e.horde.OnHordeEntityKilled += OnScoutEntityKilled;
                e.horde.OnHordeEntityDespawned += OnScoutEntityDespawned;
                this.manager.AIManager.OnHordeKilled += OnScoutHordeKilled;
            }
        }

        private bool IsScoutHorde(Horde horde)
        {
            return horde.group.list.type.EqualsCaseInsensitive("Scouts");
        }

        private bool IsScoutSpawnedZombieHorde(Horde horde)
        {
            return horde.group.list.type.EqualsCaseInsensitive("Scout");
        }

        private bool IsScreamerZombie(EntityAlive entity)
        {
            return entity.entityClass == EntityClass.FromString("zombieScreamer") // Screamers are always scouts.
                    || entity.entityClass == EntityClass.FromString("zombieScreamerFeral")
                    || entity.entityClass == EntityClass.FromString("zombieScreamerRadiated");
        }

        public void OnScoutEntityKilled(object sender, HordeEntityKilledEvent e)
        {
            if (this.scouts.ContainsKey(e.horde) && this.scouts[e.horde].ContainsKey(e.entity))
            {
                Log("[Scout] Scout entity {0} was killed.", e.entity.GetEntityId());

                Scout scout = this.scouts[e.horde][e.entity];
                scout.state = EScoutState.DEAD;
            }
        }

        public void OnScoutHordeKilled(object sender, HordeKilledEvent e)
        {
            if (!scouts.ContainsKey(e.horde))
                return;

            Log("[Scout] Scout horde for group {0} has ended.", e.horde.GetHordeInstance().playerGroup);

            scouts.Remove(e.horde);
        }

        public void OnScoutZombieHordeKilled(object sender, HordeKilledEvent e) // Fix for scout zombie hordes spawning infinitely.
        {
            if(e.horde.GetHordeInstance().group.list.type.EqualsCaseInsensitive("Scout"))
            {
                if (currentScoutZombieHordesSpawned.ContainsKey(e.horde))
                    currentScoutZombieHordesSpawned.Remove(e.horde);
            }
        }

        public void OnScoutEntityDespawned(object sender, HordeEntityDespawnedEvent e)
        {
            if(this.scouts.ContainsKey(e.horde) && this.scouts[e.horde].ContainsKey(e.entity))
            {
                Log("[Scout] Scout entity {0} was despawned.", e.entity.GetEntityId());

                Scout scout = this.scouts[e.horde][e.entity];
                scout.state = EScoutState.DESPAWNED;
            }
        }

        public void SpawnScouts(Vector3 targetPos)
        {
            EntityPlayer closest = this.manager.World.GetClosestPlayer(targetPos, -1, false);
            PlayerHordeGroup group = this.spawner.GetHordeGroupNearPlayer(closest);

            float chance = Mathf.Clamp(ImprovedHordesManager.Instance.HeatTracker.GetHeatForGroup(group), 0.0f, 0.75f);

            // Scale feral scouts based on heat.
            bool feral = this.manager.Random.RandomFloat < chance; // From 0% chance to 75% depending on heat.

            this.spawner.StartSpawningFor(group, feral, targetPos);
        }

        public void NotifyScoutSpawnedHorde(Scout scout, HordeAIHorde horde)
        {
            this.currentScoutZombieHordesSpawned.Add(horde, scout);
        }

        public void TrySpawnScoutHorde(EntityPlayer target, Scout scout)
        {
            int count = GetCurrentSpawnedScoutHordesCount(target.position);

            if (count < MAX_SCOUT_HORDES_ACTIVE_PER_PLAYER_GROUP)
            {
                this.hordeSpawner.StartSpawningFor(target, scout, false, target.position);
                manager.HeatTracker.Request(target.position, count * 4f);
            }
        }

        public int GetCurrentSpawnedScoutHordesCount(Vector3 position)
        {
            int count = 0;

            foreach(var scout in GetScoutsNear(position, false))
            {
                count += currentScoutZombieHordesSpawned.Count(entry => entry.Value == scout);
            }

            return count;
        }

        public void NotifyScoutsNear(Vector3i targetBlockPos, float value)
        {
            Vector3 targetPos = new Vector3(targetBlockPos.x, targetBlockPos.y, targetBlockPos.z);
            
            foreach (var scout in GetScoutsNear(targetPos))
            {
                if(scout == null)
                {
                    Warning("Scout was not properly removed.");

                    continue;
                }
                else if (scout.state != EScoutState.ALIVE)
                    continue;

                if (scout.aiEntity.entity.GetAttackTarget() != null && scout.aiEntity.entity.GetAttackTarget() is EntityPlayer) // Don't interrupt a scout that is already attacking something.
                    continue;

                scout.Interrupt(targetPos, value);

#if DEBUG
                Log("[Scout] Scout {0} was drawn to {1}.", scout.aiEntity.GetEntityId(), targetPos);
#endif
            }
        }

        public void Update()
        {
            if (!this.manager.PlayerManager.AnyPlayers())
                return;

            this.spawner.Update();
            this.hordeSpawner.Update();
        }

        public List<Scout> GetScoutsNear(Vector3 targetPos, bool alive = true)
        {
            int chunkDist = 16 * this.CHUNK_RADIUS;

            List<Scout> nearby = new List<Scout>();

            foreach(var hordeEntry in scouts)
            {
                foreach (var scoutEntry in hordeEntry.Value)
                {
                    var entity = scoutEntry.Key;
                    var scout = scoutEntry.Value;

                    if (scout == null || (alive && scout.state != EScoutState.ALIVE))
                        continue;

                    Vector3 scoutPos = entity.entity.position;

                    if (Vector2.Distance(new Vector2(scoutPos.x, scoutPos.z), new Vector2(targetPos.x, targetPos.z)) <= chunkDist)
                    {
                        nearby.Add(scout);
                    }
                }
            }

            return nearby;
        }

        public void Shutdown()
        {
            this.currentScoutZombieHordesSpawned.Clear();
            this.hordeSpawner.Shutdown();
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
                    if (!ImprovedHordesMod.IsHost() || !ImprovedHordesManager.Instance.ScoutManager.ENABLED)
                        return true;

                    var scoutManager = ImprovedHordesManager.Instance.ScoutManager;
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
                    if (!ImprovedHordesMod.IsHost() || !ImprovedHordesManager.Instance.ScoutManager.ENABLED)
                        return;

                    // Notify scouts in chunk of the new event to investigate.
                    if(_chunkEvent.EventType != EnumAIDirectorChunkEvent.Torch)
                    {
                        var scoutManager = ImprovedHordesManager.Instance.ScoutManager;
                        scoutManager.NotifyScoutsNear(_chunkEvent.Position, _chunkEvent.Value);
                    }
                }
            }
        }
    }
}
