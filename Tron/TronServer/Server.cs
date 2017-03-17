using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;
using Tron;

namespace TronServer
{
    class Server
    {
        //Gamesettings
        static int gamegridSize = 50;

        static void Main(string[] args)
        {
            int playerNr = 1;
            Field[,] gamegrid;

            //Gamegrid erzeugen
            gamegrid = new Field[gamegridSize, gamegridSize];
            for (int i = 0; i < gamegrid.GetLength(0); i++)
            {
                for (int j = 0; j < gamegrid.GetLength(1); j++)
                {
                    gamegrid[i, j] = new Field();
                }
            }

            // Create PlayerList
            //playerList = new List<Player>();

            //Networking
            NetPeerConfiguration config = new NetPeerConfiguration("xnaapp");
            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.Port = 1337;

            //create and start Server
            NetServer server = new NetServer(config);
            server.Start();

            //Speed
            double nextSendUpdates = NetTime.Now;

            //Escape Exit
            while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
            {
                //READING MESSAGES //
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

                                //Add new Player to List
                                msg.SenderConnection.Tag = new Player(playerNr, msg.SenderConnection.RemoteUniqueIdentifier, gamegridSize);
                                playerNr++;
                                //addPlayersServer(playerId);
                            }
                            else if (status == NetConnectionStatus.Disconnected)
                            {
                                Console.WriteLine(NetUtility.ToHexString(msg.SenderConnection.RemoteUniqueIdentifier) + "disconnected!");
                                //Delete Player from List
                                //foreach(Player player in playerList){
                                //    if (player.playerId == playerId)
                                //    {
                                //        playerList.Remove(player);
                                //    }
                                //}
                            }
                            break;

                        case NetIncomingMessageType.Data:

                            //Recieve Client Message
                            int moveX = msg.ReadInt32();
                            int moveY = msg.ReadInt32();
                            //int gridSize = msg.ReadInt32();
                            Player msgPlayer = msg.SenderConnection.Tag as Player;

                            msgPlayer.directionX = moveX;
                            msgPlayer.directionY = moveY;
                            msg.SenderConnection.Tag = msgPlayer;
                            Console.WriteLine("---------------------------------------------------------");
                            Console.WriteLine("RECIEVED MSG Nr " + msgPlayer.playerNr + " PosX: " + msgPlayer.pos[0] + " PosY: " + msgPlayer.pos[1]);

                            //foreach (Player player in playerList)
                            //{
                            //    if (player.playerId == playerId)
                            //    {
                            //        player.directionX = moveX;
                            //        player.directionY = moveY;
                            //        Console.WriteLine("Id " + playerId + " X: " + moveX + " Y: " + moveY);

                            //        msg.SenderConnection.Tag = player;
                            //        return;
                            //    }

                            //}

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
                                Console.WriteLine("ACHTUNG KEIN TAG BEIM SENDEN !!! ");
                                player.Tag = new Player(playerNr, msg.SenderConnection.RemoteUniqueIdentifier, gamegridSize);
                                playerNr++;
                            }

                            Player msgPlayer = player.Tag as Player;
                            //sendMessage.Write(msgPlayer.playerNr);

                            //Collision Detection and Movement //
                            if (!gamegrid[msgPlayer.pos[0], msgPlayer.pos[1]].isWall)
                            {
                                //Set Wall and Color
                                gamegrid[msgPlayer.pos[0], msgPlayer.pos[1]].isWall = true;
                                // Move

                                
                                Console.WriteLine("Nr " + msgPlayer.playerNr + " PosX: " + msgPlayer.pos[0] + " PosY: " + msgPlayer.pos[1]);
                                Console.WriteLine(gamegrid[msgPlayer.pos[0], msgPlayer.pos[1]].isWall);
                                msgPlayer.move();
                                Console.WriteLine("Nr " + msgPlayer.playerNr + " PosX: " + msgPlayer.pos[0] + " PosY: " + msgPlayer.pos[1]);
                                Console.WriteLine(gamegrid[msgPlayer.pos[0], msgPlayer.pos[1]].isWall);
                            }
                            else
                            {
                                msgPlayer.isAlive = false;
                            }
                            sendMessage.Write(msgPlayer.isAlive);
                            sendMessage.Write(msgPlayer.pos[0]);
                            sendMessage.Write(msgPlayer.pos[1]);


                            Console.WriteLine("SENDED MSG Nr " + msgPlayer.playerNr + " PosX: " + msgPlayer.pos[0] + " PosY: " + msgPlayer.pos[1]);
                            server.SendMessage(sendMessage, player, NetDeliveryMethod.Unreliable);
                        }
                    }

                    nextSendUpdates += (1.0 / 10.0);//Sendeintervall

                }
                //to run smootly
                Thread.Sleep(1);
            }
            server.Shutdown("Server beendet");
        }

        // ############################################################################################## //



        //public static void addPlayersServer(long playerId)
        //{
        //    playerList = new List<Player>();

        //    bool playerExists = false;
        //    foreach (Player player in playerList)
        //    {
        //        if (player.playerId == playerId)
        //        {
        //            playerExists = true;
        //        }
        //    }
        //    if (!playerExists)
        //    {
        //        playerList.Add(new Player(playerId, gamegridSize));//Erste freie Farbe aus Farblist hohlen
        //    }
        //}



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
