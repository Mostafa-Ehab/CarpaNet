using CarpaNet.Generation;
using CarpaNet.Models;

using Xunit;

namespace CarpaNet.UnitTests.Generation;

public class JsonContextGeneratorTests
{
    [Theory]
    [InlineData("string", "String")]
    [InlineData("long", "Int64")]
    [InlineData("bool", "Boolean")]
    [InlineData("int", "Int32")]
    [InlineData("byte[]", "ByteArray")]
    [InlineData("object", "Object")]
    [InlineData("System.DateTimeOffset", "System_DateTimeOffset")]
    [InlineData("System.Collections.Generic.List<string>", "System_Collections_Generic_List_string_")]
    public void ToBuiltInSuffix_ProducesValidIdentifier(string typeName, string expected)
    {
        var result = JsonContextGenerator.ToBuiltInSuffix(typeName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToBuiltInSuffix_NestedGenericType_SanitizesAngleBrackets()
    {
        var result = JsonContextGenerator.ToBuiltInSuffix("System.Collections.Generic.List<string>");

        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
        Assert.Equal("System_Collections_Generic_List_string_", result);
    }

    [Fact]
    public void GenerateJsonUnionTypeInfo_OpenUnion_UsesDirectInterfaceConverterInstantiation()
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

        var sb = new SourceBuilder();
        JsonContextGenerator.GenerateJsonUnionTypeInfo(
            sb,
            "ComAtproto.Temp.ICheckHandleAvailabilityOutputResult",
            "ComAtproto_Temp_ICheckHandleAvailabilityOutputResult",
            new List<string> { "#resultAvailable", "#resultUnavailable" },
            nsid,
            registry,
            new GeneratorOptions(),
            isClosed: false);

        var result = sb.ToString();

        Assert.Contains("CreateValueInfo<global::ComAtproto.Temp.ICheckHandleAvailabilityOutputResult>(options, new global::ComAtproto.Temp.ICheckHandleAvailabilityOutputResultJsonConverter())", result);
        Assert.DoesNotContain("private sealed class Converter_", result);
        Assert.DoesNotContain("options.GetConverter(typeof(global::ComAtproto.Temp.ICheckHandleAvailabilityOutputResult))", result);
    }
}
