using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Horde
{
    public class WanderingHorde : Horde
    {
        public List<Command> commandsList = new List<Command>();

        public WanderingHorde(HordeGroup group, int count, bool feral, int[] entities) : base(group, count, feral, entities)
        {
        }

        public WanderingHorde(Horde horde) : base(horde) { }

        public class Command
        {
            public EntityAlive entity;

            public Vector3 playerTarget;
            public Vector3 endTarget;
            public Vector3 currentTarget;

            public float PlayerWanderTime;

            public ZombieState state;
        }

        public enum ZombieState
        {
            Wandering,
            PlayerWander,
            End
        }
    }
}
