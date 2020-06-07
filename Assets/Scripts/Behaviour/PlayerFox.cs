using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlayerFox : Animal
{
    private ServerClient client;
    private int id = -1;

    private bool isLogin = false;
    private bool sendLogin = false;

    private Dictionary<int, List<MessageReader>> messages = new Dictionary<int, List<MessageReader>>();

    public override void Init(Coord coord)
    {
        client = new ServerClient("localhost", 13002);
        new Thread(client.Connect).Start();

        base.Init(coord);
    }

    protected override void ChooseNextAction()
    {
        lastActionChooseTime = Time.time;

        if (!client.isConnected())
        {
            return;
        }

        if (!isLogin)
        {
            Login();
            return;
        }

        while (client.isReaded())
        {
            ActionProcess();
        }

        Coord nextCoord = new Coord(coord.x, coord.y);
        Coord vectorToServer = new Coord(0, 0);
        if (Input.GetKey(KeyCode.W))
        {
            nextCoord.y += 1;
            vectorToServer.y = 1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            nextCoord.x += 1;
            vectorToServer.x = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            nextCoord.y -= 1;
            vectorToServer.y = -1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            nextCoord.x -= 1;
            vectorToServer.x = -1;
        }

        if (vectorToServer.x != 0 || vectorToServer.y != 0)
        {
            client.Send(new MessageBuilder()
                .put(id)
                .put(vectorToServer.x)
                .put(vectorToServer.y)
                .buildMessage(Codes.POSITION_ACTION)
            );
        }

        if (messages.ContainsKey(Codes.SET_STATE_ACTION))
        {
            while (messages[Codes.SET_STATE_ACTION].Count > 0)
            {
                MessageReader reader = messages[Codes.SET_STATE_ACTION][0];
                messages[Codes.SET_STATE_ACTION].RemoveAt(0);
                reader.getInt();

                Debug.Log("Set state: " + reader.getInt() + ", " + reader.getInt());
            }
        }

        StartMoveToCoord(nextCoord);
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

    private void Login()
    {
        if (!sendLogin)
        {
            Debug.Log("Login is sending");
            client.Send(new MessageBuilder().put("1").put("1").buildMessage(Codes.LOGIN_ACTION));
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