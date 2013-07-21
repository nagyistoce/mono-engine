#region usingStatements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using TextureAtlas;
using Projectile;
#endregion

namespace Weapon
{
    abstract class Weapon
    {
        private List<Projectile.Projectile> bulletList;
        protected int bulletRate;
        protected SpriteAnimate bulletSprite;
        private int bulletTimer;
        private int bulletCount;
        protected int bulletDelay;
        protected int maxBullets;
        protected GraphicsDevice graphics;

        public Weapon(int FiringRate, GraphicsDevice GraphicsMan)
        {
            bulletRate = FiringRate;
            bulletList = new List<Projectile.Projectile>();
            bulletDelay = 200;
            bulletTimer = 0;
            maxBullets = 1;
            bulletCount = 0;
            graphics = GraphicsMan;
        }

        public void Fire(Vector2 start, Vector2 end)
        {
            if ((bulletCount < maxBullets) && bulletTimer <= 0)
            {
                bulletCount++;
                bulletTimer = bulletDelay;
                CreateNextBullet(new Projectile.Projectile(start, end, bulletRate, bulletSprite));
            }
        }

        public void CreateNextBullet(Projectile.Projectile bullet)
        {
            bulletList.Add(bullet);
        }

        public void Update(GameTime time, List<Collidable> colliders)
        {
            if (bulletTimer < 0)
                bulletTimer = 0;
            else if(bulletTimer > 0)
                bulletTimer -= time.ElapsedGameTime.Milliseconds;

            List<int> toRemove = new List<int>();
            bulletList.ForEach(item =>
            {
                bool hitDest = false;
                if (colliders != null)
                {
                    colliders.ForEach(collide =>
                        {
                            if (item.Collision(collide))
                            {
                                hitDest = true;
                                Character.Enemy e = collide as Character.Enemy;
                                if (e != null)
                                {
                                    e.Health -= item.Damage;
                                }
                            }
                        });
                }
                if (hitDest)
                    toRemove.Add(bulletList.IndexOf(item));
            });

            toRemove = toRemove.OrderByDescending(item => item).ToList();
            toRemove.ForEach(item =>
            {
                bulletList.RemoveAt(item);
                bulletCount--;
            });

            //TODO: Collision Detection

            bulletList.ForEach(bullet =>
            {
                bullet.Update(time);
            });
        }

        public void Draw(SpriteBatch batch, GameTime time)
        {
            bulletList.ForEach(bullet =>
            {
                bullet.Draw(batch, bulletSprite, time);
            });
        }

        abstract public void Load(ContentManager Content);
    }

    class MegaBlaster : Weapon
    {
        static int FIRING_RATE = 4;
        static int MAX_BULLETS = 3;
        static int BULLET_DELAY = 200;

        public MegaBlaster(GraphicsDevice GraphicsMan) 
            : base(FIRING_RATE, GraphicsMan)
        {
            float scale = 1.0f;
                 //scale = (float)graphics.Viewport.Height / 480;
            bulletRate = (int)(bulletRate * scale);
            bulletDelay = BULLET_DELAY;
            maxBullets = MAX_BULLETS;
        }

        override public void Load(ContentManager Content)
        {
            Texture2D Bullet = Content.Load<Texture2D>(@"Bullet");
            bulletSprite = new SpriteAnimate(Bullet, 1, 1, graphics);
            bulletSprite.Zoom = 0.05f;
            bulletSprite.SetRange(0, 0);
        }
    }
}
