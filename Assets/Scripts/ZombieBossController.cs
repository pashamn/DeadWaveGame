using UnityEngine;
using UnityEngine.AI;

public class ZombieBossController : MonoBehaviour
{
    public float maxHp = 300f;
    public float baseSpeed = 3f;
    public float attackRange = 1.8f;
    public float leapMinRange = 4f;
    public float leapMaxRange = 7f;
    public float leapCooldown = 4f;

    private Animator animator;
    private NavMeshAgent agent;
    private Transform player;

    private float currentHp;
    private bool isRageMode = false;
    private bool hasRoared = false;
    private bool isDead = false;
    private float leapTimer = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        currentHp = maxHp;
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        // TEST DAMAGE DENGAN TOMBOL H
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(50f);
        }

        float dist = Vector3.Distance(transform.position, player.position);
        bool moving = agent.velocity.magnitude > 0.1f;
        bool inRange = dist < attackRange;

        animator.SetFloat("hp", currentHp);
        animator.SetBool("isMoving", moving);
        animator.SetBool("inRange", inRange);

        // Mengejar player
        if (!inRange)
        {
            agent.SetDestination(player.position);
        }

        // Aktifkan Rage Mode saat HP <= 150
        if (!isRageMode && currentHp <= 150f)
        {
            isRageMode = true;
            agent.speed = baseSpeed * 2f;

            Debug.Log("RAGE MODE AKTIF!");
        }

        // Leap Attack otomatis saat Rage Mode
        leapTimer -= Time.deltaTime;

        if (isRageMode &&
            dist >= leapMinRange &&
            dist <= leapMaxRange &&
            leapTimer <= 0f)
        {
            animator.SetTrigger("leapTrigger");
            leapTimer = leapCooldown;
        }
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHp -= dmg;

        if (currentHp < 0f)
        {
            currentHp = 0f;
        }

        Debug.Log("Boss HP: " + currentHp + " / " + maxHp);

        if (currentHp <= 0f)
        {
            StartCoroutine(OnDeath());
        }
    }

    // Dipanggil dari Animation Event pada animasi Roar
    public void OnRoarComplete()
    {
        hasRoared = true;
        animator.SetBool("hasRoared", true);
    }

    System.Collections.IEnumerator OnDeath()
    {
        isDead = true;

        if (agent != null)
        {
            agent.isStopped = true;
        }

        Debug.Log("BOSS MATI");

        yield return new WaitForSeconds(2.5f);

        gameObject.SetActive(false);
    }
}