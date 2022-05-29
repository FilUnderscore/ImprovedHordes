using System;
using System.Collections.Generic;
using UnityEngine;

using ImprovedHordes.Horde.AI.Events;
using CustomModManager.API;

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

        private static int s_sense_dist = 80;
        private static float s_threshold = 20f;
        private static float s_wander_time = 90f;

        public static int SENSE_DIST
        {
            get
            {
                return s_sense_dist;
            }
        }

        public static float THRESHOLD
        {
            get
            {
                return s_threshold;
            }
        }

        public static float WANDER_TIME
        {
            get
            {
                return s_wander_time;
            }
        }

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

        public void HookSettings(ModManagerAPI.ModSettings modSettings)
        {
            modSettings.CreateTab("hordeAISettingsTab", "IHxuiHordeAISettingsTab");

            modSettings.Hook("hordeAISenseDist", "IHxuiHordeAISenseDistModSetting", value => s_sense_dist = value, () => s_sense_dist, toStr => (toStr.ToString(), toStr.ToString() + "m"), str =>
            {
                bool success = int.TryParse(str, out int val) && val > 0;
                return (val, success);
            }).SetTab("hordeAISettingsTab");

            modSettings.Hook("hordeAISenseThreshold", "IHxuiHordeAISenseThresholdModSetting", value => s_threshold = value, () => s_threshold, toStr => (toStr.ToString(), toStr.ToString()), str =>
            {
                bool success = float.TryParse(str, out float val) && val > 0;
                return (val, success);
            }).SetTab("hordeAISettingsTab");

            modSettings.Category("hordeAIAdvancedSettingsCategory", "IHxuiHordeAIAdvancedSettingsCategory").SetTab("hordeAISettingsTab");

            modSettings.Hook("hordeAIWanderTime", "IHxuiHordeAIWanderTimeModSetting", value => s_wander_time = value, () => s_wander_time, toStr => (toStr.ToString(), toStr.ToString() + " Tick" + (toStr == 1 ? "" : "s")), str =>
            {
                bool success = float.TryParse(str, out float val) && val >= 0;
                return (val, success);
            }).SetTab("hordeAISettingsTab");
        }

        public void ReadSettings(Settings settings)
        {
            s_sense_dist = settings.GetInt("sense_dist", 1, false, 80);
            s_threshold = settings.GetFloat("threshold", 0, false, 20f);
            s_wander_time = settings.GetFloat("wander_time", 0f, false, 90f);
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
