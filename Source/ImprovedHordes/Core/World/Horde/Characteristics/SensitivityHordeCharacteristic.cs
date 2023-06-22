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
            return this.sensitivity;
        }

        public override IData Load(IDataLoader loader)
        {
            this.sensitivity = loader.Load<float>();

            return this;
        }

        public override void Save(IDataSaver saver)
        {
            saver.Save<float>(this.sensitivity);
        }
    }
}
