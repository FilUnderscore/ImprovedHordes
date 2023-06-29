using ImprovedHordes.Core.AI;

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

        float GetSeeDistance();
        void SetTarget(EntityPlayer player); // TODO rewrite
    }
}