    using UnityEngine;
    using System.Collections;

    public class DeadWaveItemPickup : MonoBehaviour
    {
        [Header("Pickup Configuration")]
        public DeadWaveWeapon weaponToGrant;
        public string interactionPrompt = "[E] Ambil Senjata";

        [Header("UI")]
        public GameObject pickupPromptUI;

        [Header("Visual Rotation (Optional)")]
        public bool rotateOnGround = true;
        public float rotationSpeed = 40f;

        private bool isPlayerNearby = false;
        private bool isAlreadyPickedUp = false;
        private PlayerWeaponCameraManager playerManager;

        private void Start()
        {
            if (pickupPromptUI != null)
            {
                pickupPromptUI.SetActive(false);
            }
        }

        private void Update()
        {
            if (isAlreadyPickedUp)
                return;

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
            if (playerManager == null)
                yield break;

            isAlreadyPickedUp = true;

            // Sembunyikan UI prompt
            if (pickupPromptUI != null)
            {
                pickupPromptUI.SetActive(false);
            }

            if (weaponToGrant == DeadWaveWeapon.Punch)
            {
                Debug.LogError("DeadWave Error: Jangan setel Weapon To Grant di tanah sebagai 'Punch'!");
                yield break;
            }

            // Matikan collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            // Sembunyikan model senjata
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                rend.enabled = false;
            }

            // Buka slot inventory
            if (weaponToGrant == DeadWaveWeapon.Melee)
            {
                playerManager.hasMelee = true;
            }
            else if (weaponToGrant == DeadWaveWeapon.Firearm)
            {
                playerManager.hasFirearm = true;
            }

            // Hapus GPS route jika ada
            if (DeadWaveQuestTracker.Instance != null)
            {
                DeadWaveQuestTracker.Instance.ClearWeaponRoute();
            }

            // Equip senjata
            playerManager.InteractAndEquipWeapon(weaponToGrant);

            Debug.Log($"DeadWave Log: Slot dibuka, memproses antrean visual untuk {weaponToGrant}");

            yield return new WaitForSeconds(0.1f);

            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isAlreadyPickedUp)
                return;

            if (other.CompareTag("Player"))
            {
                playerManager = other.GetComponent<PlayerWeaponCameraManager>();

                if (playerManager != null)
                {
                    isPlayerNearby = true;

                    if (pickupPromptUI != null)
                    {
                        pickupPromptUI.SetActive(true);
                    }

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

                if (pickupPromptUI != null)
                {
                    pickupPromptUI.SetActive(false);
                }
            }
        }
    }