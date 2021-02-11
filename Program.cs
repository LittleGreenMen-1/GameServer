using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Numerics;
using System.Collections.Generic;

namespace GameServer
{
    public static class Program
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        static PlayerManager pm = new PlayerManager();
        public static int globalID;

        public class StateObject
        {
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public List<byte> finalMessage = new List<byte>();
            public int ID;

            //public StringBuilder sb = new StringBuilder();

            public Socket workSocket = null;

            public void Reset()
            {
                buffer = new byte[BufferSize];
                
                finalMessage.Clear();
                //sb.Clear();
            }
        }

        public static void StartListening()
        {
            int PORT = 12345;
            IPAddress ipAddress = IPAddress.Parse("192.168.1.11");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    allDone.Reset();

                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            handler.Blocking = false;

            StateObject state = new StateObject();
            
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            //String content = String.Empty;

            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                Console.WriteLine(bytesRead);
                //state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                
                for(int i = 0; i < bytesRead; i++)
                    state.finalMessage.Add(state.buffer[i]);

                //content = state.sb.ToString();
                int index_of_33 = state.finalMessage.IndexOf(33);

                string data_string = "[";

                for (int i = 0; i < state.finalMessage.Count; i++)
                    data_string += state.finalMessage[i].ToString() + ", ";
                data_string += "]";

                Console.WriteLine("state.finalMessage: {0}\n", data_string);

                if (index_of_33 > -1)
                {
                    List<byte> message_bytes = state.finalMessage.GetRange(0, index_of_33);
                    Tuple<string, byte[]> message = pm.SplitMessage(message_bytes);

                    data_string = "[";

                    for(int i = 0; i < message.Item2.Length; i++)
                        data_string += message.Item2[i].ToString() + ", ";
                    data_string += "]";

                    Console.WriteLine("{0} : {1}", message.Item1, data_string);

                    switch (message.Item1) 
                    {
                        case "new_player":
                            pm.players.Add(Player.ProtoDeserialize<Player>(message.Item2));

                            break;
                        case "update_player":
                            Player p = Player.ProtoDeserialize<Player>(message.Item2);

                            //pm.players.
                            pm.players[0] = Player.ProtoDeserialize<Player>(message.Item2);
                            break;
                        case "get_players":
                            Send(handler, PlayerManager.ProtoSerialize<PlayerManager>(pm));
                            Send(handler, Encoding.ASCII.GetBytes("!"));

                            break;
                    }

                    pm.Print();

                    state.finalMessage.RemoveRange(0, index_of_33 + 1);
                    //Send(handler, content);
                }

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }

            /*if(state.finalMessage[state.finalMessage.Count - 1] == 33)
                state.finalMessage.RemoveAt(state.finalMessage.Count - 1);

            Console.WriteLine(state.finalMessage.Count);
            Player player = Player.ProtoDeserialize<Player>(state.finalMessage.ToArray());

            Console.WriteLine("Position: {0}, {1}", player.velocity.X, player.velocity.Y);

            state.finalMessage.Clear();
            state.sb.Clear();*/
        }

        private static void Send(Socket handler, byte[] data)
        {
            handler.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;

                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Shutdown(IAsyncResult ar)
        {
            Socket handler = (Socket)ar.AsyncState;

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }

        [STAThread]
        static void Main(String[] args)
        {
            globalID = 0;
            StartListening();
        }
    }
}
