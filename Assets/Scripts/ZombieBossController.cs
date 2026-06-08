using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieBossController : MonoBehaviour
{
    [Header("Target Konfigurasi")]
    public Transform playerTransform; 

    [Header("Aturan Jarak AI")]
    public float attackDistance = 3.0f; 
    
    [Header("Aturan Serangan")]
    public float attackCooldown = 1.5f; 
    private float nextAttackTime = 0f;  

    [Header("Mekanik Lompat (Leap Parabola)")]
    public float leapSpeed = 7.0f;      
    public float leapHeight = 5.0f;     
    private bool isLeaping = false;     
    private Vector3 leapStartPosition;  
    private Vector3 leapTargetPosition; 
    private float leapProgress = 0f;    

    [Header("Kustomisasi Kecepatan Animasi")]
    [Range(0.1f, 2.0f)] 
    public float runAnimationSpeed = 0.5f; 

    [Header("Audio (Sama Seperti ZombieAI)")]
    public AudioSource audioSource;
    public AudioClip idleSound;
    public AudioClip chaseSound;
    public AudioClip attackSound;
    private AudioClip currentClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float idleVolume = 0.3f;  
    [Range(0f, 1f)] public float chaseVolume = 0.8f;
    [Range(0f, 1f)] public float attackVolume = 1f;

    [Header("3D Audio Distance")]
    public float minAudioDistance = 5f;
    public float maxAudioDistance = 40f;

    public enum BossState { Idle, Chase, Attack, Dead }
    private BossState currentBossState;

    private NavMeshAgent agent;
    private Animator animator;
    private ZombieBossHealth bossHealth; 
    private float originalAcceleration; 
    private bool isRaging = false;
    private bool isScreaming = false;
    private bool isDead = false;
    
    // Variabel kunci pengaman interupsi animasi hit tanpa merubah animator
    private bool isPlayingHitAnim = false; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        bossHealth = GetComponent<ZombieBossHealth>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f; 
            audioSource.minDistance = minAudioDistance;
            audioSource.maxDistance = maxAudioDistance;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        currentBossState = BossState.Idle;

        if (agent != null)
        {
            originalAcceleration = agent.acceleration;
        }
    }

    void Update()
    {
        if (isDead || (bossHealth != null && bossHealth.IsDead) || animator == null || agent == null || playerTransform == null) return;

        // --- 1. LOGIKA FISIK PARABOLA SAAT LOMPAT ---
        if (isLeaping)
        {
            agent.enabled = false; 

            float totalDistance = Vector3.Distance(leapStartPosition, leapTargetPosition);
            if (totalDistance > 0)
            {
                leapProgress += (leapSpeed * Time.deltaTime) / totalDistance;
            }
            else
            {
                leapProgress = 1f;
            }

            leapProgress = Mathf.Clamp01(leapProgress);

            Vector3 currentPosition = Vector3.Lerp(leapStartPosition, leapTargetPosition, leapProgress);
            float heightOffset = Mathf.Sin(leapProgress * Mathf.PI) * leapHeight;
            currentPosition.y += heightOffset;

            transform.position = currentPosition;

            if (leapProgress >= 1.0f)
            {
                isLeaping = false;
                agent.enabled = true; 
                currentBossState = BossState.Chase; 
                Debug.Log("Tester Log: Mutant mendarat di tanah!");
            }
            return; 
        }

        // --- 2. JEDA PENGAMAN SAAT BERTERIAK ---
        if (isScreaming)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            return;
        }

        // --- 3. MEMBACA INPUT KEYBOARD UNTUK DEMO ---
        if (Input.GetKeyDown(KeyCode.Alpha3) && !isRaging && !isScreaming)
        {
            StartCoroutine(TriggerRageModeRoutine());
        }

        if (Input.GetKeyDown(KeyCode.Alpha5) && !isScreaming)
        {
            StartCoroutine(TriggerLeapAttackRoutine());
        }

        // --- 4. ENGINE STATE UNTUK AUDIO & NAVMESH ---
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackDistance)
        {
            currentBossState = BossState.Attack;
        }
        else
        {
            currentBossState = BossState.Chase;
        }

        // Eksekusi aksi berdasarkan state saat ini
        switch (currentBossState)
        {
            case BossState.Idle:
                PlayStateSound(idleSound, idleVolume);
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                break;

            case BossState.Chase:
                PlayStateSound(chaseSound, chaseVolume);
                
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
                
                // KUNCI PENGAMAN: Jika sedang memutar animasi kaget (hit), dilarang menimpa dengan animasi lari!
                if (!isPlayingHitAnim)
                {
                    animator.speed = runAnimationSpeed;
                    if (isRaging) animator.Play("run1");
                    else animator.Play("run2");
                }
                break;

            case BossState.Attack:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;

                Vector3 targetPosition = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
                transform.LookAt(targetPosition);

                if (Time.time >= nextAttackTime && !isPlayingHitAnim)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    
                    if (audioSource != null && attackSound != null) 
                        audioSource.PlayOneShot(attackSound, attackVolume);

                    animator.speed = 1.0f; 
                    animator.Play("attack1");
                    
                    PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
                    if (playerHealth != null) playerHealth.TakeDamage(15); 
                }
                break;
        }
    }

    // FUNGSI INTERUPSI BARU: Dipanggil oleh ZombieBossHealth saat peluru masuk
    public void PlayHitAnimationDirectly()
    {
        if (isDead || isPlayingHitAnim || isLeaping || isScreaming) return;
        StartCoroutine(HitAnimationRoutine());
    }

    private IEnumerator HitAnimationRoutine()
    {
        isPlayingHitAnim = true;
        
        // Kembalikan speed animator ke 1.0f agar gerakan kagetnya tidak melambat kaku
        animator.speed = 1.0f; 
        animator.Play("gethit1");

        // Beri jeda waktu agar animasi gethit1 selesai berputar (0.4 detik sangat ideal)
        yield return new WaitForSeconds(0.4f);

        isPlayingHitAnim = false;
    }

    void PlayStateSound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null || currentClip == clip) return;
        currentClip = clip;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(0.85f, 1.05f); 
        audioSource.Play();
    }

    IEnumerator TriggerRageModeRoutine()
    {
        isScreaming = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.acceleration = 0; 
        animator.speed = 1.0f;

        if (audioSource != null && attackSound != null) audioSource.PlayOneShot(attackSound, attackVolume);

        Debug.Log("<color=red>Tester Log: Memutar animasi state 'rage' bawaan MonsterMutant7!</color>");
        animator.Play("rage");

        yield return new WaitForSeconds(3.0f);

        isScreaming = false;
        isRaging = true; 
        
        agent.isStopped = false;
        agent.acceleration = originalAcceleration;
        agent.speed = 6.0f; 
        attackCooldown = 1.0f; 
    }

    IEnumerator TriggerLeapAttackRoutine()
    {
        currentBossState = BossState.Idle;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.acceleration = 0;

        leapStartPosition = transform.position;
        leapTargetPosition = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        leapProgress = 0f; 
        
        transform.LookAt(leapTargetPosition);

        animator.speed = 1.0f;
        animator.Play("idle1");
        yield return new WaitForSeconds(0.4f); 

        animator.Play("jump");
        isLeaping = true; 

        yield return new WaitForSeconds(1.5f);

        isLeaping = false;
        if (!agent.enabled) agent.enabled = true; 
        agent.isStopped = false;
        agent.acceleration = originalAcceleration;
    }

    public void TriggerBossDeath()
    {
        isDead = true;
        currentBossState = BossState.Dead;
        if (audioSource != null) audioSource.Stop();
        
        if (!agent.enabled) agent.enabled = true;
        agent.isStopped = true;
        agent.enabled = false; 
        
        animator.speed = 1.0f;
        animator.Play("death1");
        Debug.Log("<color=green>Tester Log: Boss mati menggunakan state death1!</color>");
    }
}