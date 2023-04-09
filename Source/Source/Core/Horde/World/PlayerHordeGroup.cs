namespace ImprovedHordes.Source.Core.Horde.World
{
    public sealed class PlayerHordeGroup
    {
        private int gamestageSum;
        private int count;

        public PlayerHordeGroup()
        {
            this.gamestageSum = 0;
            this.count = 0;
        }

        public void AddPlayer(int gamestage)
        {
            this.gamestageSum += gamestage;
            this.count += 1;
        }

        public int GetGamestage()
        {
            if (this.count == 0)
                return 0;

            return this.gamestageSum / this.count;
        }
    }
}
