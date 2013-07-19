#region usingStatements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TextureAtlas;
#endregion

namespace Projectile
{
    public class Projectile : Collidable
    {
        private Vector2 initLoc;
        private Vector2 destLoc;
        private Vector2 currLoc;
        private int speed;
        private SpriteAnimate animation;
        public bool Moving
        {
            get
            {
                return true;
            }
        }

        public int XDir
        {
            get
            {
                return xDir;
            }
        }
        public int YDir
        {
            get
            {
                return yDir;
            }
        }
        private int xDir;
        private int yDir;
        private double slope;// = (initLoc.Y - destLoc.Y) / (initLoc.X - destLoc.X);
        private double length;// = Math.Sqrt(Math.Pow((initLoc.X - destLoc.X), 2) + Math.Pow((initLoc.Y - destLoc.Y), 2));
        public bool HitDest
        {
            get
            {
                bool x = true, y = true;
                if (xDir == 1)
                {
                    if ((destLoc.X > currLoc.X))
                        x = false;
                }
                else if( xDir == -1)
                {
                    if ((destLoc.X < currLoc.X))
                        x = false;
                }

                if (yDir == 1)
                {
                    if ((destLoc.Y > currLoc.Y))
                        y = false;
                }
                else if (yDir == -1)
                {
                    if ((destLoc.Y < currLoc.Y))
                        y = false;
                }


                if (x && y)
                {
                    xDir = 0;
                    yDir = 0;
                    return true;
                }

                return false;
            }
        }

        public Projectile(Vector2 Start, Vector2 End, int Speed, SpriteAnimate Animation)
        {
            initLoc = Start;
            destLoc = End;
            currLoc = initLoc;
            speed = Speed;
            animation = Animation;
            if (initLoc.X < destLoc.X)
                xDir = 1;
            else if (initLoc.X > destLoc.X)
                xDir = -1;
            else
                xDir = 0;

            if (initLoc.Y < destLoc.Y)
                yDir = 1;
            else if (initLoc.Y > destLoc.Y)
                yDir = -1;
            else
                yDir = 0;

            slope = (initLoc.Y - destLoc.Y) / (initLoc.X - destLoc.X);
            length = Math.Sqrt(Math.Pow((initLoc.X - destLoc.X), 2) + Math.Pow((initLoc.Y - destLoc.Y), 2));
        }

        public bool Collision(Rectangle toCheck)
        {
            return animation.GetDestinationRec(currLoc).Intersects(toCheck);
        }

        public bool Collision(Collidable toCheck)
        {
            return toCheck.Collision(animation.GetDestinationRec(currLoc));
        }

        public Rectangle Bounds
        {
            get
            {
                return animation.GetDestinationRec(currLoc);
            }
        }
        public void Update(GameTime time)
        {
            animation.Update(time);

        }

        //Need to take 'time' into account - Update based on number of milliseconds passed since last draw
        public void Draw(SpriteBatch batch, SpriteAnimate sprite, GameTime time)
        {
            UpdateCurrentLocation(time);
            sprite.Draw(batch, currLoc);
        }

        private void UpdateCurrentLocation(GameTime time)
        {
            double angle = Math.Atan(slope);
            double pointX = Math.Cos(angle);
            double pointY = Math.Sin(angle);

            currLoc.X += (float)(speed * pointX) * xDir;
            currLoc.Y += (float)(speed * pointY) * yDir;
        }

        static public double getAngle(Vector2 start, Vector2 end)
        {
            double theSlope = (start.Y - end.Y) / (start.X - end.X);
            return Math.Atan(theSlope);
        }
    }
}
