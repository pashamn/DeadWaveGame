using UnityEngine;

public class DeadWaveProjectile : MonoBehaviour
{
    private int damage = 35;
    private GameObject hitEffectPrefab;

    // Fungsi untuk setup data peluru saat diciptakan oleh Player Manager
    public void SetupProjectile(int weaponDamage, GameObject impactEffect)
    {
        damage = weaponDamage;
        hitEffectPrefab = impactEffect;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Jangan tabrak diri sendiri (Player)
        if (other.CompareTag("Player")) return;

        // 1. Efek Visual Hantaman Peluru
        if (hitEffectPrefab != null)
        {
            // Munculkan efek debu/darah di posisi peluru menabrak
            GameObject impact = Instantiate(hitEffectPrefab, transform.position, transform.rotation);
            Destroy(impact, 1f);
        }

        // ======================== PERBAIKAN INTEGRASI DUA JENIS MUSUH ========================
        
        // 2a. Cek apakah peluru menabrak Zombie Kroco Biasa
        ZombieHealth zombieKroco = other.GetComponentInParent<ZombieHealth>();
        
        // 2b. Cek apakah peluru menabrak Zombie Boss Mutant
        ZombieBossHealth zombieBoss = other.GetComponentInParent<ZombieBossHealth>();

        if (zombieKroco != null)
        {
            zombieKroco.TakeDamage(damage);
            Debug.Log($"DeadWave Log: Peluru Fisik Menembus Kroco {zombieKroco.gameObject.name}! Damage: {damage}");
        }
        else if (zombieBoss != null)
        {
            // JIKA MENABRAK BOSS → Eksekusi fungsi pengurangan darah khusus Boss
            zombieBoss.TakeDamage(damage);
            Debug.Log($"DeadWave Log: Peluru Fisik Menembus BOSS MUTANT {zombieBoss.gameObject.name}! Damage: {damage}");
        }
        else
        {
            // Jika peluru menabrak objek lingkungan seperti dinding, tanah, atau container box
            Debug.Log($"DeadWave Log: Peluru Fisik Menabrak Objek Lingkungan: {other.gameObject.name}");
        }

        // =====================================================================================

        // 3. Hancurkan objek peluru ini setelah menabrak sesuatu
        Destroy(gameObject);
    }
}