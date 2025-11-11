using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BetterSnake
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D pixelTexture;

        int tileSize = 32;
        int screenWidth = 1920;
        int screenHeight = 1080;
        int[,] grid;

        int cols;
        int rows;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = screenWidth;
            _graphics.PreferredBackBufferHeight = screenHeight;
            _graphics.ApplyChanges();

            // 🔹 spočítej, kolik se vejde dlaždic
            cols = screenWidth / tileSize;
            rows = screenHeight / tileSize;
            int[,] groupIds = new int[cols, rows];
            int groupSize = 5; // 4x4 blok

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int groupX = x / groupSize;
                    int groupY = y / groupSize;
                    groupIds[x, y] = groupY * (cols / groupSize) + groupX;
                }
            }

            grid = new int[cols, rows];

            // 🔹 všechny dlaždice mají na začátku stejnou hodnotu (např. 0)
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    grid[x, y] = 0;
                }
            }

            int centerX = cols / 2;
            int centerY = rows / 2;

            int targetGroup = 0;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (groupIds[x, y] == targetGroup)
                    {
                        grid[x, y] = 1; // změna barvy/hodnoty
                    }
                }
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            Color borderColor = new Color(0, 0, 0, 50); // černá s průhledností

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Color color = grid[x, y] switch
                    {
                        0 => Color.Gray,
                        1 => Color.BlueViolet,
                        2 => Color.Red,
                        3 => Color.Green,
                        _ => Color.White
                    };

                    Rectangle rect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    _spriteBatch.Draw(pixelTexture, rect, color);
                }
            }
            // vertikální čáry
            for (int x = 0; x <= cols; x++)
            {
                _spriteBatch.Draw(pixelTexture,
                    new Rectangle(x * tileSize, 0, 1, screenHeight),
                    borderColor);
            }

            // horizontální čáry
            for (int y = 0; y <= rows; y++)
            {
                _spriteBatch.Draw(pixelTexture,
                    new Rectangle(0, y * tileSize, screenWidth, 1),
                    borderColor);
            }

            

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
