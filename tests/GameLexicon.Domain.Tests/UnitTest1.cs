namespace GameLexicon.Domain.Tests;

public class ProjectSmokeTests
{
    [Fact]
    public void DomainAssemblyCanBeLoaded()
    {
        Assert.Equal(
            "GameLexicon.Domain",
            typeof(GameLexicon.Domain.Class1).Assembly.GetName().Name);
    }
}
