using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Data.XML;
using ImprovedHordes.Implementations.World.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Data
{
    public sealed class HordeDefinitionEntityGenerator : HordeEntityGenerator
    {
        private const string PLACEHOLDER_ENTITY_CLASS = "zombieSpider";

        private readonly Core.Abstractions.Logging.ILogger logger;

        private readonly HordeDefinition.Group group;
        private readonly Dictionary<HordeDefinition.Group.Entity, int> maxEntitiesToSpawn = new Dictionary<HordeDefinition.Group.Entity, int>();

        private int lastEntityClassId;

        public HordeDefinitionEntityGenerator(ILoggerFactory loggerFactory, PlayerHordeGroup playerGroup, IRandom random, HordeDefinition definition) : base(playerGroup)
        {
            this.logger = loggerFactory.Create(typeof(HordeDefinitionEntityGenerator));
            this.group = definition.GetEligibleRandomGroup(playerGroup, random);

            if (this.group != null)
                this.CalculateEntitiesToSpawn(random);
            else
                this.logger.Warn($"No eligible '{definition.GetHordeType()}' horde groups for player group {playerGroup}. Using placeholder entity class '{PLACEHOLDER_ENTITY_CLASS}'.");
        }

        private void CalculateEntitiesToSpawn(IRandom random)
        {
            var eligibleGroupEntities = group.GetEligible(this.playerGroup, random);

            if (eligibleGroupEntities == null || eligibleGroupEntities.Count == 0) // Failed to get without chance. Ignore chances.
                eligibleGroupEntities = group.GetEligible(this.playerGroup, null);

            foreach(var entity in eligibleGroupEntities)
            {
                maxEntitiesToSpawn.Add(entity, entity.GetCount(this.playerGroup.GetGamestage()));
            }
        }

        public override int DetermineEntityCount(float density)
        {
            return Math.Max(1, Mathf.CeilToInt(maxEntitiesToSpawn.Sum(entityDefinitionEntry => entityDefinitionEntry.Value) * density));
        }

        private HordeDefinition.Group.Entity GetRandomEntity(IRandom random)
        {
            if (maxEntitiesToSpawn.Count == 0)
                return null;

            var keys = maxEntitiesToSpawn.Keys.ToList();
            HordeDefinition.Group.Entity randomEntity = random.Random(keys);

            if (maxEntitiesToSpawn[randomEntity] <= 0 && maxEntitiesToSpawn.Count > 1)
            {
                maxEntitiesToSpawn.Remove(randomEntity);
                return GetRandomEntity(random);
            }

            maxEntitiesToSpawn[randomEntity]--;
            return randomEntity;
        }

        public override int GetEntityClassId(IRandom random)
        {
            if (this.group == null)
                return EntityClass.FromString(PLACEHOLDER_ENTITY_CLASS);

            HordeDefinition.Group.Entity randomEntity = GetRandomEntity(random);

            if(!(random is ImprovedHordesWorldRandom ihRandom) || randomEntity == null || !randomEntity.GetEntityClassId(ref this.lastEntityClassId, out int entityClassId, ihRandom.GetGameRandom()))
            {
                this.logger.Warn($"Could not get entity class ID for Horde Entity. Perhaps the entity class is invalid or the entity group does not exist?");
                return EntityClass.FromString(PLACEHOLDER_ENTITY_CLASS);
            }

            return entityClassId;
        }

        public override bool IsStillValidFor(PlayerHordeGroup playerGroup)
        {
            return this.group.IsEligible(playerGroup, null, null);
        }
    }
}
