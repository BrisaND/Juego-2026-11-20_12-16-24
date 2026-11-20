using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    [Header("Mensajes y Sonidos")]
    [SerializeField] private string lockedMessage = "Cerrada. Necesitás las 3 tarjetas de acceso.";
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] private AudioClip unlockSuccessSound;

    private bool isTriggered = false;

    public void TryOpenDoor()
    {
        if (isTriggered || GameManager.Instance == null) return;

        if (GameManager.Instance.HasAllCards())
        {
            isTriggered = true;

            if (unlockSuccessSound != null)
            {
                AudioSource.PlayClipAtPoint(unlockSuccessSound, transform.position);
            }

            GameManager.Instance.WinGame();
        }
        else
        {
            if (lockedSound != null)
            {
                AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            }

            GameManager.Instance.ShowMessage(lockedMessage, 2.5f);
        }
    }
}