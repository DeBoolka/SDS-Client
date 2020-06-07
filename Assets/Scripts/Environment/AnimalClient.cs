using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = System.Random;

public class AnimalClient
{
    private ServerClient client;
    
    private int id = -1;

    private bool isLogin = false;
    private bool sendLogin = false;

    private Coord oldCoord;
    private Coord sumCoord;

    private Dictionary<int, List<MessageReader>> messages = new Dictionary<int, List<MessageReader>>();

    public AnimalClient(string host, int port, Coord coord)
    {
        client = new ServerClient(host, port);
        oldCoord = coord;
        new Thread(client.Connect).Start();
    }

    public void move(Coord coord)
    {
        if (coord.Equals(oldCoord))
        {
            return;
        }
        
        if (!client.isConnected())
        {
            return;
        }

        if (!isLogin)
        {
            Login(coord);
            oldCoord = coord;
            sumCoord = coord;
            return;
        }

        while (client.isReaded())
        {
            ActionProcess();
        }

        Coord nextCoord = new Coord(coord.x - oldCoord.x, coord.y - oldCoord.y);
        if (nextCoord.x != 0 || nextCoord.y != 0)
        {
            client.Send(new MessageBuilder()
                .put(id)
                .put(nextCoord.x)
                .put(nextCoord.y)
                .buildMessage(Codes.POSITION_ACTION)
            );
            Debug.Log(id + " to [" + coord.x + ", " + coord.y + "] from [" + oldCoord.x + ", " + oldCoord.y + "]");
            sumCoord = new Coord(sumCoord.x + nextCoord.x, sumCoord.y + nextCoord.y);
            oldCoord = coord;
        }

        if (messages.ContainsKey(Codes.SET_STATE_ACTION))
        {
            while (messages[Codes.SET_STATE_ACTION].Count > 0)
            {
                MessageReader reader = messages[Codes.SET_STATE_ACTION][0];
                messages[Codes.SET_STATE_ACTION].RemoveAt(0);
                reader.getInt();

                // oldCoord = new Coord(reader.getInt(), reader.getInt());
                // Debug.Log("Set state: " + oldCoord.x + ", " + oldCoord.y);
            }
        }
    }

    private void ActionProcess()
    {
        MessageReader reader = new MessageReader(client.Receive());
        int action = reader.getInt();

        if (action.Equals(Codes.RECONNECT_ACTION))
        {
            reader.getInt();
            reader.getInt();
            reader.getInt();

            string host = reader.getString();
            int port = reader.getInt();

            client.Reconnect(host, port);
        }

        if (!messages.ContainsKey(action))
        {
            messages[action] = new List<MessageReader>();
        }

        messages[action].Add(reader);
        Debug.Log("Read a message from the server. Len: " + reader.size());
    }

    private void Login(Coord coord)
    {
        if (!sendLogin)
        {
            Debug.Log("Login is sending");
            int logPass = new Random().Next();
            client.Send(new MessageBuilder().put(this.GetHashCode().ToString()).put(this.GetHashCode().ToString()).buildMessage(Codes.LOGIN_ACTION));
            sendLogin = true;
        }

        if (client.isReaded())
        {
            Debug.Log("Login is receiving");
            var reader = new MessageReader(client.Receive());
            if (reader.getInt() != Codes.LOGIN_ACTION)
            {
                Debug.Log("Code is invalid");
                return;
            }

            reader.getShort();
            reader.getInt();
            id = reader.getInt();
            if (!reader.getString().Equals("Login successful"))
            {
                Debug.Log("Login is invalid");
                sendLogin = false;
            }

            Debug.Log("State is sending");
            client.Send(new MessageBuilder().put(id).put(coord.x).put(coord.y).buildMessage(Codes.SET_STATE_ACTION));
            isLogin = true;
        }
    }
}