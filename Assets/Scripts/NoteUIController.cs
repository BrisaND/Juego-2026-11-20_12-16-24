using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoteUIController : MonoBehaviour
{
    public static NoteUIController Instance { get; private set; }

    [Header("Elementos del Canvas")]
    [SerializeField] private GameObject notePanel;
    [SerializeField] private TextMeshProUGUI noteContentText;
    [SerializeField] private Button closeButton;

    [Header("Opciones de Jugabilidad")]
    [Tooltip("Pausa el juego (y al enemigo) mientras el jugador lee la nota")]
    [SerializeField] private bool pauseGameWhileReading = true;

    [Header("Sonidos")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip openPaperSound;
    [SerializeField] private AudioClip closePaperSound;

    private Character playerMovement;
    private PlayerInteraction playerInteraction;
    private bool isReading = false;

    public bool IsReading => isReading;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        playerMovement = FindAnyObjectByType<Character>();
        playerInteraction = FindAnyObjectByType<PlayerInteraction>();

        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
        }

        if (notePanel != null)
        {
            notePanel.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseNote);
            closeButton.onClick.AddListener(CloseNote);
        }
    }

    public void OpenNote(string text)
    {
        if (isReading || notePanel == null) return;

        isReading = true;

        if (noteContentText != null)
        {
            noteContentText.text = text;
        }

        notePanel.SetActive(true);
        PlayPaperSound(openPaperSound);

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseGameWhileReading)
        {
            Time.timeScale = 0f;
        }
    }

    public void CloseNote()
    {
        if (!isReading || notePanel == null) return;

        isReading = false;
        notePanel.SetActive(false);

        if (pauseGameWhileReading)
        {
            Time.timeScale = 1f;
        }

        PlayPaperSound(closePaperSound);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerInteraction != null) playerInteraction.enabled = true;
    }

    private void PlayPaperSound(AudioClip clip)
    {
        if (clip == null) return;

        if (uiAudioSource != null)
        {
            uiAudioSource.ignoreListenerPause = true;
            uiAudioSource.PlayOneShot(clip);
        }
        else if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }
}