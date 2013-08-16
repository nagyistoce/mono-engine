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
using Storage;
#endregion

namespace FirstGame
{

    //enum Layer
    //{
    //    Coin,
    //    Collidable,
    //    StartPosition,
    //    BackObjects,
    //    Snail
    //}

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

    class Level
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

        public Level(int rows, int columns, int loadWidth, int loadHeight, ContentManager Content, GraphicsDevice Graphics)
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

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
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

    public class MenuItem
    {
        SpriteAnimate buttonSprite;
        SpriteAnimate buttonBubble;
        Rectangle buttonRec;
        SpriteFont textFont;
        public bool Selected { get; set; }
        string text;
        public string Text
        {
            get
            {
                return text;
            }
        }
        Vector2 drawPosition;

        public MenuItem(string ButtonText, SpriteFont TextFont, Vector2 CenterPosition)
        {
            text = ButtonText;
            textFont = TextFont;
            drawPosition = CenterPosition;
            Selected = false;
            
        }

        public void SetBubbleGreen()
        {
            buttonBubble.SetRange(0, 0);
        }

        public void SetBubbleBlue()
        {
            buttonBubble.SetRange(1, 1);
        }

        public void SetBubbleRed()
        {
            buttonBubble.SetRange(2, 2);
        }

        public void Load(ContentManager Content, GraphicsDevice graphics)
        {
            Vector2 textSize = textFont.MeasureString(text);
            buttonRec = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, (int)textSize.X, (int)textSize.Y);
            Texture2D buttonTex = Content.Load<Texture2D>(@"UITextBox");
            buttonBubble = new SpriteAnimate(Content.Load<Texture2D>(@"UI3Buttons"), 1, 3, graphics);
            buttonBubble.SetRange(2, 2);
            int origHeight = buttonBubble.Height;
            buttonBubble.Height = (int)(textSize.Y * 2.25);
            //buttonBubble.Width = buttonBubble.Height;
            buttonBubble.Width = buttonBubble.Height;//(int)((float)(buttonBubble.Height / origHeight) * buttonBubble.Width);

            buttonSprite = new SpriteAnimate(buttonTex, 1, 1, graphics);
            buttonSprite.SetRange(0, 0);
            
            buttonSprite.Width = (int)(textSize.X * 2.25);
            buttonSprite.Height = (int)(textSize.Y * 2.25);
                //new Texture2D(graphics, 1, 1);

        }

         public void Update(GameTime time)
        {

        }

         //Assumes spriteBatch.Begin() has already been called
         public void Draw(GameTime time, SpriteBatch spriteBatch)
         {

             Color textColor = Color.White;

             if (Selected)
             {
                 textColor = Color.SlateBlue;
             }
             //else
             //{

             //}
             Vector2 FontOrigin = textFont.MeasureString(text) / 2;

             //spriteBatch.Draw(buttonTex, new Rectangle((int)drawPosition.X - 20, (int)drawPosition.Y - 5,
             //    (int)((FontOrigin * 2).X * 4), (int)((FontOrigin * 2).Y * 2)),
             //    null,
             //    Color.White,
             //    0f,
             //    FontOrigin,
             //    SpriteEffects.None,
             //    0.5f);
             Vector2 boxPosition = new Vector2(drawPosition.X, drawPosition.Y + FontOrigin.Y*2.25f);
             Rectangle box = buttonSprite.GetDestinationRec(boxPosition);
             Vector2 bubblePosition = new Vector2( box.Left + (box.Width * 0.10f) - (buttonBubble.Width / 2), boxPosition.Y);

             buttonSprite.Draw(spriteBatch, boxPosition);//new Vector2(drawPosition.X, drawPosition.Y));
             buttonBubble.Draw(spriteBatch, bubblePosition);
             
             spriteBatch.DrawString(textFont, text, drawPosition, textColor,
                 0f, FontOrigin, 1.0f, SpriteEffects.None, 0.5f);
         }
    }

    public class Menu
    {
        protected List<MenuItem> buttons;
        protected string selectedButtonText;

        public Menu()
        {
            buttons = new List<MenuItem>();
            selectedButtonText = "";
        }

        protected Color CreateTransparency(Byte transparency_amount)
        {
            return Color.FromNonPremultiplied(10, 10, 10, transparency_amount);
        }

        virtual public void Update(GameTime time)
        {
            buttons.ForEach(item =>
            {
                item.Update(time);
            });
        }

        virtual public void Draw(GameTime time, SpriteBatch spriteBatch)
        {
            if (buttons.Count == 0)
                return;

            spriteBatch.Begin();

            buttons.ForEach(item =>
            {
                item.Draw(time, spriteBatch);
            });

            spriteBatch.End();
        }

        virtual public void Select(Game1 theGame)
        {
            int oldIndex = -1;
            buttons.ForEach(item =>
            {
                if (item.Selected)
                    oldIndex = buttons.IndexOf(item);
            });

            if (oldIndex != -1)
                selectedButtonText = buttons[oldIndex].Text;
            else
                selectedButtonText = "";
        }

        public void Up()
        {
            AlterSelection(-1);
        }

        public void Down()
        {
            AlterSelection(1);
        }

        protected void AlterSelection(int movement)
        {
            int oldIndex = -1;
            buttons.ForEach(item =>
            {
                if (item.Selected)
                    oldIndex = buttons.IndexOf(item);
            });

            if (oldIndex >= 0)
            {
                buttons[oldIndex].Selected = false;
                if ((oldIndex + movement) == buttons.Count)
                {
                    buttons[0].Selected = true;
                }
                else if ((oldIndex + movement) < 0)
                    buttons[buttons.Count + movement].Selected = true;
                else
                    buttons[oldIndex + movement].Selected = true;
            }
        }
    }

    public class PauseMenu : Menu
    {
        Texture2D backgroundOverlay;
        Texture2D foreGroundWindow;
        SpriteFont textFont;
        Rectangle screenSize;
        Rectangle menuSize;
        Color backgroundColor;

        public PauseMenu() : base()
        {

        }

        public void Load(ContentManager Content, GraphicsDevice graphics)
        {
            textFont = Content.Load<SpriteFont>(@"Tahoma");
            screenSize = graphics.Viewport.Bounds;

            float scaleX = 0.35f;
            float scaleY = 0.35f;
            menuSize = screenSize;
            menuSize.X += (int)(screenSize.Width * scaleX) / 2;
            menuSize.Y += (int)(screenSize.Height * scaleY) / 2;
            menuSize.Width = (int)(menuSize.Width * (1.0 - scaleX));
            menuSize.Height = (int)(menuSize.Height * (1.0 - scaleY));

            backgroundColor = CreateTransparency(200); //0 is transparent, 255 is opaque
            backgroundOverlay = new Texture2D(graphics, 1, 1);
            backgroundOverlay.SetData<Color>(new Color[] { backgroundColor });

            foreGroundWindow = new Texture2D(graphics, 1, 1);
            foreGroundWindow.SetData<Color>(new Color[] { Color.DarkSlateGray });

            Vector2 LoadPos = new Vector2(screenSize.Width / 2.0f, menuSize.Bottom - (menuSize.Height * 0.9f));
            MenuItem loadButton = new MenuItem("Load", textFont, LoadPos);
            loadButton.Load(Content, graphics);
            loadButton.SetBubbleGreen();
            buttons.Add(loadButton);

            Vector2 savePos = new Vector2(screenSize.Width / 2.0f, menuSize.Bottom - (menuSize.Height * 0.7f));
            MenuItem saveButton = new MenuItem("Save", textFont, savePos);
            saveButton.Load(Content, graphics);
            saveButton.SetBubbleBlue();
            saveButton.Selected = true;
            selectedButtonText = saveButton.Text;
            buttons.Add(saveButton);

            Vector2 exitPos = new Vector2(screenSize.Width / 2.0f, menuSize.Bottom - (menuSize.Height * 0.10f));
            MenuItem exitButton = new MenuItem("Exit", textFont, exitPos);
            exitButton.Load(Content, graphics);
            buttons.Add(exitButton);
        }

        override public void Draw(GameTime time, SpriteBatch spriteBatch)
        {

            spriteBatch.Begin();

            spriteBatch.Draw(backgroundOverlay, screenSize, backgroundColor);
            Color [] c = new Color[1];
            foreGroundWindow.GetData<Color>(c,0,1);
            spriteBatch.Draw(foreGroundWindow, menuSize, c[0]);

            
            string output = "Paused...";
            Vector2 FontOrigin = textFont.MeasureString(output) / 2;
            spriteBatch.DrawString(textFont, output, new Vector2((screenSize.Width / 2), 50), Color.Wheat,
                0f, FontOrigin, 1.0f, SpriteEffects.None, 0.5f);

            spriteBatch.End();

            base.Draw(time, spriteBatch);
        }

        public override void Select(Game1 theGame)
        {
            base.Select(theGame);

                switch (selectedButtonText)
                {
                    case "Exit":
                        theGame.Exit();
                        break;
                    case "Save":
                        SavedGame theSave = new SavedGame();
                        theSave.CurrentLevel = 1;//theGame.currentLevel;
                        theSave.date = DateTime.Today;
                        //Open Dialogue to select which 'name' the game will be saved under
                        theSave.Name = "Save1";
                        //GameSaver needs support to handle multiple concurrent savefiles?
                        GameSaver gSaver = new GameSaver();
                        gSaver.Save(theSave);
                        break;
                    case "Load":
                        GameSaver gLoader = new GameSaver();
                        SavedGame theLoad = gLoader.Load();
                        break;
                }
        }
    }

    public class MainMenu : Menu
    {
        Texture2D backgroundOverlay;
        Texture2D foreGroundWindow;
        SpriteFont textFont;
        Rectangle screenSize;
        Rectangle menuSize;
        Color backgroundColor;
        public MainMenu() : base()
        {
        }

        public void Load(ContentManager Content, GraphicsDevice graphics)
        {
            textFont = Content.Load<SpriteFont>(@"Tahoma");
            screenSize = graphics.Viewport.Bounds;

            float scaleX = 0.35f;
            float scaleY = 0.35f;
            menuSize = screenSize;
            menuSize.X += (int)(screenSize.Width * scaleX) / 2;
            menuSize.Y += (int)(screenSize.Height * scaleY) / 2;
            menuSize.Width = (int)(menuSize.Width * (1.0 - scaleX));
            menuSize.Height = (int)(menuSize.Height * (1.0 - scaleY));

            backgroundColor = CreateTransparency(200); //0 is transparent, 255 is opaque
            backgroundOverlay = new Texture2D(graphics, 1, 1);
            backgroundOverlay.SetData<Color>(new Color[] { backgroundColor });

            foreGroundWindow = new Texture2D(graphics, 1, 1);
            foreGroundWindow.SetData<Color>(new Color[] { Color.DarkSlateGray });

            Vector2 LoadPos = new Vector2(screenSize.Width / 2.0f, menuSize.Bottom - (menuSize.Height * 0.9f));
            MenuItem newButton = new MenuItem("New", textFont, LoadPos);
            newButton.Load(Content, graphics);
            newButton.SetBubbleGreen();
            buttons.Add(newButton);

            Vector2 savePos = new Vector2(screenSize.Width / 2.0f, menuSize.Bottom - (menuSize.Height * 0.7f));
            MenuItem loadButton = new MenuItem("Load", textFont, savePos);
            loadButton.Load(Content, graphics);
            loadButton.SetBubbleBlue();
            loadButton.Selected = true;
            selectedButtonText = loadButton.Text;
            buttons.Add(loadButton);

            Vector2 exitPos = new Vector2(screenSize.Width / 2.0f, menuSize.Bottom - (menuSize.Height * 0.10f));
            MenuItem exitButton = new MenuItem("Exit", textFont, exitPos);
            exitButton.Load(Content, graphics);
            buttons.Add(exitButton);
        }

        override public void Draw(GameTime time, SpriteBatch spriteBatch)
        {

            spriteBatch.Begin();

            spriteBatch.Draw(backgroundOverlay, screenSize, backgroundColor);
            Color[] c = new Color[1];
            foreGroundWindow.GetData<Color>(c, 0, 1);
            spriteBatch.Draw(foreGroundWindow, menuSize, c[0]);


            string output = "MEGA RUNNER";
            Vector2 FontOrigin = textFont.MeasureString(output) / 2;
            spriteBatch.DrawString(textFont, output, new Vector2((screenSize.Width / 2), 50), Color.Wheat,
                0f, FontOrigin, 1.0f, SpriteEffects.None, 0.5f);

            spriteBatch.End();

            base.Draw(time, spriteBatch);
        }

        public override void Select(Game1 theGame)
        {
            base.Select(theGame);

            switch (selectedButtonText)
            {
                case "Exit":
                    theGame.Exit();
                    break;
                //case "Save":
                //    SavedGame theSave = new SavedGame();
                //    theSave.CurrentLevel = 1;//theGame.currentLevel;
                //    theSave.date = DateTime.Today;
                //    //Open Dialogue to select which 'name' the game will be saved under
                //    theSave.Name = "Save1";
                //    //GameSaver needs support to handle multiple concurrent savefiles?
                //    GameSaver gSaver = new GameSaver();
                //    gSaver.Save(theSave);
                //    break;
                case "Load":
                    GameSaver gLoader = new GameSaver();
                    SavedGame theLoad = gLoader.Load();
                    theGame.bGameStarted = true;
                    theGame.theSave = theLoad;
                    break;
                case "New":
                    theGame.bGameStarted = true;
                    break;

            }
        }
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        public SavedGame theSave;
        GameSaver gameSaver;
        bool bMenuIsOpen;
        bool bEscapeUp;
        bool bUpUp;
        bool bDownUp;
        bool bEnterUp;
        public bool bGameStarted;
        Level level1;
        PauseMenu pauseMenu;
        MainMenu mainMenu;
        SpriteBatch spriteBatch;


        public Game1()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            bMenuIsOpen = false;
            bEscapeUp = true;
            bDownUp = true;
            bUpUp = true;
            bEnterUp = true;
            bGameStarted = false;
            Content.RootDirectory = "Content";
            pauseMenu = new PauseMenu();
            mainMenu = new MainMenu();
            theSave = new SavedGame();
            gameSaver = new GameSaver();
            spriteBatch = new SpriteBatch(GraphicsDevice);

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
            level1 = new Level(100, 100, 100, 100, Content, GraphicsDevice);
            pauseMenu.Load(Content, GraphicsDevice);
            mainMenu.Load(Content, GraphicsDevice);
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
            

            KeyboardState kbState = Keyboard.GetState();
            GamePadState gpState = GamePad.GetState(PlayerIndex.One);

            if (!bGameStarted)
            {
                mainMenu.Update(gameTime);
                DoMenu(kbState, gpState, mainMenu);
                base.Update(gameTime);
                return;
            }

            if(!bEscapeUp && kbState.IsKeyUp(Keys.Escape))
                bEscapeUp = true;

            if (bMenuIsOpen)
            {
                if (kbState.IsKeyDown(Keys.Escape) && bEscapeUp)
                {
                    bMenuIsOpen = false;
                    bEscapeUp = false;
                }
                else
                {
                    pauseMenu.Update(gameTime);
                    DoMenu(kbState, gpState, pauseMenu);
                    base.Update(gameTime);
                    return;
                }
            }
            else if (kbState.IsKeyDown(Keys.Escape) && bEscapeUp)
            {
                bMenuIsOpen = true;
                bEscapeUp = false;
                pauseMenu.Update(gameTime);
                DoMenu(kbState, gpState, pauseMenu);
                base.Update(gameTime);
                return;
            }


            if (gpState.Buttons.LeftStick == ButtonState.Pressed || kbState.IsKeyDown(Keys.Left))
            {
                if(!level1.Lost)
                    level1.TheMan.Left();
            }
            else if (gpState.Buttons.RightStick == ButtonState.Pressed || kbState.IsKeyDown(Keys.Right))
            {
                if (!level1.Lost)
                    level1.TheMan.Right();
            }
            else
            {
                level1.TheMan.Still();
            }

            if (kbState.IsKeyDown(Keys.Space))
            {
                level1.TheMan.Jump();
            }
            else
            {
                level1.TheMan.StopJump();
            }

            //Process Request to Shoot
            if (kbState.IsKeyDown(Keys.LeftControl))
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

            if (!bGameStarted)
            {
                mainMenu.Draw(gameTime, spriteBatch);
                base.Draw(gameTime);
                return;
            }

            level1.Draw(gameTime, spriteBatch);

            if (bMenuIsOpen)
                pauseMenu.Draw(gameTime, spriteBatch);

            base.Draw(gameTime);
        }

        public void DoMenu(KeyboardState kb, GamePadState gp, Menu theMenu)
        {
            if (kb.IsKeyDown(Keys.Up))
            {
                if (bUpUp)
                {
                    bUpUp = false;
                    theMenu.Up();
                }
            }
            else
                bUpUp = true;

            if (kb.IsKeyDown(Keys.Down))
            {
                if (bDownUp)
                {
                    bDownUp = false;
                    theMenu.Down();
                }
            }
            else
                bDownUp = true;

            if (kb.IsKeyDown(Keys.Enter))
            {
                if (bEnterUp)
                {
                    bEnterUp = false;
                    theMenu.Select(this);
                }
            }
            else
                bEnterUp = true;
        }

    }
}
