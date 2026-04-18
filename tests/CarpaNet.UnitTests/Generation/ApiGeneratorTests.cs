using CarpaNet.Generation;
using CarpaNet.Models;
using Xunit;

namespace CarpaNet.UnitTests.Generation;

public class ApiGeneratorTests
{
    [Fact]
    public void GenerateQueryExtension_NoOutput_UsesObjectReturnType()
    {
        var sb = new SourceBuilder();
        var registry = new TypeRegistry();
        var def = new LexiconDefinition
        {
            Type = "query"
        };

        ApiGenerator.GenerateQueryExtension(
            sb,
            "DeleteRecord",
            def,
            "com.atproto.repo.deleteRecord",
            "ComAtproto.Repo",
            registry);

        var result = sb.ToString();

        Assert.Contains("Task<object> ComAtprotoRepoDeleteRecordAsync(", result);
        Assert.Contains("return await client.GetAsync<object>(", result);
    }

    [Fact]
    public void GenerateProcedureExtension_NoOutput_UsesObjectReturnType()
    {
        var sb = new SourceBuilder();
        var registry = new TypeRegistry();
        var def = new LexiconDefinition
        {
            Type = "procedure"
        };

        ApiGenerator.GenerateProcedureExtension(
            sb,
            "DeleteRecord",
            def,
            "com.atproto.repo.deleteRecord",
            "ComAtproto.Repo",
            registry);

        var result = sb.ToString();

        Assert.Contains("Task<object> ComAtprotoRepoDeleteRecordAsync(", result);
        Assert.Contains("return await client.PostAsync<object, object>(", result);
    }
}
