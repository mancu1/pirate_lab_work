using System;
using System.Drawing;
using System.Net;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;


namespace ZakirovPirateCore
{
    internal static class DrawingHelper
    {
        private static Image _image;

        public static void DrawImageBg(PaintEventArgs arg, GameArea gameArea)
        {
            var request = WebRequest.Create("https://i.stack.imgur.com/Q0yP6.png");

            if (_image == null)
            {
                using var response = request.GetResponse();
                using var stream = response.GetResponseStream();
                _image = stream == null ? new Bitmap(1, 1) : Image.FromStream(stream);
            }

            arg.Graphics.DrawImage(_image, gameArea.Rectangle());
        }

        private static void CreateButton(Boards boardsForm)
        {
            Button spawnBtn = new Button();

            spawnBtn.Location = new Point(10, 40);
            spawnBtn.Text = "Start";
            spawnBtn.Click += (_, _) => boardsForm.Spawn();
            boardsForm.Controls.Add(spawnBtn);
        }

        public static void OnLoad(Boards boardsForm)
        {
            CreateButton(boardsForm);
            boardsForm.Location = Screen.AllScreens[1].WorkingArea.Location;
            boardsForm.StartPosition = FormStartPosition.CenterScreen;
        }
    }

    internal class GameArea
    {
        public Vector2 Size;
        public readonly Island Island;
        private readonly Color _color = Color.Aqua;

        public GameArea(Vector2 size)
        {
            Size = size;
            Island = new Island(new Vector2(size.X, size.Y / 2), size.Y / 2);
        }

        public bool IsInArea(Vector2 coordinate, float xSize = 0, float ySize = 0)
        {
            return coordinate.X + xSize / 2 >= 0 && coordinate.Y + ySize / 2 >= 0 &&
                   Size.X >= coordinate.X + xSize / 2 && Size.Y >= coordinate.Y + ySize / 2;
        }

        public Pen Pen()
        {
            return new Pen(_color, 20);
        }

        public SolidBrush Brush()
        {
            return new SolidBrush(_color);
        }

        public Rectangle Rectangle()
        {
            return new Rectangle(0, 0, (int) Size.X, (int) Size.Y);
        }

        public void OnResize(float x, float y)
        {
            Size.X = x;
            Size.Y = y;
            Island.OnResize(x, y / 2, x / 2);
        }
    }

    internal class Island
    {
        private Vector2 _coordinate;
        private float _radius;
        private readonly Color _color = Color.Yellow;

        public Island(Vector2 coordinate, float radius)
        {
            _coordinate = coordinate;
            _radius = radius;
        }

        public void OnResize(float x, float y, float radius)
        {
            _coordinate.X = x;
            _coordinate.Y = y;
            _radius = radius;
        }

        public Pen Pen()
        {
            return new Pen(_color, 20);
        }

        public SolidBrush Brush()
        {
            return new SolidBrush(_color);
        }

        public Rectangle Rectangle()
        {
            return new Rectangle((int) (_coordinate.X - _radius), (int) (_coordinate.Y - _radius), (int) _radius * 2,
                (int) _radius * 2);
        }

        public bool IsInIsland(Vector2 coordinate, float xSize = 0, float ySize = 0)
        {
            return Math.Pow(coordinate.X + xSize / 2 - _coordinate.X, 2) +
                   Math.Pow(coordinate.Y + ySize / 2 - _coordinate.Y, 2) <=
                   Math.Pow(_radius, 2);
        }
    }

    internal class Crew
    {
        public event Action OnMoveEnd;

        private readonly GameArea _gameArea;
        private Thread _thread;
        private Vector2 _coordinate;
        private readonly Vector2 _size;
        private readonly float _xSpeed;
        private readonly float _ySpeed;
        private readonly Color _color = Color.Salmon;

        public Crew(float xSpeed, float ySpeed, GameArea gameArea, Vector2 coordinate,
            Vector2 size)
        {
            _xSpeed = xSpeed;
            _ySpeed = ySpeed;
            _gameArea = gameArea;
            _coordinate = coordinate;
            _size = size;
        }

        public void Start()
        {
            StartFromBoard();
        }

        private void StartFromBoard()
        {
            _thread = new Thread(RunFromBoard);
            _thread.Start();
        }

        private void RunFromBoard()
        {
            while (_gameArea.IsInArea(_coordinate, _size.X+25, _size.Y+25))
            {
                _coordinate.X += _xSpeed;
                _coordinate.Y += _ySpeed;

                Thread.Sleep(1);
            }

            OnMoveEnd?.Invoke(); //onEnd
        }

        public void FollowBoard(Vector2 coord)
        {
            _coordinate = coord;
        }

        public Pen Pen()
        {
            return new Pen(_color, 20);
        }

        public SolidBrush Brush()
        {
            return new SolidBrush(_color);
        }

        public Rectangle Rectangle()
        {
            return new Rectangle((int) (_coordinate.X - _size.X/2), (int) (_coordinate.Y - _size.Y/2), (int) _size.X,
                (int) _size.Y);
        }
    }

    internal class Board
    {
        public event Action OnMoveTick;
        private readonly GameArea _gameArea;
        public readonly int Id;

        private Thread _thread;

        public readonly Crew[] Crews;

        private bool _isIsCrewDone;
        private Vector2 _coordinate;
        private readonly Vector2 _size;
        private readonly float _xSpeed;
        private readonly float _ySpeed;
        private readonly Color _color = Color.Brown;

        public Board(int id, float xSpeed, float ySpeed, GameArea gameArea, Vector2 coordinate,
            Vector2 size)
        {
            Id = id;
            _xSpeed = xSpeed;
            _ySpeed = ySpeed;
            _gameArea = gameArea;
            _coordinate = coordinate;
            _size = size;

            var rnd = new Random();
            var crewHeight = 40;
            var crewWeight = 40;
            var crewSize = new Vector2(crewWeight, crewHeight);
            Crews = new Crew[2];
            for (int i = 0; i < 2; i++)
            {
                var crewXSpeed = (float) (rnd.Next(10, 111) * 0.01);
                var crewCoordinate = new Vector2((_coordinate.X + _size.X /2),( _coordinate.Y + (_size.Y/2)));
                Crews[i] = new Crew(crewXSpeed, 0, _gameArea, crewCoordinate, crewSize);
                Crews[i].OnMoveEnd += StartBack;
            }
        }

        public void Start()
        {
            StartFromBigBoard();
        }

        private void StartBack()
        {
            if (_isIsCrewDone)
            {
                return;
            }

            _isIsCrewDone = true;
            StartFromIsland();
        }

        private void StartFromBigBoard()
        {
            _thread = new Thread(RunFromBigBoard);
            _thread.Start();
        }

        private void StartFromIsland()
        {
            _thread = new Thread(RunFromIsland);
            _thread.Start();
        }

        private void RunFromBigBoard()
        {
            while (!_gameArea.Island.IsInIsland(_coordinate, _size.X, _size.Y) &&
                   _gameArea.IsInArea(_coordinate, _size.X, _size.Y))
            {
                _coordinate.X += _xSpeed;
                _coordinate.Y += _ySpeed;
                foreach (var crew in Crews)
                {
                    var crewCoordinate = new Vector2((_coordinate.X + (_size.X/2 )),( _coordinate.Y + (_size.Y/2)));
                    crew.FollowBoard(crewCoordinate);
                }

                Thread.Sleep(1);
                OnMoveTick?.Invoke();
            }

            foreach (var crew in Crews)
            {
                crew.Start();
            }
        }

        private void RunFromIsland()
        {
            while (_gameArea.IsInArea(_coordinate, _size.X, _size.Y))
            {
                _coordinate.X -= _xSpeed;
                _coordinate.Y -= _ySpeed;

                Thread.Sleep(1);
                OnMoveTick?.Invoke();
            }
        }


        public Pen Pen()
        {
            return new Pen(_color, 5);
        }

        public SolidBrush Brush()
        {
            return new SolidBrush(_color);
        }

        public Rectangle Rectangle()
        {
            return new Rectangle((int) _coordinate.X, (int) _coordinate.Y, (int) _size.X, (int) _size.Y);
        }

        public Point Point()
        {
            return new Point((int) (_coordinate.X + _size.X / 2), (int) (_coordinate.Y + _size.Y / 2));
        }

        public PointF PointF()
        {
            return new PointF(_coordinate.X + _size.X / 2, _coordinate.Y + _size.Y / 2);
        }
    }

    internal class Boards : Form
    {
        private Board[] _pBoards;
        private readonly GameArea _gameArea;
        private readonly int _countOfBoards = 3;

        public void HandlerEv()
        {
            Invalidate();
        }

        public void Boards_Resize(object sender, EventArgs e)
        {
            _gameArea.OnResize(Width, Height);
        }

        public Boards()
        {
            Width = 600;
            Height = 600;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer,
                true);

            var windowSize = new Vector2(Width, Height);
            _gameArea = new GameArea(windowSize);
            // Spawn();
        }


        public void Spawn()
        {
            _pBoards = new Board [_countOfBoards];
            for (var i = 0; i < _countOfBoards; i++)
            {
                var rnd = new Random();
                var xSpeed = (float) (rnd.Next(10, 111) * 0.01);
                var boardHeight = 48;
                var boardWeight = 100;
                var boardSize = new Vector2(boardWeight, boardHeight);
                var boardCoord = new Vector2(15,
                    _gameArea.Size.Y / 2 - boardHeight * _countOfBoards + (boardHeight + 40) * (i + 1));
                _pBoards[i] = new Board(i, xSpeed, 0, _gameArea, boardCoord, boardSize);
                // _pBoards[i].Events += HandlerEv;
            }

            Start();
        }

        protected override void OnPaint(PaintEventArgs arg)
        {
            DrawingHelper.DrawImageBg(arg, _gameArea);
            // arg.Graphics.FillRectangle(_gameArea.Brush(), _gameArea.Rectangle());
            arg.Graphics.FillEllipse(_gameArea.Island.Brush(), _gameArea.Island.Rectangle());
            if (_pBoards == null || _pBoards.Length == 0)
            {
                return;
            }
            foreach (var board in _pBoards)
            {
                arg.Graphics.FillEllipse(board.Brush(), board.Rectangle());
                arg.Graphics.DrawString(board.Id.ToString(),
                    new Font(FontFamily.GenericMonospace, 15f, FontStyle.Bold), new SolidBrush(Color.Red),
                    board.PointF());

                foreach (var crew in board.Crews)
                {
                    arg.Graphics.FillEllipse(crew.Brush(), crew.Rectangle());
                }
            }
        }

        private void Start()
        {
            foreach (var board in _pBoards)
            {
                board.Start();
            }
        }
    }

    internal static class Program
    {
        private static bool _isDrawing = true;
        private static Boards _boardsForm;
        private static Thread _mainThread;

        private static void Redraw()
        {
            while (_isDrawing)
            {
                _boardsForm.HandlerEv();
                Thread.Sleep(1);
            }
        }

        private static void StartRedraw()
        {
            _isDrawing = true;
            _mainThread = new Thread(Redraw);
            _mainThread.Start();
        }

        private static void StopRedraw()
        {
            _isDrawing = false;
        }

        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _boardsForm = new Boards();
            _boardsForm.Load += (_, _) => DrawingHelper.OnLoad(_boardsForm);
            _boardsForm.Shown += (_, _) => StartRedraw();
            _boardsForm.FormClosing += (_, _) => StopRedraw();

            Application.Run(_boardsForm);
        }
    }
}