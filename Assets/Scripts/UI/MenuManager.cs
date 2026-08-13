using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverMenuPanel;

    [Header("Main Menu")]
    public Button startButton;
    public Button quitButton;

    [Header("Pause Menu")]
    public Button resumeButton;
    public Button restartButton;
    public Button pauseQuitButton;

    [Header("Game Over Menu")]
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI gameOverWaveText;
    public Button retryButton;
    public Button mainMenuButton;
    public Button gameOverQuitButton;

    [Header("Settings")]
    public Slider volumeSlider;

    private bool isPaused = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (pauseQuitButton != null) pauseQuitButton.onClick.AddListener(QuitToMainMenu);
        if (retryButton != null) retryButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(QuitToMainMenu);
        if (gameOverQuitButton != null) gameOverQuitButton.onClick.AddListener(QuitGame);

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        Time.timeScale = 0f;
        ShowMainMenu();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = true;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void StartGame()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.OnGameStart();
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void PauseGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (gameOverMenuPanel != null) gameOverMenuPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowGameOver(int score, int wave)
    {
        if (gameOverMenuPanel != null) gameOverMenuPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (gameOverScoreText != null) gameOverScoreText.text = $"SCORE: {score:N0}";
        if (gameOverWaveText != null) gameOverWaveText.text = $"WAVE REACHED: {wave}";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void SetVolume(float value)
    {
        AudioListener.volume = value;
    }
}