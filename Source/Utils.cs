using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using ImprovedHordes.Horde;

namespace ImprovedHordes
{
    public static class Utils
    {
        public static string ToString<T>(this List<T> list, Func<T,string> stringFunction)
        {
            if (list != null)
            {
                StringBuilder builder = new StringBuilder();

                builder.Append("{");
                for (int i = 0; i < list.Count; i++)
                {
                    T element = list[i];

                    string result = "null";

                    if (element != null)
                        result = stringFunction.Invoke(element);

                    builder.Append(result);

                    if (i != list.Count - 1)
                        builder.Append(", ");
                }
                builder.Append("}");

                return builder.ToString();
            }

            return "null";
        }

        public static string ToString<T>(this HashSet<T> set, Func<T, string> stringFunction)
        {
            if (set != null)
            {
                StringBuilder builder = new StringBuilder();

                builder.Append("{");
                int i = 0;
                foreach (var element in set)
                {
                    string result = "null";

                    if (element != null)
                        result = stringFunction.Invoke(element);

                    builder.Append(result);

                    if (i != set.Count - 1)
                        builder.Append(", ");

                    i++;
                }
                builder.Append("}");

                return builder.ToString();
            }

            return "null";
        }

        public static void GetSpawnableY(ref Vector3 pos)
        {
            pos.y = ImprovedHordesManager.Instance.World.GetHeightAt(pos.x, pos.z) + 1.0f;
        }

        public static void Randomize<T>(this List<T> list)
        {
            for(int i = 0; i < list.Count - 1; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i + 1, list.Count - 1);

                T valueAtIndex = list[randomIndex];
                list[randomIndex] = list[i];
                list[i] = valueAtIndex;
            }
        }

        public static Vector2 ToXZ(this Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        public static class Logger
        {
            public static void Log(String message)
            {
                Log("{0}", message);
            }

            public static void Log(String format, params object[] objs)
            {
                global::Log.Out(String.Format("[Improved Hordes] {0}", String.Format(format, objs)));
            }

            public static void Warning(String format, params object[] objs)
            {
                global::Log.Warning(String.Format("[Improved Hordes] {0}", String.Format(format, objs)));
            }

            public static void Warning(String message)
            {
                Warning("{0}", message);
            }

            public static void Error(String format, params object[] objs)
            {
                global::Log.Error(String.Format("[Improved Hordes] {0}", String.Format(format, objs)));
            }

            public static void Error(String message)
            {
                Error("{0}", message);
            }
        }

        public static class Math
        {
            public static int Clamp(int num, int min, int max)
            {
                if (max - min < 0)
                    throw new Exception("Minimum cannot be greater than the maximum.");

                if (num < min)
                    return min;

                if (num > max)
                    return max;

                return num;
            }

            public static float Clamp(float num, float min, float max)
            {
                if (max - min < 0.0f)
                    throw new Exception("Minimum cannot be greater than the maximum");

                if (num < min)
                    return min;

                if (num > max)
                    return max;

                return num;
            }

            public static int FastRound(float value)
            {
                if (value < 0)
                    return (int)(value - 0.5f);

                return (int)(value + 0.5f);
            }
        }
    }
}