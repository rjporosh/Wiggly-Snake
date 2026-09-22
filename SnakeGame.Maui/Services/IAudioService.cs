namespace WigglySnake.Services;

/// <summary>Short, generated-feel sound effects for eating fruit and game over.</summary>
public interface IAudioService
{
    bool Muted { get; set; }
    void PlayEat();
    void PlayGameOver();
}
