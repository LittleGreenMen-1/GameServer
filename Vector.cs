using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtoBuf;

namespace GameServer
{
    [ProtoContract]
    class Vector
    {
        [ProtoMember(1)]
        public float X;
        [ProtoMember(2)]
        public float Y;

        public Vector() { }

        public static T ProtoDeserialize<T>(byte[] data) where T : class
        {
            if (null == data) return null;

            try
            {
                using (var stream = new MemoryStream(data))
                {
                    return Serializer.Deserialize<T>(stream);
                }
            }
            catch
            {
                // Log error
                throw;
            }
        }
    }
}
