using UnityEngine;

namespace ImprovedHordes.Source.Horde.AI
{
    public interface IAIAgent
    {
        void MoveTo(Vector3 location, float dt);

        Vector3 GetLocation();
        EntityAlive GetTarget();

        bool IsDead();
    }
}