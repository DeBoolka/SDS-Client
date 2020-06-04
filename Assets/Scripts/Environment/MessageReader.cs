using System;
using System.Net;

public class MessageReader
{
    private readonly byte[] _data;
    private int _position = 0;

    public MessageReader(byte[] data)
    {
        this._data = data;
    }

    public MessageReader(byte[] data, int position)
    {
        this._data = data;
        this._position = position;
    }
    
    public int Position
    {
        get => _position;
        set => _position = value;
    }

    public short getShort()
    {
        short val = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(_data, _position));
        _position += sizeof(short);
        return val;
    }
    
    public int getInt()
    {
        int val = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_data, _position));
        _position += sizeof(int);
        return val;
    }
    
    public long getLong()
    {
        long val = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(_data, _position));
        _position += sizeof(long);
        return val;
    }

    public string getString()
    {
        int len = getInt();
        string text = System.Text.Encoding.UTF8.GetString(_data, _position, len);
        _position += text.Length;
        return text;
    }

    public int remaing()
    {
        return size() - Position;
    }

    public int size()
    {
        return _data.Length;
    }
}