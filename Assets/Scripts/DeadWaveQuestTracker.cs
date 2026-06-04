using UnityEngine;
using UnityEngine.AI; // WAJIB: Untuk menghitung rute kalkulasi jalan NavMesh

[RequireComponent(typeof(LineRenderer))]
public class DeadWaveQuestTracker : MonoBehaviour
{
    public static DeadWaveQuestTracker Instance;

    [Header("Target Weapons")]
    public Transform meleeWeaponTransform;  // Tarik objek lantai Crowbar ke sini
    public Transform rifleWeaponTransform;  // Tarik objek lantai AK74 ke sini

    [Header("Line Visual Settings")]
    public float yOffset = 0.1f;            // Biar garis agak mengambang sedikit di atas lantai (tidak tenggelam)
    
    private LineRenderer lineRenderer;
    private Transform playerTransform;
    private Transform currentTarget;
    private NavMeshPath navPath;

    void Awake()
    {
        if (Instance == null) Instance = this;
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        // Cari player secara otomatis lewat Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        navPath = new NavMeshPath();
        
        // Matikan garis di awal permainan sebelum kill tercapai
        lineRenderer.enabled = false;
    }

    void Update()
    {
        // Jika tidak ada target, tidak ada player, atau komponen dimatikan, stop eksekusi
        if (currentTarget == null || playerTransform == null || !lineRenderer.enabled) return;

        // Hitung rute jalan pintar dari posisi Player ke posisi Senjata menghindari dinding/rintangan
        if (NavMesh.CalculatePath(playerTransform.position, currentTarget.position, NavMesh.AllAreas, navPath))
        {
            // Setel jumlah titik sudut pada Line Renderer sesuai hasil tikungan NavMesh
            lineRenderer.positionCount = navPath.corners.Length;

            for (int i = 0; i < navPath.corners.Length; i++)
            {
                // Beri sedikit posisi Y offset agar garis terlihat jelas di atas permukaan lantai
                Vector3 pointPosition = navPath.corners[i];
                pointPosition.y += yOffset;
                
                lineRenderer.SetPosition(i, pointPosition);
            }
        }
    }

    // Fungsi untuk menyalakan garis petunjuk arah (Dipanggil dari ZombieSpawner)
    public void ActivationWeaponRoute(int weaponID)
    {
        if (weaponID == 1) // Jalur Melee
        {
            if (meleeWeaponTransform != null)
            {
                currentTarget = meleeWeaponTransform;
                lineRenderer.enabled = true;
            }
        }
        else if (weaponID == 2) // Jalur Rifle
        {
            if (rifleWeaponTransform != null)
            {
                currentTarget = rifleWeaponTransform;
                lineRenderer.enabled = true;
            }
        }
    }

    // Fungsi untuk mematikan garis setelah senjata sukses diambil (Dipanggil dari DeadWaveItemPickup)
    public void ClearWeaponRoute()
    {
        currentTarget = null;
        lineRenderer.enabled = false;
    }
}