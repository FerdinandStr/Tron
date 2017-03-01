using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tron
{
    class Player
    {
        public int posX;
        public int posY;
        public int directionX = 1;
        public int directionY = 0;
        //public int speed;
        public Color color;
        public int gridSize;
        public bool isAlive = true;

        public Texture2D playerTexture;

        public Player(int positionX, int positionY, Color col, int gamegridSize, Texture2D tex)
        {
            posX = positionX;
            posY = positionY;
            color = col;
            gridSize = gamegridSize;
            playerTexture = tex;
        }

        public void move()
        {
            int newPosX = posX + directionX;
            int newPosY = posY + directionY;

            //Avoid Nullpointer Wallcollision and move da Karra aufd andra Seid
            if(newPosX == -1){
                posX = gridSize -1;
            }
            else if(newPosX == gridSize){
                posX = 0;
            }
            else{
                posX = newPosX;
            }

            if(newPosY == -1){
                posY = gridSize -1;
            }
            else if(newPosY == gridSize){
                posY = 0;
            }
            else{
                posY = newPosY;
            }
        }
    }
}
