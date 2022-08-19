using ImprovedHordes.Horde;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ImprovedHordes.Command
{
    internal class ImprovedHordesStatsSubcommand : ExecutableSubcommandBase
    {
        public ImprovedHordesStatsSubcommand() : base("stats")
        {

        }

        public override bool Execute(List<string> args, CommandSenderInfo senderInfo, ref string message)
        {
            var playerManager = ImprovedHordesManager.Instance.PlayerManager;

            StringBuilder builder = new StringBuilder();

            int player = ImprovedHordesManager.Instance.World.GetPrimaryPlayerId();

            if (senderInfo.RemoteClientInfo != null)
                player = senderInfo.RemoteClientInfo.entityId;

            HordePlayer hordePlayer = playerManager.GetPlayer(player);

            Vector3 pos = hordePlayer.playerEntityInstance.position;
            Vector2i chunk = World.toChunkXZ(pos);
            builder.AppendLine("Chunk " + chunk + " Heat: " + ImprovedHordesManager.Instance.HeatTracker.GetHeatInChunk(chunk));

            if (ImprovedHordesManager.Instance.HeatPatrolManager.GetAreaPatrolTime(pos, out ulong time))
            {
                (int days, int hours, int minutes) = GameUtils.WorldTimeToElements(time);
                builder.AppendLine($"Area: " + ImprovedHordesManager.Instance.HeatPatrolManager.GetAreaFromChunk(chunk));
                builder.AppendLine($"Patrol time: Day {days} {hours}:{minutes}");
            }

            message = builder.ToString();
            return false;
        }

        public override (string name, bool optional)[] GetArgs()
        {
            return null;
        }

        public override string GetDescription()
        {
            return "Shows information regarding the area (such as patrol time / heat).";
        }
    }
}
