using System;

namespace ImprovedHordes
{
    public static class IHUtils
    {

    }

    public static class IHLog
    {
        public static void Log(String message)
        {
            Log("{0}", message);
        }

        public static void Log(String format, params Object[] objs)
        {
            global::Log.Out(String.Format("[Improved Hordes] {0}", String.Format(format, objs)));
        }

        public static void Warning(String format, params Object[] objs)
        {
            global::Log.Warning(String.Format("[Improved Hordes] {0}", String.Format(format, objs)));
        }

        public static void Warning(String message)
        {
            Warning("{0}", message);
        }

        public static void Error(String format, params Object[] objs)
        {
            global::Log.Error(String.Format("[Improved Hordes] {0}", String.Format(format, objs)));
        }

        public static void Error(String message)
        {
            Error("{0}", message);
        }
    }
}