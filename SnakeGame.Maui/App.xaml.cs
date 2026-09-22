namespace WigglySnake;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            // A generous minimum so the board never gets cramped on desktop,
            // while phones/tablets simply ignore this and fill the screen.
            MinimumWidth = 380,
            MinimumHeight = 560,
            Title = "Wiggly Snake"
        };
        return window;
    }
}
