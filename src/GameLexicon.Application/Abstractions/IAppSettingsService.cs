using GameLexicon.Application.Configuration;

namespace GameLexicon.Application.Abstractions;

public interface IAppSettingsService
{
    AppSettings Current { get; }

    AppSettings Load();

    void Save(AppSettings settings);
}
