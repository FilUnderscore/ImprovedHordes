using ImprovedHordes.Source.Core.Horde.Data;
using ImprovedHordes.Source.Core.Horde.Data.XML;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public sealed class HordeDefinitionEntityGenerator : HordeEntityGenerator
    {
        private readonly HordeDefinition.Group group;
        private readonly Dictionary<HordeDefinition.Group.Entity, int> maxEntitiesToSpawn = new Dictionary<HordeDefinition.Group.Entity, int>();

        private int lastEntityId;

        public HordeDefinitionEntityGenerator(PlayerHordeGroup playerGroup, string type) : base(playerGroup)
        {
            this.group = HordesFromXml.GetHordeDefinition(type).GetEligibleRandomGroup(playerGroup);

            this.CalculateEntitiesToSpawn();
        }

        private void CalculateEntitiesToSpawn()
        {
            foreach(var entity in group.GetEligible(this.playerGroup))
            {
                maxEntitiesToSpawn.Add(entity, entity.GetCount(this.playerGroup.GetGamestage()));
            }
        }

        public override int DetermineEntityCount(float density)
        {
            return Mathf.RoundToInt(maxEntitiesToSpawn.Sum(entityDefinitionEntry => entityDefinitionEntry.Value) * density);
        }

        private HordeDefinition.Group.Entity GetRandomEntity()
        {
            HordeDefinition.Group.Entity randomEntity = maxEntitiesToSpawn.Keys.ToList().RandomObject();

            if (maxEntitiesToSpawn[randomEntity] <= 0 && maxEntitiesToSpawn.Count > 1)
            {
                maxEntitiesToSpawn.Remove(randomEntity);
                return GetRandomEntity();
            }

            maxEntitiesToSpawn[randomEntity]--;
            return randomEntity;
        }

        public override int GetEntityId()
        {
            HordeDefinition.Group.Entity randomEntity = GetRandomEntity();

            return randomEntity.GetEntityId(ref this.lastEntityId);
        }

        public override bool IsStillValidFor(PlayerHordeGroup playerGroup)
        {
            return this.group.IsEligible(playerGroup, true);
        }
    }
}
