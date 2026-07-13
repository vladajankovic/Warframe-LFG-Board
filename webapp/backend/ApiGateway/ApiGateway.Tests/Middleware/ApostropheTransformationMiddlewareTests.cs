using System.Text;
using System.Text.Json;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiGateway.Tests.Middleware;

public class ApostropheTransformationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_TransformsRequestAndResponseAtTheGatewayBoundary()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/v1/test";
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        var request = Encoding.UTF8.GetBytes("{\"value\":\"Tenno's relic\"}");
        context.Request.ContentLength = request.Length;
        context.Request.Body = new MemoryStream(request);
        context.Response.Body = new MemoryStream();

        var middleware = new ApostropheTransformationMiddleware(
            async httpContext =>
            {
                using var requestReader = new StreamReader(
                    httpContext.Request.Body,
                    Encoding.UTF8,
                    leaveOpen: true);
                var downstreamRequest = await requestReader.ReadToEndAsync();
                using var requestJson = JsonDocument.Parse(downstreamRequest);
                Assert.Equal("Tenno&#39;s relic", requestJson.RootElement.GetProperty("value").GetString());

                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync("{\"value\":\"Tenno&#39;s relic\"}");
            },
            Options.Create(new ApostropheTransformationOptions()));

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var responseJson = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("Tenno's relic", responseJson.RootElement.GetProperty("value").GetString());
    }
}
