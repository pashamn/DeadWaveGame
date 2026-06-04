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
    [Tooltip("Jarak maksimal player kabur sebelum zombie menyerah dan kembali patroli")]
    public float loseChaseDistance = 22f; // Jarak menyerah (Harus lebih besar dari chaseDistance)
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

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip idleSound;
    public AudioClip chaseSound;
    public AudioClip attackSound;

    private AudioClip currentClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float idleVolume = 0.2f;
    [Range(0f, 1f)] public float chaseVolume = 0.7f;
    [Range(0f, 1f)] public float attackVolume = 1f;

    [Header("3D Audio")]
    public float minAudioDistance = 3f;
    public float maxAudioDistance = 30f;

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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;

            // Audio 3D
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = minAudioDistance;
            audioSource.maxDistance = maxAudioDistance;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        currentState = ZombieState.Idle;
    }

    void Update()
    {
        if (health.IsDead || player == null)
            return;

        // Kalkulasi jarak kuadrat (lebih ringan performanya dibanding Vector3.Distance biasa)
        float distanceSqr = (player.position - transform.position).sqrMagnitude;

        float chaseDistanceSqr = chaseDistance * chaseDistance;
        float loseChaseDistanceSqr = loseChaseDistance * loseChaseDistance; // Batas menyerah kuadrat
        float attackDistanceSqr = attackDistance * attackDistance;

        switch (currentState)
        {
            case ZombieState.Idle:

                PlayStateSound(idleSound, idleVolume);

                animator.SetBool("isRunning", true);
                animator.SetBool("isAttacking", false);

                if (Time.time >= nextRoamTime)
                {
                    nextRoamTime = Time.time + roamTimer;

                    Vector3 randomPosition = transform.position + new Vector3(
                        Random.Range(-roamRadius, roamRadius),
                        0,
                        Random.Range(-roamRadius, roamRadius)
                    );

                    NavMeshHit hit;

                    if (NavMesh.SamplePosition(randomPosition, out hit, roamRadius, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }

                if (distanceSqr <= chaseDistanceSqr)
                {
                    currentState = ZombieState.Chase;
                }

                break;

            case ZombieState.Chase:

                PlayStateSound(chaseSound, chaseVolume);

                animator.SetBool("isRunning", true);
                animator.SetBool("isAttacking", false);

                // PERBAIKAN UTAMA: Jika player lari menjauh melewati batas loseChaseDistance, zombie menyerah!
                if (distanceSqr > loseChaseDistanceSqr)
                {
                    agent.ResetPath(); // Stop mengejar posisi player
                    nextRoamTime = Time.time + 1f; // Beri jeda 1 detik sebelum mulai patroli biasa
                    currentState = ZombieState.Idle; // Kembalikan status zombie ke Idle/Patroli biasa
                    Debug.Log("DeadWave Log: Zombie kehilangan jejak player dan berhenti mengejar.");
                    return;
                }

                if (Time.time >= nextUpdateTime)
                {
                    nextUpdateTime = Time.time + updateRate;
                    agent.SetDestination(player.position);
                }

                if (distanceSqr <= attackDistanceSqr)
                {
                    currentState = ZombieState.Attack;
                }

                break;

            case ZombieState.Attack:

                agent.ResetPath();

                animator.SetBool("isRunning", false);
                animator.SetBool("isAttacking", true);

                Vector3 lookPos = new Vector3(
                    player.position.x,
                    transform.position.y,
                    player.position.z
                );

                transform.LookAt(lookPos);

                if (distanceSqr > attackDistanceSqr)
                {
                    animator.SetBool("isAttacking", false);
                    currentState = ZombieState.Chase;
                    return;
                }

                if (Time.time >= nextAttackTime)
                {
                    Attack();
                }

                break;

            case ZombieState.Dead:
                break;
        }
    }

    void PlayStateSound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null)
            return;

        if (currentClip == clip)
            return;

        currentClip = clip;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.volume = volume;

        audioSource.pitch = Random.Range(0.9f, 1.1f);

        audioSource.Play();
    }

    void Attack()
    {
        nextAttackTime = Time.time + attackCooldown;

        Debug.Log("Zombie Attack");

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound, attackVolume);
        }

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    public void Die()
    {
        currentState = ZombieState.Dead;

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        animator.SetBool("isRunning", false);
        animator.SetBool("isAttacking", false);

        if (agent != null)
        {
            agent.enabled = false;
        }

        animator.SetTrigger("die");

        Debug.Log("Zombie Dead");

        Destroy(gameObject, 5f);
    }
}