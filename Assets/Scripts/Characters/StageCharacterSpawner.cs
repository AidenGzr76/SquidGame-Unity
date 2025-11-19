using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using Random = UnityEngine.Random;

public class StageCharacterSpawner : MonoBehaviour
{
    [Header("Character Mapping (مخصوص این مرحله)")]
    public CharacterMapping[] characterMappings;

    [Header("Dependencies (دوربین و دکمه)")]
    public GameObject sceneCameraObject;
    public Button pushButton; // (فقط برای مرحله ۱)

    [Header("Stage 3 - Tug of War Settings")]
    public Transform teamAParent;
    public Transform teamBParent;
    public Transform[] teamASpawnPoints;
    public Transform[] teamBSpawnPoints;
    public TugOfWarController tugOfWarController; // <<< ارجاع به کنترلر

    [Header("Stage 5 - Glass Bridge Settings")]
    public QueueManager queueManager;

    [Header("Default Spawn Points")]
    public Transform[] defaultSpawnPoints;

    private Dictionary<int, GameObject> prefabLookUp;
    private System.Random rng = new System.Random();

    void Awake()
    {
        prefabLookUp = new Dictionary<int, GameObject>();
        foreach (var mapping in characterMappings)
        {
            if (!prefabLookUp.ContainsKey(mapping.characterID))
                prefabLookUp.Add(mapping.characterID, mapping.characterPrefab);
        }
    }

    void Start()
    {
        if (GameManager.Instance == null) { Debug.LogError("GameManager not found!"); return; }
        
        if (GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
        {
            SpawnPlayers_MainGame();
        }
        else // حالت تمرینی
        {
            SpawnPlayers_Practice();
        }
    }

    void SpawnPlayers_MainGame()
    {
        int stageIndex = GameManager.Instance.currentStageIndex;
        
        if (stageIndex == 2 && tugOfWarController != null) // مرحله ۳ (طناب‌کشی)
        {
            SpawnForTugOfWar_MainGame();
        }
        else if (stageIndex == 4 && queueManager != null) // مرحله ۵ (پل شیشه‌ای)
        {
            SpawnForQueue();
        }
        else // مراحل دیگر (۱، ۶ و ...)
        {
            SpawnForDefaultStages();
        }
    }
    
    // <<< --- این تابع "جدید" و مخصوص بازی اصلی است --- >>>
    void SpawnForTugOfWar_MainGame()
    {
        Debug.Log("--- شروع منطق تیم‌بندی مرحله ۳ (MainFlow) ---");
        
        // ۱. لیست‌ها را آماده کن
        List<int> allPlayers = new List<int>(GameManager.Instance.activePlayerIDs);
        int totalPlayers = allPlayers.Count;
        
        List<int> teamA_IDs = new List<int>();
        List<int> teamB_IDs = new List<int>();
        List<int> safePlayers_IDs = new List<int>();
        int actualTeamSize = 0;
        int maxTeamSize = tugOfWarController.maxTeamSize;

        // --- ۲. بازیکن اصلی (ID 0) را جدا کن و در تیم A قرار بده ---
        bool playerWasFound = allPlayers.Remove(0); // بازیکن (ID 0) را از لیست کل حذف کن
        if (playerWasFound)
        {
            teamA_IDs.Add(0); // بازیکن همیشه در تیم A است
            Debug.Log("[Spawner] بازیکن (ID 0) پیدا شد و در تیم A قرار گرفت.");
        }
        else
        {
            Debug.LogWarning("[Spawner] بازیکن (ID 0) در لیست GameManager نبود! (احتمالاً در مرحله قبل باخته)");
        }

        // --- ۳. منطق تیم‌بندی را روی "بقیه" (AIها) اجرا کن ---
        List<int> allAIs = allPlayers; // (چون بازیکن از allPlayers حذف شده)
        int totalAIs = allAIs.Count;
        allAIs = allAIs.OrderBy(a => rng.Next()).ToList(); // AI ها را بُر بزن

        int numToPlay;
        int numSafe;

        // اگر بازیکن + AI ها کمتر از 2 نفر باشند
        if (totalPlayers < 2)
        {
            actualTeamSize = 0;
            numSafe = totalAIs; // همه AI های باقیمانده امن هستند
            safePlayers_IDs = allAIs;
        }
        else
        {
            // منطق شما (مثلاً با 5 نفر کل -> 1 بازیکن، 4 AI)
            // (totalPlayers / 2) = 2. actualTeamSize = 2
            actualTeamSize = Mathf.Min(totalPlayers / 2, maxTeamSize);
            numToPlay = actualTeamSize * 2;
            numSafe = totalPlayers - numToPlay; // 5 - 4 = 1 نفر امن

            // (نفرات امن از لیست AI ها انتخاب می‌شوند)
            safePlayers_IDs = allAIs.GetRange(0, numSafe); // 1 نفر اول AI امن است
            
            // بقیه AI ها به تیم‌ها اضافه می‌شوند
            int remainingAIsToAssign = numToPlay - (playerWasFound ? 1 : 0); // (4 - 1 = 3 AI)
            List<int> aisForMatch = allAIs.GetRange(numSafe, remainingAIsToAssign);
            
            // (تیم A به 1 نفر AI دیگر نیاز دارد)
            int aisForTeamA = actualTeamSize - (playerWasFound ? 1 : 0); // (2 - 1 = 1 AI)
            teamA_IDs.AddRange(aisForMatch.GetRange(0, aisForTeamA));

            // (تیم B به 2 نفر AI نیاز دارد)
            int aisForTeamB = actualTeamSize; // (2 AI)
            teamB_IDs.AddRange(aisForMatch.GetRange(aisForTeamA, aisForTeamB));
        }

        Debug.Log($"[Spawner] تیم‌بندی: کل={totalPlayers} | تیم‌ها={actualTeamSize}v{actualTeamSize} | امن={safePlayers_IDs.Count}");

        // --- ۴. اسپاون کردن بازیکنان ---
        bool playerIsOnTeamA = teamA_IDs.Contains(0);
        
        for (int i = 0; i < teamA_IDs.Count; i++)
        {
            SpawnSinglePlayer(teamA_IDs[i], teamAParent, teamASpawnPoints[i]);
        }
        for (int i = 0; i < teamB_IDs.Count; i++)
        {
            SpawnSinglePlayer(teamB_IDs[i], teamBParent, teamBSpawnPoints[i]);
        }
        
        // ۵. اطلاع دادن به کنترلر
        tugOfWarController.InitializeMatch(playerIsOnTeamA, actualTeamSize);
    }

    void SpawnPlayers_Practice()
    {
        Debug.Log("--- شروع اسپاون (PracticeMode) ---");
        int stageIndex = GameManager.Instance.currentStageIndex;

        if (stageIndex == 2 && tugOfWarController != null)
        {
            int teamSize = tugOfWarController.maxTeamSize;
            SpawnSinglePlayer(0, teamAParent, teamASpawnPoints[0]);
            for (int i = 1; i < teamSize; i++)
            {
                if (characterMappings.Length > i)
                    SpawnSinglePlayer(characterMappings[i].characterID, teamAParent, teamASpawnPoints[i]);
            }
            for (int i = 0; i < teamSize; i++)
            {
                if (characterMappings.Length > i)
                    SpawnSinglePlayer(characterMappings[i].characterID, teamBParent, teamBSpawnPoints[i]);
            }
            tugOfWarController.InitializeMatch(true, teamSize);
        }
        else
        {
            SpawnForDefaultStages();
        }
    }

    /// <summary>
    /// تابع کمکی برای اسپاون کردن یک کاراکتر
    /// </summary>
    void SpawnSinglePlayer(int idToSpawn, Transform teamParent, Transform spawnPoint)
    {
        if (!prefabLookUp.TryGetValue(idToSpawn, out GameObject prefabToSpawn))
        {
            Debug.LogError($"!Prefab برای ID {idToSpawn} در StageCharacterSpawner تعریف نشده!");
            return;
        }

        // اسپاون به عنوان فرزند Parent تیم
        GameObject spawnedCharacter = Instantiate(prefabToSpawn, teamParent);
        // استفاده از localPosition/localRotation نقاط اسپاون
        spawnedCharacter.transform.localPosition = spawnPoint.localPosition;
        spawnedCharacter.transform.localRotation = spawnPoint.localRotation;

        CharacterIdentity identity = spawnedCharacter.GetComponent<CharacterIdentity>();
        if (identity == null) identity = spawnedCharacter.AddComponent<CharacterIdentity>();
        identity.playerID = idToSpawn;
        spawnedCharacter.SetActive(true);

        if (idToSpawn == 0)
        {
            AssignPlayerToCamera(spawnedCharacter.transform);
            AssignPlayerToButton(spawnedCharacter); // این تابع فقط اگر pushButton نال نباشد کار می‌کند
        }
    }

    // (این توابع همگی از کد شما کپی شده‌اند و درست هستند)
    #region Unchanged Helper Functions
    void SpawnForQueue()
    {
        List<int> shuffledIDs = GameManager.Instance.activePlayerIDs.OrderBy(x => rng.Next()).ToList();
        int spawnLimit = Mathf.Min(shuffledIDs.Count, queueManager.queueSlots.Count);
        List<MonoBehaviour> spawnedQueue = new List<MonoBehaviour>();
        for (int i = 0; i < spawnLimit; i++)
        {
            int idToSpawn = shuffledIDs[i];
            Transform spawnSlot = queueManager.queueSlots[i];
            if (prefabLookUp.TryGetValue(idToSpawn, out GameObject prefabToSpawn))
            {
                GameObject spawnedCharacter = Instantiate(prefabToSpawn, spawnSlot.position, spawnSlot.rotation);
                CharacterIdentity identity = spawnedCharacter.GetComponent<CharacterIdentity>();
                if (identity == null) identity = spawnedCharacter.AddComponent<CharacterIdentity>();
                identity.playerID = idToSpawn;
                spawnedCharacter.SetActive(true);
                if (idToSpawn == 0)
                {
                    AssignPlayerToCamera(spawnedCharacter.transform);
                }
                MonoBehaviour controller = spawnedCharacter.GetComponent<PlayerClickMove>() as MonoBehaviour ?? spawnedCharacter.GetComponent<GlassBridgeAI>();
                if (controller != null) spawnedQueue.Add(controller);
            }
        }
        queueManager.InitializeQueue(spawnedQueue);
    }
    
    void SpawnForDefaultStages()
    {
        List<int> playersToSpawn = GameManager.Instance.activePlayerIDs;
        int spawnLimit = Mathf.Min(playersToSpawn.Count, defaultSpawnPoints.Length);
        for (int i = 0; i < spawnLimit; i++)
        {
            int idToSpawn = playersToSpawn[i];
            if (prefabLookUp.TryGetValue(idToSpawn, out GameObject prefabToSpawn))
            {
                Transform spawnPoint = defaultSpawnPoints[i];
                GameObject spawnedCharacter = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
                Vector3 currentPosition = spawnedCharacter.transform.position;
                Vector3 correctedPosition = new Vector3(currentPosition.x, currentPosition.y, 0f);
                spawnedCharacter.transform.position = correctedPosition;
                CharacterIdentity identity = spawnedCharacter.GetComponent<CharacterIdentity>();
                if (identity == null) identity = spawnedCharacter.AddComponent<CharacterIdentity>();
                identity.playerID = idToSpawn;
                spawnedCharacter.SetActive(true);
                if (idToSpawn == 0)
                {
                    AssignPlayerToCamera(spawnedCharacter.transform);
                    AssignPlayerToButton(spawnedCharacter);
                }
            }
        }
    }

    private void AssignPlayerToCamera(Transform playerTransform)
    {
        if (sceneCameraObject == null) return;
        CameraFollow cam1 = sceneCameraObject.GetComponent<CameraFollow>();
        if (cam1 != null) { cam1.player = playerTransform; return; }
        CameraFollowX cam2 = sceneCameraObject.GetComponent<CameraFollowX>();
        if (cam2 != null) { cam2.player = playerTransform; return; }
        CameraFollowXY cam3 = sceneCameraObject.GetComponent<CameraFollowXY>();
        if (cam3 != null) { cam3.player = playerTransform; return; }
    }

    private void AssignPlayerToButton(GameObject playerObject)
    {
        if (pushButton != null)
        {
            var playerScript = playerObject.GetComponent<PlayerController>(); 
            if (playerScript != null)
            {
                pushButton.onClick.RemoveAllListeners();
                pushButton.onClick.AddListener(playerScript.SpinAttack);
            }
        }
    }
    #endregion
}









// using UnityEngine;
// using System.Collections.Generic;
// using UnityEngine.UI; // این خط برای دکمه مرحله ۱ لازم است، بگذارید بماند
// using System.Linq; // برای توابع لیستی مثل OrderBy و Take
// using Random = UnityEngine.Random; // برای جلوگیری از تداخل با System.Random

// /// <summary>
// /// (تغییر یافته)
// /// این اسکریپت در تمام مراحلی که بازیکنان اسپاون می‌شوند (۱، ۳، ۵، ۶) قرار می‌گیرد
// /// و بر اساس مرحله، رفتار متفاوتی از خود نشان می‌دهد.
// /// </summary>
// public class StageCharacterSpawner : MonoBehaviour
// {
//     [Header("Character Mapping (مخصوص این مرحله)")]
//     public CharacterMapping[] characterMappings;

//     [Header("Dependencies (دوربین و دکمه)")]
//     // <<< --- تغییر اساسی: ما دیگر نوع اسکریپت را نمی‌خواهیم --- >>>
//     [Tooltip("آبجکت 'Main Camera' که اسکریپت دوربین (از هر نوعی) روی آن است")]
//     public GameObject sceneCameraObject; // <<< --- این جایگزین CameraFollowX شد
//     // <<< --- پایان تغییر --- >>>
//     [Tooltip("فقط برای مراحلی که دکمه Push دارند (مثل مرحله ۱)")]
//     public Button pushButton;

//     // <<< --- بخش جدید مخصوص مرحله ۳ --- >>>
//     [Header("Stage 3 - Tug of War Settings")]
//     [Tooltip("آبجکت پدر برای تیم A (بازیکنان این تیم فرزند این آبجکت می‌شوند)")]
//     public Transform teamAParent;
//     [Tooltip("آبجکت پدر برای تیم B")]
//     public Transform teamBParent;

//     [Tooltip("محل‌های اسپاون 5 بازیکن تیم A (زیرمجموعه teamAParent)")]
//     public Transform[] teamASpawnPoints;
//     [Tooltip("محل‌های اسپاون 5 بازیکن تیم B (زیرمجموعه teamBParent)")]
//     public Transform[] teamBSpawnPoints;
//     // <<< --- پایان بخش جدید --- >>>



//     // <<< --- ۴. این بخش جدید را اضافه کنید --- >>>
//     [Header("Stage 5 - Glass Bridge Settings")]
//     [Tooltip("اسکریپت QueueManager حاضر در صحنه را به اینجا بکشید")]
//     public QueueManager queueManager;
//     // <<< --- پایان بخش جدید --- >>>




//     [Header("Default Spawn Points")]
//     [Tooltip("برای مراحل عادی (مثل ۱، ۵، ۶) از این لیست استفاده می‌شود")]
//     public Transform[] defaultSpawnPoints;

//     // <<< --- این خط جدید اضافه شده است --- >>>
//     [Tooltip("اسکریپت TugOfWarController که در صحنه وجود دارد را به اینجا بکشید")]
//     public TugOfWarController tugOfWarController;
//     // <<< --- پایان بخش جدید --- >>>


//     // (دیکشنری و Awake بدون تغییر)
//     private Dictionary<int, GameObject> prefabLookUp;

//     // <<< --- این خط جدید را اضافه کنید --- >>>
//     // (ما از این برای بُر زدن لیست استفاده می‌کنیم)
//     private System.Random rng = new System.Random();

//     void Awake()
//     {
//         prefabLookUp = new Dictionary<int, GameObject>();
//         foreach (var mapping in characterMappings)
//         {
//             if (!prefabLookUp.ContainsKey(mapping.characterID))
//             {
//                 prefabLookUp.Add(mapping.characterID, mapping.characterPrefab);
//             }
//         }
//     }

//     void Start()
//     {
//         if (GameManager.Instance != null && GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
//         {
//             SpawnPlayers();
//         }
//     }

//     // <<< --- تغییر یافته --- >>>
//     /// <summary>
//     /// این تابع تشخیص می‌دهد در کدام مرحله هستیم و تابع اسپاون مربوطه را صدا می‌زند
//     /// </summary>
//     void SpawnPlayers()
//     {
//         // GameManager می‌داند که مرحله ۳، ایندکس 2 است
//         if (GameManager.Instance.currentStageIndex == 2)
//         {
//             // اگر در مرحله ۳ (طناب‌کشی) بودیم
//             SpawnForTugOfWar();
//         }
//         // <<< --- ۶. این بلاک if جدید را اضافه کنید --- >>>
//         else if (GameManager.Instance.currentStageIndex == 4 && queueManager != null) // مرحله ۵ (پل شیشه‌ای)
//         {
//             SpawnForQueue();
//         }
//         // <<< --- پایان بخش جدید --- >>>
//         else
//         {
//             SpawnForDefaultStages();
//         }
//     }


//     // <<< --- ۷. این تابع "جدید" را کامل اضافه کنید --- >>>
//     /// <summary>
//     /// این تابع بازیکنان را بر اساس لیست صف می‌سازد (مخصوص مرحله ۵)
//     /// </summary>
//     // <<< --- تابع مرحله ۵ (پل شیشه‌ای) اصلاح شد --- >>>
//     void SpawnForQueue()
//     {
//         // ... (کد قبلی شما برای گرفتن لیست و بُر زدن) ...
//         // ... (کد محدود کردن spawnLimit) ...
//         List<int> shuffledIDs = GameManager.Instance.activePlayerIDs.OrderBy(x => rng.Next()).ToList();
//         int spawnLimit = Mathf.Min(shuffledIDs.Count, queueManager.queueSlots.Count);
//         List<MonoBehaviour> spawnedQueue = new List<MonoBehaviour>();

//         for (int i = 0; i < spawnLimit; i++)
//         {
//             int idToSpawn = shuffledIDs[i];
//             Transform spawnSlot = queueManager.queueSlots[i];

//             if (prefabLookUp.TryGetValue(idToSpawn, out GameObject prefabToSpawn))
//             {
//                 GameObject spawnedCharacter = Instantiate(prefabToSpawn, spawnSlot.position, spawnSlot.rotation);
                
//                 // (تنظیم ID و فعال‌سازی)
//                 CharacterIdentity identity = spawnedCharacter.GetComponent<CharacterIdentity>();
//                 if (identity == null) identity = spawnedCharacter.AddComponent<CharacterIdentity>();
//                 identity.playerID = idToSpawn;
//                 spawnedCharacter.SetActive(true);

//                 // <<< --- تغییر: استفاده از تابع کمکی جدید --- >>>
//                 if (idToSpawn == 0)
//                 {
//                     AssignPlayerToCamera(spawnedCharacter.transform);
//                 }
//                 // <<< --- پایان تغییر --- >>>

//                 // (اضافه کردن به صف)
//                 MonoBehaviour controller = spawnedCharacter.GetComponent<PlayerClickMove>() as MonoBehaviour ?? spawnedCharacter.GetComponent<GlassBridgeAI>();
//                 if (controller != null) spawnedQueue.Add(controller);
//             }
//         }
//         queueManager.InitializeQueue(spawnedQueue);
//     }
    
    

//     // <<< --- تابع جدید --- >>>
//     /// <summary>
//     /// این تابع منطق اسپاون مخصوص طناب‌کشی (مرحله ۳) را اجرا می‌کند
//     /// </summary>
//     // <<< --- این تابع تغییرات اساسی داشته است --- >>>
//     // <<< --- این تابع را کامل جایگزین کنید --- >>>
//     void SpawnForTugOfWar()
//     {
//         // --- ۱. آماده‌سازی لیست‌ها ---
//         int playerID = 0;
//         bool playerIsActive = false;
//         // یک "کپی" از لیست GameManager می‌سازیم تا لیست اصلی دستکاری نشود
//         List<int> allActivePlayers = new List<int>(GameManager.Instance.activePlayerIDs);
//         List<int> matchParticipants = new List<int>(); // لیست نهایی 10 نفره

//         // --- ۲. بازیکن (ID 0) را پیدا کن و رزرو کن ---
//         if (allActivePlayers.Contains(playerID))
//         {
//             matchParticipants.Add(playerID);         // بازیکن را به لیست 10 نفره اضافه کن
//             allActivePlayers.Remove(playerID); // بازیکن را از لیست کپی حذف کن
//             playerIsActive = true;
//             Debug.Log("SPAWNER: Player (ID 0) found and reserved for the match.");
//         }
//         else
//         {
//             Debug.LogWarning("SPAWNER: Player (ID 0) was not in the active list!");
//         }

//         // --- ۳. بقیه 9 نفر (AI) را انتخاب کن ---
//         int teamSizePerSide = 5; // (تعداد بازیکن در هر تیم از کد TugOfWarController)
//         int totalPlayersNeeded = teamSizePerSide * 2; // ( 5 * 2 = 10 )
//         int aiNeeded = totalPlayersNeeded - matchParticipants.Count; // ( 10 - 1 = 9 نفر AI نیاز داریم )

//         // (مطمئن شو که به اندازه کافی AI داریم)
//         int aiToAddCount = Mathf.Min(aiNeeded, allActivePlayers.Count);

//         // (لیست AI ها را بُر می‌زنیم و 9 نفر اول را برمی‌داریم)
//         var randomAIs = allActivePlayers.OrderBy(x => rng.Next()).Take(aiToAddCount);
//         matchParticipants.AddRange(randomAIs);

//         // --- ۴. لیست نهایی (بازیکن + 9 AI) را دوباره بُر می‌زنیم ---
//         // (این کار باعث می‌شود تیم بازیکن (A یا B) کاملاً رندوم باشد)
//         List<int> playersToSpawn = matchParticipants.OrderBy(x => rng.Next()).ToList();
        
//         // --- ۵. چک کن بازیکن در کدام تیم افتاد ---
//         bool playerIsInTeamA = false; 
//         if(playerIsActive)
//         {
//             int playerIndex = playersToSpawn.IndexOf(playerID);
//             if(playerIndex >= 0 && playerIndex < teamSizePerSide) // (اگر ایندکس 0 تا 4 بود)
//             {
//                 playerIsInTeamA = true;
//             }
//         }
        
//         // --- ۶. شروع اسپاون کردن (بر اساس لیست جدید) ---
//         int spawnLimit = playersToSpawn.Count;
//         if (teamASpawnPoints.Length < 5 || teamBSpawnPoints.Length < 5)
//         {
//             Debug.LogError("تعداد نقاط اسپاون برای تیم A یا B کمتر از 5 است!");
//             return;
//         }

//         Debug.Log($"SPAWNER: Spawning {spawnLimit} participants. Player on Team A: {playerIsInTeamA}");

//         for (int i = 0; i < spawnLimit; i++)
//         {
//             int idToSpawn = playersToSpawn[i]; // <<<--- حالا از لیست 10 نفره جدید می‌خواند
            
//             if (!prefabLookUp.TryGetValue(idToSpawn, out GameObject prefabToSpawn))
//             {
//                 Debug.LogError($"!Prefab برای ID {idToSpawn} در StageCharacterSpawner این مرحله تعریف نشده است");
//                 continue;
//             }

//             GameObject spawnedCharacter;
//             Transform spawnPoint;

//             // 5 نفر اول -> تیم A
//             if (i < teamSizePerSide) // (i < 5)
//             {
//                 spawnPoint = teamASpawnPoints[i];
//                 spawnedCharacter = Instantiate(prefabToSpawn, teamAParent);
//                 spawnedCharacter.transform.localPosition = spawnPoint.localPosition;
//                 spawnedCharacter.transform.localRotation = spawnPoint.localRotation;
//             }
//             // 5 نفر بعدی -> تیم B
//             else
//             {
//                 spawnPoint = teamBSpawnPoints[i - teamSizePerSide]; // (i - 5)
//                 spawnedCharacter = Instantiate(prefabToSpawn, teamBParent);
//                 spawnedCharacter.transform.localPosition = spawnPoint.localPosition;
//                 spawnedCharacter.transform.localRotation = spawnPoint.localRotation;
//             }

//             // تنظیم ID و فعال‌سازی
//             CharacterIdentity identity = spawnedCharacter.GetComponent<CharacterIdentity>();
//             if (identity == null) identity = spawnedCharacter.AddComponent<CharacterIdentity>();
//             identity.playerID = idToSpawn;
//             spawnedCharacter.SetActive(true);

//             if(idToSpawn == 0) Debug.Log($"!!! SPAWNER: Player (ID 0) spawned with name {spawnedCharacter.name}");
//         }

//         // --- ۷. اطلاع دادن به کنترلر ---
//         if (tugOfWarController != null)
//         {
//             // (این کد را از پاسخ قبلی داریم که 1 فریم صبر می‌کند)
//             tugOfWarController.InitializeMatch(playerIsInTeamA); 
//         }
//         else
//         {
//             Debug.LogError("اسکریپت TugOfWarController در Spawner تنظیم نشده است!");
//         }
//     }


//     // <<< --- تابع اسپاون پیش‌فرض (مرحله ۱ و ۶) اصلاح شد --- >>>
//     void SpawnForDefaultStages()
//     {
//         List<int> playersToSpawn = GameManager.Instance.activePlayerIDs;
//         int spawnLimit = GetSpawnLimit(playersToSpawn.Count);

//         if (spawnLimit > defaultSpawnPoints.Length)
//         {
//             Debug.LogWarning($"تعداد بازیکنان ({spawnLimit}) از نقاط اسپاون پیش‌فرض ({defaultSpawnPoints.Length}) بیشتر است!");
//             spawnLimit = defaultSpawnPoints.Length;
//         }

//         for (int i = 0; i < spawnLimit; i++)
//         {
//             int idToSpawn = playersToSpawn[i];
//             if (prefabLookUp.TryGetValue(idToSpawn, out GameObject prefabToSpawn))
//             {
//                 Transform spawnPoint = defaultSpawnPoints[i];
//                 GameObject spawnedCharacter = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);


//                 // <<< --- ۲. این بخش جدید اضافه شد --- >>>
//                 // موقعیت فعلی رو بگیر
//                 Vector3 currentPosition = spawnedCharacter.transform.position;
//                 // یک موقعیت جدید بساز با Z=0
//                 Vector3 correctedPosition = new Vector3(currentPosition.x, currentPosition.y, 0f);
//                 // موقعیت جدید رو به بازیکن بده
//                 spawnedCharacter.transform.position = correctedPosition;
//                 // <<< --- پایان بخش جدید --- >>>

                
//                 CharacterIdentity identity = spawnedCharacter.GetComponent<CharacterIdentity>();
//                 if (identity == null) identity = spawnedCharacter.AddComponent<CharacterIdentity>();
//                 identity.playerID = idToSpawn;
//                 spawnedCharacter.SetActive(true);

//                 if (idToSpawn == 0)
//                 {
//                     // <<< --- تغییر: استفاده از تابع کمکی جدید --- >>>
//                     AssignPlayerToCamera(spawnedCharacter.transform);
//                     // <<< --- پایان تغییر --- >>>

//                     // (کد اتصال دکمه Push بدون تغییر باقی می‌ماند)
//                     if (pushButton != null)
//                     {
//                         var playerScript = spawnedCharacter.GetComponent<PlayerController>(); // <-- نام اسکریپت خودتان
//                         if (playerScript != null)
//                         {
//                             pushButton.onClick.RemoveAllListeners();
//                             pushButton.onClick.AddListener(playerScript.SpinAttack); // <-- نام تابع خودتان
//                         }
//                     }
//                 }
                
//             }
//         }
//     }


//     // <<< --- این تابع "جدید" و کمکی است --- >>>
//     /// <summary>
//     /// این تابع بازیکن اسپاون شده را به هر نوع اسکریپت دوربینی که پیدا کند، وصل می‌کند
//     /// </summary>
//     private void AssignPlayerToCamera(Transform playerTransform)
//     {
//         if (sceneCameraObject == null)
//         {
//             Debug.LogWarning("آبجکت دوربین (Scene Camera Object) در Spawner تنظیم نشده است.");
//             return;
//         }

//         // تلاش برای پیدا کردن اسکریپت نوع اول
//         CameraFollow cam1 = sceneCameraObject.GetComponent<CameraFollow>();
//         if (cam1 != null)
//         {
//             cam1.player = playerTransform;
//             Debug.Log("بازیکن به دوربین (CameraFollow) متصل شد.");
//             return; // پیدا شد، خارج شو
//         }

//         // تلاش برای پیدا کردن اسکریپت نوع دوم
//         CameraFollowX cam2 = sceneCameraObject.GetComponent<CameraFollowX>();
//         if (cam2 != null)
//         {
//             cam2.player = playerTransform;
//             Debug.Log("بازیکن به دوربین (CameraFollowX) متصل شد.");
//             return; // پیدا شد، خارج شو
//         }

//         // تلاش برای پیدا کردن اسکریپت نوع سوم
//         CameraFollowXY cam3 = sceneCameraObject.GetComponent<CameraFollowXY>();
//         if (cam3 != null)
//         {
//             cam3.player = playerTransform;
//             Debug.Log("بازیکن به دوربین (CameraFollowXY) متصل شد.");
//             return; // پیدا شد، خارج شو
//         }

//         Debug.LogError($"هیچ اسکریپت دوربین معتبری روی آبجکت {sceneCameraObject.name} پیدا نشد!");
//     }
//     // <<< --- پایان تابع جدید --- >>>
    


//     // (این تابع بدون تغییر باقی می‌ماند)
//     private int GetSpawnLimit(int currentActivePlayers)
//     {
//         int spawnLimit = currentActivePlayers;

//         // قانون مرحله ۳: نمایش حداکثر ۱۰ بازیکن
//         if (GameManager.Instance.currentStageIndex == 2) // مرحله ۳ (index 2)
//         {
//             spawnLimit = Mathf.Min(10, currentActivePlayers);
//         }

//         return spawnLimit;
//     }
// }

