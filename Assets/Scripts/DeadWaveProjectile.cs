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

        // 2. Berikan Damage ke Zombie
        ZombieHealth zombie = other.GetComponentInParent<ZombieHealth>();
        if (zombie != null)
        {
            zombie.TakeDamage(damage);
            Debug.Log($"DeadWave Log: Peluru Fisik Menembus {zombie.gameObject.name}! Damage: {damage}");
        }
        else
        {
            Debug.Log($"DeadWave Log: Peluru Fisik Menabrak: {other.gameObject.name}");
        }

        // 3. Hancurkan objek peluru ini setelah menabrak sesuatu
        Destroy(gameObject);
    }
}