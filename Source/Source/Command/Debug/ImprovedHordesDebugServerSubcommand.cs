#if DEBUG
using ImprovedHordes.Command;
using ImprovedHordes.Source.Core.Debug;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Command.Debug
{
    internal sealed class ImprovedHordesDebugServerSubcommand : ExecutableSubcommandBase
    {
        private static HordeViewerDebugServer debugServer;

        public ImprovedHordesDebugServerSubcommand() : base("server")
        {
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                if (debugServer != null)
                {
                    message = "Debug server has already been started.";
                }
                else
                {
                    debugServer = new HordeViewerDebugServer(instance.GetWorldSize(), instance.GetHordeManager().GetTracker(), instance.GetScanner());
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