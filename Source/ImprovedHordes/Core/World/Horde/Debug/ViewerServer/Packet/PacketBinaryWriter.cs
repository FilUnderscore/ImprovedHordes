#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet
{
    public sealed class PacketBinaryWriter
    {
        private readonly BinaryWriter writer;

        public PacketBinaryWriter(BinaryWriter writer)
        {
            this.writer = writer;
        }

        public void Write(bool value)
        {
            this.writer.Write(value);
        }

        public void Write(byte value) 
        {
            this.writer.Write(value);
        }

        public void Write(float value)
        {
            this.writer.Write(value);
        }

        public void Write(int value)
        {
            this.writer.Write(value);
        }

        public void Write(byte[] b)
        {
            this.Write(b.Length);
            this.writer.Write(b);
        }

        public void Write(string value)
        {
            bool valid = value != null && value.Length > 0;

            this.Write(valid);

            if (valid)
            {
                this.Write(Encoding.UTF8.GetBytes(value));
            }
        }

        public void Write(Vector3 value)
        {
            this.Write(value.x);
            this.Write(value.y);
            this.Write(value.z);
        }

        public void Write(Vector2i value)
        {
            this.Write(value.x);
            this.Write(value.y);
        }

        public void Write<T>(List<T> value, Action<T> listCallback) where T : class
        {
            this.Write(value.Count);

            foreach(var item in value)
            {
                listCallback(item);
            }
        }

        public void WriteStruct<T>(List<T> value, Action<T> listCallback) where T : struct
        {
            this.Write(value.Count);

            foreach (var item in value)
            {
                listCallback(item);
            }
        }
    }
}
#endif