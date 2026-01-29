using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;

namespace AdvancedSnake
{
    class Program
    {
        static void Main() => new GameEngine().Run();
    }

    class GameEngine
    {
        private int _width = 50, _height = 22;
        private int _score;
        private int _highScore;
        private string _scoreFile = "highscore.txt";
        private bool _isPaused = false;

        public void Run()
        {
            Console.CursorVisible = false; // This turns off the flickering cursor
            LoadHighScore();
            LoadHighScore();
            while (true)
            {
                ShowMenu();
                StartGame();
            }
            
        }

        private void ShowMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
             █▀▀ █▄█ █▄▄ █▀▀ █▀█ █▀ █▄░█ ▄▀█ █▄▀ █▀▀
             █▄▄ ░█░ █▄█ ██▄ █▀▄ ▄█ █░▀█ █▀█ █░█ ██▄");
            Console.WriteLine("\n\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\t   [ HIGH SCORE: {_highScore} ]");
            Console.WriteLine("\n\t   1. START GAME");
            Console.WriteLine("\t   2. EXIT (or press ESC)");

            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.D2 || key == ConsoleKey.Escape) ExitGracefully();
        }

        private void StartGame()
        {
            _score = 0;
            int delay = 140;
            Console.Clear();
            DrawFrame();

            var snake = new Snake(25, 10);
            var food = new Item(ConsoleColor.Green, "●");
            var bonus = new Item(ConsoleColor.Magenta, "★") { IsActive = false };
            food.Spawn(_width, _height, snake.Body);

            while (true)
            {
                // 1. Input & Pause Handling
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape) ExitGracefully();
                    if (key == ConsoleKey.Spacebar) _isPaused = !_isPaused;

                    snake.ChangeDirection(key);
                }

                if (_isPaused)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Print(_width / 2 - 4, _height / 2, " PAUSED ");
                    continue; // Skip movement logic while paused
                }

                // 2. Movement Logic
                if (!snake.Move(_width, _height)) break;

                // 3. Collision & Food Logic
                if (snake.Head == food.Pos)
                {
                    _score += 10;
                    snake.Grow();
                    food.Spawn(_width, _height, snake.Body);
                    Beep(800, 50);
                    if (delay > 40) delay -= 5;
                }

                if (bonus.IsActive && snake.Head == bonus.Pos)
                {
                    _score += 50;
                    bonus.IsActive = false;
                    Beep(1200, 100);
                }

                if (!bonus.IsActive && new Random().Next(1, 100) == 1)
                    bonus.Spawn(_width, _height, snake.Body);

                DrawUI();
                Thread.Sleep(delay);
            }
            GameOver();
        }

        private void DrawFrame()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            for (int i = 0; i < _width; i++) { Print(i, 0, "█"); Print(i, _height - 1, "█"); }
            for (int i = 0; i < _height; i++) { Print(0, i, "█"); Print(_width - 1, i, "█"); }
        }

        private void DrawUI()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Print(2, _height, $" SCORE: {_score}  |  HIGH: {_highScore}  | [SPACE] PAUSE  | [ESC] QUIT ");
        }

        private void GameOver()
        {
            if (_score > _highScore) { _highScore = _score; SaveHighScore(); }
            Beep(200, 400);
            Console.ForegroundColor = ConsoleColor.Red;
            Print(_width / 3, _height / 2, " G A M E   O V E R ");
            Print(_width / 3, (_height / 2) + 1, " ENTER: Menu | ESC: Exit ");

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape) ExitGracefully();
                if (key == ConsoleKey.Enter) break;
            }
        }

        private void Print(int x, int y, string s) { Console.SetCursorPosition(x, y); Console.Write(s); }
        private void Beep(int f, int d) { try { Console.Beep(f, d); } catch { } }
        private void LoadHighScore() { if (File.Exists(_scoreFile)) _highScore = int.Parse(File.ReadAllText(_scoreFile)); }
        private void SaveHighScore() { File.WriteAllText(_scoreFile, _highScore.ToString()); }

        private void ExitGracefully()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n\n\t   Thank you for playing CyberSnake Pro!");
            Thread.Sleep(1000);
            Environment.Exit(0);
        }
    }

    class Snake
    {
        public List<(int x, int y)> Body { get; } = new();
        public (int x, int y) Head => Body[0];
        private (int x, int y) _dir = (1, 0);
        private (int x, int y) _lastTail;

        public Snake(int x, int y)
        {
            Body.Add((x, y));
            Body.Add((x - 1, y));
            Body.Add((x - 2, y));
        }

        public void ChangeDirection(ConsoleKey key)
        {
            _dir = key switch
            {
                ConsoleKey.UpArrow when _dir.y != 1 => (0, -1),
                ConsoleKey.DownArrow when _dir.y != -1 => (0, 1),
                ConsoleKey.LeftArrow when _dir.x != 1 => (-1, 0),
                ConsoleKey.RightArrow when _dir.x != -1 => (1, 0),
                _ => _dir
            };
        }

        public bool Move(int w, int h)
        {
            var newHead = (Head.x + _dir.x, Head.y + _dir.y);
            if (newHead.Item1 <= 0 || newHead.Item1 >= w - 1 || newHead.Item2 <= 0 || newHead.Item2 >= h - 1 || Body.Contains(newHead))
                return false;

            _lastTail = Body.Last();
            Body.Insert(0, newHead);

            Console.SetCursorPosition(newHead.Item1, newHead.Item2);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(_dir switch { (0, -1) => '^', (0, 1) => 'v', (-1, 0) => '<', (1, 0) => '>', _ => 'O' });

            if (Body.Count > 1) { Console.SetCursorPosition(Body[1].x, Body[1].y); Console.ForegroundColor = ConsoleColor.White; Console.Write("■"); }

            Console.SetCursorPosition(_lastTail.x, _lastTail.y);
            Console.Write(" ");
            Body.RemoveAt(Body.Count - 1);
            return true;
        }
        public void Grow() => Body.Add(_lastTail);
    }

    class Item
    {
        public (int x, int y) Pos;
        public bool IsActive = true;
        private ConsoleColor _color;
        private string _symbol;

        public Item(ConsoleColor color, string symbol) { _color = color; _symbol = symbol; }

        public void Spawn(int w, int h, List<(int x, int y)> snakeBody)
        {
            var rand = new Random();
            do { Pos = (rand.Next(1, w - 1), rand.Next(1, h - 1)); }
            while (snakeBody.Contains(Pos));

            IsActive = true;
            Console.ForegroundColor = _color;
            Console.SetCursorPosition(Pos.x, Pos.y);
            Console.Write(_symbol);
        }
    }
}