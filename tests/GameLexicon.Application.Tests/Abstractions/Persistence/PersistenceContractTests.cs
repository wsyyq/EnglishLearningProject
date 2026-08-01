using System.Reflection;
using GameLexicon.Application.Abstractions.Persistence;

namespace GameLexicon.Application.Tests.Abstractions.Persistence;

public sealed class PersistenceContractTests
{
    private static readonly Type[] RepositoryTypes =
    [
        typeof(IVocabularyRepository),
        typeof(ISentenceExampleRepository),
        typeof(ITagRepository)
    ];

    [Fact]
    public void VocabularyRepository_ContainsExactlyFourCoreMethods()
    {
        var methods = typeof(IVocabularyRepository).GetMethods();

        Assert.Equal(
            [
                "FindByNormalizedHeadwordAsync",
                "GetDetailsAsync",
                "SaveAsync",
                "SearchAsync"
            ],
            methods.Select(static method => method.Name).Order().ToArray());
    }

    [Fact]
    public void RepositoryInterfaces_AreDefinedInApplication()
    {
        Assert.All(RepositoryTypes, type =>
        {
            Assert.True(type.IsInterface);
            Assert.Equal("GameLexicon.Application", type.Assembly.GetName().Name);
            Assert.StartsWith("GameLexicon.Application.", type.Namespace);
        });
    }

    [Fact]
    public void EveryRepositoryMethod_IsTaskBasedAndEndsWithCancellationToken()
    {
        var methods = RepositoryTypes.SelectMany(static type => type.GetMethods()).ToArray();

        Assert.NotEmpty(methods);
        Assert.All(methods, method =>
        {
            Assert.True(IsTaskType(method.ReturnType), method.Name);
            Assert.Equal(
                typeof(CancellationToken),
                method.GetParameters().Last().ParameterType);
        });
    }

    [Fact]
    public void RepositoryPublicApi_DoesNotExposeForbiddenTypes()
    {
        var exposedTypes = RepositoryTypes
            .SelectMany(static type => type.GetMethods())
            .SelectMany(static method =>
                method.GetParameters().Select(static parameter => parameter.ParameterType)
                    .Append(method.ReturnType))
            .SelectMany(FlattenType)
            .ToArray();

        Assert.DoesNotContain(exposedTypes, static type =>
            type == typeof(IQueryable) ||
            type.Namespace?.StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal) == true ||
            type.Namespace?.StartsWith("Godot", StringComparison.Ordinal) == true ||
            type.Namespace?.StartsWith("GameLexicon.Infrastructure", StringComparison.Ordinal) == true ||
            type.Namespace?.StartsWith("System.Data", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ProjectAssemblyDependencies_FollowLayerDirection()
    {
        var applicationReferences = typeof(IVocabularyRepository).Assembly
            .GetReferencedAssemblies()
            .Select(static name => name.Name)
            .ToArray();
        var domainReferences = typeof(GameLexicon.Domain.Entries.VocabularyEntry).Assembly
            .GetReferencedAssemblies()
            .Select(static name => name.Name)
            .ToArray();

        Assert.Contains("GameLexicon.Domain", applicationReferences);
        Assert.DoesNotContain("GameLexicon.Infrastructure", applicationReferences);
        Assert.DoesNotContain("GameLexicon.Application", domainReferences);
    }

    private static bool IsTaskType(Type type) =>
        type == typeof(Task) ||
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);

    private static IEnumerable<Type> FlattenType(Type type)
    {
        yield return type;

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in FlattenType(argument))
            {
                yield return nested;
            }
        }
    }
}
