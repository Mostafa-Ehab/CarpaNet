using CarpaNet.Generation;
using CarpaNet.Models;
using Xunit;

namespace CarpaNet.UnitTests.Generation;

public class UnionGeneratorTests
{
    [Fact]
    public void GenerateUnionInterface_OpenUnion_EmitsInterfaceConverter()
    {
        var registry = new TypeRegistry();
        var nsid = "com.atproto.temp.checkHandleAvailability";

        var doc = new LexiconDocument
        {
            Id = nsid,
            Defs = new Dictionary<string, LexiconDefinition>
            {
                ["resultAvailable"] = new LexiconDefinition { Type = "object", Properties = new() },
                ["resultUnavailable"] = new LexiconDefinition { Type = "object", Properties = new() },
            }
        };
        registry.RegisterDocument(doc);

        var unionDef = new LexiconDefinition
        {
            Type = "union",
            Refs = new List<string> { "#resultAvailable", "#resultUnavailable" }
        };

        var sb = new SourceBuilder();
        UnionGenerator.GenerateUnionInterface(
            sb,
            "ICheckHandleAvailabilityOutputResult",
            unionDef,
            nsid,
            registry);

        var result = sb.ToString();

        Assert.Contains("[System.Text.Json.Serialization.JsonConverter(typeof(ICheckHandleAvailabilityOutputResultJsonConverter))]", result);
        Assert.Contains("public interface ICheckHandleAvailabilityOutputResult", result);
        Assert.Contains("public sealed class ICheckHandleAvailabilityOutputResultJsonConverter", result);
        Assert.Contains("if (!element.TryGetProperty(\"$type\", out var typeProp))", result);
        Assert.Contains("_ => null,", result);
        Assert.Contains("writer.WriteString(\"$type\", discriminator);", result);
        Assert.Contains("if (prop.NameEquals(\"$type\"))", result);
        Assert.Contains("global::ComAtproto.Temp.CheckHandleAvailabilityResultAvailable => \"com.atproto.temp.checkHandleAvailability#resultAvailable\",", result);
        Assert.Contains("global::ComAtproto.Temp.CheckHandleAvailabilityResultUnavailable => \"com.atproto.temp.checkHandleAvailability#resultUnavailable\",", result);
    }

    [Fact]
    public void GenerateUnionInterface_ClosedUnion_EmitsPolymorphicAttributes()
    {
        var registry = new TypeRegistry();
        var nsid = "com.atproto.temp.checkHandleAvailability";

        var doc = new LexiconDocument
        {
            Id = nsid,
            Defs = new Dictionary<string, LexiconDefinition>
            {
                ["resultAvailable"] = new LexiconDefinition { Type = "object", Properties = new() },
                ["resultUnavailable"] = new LexiconDefinition { Type = "object", Properties = new() },
            }
        };
        registry.RegisterDocument(doc);

        var unionDef = new LexiconDefinition
        {
            Type = "union",
            Closed = true,
            Refs = new List<string> { "#resultAvailable", "#resultUnavailable" }
        };

        var sb = new SourceBuilder();
        UnionGenerator.GenerateUnionInterface(
            sb,
            "ICheckHandleAvailabilityOutputResult",
            unionDef,
            nsid,
            registry);

        var result = sb.ToString();

        Assert.Contains("[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"$type\")]", result);
        Assert.Contains("JsonDerivedType(typeof(ComAtproto.Temp.CheckHandleAvailabilityResultAvailable), \"com.atproto.temp.checkHandleAvailability#resultAvailable\")", result);
        Assert.Contains("JsonDerivedType(typeof(ComAtproto.Temp.CheckHandleAvailabilityResultUnavailable), \"com.atproto.temp.checkHandleAvailability#resultUnavailable\")", result);
        Assert.DoesNotContain("JsonConverter(typeof(ICheckHandleAvailabilityOutputResultJsonConverter))", result);
    }
}
