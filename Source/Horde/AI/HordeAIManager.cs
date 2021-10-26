using System;
using System.Collections.Generic;
using UnityEngine;

using System.Reflection;
using HarmonyLib;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde.AI.Events;

namespace ImprovedHordes.Horde.AI
{
    public sealed class HordeAIManager
    {
        public event EventHandler<HordeEntitySpawnedEvent> OnHordeAIEntitySpawned;
        public event EventHandler<HordeKilledEvent> OnHordeKilled;
        
        private readonly Dictionary<Horde, HordeAIHorde> trackedHordes = new Dictionary<Horde, HordeAIHorde>();
        private readonly List<HordeAIHorde> hordesToRemove = new List<HordeAIHorde>();

        public readonly Dictionary<Entity, Entity> entityKilledQueue = new Dictionary<Entity, Entity>();

        public int GetHordesAlive()
        {
            return trackedHordes.Count;
        }

        public HordeAIHorde GetAIHorde(Horde horde)
        {
            return trackedHordes.ContainsKey(horde) ? trackedHordes[horde] : null;
        }

        public void Add(EntityAlive entity, Horde horde, bool despawnOnCompletion, List<HordeAICommand> commands)
        {
            HordeAIHorde aiHorde;

            if ((aiHorde = GetAIHorde(horde)) == null)
                trackedHordes.Add(horde, aiHorde = new HordeAIHorde(horde));

            HordeAIEntity hordeAIEntity = new HordeAIEntity(entity, despawnOnCompletion, commands);
            aiHorde.AddEntity(hordeAIEntity);

            OnHordeAIEntitySpawnedEvent(hordeAIEntity, aiHorde);
        }

        public void Update()
        {
            float dt = Time.fixedDeltaTime;

            foreach (var horde in trackedHordes.Values)
            {
                EHordeAIHordeUpdateState hordeUpdateState = horde.Update(dt);

                switch(hordeUpdateState)
                {
                    case EHordeAIHordeUpdateState.ALIVE:
                        continue;
                    case EHordeAIHordeUpdateState.DEAD:
                        OnHordeKilledEvent(horde);
                        break;
                }
            }

            foreach(var horde in hordesToRemove)
            {
                trackedHordes.Remove(horde.GetHordeInstance());
            }

            if(hordesToRemove.Count > 0)
                hordesToRemove.Clear();
        }

        private void OnHordeKilledEvent(HordeAIHorde horde)
        {
            this.OnHordeKilled?.Invoke(this, new HordeKilledEvent(horde));
        }

        private void OnHordeAIEntitySpawnedEvent(HordeAIEntity entity, HordeAIHorde horde)
        {
            this.OnHordeAIEntitySpawned?.Invoke(this, new HordeEntitySpawnedEvent(entity, horde));
        }

        public void EntityKilled(Entity killed, Entity killer)
        {
            if (entityKilledQueue.ContainsKey(killed))
                return;

            entityKilledQueue.Add(killed, killer);
        }
    }
}
