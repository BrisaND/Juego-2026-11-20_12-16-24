using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Progreso de Tarjetas")]
    [SerializeField] private int totalCardsNeeded = 3;
    private int currentCards = 0;

    [Header("Interfaz de Usuario (UI)")]
    [SerializeField] private TextMeshProUGUI cardsCounterText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Transición de Victoria (Fundido a Negro)")]
    [SerializeField] private CanvasGroup fadeBlackCanvasGroup;
    [SerializeField] private float fadeToBlackDuration = 2.0f;

    [Header("Audio y Música")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioClip winMusicClip;
    [Range(0f, 1f)]
    [SerializeField] private float winMusicVolume = 0.6f;
    [SerializeField] private AudioClip gameOverSound;

    private Coroutine messageCoroutine;
    private bool isGameEnded = false;

    public bool IsGameEnded => isGameEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isGameEnded = false;

        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (messageText != null) messageText.gameObject.SetActive(false);

        if (fadeBlackCanvasGroup != null)
        {
            fadeBlackCanvasGroup.alpha = 0f;
            fadeBlackCanvasGroup.blocksRaycasts = false;
            fadeBlackCanvasGroup.gameObject.SetActive(false);
        }

        UpdateUI();
    }

    public void AddCard()
    {
        currentCards++;
        UpdateUI();

        if (HasAllCards())
        {
            ShowMessage("Tenés las 3 tarjetas. ¡Corré hacia la puerta de salida!", 3.5f);
        }
        else
        {
            ShowMessage($"Tarjeta recogida ({currentCards}/{totalCardsNeeded})", 2.5f);
        }
    }

    public bool HasAllCards()
    {
        return currentCards >= totalCardsNeeded;
    }

    private void UpdateUI()
    {
        if (cardsCounterText != null)
        {
            cardsCounterText.text = $"Tarjetas: {currentCards} / {totalCardsNeeded}";
        }
    }

    public void ShowMessage(string message, float duration = 2.5f)
    {
        if (messageText == null) return;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        messageCoroutine = StartCoroutine(ShowMessageRoutine(message, duration));
    }

    private IEnumerator ShowMessageRoutine(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        messageText.gameObject.SetActive(false);
    }

    public void WinGame()
    {
        if (isGameEnded) return;
        isGameEnded = true;

        StartCoroutine(WinSequenceRoutine());
    }

    private IEnumerator WinSequenceRoutine()
    {
        // 1. Desactiva controles del jugador y detiene al enemigo para que no ataque durante el fundido
        Character playerMovement = FindAnyObjectByType<Character>();
        if (playerMovement != null) playerMovement.enabled = false;

        PlayerInteraction playerInteraction = FindAnyObjectByType<PlayerInteraction>();
        if (playerInteraction != null) playerInteraction.enabled = false;

        Enemy enemy = FindAnyObjectByType<Enemy>();
        if (enemy != null) enemy.enabled = false;

        if (messageText != null) messageText.gameObject.SetActive(false);

        // 2. Fundido gradual a negro y atenuación de la música de tensión
        float initialMusicVolume = (backgroundMusicSource != null) ? backgroundMusicSource.volume : 0f;

        if (fadeBlackCanvasGroup != null)
        {
            fadeBlackCanvasGroup.gameObject.SetActive(true);
            fadeBlackCanvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < fadeToBlackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeToBlackDuration);

                fadeBlackCanvasGroup.alpha = t;

                if (backgroundMusicSource != null)
                {
                    backgroundMusicSource.volume = Mathf.Lerp(initialMusicVolume, 0f, t);
                }

                yield return null;
            }

            fadeBlackCanvasGroup.alpha = 1f;
        }

        // 3. Reproduce la música de victoria
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.Stop();

            if (winMusicClip != null)
            {
                backgroundMusicSource.clip = winMusicClip;
                backgroundMusicSource.volume = winMusicVolume;
                backgroundMusicSource.loop = false;
                backgroundMusicSource.ignoreListenerPause = true;
                backgroundMusicSource.Play();
            }
        }

        // 4. Muestra el panel de victoria y libera el cursor
        if (winPanel != null) winPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void GameOver()
    {
        if (isGameEnded) return;
        isGameEnded = true;

        if (gameOverSound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(gameOverSound, Camera.main.transform.position);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        EndGameplayControls();
    }

    private void EndGameplayControls()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Stop();
        }

        Character playerMovement = FindAnyObjectByType<Character>();
        if (playerMovement != null) playerMovement.enabled = false;

        PlayerInteraction playerInteraction = FindAnyObjectByType<PlayerInteraction>();
        if (playerInteraction != null) playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu(string menuSceneName = "MainMenu")
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(menuSceneName);
    }
}