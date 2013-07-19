﻿#region usingStatements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using TextureAtlas;
using Projectile;
using Weapon;
#endregion

namespace Character
{
    enum CharacterState
    {
        StillLeft,
        StillRight,
        Left,
        Right,
        ShootLeft,
        ShootRight,
        JumpLeft,
        JumpRight
    }

    enum CharacterSubState
    {
        None,
        Jumping,
        Falling,
        Shooting
    }

    abstract class Character
    {
        protected CharacterState currentState;
        protected CharacterState lastState;
        protected CharacterSubState subAction;
        protected CharacterSubState lastAction;
        protected Vector2 currentPosition;
        protected float gunAngle;
        protected bool jumpPressed;
        protected float jumpStart;
        protected GraphicsDevice graphics;

        public Character(Vector2 startingPosition, GraphicsDevice graphicsDev)
        {
            subAction = CharacterSubState.None;
            currentState = CharacterState.StillRight;
            lastState = CharacterState.StillRight;
            lastAction = CharacterSubState.None;
            currentPosition = startingPosition;
            graphics = graphicsDev;
        }

        public void Left()
        {
            currentState = CharacterState.Left;
        }

        public void Right()
        {
            //lastState = currentState;
            currentState = CharacterState.Right;
        }

        public void Still()
        {
            //lastState = currentState;
            if (lastState == CharacterState.Left)
            {
                currentState = CharacterState.StillLeft;
            }
            else if (lastState == CharacterState.Right)
            {
                currentState = CharacterState.StillRight;
            }
        }

        public void Shoot(float angle)
        {
            //lastState = currentState;
            subAction = CharacterSubState.Shooting;

            gunAngle = angle;
            //if (StateIsLeft(currentState))
              //  angle += 180;
        }

        public void StopShoot()
        {
            //lastState = currentState;
            if (subAction == CharacterSubState.Shooting)
            {
                subAction = CharacterSubState.None;

                if (StateIsLeft(lastState))
                {
                    if(StateIsJumping(lastState))
                        currentState = CharacterState.JumpLeft;
                    else
                        currentState = CharacterState.StillLeft;
                }
                else
                {
                    if(StateIsJumping(lastState))
                        currentState = CharacterState.JumpRight;
                    else
                        currentState = CharacterState.StillRight;
                }

            }

        }

        virtual public void Jump()
        {
            
            //if (curren != CharacterSubState.Jumping &&
            //    subAction != CharacterSubState.Falling)
            //{
            //    subAction = CharacterSubState.Jumping;
            //}
            //if (currentState != CharacterState.JumpLeft &&
            //    currentState != CharacterState.JumpRight)
            //{

            if (subAction != CharacterSubState.Falling)
            {
                subAction = CharacterSubState.Jumping;
                jumpStart = currentPosition.Y;
            }

                jumpPressed = true;
            //}
            //lastState = currentState;
        }

        public void StopJump()
        {
            jumpPressed = false;
        }

        public bool StateIsLeft(CharacterState state)
        {
            switch (state)
            {
                case CharacterState.JumpLeft:
                case CharacterState.Left:
                case CharacterState.ShootLeft:
                case CharacterState.StillLeft:
                    return true;
            }

            return false;
        }

        public bool StateIsJumping(CharacterState state)
        {
            switch (state)
            {
                case CharacterState.JumpLeft:
                case CharacterState.JumpRight:
                    return true;
            }
            return false;
        }

        public bool StateIsRight(CharacterState state)
        {
            switch (state)
            {
                case CharacterState.JumpRight:
                case CharacterState.Right:
                case CharacterState.ShootRight:
                case CharacterState.StillRight:
                    return true;
            }

            return false;
        }
        abstract public void Load(ContentManager Content);

        virtual public void Update(GameTime time, List<Collidable> obstacles)
        {
            lastState = currentState;
            lastAction = subAction;
        }

        abstract public void Draw(SpriteBatch spriteBatch, GameTime time);

    }

    class Snail : Character, Collidable
    {
        static List<int> moveSeq;
        static int deathSeq;
        Texture2D snailImage;

        SpriteAnimate snailSprite;
        int runRate;
        int runDelay;
        int isLeft;

        public bool Moving
        {
            get
            {
                return true;
            }
        }

        public Snail(Vector2 StartingPos, GraphicsDevice GraphicsMan)
            : base(StartingPos, GraphicsMan)
        {
            isLeft = -1;
            runRate = 1;
            runDelay = 150;
            moveSeq = new List<int> { 0, 1 };
            deathSeq = 2;
            currentPosition = StartingPos;
        }

        override public void Load(ContentManager Content)
        {
            snailImage = Content.Load<Texture2D>(@"snailWalk1");
            snailSprite = new SpriteAnimate(snailImage, 1, 3, graphics);
            snailSprite.DrawSequence = moveSeq;
            snailSprite.DrawDelay = runDelay;
            snailSprite.Zoom = 0.75f;
        }

        public override void Update(GameTime time, List<Collidable> obstacles)
        {
            currentPosition.X += runRate * isLeft;
            Rectangle belowMe = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
            belowMe.Y += belowMe.Height;
            belowMe.X += belowMe.Width * isLeft;
            bool groundLeft = false;
            foreach (Collidable c in obstacles)
            {
                if (c.Bounds.Contains(belowMe.Center))
                {
                    groundLeft = true;
                    break;
                }
            }

            if (!groundLeft)
                isLeft *= -1;

            snailSprite.Update(time);
            base.Update(time, obstacles);
        }

        public bool Collision(Rectangle toCheck)
        {
            return Bounds.Intersects(toCheck);
        }

        public bool Collision(Collidable toCheck)
        {
            return Bounds.Intersects(toCheck.Bounds);
        }

        public Rectangle Bounds
        {
            get
            {
                return snailSprite.GetDestinationRec(currentPosition);
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime time)
        {
            //if we are right, need to flip
            if (isLeft == 1)
            {
                snailSprite.FlipHorizontal = true;
            }
            else 
                snailSprite.FlipHorizontal = false;
            snailSprite.Draw(spriteBatch, currentPosition);
        }
    }

    enum MegaPowerUp
    {
        None,
        Speed,
        Jump
    }

    class MegaMan : Character, Collidable
    {
        #region MegaDefs
        //Frame numbers in TextureAtlas
        //Numbers are 0,1,2,3
        //            4,5,6,7
        //            etc...
        //static int[] rightList;
        static List<int> runRight;
        //static int[] leftList;
        static List<int> runLeft;
        static int standLeft;
        static int standRight;
        static int jumpLeft;
        static int jumpRight;
        static int shootLeft;
        static int shootRight;
        static int runDelay;
        Texture2D MegaRunner;
        Texture2D MegaJumpShoot;

        SpriteAnimate runnerSprite;
        SpriteAnimate jumpSprite;
        SpriteAnimate shootSprite;
        
        //This might be variable if you hit a booster or something
        int runRate; //Number of pixels per update - used to determine jump, fall and run speed
        float jumpSpeed;
        float jumpCounter;
        Weapon.Weapon currentWeapon;

        int powerUpCounter;
        MegaPowerUp activePower;
        MegaPowerUp lastPower;
        float scale;
        
        

        static int MAX_JUMP_HEIGHT;
        int currJumpHeight;
        public bool Moving
        {
            get
            {
                return true;
            }
        }
#endregion
        public MegaMan(Vector2 StartingPos, GraphicsDevice GraphicsMan)
            : base(StartingPos, GraphicsMan)
        {
            graphics = GraphicsMan;

            runRight = new List<int>{ 1, 2, 3, 2 };
            runLeft = new List<int> { 4, 5, 6, 5 };
            standLeft = 7;
            standRight = 0;
            jumpLeft = 3;
            jumpRight = 2;
            shootLeft = 1;
            shootRight = 0;

            scale = 1.0f;
            //scale = (float)graphics.Viewport.Height / 480;
            MAX_JUMP_HEIGHT = currJumpHeight = (int)(75 * scale);
            runRate = (int)(3 * scale); //Number of pixels per update - used to determine jump, fall and run speed
            jumpSpeed = 2f;
            runDelay = 150;
            jumpCounter = StartingPos.Y;
            powerUpCounter = 0;
            activePower = MegaPowerUp.None;
            lastPower = MegaPowerUp.None;
        }
        override public void Jump()
        {
            if(!jumpPressed)
                base.Jump();
        }

        override public void Load(ContentManager Content)
        {
            MegaRunner = Content.Load<Texture2D>(@"Mega");
            MegaJumpShoot = Content.Load<Texture2D>(@"MShootJump");
            
            runnerSprite = new SpriteAnimate(MegaRunner, 2, 4, graphics);
            runnerSprite.SetRange(standRight, standRight);
            runnerSprite.DrawDelay = runDelay;
            runnerSprite.Trim = 0.03f;
            runnerSprite.Zoom = 0.08f;

            jumpSprite = new SpriteAnimate(MegaJumpShoot, 1, 4, graphics);
            jumpSprite.Zoom = 0.08f;
            jumpSprite.Trim = 0.02f;
            jumpSprite.SetRange(jumpRight, jumpRight);

            shootSprite = new SpriteAnimate(MegaJumpShoot, 1, 4, graphics);
            shootSprite.Zoom = 0.08f;
            shootSprite.Trim = 0.02f;
            shootSprite.SetRange(0, 0);

            currentWeapon = new MegaBlaster(graphics);
            currentWeapon.Load(Content);
        }

        public void PowerUp(MegaPowerUp power, int duration)
        {   
                powerUpCounter = duration;
                lastPower = MegaPowerUp.None;
                activePower = power;
            
        }
        public override void Update(GameTime time, List<Collidable> obstacles)
        {

            if (activePower != MegaPowerUp.None)
            {
                if (lastPower == MegaPowerUp.None)
                {
                    lastPower = activePower;
                }
                else
                {
                    powerUpCounter -= time.ElapsedGameTime.Milliseconds;
                }

                if (powerUpCounter <= 0)
                {
                    switch (activePower)
                    {
                        case MegaPowerUp.Jump:
                            currJumpHeight = MAX_JUMP_HEIGHT;
                            break;
                        case MegaPowerUp.Speed:
                            break;
                    }
                    activePower = MegaPowerUp.None;
                }

                switch (activePower)
                {
                    case MegaPowerUp.Jump:
                        currJumpHeight = MAX_JUMP_HEIGHT * 2;
                        break;
                    case MegaPowerUp.Speed:
                        break;
                }
            }
            Collidable c = null;
            bool collide = false;
            if (CollisionDetection(obstacles, out c))
                collide = true;
            //else
            //    collide = CollisionDetection(new List<Collidable> { floor }, out c);

            //Apply checks to see if we are striking the object from top, bottom, left or right
            if (c != null)
            {
                Rectangle collideBounds = c.Bounds;
                Rectangle charBounds = Bounds;
                int xDist = Math.Abs(charBounds.Center.X - collideBounds.Center.X);
                int yDist = Math.Abs(charBounds.Center.Y - collideBounds.Center.Y);

                if (xDist <= yDist)
                {
                    //Handle up-down collision
                    if (charBounds.Top <= collideBounds.Bottom &&
                        charBounds.Bottom > collideBounds.Bottom)
                    {
                        //Start Falling
                        lastAction = subAction;
                        subAction = CharacterSubState.Falling;
                    }
                    else
                    {
                        //Stand Still
                        
                        if (StateIsLeft(currentState))
                        {
                            if (subAction == CharacterSubState.Shooting)
                            {
                                currentState = CharacterState.ShootLeft;
                            }
                            else
                            {
                                if (subAction != CharacterSubState.Jumping)
                                {
                                    if (currentState != CharacterState.Left)
                                        currentState = CharacterState.StillLeft;
                                    lastAction = subAction;
                                    subAction = CharacterSubState.None;
                                }
                            }
                        }
                        else
                        {
                            if (subAction == CharacterSubState.Shooting)
                            {
                                currentState = CharacterState.ShootRight;
                            }
                            else
                            {
                                if (subAction != CharacterSubState.Jumping)
                                {
                                    if(currentState != CharacterState.Right)
                                        currentState = CharacterState.StillRight;
                                    lastAction = subAction;
                                    subAction = CharacterSubState.None;
                                }
                            }
                        }

                        currentPosition.Y = jumpCounter = collideBounds.Top - 1;
                    }
                }
                else
                {
                    //Handle left-right collision
                    if (charBounds.Left <= collideBounds.Right &&
                        charBounds.Right > collideBounds.Right)
                    {
                        currentPosition.X += xDist - ((charBounds.Width / 2) + (collideBounds.Width / 2)) + 1;
                        
                    }
                    else
                    {
                        currentPosition.X -= ((charBounds.Width / 2) + (collideBounds.Width / 2)) - xDist + 1;
                    }

                    if (!StateIsJumping(currentState))
                    {
                        if (StateIsLeft(currentState))
                            currentState = CharacterState.StillLeft;
                        else
                            currentState = CharacterState.StillRight;
                    }
                }
            }

            //Apply gravity if no collisions are detected and not mid-jump
                //Also, checking lastAction for 'jumping' - this causes a 1 frame delay in gravity enforcement.
                //This check allows us to reach max jump height while shooting
            if (!collide &&
                subAction != CharacterSubState.Jumping &&
                lastAction != CharacterSubState.Jumping)
            {
                //Test to see if we are 'clear' to continue falling
                currentPosition.Y += runRate * 2;
                if (!CollisionDetection(obstacles,out c))
                {
                    lastAction = subAction;
                    subAction = CharacterSubState.Falling;
                }
                //If we are 'on top' of an object, act as though we are standing on it
                else
                {
                    if (StateIsLeft(currentState))
                    {
                        if (subAction == CharacterSubState.Shooting)
                            currentState = CharacterState.ShootLeft;
                        else if(currentState != CharacterState.Left)
                            currentState = CharacterState.StillLeft;
                    }
                    else
                    {
                        if (subAction == CharacterSubState.Shooting)
                            currentState = CharacterState.ShootRight;
                        else if (currentState != CharacterState.Right)
                            currentState = CharacterState.StillRight;
                    }
                }

                currentPosition.Y -= runRate * 2;

                if (c != null)
                {
                    if (c.Moving)
                    {
                        jumpCounter = currentPosition.Y = c.Bounds.Top;
                        lastAction = subAction;
                        subAction = CharacterSubState.None;
                    }
                }
            }

            if (currentState == CharacterState.Left)
            {
                currentPosition.X -= runRate;
                if (CollisionDetection(obstacles))
                {
                    currentState = CharacterState.StillLeft;
                    currentPosition.X += runRate;
                }
                else if (lastState != CharacterState.Left)
                    runnerSprite.DrawSequence = runLeft;

            }
            else if (currentState == CharacterState.Right)
            {

                currentPosition.X += runRate;
                if (CollisionDetection(obstacles))
                {
                    currentState = CharacterState.StillRight;
                    currentPosition.X -= runRate;
                }
                else if (lastState != CharacterState.Right)
                    runnerSprite.DrawSequence = runRight;
            }

            if(subAction == CharacterSubState.Falling)
            {
                jumpCounter += runRate;

                currentPosition.Y = jumpCounter;

                //Allows jump/fall to work with shooting
                if (lastAction == CharacterSubState.Shooting)
                {
                    lastAction = subAction;
                    subAction = CharacterSubState.Shooting;
                }

                //Needed to 'correct' mid-air shooting animation
                if (StateIsLeft(currentState))
                {
                    currentState = CharacterState.JumpLeft;
                }
                else
                    currentState = CharacterState.JumpRight;

            }

            if (subAction == CharacterSubState.Shooting)
            {
                Vector2 start;// = new Vector2(currentPosition.X, currentPosition.Y - offset);
                Vector2 end;// = new Vector2(start.X, start.Y);
                if (StateIsLeft(currentState))
                {
                    gunAngle += 180;
                    if(!StateIsJumping(currentState))
                        currentState = CharacterState.ShootLeft;
                }
                else
                {
                    if(!StateIsJumping(currentState))
                        currentState = CharacterState.ShootRight;
                }
                GetVectorFromAngle(gunAngle, out start, out end);

                //if (end.X > graphics.Viewport.Width)
                //    end.X = graphics.Viewport.Width;
                //else if (end.X < 0)
                //    end.X = 0;
                currentWeapon.Fire(start, end);

                //This may need to be expanded to if lastAction != subAction
                    //For now, I just want to make it so 'shooting' does not interrupt reaching max jump height
                if (lastAction == CharacterSubState.Jumping)
                    subAction = CharacterSubState.Jumping;
            }

            
            if (currentState == CharacterState.StillLeft)
            {
                runnerSprite.SetRange(standLeft, standLeft);
            }
            else if (currentState == CharacterState.StillRight)
            {
                runnerSprite.SetRange(standRight, standRight);
            }
            else if (currentState == CharacterState.ShootLeft)
                shootSprite.SetRange(shootLeft, shootLeft);
            else if (currentState == CharacterState.ShootRight)
                shootSprite.SetRange(shootRight, shootRight);

            if (subAction == CharacterSubState.Jumping)
            {
                jumpCounter -= runRate * jumpSpeed;

                if (jumpCounter <= (jumpStart - currJumpHeight))
                {
                    jumpCounter = (jumpStart - currJumpHeight);// -(runRate * jumpSpeed);
                    subAction = CharacterSubState.Falling;
                }

                currentPosition.Y = jumpCounter;
            }

            if (subAction == CharacterSubState.Falling ||
    subAction == CharacterSubState.Jumping)
            {
                if (StateIsLeft(currentState))
                {
                    currentState = CharacterState.JumpLeft;
                    jumpSprite.SetRange(jumpLeft, jumpLeft);
                }
                else
                {
                    currentState = CharacterState.JumpRight;
                    jumpSprite.SetRange(jumpRight, jumpRight);
                }
            }


            runnerSprite.Update(time);
            jumpSprite.Update(time);
            shootSprite.Update(time);
            currentWeapon.Update(time, obstacles);
            base.Update(time, obstacles);
        }

        private void GetVectorFromAngle(float GunAngle, out Vector2 start, out Vector2 end)
        {
            int offsetY = (int)(12 * scale);
            int xStart = 0;
            if (currentState == CharacterState.JumpLeft ||
                currentState == CharacterState.JumpRight)
            {
                offsetY = (int)(16 * scale);
            }

            if (StateIsLeft(currentState))
                xStart = Bounds.Left;
            else
                xStart = Bounds.Right;

            //float oppAngle = 180 - 90 - GunAngle;
            double angleRad = (Math.PI / 180) * GunAngle;
            float yDist = currentPosition.Y;// *-1;
            float xDist = (float)(yDist / Math.Tan(angleRad));
            start = new Vector2(xStart, currentPosition.Y - offsetY);
            end = new Vector2(xDist, yDist - offsetY);
        }

        public bool Collision(Rectangle toCheck)
        {
            return Bounds.Intersects(toCheck);
        }

        public bool Collision(Collidable toCheck)
        {
            return toCheck.Collision(Bounds);
        }

        public Rectangle Bounds
        {
            get
            {
                Rectangle rec;
                if (StateIsJumping(currentState))
                {
                    rec = jumpSprite.GetDestinationRec(currentPosition);
         
                }
                else if (currentState == CharacterState.ShootLeft ||
                    currentState == CharacterState.ShootRight)
                {
                    rec = shootSprite.GetDestinationRec(currentPosition);
                    
                }
                else
                {
                    rec = runnerSprite.GetDestinationRec(currentPosition);
                 
                }

                int newWidth = (int) (rec.Width * 0.8f);
                rec.X += (int)(rec.Width - newWidth) / 2;
                rec.Width = newWidth;

                return rec;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime time)
        {
            if (currentState == CharacterState.JumpLeft ||
                currentState == CharacterState.JumpRight)
            {
                jumpSprite.Draw(spriteBatch, currentPosition);
            }
            else if (currentState == CharacterState.ShootLeft ||
                currentState == CharacterState.ShootRight)
            {
                shootSprite.Draw(spriteBatch, currentPosition);
            }
            else
                runnerSprite.Draw(spriteBatch, currentPosition);
            currentWeapon.Draw(spriteBatch, time);
        }

        private bool CollisionDetection(List<Collidable> obstacles)
        {
            Collidable c = null;
            return CollisionDetection(obstacles, out c);
        }

        private bool CollisionDetection(List<Collidable> obstacles, out Collidable objectCollided)
        {
            Collidable c = null;
            bool detected = false;
            obstacles.ForEach(item =>
            {
                if (Collision(item))
                {
                    if(!detected)
                        c = item;
                    detected = true;
                }
            });
            objectCollided = c;
            return detected;
        }

    }

    class StaticBox : Collidable
    {
        SpriteAnimate theBox;
        protected bool isMoving;
        public bool Moving
        {
            get
            {
                return isMoving;
            }
        }

        protected Vector2 currentPosition;

        public StaticBox(SpriteAnimate Image, Vector2 startingPosition)
        {
            theBox = Image;
            currentPosition = startingPosition;
            isMoving = false;
        }

        public bool Collision(Rectangle toCheck)
        {
            return theBox.GetDestinationRec(currentPosition).Intersects(toCheck);

        }

        public bool Collision(Collidable toCheck)
        {
            return toCheck.Collision(theBox.GetDestinationRec(currentPosition));
        }

        public Rectangle Bounds
        {
            get
            {
                return theBox.GetDestinationRec(currentPosition);
            }
        }

        virtual public void Update(GameTime time)
        {
            theBox.Update(time);
        }

        public void Draw(SpriteBatch batch, GameTime time)
        {
            theBox.Draw(batch, currentPosition);
        }
    }

    class EmptyBox : Collidable
    {
        private Rectangle bounds;
        public Rectangle Bounds
        {
            get
            {
                return bounds;
            }
        }
        public bool Moving
        {
            get
            {
                return false;
            }
        }


        public EmptyBox(Rectangle initRec)
        {
            bounds = initRec;
        }

        public bool Collision(Rectangle rec)
        {
            return rec.Intersects(bounds);
        }

        public bool Collision(Collidable obstacle)
        {
            return obstacle.Bounds.Intersects(bounds);
        }
    }

    class TerrainBox
    {
        SpriteAnimate theBox;
        Vector2 currentPosition;

        public TerrainBox(SpriteAnimate Image, Vector2 startingPosition)
        {
            theBox = Image;
            currentPosition = startingPosition;
        }

        public void Update(GameTime time)
        {
            theBox.Update(time);
        }

        public void Draw(SpriteBatch batch, GameTime time)
        {
            theBox.Draw(batch, currentPosition);
        }
    }

    class ElevatorBox : StaticBox
    {
        Vector2 startHeight;
        public float MaxHeight { get; set; }
        int direction;
        int riseRate;
        
        public ElevatorBox(SpriteAnimate Image, Vector2 startingPosition) :
            base(Image, startingPosition)
        {
            startHeight = startingPosition;
            MaxHeight = 128;
            direction = -1;
            riseRate = 2;
            isMoving = true;
        }

        public override void Update(GameTime time)
        {
            if (currentPosition.Y < startHeight.Y - MaxHeight ||
                currentPosition.Y > startHeight.Y)
            {
                direction *= -1;
            }

            currentPosition.Y += riseRate * direction;

            base.Update(time);
        }
    }
}
