using System;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster.Characteristics
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
            this.sensitivity = Math.Max(this.sensitivity, other.sensitivity);
        }

        public float GetSensitivity()
        {
            return this.sensitivity;
        }
    }
}
