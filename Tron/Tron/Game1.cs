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
        Texture2D textureBackground;
        Texture2D textureBtnLocal;
        Texture2D textureBtnNetwork;

        SpriteFont fontRoboto;

        private String lobbyText;
        public MenuButton btnLocal;
        public MenuButton btnNetwork;

        static int gamegridSize = 50;
        static int fieldSize = 20;
        static int screensize = gamegridSize * fieldSize;


        private int gameMode = 0;//false = Local //true = Network
        int GameTimeDivisor = 0;
        private bool lobby = true;
        private bool gamestart = false;

        int playerCount = 2;
        int playerNr = 1;
        List<Player> playerList;
        Field[,] gamegrid;

        Dictionary<Color, bool> colorList;
        
        //Server-Client
        NetClient client;

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
            textureBackground = Content.Load<Texture2D>("logogroßswitch");

            fontRoboto = Content.Load<SpriteFont>("Roboto");

            btnLocal = new MenuButton(textureBtnLocal, graphics.GraphicsDevice, textureBtnLocal.Width, textureBtnLocal.Height);
            btnNetwork = new MenuButton(textureBtnNetwork, graphics.GraphicsDevice, textureBtnNetwork.Width, textureBtnNetwork.Height);
            btnLocal.setPosition(new Vector2((screensize / 2 -100) - screensize / 4, (screensize / 2 -50)));//Mittig positionieren
            btnNetwork.setPosition(new Vector2((screensize / 2 - 100) + screensize / 4, (screensize / 2 - 50)));

            addPlayers();
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferHeight = screensize;
            graphics.PreferredBackBufferWidth = screensize;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            Window.Title = "Tron2D";
            this.IsMouseVisible = true;

            createColorList();

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
                // ######################### LOCAL ######################### //
                if (gameMode == 1)
                {
                    ProcessKeyboard();

                    if (GameTimeDivisor == 15)
                    {
                        int playersAlive = 0;
                        foreach (Player player in playerList)
                        {
                            if (player.isAlive)
                            {
                                if (!gamegrid[player.pos[0], player.pos[1]].isWall)
                                {
                                    //Set Wall and Color
                                    gamegrid[player.pos[0], player.pos[1]].isWall = true;
                                    gamegrid[player.pos[0], player.pos[1]].color = player.color;
                                    gamegrid[player.pos[0], player.pos[1]].wallTexture = textureTrail;

                                    //Send move to Server
                                    player.move();

                                    playersAlive++;
                                }
                                else
                                {
                                    player.playerTexture = textureSkull;
                                    player.isAlive = false;
                                }
                            }
                        }

                        if (playersAlive == 0)
                        {
                            // Gameover //
                            lobby = true;
                            gameMode = 0;

                            foreach (Player player in playerList)
                            {
                                player.playerTexture = textureArrow;
                                player.resetPlayer();
                            }

                        }
                        else if (playersAlive == 1)
                        {
                            foreach (Player player in playerList)
                            {
                                if (player.isAlive) player.win();
                            }
                        }
                        GameTimeDivisor = 0;
                    }
                    GameTimeDivisor++;
                }
                // ######################### NETWORK ######################### //
                else if (gameMode == 2)
                {
                    //Locale Bewegung und Wand zeichnen
                    if (gamestart)
                    {
                        ProcessKeyboard();

                        if (GameTimeDivisor == 15)
                        {
                            Player player = playerList[0];
                            if (player.isAlive)
                            {
                                gamegrid[player.pos[0], player.pos[1]].color = player.color;
                                gamegrid[player.pos[0], player.pos[1]].wallTexture = textureTrail;

                                //Send move to Server
                                sendMove();

                            }
                            else
                            {
                                player.playerTexture = textureSkull;
                            }
                            GameTimeDivisor = 0;
                        }
                        GameTimeDivisor++;
                    }


                    //Server Bewegungen abfragen
                    NetIncomingMessage msg;
                    while ((msg = client.ReadMessage()) != null)
                    {
                        switch (msg.MessageType)
                        {
                            case NetIncomingMessageType.DiscoveryResponse:
                                
                                //Server verbinden und Spieler hinzufügen
                                client.Connect(msg.SenderEndPoint);                                
                                break;

                            case NetIncomingMessageType.Data:

                                long recievedId = msg.ReadInt64();
                                //Prüfung ob Player vorhanden
                                //int playerNr = msg.ReadInt32();
                                addPlayersServer(recievedId);
                                bool alive = msg.ReadBoolean();
                                int posX = msg.ReadInt32();
                                int posY = msg.ReadInt32();

                                foreach (Player player in playerList)
                                {
                                    if (player.playerId == recievedId)
                                    {
                                        player.isAlive = alive;
                                        player.pos[0] = posX;
                                        player.pos[1] = posY;
                                        return;
                                    }
                                }

                                break;
                        }
                    }
                }
            }
            base.Update(gameTime);
        }

        public void sendMove()
        {
            //Send movement to Server
            NetOutgoingMessage sendMsg = client.CreateMessage();
            sendMsg.Write(playerList[0].directionX);//X
            sendMsg.Write(playerList[0].directionY);//Y
            client.SendMessage(sendMsg, NetDeliveryMethod.Unreliable);
        }


        private void openLobby()
        {
            processMenuKeyboard();

            //Generiere Lobby Text //
            updateInfo();

            MouseState mouse = Mouse.GetState();

            btnLocal.Update(mouse);
            btnNetwork.Update(mouse);

            if (btnLocal.isClicked)
            {
                btnLocal.isClicked = false;
                lobby = false;
                gameMode = 1;

                initialzeGameGrid();
            }
            else if (btnNetwork.isClicked)
            {
                btnLocal.isClicked = false;
                lobby = false;
                gameMode = 2;
                initialzeGameGrid();
                client.DiscoverLocalPeers(1337);
                playerList = new List<Player>();
            }
        }

        public void updateInfo()
        {
            String playerWins = "\n Punke | Sp1: " + playerList[0].getWins() +
                                " | Sp2: " + playerList[1].getWins();

            if (playerCount >= 3)
            {
                playerWins += " | Sp3: " + playerList[2].getWins();
                if (playerCount == 4) playerWins += " | Sp4: " + playerList[3].getWins();
            }

            lobbyText = "Spieleranzahl: " + playerCount + playerWins;
        }

        private void initialzeGameGrid()
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
        }


        public void addPlayers()
        {
            playerList = new List<Player>();
            if (playerCount >= 1) playerList.Add(new Player(1,1, Color.Yellow, gamegridSize, textureArrow));
            if (playerCount >= 2) playerList.Add(new Player(2,2, Color.Red, gamegridSize, textureArrow));
            if (playerCount >= 3) playerList.Add(new Player(3,3, Color.Blue, gamegridSize, textureArrow));
            if (playerCount >= 4) playerList.Add(new Player(4,4, Color.Green, gamegridSize, textureArrow));
            //if (playerCount >= 5) playerList.Add(new Player(playerId, Color.Orange, gamegridSize, textureArrow));
            //if (playerCount >= 6) playerList.Add(new Player(playerId, Color.Purple, gamegridSize, textureArrow));
            //if (playerCount >= 7) playerList.Add(new Player(playerId, Color.Black, gamegridSize, textureArrow));
            //if (playerCount >= 8) playerList.Add(new Player(playerId, Color.White, gamegridSize, textureArrow));
        }

        public void addPlayersServer(long playerId)
        {
            bool playerExists = false;
            foreach (Player player in playerList)
            {
                if (player.playerId == playerId)
                {
                    playerExists = true;
                }
            }
            if (!playerExists)
            {
                playerList.Add(new Player(playerNr, playerId, colorList.First(x => x.Value == false).Key, gamegridSize, textureArrow));//Erste freie Farbe aus Farblist hohlen
                colorList[colorList.First(x => x.Value == false).Key] = true;//Erste Farbe als vergeben markieren
                gamestart = true;
            }
        }

        // ###### Initialize Methods ####### //

        public void createColorList()
        {
            colorList = new Dictionary<Color, bool>();
            colorList.Add(Color.Yellow, false);
            colorList.Add(Color.Red, false);
            colorList.Add(Color.Blue, false);
            colorList.Add(Color.Green, false);
            colorList.Add(Color.Orange, false);
            colorList.Add(Color.Purple, false);
            colorList.Add(Color.Black, false);
            colorList.Add(Color.Brown, false);
        }


        // ########################################################################## DRAW ################################################################################ //

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            if (lobby)
            {

                spriteBatch.Draw(textureBackground,new Rectangle(0,0, screensize,screensize), Color.White);
                spriteBatch.DrawString(fontRoboto, lobbyText, new Vector2(20, 10), Color.Black);
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


        private void drawGamegrid()
        {
            for (int i = 0; i < gamegrid.GetLength(0); i++)
            {
                for (int j = 0; j < gamegrid.GetLength(1); j++)
                {
                    Rectangle rect = new Rectangle(i * fieldSize, j * fieldSize, fieldSize, fieldSize);
                    spriteBatch.Draw(gamegrid[i, j].wallTexture, rect, gamegrid[i, j].color);
                }
            }
        }

        private void drawPlayer()
        {

            foreach (Player player in playerList)
            {
                //if (player.isAlive == true)
                // new Rectangle((player.pos[0] * fieldSize) + fieldSize / 2, (player.pos[1] * fieldSize) +1 fieldSize / 2, fieldSize, fieldSize)//Position des Feldes plus hälfte der Feldgrößen, damit das rectangle in der mitte gedreht werden kann
                spriteBatch.Draw(player.playerTexture, new Rectangle((player.pos[0] * fieldSize) + fieldSize / 2, (player.pos[1] * fieldSize) + fieldSize / 2, fieldSize, fieldSize), null, player.color, MathHelper.ToRadians(player.rotation), new Vector2(fieldSize / 2, fieldSize / 2), SpriteEffects.None, 0f);
                spriteBatch.DrawString(fontRoboto, player.getName(), new Vector2(player.pos[0] * fieldSize + 20, player.pos[1] * fieldSize + 20), Color.Gray);
            }
        }


        //protected override void OnExiting(object sender, EventArgs args)
        //{
        //    client.Shutdown("Client verlassen " + playerList[0].name);

        //    base.OnExiting(sender, args);
        //}



        // ########################################################### PROCESS KEYBOARD ########################################################### //

        public void processMenuKeyboard()
        {
            KeyboardState boardState = Keyboard.GetState();

            if (boardState.IsKeyDown(Keys.D2))
            {
                playerCount = 2;
                addPlayers();
            }
            if (boardState.IsKeyDown(Keys.D3))
            {
                playerCount = 3;
                addPlayers();
            }
            if (boardState.IsKeyDown(Keys.D4))
            {
                playerCount = 4;
                addPlayers();
            }
        }


        public void ProcessKeyboard()
        {
            KeyboardState boardState = Keyboard.GetState();

            // Player Movement //
 
            Player player1 = playerList[0];

            if (player1.isAlive)
            {
                //PFEILE// 
                if (boardState.IsKeyDown(Keys.Left))
                {
                    if (player1.directionX != 1 && player1.directionY != 0)
                    {
                        player1.directionX = -1;
                        player1.directionY = 0;
                        player1.changeRotation();
                    }
                }
                if (boardState.IsKeyDown(Keys.Right))
                {
                    if (player1.directionX != -1 && player1.directionY != 0)
                    {
                        player1.directionX = 1;
                        player1.directionY = 0;
                        player1.changeRotation();
                    }
                }
                if (boardState.IsKeyDown(Keys.Up))
                {
                    if (player1.directionX != 0 && player1.directionY != 1)
                    {
                        player1.directionX = 0;
                        player1.directionY = -1;
                        player1.changeRotation();
                    }
                }
                if (boardState.IsKeyDown(Keys.Down))
                {
                    if (player1.directionX != 0 && player1.directionY != -1)
                    {
                        player1.directionX = 0;
                        player1.directionY = 1;
                        player1.changeRotation();
                    }
                }
            }

            if (playerList.Count >= 2 && gameMode == 1)
            {
                Player player2 = playerList[1];
                if (player2.isAlive)
                {
                    //WASD// 
                    if (boardState.IsKeyDown(Keys.A))
                    {
                        if (player2.directionX != 1 && player2.directionY != 0)
                        {
                            player2.directionX = -1;
                            player2.directionY = 0;
                            player2.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.D))
                    {
                        if (player2.directionX != -1 && player2.directionY != 0)
                        {
                            player2.directionX = 1;
                            player2.directionY = 0;
                            player2.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.W))
                    {
                        if (player2.directionX != 0 && player2.directionY != 1)
                        {
                            player2.directionX = 0;
                            player2.directionY = -1;
                            player2.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.S))
                    {
                        if (player2.directionX != 0 && player2.directionY != -1)
                        {
                            player2.directionX = 0;
                            player2.directionY = 1;
                            player2.changeRotation();
                        }
                    }
                }
            }

            if (playerList.Count >= 3 && gameMode == 1)
            {
                Player player3 = playerList[2];
                if (player3.isAlive)
                {
                    //WASD// 
                    if (boardState.IsKeyDown(Keys.NumPad1))
                    {
                        if (player3.directionX != 1 && player3.directionY != 0)
                        {
                            player3.directionX = -1;
                            player3.directionY = 0;
                            player3.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.NumPad3))
                    {
                        if (player3.directionX != -1 && player3.directionY != 0)
                        {
                            player3.directionX = 1;
                            player3.directionY = 0;
                            player3.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.NumPad5))
                    {
                        if (player3.directionX != 0 && player3.directionY != 1)
                        {
                            player3.directionX = 0;
                            player3.directionY = -1;
                            player3.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.NumPad2))
                    {
                        if (player3.directionX != 0 && player3.directionY != -1)
                        {
                            player3.directionX = 0;
                            player3.directionY = 1;
                            player3.changeRotation();
                        }
                    }
                }
            }

            if (playerList.Count >= 4 && gameMode == 1)
            {
                Player player4 = playerList[3];
                if (player4.isAlive)
                {
                    //WASD// 
                    if (boardState.IsKeyDown(Keys.J))
                    {
                        if (player4.directionX != 1 && player4.directionY != 0)
                        {
                            player4.directionX = -1;
                            player4.directionY = 0;
                            player4.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.L))
                    {
                        if (player4.directionX != -1 && player4.directionY != 0)
                        {
                            player4.directionX = 1;
                            player4.directionY = 0;
                            player4.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.I))
                    {
                        if (player4.directionX != 0 && player4.directionY != 1)
                        {
                            player4.directionX = 0;
                            player4.directionY = -1;
                            player4.changeRotation();
                        }
                    }
                    if (boardState.IsKeyDown(Keys.K))
                    {
                        if (player4.directionX != 0 && player4.directionY != -1)
                        {
                            player4.directionX = 0;
                            player4.directionY = 1;
                            player4.changeRotation();
                        }
                    }
                }
            }

        }
    }
}
