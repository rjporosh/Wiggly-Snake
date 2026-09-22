namespace WigglySnake.Services;

/// <summary>Persists the best score between launches.</summary>
public interface IHighScoreService
{
    int Load();
    void Save(int score);
}
