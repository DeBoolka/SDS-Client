using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : LivingEntity {
    float amountRemaining = 1;
    const float consumeSpeed = 8;

    public float AmountRemaining {
        get {
            return amountRemaining;
        }
    }
}