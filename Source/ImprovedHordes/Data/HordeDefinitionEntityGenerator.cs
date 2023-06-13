using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Data.XML;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Data
{
    public sealed class HordeDefinitionEntityGenerator : HordeEntityGenerator
    {
        private const string PLACEHOLDER_ENTITY_CLASS = "zombieSpider";

        private readonly HordeDefinition.Group group;
        private readonly Dictionary<HordeDefinition.Group.Entity, int> maxEntitiesToSpawn = new Dictionary<HordeDefinition.Group.Entity, int>();

        private int lastEntityId;

        public HordeDefinitionEntityGenerator(PlayerHordeGroup playerGroup, HordeDefinition definition) : base(playerGroup)
        {
            this.group = definition.GetEligibleRandomGroup(playerGroup);
            
            if (this.group != null)
                this.CalculateEntitiesToSpawn();
            else
                Log.Error($"[Improved Hordes] No eligible '{definition.GetHordeType()}' horde groups for player group {playerGroup}. Using placeholder entity class '{PLACEHOLDER_ENTITY_CLASS}'.");
        }

        private void CalculateEntitiesToSpawn()
        {
            var eligibleGroupEntities = group.GetEligible(this.playerGroup);

            if (eligibleGroupEntities == null)
                return;

            foreach(var entity in eligibleGroupEntities)
            {
                maxEntitiesToSpawn.Add(entity, entity.GetCount(this.playerGroup.GetGamestage()));
            }
        }

        public override int DetermineEntityCount(float density)
        {
            return Mathf.RoundToInt(maxEntitiesToSpawn.Sum(entityDefinitionEntry => entityDefinitionEntry.Value) * density);
        }

        private HordeDefinition.Group.Entity GetRandomEntity(GameRandom random)
        {
            var keys = maxEntitiesToSpawn.Keys.ToList();
            HordeDefinition.Group.Entity randomEntity = keys[random.RandomRange(keys.Count)];

            if (maxEntitiesToSpawn[randomEntity] <= 0 && maxEntitiesToSpawn.Count > 1)
            {
                maxEntitiesToSpawn.Remove(randomEntity);
                return GetRandomEntity(random);
            }

            maxEntitiesToSpawn[randomEntity]--;
            return randomEntity;
        }

        public override int GetEntityClassId(GameRandom random)
        {
            if (this.group == null)
                return EntityClass.FromString(PLACEHOLDER_ENTITY_CLASS);

            HordeDefinition.Group.Entity randomEntity = GetRandomEntity(random);

            return randomEntity.GetEntityId(ref this.lastEntityId, random);
        }

        public override bool IsStillValidFor(PlayerHordeGroup playerGroup)
        {
            return this.group.IsEligible(playerGroup, true);
        }
    }
}
