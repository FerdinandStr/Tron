using System;
using System.Threading;

using Lidgren.Network;

namespace TronServer
{
    class Server
    {
        //Gamesettings

        static void Main(string[] args)
        {
            //Networking
            NetPeerConfiguration config = new NetPeerConfiguration("xnaapp");
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.Port = 1337;

            //create and start Server
            NetServer server = new NetServer(config);
            server.Start();

            double nextSendUpdates = NetTime.Now;

            while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
            {
                NetIncomingMessage msg;
                while ((msg = server.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.DiscoveryRequest:

                            //Recieved discovery request from cient -> send discovery response

                            server.SendDiscoveryResponse(null, msg.SenderEndPoint);
                            break;

                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            //Write all error messages
                            Console.WriteLine(msg.ReadString());
                            break;

                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
                            if(status == NetConnectionStatus.Connected)
                            {
                                // Player Connected !
                                Console.WriteLine(NetUtility.ToHexString(msg.SenderConnection.RemoteUniqueIdentifier) + " is connected !");

                                //Store Position in Connection Tag

                                msg.SenderConnection.Tag = new int[]{10,10};
                            }
                            else if (status == NetConnectionStatus.Disconnected)
                            {
                                Console.WriteLine(NetUtility.ToHexString(msg.SenderConnection.RemoteUniqueIdentifier) + "disconnected!");
                            }
                            break;

                        case NetIncomingMessageType.Data:

                            //Client sent input to Server

                            int moveX = msg.ReadInt32();
                            int moveY = msg.ReadInt32();
                            int gridSize = msg.ReadInt32();
                            int[] pos = msg.SenderConnection.Tag as int[];

                            Console.WriteLine(moveX + moveY + gridSize);

                            pos = movePlayer(pos, moveX, moveY, gridSize);

                            msg.SenderConnection.Tag = pos;

                            break;
                    }


                    double now = NetTime.Now;

                    if (now > nextSendUpdates)
                    {
                        //Send Position Updates back

                        foreach (NetConnection player in server.Connections)
                        {
                            NetOutgoingMessage sendMessage = server.CreateMessage();

                            sendMessage.Write(player.RemoteUniqueIdentifier);

                            if (player.Tag == null)
                            {
                                player.Tag = new int[2];
                            }

                            int[] pos = player.Tag as int[];
                            sendMessage.Write(pos[0]);
                            sendMessage.Write(pos[1]);

                            server.SendMessage(sendMessage, player, NetDeliveryMethod.Unreliable);//Udp ???
                        }
                    }

                    nextSendUpdates += (1.0 / 10.0);//Sendeintervall

                }
                //to run smootly
                Thread.Sleep(1);
            }
            server.Shutdown("Server beendet");
        }

        public static int[] movePlayer(int[] playerPos, int directionX, int directionY, int gridSize)
        {
            int newPosX = playerPos[0] + directionX;
            int newPosY = playerPos[1] + directionY;

            //Avoid Nullpointer Wallcollision and move da Karra aufd andra Seid
            if (newPosX == -1)
            {
                playerPos[0] = gridSize - 1;
            }
            else if (newPosX == gridSize)
            {
                playerPos[0] = 0;
            }
            else
            {
                playerPos[0] = newPosX;
            }

            if (newPosY == -1)
            {
                playerPos[1] = gridSize - 1;
            }
            else if (newPosY == gridSize)
            {
                playerPos[1] = 0;
            }
            else
            {
                playerPos[1] = newPosY;
            }

            return playerPos;
        }
    }
}
