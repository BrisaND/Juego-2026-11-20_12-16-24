using UnityEngine;

public class InteractableMessage : MonoBehaviour
{
    [Header("Configuración de Textos")]
    [Tooltip("Acción que aparece junto a la tecla [E] al apuntar al objeto")]
    [SerializeField] private string promptAction;

    [Tooltip("Mensaje que se muestra en pantalla al interactuar")]
    [TextArea]
    [SerializeField] private string messageToShow;

    [SerializeField] private float messageDuration = 2.5f;

    [Header("Sonido")]
    [Tooltip("Sonido de botón trabado, chispa o estática breve al presionar E")]
    [SerializeField] private AudioClip interactSound;

    public string GetPromptAction()
    {
        return promptAction;
    }

    public void Interact()
    {
        if (interactSound != null)
        {
            AudioSource.PlayClipAtPoint(interactSound, transform.position);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowMessage(messageToShow, messageDuration);
        }
    }
}