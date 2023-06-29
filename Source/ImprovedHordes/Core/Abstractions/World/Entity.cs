using ImprovedHordes.Core.AI;
using UnityEngine;

namespace ImprovedHordes.Core.Abstractions.World
{
    public interface IEntity : IAIAgent
    {
        int GetEntityId();
        int GetEntityClassId();

        void PlaySound(string soundName);
        string GetAlertSound();

        bool IsStunned();
        bool IsPlayer();

        bool CanSee(Vector3 pos);
        void SetTarget(EntityPlayer player); // TODO rewrite
    }
}