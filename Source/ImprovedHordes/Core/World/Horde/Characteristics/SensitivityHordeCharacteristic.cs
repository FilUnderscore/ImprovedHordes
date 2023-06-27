using ImprovedHordes.Core.Abstractions.Data;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Characteristics
{
    public sealed class SensitivityHordeCharacteristic : HordeCharacteristic<SensitivityHordeCharacteristic>
    {
        private float sensitivity;

        public SensitivityHordeCharacteristic(float sensitivity)
        {
            this.sensitivity = sensitivity;
        }

        public override void Merge(SensitivityHordeCharacteristic other)
        {
            this.sensitivity = Mathf.Max(this.sensitivity, other.sensitivity);
        }

        public float GetSensitivity()
        {
            // TODO blood moon scaling?
            return this.sensitivity + GetFeralSenseSensitivity();
        }

        private float GetFeralSenseSensitivity()
        {
            bool isDay = GameManager.Instance.World.IsDaytime();

            switch(GamePrefs.GetInt(EnumGamePrefs.ZombieFeralSense))
            {
                case 1:
                    return isDay ? 1.0f : 0.0f;
                case 2:
                    return !isDay ? 1.0f : 0.0f;
                case 3:
                    return 1.0f;
            }

            return 0.0f;
        }

        public override void Save(IDataSaver saver)
        {
            saver.Save<float>(this.sensitivity);
        }
    }
}
