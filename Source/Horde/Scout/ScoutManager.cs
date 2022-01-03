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
    public class ScoutManager : IManager
    {
        private int s_chunk_radius;
        private float s_feral_horde_chance_multiplier;

        public int CHUNK_RADIUS
        {
            get
            {
                return s_chunk_radius;
            }
        }

        public float FERAL_HORDE_CHANCE_MULTIPLIER
        {
            get
            {
                return s_feral_horde_chance_multiplier;
            }
        }

        private readonly Dictionary<HordeAIHorde, Dictionary<HordeAIEntity, Scout>> scouts = new Dictionary<HordeAIHorde, Dictionary<HordeAIEntity, Scout>>();
        
        public readonly ImprovedHordesManager manager;
        private readonly ScoutSpawner spawner;
        private readonly ScoutHordeSpawner hordeSpawner;

        private readonly HashSet<PlayerHordeGroup> currentScoutZombieHordesSpawned = new HashSet<PlayerHordeGroup>();

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
            this.s_chunk_radius = settings.GetInt("chunk_radius", GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance), true, GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance));
            this.s_feral_horde_chance_multiplier = settings.GetFloat("feral_horde_chance_multiplier", 0.0f, false, 1.0f);
        }

        public void OnScoutEntitySpawned(object sender, HordeEntitySpawnedEvent e)
        {
            if(!IsScoutHorde(e.horde.GetHordeInstance()) && !IsScoutSpawnedZombieHorde(e.horde.GetHordeInstance()))
                return;

            Scout scout = new Scout(e.entity, e.horde);

            if(!this.scouts.ContainsKey(e.horde))
                this.scouts.Add(e.horde, new Dictionary<HordeAIEntity, Scout>());

            this.scouts[e.horde].Add(e.entity, scout);

            e.entity.commands.Add(new HordeAICommandScout(this, e.entity, IsScreamerZombie(e.entity.entity) || IsScoutHorde(e.horde.GetHordeInstance())));
            e.entity.commands.RemoveRange(0, e.entity.commands.Count - 1); // Make Scout command the only command.

            e.horde.OnHordeEntityKilled += OnScoutEntityKilled;
            e.horde.OnHordeEntityDespawned += OnScoutEntityDespawned;
            this.manager.AIManager.OnHordeKilled += OnScoutHordeKilled;
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

                if (this.IsScoutHorde(e.horde.GetHordeInstance()) &&
                    e.killer != null && e.killer is EntityPlayer)
                {
                    EntityPlayer killer = e.killer as EntityPlayer;
                    
                    if (scout.aiHorde.GetHordeInstance().feral)
                    {
                        Log("[Scout] Player {0} killed feral scout.", killer.EntityName);
                        scout.killer = killer;
                    }
                }

                scout.state = EScoutState.DEAD;
            }
        }

        public void OnScoutHordeKilled(object sender, HordeKilledEvent e)
        {
            if (!scouts.ContainsKey(e.horde))
                return;

            Log("[Scout] Scout horde for group {0} has ended.", e.horde.GetHordeInstance().playerGroup);

            int totalKilled;
            // Surprise players with a horde called by the living scouts to avenge the killed scouts.
            if (IsScoutHorde(e.horde.GetHordeInstance()) && e.horde.GetHordeInstance().feral && (totalKilled = e.horde.GetStat(EHordeAIStats.TOTAL_KILLED)) > 0)
            {
                Log("[Scout] {0} feral scouts were killed. Attempting to spawn horde.", totalKilled);

                foreach(var scoutEntry in scouts[e.horde])
                {
                    var scout = scoutEntry.Value;

                    if(scout.state == EScoutState.DEAD && scout.killer != null)
                    {
                        this.TrySpawnScoutHorde(scout.killer);
                    }
                }
            }

            scouts.Remove(e.horde);
        }

        public void OnScoutZombieHordeKilled(object sender, HordeKilledEvent e) // Fix for scout zombie hordes spawning infinitely.
        {
            if(e.horde.GetHordeInstance().group.list.type.EqualsCaseInsensitive("Scout"))
            {
                if (currentScoutZombieHordesSpawned.Contains(e.horde.GetHordeInstance().playerGroup))
                    currentScoutZombieHordesSpawned.Remove(e.horde.GetHordeInstance().playerGroup);
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
            const float DIFFICULTY_MODIFIER = 720; // Max gamestage.

            EntityPlayer closest = this.manager.World.GetClosestPlayer(targetPos, -1, false);
            PlayerHordeGroup group = this.spawner.GetHordeGroupNearPlayer(closest);

            int groupGamestage = group.GetGroupGamestage();
            float chance = Mathf.Clamp((groupGamestage / DIFFICULTY_MODIFIER) * FERAL_HORDE_CHANCE_MULTIPLIER, 0.0f, 0.75f);

            // Scale feral scouts based on GS.
            bool feral = this.manager.Random.RandomFloat < chance; // From 0% chance to 75% depending on GS.

            this.spawner.StartSpawningFor(group, feral, targetPos);
        }

        public void TrySpawnScoutHorde(EntityPlayer target)
        {
            var playerHordeGroup = this.hordeSpawner.GetHordeGroupNearPlayer(target);

            if (!currentScoutZombieHordesSpawned.Contains(playerHordeGroup))
            {
                currentScoutZombieHordesSpawned.Add(playerHordeGroup);
                this.hordeSpawner.StartSpawningFor(target, false, target.position);
            }
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

                scout.Interrupt(targetPos, value);
                Log("[Scout] Scout {0} was drawn to {1}.", scout.aiEntity.GetEntityId(), targetPos);
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
            int chunkDist = 16 * this.CHUNK_RADIUS;

            List<Scout> nearby = new List<Scout>();

            foreach(var hordeEntry in scouts)
            {
                foreach (var scoutEntry in hordeEntry.Value)
                {
                    var entity = scoutEntry.Key;
                    var scout = scoutEntry.Value;

                    if (scout != null && scout.state != EScoutState.ALIVE)
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
