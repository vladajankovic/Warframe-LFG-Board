using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace ApiGateway.Middleware;

public sealed class ApostropheTransformationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApostropheTransformationOptions _options;

    public ApostropheTransformationMiddleware(
        RequestDelegate next,
        IOptions<ApostropheTransformationOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled
            || !context.Request.Path.StartsWithSegments("/v1")
            || context.WebSockets.IsWebSocketRequest
            || HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        await TransformRequestAsync(context);

        var originalResponseBody = context.Response.Body;
        await using var bufferingBody = new BufferingResponseStream(
            originalResponseBody,
            _options.MaxResponseBodyBytes);
        context.Response.Body = bufferingBody;

        context.Response.OnStarting(static state =>
        {
            var httpContext = (HttpContext)state;
            if (CanTransformResponse(httpContext))
            {
                // A transformed response can have a different byte length.
                httpContext.Response.ContentLength = null;
            }

            return Task.CompletedTask;
        }, context);

        try
        {
            await _next(context);

            if (!bufferingBody.IsPassThrough)
            {
                var original = bufferingBody.GetBufferedBody();
                var output = CanTransformResponse(context)
                    ? ApostropheContentTransformer.DecodeForClient(original, context.Response.ContentType)
                    : original;

                if (!context.Response.HasStarted && CanTransformResponse(context))
                {
                    context.Response.ContentLength = output.Length;
                }

                await originalResponseBody.WriteAsync(output, context.RequestAborted);
            }
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    private async Task TransformRequestAsync(HttpContext context)
    {
        if (!ApostropheContentTransformer.CanTransform(context.Request.ContentType))
        {
            return;
        }

        if (context.Request.ContentLength > _options.MaxRequestBodyBytes)
        {
            return;
        }

        context.Request.EnableBuffering();
        var originalBody = context.Request.Body;

        using var copy = new MemoryStream();
        await originalBody.CopyToAsync(copy, context.RequestAborted);
        originalBody.Position = 0;

        if (copy.Length > _options.MaxRequestBodyBytes)
        {
            return;
        }

        var transformed = ApostropheContentTransformer.EncodeForBackend(
            copy.ToArray(),
            context.Request.ContentType);
        var transformedBody = new MemoryStream(transformed, writable: false);
        context.Request.Body = transformedBody;
        context.Request.ContentLength = transformed.Length;
        context.Response.RegisterForDispose(transformedBody);
    }

    private static bool CanTransformResponse(HttpContext context)
    {
        var contentEncoding = context.Response.Headers[HeaderNames.ContentEncoding];
        var isUnencoded = contentEncoding.Count == 0
            || contentEncoding.All(value => string.Equals(value, "identity", StringComparison.OrdinalIgnoreCase));

        return isUnencoded
            && ApostropheContentTransformer.CanTransform(context.Response.ContentType);
    }
}
