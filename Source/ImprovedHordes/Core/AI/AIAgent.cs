using ImprovedHordes.Core.Abstractions.World;
using UnityEngine;

namespace ImprovedHordes.Core.AI
{
    public interface IAIAgent
    {
        void MoveTo(Vector3 location, float dt);

        Vector3 GetLocation();
        IEntity GetTarget();
        
        bool IsDead();

        void Sleep();
        void WakeUp();

        bool IsSleeping();

        bool AnyPlayersNearby(out float distance, out EntityPlayer nearby);
    }
}