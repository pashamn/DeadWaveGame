using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Specs")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 5.5f;
    public float sprintSpeed = 8.5f;
    public float rotationSpeed = 10.0f;
    
    [Header("Jump & Gravity")]
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.1f;
    private float cameraPitch = 0.0f;

    // Komponen internal
    private CharacterController controller;
    private Animator animator;
    private PlayerInputActions inputActions;

    // Variabel kalkulasi data
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSprinting = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerInputActions();

        // Mengunci kursor mouse di tengah layar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Sprint.performed += ctx => isSprinting = true;
        inputActions.Player.Sprint.canceled += ctx => isSprinting = false;

        inputActions.Player.Jump.performed += ctx => TryJump();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        CalculateMovement();
        CalculateCameraRotation();
        UpdateAnimatorParameters();
    }

    private void CalculateMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Mengunci player ke tanah
        }

        // Arah gerak lurus mengikuti sudut pandang kamera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        moveDirection = forward * moveInput.y + right * moveInput.x;

        // Menentukan kecepatan berdasarkan input WASD dan tombol Sprint
        float currentSpeed = walkSpeed;
        if (moveInput != Vector2.zero)
        {
            currentSpeed = isSprinting ? sprintSpeed : runSpeed;
        }

        // Gerakkan Player ke arah X dan Z
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Mode Eksplorasi Bebas (Unarmed): Badan berputar smooth mengikuti arah tombol WASD yang ditekan
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Hitung gaya gravitasi vertikal (Y)
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void TryJump()
    {
        if (isGrounded)
        {
            // Rumus fisika lompat vertikal
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            // Memainkan animasi lompat bawaan file controller kamu
            if (moveInput.magnitude < 0.1f)
                animator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                animator.CrossFadeInFixedTime("JumpMove", 0.2f);
        }
    }

   private void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        // 1. Set Parameter IsGrounded & IsSprinting sesuai status asli player
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsSprinting", isSprinting && moveInput != Vector2.zero);
        animator.SetBool("IsStrafing", false); // Sementara dimatikan karena belum membidik

        // 2. KOREKSI UTAMA: Mengirim data gerakan horizontal (A/D) dan vertikal (W/S) secara akurat
        // moveInput.y akan bernilai 1 jika tekan W, dan bernilai -1 jika tekan S (Mundur)
        float targetH = moveInput.x;
        float targetV = moveInput.y;

        animator.SetFloat("InputHorizontal", Mathf.Lerp(animator.GetFloat("InputHorizontal"), targetH, Time.deltaTime * 10f));
        animator.SetFloat("InputVertical", Mathf.Lerp(animator.GetFloat("InputVertical"), targetV, Time.deltaTime * 10f));

        // 3. Set Parameter InputMagnitude untuk mendeteksi kecepatan total gabungan
        float targetMagnitude = 0f;
        if (moveInput != Vector2.zero)
        {
            if (isSprinting) targetMagnitude = 1.5f; // Memicu animasi Sprint Invector
            else targetMagnitude = 1.0f;             // Memicu animasi Run Invector
        }
        animator.SetFloat("InputMagnitude", Mathf.Lerp(animator.GetFloat("InputMagnitude"), targetMagnitude, Time.deltaTime * 10f));
    }
    private void CalculateCameraRotation()
    {
        // Memutar sudut pandang kamera atas dan bawah lewat gerakan Mouse Y
        cameraPitch -= lookInput.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -30f, 60f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, cameraTransform.localRotation.eulerAngles.y, 0f);
        }
    }
}