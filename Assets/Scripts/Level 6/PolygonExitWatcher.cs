using UnityEngine;

public class AreaExitWatcher : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string EnemyTag = "Enemy"; 

    public static AreaExitWatcher Instance { get; private set; }
    
    // --- متغیرهای جدید برای مدیریت مرحله ---
    private int eliminatedAICount = 0; // شمارشگر داخلی
    private int totalEnemiesInStage = 0; // تعداد کل دشمنان در شروع
    private bool playerHasFallen = false; // برای جلوگیری از باخت چندباره
    private bool playerHasWon = false; // برای جلوگیری از برد چندباره

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // <<< --- این تابع جدید اضافه شده است --- >>>
    void Start()
    {
        // در شروع مرحله، از GameManager می‌پرسیم چند نفر در بازی هستند
        if (GameManager.Instance != null && GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
        {
            // تعداد کل دشمنان = تمام بازیکنان باقیمانده، منهای 1 (که خود بازیکن است)
            totalEnemiesInStage = GameManager.Instance.remainingPlayers - 1;
            Debug.Log($"[AreaExitWatcher] مرحله 6 با {totalEnemiesInStage} دشمن شروع شد.");
        }
        else
        {
            // --- <<< راه‌حل اینجاست >>> ---
            // --- منطق حالت تمرینی ---
            // در حالت تمرین، ما باید دشمنان را خودمان بشماریم
            // (مطمئن شو که تگ دشمنان در Inspector این اسکریپت درست تنظیم شده)
            GameObject[] practiceEnemies = GameObject.FindGameObjectsWithTag(EnemyTag);

            totalEnemiesInStage = practiceEnemies.Length;

            Debug.Log($"[AreaExitWatcher] (Practice Mode): مرحله 6 با {totalEnemiesInStage} دشمن شروع شد.");
        }
    
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // اگر بازیکن برنده شده، دیگر چیزی را پردازش نکن
        if (playerHasWon) return; 

        // <<< --- این بخش برای بازیکن دوباره اضافه و اصلاح می‌شود --- >>>
        if (other.CompareTag(playerTag))
        {
            // اگر بازیکن قبلا نیفتاده باشد...
            if (!playerHasFallen)
            {
                playerHasFallen = true; // ثبت کن که افتاده
                PlayerSquidController player = other.GetComponent<PlayerSquidController>();
                if (player != null)
                {
                    Debug.Log("بازیکن از محدوده خارج شد! -> فراخوانی OnKilledByGun");
                    player.OnKilledByGun(); // این درست است، چون می‌خواهیم شلیک شود
                }
            }
            // اگر قبلا افتاده بود، هیچ کاری نکن (چون منتظر Revive هستیم)
        }
        else if (other.CompareTag(EnemyTag))
        {
            SimpleEnemy enemy = other.GetComponent<SimpleEnemy>();
            if (enemy != null && enemy.isAlive)
            {
                // --- <<< ۱. (تغییر کلیدی) ثبت حذف در GameManager --- >>>
                // اول ID دشمن را می‌گیریم
                CharacterIdentity identity = enemy.GetComponent<CharacterIdentity>();
                if (identity != null && GameManager.Instance != null)
                {
                    // به گیم منیجر می‌گوییم این ID را از لیست بازیکنان زنده حذف کند
                    GameManager.Instance.EliminatePlayer(identity.playerID);
                }
                else
                {
                    Debug.LogWarning($"Enemy {enemy.name} ID ندارد و نتوانست به GameManager گزارش دهد!");
                }

                // --- ۲. (کد قبلی شما) تابع مرگ خود دشمن را صدا می‌زنیم ---
                enemy.OnKilledByGun(); 

                // --- ۳. (تغییر کلیدی) شمارشگر داخلی و چک کردن بُرد ---
                eliminatedAICount++;
                Debug.Log($"دشمن حذف شد. تعداد حذف شده: {eliminatedAICount} / {totalEnemiesInStage}");

                // اگر تعداد حذف شده‌ها با تعداد کل دشمنان برابر شد...
                if (eliminatedAICount >= totalEnemiesInStage)
                {
                    CheckForWin();
                }
            }
        }
    }

    // <<< --- این تابع جدید اضافه شده است --- >>>
    /// <summary>
    /// چک می‌کند که آیا بازیکن برنده شده است (آخرین نفر ایستاده)
    /// </summary>
    void CheckForWin()
    {
        // اگر بازیکن همزمان با آخرین دشمن افتاده باشد، برنده نمی‌شود
        if (playerHasFallen)
        {
            Debug.Log("آخرین دشمن حذف شد، اما بازیکن هم افتاده بود. (مساوی)");
            return;
        }

        playerHasWon = true;
        Debug.Log("--- تمام دشمنان حذف شدند! بازیکن برنده مرحله 6 شد! ---");

        // به گیم منیجر اطلاع می‌دهیم که مرحله تمام شده است
        // گیم منیجر خودش بر اساس لیست، جایزه نهایی را محاسبه می‌کند
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StageComplete();
        }
    }

    // <<< --- این تابع دیگر استفاده نمی‌شود و می‌تواند حذف شود --- >>>
    // public int GetEliminatedCount()
    // {
    //     return eliminatedAICount;
    // }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            // Debug.Log("[AreaExitWatcher] Player entered the area!");
        }
    }

    // <<< --- این تابع جدید اضافه شده است --- >>>
    /// <summary>
    /// این تابع توسط PlayerSquidController.Respawn صدا زده می‌شود
    /// تا وضعیت افتادن بازیکن را ریست کند.
    /// </summary>
    public void ResetPlayerFallState()
    {
        playerHasFallen = false;
        Debug.Log("[AreaExitWatcher] Player fall state reset.");
    }
}
