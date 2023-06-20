using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.AI;

namespace ImprovedHordes.Core.World.Horde.Cluster.Data
{
    public sealed class HordeClusterData : IData
    {
        private IHorde horde;
        private float density;
        private IAICommandGenerator<EntityAICommand> entityCommandGenerator;

        public HordeClusterData() { }

        public HordeClusterData(IHorde horde, float density, IAICommandGenerator<EntityAICommand> entityCommandGenerator)
        {
            this.horde = horde;
            this.density = density;
            this.entityCommandGenerator = entityCommandGenerator;
        }

        public IData Load(IDataLoader loader)
        {
            this.horde = loader.Load<IHorde>();
            this.density = loader.Load<float>();
            this.entityCommandGenerator = loader.Load<IAICommandGenerator<EntityAICommand>>();

            return this;
        }

        public void Save(IDataSaver saver)
        {
            saver.Save<IHorde>(this.horde);
            saver.Save<float>(this.density);
            saver.Save<IAICommandGenerator<EntityAICommand>>(this.entityCommandGenerator);
        }

        public IHorde GetHorde() 
        {
            return this.horde;
        }

        public float GetDensity() 
        {
            return this.density;
        }

        public IAICommandGenerator<EntityAICommand> GetEntityCommandGenerator()
        {
            return this.entityCommandGenerator;
        }
    }
}
