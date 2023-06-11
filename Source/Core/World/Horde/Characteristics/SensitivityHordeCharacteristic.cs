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
            // TODO: blood moon scaling.
            //bool isBloodMoon = GameManager.Instance.World.GetAIDirector().BloodMoonComponent.BloodMoonActive;

            //return isBloodMoon ? this.sensitivity : this.sensitivity * 5.0f;
            return this.sensitivity;
        }
    }
}
