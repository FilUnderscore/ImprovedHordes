using ImprovedHordes.Horde;
using ImprovedHordes.Horde.AI.Commands;
using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.Wandering.AI.Commands;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ImprovedHordes.Horde.Data;

namespace ImprovedHordes.Command
{
    internal class ImprovedHordesSpawnSubcommand : ExecutableSubcommandBase
    {
        private GeneralHordeSpawner hordeSpawner;

        public ImprovedHordesSpawnSubcommand() : base("spawn")
        {
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            string hordeType = args[0];
            string hordeName = args[1];
            int groupDistance = int.Parse(args[2]);
            bool all = bool.Parse(args[3]);
            int playerId = args.Count >= 5 ? int.Parse(args[4]) : -1;

            if(HordesList.hordes.TryGetValue(hordeType, out HordeGroupList hordeGroupList))
            {
                if(hordeGroupList.hordes.TryGetValue(hordeName, out HordeGroup hordeGroup))
                {
                    if (hordeSpawner == null)
                        hordeSpawner = new GeneralHordeSpawner(ImprovedHordesManager.Instance);

                    hordeSpawner.SetGroupDistance(groupDistance);

                    if (!all)
                    {
                        if (playerId == -1)
                        {
                            // Spawn for player that executed command.

                            if (_senderInfo.IsLocalGame)
                            {
                                hordeSpawner.StartSpawningFor(hordeSpawner.GetHordeGroupNearPlayer(ImprovedHordesManager.Instance.World.GetPrimaryPlayer()), false, hordeGroup);
                                message = $"Spawning horde type {hordeType} horde group {hordeName}.";
                            }
                            else
                            {
                                message = $"Player ID must be specified when spawning specific hordes from console.";
                            }
                        }
                        else
                        {
                            // Spawn for player targeted.
                            EntityPlayer player = ImprovedHordesManager.Instance.World.GetPlayers().First(playerEntity => playerEntity.entityId == playerId);

                            if (player != null)
                            {
                                hordeSpawner.StartSpawningFor(hordeSpawner.GetHordeGroupNearPlayer(player), false, hordeGroup);
                                message = $"Spawning horde type {hordeType} horde group {hordeName} for player {playerId}.";
                            }
                            else
                            {
                                message = $"Player with id {playerId} could not be found!";
                            }
                        }
                    }
                    else
                    {
                        // Spawn for all players.
                        hordeSpawner.StartSpawningFor(hordeSpawner.GetAllHordeGroups(), false, hordeGroup);
                        message = $"Spawning horde type {hordeType} horde group {hordeName} for all players.";
                    }
                }
                else
                {
                    message = $"Horde group {hordeName} does not exist for horde type {hordeType}!";
                }
            }
            else
            {
                message = $"Horde type {hordeType} does not exist!";
            }

            return false;
        }

        public override (string name, bool optional)[] GetArgs()
        {
            return new (string name, bool optional)[] 
            {
                ("horde type", false),
                ("hordegroup name", false),
                ("group distance", false),
                ("all", false),
                ("player id", true)
            };
        }

        public override string GetDescription()
        {
            return "Spawn a specific horde group. If all is true, the horde will spawn for all players. If all is false, the horde will spawn for the given player id. If no player id is provided and all is false, the horde will spawn for the player that executed the command.";
        }

        internal static GeneralHordeGenerator GENERAL_HORDE_GENERATOR = new GeneralHordeGenerator();

        internal class GeneralHordeGenerator : HordeGenerator
        {
            public GeneralHordeGenerator() : base(null)
            {
            }
        }

        internal class GeneralHordeSpawner : HordeSpawner
        {
            public GeneralHordeSpawner(ImprovedHordesManager manager) : base(manager, GENERAL_HORDE_GENERATOR)
            {

            }

            private int groupDistance = 0;
            public void SetGroupDistance(int groupDistance)
            {
                this.groupDistance = groupDistance;
            }

            public override int GetGroupDistance()
            {
                return this.groupDistance;
            }

            protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
            {
                List<HordeAICommand> commands = new List<HordeAICommand>();
                const int DEST_RADIUS = 10;
                float wanderTime = HordeAIManager.WANDER_TIME + this.manager.Random.RandomFloat * 4f;

                commands.Add(new HordeAICommandDestinationMoving(() => group.CalculateAverageGroupPosition(true), DEST_RADIUS * 2));
                commands.Add(new HordeAICommandWander(wanderTime));
                commands.Add(new HordeAICommandDestination(GetRandomNearbyPosition(horde.targetPosition, DEST_RADIUS), DEST_RADIUS));

                AstarManager.Instance.AddLocation(entity.position, 64);
                horde.aiHorde.AddEntity(entity, true, commands);
            }
        }
    }
}
