using ImprovedHordes.Source.Core.Horde.World.Cluster;

namespace ImprovedHordes.Source.Core.Horde.Characteristics
{
    public sealed class WalkSpeedHordeCharacteristic : HordeCharacteristic<WalkSpeedHordeCharacteristic>
    {
        private float walkSpeed;

        public WalkSpeedHordeCharacteristic(float walkSpeed)
        {
            this.walkSpeed = walkSpeed;
        }

        public override void Merge(WalkSpeedHordeCharacteristic other)
        {
            this.walkSpeed += other.walkSpeed;
            this.walkSpeed /= 2;
        }

        public float GetWalkSpeed()
        {
            bool isDay = GameManager.Instance.World.IsDaytime();
            
            // TODO
            //int moveSpeedSetting = GamePrefs.GetInt(EnumGamePrefs.ZombieMoveNight);

            return this.walkSpeed * (isDay ? 1.0f : 2.0f);
        }
    }
}
