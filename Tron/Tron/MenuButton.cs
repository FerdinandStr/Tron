using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Tron
{
    public class MenuButton
    {
        Texture2D btnTexture;
        Vector2 position;
        Rectangle rectangle;
        public Vector2 size;

        Color color = new Color(255, 255, 255, 255);

        public MenuButton(Texture2D newTexture, GraphicsDevice graphics, int sizeX, int sizeY)
        {
            btnTexture = newTexture;

            // 500 : 8 = 62 //Size to Screensize
            size = new Vector2(sizeX, sizeY);
        }

        bool down;
        public bool isClicked;
        public void Update(MouseState mouse)
        {
            rectangle = new Rectangle((int) position.X,(int) position.Y, (int) size.X, (int) size.Y);

            Rectangle mouseRectangle = new Rectangle(mouse.X, mouse.Y, 1, 1);

            if (mouseRectangle.Intersects(rectangle))
            {
                if (color.A == 255) down = false;
                if (color.A == 0) down = true;
                if (down) color.A += 3; else color.A -= 3;
                if (mouse.LeftButton == ButtonState.Pressed) isClicked = true;
            }
            else if (color.A < 255)
            {
                color.A += 3;
                isClicked = false;
            }
        }

        public void setPosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(btnTexture, rectangle, color);
        }
    }
}
