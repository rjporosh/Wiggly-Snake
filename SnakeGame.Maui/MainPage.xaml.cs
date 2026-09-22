using WigglySnake.Services;

namespace WigglySnake;

public partial class MainPage : ContentPage
{
    private readonly GameEngine _engine;
    private readonly IAudioService _audio;
    private readonly IHighScoreService _highScores;

    private IDispatcherTimer? _loopTimer;
    private CancellationTokenSource? _wiggleCts;

    public MainPage(GameEngine engine, IAudioService audio, IHighScoreService highScores)
    {
        InitializeComponent();

        _engine = engine;
        _audio = audio;
        _highScores = highScores;

        Board.Engine = _engine;

        _engine.Changed += OnEngineChanged;
        _engine.GameOver += OnEngineGameOver;

        RefreshChrome();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _wiggleCts = new CancellationTokenSource();
        _ = WiggleTitleAsync(_wiggleCts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _wiggleCts?.Cancel();
        StopLoop();
    }

#if WINDOWS
    // Windows/desktop is the one platform where a physical keyboard is the
    // norm rather than the exception, so it gets a native key hook in
    // addition to the on-screen D-pad, touch and mouse that already work
    // everywhere via the XAML gesture recognizers and button clicks above.
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement platformView)
        {
            platformView.KeyDown -= OnWindowsKeyDown;
            platformView.KeyDown += OnWindowsKeyDown;
        }
    }

    private void OnWindowsKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Up:
            case Windows.System.VirtualKey.W:
                Move(Direction.Up);
                break;
            case Windows.System.VirtualKey.Down:
            case Windows.System.VirtualKey.S:
                Move(Direction.Down);
                break;
            case Windows.System.VirtualKey.Left:
            case Windows.System.VirtualKey.A:
                Move(Direction.Left);
                break;
            case Windows.System.VirtualKey.Right:
            case Windows.System.VirtualKey.D:
                Move(Direction.Right);
                break;
            case Windows.System.VirtualKey.Space:
            case Windows.System.VirtualKey.P:
                OnPauseClicked(this, EventArgs.Empty);
                break;
            case Windows.System.VirtualKey.Enter:
                if (_engine.State != GameState.Playing) OnPrimaryActionClicked(this, EventArgs.Empty);
                break;
        }
    }
#endif

    // ---------- Engine event handlers ----------

    private void OnEngineChanged() => Dispatcher.Dispatch(RefreshChrome);

    private void OnEngineGameOver()
    {
        _highScores.Save(_engine.HighScore);
        Dispatcher.Dispatch(() =>
        {
            _audio.PlayGameOver();
            StopLoop();
        });
    }

    // ---------- Game loop ----------

    private void StartLoop()
    {
        StopLoop();
        _loopTimer = Dispatcher.CreateTimer();
        _loopTimer.Interval = TimeSpan.FromMilliseconds(_engine.CurrentSpeedMs());
        _loopTimer.Tick += OnLoopTick;
        _loopTimer.Start();
    }

    private void StopLoop()
    {
        if (_loopTimer is null) return;
        _loopTimer.Stop();
        _loopTimer.Tick -= OnLoopTick;
        _loopTimer = null;
    }

    private void OnLoopTick(object? sender, EventArgs e)
    {
        if (_engine.State != GameState.Playing)
        {
            StopLoop();
            return;
        }

        int scoreBefore = _engine.Score;
        _engine.Tick();

        if (_engine.State == GameState.Playing)
        {
            // The speed ramps up with score, so re-arm the timer with the
            // current interval rather than assuming it hasn't changed.
            StartLoop();
            if (_engine.Score != scoreBefore) _audio.PlayEat();
        }
        else
        {
            StopLoop();
        }
    }

    // ---------- UI sync ----------

    private void RefreshChrome()
    {
        ScoreLabel.Text = _engine.Score.ToString();
        HighScoreLabel.Text = _engine.HighScore.ToString();
        PauseButton.Text = _engine.State == GameState.Playing ? "❚❚" : "▶";
        MuteButton.Text = _audio.Muted ? "🔇" : "🔊";

        switch (_engine.State)
        {
            case GameState.Ready:
                ShowOverlay("🐍🍎", "Wiggly Snake",
                    "Guide the snake to munch yummy fruit and grow super long!",
                    "Swipe on the board, use the arrows below, your keyboard, or a controller.",
                    "▶ Play");
                break;

            case GameState.Paused:
                ShowOverlay("⏸️", "Paused", string.Empty, string.Empty, "▶ Resume");
                break;

            case GameState.GameOver:
                string noun = _engine.Score == 1 ? "fruit" : "fruits";
                ShowOverlay(
                    _engine.NewBest ? "🏆" : "💥",
                    _engine.NewBest ? "New Best!" : "Game Over",
                    $"You scored {_engine.Score} {noun}!",
                    string.Empty,
                    "↻ Play Again");
                break;

            case GameState.Playing:
                OverlayPanel.IsVisible = false;
                break;
        }

        Board.Invalidate();
    }

    private void ShowOverlay(string emoji, string title, string message, string hint, string actionText)
    {
        OverlayPanel.IsVisible = true;
        OverlayEmoji.Text = emoji;
        OverlayTitle.Text = title;
        OverlayMessage.Text = message;
        OverlayMessage.IsVisible = !string.IsNullOrEmpty(message);
        OverlayHint.Text = hint;
        OverlayHint.IsVisible = !string.IsNullOrEmpty(hint);
        PrimaryActionButton.Text = actionText;
    }

    // ---------- Input ----------

    private void OnPrimaryActionClicked(object? sender, EventArgs e)
    {
        if (_engine.State == GameState.Paused)
        {
            _engine.Resume();
            StartLoop();
        }
        else
        {
            _engine.StartGame();
            StartLoop();
        }
    }

    private void OnPauseClicked(object? sender, EventArgs e)
    {
        _engine.TogglePause();
        if (_engine.State == GameState.Playing) StartLoop();
        else StopLoop();
    }

    private void OnMuteClicked(object? sender, EventArgs e)
    {
        _audio.Muted = !_audio.Muted;
        MuteButton.Text = _audio.Muted ? "🔇" : "🔊";
    }

    private void OnUpClicked(object? sender, EventArgs e) => Move(Direction.Up);
    private void OnDownClicked(object? sender, EventArgs e) => Move(Direction.Down);
    private void OnLeftClicked(object? sender, EventArgs e) => Move(Direction.Left);
    private void OnRightClicked(object? sender, EventArgs e) => Move(Direction.Right);

    private void OnSwiped(object? sender, SwipedEventArgs e)
    {
        var direction = e.Direction switch
        {
            SwipeDirection.Up => Direction.Up,
            SwipeDirection.Down => Direction.Down,
            SwipeDirection.Left => Direction.Left,
            _ => Direction.Right
        };
        Move(direction);
    }

    private void OnBoardTapped(object? sender, TappedEventArgs e)
    {
        // Matches the web version: tapping the board while not actively
        // playing (co-located with the Ready/Paused/Game Over overlay)
        // starts a fresh run — the dedicated pause button is the way to
        // resume without restarting.
        if (_engine.State != GameState.Playing)
        {
            _engine.StartGame();
            StartLoop();
        }
    }

    private void Move(Direction direction)
    {
        bool wasReady = _engine.State == GameState.Ready;
        _engine.SetDirection(direction);
        if (wasReady) StartLoop();
    }

    // ---------- Decorative title wiggle ----------

    private async Task WiggleTitleAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await TitleEmoji.RotateTo(-8, 800, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await TitleEmoji.RotateTo(8, 800, Easing.SinInOut);
            }
        }
        catch (TaskCanceledException) { /* page navigated away */ }
    }
}
