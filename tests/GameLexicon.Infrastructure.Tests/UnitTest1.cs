namespace GameLexicon.Infrastructure.Tests;

public class ProjectSmokeTests
{
    [Fact]
    public void InfrastructureAssemblyCanBeLoaded()
    {
        Assert.Equal(
            "GameLexicon.Infrastructure",
            typeof(GameLexicon.Infrastructure.Class1).Assembly.GetName().Name);
    }
}
