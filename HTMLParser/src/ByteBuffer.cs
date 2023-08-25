using System.Diagnostics.Contracts;
using System.Text;

namespace HTML_NET;

public class ByteBuffer
{
    private readonly byte[] _data;

    public ByteBuffer(byte[] data)
    {
        Position = 0;
        Length = data.Length;
        _data = data;
    }

    public long Length { get; }
    public long Position { get; private set; }

    [Pure]
    public bool IsEndOfBuffer()
    {
        return Position >= Length;
    }

    [Pure]
    public Span<byte> PeekRemainingBytes()
    {
        UnreadByte();
        var count = Math.Max(Length - Position, 25);
        return PeekBytes((int)count);
    }

    [Pure]
    public bool MatchCaseInsensitiveString(string word)
    {
        UnreadByte();
        var bytes = PeekBytes(word.Length);
        var str = Encoding.UTF8.GetString(bytes);
        return string.Equals(str, word, StringComparison.OrdinalIgnoreCase);
    }

    [Pure]
    public byte PeekByte(int offset = 0)
    {
        return _data[(int)Position + offset];
    }

    [Pure]
    private Span<byte> PeekBytes(int count)
    {
        return new Span<byte>(_data, (int)Position, count);
    }
    
    public void UnreadByte()
    {
        Position--;
    }

    public void Skip(int count)
    {
        Position += count;
    }

    [Pure]
    public byte ReadByte()
    {
        return _data[Position++];
    }
}