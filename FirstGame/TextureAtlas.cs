#region usingStatements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#endregion

namespace TextureAtlas
{
    public interface Collidable
    {
        bool Moving { get; }
        bool Collision(Rectangle toCheck);
        bool Collision(Collidable toCheck);
        Rectangle Bounds { get; }
    }

    public class SpriteAnimate
    {
        public Texture2D Texture { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int DrawDelay { get; set; }
        public float Trim { get; set; }
        public bool FlipHorizontal { get; set; }
        private float zoom;
        public float Zoom
        {
            get
            {
                return zoom;
            }
            set
            {
                zoom = value;
                //float scale = (float)graphics.Viewport.Height / 480;
                //zoom *= scale;

            }
        }
        
        public List<int> DrawSequence 
        {
            get
            {
                return drawSequence;
            }

            set
            {
                drawSequence = value;
                if (drawSequence != null)
                {
                    currDrawSeq = drawSequence.GetEnumerator();
                    currDrawSeq.MoveNext();
                }
                this.timeElapsed = this.DrawDelay;
            } 
        }
        private List<int> drawSequence; 
        private int timeElapsed;
        private int [] range;
        private int currentFrame;
        private int totalFrames;
        private static int BEGIN = 0;
        private static int END = 1;
        private IEnumerator<int> currDrawSeq;
        private GraphicsDevice graphics;
        

        public SpriteAnimate(Texture2D texture, int rows, int columns, GraphicsDevice gManager)
        {
            this.graphics = gManager;
            this.Texture = texture;
            this.Rows = rows;
            this.Columns = columns;
            this.currentFrame = 0;
            this.totalFrames = Rows * Columns;
            this.range = new int[2];
            this.range[BEGIN] = 0;
            this.range[END] = this.totalFrames - 1;
            this.DrawDelay = 50;
            this.timeElapsed = 0;
            this.Trim = 0.00f;
            this.Zoom = 1.0f;
            this.FlipHorizontal = false;
        }

        public void Update(GameTime time)
        {
            this.timeElapsed += time.ElapsedGameTime.Milliseconds;
            if (this.timeElapsed >= this.DrawDelay)
            {
                this.timeElapsed -= this.DrawDelay;
                if (this.DrawSequence != null && this.DrawSequence.Count > 0)
                {
                    this.currentFrame = this.currDrawSeq.Current;
                    if (!this.currDrawSeq.MoveNext())
                    {
                        this.currDrawSeq = this.DrawSequence.GetEnumerator();
                        this.currDrawSeq.MoveNext();
                    }

                }
                else
                {
                    this.currentFrame++;
                    if (this.currentFrame > this.range[END])
                        this.currentFrame = this.range[BEGIN];

                }
            }
            
            
        }

        public void SetRange(int begin, int end)
        {
            if (begin <= end)
            {
                this.range[BEGIN] = begin;
                this.range[END] = end;
                if (this.DrawSequence != null)
                    this.DrawSequence = null;
            }
            else
                throw new ArgumentOutOfRangeException();

            this.timeElapsed = this.DrawDelay;
            currentFrame = begin;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 location)
        {
            Rectangle sourceRectangle = GetSourceRec(location);

            Rectangle destinationRectangle = GetDestinationRec(location);
            float scale = (float)graphics.Viewport.Height / 480;
            SpriteEffects effect = SpriteEffects.None;
            if (FlipHorizontal)
                effect = SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(Texture,new Vector2( destinationRectangle.X,destinationRectangle.Y) , sourceRectangle, Color.White, 0.0f, Vector2.Zero, scale * zoom, effect, 0.0f);
        }

        public Rectangle GetDestinationRec(Vector2 location)
        {
            int width = this.Texture.Width / this.Columns;
            int height = this.Texture.Height / this.Rows;
            return new Rectangle((int)location.X - (int)((width * this.Zoom) / 2),
                (int)location.Y - (int)(height * this.Zoom),
                (int)(width * this.Zoom),
                (int)(height * this.Zoom));
        }

        public Rectangle GetSourceRec(Vector2 location)
        {
            int width = this.Texture.Width / this.Columns;
            int height = this.Texture.Height / this.Rows;
            int row = (int)((float)currentFrame / (float)this.Columns);
            int column = currentFrame % this.Columns;
            return new Rectangle((width * column) + (int)(width * this.Trim),
                (height * row) + (int)(height * this.Trim),
                width - (2 * (int)(width * this.Trim)),
                height - (2 * (int)(height * this.Trim)));
        }
    }
}
