using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField]
    private PartsOfBody _part;
    public Health _health;
    internal void HealthDamage(float damege)
    {
        switch (_part)
        {
            case PartsOfBody.Head:
                _health.health -= damege;
                return;
            case PartsOfBody.Leg:
                _health.health -= damege * 0.2f;
                return;
            case PartsOfBody.Hand:
                _health.health -= damege * 0.2f;
                return;
            case PartsOfBody.Neak:
                _health.health -= damege * 0.7f;
                return;
            case PartsOfBody.Chest:
                _health.health -= damege * 0.3f;
                return;
        }
    }
    enum PartsOfBody
    {
        Head,
        Leg,
        Hand,
        Neak,
        Chest
    }
}
