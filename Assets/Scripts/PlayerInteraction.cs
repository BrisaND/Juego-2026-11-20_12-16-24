using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 4.5f;
    [SerializeField] private float detectionRadius = 0.15f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactLayer = ~0;

    [Header("UI de Aviso")]
    [SerializeField] private TextMeshProUGUI promptText;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerCamera == null) return;

        Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * 0.3f;
        Vector3 direction = playerCamera.transform.forward;

        if (Physics.SphereCast(origin, detectionRadius, direction, out RaycastHit hit, interactDistance, interactLayer, QueryTriggerInteraction.Collide))
        {
            Keycard card = hit.collider.GetComponentInParent<Keycard>();
            if (card != null)
            {
                ShowPrompt($"[{interactKey}] Agarrar tarjeta");
                if (Input.GetKeyDown(interactKey))
                {
                    card.Collect();
                }
                return;
            }

            ExitDoor door = hit.collider.GetComponentInParent<ExitDoor>();
            if (door != null)
            {
                ShowPrompt($"[{interactKey}] Abrir puerta");
                if (Input.GetKeyDown(interactKey))
                {
                    door.TryOpenDoor();
                }
                return;
            }

            InteractableMessage inspectable = hit.collider.GetComponentInParent<InteractableMessage>();
            if (inspectable != null)
            {
                ShowPrompt($"[{interactKey}] {inspectable.GetPromptAction()}");
                if (Input.GetKeyDown(interactKey))
                {
                    inspectable.Interact();
                }
                return;
            }

            ReadableNote note = hit.collider.GetComponentInParent<ReadableNote>();
            if (note != null)
            {
                ShowPrompt($"[{interactKey}] {note.GetPromptAction()}");
                if (Input.GetKeyDown(interactKey))
                {
                    if (promptText != null) promptText.gameObject.SetActive(false);
                    note.Read();
                }
                return;
            }
        }

        if (promptText != null && promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    private void ShowPrompt(string text)
    {
        if (promptText == null) return;
        promptText.text = text;
        if (!promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(true);
        }
    }
}