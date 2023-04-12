using UnityEngine;

namespace ImprovedHordes.Source.Horde.AI
{
    public interface IAIAgent
    {
        bool IsDead();
        Vector3 GetLocation();

        bool CanInterrupt();
        EntityAlive GetTarget();
        void MoveTo(Vector3 location, float dt);
    }
}