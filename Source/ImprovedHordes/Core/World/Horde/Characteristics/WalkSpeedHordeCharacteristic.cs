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
    }
}
