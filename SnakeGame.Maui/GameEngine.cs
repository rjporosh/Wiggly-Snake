namespace WigglySnake;

/// <summary>
/// Direction the snake is currently moving, or is queued to move, in.
/// </summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// High-level state machine for a single play session.
/// </summary>
public enum GameState
{
    Ready,
    Playing,
    Paused,
    GameOver
}

/// <summary>
/// A single board cell coordinate.
/// </summary>
public readonly record struct Cell(int X, int Y);

/// <summary>
/// Raised whenever the engine wants the UI to redraw (score changed, snake moved,
/// food eaten, game over, etc). The UI owns the render loop / timer, the engine
/// only owns rules and state.
/// </summary>
public sealed class GameEngine
{
    public const int Cols = 17;
    public const int Rows = 17;

    private const int BaseSpeedMs = 170;
    private const int MinSpeedMs = 80;

    private static readonly string[] Fruits = { "🍎", "🍓", "🍇", "🍊", "🍒", "🍑", "🍉", "🥝", "🍍" };

    private readonly List<Cell> _snake = new();
    private readonly Random _rng = new();

    private Direction _dir = Direction.Right;
    private Direction _pendingDir = Direction.Right;
    private bool _dirLocked;

    public GameState State { get; private set; } = GameState.Ready;
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public bool NewBest { get; private set; }
    public string FoodEmoji { get; private set; } = Fruits[0];
    public Cell Food { get; private set; }

    /// <summary>Read-only snapshot of the snake, head first.</summary>
    public IReadOnlyList<Cell> Snake => _snake;

    public Direction FacingDirection => _dir;

    /// <summary>Fired after any state change that should trigger a redraw.</summary>
    public event Action? Changed;

    /// <summary>Fired once, the instant a run ends (for sound effects, haptics, etc).</summary>
    public event Action? FoodEaten;

    /// <summary>Fired once, the instant a run ends (for sound effects, haptics, etc).</summary>
    public event Action? GameOver;

    public GameEngine(int savedHighScore = 0)
    {
        HighScore = savedHighScore;
        ResetBoard();
    }

    /// <summary>Current tick interval — the game speeds up gradually as the score rises.</summary>
    public int CurrentSpeedMs()
    {
        int speed = BaseSpeedMs - Score * 5;
        return Math.Max(MinSpeedMs, speed);
    }

    public void StartGame()
    {
        ResetBoard();
        State = GameState.Playing;
        Changed?.Invoke();
    }

    public void TogglePause()
    {
        if (State == GameState.Playing) State = GameState.Paused;
        else if (State == GameState.Paused) State = GameState.Playing;
        Changed?.Invoke();
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Changed?.Invoke();
    }

    /// <summary>
    /// Queue a new heading. From the Ready screen this also starts the run,
    /// matching the original Blazor behaviour (tap/press to begin).
    /// </summary>
    public void SetDirection(Direction d)
    {
        if (State == GameState.Ready)
        {
            StartGame();
            return;
        }

        if (State != GameState.Playing || _dirLocked) return;

        bool opposite =
            (d == Direction.Up && _dir == Direction.Down) ||
            (d == Direction.Down && _dir == Direction.Up) ||
            (d == Direction.Left && _dir == Direction.Right) ||
            (d == Direction.Right && _dir == Direction.Left);

        if (opposite) return;

        _pendingDir = d;
        _dirLocked = true;
    }

    /// <summary>Advance the simulation by one step. Call this from an external timer.</summary>
    public void Tick()
    {
        if (State != GameState.Playing) return;

        _dir = _pendingDir;
        _dirLocked = false;

        var head = _snake[0];
        var next = _dir switch
        {
            Direction.Up => new Cell(head.X, head.Y - 1),
            Direction.Down => new Cell(head.X, head.Y + 1),
            Direction.Left => new Cell(head.X - 1, head.Y),
            _ => new Cell(head.X + 1, head.Y)
        };

        if (next.X < 0 || next.X >= Cols || next.Y < 0 || next.Y >= Rows)
        {
            EndGame();
            return;
        }

        bool eating = next.Equals(Food);
        int checkCount = eating ? _snake.Count : _snake.Count - 1;
        for (int i = 0; i < checkCount; i++)
        {
            if (_snake[i].Equals(next))
            {
                EndGame();
                return;
            }
        }

        _snake.Insert(0, next);

        if (eating)
        {
            Score++;
            FoodEaten?.Invoke();
            PlaceFood();
        }
        else
        {
            _snake.RemoveAt(_snake.Count - 1);
        }

        Changed?.Invoke();
    }

    private void ResetBoard()
    {
        _snake.Clear();
        int cx = Cols / 2, cy = Rows / 2;
        _snake.Add(new Cell(cx, cy));
        _snake.Add(new Cell(cx - 1, cy));
        _snake.Add(new Cell(cx - 2, cy));
        _dir = _pendingDir = Direction.Right;
        _dirLocked = false;
        Score = 0;
        NewBest = false;
        PlaceFood();
    }

    private void EndGame()
    {
        State = GameState.GameOver;
        if (Score > HighScore)
        {
            HighScore = Score;
            NewBest = true;
        }
        GameOver?.Invoke();
        Changed?.Invoke();
    }

    private void PlaceFood()
    {
        var occupied = new HashSet<Cell>(_snake);
        var free = new List<Cell>();
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
            {
                var p = new Cell(x, y);
                if (!occupied.Contains(p)) free.Add(p);
            }

        if (free.Count == 0)
        {
            // Board is full — a win, celebrated the same way as game over.
            EndGame();
            return;
        }

        Food = free[_rng.Next(free.Count)];
        FoodEmoji = Fruits[_rng.Next(Fruits.Length)];
    }

    /// <summary>
    /// Classifies a cell for rendering: what's in it, and (for body segments)
    /// how far along the tail it is, 0 = head .. 1 = tail tip, used to tint the gradient.
    /// </summary>
    public CellKind Classify(int x, int y)
    {
        var p = new Cell(x, y);

        if (Food.Equals(p) && State != GameState.Ready)
            return CellKind.Food;

        int idx = _snake.FindIndex(s => s.Equals(p));
        if (idx == 0) return CellKind.Head;
        if (idx > 0) return CellKind.Body;

        return (x + y) % 2 == 0 ? CellKind.EmptyLight : CellKind.EmptyDark;
    }

    /// <summary>Normalized position (0 = head, 1 = tail tip) used for the body colour gradient.</summary>
    public double BodyGradientPosition(int x, int y)
    {
        int idx = _snake.FindIndex(s => s.X == x && s.Y == y);
        if (idx <= 0 || _snake.Count <= 1) return 0;
        return (double)idx / _snake.Count;
    }
}

public enum CellKind
{
    EmptyLight,
    EmptyDark,
    Body,
    Head,
    Food
}
