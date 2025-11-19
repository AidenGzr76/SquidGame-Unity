using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems; // <<< --- این خط جدید اضافه شود --- >>>

public class PathChecker : MonoBehaviour
{
    // این متغیرها توسط PatternManager در ابتدای بازی مقداردهی می‌شوند
    public SpriteRenderer targetSprite;
    public Texture2D maskTexture;

    public float coverageThreshold = 5f;
    public float requiredCoveragePercentage = 90f;
    public TextMeshProUGUI coverageText;

    public PatternManager patternManager; // <-- این خط را اضافه کن

    [Header("Joystick Drawing")]
    public Joystick joystick;
    public Transform pen;
    public Transform startPoint;
    public float penSpeed = 2f;

    [HideInInspector] public bool isDrawing = false;

    private List<Vector2> drawnPoints = new List<Vector2>();
    private List<Vector2> edgePoints;
    public UIManager uiManager;

    [Header("Sound Effects")]
    private AudioSource crackingAudioSource;
    public AudioSource brokenCandyAudioSource;
    public AudioClip crackingEffect;
    public AudioClip brokenCandy;

    private float originalRequiredCoverage; // <<< این متغیر جدید اضافه شده است
    private Coroutine errorMarginCoroutine; // <<< این متغیر جدید اضافه شده است
    private BackgroundMusic music;


    // متد Start حالا خیلی ساده‌تر است!
    void Start()
    {
        crackingAudioSource = gameObject.AddComponent<AudioSource>();
        music = FindFirstObjectByType<BackgroundMusic>();

        // PatternManager قبلاً همه چیز را تنظیم کرده است
        // ما فقط باید نقاط لبه را محاسبه کنیم

        // if (isDrawing)
        edgePoints = GetEdgePoints(maskTexture);

        Debug.Log("Edge points calculated: " + edgePoints.Count);

        if (pen && startPoint)
        {
            pen.position = startPoint.position;
        }

        originalRequiredCoverage = requiredCoveragePercentage; // <<< این خط جدید اضافه شده است
        ResetPath(); // برای محاسبه اولیه نقاط لبه
    }

    // تمام کدهای دیگر از Update به بعد بدون تغییر باقی می‌مانند
    // ... (کدهای Update, IsWhitePixel, IsInsideMask و بقیه را اینجا داشته باشید)
    // ... (من برای خلاصه‌سازی، آنها را دوباره اینجا کپی نکرده‌ام)
    // ...
    // ...
    void Update()
    {
        Vector2 input = new Vector2(joystick.Horizontal, joystick.Vertical);
        if (!isDrawing && input.sqrMagnitude > 0.01f)
        {
            isDrawing = true;
            drawnPoints.Clear();
        }
        if (isDrawing)
        {
            pen.position += (Vector3)input.normalized * penSpeed * Time.deltaTime;
            Vector3 worldPoint = pen.position;
            worldPoint.z = 0;
            if (IsWhitePixel(worldPoint))
            {
                if (!crackingAudioSource.isPlaying)
                {
                    crackingAudioSource.clip = crackingEffect;
                    crackingAudioSource.loop = true;
                    crackingAudioSource.Play();
                }
                Vector2 point = WorldToMaskPixel(worldPoint);
                drawnPoints.Add(point);
            }
            else if (!IsInsideMask(worldPoint))
            {
                if (crackingAudioSource.isPlaying)
                {
                    crackingAudioSource.Stop();
                    crackingAudioSource.loop = false;
                }
            }
            else
            {
                if (crackingAudioSource.isPlaying)
                {
                    crackingAudioSource.Stop();
                    crackingAudioSource.loop = false;
                }
                Debug.Log("به مسیر سیاه خوردی!");
                drawnPoints.Clear();
                isDrawing = false;

                if (!brokenCandyAudioSource.isPlaying)
                {
                    brokenCandyAudioSource.PlayOneShot(brokenCandy);
                }

                patternManager.BlackTouchedTriggerLoseState(); // <-- این خط را اضافه کن
                // uiManager.ShowLoseBlackTouchedPanel();
            }
            if (input.sqrMagnitude < 0.01f)
            {
                if (crackingAudioSource.isPlaying)
                {
                    crackingAudioSource.Stop();
                    crackingAudioSource.loop = false;
                }
                isDrawing = false;
                CheckCoverage();
            }
        }
    }

    // <<< این تابع کاملاً جدید است >>>
    /// <summary>
    /// مسیر کشیده شده را پاک می‌کند و نقاط لبه را دوباره محاسبه می‌کند.
    /// </summary>
    public void ResetPath()
    {
        drawnPoints.Clear();
        isDrawing = false;

        // <<< تغییر کلیدی اینجاست! >>>
        // قلم را به موقعیت اولیه برگردان
        if (pen != null && startPoint != null)
        {
            pen.position = startPoint.position;
        }

        if (maskTexture != null)
        {
            edgePoints = GetEdgePoints(maskTexture);
            Debug.Log("PathChecker has been reset. New edge points: " + edgePoints.Count);
        }

        // <<< --- این بخش جدید اضافه شده است --- >>>
        // جوی‌استیک را به زور ریست کن
        if (joystick != null)
        {
            // ۱. مقادیر داخلی‌اش را صفر کن
            // joystick.Horizontal = 0f;
            // joystick.Vertical = 0f;
            
            // ۲. (مهم) بهش بگو که انگشت برداشته شده
            // این کار باعث می‌شه handle (دسته جوی‌استیک) به مرکز برگرده
            joystick.OnPointerUp(new PointerEventData(EventSystem.current));
        }
        // <<< --- پایان بخش جدید --- >>>
    }


    // <<< این دو تابع کاملاً جدید هستند >>>
    /// <summary>
    /// فیچر کاهش درصد خطا را برای مدت زمان مشخصی فعال می‌کند.
    /// </summary>
    public void ActivateErrorMarginBoost(float reductionValue, float duration)
    {
        if (errorMarginCoroutine != null)
        {
            StopCoroutine(errorMarginCoroutine);
        }
        errorMarginCoroutine = StartCoroutine(ErrorMarginRoutine(reductionValue, duration));
    }

    private IEnumerator ErrorMarginRoutine(float reduction, float duration)
    {
        requiredCoveragePercentage = originalRequiredCoverage - reduction;
        Debug.Log($"Error Margin Boost Activated! New required coverage: {requiredCoveragePercentage}%");

        yield return new WaitForSeconds(duration);

        requiredCoveragePercentage = originalRequiredCoverage;
        Debug.Log("Error Margin Boost Finished. Coverage reverted to normal.");
        errorMarginCoroutine = null;
    }

    bool IsWhitePixel(Vector3 worldPoint)
    {
        Vector2 pixel = WorldToMaskPixel(worldPoint);
        if (!IsPixelInsideBounds((int)pixel.x, (int)pixel.y)) return false;
        Color color = maskTexture.GetPixel((int)pixel.x, (int)pixel.y);
        return color.grayscale > 0.8f;
    }
    public bool IsInsideMask(Vector3 worldPoint)
    {
        Vector2 pixel = WorldToMaskPixel(worldPoint);
        if (!IsPixelInsideBounds((int)pixel.x, (int)pixel.y)) return false;
        Color pixelColor = maskTexture.GetPixel((int)pixel.x, (int)pixel.y);
        return pixelColor.a > 0.5f;
    }
    Vector2 WorldToMaskPixel(Vector3 worldPoint)
    {
        Vector3 localPos = targetSprite.transform.InverseTransformPoint(worldPoint);
        float normalizedX = (localPos.x + targetSprite.sprite.bounds.extents.x) / targetSprite.sprite.bounds.size.x;
        float normalizedY = (localPos.y + targetSprite.sprite.bounds.extents.y) / targetSprite.sprite.bounds.size.y;
        int pixelX = Mathf.RoundToInt(normalizedX * maskTexture.width);
        int pixelY = Mathf.RoundToInt(normalizedY * maskTexture.height);
        return new Vector2(pixelX, pixelY);
    }
    bool IsPixelInsideBounds(int x, int y)
    {
        return x >= 0 && x < maskTexture.width && y >= 0 && y < maskTexture.height;
    }
    List<Vector2> GetEdgePoints(Texture2D texture)
    {
        List<Vector2> points = new List<Vector2>();
        for (int x = 1; x < texture.width - 1; x++)
        {
            for (int y = 1; y < texture.height - 1; y++)
            {
                Color c = texture.GetPixel(x, y);
                if (c.grayscale > 0.8f)
                {
                    if (texture.GetPixel(x + 1, y).grayscale < 0.5f ||
                        texture.GetPixel(x - 1, y).grayscale < 0.5f ||
                        texture.GetPixel(x, y + 1).grayscale < 0.5f ||
                        texture.GetPixel(x, y - 1).grayscale < 0.5f)
                    {
                        points.Add(new Vector2(x, y));
                    }
                }
            }
        }
        return points;
    }
    void CheckCoverage()
    {
        int covered = 0;
        foreach (Vector2 edgePoint in edgePoints)
        {
            bool isCovered = drawnPoints.Any(p => Vector2.Distance(p, edgePoint) <= coverageThreshold);
            if (isCovered)
                covered++;
        }
        float coveragePercent = (covered / (float)edgePoints.Count) * 100f * 4f;
        Debug.Log($"Coverage: {coveragePercent:F2}% (Required: {requiredCoveragePercentage}%)");
        drawnPoints.Clear();
        isDrawing = false;
        if (coveragePercent >= requiredCoveragePercentage)
        {
            Debug.Log("موفقیت! مسیر به درستی دنبال شد.");
            isDrawing = false;

            if (music != null) music.StopMusic();

            if (GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
                uiManager.uiAudioSource.PlayOneShot(uiManager.WinEffect);
        
            patternManager.TriggerWinState(); // <-- این خط را اضافه کن
            // uiManager.ShowWinPanel();
        }
        else
        {
            Debug.Log("پوشش کافی نیست.");
            // patternManager.TriggerLoseState(); // <-- این خط را اضافه کن
            coverageText.text = $" {coveragePercent:F2}%";
            // uiManager.ShowLosePanel();
            uiManager.ShowCoveragePercentPanel();
        }
    }



    void OnEnable()
    {
        GameManager.OnFeatureActivated += HandleFeatureActivation;
        GameManager.OnStageRespawn += ResetPath;
    }
    void OnDisable()
    {
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
        GameManager.OnStageRespawn -= ResetPath;
    }
    private void HandleFeatureActivation(FeatureData data)
    {
        if (data.type == FeatureData.FeatureType.ErrorMarginBoostDalgona)
        {
            ActivateErrorMarginBoost(data.value, data.duration);
        }
    }

}
