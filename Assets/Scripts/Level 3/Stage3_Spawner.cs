using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// این اسکریپت رو به یک آبجکت خالی در صحنه (مثلاً _Spawner) وصل کن
public class Stage3_Spawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("پریفب بازیکن اصلی (باید CharacterIdentity با ID 0 داشته باشد)")]
    public GameObject playerPrefab;
    [Tooltip("لیست پریفب تمام دشمنان (باید CharacterIdentity با ID > 0 داشته باشند)")]
    public GameObject[] enemyPrefabs;

    [Header("References")]
    [Tooltip("اسکریپت TugOfWarController در صحنه")]
    public TugOfWarController gameController; // TugOfWarController رو از Hierarchy بکش اینجا

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager پیدا نشد!");
            return;
        }

        if (gameController == null)
        {
            Debug.LogError("GameController به Spawner وصل نشده!");
            return;
        }

        // اگر در حالت تمرینی هستیم، فقط یک بازی استاندارد 5v5 اسپاون کن
        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            SpawnPracticeMode();
        }
        else // اگر در حالت بازی اصلی هستیم
        {
            SpawnMainGameMode();
        }
    }

    // --- منطق اصلی تیم‌بندی و اسپاون ---

    void SpawnMainGameMode()
    {
        // ۱. گرفتن لیست بازیکنان باقیمانده و Shuffle کردن
        List<int> allPlayerIDs = new List<int>(GameManager.Instance.activePlayerIDs);
        int totalPlayers = allPlayerIDs.Count;
        System.Random rng = new System.Random();
        allPlayerIDs = allPlayerIDs.OrderBy(a => rng.Next()).ToList();

        // ۲. محاسبه تیم‌ها بر اساس منطق شما
        List<int> teamA_IDs = new List<int>();
        List<int> teamB_IDs = new List<int>();
        List<int> safePlayers_IDs = new List<int>();
        int actualTeamSize = 0;
        int maxTeamSize = gameController.maxTeamSize; // خواندن maxTeamSize از کنترلر

        if (totalPlayers < 2)
        {
            safePlayers_IDs = allPlayerIDs; // همه امن هستند
        }
        else
        {
            // اندازه تیم = (تعداد کل / ۲) گرد شده به پایین، اما حداکثر ۵ نفر
            actualTeamSize = Mathf.Min(totalPlayers / 2, maxTeamSize);
            
            int numToPlay = actualTeamSize * 2;
            int numSafe = totalPlayers - numToPlay;

            // تخصیص بازیکنان به لیست‌ها
            safePlayers_IDs = allPlayerIDs.GetRange(0, numSafe);
            teamA_IDs = allPlayerIDs.GetRange(numSafe, actualTeamSize);
            teamB_IDs = allPlayerIDs.GetRange(numSafe + actualTeamSize, actualTeamSize);
        }

        // ۳. اسپاون کردن بازیکنان در تیم‌های درست
        bool isPlayerOnTeamA = false;
        
        // اسپاون تیم A
        foreach (int id in teamA_IDs)
        {
            SpawnPlayer(id, gameController.teamAParent);
            if (id == 0) isPlayerOnTeamA = true;
        }
        
        // اسپاون تیم B
        foreach (int id in teamB_IDs)
        {
            SpawnPlayer(id, gameController.teamBParent);
            // (اگر بازیکن در تیم A نبود، isPlayerOnTeamA از قبل false هست)
        }

        // ۴. لاگ کردن نتیجه
        Debug.Log($"--- تیم‌بندی مرحله ۳ (Spawner) ---");
        Debug.Log($"بازیکنان کل: {totalPlayers} | تیم‌ها: {actualTeamSize} در برابر {actualTeamSize} | امن: {safePlayers_IDs.Count}");
        
        // ۵. (مهم) به کنترلر بازی بگو که اسپاون تمام شده و بازی را شروع کند
        // و بهش بگو که اندازه تیم چقدره و بازیکن کجاست
        gameController.InitializeMatch(isPlayerOnTeamA, actualTeamSize);
    }

    /// <summary>
    /// یک بازیکن (یا دشمن) را بر اساس ID اسپاون می‌کند.
    /// </summary>
    void SpawnPlayer(int id, Transform parentTeam)
    {
        GameObject prefabToSpawn = null;
        if (id == 0) // اگر بازیکن اصلی است
        {
            prefabToSpawn = playerPrefab;
        }
        else // اگر دشمن است
        {
            // پیدا کردن پریفب دشمن بر اساس ID
            prefabToSpawn = enemyPrefabs.FirstOrDefault(p => p.GetComponent<CharacterIdentity>() != null && p.GetComponent<CharacterIdentity>().playerID == id);
        }

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, parentTeam); // اسپاون به عنوان فرزند تیم
        }
        else
        {
            Debug.LogError($"پریفبی برای ID {id} پیدا نشد! (آیا CharacterIdentity روی پریفب دشمنان تنظیم شده؟)");
        }
    }
    
    // (اختیاری) یک تابع برای حالت تمرینی
    void SpawnPracticeMode()
    {
        int teamSize = gameController.maxTeamSize;
        // اسپاون بازیکن در تیم A
        SpawnPlayer(0, gameController.teamAParent);
        // اسپاون 4 یار در تیم A
        for (int i = 0; i < teamSize - 1; i++)
        {
            if (enemyPrefabs.Length > i)
                SpawnPlayer(enemyPrefabs[i].GetComponent<CharacterIdentity>().playerID, gameController.teamAParent);
        }
        // اسپاون 5 دشمن در تیم B
        for (int i = 0; i < teamSize; i++)
        {
            if (enemyPrefabs.Length > i)
                SpawnPlayer(enemyPrefabs[i].GetComponent<CharacterIdentity>().playerID, gameController.teamBParent);
        }
        
        // شروع بازی
        gameController.InitializeMatch(true, teamSize);
    }
}