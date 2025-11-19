using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PrizeSequenceManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI remainingPlayersText;
    public TextMeshProUGUI prizeMoneyText;

    [Header("Game Objects & Prefabs")]
    public GameObject cashPrefab;
    public Transform spawnArea; // جایی که پول "جدید" از بالا می‌ریزد
    // public Transform prizeBoxArea; // جایی که پول "قبلی" در کف باکس ایجاد می‌شود
    public Transform preSpawnDropZone; // <<< --- این خط جدید --- >>> (جایی که پول "قبلی" از نزدیک می‌ریزد)
    public BoxCollider2D preSpawnCollider; // <<< --- این خط جدید --- >>> (کولایدر محدوده اسپاون پول قبلی)


    [Header("Sequence Settings")]
    public long moneyPerPlayer = 1000;
    public int moneyPerCashBundle = 500;

    [Header("Timing Settings")]
    public float cashSpawnInterval = 0.2f;
    public float preSpawnInterval = 0.1f; // <<< --- این خط جدید --- >>> (برای پول قبلی، سریعتر)
    public float delayAfterAllCashDropped = 4f;
    public float cashSettleTime = 1.5f; // زمانی که به پول‌های قبلی فرصت ته‌نشین شدن می‌دهیم

    [Header("Object Pool Settings")]
    public int poolSize = 100;

    // متغیرهای داخلی
    private List<GameObject> cashPool;
    private int cashDroppedCount = 0;
    private int currentPlayersDisplay;
    private long currentPrizeDisplay;
    private int eliminatedPlayers; 
    private int startPlayers;

    void Awake()
    {
        eliminatedPlayers = PlayerPrefs.GetInt("EliminatedInLastStage", 0);
        startPlayers = PlayerPrefs.GetInt("StartPlayersThisRound", 0);

        int stageIndex = PlayerPrefs.GetInt("CurrentStageIndex", 0);

        if (stageIndex == 0)
        {
            // اگر بعد از مرحله ۱ هستیم، پول از صفر شروع شود
            currentPrizeDisplay = 0;
        }
        else
        {
            // در مراحل بعدی، پول قبلی را بخوان
            currentPrizeDisplay = long.Parse(PlayerPrefs.GetString("PreviousRunPrize", "0"));
        }
        
        Debug.LogWarning($"[PrizeScene] Loaded: Eliminated={eliminatedPlayers}, StartPlayers={startPlayers}, StartingPrize={currentPrizeDisplay}");
    }

    // <<< --- تابع Start به این شکل اصلاح شد --- >>>
    void Start()
    {
        // فقط این یک کوروتین را صدا بزن.
        // این تابع همه‌چیز را به ترتیب مدیریت می‌کند.
        StartCoroutine(InitializeAndRunSequence());
    }
    // <<< --- پایان اصلاح Start --- >>>

    /// <summary>
    /// این کوروتین اصلی است که همه‌چیز را به ترتیب مدیریت می‌کند
    /// </summary>
    private IEnumerator InitializeAndRunSequence()
    {
        CreateCashPool();
        InitializeDisplays();

        // ۱. اول اسکناس‌های قبلی را در باکس ایجاد کن
        yield return StartCoroutine(PreSpawnCashRoutine());
        
        // ۲. حالا که اسکناس‌های قبلی ته‌نشین شدند، انیمیشن اصلی را شروع کن
        //    (این تابع MainSequence قبلی است که حالا اینجا صدا زده می‌شود)
        yield return StartCoroutine(MainSequence());
    }

    /// <summary>
    /// اسکناس‌های قبلی را به تدریج از نقاط رندوم داخل کولایدر preSpawnCollider می‌ریزد
    /// و صبر می‌کند تا ته‌نشین شوند.
    /// </summary>
    private IEnumerator PreSpawnCashRoutine()
    {
        // <<< --- فقط این بخش چک کردن تغییر می‌کند --- >>>
        if (preSpawnCollider == null) // حالا کولایدر رو چک می‌کنیم
        {
            Debug.LogError("PreSpawnCollider در Inspector تنظیم نشده!");
            yield break;
        }
        // <<< --- پایان تغییر --- >>>

        int bundlesToSpawn = (int)(currentPrizeDisplay / moneyPerCashBundle);

        if (bundlesToSpawn == 0)
        {
            yield break;
        }

        Debug.Log($"Dropping {bundlesToSpawn} pre-existing cash bundles from collider volume...");

        // <<< --- مهم‌ترین تغییر اینجاست --- >>>
        // Bounds رو از "کولایدر" می‌گیریم، نه از transform.localScale
        Bounds dropBounds = preSpawnCollider.bounds;
        // <<< --- پایان تغییر --- >>>

        for (int i = 0; i < bundlesToSpawn; i++)
        {
            GameObject cash = GetPooledCash();
            if (cash != null)
            {
                // ۱. یک موقعیت تصادفی "داخل" Bounds کولایدر پیدا کن
                float randomX = Random.Range(dropBounds.min.x, dropBounds.max.x);
                float randomY = Random.Range(dropBounds.min.y, dropBounds.max.y);

                cash.transform.position = new Vector3(randomX, randomY, 0);
                cash.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

                cash.SetActive(true);

                // صبر کردن بعد از هر اسکناس برای جلوگیری از لگ
                yield return new WaitForSeconds(preSpawnInterval);
            }
        }

        // زمان دادن برای ته‌نشین شدن
        Debug.Log("Waiting for pre-spawned cash to settle naturally...");
        yield return new WaitForSeconds(cashSettleTime);
        Debug.Log("Pre-spawned cash settled.");
    }


    private IEnumerator MainSequence()
    {
        // انیمیشن اعداد و ریختن پول "جدید" را شروع کن
        StartCoroutine(SpawnCashAndAnimateScores());

        // منتظر بمان تا تمام پول‌های "جدید" ریخته شوند
        // این WaitUntil باید درست کار کند، چون cashDroppedCount از صفر شروع می‌شود
        yield return new WaitUntil(() => cashDroppedCount >= eliminatedPlayers);

        // مکث نهایی
        yield return new WaitForSeconds(delayAfterAllCashDropped);

        // به GameManager خبر بده که کار تمام شده
        GameManager.Instance.PrizeSceneComplete();
    }
    
    private IEnumerator SpawnCashAndAnimateScores()
    {
        // اگر هیچکس حذف نشده، کاری نکن (این مشکل "یک نفر" را هم چک می‌کند)
        if (eliminatedPlayers <= 0)
        {
            yield break;
        }

        int bundlesPerPlayer = (int)(moneyPerPlayer / moneyPerCashBundle);

        for (int i = 0; i < eliminatedPlayers; i++)
        {
            for (int j = 0; j < bundlesPerPlayer; j++)
            {
                SpawnSingleCash(); // پول جدید از "بالا" می‌ریزد
            }

            currentPlayersDisplay--;
            currentPrizeDisplay += moneyPerPlayer;
            
            remainingPlayersText.text = currentPlayersDisplay.ToString();
            prizeMoneyText.text = currentPrizeDisplay.ToString("N0");
            
            cashDroppedCount++;

            yield return new WaitForSeconds(cashSpawnInterval);
        }
    }

    // پول جدید را از بالا اسپاون می‌کند
    void SpawnSingleCash()
    {
        GameObject cash = GetPooledCash();
        if (cash != null)
        {
            // از spawnArea (بالای صحنه) استفاده می‌کند
            float randomX = Random.Range(spawnArea.position.x - spawnArea.localScale.x / 2, spawnArea.position.x + spawnArea.localScale.x / 2);
            cash.transform.position = new Vector3(randomX, spawnArea.position.y, 0);
            cash.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            cash.SetActive(true);
        }
    }

    // --- توابع مدیریت پول (اینها را از کد قبلی‌ات کپی کردم) ---
    void CreateCashPool()
    {
        cashPool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject cash = Instantiate(cashPrefab);
            cash.transform.SetParent(transform);
            cash.SetActive(false);
            cashPool.Add(cash);
        }
    }

    GameObject GetPooledCash()
    {
        foreach (GameObject cash in cashPool)
        {
            if (!cash.activeInHierarchy)
            {
                return cash;
            }
        }
        GameObject newCash = Instantiate(cashPrefab);
        newCash.transform.SetParent(transform);
        newCash.SetActive(false);
        cashPool.Add(newCash);
        return newCash;
    }

    void InitializeDisplays()
    {
        currentPlayersDisplay = startPlayers;
        remainingPlayersText.text = currentPlayersDisplay.ToString();
        prizeMoneyText.text = currentPrizeDisplay.ToString("N0");
    }
}