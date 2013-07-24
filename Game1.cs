#region Using Statements
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using TextureAtlas;
using Projectile;
using Weapon;
using Character;
#endregion

namespace FirstGame
{

    enum Layer
    {
        Coin,
        Collidable,
        StartPosition,
        BackObjects,
        Snail
    }

    enum LevelLayer
    {
        Coin,
        Obstacle,
        Platform,
        StartPosition,
        BackObjects,
        Death,
        Enemies
    }

    class Level1
    {
        private int[,] FullLevel;
        private int[,] LoadedLevel;
        List<Collidable> obstacles;
        List<TerrainBox> terrain;
        List<StaticBox> coins;
        List<ElevatorBox> platforms;
        List<EmptyBox> deathRec;
        List<TerrainBox> healthBar;
        List<TerrainBox> powerUps;
        List<Enemy> enemies;
        ContentManager content;
        Texture2D levelSprites;
        GraphicsDevice graphics;
        Texture2D hudItems;
        SpriteAnimate background;
        Vector2 backgroundLoc;
        SpriteFont tahoma;
        SpriteBatch spriteBatch;
        bool bWon;
        public bool Won
        {
            get
            {
                return bWon;
            }
        }

        bool bLost;
        public bool Lost
        {
            get
            {
                return bLost;
            }
        }
        int lostCounter;
        public int LostCounter { 
            get
            {
                return lostCounter;
            }
        }

        float cameraMovementX;
        float cameraMovementY;

        public Vector2 CameraMovement
        {
            get
            {
                return new Vector2(cameraMovementX, cameraMovementY);
            }
        }

        MegaMan theMan;

        public Character.Character TheMan
        {
            get
            {
                return theMan;
            }
        }

        string levelName;

        public Level1(int rows, int columns, int loadWidth, int loadHeight, ContentManager Content, GraphicsDevice Graphics)
        {
            content = Content;
            FullLevel = new int [rows,columns];
            LoadedLevel = new int[loadWidth, loadHeight];
            levelName = @"\Level1.xml";
            levelSprites = content.Load<Texture2D>(@"JnRTiles");
            graphics = Graphics;
            cameraMovementX = cameraMovementY = 0.0f;
            bWon = bLost = false;
            tahoma = content.Load<SpriteFont>(@"tahoma");
            theMan = new MegaMan(Vector2.Zero, graphics);
            theMan.Load(content);
            hudItems = Content.Load<Texture2D>("hud_heartGems");
            healthBar = new List<TerrainBox>();
            powerUps = new List<TerrainBox>();
            for (int i = 0; i < 3; i++)
            {
                SpriteAnimate heart = new SpriteAnimate(hudItems, 2, 3, graphics);
                heart.SetRange(0, 0);
                heart.Zoom = 0.5f;
                healthBar.Add(new TerrainBox(heart,
                    new Vector2(i * heart.GetDestinationRec(Vector2.Zero).Width + heart.GetDestinationRec(Vector2.Zero).Width / 2,
                        heart.GetDestinationRec(Vector2.Zero).Height + heart.GetDestinationRec(Vector2.Zero).Height / 2)));
            }
            background = new SpriteAnimate(Content.Load<Texture2D>(@"Background"), 1, 1, graphics);
            background.SetRange(0, 0);
            background.Zoom = 2.0f;
            backgroundLoc = new Vector2(graphics.Viewport.Width / 2, graphics.Viewport.Height);
            spriteBatch = new SpriteBatch(graphics);

            LoadLevelAndStartPosition();
        }

        void LoadLayer(LevelLayer theLayer, XmlReader reader, float scaledX, Texture2D levelSprites)
        {

            bool bStone, bCoin, bStart, bObstacle, bEnemy;
            bStone = bCoin = bStart = bObstacle = bEnemy = false;
            switch (theLayer)
            {
                case LevelLayer.BackObjects:
                    bStone = true;
                    break;
                case LevelLayer.Coin:
                    bCoin = true;
                    break;
                case LevelLayer.Obstacle:
                    bObstacle = true;
                    break;
                case LevelLayer.StartPosition:
                    bStart = true;
                    break;
                case LevelLayer.Enemies:
                    bEnemy = true;
                    break;
                default:
                    return;
            }
            int row = 0;
            int column = 0;
            int[,] layout = new int[100, 100];
            reader.ReadToFollowing("tile");
            if (bStart)
            {
                Vector2 startPos = new Vector2();

                do
                {

                    reader.MoveToAttribute("gid");
                    int.TryParse(reader.Value, out layout[column, row]);
                    if (layout[column, row] > 0)
                    {
                        startPos.X = (scaledX * (column + 1)) - scaledX / 2;
                        startPos.Y = scaledX * row;
                        //cameraMovementY -= graphics.Viewport.Height - (scaledX * 3);
                    }
                    column++;
                    if (column > 99)
                    {
                        column = 0;
                        row++;
                    }
                    reader.Read();
                    reader.Read();
                } while (reader.Name == "tile");

                theMan.Teleport(startPos);
                CheckCameraPosition();
                cameraMovementY -= (32 * 5) * scaledX;
            }
            else
            {
                do
                {

                    reader.MoveToAttribute("gid");
                    int.TryParse(reader.Value, out layout[column, row]);
                    column++;
                    if (column > 99)
                    {
                        column = 0;
                        row++;
                    }
                    reader.Read();
                    reader.Read();
                } while (reader.Name == "tile");
                //0, 62 should be the very bottom-left of the screen
                //0, 47 should be top left
                //25, 62 should be the bottom right
                //32 is the width and height

                Random r = new Random(); //Used to randomize elevator boxes
                for (int i = 0; i < 100; i++)
                {
                    for (int j = 0; j < 100; j++)
                    {
                        int sequence = layout[i, j];
                        if (sequence > 0)
                        {
                            SpriteAnimate sprite = new SpriteAnimate(levelSprites, 1, 18, graphics);
                            sprite.SetRange(sequence - 1, sequence - 1);
                            Vector2 position = new Vector2((scaledX * (i + 1)) - scaledX / 2, scaledX * j);
                            if (bCoin)
                                coins.Add(new StaticBox(sprite, position));
                            else if (bStone)
                                terrain.Add(new TerrainBox(sprite, position));
                            else if (bObstacle)
                            {
                                if (sequence == 17)
                                {
                                    ElevatorBox e = new ElevatorBox(sprite, position);
                                    e.MaxHeight += r.Next(0, 10);
                                    platforms.Add(e);
                                }
                                else
                                    obstacles.Add(new StaticBox(sprite, position));
                            }
                            else if (bEnemy)
                            {
                                Snail s = new Snail(position, graphics);
                                s.Load(content);
                                enemies.Add(s);
                            }
                        }
                    }
                }
            }
        }

        void LoadLevelAndStartPosition()
        {
            
            obstacles = new List<Collidable>();
            terrain = new List<TerrainBox>();
            coins = new List<StaticBox>();
            deathRec = new List<EmptyBox>();
            platforms = new List<ElevatorBox>();
            enemies = new List<Enemy>();
            using (XmlReader reader = XmlReader.Create(new StreamReader(content.RootDirectory + levelName)))
            {
                string tilesetImage = "";
                int numTiles = 0;
                int tileWidth = 0;
                reader.Read();
                reader.ReadToFollowing("map");
                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == "tilewidth")
                    {
                        int.TryParse(reader.Value, out 
                    tileWidth);
                        break;
                    }
                }

                reader.ReadToFollowing("image");
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "source":
                            tilesetImage = reader.Value;
                            break;
                        case "tileWidth":
                            int width = 0;
                            int.TryParse(reader.Value, out tileWidth);
                            numTiles = width / tileWidth;
                            break;
                    }

                }


                int layerCount = 0;
                float scaledX = tileWidth;
                float scale = 1.0f;

                scale = (float)graphics.Viewport.Height / 480;
                scaledX *= scale;


                while (reader.ReadToFollowing("layer"))
                {
                    layerCount++;
                    reader.MoveToAttribute("name");
                    switch (reader.Name)
                    {
                        case "name":
                            if (reader.Value == "Background Stone")
                            {
                                LoadLayer(LevelLayer.BackObjects, reader, scaledX, levelSprites);
                            }
                            else if (reader.Value == "Coin Layer")
                            {
                                LoadLayer(LevelLayer.Coin, reader, scaledX, levelSprites);

                            }
                            else if (reader.Value == "Collide Layer")
                            {
                                LoadLayer(LevelLayer.Obstacle, reader, scaledX, levelSprites);
                            }
                            else if (reader.Value == "Starting Position")
                            {
                                LoadLayer(LevelLayer.StartPosition, reader, scaledX, levelSprites);

                            }
                            else if (reader.Value == "Snail Layer")
                            {
                                LoadLayer(LevelLayer.Enemies, reader, scaledX, null);
                            }
                            break;
                    }
                    if (layerCount == 5)
                        break;
                }


                if (reader.ReadToFollowing("objectgroup"))
                {
                    while (reader.ReadToFollowing("object"))
                    {
                        Rectangle rec = new Rectangle();
                        while (reader.MoveToNextAttribute())
                        {

                            switch (reader.Name)
                            {
                                case "x":
                                    int.TryParse(reader.Value, out rec.X);
                                    rec.X = (int)(rec.X * scale);
                                    break;
                                case "y":
                                    int.TryParse(reader.Value, out rec.Y);
                                    rec.Y = (int)(rec.Y * scale);
                                    break;
                                case "width":
                                    int.TryParse(reader.Value, out rec.Width);
                                    rec.Width = (int)(rec.Width * scale);
                                    break;
                                case "height":
                                    int.TryParse(reader.Value, out rec.Height);
                                    rec.Height = (int)(rec.Height * scale);
                                    break;
                            }
                        }
                        deathRec.Add(new EmptyBox(rec));
                    }
                }

            }

        }

        void CheckCameraPosition()
        {
            const float ViewMarginX = 0.35f;
            const float ViewMarginY = 0.20f;


            // Calculate the edges of the screen.

            float marginWidth = graphics.Viewport.Width * ViewMarginX;
            float marginHeight = graphics.Viewport.Height * ViewMarginY;

            float marginLeft = cameraMovementX + marginWidth;
            float marginRight = cameraMovementX + graphics.Viewport.Width - marginWidth;

            float marginTop = cameraMovementY + marginHeight;
            float marginBottom = cameraMovementY + graphics.Viewport.Height - marginHeight;

            // Calculate how far to scroll when the player is near the edges of the screen.
            float camMoveX = 0.0f;
            float camMoveY = 0.0f;
            if (theMan.Bounds.Left < marginLeft)
                camMoveX = theMan.Bounds.Left - marginLeft;
            else if (theMan.Bounds.Right > marginRight)
                camMoveX = theMan.Bounds.Right - marginRight;

            if (theMan.Bounds.Top < marginTop)
                camMoveY = theMan.Bounds.Top - marginTop;
            else if (theMan.Bounds.Bottom > marginBottom)
                camMoveY = theMan.Bounds.Bottom - marginBottom;

            // Update the camera position, but prevent scrolling off the ends of the level.

            float maxCameraPositionX = obstacles[0].Bounds.Width * 100 - graphics.Viewport.Width; //TODO: 100 should be sizeof level width
            cameraMovementX = MathHelper.Clamp(cameraMovementX + camMoveX, 0.0f, maxCameraPositionX);

            float maxCameraPositionY = obstacles[0].Bounds.Height * 100 - graphics.Viewport.Height; //TODO: 100 should be sizeof level width
            cameraMovementY = MathHelper.Clamp(cameraMovementY + camMoveY, 0.0f, maxCameraPositionY);
        }

        public void Update(GameTime gameTime)
        {
            platforms.ForEach(item =>
            {
                item.Update(gameTime);
            });
            foreach (StaticBox c in obstacles)
            {
                c.Update(gameTime);
            }

            terrain.ForEach(item =>
            {
                item.Update(gameTime);
            });

            List<int> toRemove = new List<int>();
            coins.ForEach(item =>
            {
                item.Update(gameTime);
                if (theMan.Collision(item))
                {
                    toRemove.Add(coins.IndexOf(item));
                    theMan.PowerUp(MegaPowerUp.Jump, 10000);

                    SpriteAnimate gem = new SpriteAnimate(hudItems, 2, 3, graphics);
                    gem.SetRange(3, 3);
                    gem.Zoom = 0.5f;
                    powerUps.Add(new TerrainBox(gem,
                        new Vector2(graphics.Viewport.Width - (gem.GetDestinationRec(Vector2.Zero).Width * 1.5f),
                        gem.GetDestinationRec(Vector2.Zero).Height + gem.GetDestinationRec(Vector2.Zero).Height / 2)));

                }
            });

            if (powerUps.Count > 0)
                if (theMan.ActivePower.Count == 0)
                    powerUps.Clear();

            toRemove.ForEach(item =>
            {
                coins.RemoveAt(item);
            });
            List<Collidable> obstaclesNWalls = new List<Collidable>();
            List<Rectangle> walls = new List<Rectangle>();
            Rectangle left, right, top, bottom;
            left = new Rectangle(graphics.Viewport.Bounds.Left - 5, graphics.Viewport.Y, 5, graphics.Viewport.Height);
            right = new Rectangle(graphics.Viewport.Bounds.Right, graphics.Viewport.Y, 5, graphics.Viewport.Height);
            top = new Rectangle(graphics.Viewport.Bounds.Left, graphics.Viewport.Bounds.Top - 5, graphics.Viewport.Width, 5);
            bottom = new Rectangle(graphics.Viewport.Bounds.Left, graphics.Viewport.Bounds.Bottom, graphics.Viewport.Width, 5);

            walls.Add(left);
            walls.Add(right);
            walls.Add(top);
            walls.Add(bottom);

            walls.ForEach(item =>
            {
                item.X += (int)cameraMovementX;
                item.Y += (int)cameraMovementY;
                obstaclesNWalls.Add(new EmptyBox(item));
            });

            obstaclesNWalls.AddRange(obstacles);

            obstaclesNWalls.AddRange(platforms);

            //Update enemies and add them to a list to send to the 'character' for special collision detection rules
            List<Enemy> badGuys = new List<Enemy>();
            enemies.ForEach(item =>
            {
                item.Update(gameTime, obstaclesNWalls);
                badGuys.Add(item);
            });

            theMan.Update(gameTime, obstaclesNWalls, badGuys);

            //Check to see if you fell into a 'pit'
            deathRec.ForEach(item =>
            {
                if (item.Bounds.Contains(theMan.Bounds.Center))
                {
                    bLost = true;
                    theMan.Health = 0;
                }
            });

            //Update Health Level
            int currHP = theMan.Health;
            if (currHP <= 0)
                bLost = true;
            healthBar.ForEach(item =>
            {
                if (currHP > 1)
                {
                    item.theBox.SetRange(0, 0);
                    currHP -= 2;
                }
                else if (currHP > 0)
                {
                    item.theBox.SetRange(1, 1);
                    currHP -= 1;
                }
                else
                {
                    item.theBox.SetRange(2, 2);
                }
            });

            if (bWon || bLost)
            {
                if (bLost)
                {
                    lostCounter++;
                }
            }

            CheckCameraPosition();
        }

        public void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            background.Draw(spriteBatch, backgroundLoc);
            spriteBatch.End();

            Matrix cameraTransform = Matrix.CreateTranslation(-cameraMovementX, -cameraMovementY, 0.0f);// *Matrix.CreateScale(curZoom, curZoom, 1); ;
            //Could set effects here if theMan.EffectIsActive(enum.Effect);
            //Effect effect = new Effect(GraphicsDevice, {0});
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp,
                DepthStencilState.Default, RasterizerState.CullNone, null, cameraTransform);

            platforms.ForEach(item =>
            {
                item.Draw(spriteBatch, gameTime);
            });

            terrain.ForEach(item =>
            {
                item.Draw(spriteBatch, gameTime);
            });

            coins.ForEach(item =>
            {
                item.Draw(spriteBatch, gameTime);
            });

            foreach (StaticBox c in obstacles)
            {
                c.Draw(spriteBatch, gameTime);
            }

            enemies.ForEach(item =>
            {
                item.Draw(spriteBatch, gameTime);
            });

            theMan.Draw(spriteBatch, gameTime);

            //DEBUG
            //deathRec.ForEach(item =>
            //    {
            //        spriteBatch.Draw(Content.Load<Texture2D>(@"JnRTiles"), item.Bounds, Color.Red);
            //    });
            if (bWon || bLost)
            {
                string output = "You Win!";
                if (bLost)
                {
                    output = "Game Over...";
                }

                // Find the center of the string
                Vector2 FontOrigin = tahoma.MeasureString(output) / 2;
                spriteBatch.DrawString(tahoma, output, new Vector2((graphics.Viewport.Width / 2) + cameraMovementX, cameraMovementY + 50), Color.Wheat,
                    0f, FontOrigin, 1.0f, SpriteEffects.None, 0.5f);
            }

            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.AnisotropicClamp,
                DepthStencilState.Default, RasterizerState.CullNone);
            healthBar.ForEach(item =>
            {
                item.Draw(spriteBatch, gameTime);
            });

            powerUps.ForEach(item =>
            {
                item.Draw(spriteBatch, gameTime);
            });
            spriteBatch.End();
        }
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        
        GraphicsDeviceManager graphics;
        //SpriteBatch spriteBatch;

        //Character.MegaMan theMan;
        //List<Collidable> obstacles;
        //List<TerrainBox> terrain;
        //List<StaticBox> coins;
        //List<ElevatorBox> platforms;
        //List<EmptyBox> deathRec;
        //List<Snail> enemies;
        //List<TerrainBox> healthBar;
        //List<TerrainBox> powerUps;
        //Texture2D hudItems;
        //SpriteAnimate background;
        //Vector2 backgroundLoc;
        //SpriteFont tahoma;
        //bool bWon;
        //bool bLost;
        //int lostCounter;
        //float cameraMovementX;
        //float cameraMovementY;
        //float curZoom;
        //Weapon.MegaBlaster blaster;
        Level1 level1;

        public Game1()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            
            Content.RootDirectory = "Content";
            //bWon = false;
            //bLost = false;
            //lostCounter = 0;
            //cameraMovementX = 0.0f;
            //cameraMovementY = 0.0f;
            //curZoom = 0.0f;

            //TODO Test Fullscreen:
            //graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            //graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            //graphics.IsFullScreen = true;
            //graphics.ApplyChanges();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            
            base.Initialize();

        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            level1 = new Level1(100, 100, 100, 100, Content, GraphicsDevice);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (GamePad.GetState(PlayerIndex.One).Buttons.LeftStick == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                if(!level1.Lost)
                    level1.TheMan.Left();
            }
            else if (GamePad.GetState(PlayerIndex.One).Buttons.RightStick == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                if (!level1.Lost)
                    level1.TheMan.Right();
            }
            else
            {
                level1.TheMan.Still();
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                level1.TheMan.Jump();
            }
            else
            {
                level1.TheMan.StopJump();
            }

            //Process Request to Shoot
            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
            {
                level1.TheMan.Shoot(0); //Currently using a constant, should be 'degrees'
                //This Character is hardcoded to know that 'left-facing' should add 180 to degrees (internally)
            }
            else
                level1.TheMan.StopShoot();

            

            level1.Update(gameTime);
            if(level1.Lost)
                if (level1.LostCounter > 150)
                    Exit();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {   
            GraphicsDevice.Clear(Color.CornflowerBlue);

            level1.Draw(gameTime);

            base.Draw(gameTime);
        }
    }
}
