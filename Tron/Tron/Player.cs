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
        public long name;
        public int[] pos;
        public int directionX = 1;
        public int directionY = 0;
        //public int speed;
        public Color color;
        public int gridSize;
        public bool isAlive = true;

        public Texture2D playerTexture;

        public Player(int positionX, int positionY, Color col, int gamegridSize, Texture2D tex)
        {
            pos = new int[] { positionX, positionY };
            color = col;
            gridSize = gamegridSize;
            playerTexture = tex;
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
    }
}
