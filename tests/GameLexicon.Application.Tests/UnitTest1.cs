namespace GameLexicon.Application.Tests;

public class ProjectSmokeTests
{
    [Fact]
    public void ApplicationAssemblyCanBeLoaded()
    {
        Assert.Equal(
            "GameLexicon.Application",
            typeof(GameLexicon.Application.Class1).Assembly.GetName().Name);
    }
}
