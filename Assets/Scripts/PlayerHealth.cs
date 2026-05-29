using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    private int currentHealth;

    public bool IsDead { get; private set; }

    [Header("UI")]
    public Image healthFill;

    void Start()
    {
        currentHealth = maxHealth;

        UpdateHealthBar();
    }

    void Update()
    {
        // TEST DAMAGE
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        currentHealth -= damage;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        Debug.Log("Player HP : " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthBar()
    {
        healthFill.fillAmount =
            (float)currentHealth / maxHealth;
    }

    void Die()
    {
        IsDead = true;

        Debug.Log("PLAYER DEAD");
    }
}