using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class Character : MonoBehaviour
{
    [Header("Camara y Sensibilidad")]
    [SerializeField] private Camera camara;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Velocidades de Movimiento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7.5f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Estamina y Fatiga")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 25f;
    [SerializeField] private float staminaRegenRate = 12f;
    [SerializeField] private float exhaustionDuration = 3.5f;

    [Header("UI y Sonido de Fatiga")]
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private Color normalStaminaColor = Color.white;
    [SerializeField] private Color exhaustedStaminaColor = new Color(0.85f, 0.2f, 0.2f, 1f);
    [SerializeField] private AudioSource fatigueAudioSource;
    [SerializeField] private AudioClip fatigueBreathingClip;

    [Header("Linterna y Apuntado Fluido")]
    [Tooltip("Hueso del hombro/brazo derecho (o UpperChest) para inclinarse con la cámara")]
    [SerializeField] private Transform aimBone;
    [Tooltip("Objeto que tiene el componente Light")]
    [SerializeField] private Transform flashlightLight;
    [Tooltip("Empty ubicado en el lente/punta del modelo 3D de la linterna")]
    [SerializeField] private Transform flashlightTip;
    [SerializeField] private float armSmoothSpeed = 15f;
    [SerializeField] private float lightSmoothSpeed = 10f;

    [Header("Animaciones")]
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private string isWalkingParameterName = "isWalking";
    [SerializeField] private string isRunningParameterName = "isRunning";

    private Rigidbody rb;
    private Animator animator;

    private float rotationY;
    private float currentArmRotationY;
    private float moveInputX;
    private float moveInputZ;

    private float currentStamina;
    private float exhaustionTimer;
    private bool isExhausted;
    private bool isSprinting;

    private bool hasSpeedParam;
    private bool hasWalkingParam;
    private bool hasRunningParam;

    public bool IsSprinting => isSprinting;
    public bool IsMoving => (moveInputX != 0f || moveInputZ != 0f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        animator = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();

        if (camara == null)
        {
            camara = Camera.main;
        }
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == speedParameterName && param.type == AnimatorControllerParameterType.Float)
                hasSpeedParam = true;
            else if (param.name == isWalkingParameterName && param.type == AnimatorControllerParameterType.Bool)
                hasWalkingParam = true;
            else if (param.name == isRunningParameterName && param.type == AnimatorControllerParameterType.Bool)
                hasRunningParam = true;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    private void Update()
    {
        HandleCameraLook();
        HandleMovementInput();
        HandleStamina();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void LateUpdate()
    {
        UpdateFlashlightAim();
    }

    private void HandleCameraLook()
    {
        if (camara == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        rotationY -= mouseY;
        rotationY = Mathf.Clamp(rotationY, -maxLookAngle, maxLookAngle);
        camara.transform.localRotation = Quaternion.Euler(rotationY, 0f, 0f);
    }

    private void HandleMovementInput()
    {
        moveInputX = Input.GetAxisRaw("Horizontal");
        moveInputZ = Input.GetAxisRaw("Vertical");
    }

    private void ApplyMovement()
    {
        Vector3 moveDirection = (transform.right * moveInputX + transform.forward * moveInputZ).normalized;
        float currentSpeed = isSprinting ? runSpeed : walkSpeed;

        rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);
    }

    private void HandleStamina()
    {
        bool isMoving = (moveInputX != 0f || moveInputZ != 0f);

        if (isExhausted)
        {
            isSprinting = false;
            exhaustionTimer -= Time.deltaTime;

            currentStamina = Mathf.MoveTowards(currentStamina, maxStamina, staminaRegenRate * Time.deltaTime);

            if (exhaustionTimer <= 0f)
            {
                isExhausted = false;
                if (fatigueAudioSource != null && fatigueAudioSource.isPlaying)
                {
                    fatigueAudioSource.Stop();
                }
            }
        }
        else if (Input.GetKey(sprintKey) && isMoving && currentStamina > 0f)
        {
            isSprinting = true;
            currentStamina -= staminaDrainRate * Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                TriggerExhaustion();
            }
        }
        else
        {
            isSprinting = false;
            if (currentStamina < maxStamina)
            {
                currentStamina = Mathf.MoveTowards(currentStamina, maxStamina, staminaRegenRate * Time.deltaTime);
            }
        }

        UpdateStaminaUI();
    }

    private void TriggerExhaustion()
    {
        isExhausted = true;
        isSprinting = false;
        exhaustionTimer = exhaustionDuration;

        if (fatigueAudioSource != null && fatigueBreathingClip != null)
        {
            fatigueAudioSource.clip = fatigueBreathingClip;
            fatigueAudioSource.loop = true;
            fatigueAudioSource.Play();
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaBarFill == null) return;

        staminaBarFill.fillAmount = currentStamina / maxStamina;
        staminaBarFill.color = isExhausted ? exhaustedStaminaColor : normalStaminaColor;
    }

    private void UpdateFlashlightAim()
    {
        currentArmRotationY = Mathf.Lerp(currentArmRotationY, rotationY, Time.deltaTime * armSmoothSpeed);

        if (aimBone != null)
        {
            aimBone.rotation = Quaternion.AngleAxis(currentArmRotationY, transform.right) * aimBone.rotation;
        }

        if (flashlightLight != null)
        {
            if (flashlightTip != null && flashlightTip != flashlightLight && !flashlightTip.IsChildOf(flashlightLight))
            {
                flashlightLight.position = flashlightTip.position;
            }

            flashlightLight.rotation = Quaternion.Slerp(
                flashlightLight.rotation,
                camara.transform.rotation,
                Time.deltaTime * lightSmoothSpeed
            );
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = (moveInputX != 0f || moveInputZ != 0f);
        float targetAnimSpeed = !isMoving ? 0f : (isSprinting ? 1f : 0.5f);

        if (hasSpeedParam)
        {
            animator.SetFloat(speedParameterName, targetAnimSpeed, 0.1f, Time.deltaTime);
        }

        if (hasWalkingParam)
        {
            animator.SetBool(isWalkingParameterName, isMoving);
        }

        if (hasRunningParam)
        {
            animator.SetBool(isRunningParameterName, isMoving && isSprinting);
        }
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        isSprinting = false;

        if (hasWalkingParam && animator != null) animator.SetBool(isWalkingParameterName, false);
        if (hasRunningParam && animator != null) animator.SetBool(isRunningParameterName, false);

        if (fatigueAudioSource != null && fatigueAudioSource.isPlaying)
        {
            fatigueAudioSource.Stop();
        }
    }
}