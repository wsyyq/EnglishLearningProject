namespace GameLexicon.Application.Abstractions;

public interface IAppLogger : IDisposable
{
    bool DevelopmentMode { get; }

    void SetDevelopmentMode(bool enabled);

    void Debug(string category, string eventName, string message);

    void Information(string category, string eventName, string message);

    void Warning(string category, string eventName, string message);

    void Error(string category, string eventName, string message, Exception? exception = null);
}
