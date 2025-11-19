using UnityEngine;
using System.Collections; // برای استفاده از Coroutines این خط لازم است

public class PatternManager : MonoBehaviour
{
    [Header("Pattern Data")]
    public CandyPattern[] allCandyPatterns; // آرایه‌ای از تمام الگوهای شیرینی شما

    [Header("Scene References")]
    public PathChecker pathChecker;             // اسکریپت بررسی مسیر
    public UIManager uiManager; // رفرنس به UIManager را اضافه می‌کنیم
    public SpriteRenderer mainCandyRenderer;    // رندرِر شیرینی اصلی در صحنه
    public SpriteRenderer hollowCandyRenderer;  // رندرِر شکل توخالی (برای برد)
    public SpriteRenderer shapeRenderer;        // رندرِر خودِ شکل جدا شده (برای برد)

    private CandyPattern selectedPattern;       // الگوی انتخاب شده برای این مرحله
    private int currentPatternIndex; // <<< این متغیر جدید اضافه شده است

    [Header("Animation Settings")]
    public float loseAnimationDelay = 0.75f; // مدت زمان پخش انیمیشن ترک خوردن
    public float moveDownTime = 1f; // مدت زمان پایین رفتن قالب
    public Vector3 hollowShapeEndPosition = new Vector3(0, -10, 0); // موقعیت نهایی قالب در پایین صفحه
    public float winUIDelay = 0.25f; // وقفه کوتاه بعد از انیمیشن برد

    void Awake()
    {
        // <<< این تابع برای انتخاب شکل اولیه بازنویسی شده است >>>
        SelectRandomPattern();
        SetupLevel();
    }

    // <<< این دو تابع کاملاً جدید هستند >>>
    private void SelectRandomPattern()
    {
        currentPatternIndex = Random.Range(0, allCandyPatterns.Length);
        selectedPattern = allCandyPatterns[currentPatternIndex];
    }
    
    /// <summary>
    /// یک شکل جدید و متفاوت را به صورت تصادفی انتخاب و جایگزین می‌کند.
    /// </summary>
    public void RerollPattern()
    {
        if (allCandyPatterns.Length <= 1) return; // اگر فقط یک شکل وجود دارد، کاری نکن

        int newIndex;
        // آنقدر یک ایندکس جدید انتخاب کن تا با ایندکس فعلی متفاوت باشد
        do
        {
            newIndex = Random.Range(0, allCandyPatterns.Length);
        } while (newIndex == currentPatternIndex);

        currentPatternIndex = newIndex;
        selectedPattern = allCandyPatterns[currentPatternIndex];

        // مرحله را با شکل جدید دوباره تنظیم کن
        SetupLevel();
        
        // PathChecker را هم ریست کن تا محاسباتش را از نو انجام دهد
        if(pathChecker != null)
        {
            pathChecker.ResetPath();
        }
        
        Debug.Log($"Pattern rerolled! New shape: {selectedPattern.patternName}");
    }

    void SetupLevel()
    {
        // 1. نمایش شکل اصلی و تنظیم ماسک
        mainCandyRenderer.gameObject.SetActive(true);
        mainCandyRenderer.sprite = selectedPattern.fullSprite;
        pathChecker.maskTexture = selectedPattern.maskTexture;
        pathChecker.targetSprite = mainCandyRenderer; // مطمئن شویم PathChecker رفرنس درست را دارد

        // 2. مخفی کردن آبجکت‌های مربوط به برد و باخت در ابتدای کار
        hollowCandyRenderer.gameObject.SetActive(false);
        shapeRenderer.gameObject.SetActive(false);
    }

    // این تابع‌ها جایگزین ShowWinState و ShowLoseState می‌شوند
    public void TriggerWinState()
    {
        StartCoroutine(WinAnimationRoutine());
    }

    public void TriggerLoseState()
    {
        StartCoroutine(LoseAnimationRoutine());
    }

    public void BlackTouchedTriggerLoseState()
    {
        StartCoroutine(BlackTouchedLoseAnimationRoutine());
    }

    private IEnumerator WinAnimationRoutine()
    {
        // 1. آماده‌سازی صحنه برای انیمیشن برد
        hollowCandyRenderer.sprite = selectedPattern.hollowSprite;
        shapeRenderer.sprite = selectedPattern.shapeSprite;
        hollowCandyRenderer.gameObject.SetActive(true);
        shapeRenderer.gameObject.SetActive(true);
        mainCandyRenderer.gameObject.SetActive(false);

        yield return new WaitForSeconds(winUIDelay);

        // 2. انیمیشن پایین رفتن قالب خالی
        LeanTween.move(hollowCandyRenderer.gameObject, hollowShapeEndPosition, moveDownTime)
            .setEase(LeanTweenType.easeInQuad);

        // 3. منتظر بمان تا انیمیشن حرکت تمام شود
        yield return new WaitForSeconds(moveDownTime);

        // 4. یک وقفه کوتاه دیگر
        yield return new WaitForSeconds(winUIDelay);

        // 5. حالا بر اساس حالت بازی، تصمیم بگیر
        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            // اگر تمرینی بود، فقط پنل برد رو نشون بده
            uiManager.ShowWinPanel();
        }
        else // --- اینجا یعنی حالت MainFlow (بازی اصلی) است ---
        {
            // --- <<< شروع تغییرات اصلی >>> ---

            // قانون ۱: حداقل ۱۰ نفر باید برای مرحله ۳ باقی بمانند
            const int MIN_PLAYERS_TO_REMAIN = 10;
            int currentPlayers = GameManager.Instance.remainingPlayers;

            // حداکثر تعدادی که "اجازه" داریم حذف کنیم تا قانون ۱ نقض نشود
            // مثلا اگر ۱۲ نفر داریم، حداکثر ۲ نفر (12 - 10) می‌توانند حذف شوند
            int maxAllowedEliminations = currentPlayers - MIN_PLAYERS_TO_REMAIN;

            int actualEliminations;

            if (maxAllowedEliminations <= 0)
            {
                // ما در حال حاضر ۱۰ نفر یا کمتر داریم. پس هیچکس دیگری حذف نمی‌شود.
                actualEliminations = 0;
            }
            else
            {
                // قانون ۲: می‌خواهیم ۱، ۲، ۳ یا ۴ نفر حذف شوند
                int desiredEliminations = Random.Range(1, 5); // (عدد ۱ تا ۴)

                // عدد نهایی، کوچکترین عدد از بین "خواسته ما" (1-4) و "مقدار مجاز" است
                // مثال ۱: ۱۲ نفر داریم -> مجاز ۲ نفر -> خواسته ۳ نفر -> نهایی ۲ نفر
                // مثال ۲: ۱۵ نفر داریم -> مجاز ۵ نفر -> خواسته ۲ نفر -> نهایی ۲ نفر
                actualEliminations = Mathf.Min(desiredEliminations, maxAllowedEliminations);
            }

            Debug.Log($"Stage 2 Complete. Players in: {currentPlayers}. Max allowed to elim: {maxAllowedEliminations}. Actual elim: {actualEliminations}");

            // گزارش برد به گیم منیجر
            GameManager.Instance.StageComplete(actualEliminations);

            // --- <<< پایان تغییرات اصلی >>> ---
        }
    }


    private IEnumerator LoseAnimationRoutine()
    {
        // 1. نمایش اسپرایت ترک خورده
        mainCandyRenderer.sprite = selectedPattern.crackedSprite;

        // (اختیاری) اگر انیمیشن فریم-به-فریم ساختی، خط بالا را کامنت کن و خط زیر را فعال
        // mainCandyAnimator.SetTrigger("Crack");

        // 2. منتظر بمان تا انیمیشن دیده شود
        yield return new WaitForSeconds(loseAnimationDelay);

        // 3. حالا پنل باخت را نمایش بده
        // uiManager.DisplayLosePanel();
        uiManager.ShowLosePanel();
    }

    private IEnumerator BlackTouchedLoseAnimationRoutine()
    {
        // 1. نمایش اسپرایت ترک خورده
        mainCandyRenderer.sprite = selectedPattern.crackedSprite;

        // (اختیاری) اگر انیمیشن فریم-به-فریم ساختی، خط بالا را کامنت کن و خط زیر را فعال
        // mainCandyAnimator.SetTrigger("Crack");

        // 2. منتظر بمان تا انیمیشن دیده شود
        yield return new WaitForSeconds(loseAnimationDelay);

        // 3. حالا پنل باخت را نمایش بده
        // uiManager.DisplayLosePanel();
        uiManager.ShowLoseBlackTouchedPanel();
    }


    void OnEnable()
    {
        GameManager.OnFeatureActivated += HandleFeatureActivation;
        GameManager.OnStageRespawn += ResetCandyState;
    }

    void OnDisable()
    {
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
        GameManager.OnStageRespawn -= ResetCandyState;
    }

    // <<< این تابع کاملاً جدید است >>>
    public void ResetCandyState()
    {
        // تمام کوروتین‌های انیمیشن را متوقف کن
        StopAllCoroutines();
        // مرحله را با همان الگوی انتخاب شده، از نو بچین
        SetupLevel();
        Debug.Log("PatternManager has been reset.");
    }

    private void HandleFeatureActivation(FeatureData data)
    {
        if (data.type == FeatureData.FeatureType.ChangeDalgonaShape)
        {
            RerollPattern();
        }
    }



}