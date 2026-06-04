using UnityEngine;
using System.Collections;

public class DeadWaveItemPickup : MonoBehaviour
{
    [Header("Pickup Configuration")]
    public DeadWaveWeapon weaponToGrant; 
    public string interactionPrompt = "Press E to pick up weapon";

    [Header("Visual Rotation (Optional)")]
    public bool rotateOnGround = true;
    public float rotationSpeed = 40f;

    private bool isPlayerNearby = false;
    private bool isAlreadyPickedUp = false; 
    private PlayerWeaponCameraManager playerManager;

    private void Update()
    {
        if (isAlreadyPickedUp) return; 

        if (rotateOnGround)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        }

        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(PickupProcessRoutine());
        }
    }

    private IEnumerator PickupProcessRoutine()
    {
        if (playerManager == null) yield break;

        isAlreadyPickedUp = true;

        if (weaponToGrant == DeadWaveWeapon.Punch)
        {
            Debug.LogError("DeadWave Error: Jangan setel Weapon To Grant di tanah sebagai 'Punch'!");
            yield break; 
        }

        // 1. Matikan collider sensor & mesh visual di lantai agar langsung terlihat terambil
        if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;
        foreach (var mesh in GetComponentsInChildren<Renderer>()) mesh.enabled = false;

        // 2. Buka gembok slot inventory senjata di script Player
        if (weaponToGrant == DeadWaveWeapon.Melee) playerManager.hasMelee = true;
        else if (weaponToGrant == DeadWaveWeapon.Firearm) playerManager.hasFirearm = true;

        // PERBAIKAN TUTORIAL: Matikan garis jalur GPS di lantai karena barang sudah di tangan player!
        if (DeadWaveQuestTracker.Instance != null)
        {
            DeadWaveQuestTracker.Instance.ClearWeaponRoute();
        }

        // 3. Panggil fungsi antrean animasi yang baru di script Player
        playerManager.InteractAndEquipWeapon(weaponToGrant);

        Debug.Log($"DeadWave Log: Slot dibuka, memproses antrean visual untuk {weaponToGrant}");

        // 4. Hancurkan objek sisa di tanah secara aman dari memory
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerManager = other.GetComponent<PlayerWeaponCameraManager>();
            if (playerManager != null)
            {
                isPlayerNearby = true;
                Debug.Log(interactionPrompt);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerManager = null;
        }
    }
}