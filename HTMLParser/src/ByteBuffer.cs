using System.Diagnostics.Contracts;
using System.Text;

namespace HTML_NET;

public class ByteBuffer
{
    public long Length { get; }

    private readonly byte[] _data;
    private long _position;

    public ByteBuffer(byte[] data)
    {
        _position = 0;
        Length = data.Length;
        _data = data;
    }
    
    [Pure]
    public bool IsEndOfBuffer()
    {
        return !CanPeekByte(1);
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
        AssertRead();
        return _data[_position + offset];
    }

    [Pure]
    public byte[] PeekBytes(int count)
    {
        AssertRead(count);
        var result = new byte[count];
        Array.Copy(_data, _position, result, 0, count);
        return result;
    }

    public void UnreadByte()
    {
        if (_position <= 0) throw new ArgumentOutOfRangeException(nameof(_position), "Cannot unread byte under position 0");
        _position--;
    }

    public void Skip(int count)
    {
        AssertRead(count);
        _position += count;
    }

    [Pure]
    public byte ReadByte()
    {
        AssertRead();
        return _data[_position++];
    }

    [Pure]
    public byte[] ReadBytes(int count)
    {
        AssertRead(count);
        var result = new byte[count];
        Array.Copy(_data, _position, result, 0, count);
        _position += count;
        return result;
    }

    [Pure]
    private bool CanPeekByte(int count)
    {
        return _position + count <= Length;
    }

    private void AssertRead(int count = 1)
    {
        if (!CanPeekByte(count)) throw new ArgumentOutOfRangeException(nameof(_position), "Cannot read past the end of the buffer");
    }
}