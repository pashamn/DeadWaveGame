using UnityEngine;
using Invector.vCharacterController;

public enum DeadWaveWeapon { Punch, Knife, Firearm }

public class PlayerWeaponCameraManager : MonoBehaviour
{
    private int lastPunchID = 1;

    [Header("Current Status")]
    public DeadWaveWeapon activeWeapon = DeadWaveWeapon.Punch;

    [Header("Attack Settings")]
    public float punchCooldown = 0.5f; // Jeda waktu antar pukulan agar tidak bisa di-spam
    private float nextAttackTime = 0f;
    public int punchDamage = 25;       // Jumlah damage sekali hit
    public float punchRange = 1.8f;    // Jangkauan pukulan standar ke depan

    [Header("Invector & Unity References")]
    private vThirdPersonController controller;
    private vThirdPersonCamera vCam;
    private Animator animator;

    [System.Serializable]
    public struct CameraValues
    {
        public float distance;     // Jarak mundur kamera
        public float height;       // Tinggi kamera
        public float rightOffset;  // Geser kanan (Over-the-shoulder)
    }

    [Header("Camera Profiles Settings")]
    public CameraValues punchCamera = new CameraValues { distance = 3.0f, height = 1.4f, rightOffset = 0.0f };
    public CameraValues knifeCamera = new CameraValues { distance = 2.4f, height = 1.3f, rightOffset = 0.2f };
    public CameraValues firearmCamera = new CameraValues { distance = 1.8f, height = 1.5f, rightOffset = 0.6f };

    private void Awake()
    {
        // Mencari komponen otomatis di dalam Object Player
        controller = GetComponent<vThirdPersonController>();
        animator = GetComponent<Animator>();
        vCam = FindFirstObjectByType<vThirdPersonCamera>();
    }

    private void Update()
    {
        // 1. Fitur simulasi ganti senjata menggunakan tombol angka 1, 2, 3 di keyboard
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeWeaponState(DeadWaveWeapon.Punch);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeWeaponState(DeadWaveWeapon.Knife);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeWeaponState(DeadWaveWeapon.Firearm);

        // 2. DETEKSI INPUT MEMUKUL: Jika klik kiri ditekan
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }

        // 3. Transisi perubahan posisi kamera secara smooth setiap frame
        ApplyCameraSmoothTransition();
    }

    private void TryAttack()
    {
        // Cek apakah jeda cooldown memukul sudah selesai
        if (Time.time < nextAttackTime) return;

        // Hanya eksekusi pukulan jika player sedang berada di mode tangan kosong (Punch)
        if (activeWeapon == DeadWaveWeapon.Punch)
        {
            ExecutePunch();
        }
    }

    private void ExecutePunch()
    {
        if (animator == null) return;

        // Bersihkan sisa-sisa antrean trigger sebelumnya agar tidak tersumbat sistem Invector
        animator.ResetTrigger("Punch");

        // Atur jeda waktu untuk serangan berikutnya
        nextAttackTime = Time.time + punchCooldown;

        // Sistem bergantian: jika pukulan terakhir kanan (1), sekarang kiri (0), dan sebaliknya
        if (lastPunchID == 1)
        {
            lastPunchID = 0; // Pukulan Kiri
        }
        else
        {
            lastPunchID = 1; // Pukulan Kanan
        }

        // Kirim data ke parameter di Animator Controller Invector
        animator.SetInteger("PunchID", lastPunchID); 
        animator.SetTrigger("Punch");               

        // Konfirmasi di tab Console
        string tangan = (lastPunchID == 0) ? "KIRI" : "KANAN";
        Debug.Log($"DeadWave Log: Player memukul dengan tangan {tangan}!");

        // Eksekusi kalkulasi hit damage ke zombie
        CheckMeleeHit();
    }

    private void CheckMeleeHit()
    {
        // Titik awal deteksi di area dada player
        Vector3 rayOrigin = transform.position + Vector3.up * 1.2f; 
        Vector3 rayDirection = transform.forward; 

        // --- SOLUSI 1: DETEKSI JARAK DEKAT / MENEMPEL (OverlapSphere) ---
        // Membuat lingkaran sensor tak terlihat sebesar radius 0.6 meter di sekeliling player
        Collider[] closeHits = Physics.OverlapSphere(rayOrigin, 0.6f);
        foreach (Collider hitCollider in closeHits)
        {
            // Pastikan kita tidak memukul diri kita sendiri
            if (hitCollider.gameObject == this.gameObject) continue;

            ZombieHealth zombieClose = hitCollider.GetComponent<ZombieHealth>();
            if (zombieClose != null)
            {
                zombieClose.TakeDamage(punchDamage);
                Debug.Log($"DeadWave Log: [JARAK DEKAT] Pukulan mengenai {hitCollider.name}! Zombie diberi damage: {punchDamage}");
                return; // Langsung keluar dari fungsi agar tidak memicu double-hit di frame yang sama
            }
        }

        // --- SOLUSI 2: DETEKSI JARAK STANDAR (SphereCast) ---
        // Jika tidak ada zombie yang menempel ketat, gunakan tembakan sensor lurus ke depan
        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, 0.3f, rayDirection, out hit, punchRange))
        {
            ZombieHealth zombieStandard = hit.transform.GetComponent<ZombieHealth>();
            if (zombieStandard != null)
            {
                zombieStandard.TakeDamage(punchDamage);
                Debug.Log($"DeadWave Log: [JARAK STANDAR] Pukulan mengenai {hit.transform.name}! Zombie diberi damage: {punchDamage}");
            }
        }
    }

    public void ChangeWeaponState(DeadWaveWeapon newWeapon)
    {
        activeWeapon = newWeapon;

        if (controller == null) return;

        // Aturan Perilaku Karakter & Kamera TPS berdasarkan senjata yang dipilih
        switch (activeWeapon)
        {
            case DeadWaveWeapon.Punch:
                controller.isStrafing = false; // Bebas berputar (Free Locomotion)
                break;

            case DeadWaveWeapon.Knife:
                controller.isStrafing = false; // Tetap bebas berputar agar lincah menyabet pisau
                break;

            case DeadWaveWeapon.Firearm:
                controller.isStrafing = true;  // Mengunci badan player menghadap lurus ke depan screen (Membidik)
                break;
        }
    }

    private void ApplyCameraSmoothTransition()
    {
        if (vCam == null) return;

        // Ambil target profile kamera berdasarkan senjata aktif saat ini
        CameraValues targetProfile = punchCamera;
        if (activeWeapon == DeadWaveWeapon.Knife) targetProfile = knifeCamera;
        if (activeWeapon == DeadWaveWeapon.Firearm) targetProfile = firearmCamera;

        // Lakukan LERP (perubahan angka secara halus) ke variabel vThirdPersonCamera bawaan Invector
        vCam.defaultDistance = Mathf.Lerp(vCam.defaultDistance, targetProfile.distance, Time.deltaTime * 6f);
        vCam.height = Mathf.Lerp(vCam.height, targetProfile.height, Time.deltaTime * 6f);
        vCam.rightOffset = Mathf.Lerp(vCam.rightOffset, targetProfile.rightOffset, Time.deltaTime * 6f);
    }

    // Menggambar garis indikator merah di Unity Scene View untuk membantu kamu melihat jangkauan pukulan
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.2f;
        Gizmos.DrawRay(rayOrigin, transform.forward * punchRange);
        Gizmos.DrawWireSphere(rayOrigin, 0.6f);
    }
}