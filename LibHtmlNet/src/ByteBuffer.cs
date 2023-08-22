using System.Text;

namespace LibHtmlNet;

public class ByteBuffer
{
    private readonly byte[] _data;
    private long _position;

    public ByteBuffer(byte[] data)
    {
        _position = 0;
        Length = data.Length;
        _data = data;
    }

    public long Length { get; }

    public bool IsEndOfBuffer()
    {
        return !CanPeek(1);
    }

    public bool MatchCaseInsensitiveString(string word)
    {
        UnreadByte();
        var bytes = PeekBytes(word.Length);
        var str = Encoding.UTF8.GetString(bytes);
        return string.Equals(str, word, StringComparison.OrdinalIgnoreCase);
    }

    public byte PeekByte(int offset = 0)
    {
        ValidateRead();
        return _data[_position + offset];
    }

    public byte[] PeekBytes(int count)
    {
        ValidateRead(count);
        var result = new byte[count];
        Array.Copy(_data, _position, result, 0, count);
        return result;
    }

    public void UnreadByte()
    {
        if (_position <= 0) throw new Exception("Cannot unread byte");
        _position--;
    }

    public byte ReadByte()
    {
        ValidateRead();
        return _data[_position++];
    }

    public byte[] ReadBytes(int count)
    {
        ValidateRead(count);
        var result = new byte[count];
        Array.Copy(_data, _position, result, 0, count);
        _position += count;
        return result;
    }

    private bool CanPeek(int count)
    {
        return _position + count <= Length;
    }

    private void ValidateRead(int count = 1)
    {
        if (!CanPeek(count))
            // TODO: Implement EndOfBufferException or something like that
            throw new Exception("End of buffer");
    }
}