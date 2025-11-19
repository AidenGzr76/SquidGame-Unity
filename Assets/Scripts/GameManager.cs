// کد کامل و نهایی GameManager.cs

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic; // <<< جدید: برای استفاده از List
using System.Linq;            // <<< جدید: برای استفاده از توابع List

/// <summary>
/// این اسکریپت مغز متفکر و کارگردان اصلی بازی است.
/// وظیفه آن مدیریت چرخه اصلی بازی، ذخیره وضعیت و امتیازات است.
/// این آبجکت باید در صحنه منوی اصلی قرار گیرد و در طول بازی از بین نمی‌رود.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton Pattern
    // الگوی Singleton برای اطمینان از اینکه همیشه فقط یک نمونه از GameManager وجود دارد.
    public static GameManager Instance { get; private set; }

    // <<< این رویداد جدید و قدرتمند اضافه شده است >>>
    // هر کسی می‌تواند به این رویداد گوش دهد تا از فعال شدن یک فیچر باخبر شود
    // *** نکته: مطمئن شوید کلاس FeatureData در پروژه شما وجود دارد ***
    public static Action<FeatureData> OnFeatureActivated; 

    // <<< این رویداد جدید اضافه شده است >>>
    // هر مرحله‌ای به این رویداد گوش می‌دهد تا خودش را ریست کند
    public static Action OnStageRespawn;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // این آبجکت با تغییر صحنه از بین نمی‌رود.
            
            // اولین بار که بازی اجرا می‌شود، سکه‌ها را از حافظه بخوان
            LoadCoins();
            
            // <<< جدید >>>: لیست و ابزار رندوم را مقداردهی اولیه می‌کنیم
            activePlayerIDs = new List<int>(); 
            rng = new System.Random(); 
        }
        else
        {
            Destroy(gameObject); // اگر یک GameManager دیگر از قبل وجود داشت، این یکی را نابود کن.
        }
    }
    #endregion

    // --- متغیرهای جدید برای مدیریت سکه‌ها ---
    [Header("Player Economy")]
    public int totalCoins;
    public int totalPrize; // (پول سبز)
    [Tooltip("هزینه (سکه) برای هر بار شروع بازی اصلی")]
    public int startGameCost = 25; // <<< --- این خط جدید --- >>>

    [Tooltip("هزینه مورد نیاز برای بازگشت به بازی بعد از باخت")]
    public int reviveCost = 30; 

    // این یک رویداد است که هر وقت تعداد سکه‌ها تغییر کند، به UI خبر می‌دهد
    public static Action<int> OnCoinsChanged;
    public static Action<int> OnPrizeChanged; 
    
    // <<< این خط جدید اضافه شده است >>>
    // این یک رویداد سراسری است که هر کسی می‌تواند به آن گوش دهد
    public static Action OnStageReset;

    #region Game State Variables
    // --- متغیرهای وضعیت بازی ---
    [Header("Main Game Flow State")]
    public int currentStageIndex = 0;
    
    // <<< تغییر یافته >>>
    // این متغیر همچنان وجود دارد، اما "منبع حقیقت" (Source of Truth)
    // لیست activePlayerIDs خواهد بود. ما این دو را با هم سینک نگه می‌داریم.
    public int remainingPlayers;
    public long currentRunPrize = 0; // جایزه‌ای که فقط در این دور از بازی (run) جمع شده.

    // حالت فعلی بازی را نگه می‌دارد تا اسکریپت‌های مراحل بدانند چگونه رفتار کنند.
    public enum GameMode { MainFlow, Practice }
    public GameMode currentMode;

    // <<< --- بخش مدیریت بازیکنان (جدید) --- >>>
    [Header("Player Management")]
    [Tooltip("لیست ID بازیکنانی که هنوز در بازی زنده هستند")]
    public List<int> activePlayerIDs; // این لیست در DontDestroyOnLoad حفظ می‌شود

    [Tooltip("تعداد کل انواع کاراکترها (مثلاً 1 بازیکن + 9 دشمن = 10)")]
    public int totalCharacterTypes = 10;
    
    [Tooltip("تعداد کل بازیکنان در شروع بازی")]
    public int totalStartPlayers = 20;

    private System.Random rng; // برای کارهای رندوم مثل Shuffle
    private int playersAtStageStart; // تعداد بازیکنان در "شروع" این مرحله
    // <<< --- پایان بخش جدید --- >>>
    
    #endregion

    #region Scene Management
    [Header("Scene Names")]
    [Tooltip("نام دقیق 6 صحنه اصلی بازی را به ترتیب وارد کنید.")]
    public string[] mainStageSceneNames = new string[6];
    
    [Tooltip("نام صحنه‌های واسط")]
    public string prizeSceneName = "PrizeScene";
    public string votingSceneName = "VotingScene";
    public string mainMenuSceneName = "MainMenu";
    #endregion


    #region Coin Management Methods
    // --- توابع جدید برای مدیریت سکه‌ها ---

    private void LoadCoins()
    {
        // سکه‌ها را از PlayerPrefs بخوان. اگر وجود نداشت، با ۱۰۰ سکه شروع کن.
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 100);

        // پول را هم از PlayerPrefs بخوان (شروع با 0)
        totalPrize = PlayerPrefs.GetInt("TotalPrize", 0);
    }

    public bool HasEnoughCoins(int amount)
    {
        return totalCoins >= amount;
    }

    public void SpendCoins(int amount)
    {
        if (HasEnoughCoins(amount))
        {
            totalCoins -= amount;
            PlayerPrefs.SetInt("TotalCoins", totalCoins); // ذخیره مقدار جدید
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(totalCoins); // به همه خبر بده که سکه‌ها تغییر کرده!
            Debug.Log($"Spent {amount} coins. Remaining: {totalCoins}");
        }
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(totalCoins);
        Debug.Log($"Added {amount} coins. Total: {totalCoins}");
    }
    #endregion


    #region Prize Management Methods
    // --- بخش مدیریت "پول" --- 
    
    public bool HasEnoughPrize(int amount)
    {
        return totalPrize >= amount;
    }

    public void SpendPrize(int amount)
    {
        if (HasEnoughPrize(amount))
        {
            totalPrize -= amount;
            PlayerPrefs.SetInt("TotalPrize", totalPrize); // ذخیره مقدار جدید
            PlayerPrefs.Save();
            OnPrizeChanged?.Invoke(totalPrize); // به UI خبر بده
            Debug.Log($"Spent {amount} prize. Remaining: {totalPrize}");
        }
    }

    public void AddPrize(int amount)
    {
        totalPrize += amount;
        PlayerPrefs.SetInt("TotalPrize", totalPrize);
        PlayerPrefs.Save();
        OnPrizeChanged?.Invoke(totalPrize);
        Debug.Log($"Added {amount} prize. Total: {totalPrize}");
    }
    #endregion


    #region Core Game Flow Methods
    // --- توابع اصلی مدیریت چرخه بازی ---

    /// <summary>
    /// بازی اصلی را شروع می‌کند، اگر بازیکن سکه کافی داشته باشد.
    /// </summary>
    /// <returns>True اگر بازی با موفقیت شروع شد، False اگر سکه کافی نبود.</returns>
    public bool StartMainGame() // <<< --- نوع خروجی به bool تغییر کرد --- >>>
    {
        // ۱. چک کردن سکه
        if (!HasEnoughCoins(startGameCost))
        {
            Debug.Log($"Not enough coins to start the game. Need {startGameCost}, have {totalCoins}.");
            return false; // <<< --- اگر سکه کافی نیست، خارج شو --- >>>
        }

        // ۲. خرج کردن سکه (اگر کافی بود)
        SpendCoins(startGameCost);

        // ۳. شروع بازی (کد قبلی شما)
        currentMode = GameMode.MainFlow;
        currentStageIndex = 0;
        currentRunPrize = 0;
        InitializePlayerList();
        playersAtStageStart = activePlayerIDs.Count;
        remainingPlayers = activePlayerIDs.Count;
        SceneManager.LoadScene(mainStageSceneNames[0]);

        return true; // <<< --- بازی با موفقیت شروع شد --- >>>
    }


    /// <summary>
    /// این تابع باید از منوی تمرینی، قبل از بارگذاری هر مرحله تمرینی، صدا زده شود.
    /// </summary>
    public void SetPracticeMode()
    {
        currentMode = GameMode.Practice;
        Debug.Log("Game mode set to: Practice");
    }


    // <<< --- تغییر بزرگ: StageComplete دو بخشی شد --- >>>

    /// <summary>
    /// (جدید) برای مراحل قابل مشاهده (۱، ۳، ۵، ۶) که بازیکنان در صحنه حذف می‌شوند.
    /// این تابع تعداد حذف‌شده‌ها را بر اساس تفاوت لیست محاسبه می‌کند.
    /// </summary>
    /// <summary>
    /// ...
    /// </summary>
    public void StageComplete()
    {
        if (currentMode != GameMode.MainFlow) return;

        int eliminatedCount = playersAtStageStart - activePlayerIDs.Count;
        remainingPlayers = activePlayerIDs.Count;

        // <<< --- تغییر کلیدی ۱ --- >>>
        // جایزه "قبلی" (مثلا ۸۰۰۰) را قبل از اضافه کردن، ذخیره کن
        PlayerPrefs.SetString("PreviousRunPrize", currentRunPrize.ToString());

        // حالا جایزه جدید را اضافه کن
        currentRunPrize += (long)eliminatedCount * 1000; 

        PlayerPrefs.SetInt("EliminatedInLastStage", eliminatedCount);
        PlayerPrefs.SetInt("StartPlayersThisRound", playersAtStageStart);

        PlayerPrefs.SetInt("CurrentStageIndex", currentStageIndex);

        PlayerPrefs.Save();
        
        SceneManager.LoadScene(prizeSceneName);
    }

    /// <summary>
    /// ...
    /// </summary>
    public void StageComplete(int randomEliminationCount)
    {
        if (currentMode != GameMode.MainFlow) return;

        EliminateRandomPlayers(randomEliminationCount);
        
        // <<< --- تغییر کلیدی ۲ --- >>>
        // اینجا هم جایزه "قبلی" را ذخیره کن
        PlayerPrefs.SetString("PreviousRunPrize", currentRunPrize.ToString());

        currentRunPrize += (long)randomEliminationCount * 1000;

        PlayerPrefs.SetInt("EliminatedInLastStage", randomEliminationCount);
        PlayerPrefs.SetInt("StartPlayersThisRound", playersAtStageStart);

        PlayerPrefs.SetInt("CurrentStageIndex", currentStageIndex);

        PlayerPrefs.Save();
        
        SceneManager.LoadScene(prizeSceneName);
    }


    /// <summary>
    /// این تابع زمانی صدا زده می‌شود که بازیکن در یک مرحله ببازد.
    /// </summary>
    public void StageFailed()
    {
        if (currentMode != GameMode.MainFlow) return;
        
        // تمام امتیازات کسب شده در این دور از بین می‌رود.
        currentRunPrize = 0;
        
        // <<< جدید >>>: لیست بازیکنان را برای دور بعدی خالی کن
        activePlayerIDs.Clear();
        remainingPlayers = 0;

        // بازی را تمام کن (بدون دادن امتیاز).
        EndMainGameRun();
    }

    /// <summary>
    /// این تابع توسط اسکریپت صحنه جایزه (PrizeScene) بعد از اتمام انیمیشن صدا زده می‌شود.
    /// </summary>
    public void PrizeSceneComplete()
    {
        // لاگ برای اطمینان از اجرا و تعداد بازیکنان
        Debug.Log($"[GameManager] PrizeSceneComplete called. Remaining players: {remainingPlayers}");

        // --- <<< ۱. بررسی شرط تک‌نفره شدن (مهم‌ترین بخش) >>> ---
        // آیا در حالت بازی اصلی هستیم و فقط ۱ نفر باقی مانده؟
        if (currentMode == GameMode.MainFlow && remainingPlayers == 1)
        {
            Debug.Log("[GameManager] Only one player remains! Ending game as winner.");

            // محاسبه جایزه نهایی بر اساس تعداد بازیکنان اولیه
            // (مثلاً ۲۰ نفر * ۱۰۰۰ = ۲۰۰۰۰)
            long finalPrize = (long)totalStartPlayers * 1000; // از متغیر استفاده می‌کنیم نه عدد ثابت
            currentRunPrize = finalPrize; // جایزه این دور را برابر با جایزه نهایی قرار می‌دهیم

            // تابع استاندارد پایان بازی را صدا می‌زنیم (که جایزه را اضافه و به منو می‌رود)
            EndMainGameRun();
            
            return; // <<<--- مهم: از ادامه تابع خارج می‌شویم --->>>
        }
        // --- <<< پایان بررسی شرط تک‌نفره شدن >>> ---

        // --- ۲. منطق عادی (اگر بیشتر از ۱ نفر باقی مانده) ---
        // (این بخش کد قبلی شماست، فقط اندیس چک کردن مرحله آخر اصلاح شده)
        // آیا مرحله ششم (آخرین مرحله با اندیس Length - 1) تمام شده است؟
        if (currentStageIndex >= mainStageSceneNames.Length - 1)
        {
            Debug.Log("[GameManager] Final stage completed with multiple survivors. Ending game.");
            // بازی با موفقیت بعد از آخرین مرحله به پایان رسید.
            EndMainGameRun();
        }
        else
        {
            Debug.Log("[GameManager] Proceeding to Voting Scene.");
            // به مرحله رأی‌گیری برو.
            SceneManager.LoadScene(votingSceneName);
        }
    }

    /// <summary>
    /// این تابع توسط اسکریپت صحنه رأی‌گیری (VotingScene) بعد از اتمام رأی‌گیری صدا زده می‌شود.
    /// </summary>
    /// <param name="passed">اگر رأی مثبت بود true وگرنه false.</param>
    public void VotingComplete(bool passed)
    {
        if (passed)
        {
            // رأی مثبت بود، به مرحله بعد برو.
            currentStageIndex++;

            // <<< جدید >>>: قبل از رفتن به مرحله بعد، تعداد بازیکنان فعلی را ذخیره کن
            // این برای محاسبه در StageComplete بعدی استفاده می‌شود.
            playersAtStageStart = activePlayerIDs.Count;
            
            // <<< --- این خط جدید اضافه شود --- >>>
            // BackgroundMusic.PlayMusic();
            
            SceneManager.LoadScene(mainStageSceneNames[currentStageIndex]);
        }
        else
        {
            // رأی منفی بود، بازی تمام می‌شود و هرچی امتیاز کسب کرده را می‌گیرد.
            EndMainGameRun();
        }
    }


    private void EndMainGameRun()
    {
        // ما جایزه "پول" رو در یک کلید جدید به اسم "PrizeToAdd" ذخیره می‌کنیم
        PlayerPrefs.SetInt("PrizeToAdd", (int)currentRunPrize);

        if (currentRunPrize > 0)
        {
            // "فقط" به totalPrize (پول) اضافه می‌شود
            AddPrize((int)currentRunPrize); 
        }

        PlayerPrefs.Save(); 
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion



    // <<< --- توابع جدید برای مدیریت لیست بازیکنان --- >>>
    #region Player List Management

    /// <summary>
    /// این تابع در StartMainGame صدا زده می‌شود تا لیست ۲۰ نفره اولیه را بسازد.
    /// </summary>
    // در فایل GameManager.cs

    private void InitializePlayerList()
    {
        activePlayerIDs.Clear();

        // <<< --- منطق جدید برای جلوگیری از تکرار بازیکن --- >>>

        // ۱. (جدید) اضافه کردن "فقط یک" بازیکن با ID 0
        activePlayerIDs.Add(0); 

        // ۲. (جدید) اطمینان از وجود حداقل "یک" دشمن از هر نوع (1 تا 9)
        // (totalCharacterTypes همچنان باید 10 باشد)
        for (int id = 1; id < totalCharacterTypes; id++)
        {
            // این حلقه ID های 1, 2, 3, 4, 5, 6, 7, 8, 9 را اضافه می‌کند
            activePlayerIDs.Add(id);
        }
        
        // در این لحظه، ما 10 بازیکن (1 بازیکن اصلی + 9 نوع دشمن) در لیست داریم.
        // این کار، خواسته شما مبنی بر "حداقل از هر مدلی یکی باشه" را برآورده می‌کند.

        // ۳. (جدید) پر کردن الباقی لیست (10 نفر) "فقط با دشمنان"
        int remainingToAdd = totalStartPlayers - activePlayerIDs.Count; // 20 - 10 = 10
        
        for (int i = 0; i < remainingToAdd; i++)
        {
            // <<< --- تغییر کلیدی --- >>>
            // ما یک ID رندوم "فقط" از بین دشمنان (1 تا 9) انتخاب می‌کنیم
            // Random.Range(1, 10) اعداد 1 تا 9 را برمی‌گرداند.
            activePlayerIDs.Add(UnityEngine.Random.Range(1, totalCharacterTypes)); 
        }

        // <<< --- پایان منطق جدید --- >>>


        // ۴. (بدون تغییر) لیست را بُر می‌زنیم (Shuffle)
        // این کار باعث می‌شود بازیکن (ID 0) در یک نقطه رندوم در صف اسپاون قرار گیرد.
        int n = activePlayerIDs.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int value = activePlayerIDs[k];
            activePlayerIDs[k] = activePlayerIDs[n];
            activePlayerIDs[n] = value;
        }

        Debug.Log($"لیست بازیکنان با {activePlayerIDs.Count} نفر ایجاد شد.");
        
        // <<< --- این خط دیباگ حیاتی را به "آخر" تابع اضافه کنید --- >>>
        Debug.LogWarning($"--- GAME MANAGER LIST INITIALIZED --- Count: {activePlayerIDs.Count} -- Contains Player (ID 0): {activePlayerIDs.Contains(0)}");

        // (اختیاری) برای اینکه ۱۰۰٪ مطمئن شوید، می‌توانید این خط را برای تست اضافه کنید:
        // int playerCount = activePlayerIDs.Count(id => id == 0);
        // Debug.Log($"تعداد واقعی بازیکن (ID 0) در لیست: {playerCount}"); // باید همیشه 1 باشد
    }

    /// <summary>
    /// این تابع توسط اسکریپت دشمنان (یا بازیکن) در مراحل ۱، ۳، ۵، ۶ صدا زده می‌شود
    /// زمانی که می‌میرند یا حذف می‌شوند.
    /// </summary>
    /// <param name="id">ID کاراکتری که باید حذف شود</param>
    public void EliminatePlayer(int id)
    {
        // .Remove() اولین مورد مطابق با ID را پیدا کرده و حذف می‌کند
        if (activePlayerIDs.Remove(id))
        {
            // شمارنده عمومی را هم به‌روز می‌کنیم
            remainingPlayers = activePlayerIDs.Count; 
            Debug.Log($"بازیکن با ID {id} حذف شد. {remainingPlayers} نفر باقی‌مانده‌اند.");
        }
        else
        {
            Debug.LogWarning($"تلاش برای حذف ID {id} بود، اما در لیست وجود نداشت! (احتمالا قبلا حذف شده)");
        }
    }

    /// <summary>
    /// این تابع توسط StageComplete(int count) برای مراحل ۲ و ۴ استفاده می‌شود.
    /// </summary>
    /// <param name="count">تعداد بازیکنانی که باید رندوم حذف شوند</param>
    public void EliminateRandomPlayers(int count)
    {
        int eliminatedSoFar = 0;
        
        // قانون پیشنهادی: بازیکن اصلی (ID 1) در حذف رندوم ایمن است
        bool playerIsSafe = true; 

        // اطمینان از اینکه بیشتر از تعداد موجود حذف نمی‌کنیم
        int countToEliminate = Mathf.Min(count, activePlayerIDs.Count);
        
        // اگر بازیکن امن است و در لیست هست، تعداد قابل حذف یکی کمتر است
        if(playerIsSafe && activePlayerIDs.Contains(0))
        {
             countToEliminate = Mathf.Min(count, activePlayerIDs.Count - 1);
             if(countToEliminate < 0) countToEliminate = 0;
        }


        for(int i = 0; i < countToEliminate; i++)
        {
            if (activePlayerIDs.Count == 0) break; // لیست خالی شد

            int randomIndex = UnityEngine.Random.Range(0, activePlayerIDs.Count);
            int idToRemove = activePlayerIDs[randomIndex];

            // اگر به هر دلیلی ID 1 انتخاب شد، دوباره تلاش کن
            if (playerIsSafe && idToRemove == 0)
            {
                i--; // این دور را حساب نکن و دوباره تلاش کن
                continue;
            }

            activePlayerIDs.RemoveAt(randomIndex);
            eliminatedSoFar++;
        }

        remainingPlayers = activePlayerIDs.Count; // سینک نهایی
        Debug.Log($"{eliminatedSoFar} بازیکن به صورت رندوم حذف شدند. {remainingPlayers} نفر باقی‌مانده‌اند.");
    }
    
    #endregion
    // <<< --- پایان توابع جدید --- >>>
}