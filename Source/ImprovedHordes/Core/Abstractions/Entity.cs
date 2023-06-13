using ImprovedHordes.Core.AI;

namespace ImprovedHordes.Core.Abstractions
{
    public interface IEntity : IAIAgent
    {
        int GetEntityId();
        int GetEntityClassId();

        void PlaySound(string soundName);
        string GetAlertSound();

        bool IsStunned();
        bool IsPlayer();
    }
}