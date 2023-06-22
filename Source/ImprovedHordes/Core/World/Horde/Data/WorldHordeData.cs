using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Core.World.Horde.Cluster;
using ImprovedHordes.Core.World.Horde.Spawn;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Data
{
    public sealed class WorldHordeData : IData
    {
        private Vector3 location;
        private HordeSpawnData spawnData;
        private List<HordeCluster> clusters;
        private HordeCharacteristics characteristics; // TODO temp
        private IAICommandGenerator<AICommand> commandGenerator;

        public WorldHordeData() { }

        public WorldHordeData(Vector3 location, HordeSpawnData spawnData, List<HordeCluster> clusters, HordeCharacteristics characteristics, IAICommandGenerator<AICommand> commandGenerator)
        {
            this.location = location;
            this.spawnData = spawnData;
            this.clusters = clusters;
            this.characteristics = characteristics;
            this.commandGenerator = commandGenerator;
        }

        public IData Load(IDataLoader loader)
        {
            this.location = loader.Load<Vector3>();
            this.spawnData = loader.Load<HordeSpawnData>();
            this.clusters = loader.Load<List<HordeCluster>>();
            this.characteristics = loader.Load<HordeCharacteristics>();
            this.commandGenerator= loader.Load<IAICommandGenerator<AICommand>>();

            return this;
        }

        public void Save(IDataSaver saver)
        {
            saver.Save<Vector3>(this.location);
            saver.Save<HordeSpawnData>(this.spawnData);
            saver.Save<List<HordeCluster>>(this.clusters);
            saver.Save<HordeCharacteristics>(this.characteristics);
            saver.Save<IAICommandGenerator<AICommand>>(this.commandGenerator);
        }

        public Vector3 GetLocation()
        {
            return this.location;
        }

        public HordeSpawnData GetSpawnData()
        {
            return this.spawnData;
        }

        public List<HordeCluster> GetClusters()
        {
            return this.clusters;
        }

        public HordeCharacteristics GetCharacteristics()
        {
            return this.characteristics;
        }

        public IAICommandGenerator<AICommand> GetCommandGenerator()
        {
            return this.commandGenerator;
        }
    }
}
