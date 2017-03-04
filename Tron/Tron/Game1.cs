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

        Texture2D textureField;
        Texture2D textureArrow;
        Texture2D textureTrail;
        Texture2D textureSkull;
        //Texture2D textureBackground;
        Texture2D textureBtnLocal;
        Texture2D textureBtnNetwork;

        SpriteFont font;

        private String lobbyText;
        public MenuButton btnLocal;
        public MenuButton btnNetwork;

        static int gamegridSize = 50;
        static int fieldSize = 20;
        static int screensize = gamegridSize * fieldSize;


        private int gameMode = 0;//false = Local //true = Network
        private TimeSpan gameSpeed = TimeSpan.FromSeconds(1.0f / 5.0f);
        private bool lobby = true;

        //Dictionary<long, Player> players = new Dictionary<long, Player>();

        List<Player> playerList;
        Field[,] gamegrid;

        NetClient client;

        //Player player = new Player(10, 10, Color.Yellow, gamegridSize);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";            
            
            NetPeerConfiguration config = new NetPeerConfiguration("xnaapp");
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

            client = new NetClient(config);
            client.Start();

        }

        protected override void LoadContent()
        {
            device = graphics.GraphicsDevice;
            spriteBatch = new SpriteBatch(GraphicsDevice);

            textureField = Content.Load<Texture2D>("field");
            textureArrow = Content.Load<Texture2D>("arrowUp");
            textureTrail = Content.Load<Texture2D>("trail");
            textureSkull = Content.Load<Texture2D>("skull");
            textureBtnLocal = Content.Load<Texture2D>("btnLocal");
            textureBtnNetwork = Content.Load<Texture2D>("btnNetwork");
            //textureBackground = Content.Load<Texture2D>("background");

            font = Content.Load<SpriteFont>("Roboto");

            btnLocal = new MenuButton(textureBtnLocal, graphics.GraphicsDevice, textureBtnLocal.Width, textureBtnLocal.Height);
            btnNetwork = new MenuButton(textureBtnNetwork, graphics.GraphicsDevice, textureBtnNetwork.Width, textureBtnNetwork.Height);
            btnLocal.setPosition(new Vector2((screensize / 2) - 50, screensize / 2));
            btnNetwork.setPosition(new Vector2((screensize / 2) - 50, (screensize / 2)+ 50));

        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferHeight = screensize;
            graphics.PreferredBackBufferWidth = screensize;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            Window.Title = "Tron2D";
            this.IsMouseVisible = true;

            client.DiscoverLocalPeers(1337);

            base.Initialize();
        }


        // ########################################################################## UPDATE ################################################################################ //

        protected override void Update(GameTime gameTime)
        {
            if (lobby == true)
            {
                openLobby();
            }
            else
            {
                if (gameMode == 1)// LOCAL
                {
                    ProcessKeyboard();
                    int playersAlive = 0;

                    foreach (Player player in playerList)
                    {
                        if (!gamegrid[player.pos[0], player.pos[1]].isWall)
                        {
                            //Set Wall and Color
                            gamegrid[player.pos[0], player.pos[1]].isWall = true;
                            gamegrid[player.pos[0], player.pos[1]].color = player.color;
                            gamegrid[player.pos[0], player.pos[1]].wallTexture = textureTrail;

                            //Send move to Server
                            player.move();

                            playersAlive += 1;
                        }
                        else
                        {
                            player.playerTexture = textureSkull;
                            player.isAlive = false;
                        }
                    }

                    if (playersAlive == 0)
                    {
                        lobby = true;
                        gameMode = 0;

                    }

                }
                else if(gameMode == 2)// NETWORK
                {
                    //Read Message from Server
                    Player player = playerList[0];

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

            if (gameMode == 1)//Abfrage nur bei localem Spiel
            {
                Player player2 = playerList[1];
                //PFEILE// 
                if (boardState.IsKeyDown(Keys.A))
                {
                    player2.directionX = -1;
                    player2.directionY = 0;
                }
                if (boardState.IsKeyDown(Keys.D))
                {
                    player2.directionX = 1;
                    player2.directionY = 0;
                }
                if (boardState.IsKeyDown(Keys.W))
                {
                    player2.directionX = 0;
                    player2.directionY = -1;
                }
                if (boardState.IsKeyDown(Keys.S))
                {
                    player2.directionX = 0;
                    player2.directionY = 1;
                }
            }
        }

        private void openLobby()
        {
            lobbyText = "Willkommen in der Lobby";
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 60.0f);

            MouseState mouse = Mouse.GetState();

            btnLocal.Update(mouse);
            btnNetwork.Update(mouse);

            if (btnLocal.isClicked)
            {
                lobby = false;
                gameMode = 1;

                initialzeGame();
            }
            else if(btnNetwork.isClicked)
            {
                lobby = false;
                gameMode = 2;
            }
        }

        private void initialzeGame()
        {
            // Locales Spiel neu vorbereiten

            //Gamegrid erzeugen
            gamegrid = new Field[gamegridSize, gamegridSize];
            for (int i = 0; i < gamegrid.GetLength(0); i++)
            {
                for (int j = 0; j < gamegrid.GetLength(1); j++)
                {
                    gamegrid[i, j] = new Field(textureField);
                }
            }

            //Player initialisieren
            playerList = new List<Player>();
            playerList.Add(new Player(0, 0, Color.Yellow, gamegridSize, textureArrow));
            playerList.Add(new Player(1, 5, Color.Red, gamegridSize, textureArrow));
            
            // set gamespeed
            this.TargetElapsedTime = gameSpeed;
        }


        // ########################################################################## DRAW ################################################################################ //

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            if (lobby)
            {
                //spriteBatch.DrawString(font,lobbyText,new Vector2(10,20),Color.HotPink);

                //spriteBatch.Draw(textureBackground,new Rectangle(0,0, screensize,screensize), Color.White);
                
                btnLocal.Draw(spriteBatch);
                btnNetwork.Draw(spriteBatch);
            }
            else
            {
                drawGamegrid();
                drawPlayer();
            }
            
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


        //protected override void OnExiting(object sender, EventArgs args)
        //{
        //    client.Shutdown("Client verlassen " + playerList[0].name);

        //    base.OnExiting(sender, args);
        //}
    }
}
