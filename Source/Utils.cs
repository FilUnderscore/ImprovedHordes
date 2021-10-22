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

        public static bool GetSpawnableY(ref Vector3 pos)
        {
            //int y = Utils.Fastfloor(playerY - 1f);
            int y = (int)byte.MaxValue;
            int x = global::Utils.Fastfloor(pos.x);
            int z = global::Utils.Fastfloor(pos.z);

            if (HordeManager.Instance.World.GetWorldExtent(out Vector3i minSize, out Vector3i maxSize))
            {
                x = Mathf.Clamp(x, minSize.x, maxSize.x);
                z = Mathf.Clamp(z, minSize.z, maxSize.z);
            }
            while (HordeManager.Instance.World.GetBlock(x, y, z).type == 0)
            {
                if (--y < 0)
                    return false;
            }

            pos.x = (float)x;
            pos.y = (float)(y + 1);
            pos.z = z;
            return true;
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

            public static int FindLineCircleIntersections(float centerX, float centerY, float radius, Vector2 point1, Vector2 point2, out Vector2 int1, out Vector2 int2)
            {
                float dx, dy, A, B, C, det, t;

                dx = point2.x - point1.x;
                dy = point2.y - point1.y;

                A = dx * dx + dy * dy;
                B = 2 * (dx * (point1.x - centerX) + dy * (point1.y - centerY));
                C = (point1.x - centerX) * (point1.x - centerX) + (point1.y - centerY) * (point1.y - centerY) - radius * radius;

                det = B * B - 4 * A * C;

                if (A <= 0.0000001 || det < 0)
                {
                    // No solutions.
                    int1 = Vector2.zero;
                    int2 = Vector2.zero;

                    return 0;
                }
                else if (det == 0)
                {
                    // One solution.
                    t = -B / (2 * A);

                    int1 = new Vector2(point1.x + t * dx, point1.y + t * dy);
                    int2 = Vector2.zero;

                    return 1;
                }
                else
                {
                    // Two solutions.
                    t = (float)((-B + System.Math.Sqrt(det)) / (2 * A));
                    int1 = new Vector2(point1.x + t * dx, point1.y + t * dy);

                    t = (float)((-B - System.Math.Sqrt(det)) / (2 * A));
                    int2 = new Vector2(point1.x + t * dx, point1.y + t * dy);

                    return 2;
                }
            }

        }
    }
}