using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BetterSnake
{
    // ✅ Rozhraní pro herní entity
    interface IEntity
    {
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, int tileSize);
    }

    // ✅ Třída pro jablko
    class Apple : IEntity
    {
        public Point Position { get; private set; }
        public bool IsPlaced { get; set; } = false;
        private Random rnd = new Random();

        public void Spawn(List<Point> allowedPositions)
        {
            if (allowedPositions.Count == 0)
            {
                IsPlaced = false;
                return;
            }

            Position = allowedPositions[rnd.Next(allowedPositions.Count)];
            IsPlaced = true;
        }

        public void Update(GameTime gameTime)
        {
            // apple logika se spouští z Game1, nic se zde nedeje
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, int tileSize)
        {
            if (!IsPlaced) return;

            Rectangle rect = new Rectangle(Position.X * tileSize, Position.Y * tileSize, tileSize, tileSize);
            spriteBatch.Draw(pixelTexture, rect, Color.Red);
        }
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D pixelTexture;

        int tileSize = 32;
        int screenWidth = 1920;
        int screenHeight = 1080;

        int cols, rows;
        int[,] grid;
        int[,] groupIds;
        int groupSize = 5;
        int nextGroupToEnable = 0;
        int totalGroups;

        List<Point> snake;
        Point direction = new Point(1, 0);

        Apple apple;
        bool gameStarted = false;

        double moveTimer = 0;
        double moveInterval = 180;
        readonly double minMoveInterval = 50;
        readonly double maxMoveInterval = 1000;
        readonly double moveStep = 20;

        KeyboardState prevKeyboardState;

        bool isGameOver = false;
        int expandThreshold = 5; // počet dlaždic do rozšíření

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

            cols = screenWidth / tileSize;
            rows = screenHeight / tileSize;

            grid = new int[cols, rows];
            groupIds = new int[cols, rows];

            int gxCount = Math.Max(1, cols / groupSize);
            int gyCount = Math.Max(1, rows / groupSize);
            totalGroups = gxCount * gyCount;

            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    groupIds[x, y] = (y / groupSize) * gxCount + (x / groupSize);

            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    grid[x, y] = 0;

            nextGroupToEnable = 0;
            EnableNextGroup();

            snake = new List<Point>();
            Point start = FindAllowedTileNear(new Point(cols / 2, rows / 2));
            if (start == Point.Zero)
                start = new Point(cols / 2, rows / 2);

            snake.Add(start);
            snake.Add(new Point(start.X - 1, start.Y));
            snake.Add(new Point(start.X - 2, start.Y));
            direction = new Point(1, 0);

            // vytvoření apple
            apple = new Apple();

            gameStarted = false;
            prevKeyboardState = Keyboard.GetState();

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

            HandleInput();

            moveTimer += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (!isGameOver && moveTimer >= moveInterval)
            {
                moveTimer -= moveInterval;
                MoveSnake();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();

            // vykreslení gridu
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                {
                    Color color = (grid[x, y] == 1) ? Color.BlueViolet : Color.Gray;
                    Rectangle r = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    _spriteBatch.Draw(pixelTexture, r, color);
                }

            // čáry
            Color gridLineColor = new Color(0, 0, 0, 50);
            for (int x = 0; x <= cols; x++)
                _spriteBatch.Draw(pixelTexture, new Rectangle(x * tileSize, 0, 1, screenHeight), gridLineColor);
            for (int y = 0; y <= rows; y++)
                _spriteBatch.Draw(pixelTexture, new Rectangle(0, y * tileSize, screenWidth, 1), gridLineColor);

            // vykreslení apple přes IEntity
            apple.Draw(_spriteBatch, pixelTexture, tileSize);

            // vykreslení hada
            for (int i = 0; i < snake.Count; i++)
            {
                Color c = (i == 0) ? Color.Yellow : Color.Green;
                Rectangle r = new Rectangle(snake[i].X * tileSize, snake[i].Y * tileSize, tileSize, tileSize);
                _spriteBatch.Draw(pixelTexture, r, c);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        // ---------- logika ----------
        private void HandleInput()
        {
            KeyboardState ks = Keyboard.GetState();

            if ((ks.IsKeyDown(Keys.OemPlus) && !prevKeyboardState.IsKeyDown(Keys.OemPlus))
                || (ks.IsKeyDown(Keys.Add) && !prevKeyboardState.IsKeyDown(Keys.Add)))
                moveInterval = Math.Min(maxMoveInterval, moveInterval + moveStep);

            if ((ks.IsKeyDown(Keys.OemMinus) && !prevKeyboardState.IsKeyDown(Keys.OemMinus))
                || (ks.IsKeyDown(Keys.Subtract) && !prevKeyboardState.IsKeyDown(Keys.Subtract)))
                moveInterval = Math.Max(minMoveInterval, moveInterval - moveStep);

            Point nd = direction;

            if (ks.IsKeyDown(Keys.Up) || ks.IsKeyDown(Keys.W)) nd = new Point(0, -1);
            else if (ks.IsKeyDown(Keys.Down) || ks.IsKeyDown(Keys.S)) nd = new Point(0, 1);
            else if (ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.A)) nd = new Point(-1, 0);
            else if (ks.IsKeyDown(Keys.Right) || ks.IsKeyDown(Keys.D)) nd = new Point(1, 0);

            if (!(nd.X == -direction.X && nd.Y == -direction.Y))
                direction = nd;

            prevKeyboardState = ks;
        }

        private void MoveSnake()
        {
            // první pohyb → spawn apple
            if (!gameStarted)
            {
                gameStarted = true;
                if (!apple.IsPlaced)
                    SpawnApple();
            }

            Point head = snake[0];
            Point newHead = new Point(head.X + direction.X, head.Y + direction.Y);

            if (!IsInside(newHead) || !IsWalkable(newHead) || IsSnakeBody(newHead))
            {
                ResetGame();
                return;
            }

            bool ate = apple.IsPlaced && newHead == apple.Position;
            snake.Insert(0, newHead);

            if (!ate) snake.RemoveAt(snake.Count - 1);
            else
            {
                apple.IsPlaced = false;
                SpawnApple();
            }

            if (snake.Count >= CountAllowedTiles() - expandThreshold && nextGroupToEnable < totalGroups)
                EnableNextGroup();
        }

        private void SpawnApple()
        {
            List<Point> spots = new List<Point>();
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    if (grid[x, y] == 1 && !IsSnakeBody(new Point(x, y)))
                        spots.Add(new Point(x, y));

            apple.Spawn(spots);
        }

        private bool IsInside(Point p) => p.X >= 0 && p.X < cols && p.Y >= 0 && p.Y < rows;
        private bool IsWalkable(Point p) => grid[p.X, p.Y] == 1;
        private bool IsSnakeBody(Point p)
        {
            foreach (var s in snake) if (s == p) return true;
            return false;
        }

        private Point FindAllowedTileNear(Point center)
        {
            int rad = Math.Max(cols, rows);
            for (int r = 0; r < rad; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int x = center.X + dx;
                        int y = center.Y + dy;
                        if (IsInside(new Point(x, y)) && grid[x, y] == 1)
                            return new Point(x, y);
                    }
            return Point.Zero;
        }

        private int CountAllowedTiles()
        {
            int c = 0;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    if (grid[x, y] == 1) c++;
            return c;
        }

        private void EnableNextGroup()
        {
            int g = nextGroupToEnable;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    if (groupIds[x, y] == g) grid[x, y] = 1;

            nextGroupToEnable++;
        }

        private void ResetGame()
        {
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    grid[x, y] = 0;

            nextGroupToEnable = 0;
            EnableNextGroup();

            snake.Clear();
            Point start = FindAllowedTileNear(new Point(cols / 2, rows / 2));
            if (start == Point.Zero) start = new Point(cols / 2, rows / 2);

            snake.Add(start);
            snake.Add(new Point(start.X - 1, start.Y));
            snake.Add(new Point(start.X - 2, start.Y));
            direction = new Point(1, 0);

            apple.IsPlaced = false;
            gameStarted = false;
            moveTimer = 0;
        }
    }
}
