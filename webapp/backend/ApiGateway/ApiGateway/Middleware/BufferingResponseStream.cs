namespace ApiGateway.Middleware;

internal sealed class BufferingResponseStream : Stream
{
    private readonly Stream _inner;
    private readonly MemoryStream _buffer = new();
    private readonly long _maximumBufferSize;
    private bool _passThrough;

    public BufferingResponseStream(Stream inner, long maximumBufferSize)
    {
        _inner = inner;
        _maximumBufferSize = Math.Max(1, maximumBufferSize);
    }

    public bool IsPassThrough => _passThrough;

    public byte[] GetBufferedBody() => _buffer.ToArray();

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        if (_passThrough)
        {
            _inner.Flush();
        }
    }

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _passThrough ? _inner.FlushAsync(cancellationToken) : Task.CompletedTask;

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (ShouldSwitchToPassThrough(count))
        {
            SwitchToPassThrough();
        }

        if (_passThrough)
        {
            _inner.Write(buffer, offset, count);
        }
        else
        {
            _buffer.Write(buffer, offset, count);
        }
    }

    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (ShouldSwitchToPassThrough(buffer.Length))
        {
            await SwitchToPassThroughAsync(cancellationToken);
        }

        if (_passThrough)
        {
            await _inner.WriteAsync(buffer, cancellationToken);
        }
        else
        {
            await _buffer.WriteAsync(buffer, cancellationToken);
        }
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    private bool ShouldSwitchToPassThrough(int incomingBytes) =>
        !_passThrough && _buffer.Length + incomingBytes > _maximumBufferSize;

    private void SwitchToPassThrough()
    {
        if (_passThrough)
        {
            return;
        }

        _buffer.Position = 0;
        _buffer.CopyTo(_inner);
        _buffer.SetLength(0);
        _passThrough = true;
    }

    private async Task SwitchToPassThroughAsync(CancellationToken cancellationToken)
    {
        if (_passThrough)
        {
            return;
        }

        _buffer.Position = 0;
        await _buffer.CopyToAsync(_inner, cancellationToken);
        _buffer.SetLength(0);
        _passThrough = true;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _buffer.Dispose();
        }

        base.Dispose(disposing);
    }
}
