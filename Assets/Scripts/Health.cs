using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [SerializeField]
    private float maxHealth = 100f;
    public float currentHealth;
    [SerializeField]
    private Text textBox;
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
        textBox.text = currentHealth.ToString();
        if (health == 0)
        {
            isDead = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Destroy(transform.gameObject);
            return;
        }
    }
}
