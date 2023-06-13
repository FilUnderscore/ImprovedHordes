namespace ImprovedHordes.Core.World.Horde
{
    public abstract class HordeEntityGenerator
    {
        protected PlayerHordeGroup playerGroup;
        
        public HordeEntityGenerator(PlayerHordeGroup playerGroup)
        {
            this.playerGroup = playerGroup;
        }

        public void SetPlayerGroup(PlayerHordeGroup playerGroup)
        {
            this.playerGroup = playerGroup;
        }

        public abstract bool IsStillValidFor(PlayerHordeGroup playerGroup);

        public abstract int GetEntityClassId(GameRandom random);

        public abstract int DetermineEntityCount(float density);
    }
}
