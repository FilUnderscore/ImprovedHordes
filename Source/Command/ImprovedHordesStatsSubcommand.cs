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
            Vector2i chunk = global::World.toChunkXZ(pos);
            builder.AppendLine("Chunk " + chunk + " Heat: " + ImprovedHordesManager.Instance.HeatTracker.GetHeatInChunk(chunk));

            if (ImprovedHordesManager.Instance.HeatPatrolManager.GetAreaPatrolTime(pos, out ulong time))
            {
                (int days, int hours, int minutes) = GameUtils.WorldTimeToElements(time);
                builder.AppendLine($"Area: " + ImprovedHordesManager.Instance.HeatPatrolManager.GetAreaFromChunk(chunk));
                builder.AppendLine($"Patrol time: Day {days} {hours}:{minutes}");

                IChunk chunkInstance = GameManager.Instance.World.GetChunkSync(Chunk.ToAreaMasterChunkPos(new Vector3i(global::Utils.Fastfloor(pos.x), global::Utils.Fastfloor(pos.y), global::Utils.Fastfloor(pos.z))));
                ChunkAreaBiomeSpawnData chunkAreaBiomeSpawnData = chunk != null ? ((Chunk)chunkInstance).GetChunkBiomeSpawnData() : null;

                if (chunkAreaBiomeSpawnData != null)
                    Utils.CheckPOITags(chunkAreaBiomeSpawnData);

                if (chunkAreaBiomeSpawnData != null && chunkAreaBiomeSpawnData.checkedPOITags && !chunkAreaBiomeSpawnData.poiTags.IsEmpty)
                    builder.AppendLine($"POI tags: {chunkAreaBiomeSpawnData.poiTags.GetTagNames().ToString(str => str)}");
                else
                    builder.AppendLine("No POI tags found.");
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
            return "Shows information regarding the area (such as patrol time / heat / poi tags).";
        }
    }
}
