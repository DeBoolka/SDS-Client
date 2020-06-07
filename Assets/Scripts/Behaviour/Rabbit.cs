using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Rabbit : Animal {
    public static readonly string[] GeneNames = { "A", "B" };
    
    float lastSendedPositionTime = 0;

    private AnimalClient _client;

    public override void Init(Coord coord)
    {
        _client = new AnimalClient("localhost", 13002, coord);
        base.Init(coord);
    }

    protected override void ChooseNextAction()
    {
        base.ChooseNextAction();
        
        float deltaTime= Time.time - lastSendedPositionTime;
        if (deltaTime > 0)
        {
            lastSendedPositionTime = Time.time;
            _client.move(coord);
        }
    }
}