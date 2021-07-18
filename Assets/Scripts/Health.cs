using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField]
    private float maxHealth = 100f;
    public float currentHealth;
    protected bool isDead { get; set; }
    protected internal float health { get; set; }
    void Start()
    {
        health = maxHealth;
    }
    private void Update()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        currentHealth = health;
        if (health == 0)
        {
            isDead = true;
            Destroy(transform.gameObject);
            return;
        }
    }
}
