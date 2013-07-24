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

        virtual public void Teleport(Vector2 position)
        {
            currentPosition = position;
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

    interface Enemy : Collidable
    {
        int Health { get; set; }
        int Damage { get; }
        void Update(GameTime gameTime, List<Collidable> collidables);
        void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }

    class Snail : Character, Enemy
    {
        static List<int> moveSeq;
        static int deathSeq;
        Texture2D snailImage;

        SpriteAnimate snailSprite;
        int runRate;
        int runDelay;
        int isLeft;
        bool isDead;
        int deathCounter;

        public bool Moving
        {
            get
            {
                return false;
            }
        }

        public int Health { get; set; }

        public int Damage
        {
            get
            {
                isLeft *= -1;
                return 1;
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
            Health = 6;
            isDead = false;
            deathCounter = 0;
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
            if (!isDead)
            {
                currentPosition.X += runRate * isLeft;
                Rectangle belowMe = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
                belowMe.Y += belowMe.Height;
                belowMe.X += (belowMe.Width / 2) * isLeft;
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
            }
            if (Health <= 0)
            {
                isDead = true;
            }

            snailSprite.Update(time);
            base.Update(time, obstacles);
        }

        public bool Collision(Rectangle toCheck)
        {
            if (isDead)
                return false;
            return Bounds.Intersects(toCheck);
        }

        public bool Collision(Collidable toCheck)
        {
            if (isDead)
                return false;
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
            if (deathCounter < 20000)
            {
                //if we are right, need to flip
                if (isLeft == 1)
                {
                    snailSprite.FlipHorizontal = true;
                }
                else
                    snailSprite.FlipHorizontal = false;
                if (isDead)
                {
                    if (deathCounter == 0)
                        snailSprite.SetRange(deathSeq, deathSeq);
                    deathCounter += time.ElapsedGameTime.Milliseconds;
                    float rotationAngle = deathCounter / 1000.0f;
                    if (rotationAngle <= MathHelper.Pi)
                    {
                        //float circle = MathHelper.Pi * 2;
                        //rotationAngle = rotationAngle % circle;
                        snailSprite.Rotate = MathHelper.Pi - rotationAngle;
                    }
                }
                snailSprite.Draw(spriteBatch, currentPosition);
            }
        }
    }

    enum MegaPowerUp
    {
        None,
        Speed,
        Jump,
        Invincible
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

        int blinkCounter;

        private List<KeyValuePair<MegaPowerUp, int>> activePower;
        public List<KeyValuePair<MegaPowerUp, int>> ActivePower { get { return activePower; } }
        bool isInvincible;
        float scale;
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        
        

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
            activePower = new List<KeyValuePair<MegaPowerUp, int>>();
            Health = MaxHealth = 6;
            isInvincible = false;
            blinkCounter = runDelay;
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
            runnerSprite.ScrollY = 2;

            jumpSprite = new SpriteAnimate(MegaJumpShoot, 1, 4, graphics);
            jumpSprite.Zoom = 0.08f;
            jumpSprite.Trim = 0.02f;
            jumpSprite.SetRange(jumpRight, jumpRight);

            shootSprite = new SpriteAnimate(MegaJumpShoot, 1, 4, graphics);
            shootSprite.Zoom = 0.08f;
            shootSprite.Trim = 0.02f;
            shootSprite.SetRange(0, 0);
            shootSprite.ScrollY = 2;

            currentWeapon = new MegaBlaster(graphics);
            currentWeapon.Load(Content);
        }

        public void PowerUp(MegaPowerUp power, int duration)
        {
            bool found = false;
            activePower.ForEach(powerUp =>
                {
                    if (powerUp.Key == power)
                        found = true;
                });

            if (!found)
            {
                KeyValuePair<MegaPowerUp, int> newPowerUp = new KeyValuePair<MegaPowerUp, int>(power, duration);
                activePower.Add(newPowerUp);
            }
            
        }
        public void Update(GameTime time, List<Collidable> obstacles, List<Enemy> enemies)
        {
            List<int> toRemove = new List<int>();
            activePower.ForEach(power =>
            {
                MegaPowerUp thePower = power.Key;
                int timeLeft = power.Value;
                KeyValuePair<MegaPowerUp, int> newPower = new KeyValuePair<MegaPowerUp, int>(thePower, timeLeft - time.ElapsedGameTime.Milliseconds);
                activePower[activePower.IndexOf(power)] = newPower;

                if (newPower.Value <= 0)
                {
                    switch (newPower.Key)
                    {
                        case MegaPowerUp.Jump:
                            currJumpHeight = MAX_JUMP_HEIGHT;
                            break;
                        case MegaPowerUp.Speed:
                            break;
                        case MegaPowerUp.Invincible:
                            isInvincible = false;
                            break;
                    }
                    toRemove.Add(activePower.IndexOf(newPower));

                }
                else
                {
                    switch (newPower.Key)
                    {
                        case MegaPowerUp.Jump:
                            currJumpHeight = MAX_JUMP_HEIGHT * 2;
                            break;
                        case MegaPowerUp.Speed:
                            break;
                        case MegaPowerUp.Invincible:
                            isInvincible = true;
                            break;
                    }
                }
            });

            toRemove = toRemove.OrderByDescending(item => item).ToList();
            toRemove.ForEach(item =>
                {
                    activePower.RemoveAt(item);
                });
            Collidable c = null;
            Enemy e = null;
            if (EnemyCollision(enemies, out e))
            {
                Health -= e.Damage;
                //Damage animation

                //Apply invincibility for 5 seconds
                PowerUp(MegaPowerUp.Invincible, 5000);
                return;
            }
            //Apply gravity if no collisions are detected and not mid-jump
                //Also, checking lastAction for 'jumping' - this causes a 1 frame delay in gravity enforcement.
                //This check allows us to reach max jump height while shooting
            if (subAction != CharacterSubState.Jumping &&
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
                        jumpCounter = currentPosition.Y = c.Bounds.Top - 1;
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

            CollisionDetection(obstacles, out c);
            //else
            //    collide = CollisionDetection(new List<Collidable> { floor }, out c);

            //Apply checks to see if we are striking the object from top, bottom, left or right
            if (c != null)
            {
                Rectangle collideBounds = c.Bounds;
                Rectangle charBounds = Bounds;

                //Handle up-down collision
                if (charBounds.Top <= collideBounds.Bottom &&
                    charBounds.Bottom > collideBounds.Bottom)
                {
                    //Start Falling
                    lastAction = subAction;
                    subAction = CharacterSubState.Falling;
                }
                else if (charBounds.Bottom >= collideBounds.Top &&
                    charBounds.Top < collideBounds.Top)
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
                                if (currentState != CharacterState.Right)
                                    currentState = CharacterState.StillRight;
                                lastAction = subAction;
                                subAction = CharacterSubState.None;
                            }
                        }
                    }

                    currentPosition.Y = jumpCounter = collideBounds.Top - 1;
                }
            }

            List<Collidable> obstaclesNEnemies = new List<Collidable>();
            obstaclesNEnemies.AddRange(obstacles);
            obstaclesNEnemies.AddRange(enemies);

            runnerSprite.Update(time);
            jumpSprite.Update(time);
            shootSprite.Update(time);
            currentWeapon.Update(time, obstaclesNEnemies);
            base.Update(time, obstacles);
        }

        public override void Teleport(Vector2 position)
        {
            jumpCounter = position.Y;
            base.Teleport(position);
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
                    rec = runnerSprite.GetDestinationRec(currentPosition);

                int newWidth = (int) (rec.Width * 0.8f);
                int newHeight = (int)(rec.Height * 0.8f);
                //rec.X += (int)(rec.Width - newWidth) / 2;
                rec.Width = newWidth;
                //rec.Height = newHeight;

                return rec;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime time)
        {
            bool toDraw = true;
            if (isInvincible)
            {
                if (blinkCounter >= runDelay)
                {
                    toDraw = false;
                    blinkCounter -= time.ElapsedGameTime.Milliseconds;
                }
                else
                {
                    blinkCounter -= time.ElapsedGameTime.Milliseconds;
                    if (blinkCounter <= -runDelay)
                        blinkCounter = runDelay;
                }
            }

            if (toDraw)
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
            }
            currentWeapon.Draw(spriteBatch, time);
        }

        private bool EnemyCollision(List<Enemy> obstacles, out Enemy enemyCollided)
        {
            bool collided = false;
            Collidable c = null;
            if (!isInvincible)
            {
                List<Collidable> collList = new List<Collidable>();
                
                obstacles.ForEach(item =>
                    {
                        collList.Add(item);
                    });

                collided = CollisionDetection(collList, out c);
            }

            enemyCollided = c as Enemy;
            return collided;
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
                    if (!detected)
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
        public SpriteAnimate theBox;
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
