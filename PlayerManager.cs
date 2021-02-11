using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ProtoBuf;

namespace GameServer
{
    [ProtoContract]
    class PlayerManager
    {
        [ProtoMember(1)]
        public List<Player> players;

        public PlayerManager()
        {
            players = new List<Player>();
        }

        public Tuple<string, byte[]> SplitMessage(List<byte> message)
        {
            //message.RemoveAt(message.Count - 1);

            int bodyBound = message.IndexOf((byte) ':');

            if(bodyBound > -1)
            {
                string body = Encoding.UTF8.GetString(message.GetRange(0, bodyBound).ToArray());
                byte[] data = message.GetRange(bodyBound + 1, message.Count - bodyBound - 1).ToArray();

                return Tuple.Create(body, data);
            }

            return Tuple.Create(Encoding.UTF8.GetString(message.ToArray()), new byte[0]);
        }

        public static byte[] ProtoSerialize<T>(T record) where T : class
        {
            if (null == record) return null;

            try
            {
                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, record);
                    return stream.ToArray();
                }
            }
            catch
            {
                // Log error
                throw;
            }
        }

        public void Print()
        {
            int i = 0;
            foreach(Player p in players)
            {
                Console.WriteLine("Player {0} - X: {1}, Y: {2}", i, p.position.X, p.position.Y);
                i++;
            }
        }
    }
}
