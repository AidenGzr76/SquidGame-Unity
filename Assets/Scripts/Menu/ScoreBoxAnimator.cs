using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using RTLTMPro;

public class ScoreBoxAnimator : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform scoreBox;
    public RTLTextMeshPro scoreText;
    public Transform canvasTransform;

    [Header("Animation Settings")]
    public float scaleUpDuration = 0.2f;
    public float scaleDownDuration = 0.3f;
    public float numberCountDuration = 1.0f;
    public float scaleMultiplier = 1.15f;

    [Header("Coin Animation")]
    public GameObject coinPrefab;
    public Transform coinTarget;
    public int numberOfCoins = 7;
    public float coinFlyDuration = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip scoreTickSound;
    public AudioClip finalScoreSound;

    // <<< این بخش جدید اضافه شده است >>>
    [Header("Debug & Testing")]
    [Tooltip("برای تست انیمیشن در ادیتور، این تیک را بزنید و بازی را اجرا کنید.")]
    public bool forceAnimationOnStart = false;
    [Tooltip("مقداری که می‌خواهید برای تست اضافه شود.")]
    public int testScoreToAdd = 100;
    // <<< پایان بخش جدید >>>

    private Vector3 initialScale;
    private List<GameObject> coinPool = new List<GameObject>();
    private int totalScore = 0; // <<< این متغیر برگشته تا منطق تست را مدیریت کند

    void Start()
    {
        initialScale = scoreBox.localScale;
        CreateCoinPool();

        // <<< --- منطق "کامل" جایگزین شد --- >>>

        // ۱. امتیاز "نهایی" سکه‌ها رو "فقط" از گیم منیجر بخوان
        // (دیگه از PlayerPrefs["TotalScore"] نمی‌خونیم)
        totalScore = GameManager.Instance.totalCoins; 
        
        // ۲. امتیاز "سکه" اضافه شده را از کلید "CoinsToAdd" بخوان
        // (این کلید "فقط" توسط منوی تبدیل پول ساخته می‌شه)
        int scoreToAdd = PlayerPrefs.GetInt("CoinsToAdd", 0); 

        // ۳. اگر در حالت تست هستیم، از مقادیر تستی استفاده کن
        if (forceAnimationOnStart)
        {
            scoreToAdd = testScoreToAdd;
            // امتیاز اولیه رو از گیم منیجر بخون که باگ نده
            totalScore = GameManager.Instance.totalCoins;
        }

        if (scoreToAdd > 0)
        {
            // ۴. امتیاز اولیه سکه‌ها = امتیاز نهایی - امتیاز اضافه شده
            int initialScore = totalScore - scoreToAdd; 
            
            scoreText.text = initialScore.ToString("N0");
            StartCoroutine(AnimateScoreUpdate(initialScore, totalScore));
            
            // ۵. کلید را پاک کن
            if (!forceAnimationOnStart)
            {
                PlayerPrefs.DeleteKey("CoinsToAdd");
            }
        }
        else
        {
            // اگر سکه‌ای اضافه نشده، فقط مقدار نهایی رو نشون بده
            scoreText.text = totalScore.ToString("N0");
        }
    }

    void CreateCoinPool()
    {
        for (int i = 0; i < numberOfCoins; i++)
        {
            GameObject coin = Instantiate(coinPrefab, canvasTransform);
            coin.SetActive(false);
            coinPool.Add(coin);
        }
    }

    GameObject GetPooledCoin()
    {
        foreach (var coin in coinPool)
        {
            if (!coin.activeInHierarchy)
            {
                return coin;
            }
        }
        GameObject newCoin = Instantiate(coinPrefab, canvasTransform);
        newCoin.SetActive(false);
        coinPool.Add(newCoin);
        return newCoin;
    }

    public IEnumerator AnimateScoreUpdate(int initialScore, int finalScore)
    {
        CoinDisplayUI.IsAnimatingScore = true;
        Debug.Log("Score animation started, silencing CoinDisplayUI.");

        yield return StartCoroutine(ScaleBox(initialScale * scaleMultiplier, scaleUpDuration));

        if (audioSource && scoreTickSound) audioSource.PlayOneShot(scoreTickSound);
        StartCoroutine(AnimateNumber(initialScore, finalScore, numberCountDuration));
        StartCoroutine(AnimateCoins());

        yield return new WaitForSeconds(numberCountDuration);

        if (audioSource && finalScoreSound) audioSource.PlayOneShot(finalScoreSound);

        yield return StartCoroutine(ScaleBox(initialScale, scaleDownDuration));

        CoinDisplayUI.IsAnimatingScore = false;
        Debug.Log("Score animation finished, CoinDisplayUI is now active.");
        
        // <<< اضافه شد: اگر در حالت تست بودیم، امتیاز نهایی را ذخیره کن >>>
        if (forceAnimationOnStart)
        {
            PlayerPrefs.SetInt("TotalScore", finalScore);
            GameManager.Instance.totalCoins = finalScore; // آپدیت حافظه GameManager
            PlayerPrefs.Save();
        }
    }

    private IEnumerator ScaleBox(Vector3 targetScale, float duration)
    {
        Vector3 startScale = scoreBox.localScale;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            scoreBox.localScale = Vector3.Lerp(startScale, targetScale, timer / duration);
            yield return null;
        }
        scoreBox.localScale = targetScale;
    }

    private IEnumerator AnimateNumber(int from, int to, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            int currentDisplay = (int)Mathf.Lerp(from, to, progress);
            scoreText.text = currentDisplay.ToString("N0");
            yield return null;
        }
        scoreText.text = to.ToString("N0");
    }

    private IEnumerator AnimateCoins()
    {
        for (int i = 0; i < numberOfCoins; i++)
        {
            GameObject coin = GetPooledCoin();
            if (coin != null)
            {
                Vector2 screenPoint = new Vector2(Random.Range(-0.2f, 1.2f), Random.Range(-0.2f, -0.1f));
                if (Random.value > 0.5f) screenPoint.x = Random.value > 0.5f ? -0.1f : 1.1f;

                Vector3 startPos = Camera.main.ViewportToScreenPoint(screenPoint);
                coin.SetActive(true);

                CoinAnimator animator = coin.GetComponent<CoinAnimator>();
                if (animator != null)
                {
                    StartCoroutine(animator.MoveToTarget(startPos, coinTarget.position, coinFlyDuration));
                }
                else
                {
                    Debug.LogError("Coin Prefab is missing CoinAnimator script!", coin);
                }

                yield return new WaitForSeconds(0.05f);
            }
        }
    }


    /// <summary>
    /// این تابع توسط CurrencyExchangeManager صدا زده می‌شود
    /// تا انیمیشن سکه را "دوباره" اجرا کند.
    /// </summary>
    public void TriggerCoinAnimation()
    {
        // ۱. امتیاز نهایی سکه‌ها رو از گیم منیجر بخوان
        totalScore = GameManager.Instance.totalCoins;

        // ۲. امتیاز اضافه شده را از "CoinsToAdd" بخوان
        int scoreToAdd = PlayerPrefs.GetInt("CoinsToAdd", 0);

        if (scoreToAdd > 0)
        {
            // ۳. امتیاز اولیه = نهایی - اضافه شده
            int initialScore = totalScore - scoreToAdd;

            scoreText.text = initialScore.ToString("N0");
            StartCoroutine(AnimateScoreUpdate(initialScore, totalScore));

            PlayerPrefs.DeleteKey("CoinsToAdd");
        }
        else
        {
            // اگر چیزی نبود، فقط متن را آپدیت کن
            scoreText.text = totalScore.ToString("N0");
        }
    }
    
}
