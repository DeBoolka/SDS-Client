using System;
using System.Collections.Generic;
using System.Net;

public class MessageBuilder
{
    private readonly List<byte> _data;

    public MessageBuilder()
    {
        _data = new List<byte>();
    }
    
    public MessageBuilder(int capacity)
    {
        _data = new List<byte>(capacity);
    }
    
    public MessageBuilder(byte[] data)
    {
        _data = new List<byte>(data);
    }
    
    public MessageBuilder(MessageBuilder data)
    {
        _data = new List<byte>(data._data);
    }

    public MessageBuilder put(int value)
    {
        _data.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
        return this;
    }

    public MessageBuilder put(short value)
    {
        _data.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
        return this;
    }
    
    public MessageBuilder put(long value)
    {
        _data.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
        return this;
    }
    
    public MessageBuilder put(String value)
    {
        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(value);
        put(textBytes.Length);
        _data.AddRange(textBytes);
        return this;
    }

    public MessageBuilder put(byte[] value)
    {
        _data.AddRange(value);
        return this;
    }

    public byte[] array()
    {
        return _data.ToArray();
    }

    public int size()
    {
        return _data.Count;
    }
    
    public byte[] buildMessage(int action)
    {
        return new MessageBuilder(sizeof(int) * 2 + size())
            .put(size() + sizeof(int))
            .put(action)
            .put(array())
            .array();
    }
    
    public static byte[] HostToNetworkOrder(double host)
    {
        byte[] bytes = BitConverter.GetBytes(host);
 
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
 
        return bytes;
    }

}