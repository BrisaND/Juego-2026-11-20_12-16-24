using UnityEngine;

[RequireComponent(typeof(Character))]
public class FootstepSystem : MonoBehaviour
{
    [Header("Audio Source de Pasos")]
    [SerializeField] private AudioSource footstepAudioSource;

    [Header("Sonidos sobre Pasto (Terreno)")]
    [SerializeField] private AudioClip[] grassWalkClips;
    [SerializeField] private AudioClip[] grassRunClips;

    [Header("Sonidos sobre Madera")]
    [SerializeField] private AudioClip[] woodWalkClips;
    [SerializeField] private AudioClip[] woodRunClips;

    [Header("Ritmo y Detección de Suelo")]
    [SerializeField] private float walkStepInterval = 0.55f;
    [SerializeField] private float runStepInterval = 0.34f;
    [SerializeField] private string woodTag = "Wood";
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Variación Natural")]
    [SerializeField] private float minPitch = 0.92f;
    [SerializeField] private float maxPitch = 1.08f;

    private Character character;
    private Collider playerCollider;
    private float stepTimer;

    private void Awake()
    {
        character = GetComponent<Character>();
        playerCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!character.enabled || !character.IsMoving)
        {
            stepTimer = 0.1f;
            return;
        }

        if (!CheckGroundSurface(out bool isOnWood))
        {
            return;
        }

        float currentInterval = character.IsSprinting ? runStepInterval : walkStepInterval;
        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayFootstep(isOnWood, character.IsSprinting);
            stepTimer = currentInterval;
        }
    }

    private bool CheckGroundSurface(out bool isOnWood)
    {
        isOnWood = false;

        Vector3 rayOrigin = (playerCollider != null)
            ? new Vector3(transform.position.x, playerCollider.bounds.min.y + 0.2f, transform.position.z)
            : transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 0.5f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag(woodTag))
            {
                isOnWood = true;
            }
            return true;
        }

        return false;
    }

    private void PlayFootstep(bool isOnWood, bool isRunning)
    {
        if (footstepAudioSource == null) return;

        AudioClip[] selectedArray;

        if (isOnWood)
        {
            selectedArray = isRunning ? woodRunClips : woodWalkClips;
        }
        else
        {
            selectedArray = isRunning ? grassRunClips : grassWalkClips;
        }

        if (selectedArray == null || selectedArray.Length == 0) return;

        AudioClip clipToPlay = selectedArray[Random.Range(0, selectedArray.Length)];
        if (clipToPlay == null) return;

        footstepAudioSource.pitch = Random.Range(minPitch, maxPitch);
        footstepAudioSource.PlayOneShot(clipToPlay);
    }
}