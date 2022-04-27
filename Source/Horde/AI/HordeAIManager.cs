using System;
using System.Collections.Generic;
using UnityEngine;

using ImprovedHordes.Horde.AI.Events;

namespace ImprovedHordes.Horde.AI
{
    public sealed class HordeAIManager : IManager
    {
        public event EventHandler<HordeEntitySpawnedEvent> OnHordeAIEntitySpawned;
        public event EventHandler<HordeKilledEvent> OnHordeKilled;
        
        private readonly Dictionary<Horde, HordeAIHorde> trackedHordes = new Dictionary<Horde, HordeAIHorde>();

        private readonly Dictionary<Horde, HordeAIHorde> hordesToAdd = new Dictionary<Horde, HordeAIHorde>();
        private readonly List<HordeAIHorde> hordesToRemove = new List<HordeAIHorde>();

        public readonly Dictionary<Entity, Entity> entityKilledQueue = new Dictionary<Entity, Entity>();

        public int GetHordesAlive()
        {
            return trackedHordes.Count;
        }

        public HordeAIHorde GetAsAIHorde(Horde horde)
        {
            HordeAIHorde aiHorde;

            if (!trackedHordes.ContainsKey(horde))
            {
                if (!hordesToAdd.ContainsKey(horde))
                    hordesToAdd.Add(horde, aiHorde = new HordeAIHorde(horde));
                else
                    aiHorde = hordesToAdd[horde];

                return aiHorde;
            }

            return trackedHordes[horde];
        }

        public void Update()
        {
            float dt = Time.fixedDeltaTime;

            foreach (var horde in trackedHordes.Values)
            {
                EHordeAIHordeUpdateState hordeUpdateState = horde.Update(dt);

                switch (hordeUpdateState)
                {
                    case EHordeAIHordeUpdateState.ALIVE:
                        continue;
                    case EHordeAIHordeUpdateState.DEAD:
                        OnHordeKilledEvent(horde);
                        hordesToRemove.Add(horde);
                        break;
                }
            }

            foreach (var horde in hordesToRemove)
            {
                trackedHordes.Remove(horde.GetHordeInstance());
            }

            if (hordesToRemove.Count > 0)
                hordesToRemove.Clear();
        
            foreach (var horde in hordesToAdd)
            {
                trackedHordes.Add(horde.Key, horde.Value);
            }

            if (hordesToAdd.Count > 0)
                hordesToAdd.Clear();
        }

        private void OnHordeKilledEvent(HordeAIHorde horde)
        {
            this.OnHordeKilled?.Invoke(this, new HordeKilledEvent(horde));
        }

        public void OnHordeAIEntitySpawnedEvent(HordeAIEntity entity, HordeAIHorde horde)
        {
            this.OnHordeAIEntitySpawned?.Invoke(this, new HordeEntitySpawnedEvent(entity, horde));
        }

        public void EntityKilled(Entity killed, Entity killer)
        {
            if (entityKilledQueue.ContainsKey(killed))
                return;

            entityKilledQueue.Add(killed, killer);
        }

        public void Shutdown()
        {
            this.trackedHordes.Clear();
            this.hordesToAdd.Clear();
            this.hordesToRemove.Clear();
            this.entityKilledQueue.Clear();
        }
    }
}
