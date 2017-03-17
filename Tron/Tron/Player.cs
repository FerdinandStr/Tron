using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tron
{


    public class Player
    {
        public String name = "";
        public int playerNr;
        public long playerId;
        public int[] pos;
        public int directionX = 0;
        public int directionY = 0;
        public float rotation = 90;
        //public int speed;
        public Color color;
        public int gridSize;
        public bool isAlive = true;
        private int wins = 0;
        private int winsOld = 0;

        public Texture2D playerTexture;

        public Player(int positionX, int positionY, int dirX, int dirY, Color col, int gamegridSize, Texture2D tex)
        {
            pos = new int[] { positionX, positionY };
            color = col;
            gridSize = gamegridSize;
            playerTexture = tex;
            directionX = dirX;
            directionY = dirY;
        }
        public Player(int nr, long id, Color col, int gamegridSize, Texture2D tex)
        {
            color = col;
            gridSize = gamegridSize;
            playerTexture = tex;
            playerNr = nr;
            playerId = id;
            setStartDirection();
            changeRotation();
        }

        public Player(int nr, long id, int gamegridSize)//Server Player
        {
            gridSize = gamegridSize;
            playerNr = nr;
            playerId = id;
            setStartDirection();
            changeRotation();
        }

        private void setStartDirection()
        {
            //Kompliziert vereinfachte hoch wissenschaftliche Mathematik
            int mid = (gridSize-1) / 2;
            int space = mid / 5;
            int posVal1 = space; //Abstand zu 0-Koordinate am geringsten
            int posVal2 = mid - space;
            int posVal3 = mid + space;
            int posVal4 = (gridSize-1) - space; //Abstand zu 0-Koordinate am größten

            // X             Y               DirX,DirY
            int[,] playerSetup = new int[8, 4] {{posVal2, posVal1,            0, 1},//Player1
                                                {posVal3, posVal4,            0,-1},//Player2
                                                {posVal4, posVal2,           -1, 0},//Player3
                                                {posVal1, posVal3,            1, 0},//Player4
                                                {posVal3, posVal1,            0, 1},//Player5
                                                {posVal2, posVal4,            0,-1},//Player6
                                                {posVal1, posVal2,            1, 0},//Player7
                                                {posVal4, posVal3,           -1, 0}};//Player8

            int posArray = (int) playerNr - 1;
            pos = new int[] { playerSetup[posArray, 0], playerSetup[posArray, 1] };
            directionX = playerSetup[posArray, 2];
            directionY = playerSetup[posArray, 3];
        }

        public void move()
        {
            int newPosX = pos[0] + directionX;
            int newPosY = pos[1] + directionY;

            //Avoid Nullpointer Wallcollision and move da Karra aufd andra Seid
            if(newPosX == -1){
                pos[0] = gridSize - 1;
            }
            else if(newPosX == gridSize){
                pos[0] = 0;
            }
            else{
                pos[0] = newPosX;
            }

            if(newPosY == -1){
                pos[1] = gridSize - 1;
            }
            else if(newPosY == gridSize){
                pos[1] = 0;
            }
            else{
                pos[1] = newPosY;
            }
        }

        public void changeRotation()
        {
            if (directionX == -1 && directionY == 0)
            {//A
                rotation = 270;
            }

            if (directionX == 1 && directionY == 0)
            {//D
                rotation = 90;
            }

            if (directionX == 0 && directionY == -1)
            {//W
                rotation = 0;
            }

            if (directionX == 0 && directionY == 1)
            {//S
                rotation = 180;
            }
        }

        public void resetPlayer(){
            setStartDirection();
            changeRotation();
            isAlive = true;
            winsOld = wins;
        }


        public void win() {
            if (wins == winsOld)
            {
                wins++;
            }
        }
        public int getWins()
        {
            return wins;
        }

        public String getName()
        {
            if (name == "")
            {
                return playerNr.ToString();
            }
            else
            {
                return name;
            }
        }
    }
}
