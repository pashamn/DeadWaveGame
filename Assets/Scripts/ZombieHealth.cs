using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    private int currentHealth;
    private Animator animator;
    private ZombieAI zombieAI;

    public bool IsDead { get; private set; }

    void Start()
    {
        currentHealth = maxHealth;

        zombieAI = GetComponent<ZombieAI>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // TEST DAMAGE
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(25);
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        currentHealth -= damage;

        // Jika masih hidup → mainkan hit
        if (currentHealth > 0)
        {
            animator.SetTrigger("hit");
        }

        // Jika HP habis → mati
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        IsDead = true;

        zombieAI.Die();
    }
}