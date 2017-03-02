using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Lidgren.Network;

namespace Tron
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GraphicsDevice device;

        Texture2D fieldTexture;
        Texture2D arrowTexture;
        Texture2D trailTexture;
        Texture2D skullTexture;

        static int gamegridSize = 50;
        static int fieldSize = 20;

        Dictionary<long, Player> players = new Dictionary<long, Player>();

        //List<Player> playerList = new List<Player>();
        Field[,] gamegrid = new Field[gamegridSize, gamegridSize];

        NetClient client;

        //Player player = new Player(10, 10, Color.Yellow, gamegridSize);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 5.0f);
            
            
            NetPeerConfiguration config = new NetPeerConfiguration("xnaapp");
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

            client = new NetClient(config);
            client.Start();

        }

        protected override void LoadContent()
        {
            device = graphics.GraphicsDevice;
            spriteBatch = new SpriteBatch(GraphicsDevice);

            fieldTexture = Content.Load<Texture2D>("field");
            arrowTexture = Content.Load<Texture2D>("arrowUp");
            trailTexture = Content.Load<Texture2D>("trail");
            skullTexture = Content.Load<Texture2D>("skull");

            //Player initialisieren
            //playerList.Add(new Player(0, 0, Color.Yellow, gamegridSize, arrowTexture));
            //playerList.Add(new Player(1, 5, Color.Red, gamegridSize, arrowTexture));

            //Gamegrid erzeugen
            for (int i = 0; i < gamegrid.GetLength(0); i++)
            {
                for (int j = 0; j < gamegrid.GetLength(1); j++)
                {
                    gamegrid[i, j] = new Field(fieldTexture);
                }
            }
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferHeight = gamegridSize * fieldSize;
            graphics.PreferredBackBufferWidth = gamegridSize * fieldSize;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            Window.Title = "Tron2D";

            client.DiscoverLocalPeers(1337);

            base.Initialize();
        }


        // ########################################################################## UPDATE ################################################################################ //

        protected override void Update(GameTime gameTime)
        {
            ProcessKeyboard();
            
            //Collision detection
            Player player = playerList[0];

            if (!gamegrid[player.pos[0], player.pos[1]].isWall)
            {
                //Set Wall and Color
                gamegrid[player.pos[0], player.pos[1]].isWall = true;
                gamegrid[player.pos[0], player.pos[1]].color = player.color;
                gamegrid[player.pos[0], player.pos[1]].wallTexture = trailTexture;

                //Send move to Server
                sendMove(player);

                gamegrid[player.pos[0], player.pos[1]].isWall = true;
                gamegrid[player.pos[0], player.pos[1]].color = player.color;
                gamegrid[player.pos[0], player.pos[1]].wallTexture = trailTexture;
            }
            else
            {
                player.playerTexture = skullTexture;
                player.isAlive = false;
            }

            if (!player.isAlive)
            {
                //Spieler verloren
            }


            //Read Message from Server

            NetIncomingMessage msg;
            while ((msg = client.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.DiscoveryResponse:
                        client.Connect(msg.SenderEndPoint);
                        break;
                    case NetIncomingMessageType.Data:

                        player.name = msg.ReadInt64();
                        player.pos[0] = msg.ReadInt32();
                        player.pos[1] = msg.ReadInt32();
                        break;
                }
            } 
            base.Update(gameTime);
        }

        public void sendMove(Player player)
        {
            //Send movement to Server
            NetOutgoingMessage sendMsg = client.CreateMessage();
            sendMsg.Write(player.directionX);//X
            sendMsg.Write(player.directionY);//Y
            sendMsg.Write(gamegridSize);//Gridsize
            client.SendMessage(sendMsg, NetDeliveryMethod.Unreliable);
        }

        public void ProcessKeyboard()
        {
            KeyboardState boardState = Keyboard.GetState();

            Player player1 = playerList[0];
            Player player2 = playerList[1];

            //PFEILE// 
            if (boardState.IsKeyDown(Keys.Left))
            {
                player1.directionX = -1;
                player1.directionY = 0;
            }
            if (boardState.IsKeyDown(Keys.Right))
            {
                player1.directionX = 1;
                player1.directionY = 0;
            }
            if (boardState.IsKeyDown(Keys.Up))
            {
                player1.directionX = 0;
                player1.directionY = -1;
            }
            if (boardState.IsKeyDown(Keys.Down))
            {
                player1.directionX = 0;
                player1.directionY = 1;
            }
        }




        // ########################################################################## DRAW ################################################################################ //

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            drawGamegrid();
            drawPlayer();
            spriteBatch.End();

            base.Draw(gameTime);
        }


        private void drawGamegrid() {
            for (int i = 0; i < gamegrid.GetLength(0); i++)
            {
                for (int j = 0; j < gamegrid.GetLength(1); j++)
                {
                    //if (gamegrid[i, j].isDrawn == false)
                    //{
                    Rectangle rect = new Rectangle(i * fieldSize, j * fieldSize, fieldSize, fieldSize);
                        spriteBatch.Draw(gamegrid[i,j].wallTexture, rect, gamegrid[i, j].color);
                    //}
                }
            }
        }

        private void drawPlayer(){

            foreach (Player player in playerList )
            {
                //if (player.isAlive == true)

                spriteBatch.Draw(player.playerTexture, new Rectangle(player.pos[0] * fieldSize, player.pos[1] * fieldSize, fieldSize, fieldSize), player.color);
            }
        }


        protected override void OnExiting(object sender, EventArgs args)
        {
            client.Shutdown("Client verlassen " + playerList[0].name);

            base.OnExiting(sender, args);
        }
    }
}
