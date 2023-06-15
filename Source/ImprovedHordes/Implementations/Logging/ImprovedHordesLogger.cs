using ImprovedHordes.Core.Abstractions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ImprovedHordes.Implementations.Logging
{
    public sealed class ImprovedHordesLogger : ILogger
    {
        public static bool VERBOSE;
        private readonly Type type;

        public ImprovedHordesLogger(Type type)
        {
            this.type = type;
        }

        private string GetMethodSignature(MethodBase method)
        {
            string[] param = method.GetParameters().Select(p => $"{p.ParameterType.Name}").ToArray();

            return $"{method.Name}({string.Join(",", param)})";
        }

        private string FormatMessageVerbose(string message)
        {
            StringBuilder builder = new StringBuilder();
            StackTrace trace = new StackTrace();
            string methodSignature;

            for(int i = 4; i < trace.FrameCount; i++)
            {
                StackFrame frame = trace.GetFrame(i);
                methodSignature = GetMethodSignature(frame.GetMethod());

                builder.AppendLine($"Called by [{frame.GetMethod().DeclaringType.FullName}] {methodSignature}");
            }

            methodSignature = GetMethodSignature(trace.GetFrame(3).GetMethod());

            return $"[Improved Hordes] [{type.Name}] {methodSignature}: {message} \nDetailed stacktrace: \n{builder.ToString()}";
        }

        private string FormatMessage(string message)
        {
            if (VERBOSE)
                return FormatMessageVerbose(message);

            string methodSignature = GetMethodSignature(new StackFrame(2).GetMethod());

            return $"[Improved Hordes] [{type.Name}] {methodSignature}: {message}";
        }

        public void Error(string message)
        {
            Log.Error(this.FormatMessage(message));
        }

        public void Exception(Exception e)
        {
            Log.Exception(e);
        }

        public void Info(string message)
        {
            Log.Out(this.FormatMessage(message));
        }

        public void Warn(string message)
        {
            Log.Warning(this.FormatMessage(message));
        }

        public void Verbose(string message)
        {
            if (!VERBOSE)
                return;

            Log.Out($"VERBOSE: {this.FormatMessage(message)}");
        }
    }
}
