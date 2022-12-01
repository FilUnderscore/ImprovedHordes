using ImprovedHordes.Horde.Data;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.World
{
    public enum WorldEntityType
    {
        Animal,
        Enemy
    }

    public class WorldEntityDefinition
    {
        public readonly string Name;
        public readonly RuntimeEval.Value<float> chance;

        public readonly List<Entity> entities = new List<Entity>();

        public WorldEntityDefinition(string name, RuntimeEval.Value<float> chance)
        {
            this.Name = name;
            this.chance = chance;
        }

        public class Entity
        {
            public readonly GS gs;

            public readonly string EntityName;
            public readonly string EntityGroup;

            public readonly RuntimeEval.Value<HashSet<string>> biomes;
            public readonly RuntimeEval.Value<float> chance;

            public readonly RuntimeEval.Value<ETimeOfDay> timeOfDay;
            public readonly RuntimeEval.Value<POITags> tags;

            public Entity(GS gs, string entityName, string entityGroup, RuntimeEval.Value<HashSet<string>> biomes, RuntimeEval.Value<float> chance, RuntimeEval.Value<ETimeOfDay> timeOfDay, RuntimeEval.Value<POITags> tags)
            {
                this.gs = gs;
                this.EntityName = entityName;
                this.EntityGroup = entityGroup;
                this.biomes = biomes;
                this.chance = chance;
                this.timeOfDay = timeOfDay;
                this.tags = tags;
            }

            public string GetGroupOrName()
            {
                return this.EntityName != null ? this.EntityName : this.EntityGroup;
            }
        }

        public static WorldEntityType DetermineWorldEntityType(EntityAlive entity)
        {
            if (entity is EntityEnemy)
            {
                return WorldEntityType.Enemy;
            }
            else if (entity is EntityAnimal)
            {
                return WorldEntityType.Animal;
            }
            else
            {
                throw new InvalidOperationException($"WorldEntityType could not be determined for entity type {entity.GetType().Name}");
            }
        }
    }
}
