using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    private NavMeshAgent agent;
    private Animator animator;
    private ZombieHealth health;

    [Header("Zombie Settings")]
    public float chaseDistance = 15f;
    public float attackDistance = 2f;
    public float attackCooldown = 1.5f;
    public int damage = 10;
    
    [Header("Roaming")]
    public float roamRadius = 5f;
    public float roamTimer = 4f;

    private float nextRoamTime;

    [Header("Optimization")]
    public float updateRate = 0.25f;

    private float nextUpdateTime;
    private float nextAttackTime;

    public enum ZombieState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    public ZombieState currentState;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<ZombieHealth>();

        // Cari player otomatis
        player = GameObject.FindGameObjectWithTag("Player").transform;

        currentState = ZombieState.Idle;
    }

    void Update()
    {
        // Stop semua AI jika zombie mati
        if (health.IsDead)
            return;

        float distanceSqr =
            (player.position - transform.position).sqrMagnitude;

        float chaseDistanceSqr =
            chaseDistance * chaseDistance;

        float attackDistanceSqr =
            attackDistance * attackDistance;

        switch (currentState)
        {
           case ZombieState.Idle:
                animator.SetBool("isRunning", true);
                animator.SetBool("isAttacking", false);

                // Roaming random
                if (Time.time >= nextRoamTime)
                {
                    nextRoamTime =
                        Time.time + roamTimer;

                    Vector3 randomPosition =
                        transform.position +
                        new Vector3(
                            Random.Range(-roamRadius, roamRadius),
                            0,
                            Random.Range(-roamRadius, roamRadius)
                        );

                    NavMeshHit hit;

                    // Cari posisi valid di NavMesh
                    if (NavMesh.SamplePosition(
                        randomPosition,
                        out hit,
                        roamRadius,
                        NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }

                // Jika player dekat → chase
                if (distanceSqr <= chaseDistanceSqr)
                {
                    currentState = ZombieState.Chase;
                }

                break;

            case ZombieState.Chase:

                animator.SetBool("isRunning", true);
                animator.SetBool("isAttacking", false);

                // Update path tidak setiap frame
                if (Time.time >= nextUpdateTime)
                {
                    nextUpdateTime =
                        Time.time + updateRate;

                    agent.SetDestination(player.position);
                }

                // Jika dekat player → attack
                if (distanceSqr <= attackDistanceSqr)
                {
                    currentState = ZombieState.Attack;
                }

                break;

            case ZombieState.Attack:

                agent.ResetPath();

                animator.SetBool("isRunning", false);
                animator.SetBool("isAttacking", true);

                // Menghadap player
                Vector3 lookPos = new Vector3(
                    player.position.x,
                    transform.position.y,
                    player.position.z
                );

                transform.LookAt(lookPos);

                // Jika player menjauh
                if (distanceSqr > attackDistanceSqr)
                {
                    animator.SetBool("isAttacking", false);

                    currentState = ZombieState.Chase;

                    return;
                }

                // Cooldown attack
                if (Time.time >= nextAttackTime)
                {
                    Attack();
                }

                break;

            case ZombieState.Dead:
                break;
        }
    }

    void Attack()
    {
        nextAttackTime =
            Time.time + attackCooldown;

        Debug.Log("Zombie Attack");

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    public void Die()
    {
        currentState = ZombieState.Dead;

        animator.SetBool("isRunning", false);
        animator.SetBool("isAttacking", false);

        agent.enabled = false;

        animator.SetTrigger("die");

        Debug.Log("Zombie Dead");

        Destroy(gameObject, 5f);
    }
}