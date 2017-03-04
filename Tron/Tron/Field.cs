using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Tron
{
    public class Field
    {
        public bool isWall = false;
        public bool isDrawn = false;

        public Color color = Color.White;

        public Texture2D wallTexture;

        public Field(Texture2D wallTex)
        {
            wallTexture = wallTex;
        }
    }

    public class newFieldClass
    {
        public bool test = true;
    }
}
