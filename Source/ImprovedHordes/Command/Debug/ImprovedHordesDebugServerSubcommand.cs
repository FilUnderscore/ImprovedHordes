#if DEBUG
using ImprovedHordes.Core.Command;
using ImprovedHordes.Core.World.Horde.Debug;
using System.Collections.Generic;

namespace ImprovedHordes.Command.Debug
{
    internal sealed class ImprovedHordesDebugServerSubcommand : ExecutableSubcommandBase
    {
        private static HordeViewerDebugServer debugServer;

        public ImprovedHordesDebugServerSubcommand() : base("server")
        {
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            if (ImprovedHordesMod.TryGetInstance(out var instance))
            {
                if(debugServer == null)
                    debugServer = new HordeViewerDebugServer(instance.GetCore().GetLoggerFactory(), instance.GetCore().GetRandomFactory(), instance.GetCore().GetWorldSize(), instance.GetCore().GetWorldHordeTracker(), instance.GetPOIScanner());

                if (debugServer.Started)
                {
                    message = "Debug server has already been started.";
                }
                else
                {
                    debugServer.StartServer();
                    message = "Started debug server. Connect to 127.0.0.1 with IHDebugViewer to view World Horde state.";
                }
            }
            else
            {
                message = "Failed to start debug server.";
            }

            return false;
        }

        public override (string name, bool optional)[] GetArgs()
        {
            return null;
        }

        public override string GetDescription()
        {
            return "Start the debug server. Requires IHDebugViewer to view.";
        }
    }
}
#endif