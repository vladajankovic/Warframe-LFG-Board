namespace ApiGateway.Middleware;

public sealed class ApostropheTransformationOptions
{
    public bool Enabled { get; set; } = true;

    public long MaxRequestBodyBytes { get; set; } = 1_048_576;

    public long MaxResponseBodyBytes { get; set; } = 4_194_304;
}
