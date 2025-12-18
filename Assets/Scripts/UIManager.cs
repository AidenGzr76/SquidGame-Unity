// using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public GameObject winPanel, losePanel, settingsPanel, tutorialPanel, BlackTouchedPanel, TimeEndPanel;

    // <<< --- این بخش جدید اضافه شده است --- >>>
    [Header("Exit Confirmation")]
    [Tooltip("پنل اصلی تأیید خروج")]
    public GameObject exitConfirmationPanel;
    [Tooltip("دکمه تأیید خروج (بله، خارج شو) در پنل تأیید")]
    public Button confirmExitButton; // لازم نیست پابلیک باشه ولی برای راحتی میذاریم
    [Tooltip("دکمه انصراف (نه، ادامه بده) در پنل تأیید")]
    public Button cancelExitButton; // اسمش رو عوض کردم که با ResumeGame قاطی نشه
    // <<< --- پایان بخش جدید --- >>>

    public string PlayerPrefsKey = "TutorialSeen_Lvl3";
    // public string ExitMenu = "Practice Menu";
    [Header("Scene Names")] // بهتره اسم صحنه‌ها اینجا باشن
    public string mainMenuSceneName = "MainMenu"; // <<-- اسم منوی اصلی رو اینجا بذار
    public GameObject joystickCanvas;

    public AudioSource uiAudioSource;
    public AudioClip WinEffect;
    public AudioClip LoseEffect;

    private BackgroundMusic music;

    // <<< این خط جدید اضافه شده است >>>
    [Header("System Connections")]
    [Tooltip("آبجکتی که اسکریپت ReviveManager روی آن است را اینجا بکشید")]
    public ReviveManager reviveManager;

    [Header("Stage 6 Specific")] // یک هدر جدید برای مرتب‌سازی
    public Button spinAttackButton; // دکمه هل دادن رو اینجا بکش
    private PlayerSquidController playerControllerInstance; // برای نگه داشتن ارجاع به بازیکن

    // ... (توابع Start, Update, و توابع برد بدون تغییر) ...
    #region Unchanged Code

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // اگر لازم داری بین صحنه‌ها باقی بمونه:
            // DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        Debug.LogWarning(PlayerPrefsKey + " value: " + PlayerPrefs.GetInt(PlayerPrefsKey, 0));
        MenuMusic.StopMusic();
        // if (PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 0)
        // {
        //     tutorialPanel.SetActive(true);
        //     PauseGame();
        // }
        // else
        // {
        //     ResumeGame();
        //     tutorialPanel.SetActive(false);
        // }

        music = FindFirstObjectByType<BackgroundMusic>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Practice Menu");
        }
    }

    public void ShowWinPanel()
    {
        if (joystickCanvas != null)
            joystickCanvas.SetActive(false);

        if (music != null) music.StopMusic();
    
        uiAudioSource.PlayOneShot(WinEffect);
        winPanel.SetActive(true);
        PauseGame();
    }

    public void ReportWinToGameManager(int eliminatedCount)
    {
        if (GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
        {
            // <<< --- این خط جدید اضافه شده است --- >>>
            if (music != null) music.StopMusic();
            // <<< --- پایان بخش جدید --- >>>
        
            uiAudioSource.PlayOneShot(WinEffect);
            GameManager.Instance.StageComplete(eliminatedCount);
        }
    }
    #endregion

    // --- این توابع باخت به طور کامل بازنویسی شده‌اند ---

    public void ShowLosePanel()
    {
        if (music != null) music.PauseMusic();
        
        uiAudioSource.PlayOneShot(LoseEffect);


        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            if (joystickCanvas != null)
                joystickCanvas.SetActive(false);
            
            losePanel.SetActive(true);
            PauseGame();
        }
        else // حالت اصلی (MainFlow)
        {
            // <<< تغییر کلیدی: به جای تمام کردن بازی، منوی بازگشت را نشان بده >>>
            if (reviveManager != null)
            {
                reviveManager.ShowRevivePanel();
            }
            else
            {
                // اگر به هر دلیلی ReviveManager پیدا نشد، بازی را تمام کن
                GameManager.Instance.StageFailed();
            }
        }
    }

    public void ShowCoveragePercentPanel()
    {
        if (music != null) music.PauseMusic();
        uiAudioSource.PlayOneShot(LoseEffect);

        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            BlackTouchedPanel.SetActive(true);
            PauseGame();
        }
        else
        {
            // <<< تغییر کلیدی >>>
            if (reviveManager != null) losePanel.SetActive(true); //reviveManager.ShowRevivePanel();
            else GameManager.Instance.StageFailed();

            PauseGame();
        }
    }

    public void ShowLoseBlackTouchedPanel()
    {
        if (music != null) music.PauseMusic();
        uiAudioSource.PlayOneShot(LoseEffect);

        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            BlackTouchedPanel.SetActive(true);
            PauseGame();
        }
        else
        {
            // <<< تغییر کلیدی >>>
            if (reviveManager != null) BlackTouchedPanel.SetActive(true); //reviveManager.ShowRevivePanel();
            else GameManager.Instance.StageFailed();

            PauseGame();
        }
    }

    public void TimeEnd()
    {
        if (music != null) music.PauseMusic(); // ← موزیک قطع شود
        uiAudioSource.PlayOneShot(LoseEffect);
        
        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            TimeEndPanel.SetActive(true);
            PauseGame();
        }
        else
        {
            // <<< تغییر کلیدی >>>
            if (reviveManager != null) reviveManager.ShowRevivePanel();
            else GameManager.Instance.StageFailed();
        }
    }




    // در UIManager.cs (یا مدیر مرحله ۶)

    /// <summary>
    /// این تابع توسط PlayerSquidController صدا زده می‌شود تا خودش را معرفی کند.
    /// </summary>
    public void RegisterPlayer(PlayerSquidController player)
    {
        playerControllerInstance = player;
        Debug.Log("Player registered with UIManager.");

        // حالا که بازیکن را داریم، دکمه را به تابع SpinAttack وصل می‌کنیم
        if (spinAttackButton != null && playerControllerInstance != null)
        {
            // اول Listener های قبلی رو پاک می‌کنیم (محض احتیاط)
            spinAttackButton.onClick.RemoveAllListeners();
            // حالا Listener جدید رو اضافه می‌کنیم
            spinAttackButton.onClick.AddListener(playerControllerInstance.SpinAttack);
            Debug.Log("Spin Attack button listener connected!");
        }
        else
        {
            if (spinAttackButton == null) Debug.LogError("Spin Attack Button به UIManager وصل نشده!");
            if (playerControllerInstance == null) Debug.LogError("Player instance در UIManager نرسیده!");
        }
    }






    // ... (بقیه توابع شما مثل CloseTutorial, PauseGame, RestartGame و... بدون تغییر) ...
    public void ShowSettings()
    {
        settingsPanel.SetActive(true);
        PauseGame();
    }

    public TutorialManager tutorialManager;
    // public void ShowTutorial()
    // {
    //     // PauseGame();
    //     tutorialManager.gameObject.SetActive(true);
    // }
    // <<< تغییر: تابع نمایش توتوریال (مثلاً از دکمه راهنما در تنظیمات) >>>

    public void ShowTutorial()
    {
        // بستن پنل تنظیمات اگر باز است
        if (settingsPanel != null && settingsPanel.activeSelf) 
            settingsPanel.SetActive(false);
            
        // استفاده از متغیری که در اینسپکتور پر می‌کنی (بدون نیاز به instance)
        if (tutorialManager != null)
        {
            tutorialManager.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("TutorialManager Script is NOT assigned in UIManager Inspector!");
        }
    }

    public void CloseTutorial()
    {
        PlayerPrefs.SetInt(PlayerPrefsKey, 1);
        PlayerPrefs.Save();
        tutorialPanel.SetActive(false);
        ResumeGame();
    }

    public void CloseSetting()
    {
        settingsPanel.SetActive(false);
        ResumeGame();
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
        if (joystickCanvas != null)
            joystickCanvas.SetActive(false);
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        if (joystickCanvas != null)
            joystickCanvas.SetActive(true);
    }

    public void RestartGame()
    {
        PauseGame();
        // BackgroundMusic.PlayMusic();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitGame()
    {
        ResumeGame();
        SceneManager.LoadScene(mainMenuSceneName);
    }



    // <<< --- این سه تابع کامل جدید هستند --- >>>

    /// <summary>
    /// این تابع باید توسط دکمه خروج "اولیه" در UI اصلی بازی صدا زده شود.
    /// پنل تأیید را نشان می‌دهد و بازی را متوقف می‌کند.
    /// </summary>
    public void ShowExitConfirmation()
    {
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(true);
            PauseGame(); // از تابع موجود PauseGame استفاده می‌کنیم
            Debug.Log("Exit confirmation panel shown. Game paused.");
        }
        else
        {
            Debug.LogError("Exit Confirmation Panel به UIManager وصل نشده!");
        }
    }

    /// <summary>
    /// این تابع توسط دکمه "بله، خارج شو" در پنل تأیید صدا زده می‌شود.
    /// بازی را از حالت توقف خارج کرده و با اعلام باخت به منو برمی‌گردد.
    /// </summary>
    public void ConfirmExit()
    {
        Debug.Log("Confirm Exit clicked.");
        ResumeGame(); // اول بازی رو از حالت Pause در میاریم (Time.timeScale = 1)

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
            {
                // در حالت اصلی، StageFailed رو صدا می‌زنیم تا امتیازات صفر بشه
                GameManager.Instance.StageFailed(); // این خودش به منو برمی‌گردونه
                Debug.Log("Called GameManager.StageFailed()");
            }
            else // در حالت تمرینی، فقط به منو برمی‌گردیم
            {
                SceneManager.LoadScene(mainMenuSceneName);
                Debug.Log($"Practice mode exit. Loading scene: {mainMenuSceneName}");
            }
        }
        else
        {
            Debug.LogError("GameManager not found! Returning to menu without failing stage.");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    /// <summary>
    /// این تابع توسط دکمه "نه، ادامه بده" در پنل تأیید صدا زده می‌شود.
    /// پنل را مخفی کرده و بازی را ادامه می‌دهد.
    /// </summary>
    public void CancelExit() // اسمش رو عوض کردم
    {
        if (exitConfirmationPanel != null)
        {
            exitConfirmationPanel.SetActive(false);
            ResumeGame(); // از تابع موجود ResumeGame استفاده می‌کنیم
            Debug.Log("Exit cancelled. Resuming game.");
        }
    }
    // <<< --- پایان توابع جدید --- >>>

    public void ResetPath()
    {
        losePanel.SetActive(false);
        BlackTouchedPanel.SetActive(false);
        TimeEndPanel.SetActive(false);
        ResumeGame();
    }

    void OnEnable()
    {
        if (GameManager.Instance.currentStageIndex == 1)
            GameManager.OnStageRespawn += ResetPath;
    }
    void OnDisable()
    {
        if (GameManager.Instance.currentStageIndex == 1)
            GameManager.OnStageRespawn -= ResetPath;
    }


}


