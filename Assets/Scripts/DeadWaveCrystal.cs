using UnityEngine;

public class DeadWaveCrystal : MonoBehaviour
{
    public enum LootType { AmmoBox, Medkit }

    [Header("Tipe Kotak Loot")]
    public LootType jenisLoot = LootType.AmmoBox;

    [Header("Pengaturan Magnet GPS")]
    public float magnetRadius = 5f;    
    public float flySpeed = 8f;        

    [Header("Bonus Skor (EXP)")]
    public int scoreValue = 100;       

    [Header("Jika Berisi Peluru (Ammo Settings)")]
    public int minAmmoToGrant = 15;    
    public int maxAmmoToGrant = 30;    

    [Header("Jika Berisi Medkit (Health Settings)")]
    public int healAmount = 25;

    private Transform playerTransform;
    private PlayerWeaponCameraManager playerWeaponManager; 
    private PlayerHealth playerHealth;
    private bool isFlying = false;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerWeaponManager = player.GetComponent<PlayerWeaponCameraManager>();
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // --- GERBANG PENGAMAN BARU (ANTI-SEDOT JIKA BELUM PUNYA SENJATA) ---
        if (jenisLoot == LootType.AmmoBox)
        {
            // Cek apakah player sudah membuka gembok senjata api (hasFirearm)
            if (playerWeaponManager != null && !playerWeaponManager.hasFirearm)
            {
                // Jika belum punya AK74, kunci magnetnya agar kotak peluru tetap diam di lantai
                isFlying = false;
                return; 
            }
        }
        // -------------------------------------------------------------------

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // PENGAMAN MEDKIT: Jika darah player sudah penuh, medkit tidak akan menyedot
        if (jenisLoot == LootType.Medkit && playerHealth != null && playerHealth.currentHealth >= playerHealth.maxHealth)
        {
            isFlying = false;
            return; 
        }

        // Jika lolos semua pengaman dan masuk radius, aktifkan magnet terbang
        if (distanceToPlayer <= magnetRadius)
        {
            isFlying = true;
        }

        if (isFlying)
        {
            Vector3 targetPosition = playerTransform.position + Vector3.up * 1f; 
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, flySpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
            {
                ExecuteAutoUseLoot();
            }
        }
    }

    private void ExecuteAutoUseLoot()
    {
        if (ZombieSpawner.Instance != null)
        {
            ZombieSpawner.Instance.AddScore(scoreValue);
        }

        if (jenisLoot == LootType.AmmoBox && playerWeaponManager != null)
        {
            int randomAmmoGained = Random.Range(minAmmoToGrant, maxAmmoToGrant + 1);
            playerWeaponManager.AddCarriableAmmo(randomAmmoGained);
        }
        else if (jenisLoot == LootType.Medkit && playerHealth != null)
        {
            int newHealth = playerHealth.currentHealth + healAmount;
            playerHealth.SetHealthFromSpawner(newHealth);
        }

        Destroy(gameObject);
    }
}