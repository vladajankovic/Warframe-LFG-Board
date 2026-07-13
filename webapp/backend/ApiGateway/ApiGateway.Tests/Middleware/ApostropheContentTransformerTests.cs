using System.Text;
using System.Text.Json;
using ApiGateway.Middleware;

namespace ApiGateway.Tests.Middleware;

public class ApostropheContentTransformerTests
{
    [Fact]
    public void EncodeForBackend_TransformsJsonStringValuesWithoutChangingTheContract()
    {
        var input = Encoding.UTF8.GetBytes(
            """
            {"owner'sName":"O'Brien","count":3,"enabled":true,"nested":["don't",null]}
            """);

        var output = ApostropheContentTransformer.EncodeForBackend(input, "application/json; charset=utf-8");

        using var document = JsonDocument.Parse(output);
        Assert.Equal("O'Brien", document.RootElement.GetProperty("owner'sName").GetString()?.Replace("&#39;", "'"));
        Assert.Equal("O&#39;Brien", document.RootElement.GetProperty("owner'sName").GetString());
        Assert.Equal("don&#39;t", document.RootElement.GetProperty("nested")[0].GetString());
        Assert.Equal(3, document.RootElement.GetProperty("count").GetInt32());
        Assert.True(document.RootElement.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public void DecodeForClient_DecodesNestedJsonStringValues()
    {
        var input = Encoding.UTF8.GetBytes(
            """
            {"message":"Tenno&#39;s request","nested":{"value":"it&#39;s ready"}}
            """);

        var output = ApostropheContentTransformer.DecodeForClient(input, "application/problem+json");

        using var document = JsonDocument.Parse(output);
        Assert.Equal("Tenno's request", document.RootElement.GetProperty("message").GetString());
        Assert.Equal("it's ready", document.RootElement.GetProperty("nested").GetProperty("value").GetString());
    }

    [Fact]
    public void Transform_HandlesPlainTextAndLeavesUnsupportedContentAlone()
    {
        var text = Encoding.UTF8.GetBytes("Tenno's relic");

        var encoded = ApostropheContentTransformer.EncodeForBackend(text, "text/plain; charset=utf-8");
        var binary = ApostropheContentTransformer.EncodeForBackend(text, "application/octet-stream");

        Assert.Equal("Tenno&#39;s relic", Encoding.UTF8.GetString(encoded));
        Assert.Equal(text, binary);
        Assert.False(ApostropheContentTransformer.CanTransform("text/event-stream"));
    }

    [Fact]
    public void Transform_LeavesMalformedJsonForDownstreamValidation()
    {
        var input = Encoding.UTF8.GetBytes("{\"message\":\"isn't closed\"");

        var output = ApostropheContentTransformer.EncodeForBackend(input, "application/json");

        Assert.Equal(input, output);
    }
}
