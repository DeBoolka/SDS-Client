using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class StateObject
{
    public Socket workSocket = null;
    public Selector selector = null;
    public const int BufferSize = 256;
    public byte[] buffer = new byte[BufferSize];
    public int bytesRead = -1;
}

public class ServerClient
{
    private Socket client;
    private string host;
    private int port;


    private Selector selector = new Selector(Operation.OP_EMPTY);
    private static ManualResetEvent eventSignal = new ManualResetEvent(false);

    private ConcurrentQueue<byte[]> sendingData = new ConcurrentQueue<byte[]>();
    private ConcurrentQueue<byte[]> receivedData = new ConcurrentQueue<byte[]>();

    public ServerClient(string host, int port)
    {
        this.host = host;
        this.port = port;
    }

    public void Connect()
    {
        StateObject state = Connecting();

        while (client.Connected)
        {
            waitSignal();

            if (selector.isOp(Operation.OP_RECONNECT))
            {
                Debug.Log("[Reconnect]");
                selector.set(Operation.OP_EMPTY);
                client.Shutdown(SocketShutdown.Both);
                client.BeginDisconnect(false, ReconnectCallback, state);

                waitSignal();

                Debug.Log("[Fish reconnecting]");
                state = Connecting();
            }
            else if (selector.isOp(Operation.OP_READ))
            {
                ReadFromChannel(state);
            }
            else if (selector.isOp(Operation.OP_WRITE))
            {
                WriteToChannel();
            }
        }
    }

    private StateObject Connecting()
    {
        IPHostEntry ipHostInfo = Dns.GetHostEntry(host);
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
        Debug.Log("Connecting to " + ipAddress + ":" + port);

        client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        client.BeginConnect(remoteEP, ConnectCallback, client);
        while (!client.Connected)
        {
            eventSignal.WaitOne();
        }

        Debug.Log("Connected to " + ipAddress + ":" + port);
        StateObject state = new StateObject();
        state.workSocket = client;
        state.selector = selector;
        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);

        return state;
    }

    public void Send(byte[] data)
    {
        sendingData.Enqueue(data);
        selector.addOp(Operation.OP_WRITE);
        eventSignal.Set();
    }

    public byte[] Receive()
    {
        if (receivedData.IsEmpty)
        {
            return null;
        }

        byte[] data = null;
        receivedData.TryDequeue(out data);
        return data;
    }

    public bool isConnected()
    {
        return client != null && client.Connected;
    }

    public bool isReaded()
    {
        return !receivedData.IsEmpty;
    }

    private void waitSignal()
    {
        if (selector.operation == Operation.OP_EMPTY)
        {
            eventSignal.WaitOne();
        }
    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket client = (Socket) ar.AsyncState;

            client.EndConnect(ar);

            Console.WriteLine("Client connected to {0}", client.RemoteEndPoint);
            eventSignal.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private void ReadFromChannel(StateObject state)
    {
        MessageReader reader = new MessageReader(state.buffer);

        while (reader.Position < state.bytesRead)
        {
            byte[] received = new byte[reader.getInt()];
            Array.Copy(state.buffer, reader.Position, received, 0, received.Length);
            reader.Position += received.Length;
            receivedData.Enqueue(received);
        }


        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
        selector.removeOp(Operation.OP_READ);
    }

    private static void ReadCallback(IAsyncResult ar)
    {
        try
        {
            StateObject state = (StateObject) ar.AsyncState;
            Socket client = state.workSocket;

            int bytesRead = client.EndReceive(ar);
            state.bytesRead = bytesRead;
            state.selector.addOp(Operation.OP_READ);
            eventSignal.Set();
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    private void WriteToChannel()
    {
        while (!sendingData.IsEmpty)
        {
            byte[] data;
            if (!sendingData.TryDequeue(out data))
            {
                return;
            }

            client.BeginSend(data, 0, data.Length, 0, WriteCallback, client);
        }

        selector.removeOp(Operation.OP_WRITE);
    }

    private static void WriteCallback(IAsyncResult ar)
    {
        try
        {
            Socket client = (Socket) ar.AsyncState;

            int bytesSent = client.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    public void Reconnect(string host, int port)
    {
        this.host = host;
        this.port = port;

        selector.addOp(Operation.OP_RECONNECT);
        eventSignal.Set();
    }

    private static void ReconnectCallback(IAsyncResult ar)
    {
        StateObject state = (StateObject) ar.AsyncState;
        Socket client = state.workSocket;
        client.EndDisconnect(ar);
        while (client.Connected)
        {
            Thread.Sleep(100);
        }

        client.Close();
        eventSignal.Set();
    }
}

[Flags]
public enum Operation
{
    OP_EMPTY = 0b_0000,
    OP_WRITE = 0b_0001,
    OP_READ = 0b_0010,
    OP_RECONNECT = 0b_0100,
    OP_READ_WRITE = OP_READ | OP_WRITE
}

public class Selector
{
    public Operation operation = Operation.OP_EMPTY;

    public Selector()
    {
    }

    public Selector(Operation op)
    {
        operation = op;
    }

    public void addOp(Operation op)
    {
        operation |= op;
    }

    public void removeOp(Operation op)
    {
        operation &= ~op;
    }

    public void set(Operation op)
    {
        operation = op;
    }

    public bool isOp(Operation op)
    {
        return (operation & op) != 0;
    }
}