using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class MarblesGameManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject panelGuessOpponent;
    public GameObject panelChooseMyMarbles;
    public GameObject panelWaiting;

    [Header("UI Elements")]
    public Button btnOdd;
    public Button btnEven;
    public Button btnPlus, btnMinus, btnConfirmMyChoice;
    public TextMeshProUGUI txtChosenMarbles;
    public TextMeshProUGUI waitingText;
    public TextMeshProUGUI txtTimer;
    public TextMeshProUGUI playerMarblesText;
    public TextMeshProUGUI opponentMarblesText;
    public UIManager uiManager;

    [Header("Feature Buttons")]
    [Tooltip("دکمه فیچر 'حذف گزینه' را اینجا بکشید")]
    public Button removeOptionButton;
    [Tooltip("دکمه فیچر 'افزایش زمان' را اینجا بکشید")]
    public Button addTimeButton;

    [Header("Guess Visuals")]
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    private Image oddImage, evenImage;
    private Color defaultColor;

    // Private state variables
    private int playerMarbles = 10;
    private int opponentMarbles = 10;
    private int hiddenOpponentMarbles = 0;
    private int mySelectedMarbles = 1;

    private bool hasGuessed = false;
    private bool isFirstRound = true;
    private Coroutine timerCoroutine = null;
    private float timeLeft; // متغیر جدید برای کنترل دقیق زمان
    private Coroutine waitingAnimationCoroutine = null;
    private string waitingBaseText;

    #region Unity Lifecycle & Event Handling
    void OnEnable()
    {
        // به محض فعال شدن، برای دریافت اعلان فیچرها ثبت‌نام کن
        GameManager.OnFeatureActivated += HandleFeatureActivation;
        
        // <<< --- این خط جدید اضافه شده است --- >>>
        // برای دریافت اعلان "بازگشت به بازی" (Revive) ثبت‌نام کن
        GameManager.OnStageRespawn += HandleRespawn;
    }

    void OnDisable()
    {
        // وقتی غیرفعال می‌شوی، ثبت‌نام را لغو کن
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
        
        // <<< --- این خط جدید اضافه شده است --- >>>
        // لغو ثبت‌نام اعلان "بازگشت به بازی"
        GameManager.OnStageRespawn -= HandleRespawn;
    }
    #endregion

    void Start()
    {
        oddImage = btnOdd.GetComponent<Image>();
        evenImage = btnEven.GetComponent<Image>();
        defaultColor = oddImage.color;

        btnOdd.onClick.AddListener(() => PlayerGuess(true));
        btnEven.onClick.AddListener(() => PlayerGuess(false));
        btnPlus.onClick.AddListener(IncreaseBet);
        btnMinus.onClick.AddListener(DecreaseBet);
        btnConfirmMyChoice.onClick.AddListener(ConfirmBet);

        waitingBaseText = waitingText.text;
        UpdateMarbleUI(true);
        StartOpponentTurn();
    }

    #region Feature Activation Logic
    /// <summary>
    /// این تابع به رویداد سراسری GameManager گوش می‌دهد و فیچر مربوطه را فعال می‌کند.
    /// </summary>
    private void HandleFeatureActivation(FeatureData data)
    {
        switch (data.type)
        {
            case FeatureData.FeatureType.Marbles_AddTime:
                AddTime(data.value);
                break;
            case FeatureData.FeatureType.Marbles_RemoveOption:
                RemoveWrongOption();
                break;
        }
    }

    /// <summary>
    /// فیچر افزایش زمان را فعال می‌کند.
    /// </summary>
    public void AddTime(float timeToAdd)
    {
        // فقط اگر تایمر در حال اجراست، زمان را اضافه کن
        if (timerCoroutine != null)
        {
            timeLeft += timeToAdd;
            Debug.Log($"{timeToAdd} seconds added to the timer!");
        }
    }

    /// <summary>
    /// فیچر حذف یک گزینه اشتباه را در مرحله حدس زدن فعال می‌کند.
    /// </summary>
    public void RemoveWrongOption()
    {
        // آیا در مرحله درست هستیم و هنوز حدس نزده‌ایم؟
        if (panelGuessOpponent.activeSelf && !hasGuessed)
        {
            bool isActuallyOdd = hiddenOpponentMarbles % 2 != 0;

            // گزینه اشتباه را پیدا و غیرفعال کن
            if (isActuallyOdd)
            {
                btnEven.interactable = false;
                evenImage.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0.3f);
            }
            else
            {
                btnOdd.interactable = false;
                oddImage.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0.3f);
            }
            Debug.Log("Remove Option feature activated!");

            // چون فیچر استفاده شده، دکمه‌اش را غیرفعال کن تا دوباره استفاده نشود
            if (removeOptionButton != null) removeOptionButton.interactable = false;
        }
        else
        {
            Debug.Log("Cannot use Remove Option feature in the current phase.");
        }
    }
    #endregion
    
    // <<< --- این تابع کامل جدید است --- >>>
    /// <summary>
    /// این تابع زمانی اجرا می‌شود که بازیکن دکمه "Revive" را فشار دهد.
    /// </summary>
    private void HandleRespawn()
    {
        Debug.Log("HandleRespawn called in MarblesGameManager!");

        // ۱. ریست کردن تعداد تیله‌ها طبق درخواست شما
        playerMarbles = 5;
        opponentMarbles = 15;

        // ۲. آپدیت کردن فوری UI
        UpdateMarbleUI(true);

        // ۳. ریست کردن پنل‌های UI (مخفی کردن پنل انتظار و ...)
        // و شروع مجدد بازی از نوبت حریف (مرحله حدس زوج یا فرد)
        StartOpponentTurn();
    }
    // <<< --- پایان تابع جدید --- >>>

    #region Game Phase Management
    void StartOpponentTurn()
    {
        if (IsGameOver()) return;
        UpdateFeatureButtonsState(true, true); // فعال کردن هر دو دکمه فیچر

        panelGuessOpponent.SetActive(true);
        panelChooseMyMarbles.SetActive(false);
        panelWaiting.SetActive(false);

        if (!isFirstRound) MarblesSoundManager.Instance.PlaySound(MarblesSoundManager.Instance.guessStartGongSound);
        isFirstRound = false;

        hiddenOpponentMarbles = Random.Range(1, Mathf.Min(6, opponentMarbles, playerMarbles) + 1);
        hasGuessed = false;
        ResetGuessPanel();
        StartNewTimer(10f, () => uiManager.ShowLosePanel());
    }

    void GoToPlayerChoicePhase()
    {
        if (IsGameOver()) return;
        UpdateFeatureButtonsState(false, true); // غیرفعال کردن "حذف گزینه"، فعال نگه داشتن "زمان"

        panelGuessOpponent.SetActive(false);
        panelChooseMyMarbles.SetActive(true);
        panelWaiting.SetActive(false);
        
        btnPlus.interactable = true;
        btnMinus.interactable = true;
        
        MarblesSoundManager.Instance.PlaySound(MarblesSoundManager.Instance.turnChangeChimeSound);
        ResetPlayerMarbleChoice();
        StartNewTimer(10f, () => uiManager.ShowLosePanel());
    }
    
    void GoToWaitingPhase()
    {
        UpdateFeatureButtonsState(false, false); // غیرفعال کردن همه فیچرها

        panelChooseMyMarbles.SetActive(false);
        panelWaiting.SetActive(true);
        panelGuessOpponent.SetActive(false);
        
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
        txtTimer.text = "00:00";
        
        waitingAnimationCoroutine = StartCoroutine(WaitingTextAnimationRoutine());
        MarblesSoundManager.Instance.PlaySound(MarblesSoundManager.Instance.tensionTickTockSound);
        StartCoroutine(OpponentGuessCoroutine());
    }
    #endregion

    #region Main Gameplay Methods
    void PlayerGuess(bool guessOdd)
    {
        if (hasGuessed) return;
        hasGuessed = true;

        btnOdd.interactable = false;
        btnEven.interactable = false;

        Image chosenImage = guessOdd ? oddImage : evenImage;
        Image otherImage = guessOdd ? evenImage : oddImage;
        
        bool isOdd = hiddenOpponentMarbles % 2 != 0;
        bool correctGuess = (guessOdd == isOdd);

        if (correctGuess)
        {
            MarblesSoundManager.Instance.PlaySound(MarblesSoundManager.Instance.guessCorrectSound);
            playerMarbles += hiddenOpponentMarbles;
            opponentMarbles -= hiddenOpponentMarbles;
            chosenImage.color = correctColor;
        }
        else
        {
            MarblesSoundManager.Instance.PlaySound(MarblesSoundManager.Instance.guessWrongSound);
            playerMarbles -= hiddenOpponentMarbles;
            opponentMarbles += hiddenOpponentMarbles;
            chosenImage.color = wrongColor;
        }

        otherImage.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0.5f);
        UpdateMarbleUI(false, GoToPlayerChoicePhase);
    }

    IEnumerator OpponentGuessCoroutine()
    {
        yield return new WaitForSeconds(Random.Range(2f, 4f));
        
        if (waitingAnimationCoroutine != null) StopCoroutine(waitingAnimationCoroutine);
        waitingText.text = waitingBaseText;
        
        bool opponentGuessIsOdd = Random.value > 0.5f;
        bool isActuallyOdd = (mySelectedMarbles % 2 == 1);

        if (opponentGuessIsOdd == isActuallyOdd)
        {
            int transfer = Mathf.Min(mySelectedMarbles, playerMarbles);
            playerMarbles -= transfer;
            opponentMarbles += transfer;
        }
        else
        {
            int transfer = Mathf.Min(mySelectedMarbles, opponentMarbles);
            playerMarbles += transfer;
            opponentMarbles -= transfer;
        }

        UpdateMarbleUI(false, StartOpponentTurn);
    }
    #endregion

    #region Helper & UI Methods
    void UpdateFeatureButtonsState(bool canRemoveOption, bool canAddTime)
    {
        if (removeOptionButton != null) removeOptionButton.interactable = canRemoveOption;
        if (addTimeButton != null) addTimeButton.interactable = canAddTime;
    }

    void IncreaseBet()
    {
        if (mySelectedMarbles < playerMarbles)
        {
            mySelectedMarbles++;
            UpdateMarbleChoiceText();
        }
    }

    void DecreaseBet()
    {
        if (mySelectedMarbles > 1)
        {
            mySelectedMarbles--;
            UpdateMarbleChoiceText();
        }
    }
    
    void ConfirmBet()
    {
        btnConfirmMyChoice.interactable = false;
        GoToWaitingPhase();
    }

    IEnumerator WaitingTextAnimationRoutine()
    {
        int dotCount = 0;
        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            waitingText.text = waitingBaseText + new string('.', dotCount);
            yield return new WaitForSeconds(0.5f);
        }
    }

    void UpdateMarbleUI(bool immediate = false, System.Action onComplete = null)
    {
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }

        if (immediate)
        {
            playerMarblesText.text = playerMarbles.ToString();
            opponentMarblesText.text = opponentMarbles.ToString();
            onComplete?.Invoke();
        }
        else
        {
            int oldPlayer = int.Parse(playerMarblesText.text);
            int oldOpponent = int.Parse(opponentMarblesText.text);
            float finalDelay = 1.5f; 
            // فرض می‌کنیم MarblesAnimationManager وجود دارد
            // MarblesAnimationManager.Instance.AnimateScoreChange(...);
            
            // جایگزین ساده‌تر برای انیمیشن امتیاز:
            StartCoroutine(AnimateScoreRoutine(oldPlayer, playerMarbles, oldOpponent, opponentMarbles, finalDelay, onComplete));
        }
    }

    // یک کوروتین ساده برای انیمیشن امتیاز اگر MarblesAnimationManager پیچیده است
    IEnumerator AnimateScoreRoutine(int oldP, int newP, int oldO, int newO, float duration, System.Action onComplete)
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            playerMarblesText.text = Mathf.RoundToInt(Mathf.Lerp(oldP, newP, progress)).ToString();
            opponentMarblesText.text = Mathf.RoundToInt(Mathf.Lerp(oldO, newO, progress)).ToString();
            yield return null;
        }
        playerMarblesText.text = newP.ToString();
        opponentMarblesText.text = newO.ToString();
        onComplete?.Invoke();
    }
    
    void ResetGuessPanel()
    {
        btnOdd.interactable = true;
        btnEven.interactable = true;
        oddImage.color = defaultColor;
        evenImage.color = defaultColor;
    }

    void UpdateMarbleChoiceText() { txtChosenMarbles.text = mySelectedMarbles.ToString(); }
    void ResetPlayerMarbleChoice() { mySelectedMarbles = 1; UpdateMarbleChoiceText(); btnConfirmMyChoice.interactable = true; }

    IEnumerator StartTimer(float duration, System.Action onTimeOut) 
    { 
        timeLeft = duration; 
        while (timeLeft > 0f) 
        { 
            timeLeft -= Time.deltaTime; 
            txtTimer.text = "00:" + Mathf.CeilToInt(timeLeft).ToString("00"); 
            yield return null; 
        } 
        timerCoroutine = null;
        onTimeOut?.Invoke(); 
    }
    
    void StartNewTimer(float duration, System.Action onTimeOut) 
    { 
        if (timerCoroutine != null) StopCoroutine(timerCoroutine); 
        timerCoroutine = StartCoroutine(StartTimer(duration, onTimeOut)); 
    }

    bool IsGameOver()
    {
        if (playerMarbles <= 0 || opponentMarbles <= 0)
        {
            EndGame();
            return true;
        }
        return false;
    }

    void EndGame()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        UpdateFeatureButtonsState(false, false);
        panelGuessOpponent.SetActive(false);
        panelChooseMyMarbles.SetActive(false);
        panelWaiting.SetActive(false);

        // --- <<< شروع تغییرات اصلی >>> ---

        if (opponentMarbles <= 0) // حالت برد بازیکن
        {
            uiManager.uiAudioSource.PlayOneShot(uiManager.WinEffect);
            
            if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
            {
                // در حالت تمرینی، فقط پنل برد رو نشون بده
                uiManager.ShowWinPanel();
            }
            else // در حالت بازی اصلی (MainFlow)
            {
                // ۱. تعداد کل بازیکنان فعلی رو از گیم منیجر می‌گیریم
                int totalPlayers = GameManager.Instance.remainingPlayers;

                // ۲. محاسبه می‌کنیم چند نفر باید حذف بشن
                // جادوی این خط در "تقسیم عدد صحیح" (Integer Division) هست:
                // اگر 10 بازیکن باشند: 10 / 2 = 5 نفر حذف می‌شوند
                // اگر 11 بازیکن باشند: 11 / 2 = 5 نفر حذف می‌شوند (و 1 نفر امن می‌مونه)
                int eliminatedCount = totalPlayers / 2;

                Debug.Log($"Stage 4 Won. Players in: {totalPlayers}. Calculated eliminations: {eliminatedCount}");

                // ۳. تعداد حذف شده‌ها رو به گیم منیجر گزارش می‌دیم
                GameManager.Instance.StageComplete(eliminatedCount);
            }
        }
        else if (playerMarbles <= 0) // حالت باخت بازیکن
        {
            // این بخش درسته. UIManager خودش حالت بازی رو چک می‌کنه
            // و اگه MainFlow باشه، منوی Revive رو میاره.
            uiManager.ShowLosePanel();
        }
        // --- <<< پایان تغییرات اصلی >>> ---
    }
    
    #endregion
}