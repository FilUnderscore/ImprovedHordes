using ImprovedHordes.Core.Abstractions.Data;

namespace ImprovedHordes.Core.World.Horde.Characteristics
{
    public sealed class WalkSpeedHordeCharacteristic : HordeCharacteristic<WalkSpeedHordeCharacteristic>
    {
        private float dayWalkSpeed;
        private float nightWalkSpeed;

        public WalkSpeedHordeCharacteristic(float dayWalkSpeed, float nightWalkSpeed)
        {
            this.dayWalkSpeed = dayWalkSpeed;
            this.nightWalkSpeed = nightWalkSpeed;
        }

        public override void Merge(WalkSpeedHordeCharacteristic other)
        {
            this.dayWalkSpeed += other.dayWalkSpeed;
            this.nightWalkSpeed += other.nightWalkSpeed;

            this.dayWalkSpeed /= 2;
            this.nightWalkSpeed /= 2;
        }

        public float GetWalkSpeed()
        {
            bool isDay = GameManager.Instance.World.IsDaytime();
            return isDay ? this.dayWalkSpeed : this.nightWalkSpeed;
        }

        public override IData Load(IDataLoader loader)
        {
            this.dayWalkSpeed = loader.Load<float>();
            this.nightWalkSpeed = loader.Load<float>();

            return this;
        }

        public override void Save(IDataSaver saver)
        {
            saver.Save<float>(this.dayWalkSpeed);
            saver.Save<float>(this.nightWalkSpeed);
        }
    }
}
