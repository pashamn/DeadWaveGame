using UnityEngine;
using Invector.vCharacterController;
using System.Collections;
using TMPro; // WAJIB: Ditambahkan agar bisa mengontrol TextMeshPro UI

// Status 3 Senjata DeadWave
public enum DeadWaveWeapon { Punch, Melee, Firearm }

public class PlayerWeaponCameraManager : MonoBehaviour
{
    private int lastPunchID = 1;
    private bool isPickingUp = false;

    [Header("Weapon Objects")]
    public GameObject punchMeshObject;
    public GameObject meleeMeshObject;
    public GameObject firearmMeshObject;

    [Header("Sistem Tembakan Proyektil")]
    public Transform firePoint;             
    public GameObject muzzleFlashPrefab;   
    public GameObject hitEffectPrefab;     
    public GameObject bulletPrefab;        
    public float bulletSpeed = 80f;        

    [Header("Sistem Peluru & UI")]
    public TextMeshProUGUI ammoUIText;     // Tarik objek AmmoText (TMP) ke slot ini
    public int magCapacity = 30;           // Kapasitas maksimal magasin AK74 (Dinamis)
    public int ammoInMag = 30;             // Peluru di dalam senjata saat ini
    public int carriableAmmo = 60;         // Peluru cadangan di dalam tas
    public int maxCarriableAmmo = 180;     // Batas maksimal peluru cadangan di tas
    private bool isReloading = false;      

    [Header("Current Status")]
    public DeadWaveWeapon activeWeapon = DeadWaveWeapon.Punch;

    [Header("Ownership Status (Kunci Senjata)")]
    public bool hasMelee = false;
    public bool hasFirearm = false;

    [Header("Punch Settings")]
    public float punchCooldown = 0.5f;
    public int   punchDamage   = 25;
    public float punchRange    = 1.8f;

    [Header("Melee (Crowbar/Bat) Settings")]
    public float meleeCooldown = 0.6f;
    public int   meleeDamage   = 45;
    public float meleeRange    = 2.2f;     

    [Header("Firearm (Gun) Settings")]
    public float fireRate   = 0.2f;   // Diatur 'public' agar bisa dimodifikasi oleh Spawner
    public int   fireDamage = 35;     // Diatur 'public' agar bisa dimodifikasi oleh Spawner
    public float fireRange  = 50f;    

    [Header("WeaponSwitch Settings")]
    public float switchDuration = 0.5f; 

    [Header("DeadWave Audio Clips")]
    [Tooltip("Masukkan 2 atau lebih variasi suara langkah kaki di sini")]
    public AudioClip[] walkSounds;       // Array variasi langkah kaki
    public AudioClip punchSound;         // Efek suara hantaman tinju
    public AudioClip meleeSound;         // Efek suara hantaman/ayunan linggis keras
    public AudioClip firearmSound;       // Letusan suara tembakan AK74
    
    [Header("DeadWave Volume Controllers")]
    [Range(0f, 1f)] public float walkVolume = 0.25f;    // Bawaan 25% (Lembut)
    [Range(0f, 1f)] public float runVolume = 0.45f;     // Bawaan 45% (Sedang)
    [Range(0f, 1f)] public float punchVolume = 0.5f;    // Bawaan 50%
    [Range(0f, 1f)] public float meleeVolume = 0.6f;    // Bawaan 60%
    [Range(0f, 1f)] public float firearmVolume = 0.55f; // Bawaan 55%

    [Header("Footstep Timers")]
    [Range(0.1f, 1f)] public float footstepIntervalWalk = 0.5f; // Ritme jeda jalan
    [Range(0.1f, 1f)] public float footstepIntervalRun = 0.28f; // Ritme jeda lari

    [Header("Invector & Unity References")]
    private vThirdPersonController controller;
    private vThirdPersonCamera      vCam;
    private Animator                animator;
    private AudioSource             audioSource; 

    [System.Serializable]
    public struct CameraValues
    {
        public float distance;
        public float height;
        public float rightOffset;
    }

    [Header("Camera Profiles Settings")]
    public CameraValues punchCamera   = new CameraValues { distance = 3.0f, height = 1.4f, rightOffset = 0.0f };
    public CameraValues meleeCamera   = new CameraValues { distance = 2.5f, height = 1.3f, rightOffset = 0.3f };
    public CameraValues firearmCamera = new CameraValues { distance = 1.8f, height = 1.4f, rightOffset = 0.9f }; 

    private float nextFireTime = 0f;
    private bool  isSwitching  = false;
    private float footstepTimer = 0f;

    private void Awake()
    {
        controller  = GetComponent<vThirdPersonController>();
        animator    = GetComponent<Animator>();
        vCam        = FindFirstObjectByType<vThirdPersonCamera>();
        audioSource = GetComponent<AudioSource>(); 
    }

    private void Start()
    {
        ApplyWeaponLayerState();
        UpdateAmmoUI(); 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0) && !Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleFootstepAudio();

        if (isSwitching || isPickingUp || isReloading) return; 

        HandleWeaponSwitchInput();
        HandleAttackInput();
        HandleReloadInput();
        ApplyCameraSmoothTransition();

        if (activeWeapon == DeadWaveWeapon.Firearm && controller != null && vCam != null)
        {
            controller.isStrafing = true;
            Vector3 cameraForward = vCam.transform.forward;
            cameraForward.y = 0; 
            if (cameraForward != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(cameraForward);
            }
        }
    }

    private void HandleFootstepAudio()
    {
        if (controller == null || audioSource == null || walkSounds == null || walkSounds.Length == 0) return;

        if (controller.input.magnitude > 0.1f && controller.isGrounded)
        {
            footstepTimer += Time.deltaTime;

            if (controller.isSprinting)
            {
                if (footstepTimer >= footstepIntervalRun)
                {
                    audioSource.pitch = Random.Range(1.1f, 1.2f); 
                    PlayRandomFootstep(runVolume); 
                    footstepTimer = 0f;
                }
            }
            else
            {
                if (footstepTimer >= footstepIntervalWalk)
                {
                    audioSource.pitch = Random.Range(0.95f, 1.05f);
                    PlayRandomFootstep(walkVolume); 
                    footstepTimer = 0f;
                }
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void PlayRandomFootstep(float volume)
    {
        int randomIndex = Random.Range(0, walkSounds.Length);
        AudioClip selectedClip = walkSounds[randomIndex];
        
        if (selectedClip != null)
        {
            audioSource.PlayOneShot(selectedClip, volume);
        }
    }

    private void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = 1f; 
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void HandleWeaponSwitchInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) StartWeaponSwitch(DeadWaveWeapon.Punch, "Switch1");
        if (Input.GetKeyDown(KeyCode.Alpha2) && hasMelee) StartWeaponSwitch(DeadWaveWeapon.Melee, "Switch2");
        if (Input.GetKeyDown(KeyCode.Alpha3) && hasFirearm) StartWeaponSwitch(DeadWaveWeapon.Firearm, "Switch3");
    }

    private void HandleAttackInput()
    {
        if (Input.GetMouseButton(0))
            TryAttack();
    }

    private void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
            TriggerReload();
    }

    private void StartWeaponSwitch(DeadWaveWeapon newWeapon, string switchTrigger)
    {
        if (activeWeapon == newWeapon) return;

        activeWeapon = newWeapon;
        isSwitching  = true;

        if (animator != null && animator.layerCount > 4)
        {
            animator.SetLayerWeight(4, 1f);
        }
        
        animator.SetBool("IsSwitching", true);
        animator.SetFloat("WeaponIndex", (float)newWeapon);
        animator.SetTrigger(switchTrigger);

        Invoke(nameof(FinishWeaponSwitch), switchDuration);
    }

    private void FinishWeaponSwitch()
    {
        isSwitching = false;
        animator.SetBool("IsSwitching", false);
        
        if (animator != null && animator.layerCount > 4) 
        {
            animator.SetLayerWeight(4, 0f); 
        }
        
        ApplyWeaponLayerState();
        UpdateAmmoUI(); 
    }

    public void ChangeWeaponState(DeadWaveWeapon newWeapon)
    {
        string trigger = "Switch" + ((int)newWeapon + 1);
        StartWeaponSwitch(newWeapon, trigger);
    }

    private void ApplyWeaponLayerState()
    {
        if (controller == null || animator == null) return;

        bool hasLayer1 = animator.layerCount > 1;
        bool hasLayer2 = animator.layerCount > 2;
        bool hasLayer4 = animator.layerCount > 4;

        switch (activeWeapon)
        {
            case DeadWaveWeapon.Punch:
                controller.isStrafing = false;
                animator.SetBool("IsHoldingKnife", false);
                animator.SetBool("IsAiming",       false);
                if (hasLayer1) animator.SetLayerWeight(1, 0f); 
                if (hasLayer2) animator.SetLayerWeight(2, 0f); 
                if (hasLayer4) animator.SetLayerWeight(4, 0f); 
                break;

            case DeadWaveWeapon.Melee:
                controller.isStrafing = false;
                animator.SetBool("IsHoldingKnife", true);
                animator.SetBool("IsAiming",       false);
                if (hasLayer1) animator.SetLayerWeight(1, 1f);
                if (hasLayer2) animator.SetLayerWeight(2, 0f);
                if (hasLayer4) animator.SetLayerWeight(4, 0f);
                break;

            case DeadWaveWeapon.Firearm:
                controller.isStrafing = true;
                animator.SetBool("IsHoldingKnife", false);
                animator.SetBool("IsAiming",       true);
                if (hasLayer1) animator.SetLayerWeight(1, 0f);
                if (hasLayer2) animator.SetLayerWeight(2, 1f);
                if (hasLayer4) animator.SetLayerWeight(4, 0f);
                break;
        }

        SetWeaponObjects(activeWeapon);
    }

    private void SetWeaponObjects(DeadWaveWeapon w)
    {
        if (punchMeshObject)   punchMeshObject.SetActive(w == DeadWaveWeapon.Punch);
        if (meleeMeshObject)   meleeMeshObject.SetActive(w == DeadWaveWeapon.Melee);
        if (firearmMeshObject) firearmMeshObject.SetActive(w == DeadWaveWeapon.Firearm);
    }

    public void TriggerReload()
    {
        if (activeWeapon != DeadWaveWeapon.Firearm || isSwitching || isReloading || ammoInMag >= magCapacity || carriableAmmo <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        animator.SetTrigger("Reload");
        Debug.Log("DeadWave Log: Memulai Reload...");

        yield return new WaitForSeconds(1.2f); 

        // PERBAIKAN UTAMA: Perhitungan reload sekarang dinamis mengikuti variabel magCapacity hasil upgrade!
        int ammoNeeded = magCapacity - ammoInMag; 
        int ammoToTransfer = Mathf.Min(ammoNeeded, carriableAmmo);

        ammoInMag += ammoToTransfer;
        carriableAmmo -= ammoToTransfer;

        isReloading = false;
        UpdateAmmoUI(); 
        Debug.Log($"DeadWave Log: Reload Selesai dengan Kapasitas Baru!");
    }

    private void TryAttack()
    {
        if (Time.time < nextFireTime || isReloading) return;

        switch (activeWeapon)
        {
            case DeadWaveWeapon.Punch:
                if (Input.GetMouseButtonDown(0)) ExecutePunch();
                break;
            case DeadWaveWeapon.Melee:
                if (Input.GetMouseButtonDown(0)) ExecuteMeleeAttack();
                break;
            case DeadWaveWeapon.Firearm:
                ExecuteFirearm();
                break;
        }
    }

    private void ExecutePunch()
    {
        if (animator == null) return;
        nextFireTime = Time.time + punchCooldown;
        lastPunchID = lastPunchID == 1 ? 0 : 1;
        animator.SetInteger("PunchID", lastPunchID);
        animator.ResetTrigger("Punch");
        animator.SetTrigger("Punch");

        PlaySFX(punchSound, punchVolume); 

        CheckMeleeHit(punchDamage, punchRange);
    }

    private void ExecuteMeleeAttack()
    {
        if (animator == null) return;
        nextFireTime = Time.time + meleeCooldown;
        animator.ResetTrigger("MeleeAttack");
        animator.SetTrigger("MeleeAttack");

        PlaySFX(meleeSound, meleeVolume); 

        StartCoroutine(DelayedMeleeHitCheck(0.15f, meleeDamage, meleeRange));
    }

    private IEnumerator DelayedMeleeHitCheck(float delay, int damage, float range)
    {
        yield return new WaitForSeconds(delay);
        CheckMeleeHit(damage, range);
    }

    private void ExecuteFirearm()
    {
        if (animator == null) return;

        if (ammoInMag <= 0)
        {
            Debug.Log("DeadWave Log: *Klik* Peluru Habis! Tekan R untuk Reload.");
            nextFireTime = Time.time + 0.5f;
            return;
        }

        ammoInMag--;
        nextFireTime = Time.time + fireRate;
        UpdateAmmoUI(); 

        PlaySFX(firearmSound, firearmVolume); 

        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            flash.transform.parent = firePoint; 
            Destroy(flash, 0.5f);
        }

        if (bulletPrefab != null && firePoint != null)
        {
            Camera mainCam = Camera.main;
            Vector3 targetPoint;

            Vector3 rayOrigin = mainCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(rayOrigin, mainCam.transform.forward, out RaycastHit hit, fireRange))
            {
                targetPoint = hit.point; 
            }
            else
            {
                targetPoint = rayOrigin + mainCam.transform.forward * fireRange; 
            }

            Vector3 travelDirection = (targetPoint - firePoint.position).normalized;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(travelDirection));
            
            DeadWaveProjectile projScript = bullet.GetComponent<DeadWaveProjectile>();
            if (projScript != null)
            {
                projScript.SetupProjectile(fireDamage, hitEffectPrefab);
            }

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = travelDirection * bulletSpeed;
            }

            Destroy(bullet, 3f);
        }
    }

    private void CheckMeleeHit(int damage, float range)
    {
        Vector3 rayOrigin    = transform.position + Vector3.up * 1.2f;
        Vector3 rayDirection = transform.forward;

        foreach (Collider col in Physics.OverlapSphere(rayOrigin, 0.8f))
        {
            if (col.gameObject == gameObject) continue;
            ZombieHealth zombie = col.GetComponentInParent<ZombieHealth>();
            if (zombie != null) { zombie.TakeDamage(damage); return; }
        }

        if (Physics.SphereCast(rayOrigin, 0.4f, rayDirection, out RaycastHit hit, range))
        {
            ZombieHealth zombie = hit.transform.GetComponentInParent<ZombieHealth>();
            if (zombie != null) zombie.TakeDamage(damage);
        }
    }

    private void ApplyCameraSmoothTransition()
    {
        if (vCam == null) return;

        CameraValues target = activeWeapon switch
        {
            DeadWaveWeapon.Melee   => meleeCamera,
            DeadWaveWeapon.Firearm => firearmCamera,
            _                      => punchCamera
        };

        float t = Time.deltaTime * 6f;
        vCam.defaultDistance = Mathf.Lerp(vCam.defaultDistance, target.distance,    t);
        vCam.height          = Mathf.Lerp(vCam.height,          target.height,      t);
        vCam.rightOffset     = Mathf.Lerp(vCam.rightOffset,     target.rightOffset, t);

        if (activeWeapon == DeadWaveWeapon.Firearm)
        {
            vCam.xMouseSensitivity = 1.5f; 
            vCam.yMouseSensitivity = 1.5f;
        }
        else
        {
            vCam.xMouseSensitivity = 3.5f; 
            vCam.yMouseSensitivity = 3.5f;
        }
    }

    public void UpdateAmmoUI()
    {
        if (ammoUIText == null) return;

        if (activeWeapon == DeadWaveWeapon.Firearm)
        {
            ammoUIText.text = $"{ammoInMag} / {carriableAmmo}";
        }
        else
        {
            ammoUIText.text = "---"; 
        }
    }

    public void AddCarriableAmmo(int amount)
    {
        carriableAmmo = Mathf.Min(carriableAmmo + amount, maxCarriableAmmo);
        UpdateAmmoUI(); 
        Debug.Log($"DeadWave Log: Peluru Cadangan Ditambahkan! Total: {carriableAmmo}");
    }

    // PERBAIKAN: Fungsi ini diaktifkan agar saat panel ditutup, UI teks amunisi langsung dipaksa refresh
    public void UpgradeWeaponStats(int waveSelesai)
    {
        UpdateAmmoUI();
    }

    public void OnWeaponSwitch() { }
    public void OnSwitchStart()  { }
    public void OnMagOut()       { }
    public void OnMagIn()        { }
    public void OnBolt()         { }
    public void OnItemEquip()    { ApplyWeaponLayerState(); }   

    public void InteractAndEquipWeapon(DeadWaveWeapon newWeapon)
    {
        if (isSwitching || isPickingUp || isReloading) return;
        StartCoroutine(PickupThenSwitchRoutine(newWeapon));
    }

    private IEnumerator PickupThenSwitchRoutine(DeadWaveWeapon newWeapon)
    {
        isPickingUp = true;
        
        if (animator != null && animator.layerCount > 3)
        {
            animator.SetLayerWeight(3, 1f);
        }
        
        animator.SetTrigger("ItemPickUp");

        yield return null;
        yield return new WaitForSeconds(0.35f);
        activeWeapon = newWeapon;

        if (animator != null && animator.layerCount > 3)
        {
            while (animator.GetCurrentAnimatorStateInfo(3).IsName("ItemPickup") || 
                   animator.GetCurrentAnimatorStateInfo(3).IsName("ItemPickUp") ||
                   animator.IsInTransition(3))
            {
                yield return null;
            }
            animator.SetLayerWeight(3, 0f);
        }
        
        isPickingUp = false;
        isSwitching = true;
        
        if (animator != null && animator.layerCount > 4)
        {
            animator.SetLayerWeight(4, 1f);
        }
        
        animator.SetBool("IsSwitching", true);
        animator.SetFloat("WeaponIndex", (float)newWeapon);
        
        string switchTrigger = "Switch" + ((int)newWeapon + 1);
        animator.SetTrigger(switchTrigger);

        Invoke(nameof(FinishWeaponSwitch), switchDuration);
    }
}