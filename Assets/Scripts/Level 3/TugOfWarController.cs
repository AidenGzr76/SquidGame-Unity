using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TugOfWarController : MonoBehaviour
{
    [Header("Team Parents")]
    public Transform teamAParent;
    public Transform teamBParent;

    [Header("Game References")]
    public Transform ropeParent;
    public UIManager uiManager;

    [Header("Platform Edges")]
    public Transform leftPlatformEdge;
    public Transform rightPlatformEdge;

    [Header("Settings")]
    [Tooltip("حداکثر اندازه هر تیم (توسط Spawner خوانده می‌شود)")]
    public int maxTeamSize = 5; 
    public float moveSpeed = 1f;
    public float endGameDelay = 2.0f;
    
    // --- متغیرهای داخلی ---
    private int actualTeamSize; // Spawner این مقدار را به ما می‌دهد
    private bool isPlayerOnTeamA = true; // Spawner این را به ما می‌دهد

    private List<Transform> teamA_Roots = new List<Transform>();
    private List<Transform> teamB_Roots = new List<Transform>();
    private Transform player_Root;
    private PlayerControlledUnit player_Controller;
    private List<PlayerUnit> teamA_AI_Units = new List<PlayerUnit>();
    private List<PlayerUnit> teamB_AI_Units = new List<PlayerUnit>();
    
    private List<Transform> teamA_ActivePlayers = new List<Transform>();
    private List<Transform> teamB_ActivePlayers = new List<Transform>();
    
    private Vector3 initialRopePosition;
    private Vector3 initialTeamAPosition; // برای ریست کردن
    private Vector3 initialTeamBPosition; // برای ریست کردن
    private Dictionary<Transform, Vector3> initialLocalPositions = new Dictionary<Transform, Vector3>();
    
    private bool gameIsOver = false;
    private Coroutine simulationCoroutine;

    private BackgroundMusic music;


    // (Awake خالی می‌ماند، چون Spawner ارجاع دارد)
    // void Awake() { }

    // (به رویداد Revive گوش می‌دهد)
    void OnEnable() { GameManager.OnStageRespawn += ResetMatch; } 
    void OnDisable() { GameManager.OnStageRespawn -= ResetMatch; }

    void Start()
    {
        // موقعیت‌های اولیه را برای ریست کردن ذخیره کن
        initialRopePosition = ropeParent.position;
        initialTeamAPosition = teamAParent.position;
        initialTeamBPosition = teamBParent.position;

        music = FindFirstObjectByType<BackgroundMusic>();

        // <<< ---  راه‌حل اینجاست --- >>>
        // اگر در حالت تمرینی هستیم، Spawner وجود ندارد
        // پس باید خودمان کنترلر را به صورت دستی راه‌اندازی کنیم
        if (GameManager.Instance != null && GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            // شما گفتید بازیکنان به صورت پیش‌فرض در صحنه هستند
            // پس ما فقط تابع را صدا می‌زنیم تا آن‌ها را پیدا کند و بازی را شروع کند
            Debug.LogWarning("PRACTICE MODE: Manually starting TugOfWarController...");
            
            // این مقادیر را بر اساس صحنه تمرینی خودتان تنظیم کنید
            // (true = بازیکن در تیم A است، maxTeamSize = اندازه تیم)
            InitializeMatch(true, maxTeamSize); 
        }
        // <<< --- پایان راه‌حل --- >>>

    }

    /// <summary>
    /// این تابع توسط StageCharacterSpawner صدا زده می‌شود
    /// </summary>
    public void InitializeMatch(bool playerTeamA, int teamSize)
    {
        this.isPlayerOnTeamA = playerTeamA;
        this.actualTeamSize = teamSize; // اندازه تیم را از Spawner می‌گیریم
        StartCoroutine(DelayedInitialize());
    }
    
    private IEnumerator DelayedInitialize()
    {
        yield return null; 
        Debug.Log($"--- CONTROLLER (Delayed): Starting match. Player on Team A: {isPlayerOnTeamA}, Team Size: {actualTeamSize} ---");
        CacheAllTeamMembers(); 
        ResetMatch();
    }
    
    void CacheAllTeamMembers()
    {
        teamA_Roots.Clear(); teamB_Roots.Clear();
        teamA_AI_Units.Clear(); teamB_AI_Units.Clear();
        player_Root = null; player_Controller = null;
        initialLocalPositions.Clear();

        ProcessTeamCache(teamAParent, teamA_Roots, teamA_AI_Units);
        ProcessTeamCache(teamBParent, teamB_Roots, teamB_AI_Units);
        
        Debug.Log($"Cache Complete: {teamA_AI_Units.Count} AI in A, {teamB_AI_Units.Count} AI in B, Player Found: {player_Root != null}");
    }

    void ProcessTeamCache(Transform parent, List<Transform> rootList, List<PlayerUnit> unitList)
    {
        foreach (Transform prefabRoot in parent)
        {
            CharacterIdentity identity = prefabRoot.GetComponent<CharacterIdentity>();
            if (identity == null) continue; 

            if (!initialLocalPositions.ContainsKey(prefabRoot))
            {
                initialLocalPositions.Add(prefabRoot, prefabRoot.localPosition);
            }

            if (identity.playerID == 0)
            {
                player_Root = prefabRoot;
                player_Controller = prefabRoot.GetComponentInChildren<PlayerControlledUnit>(true);
            }
            else
            {
                PlayerUnit aiUnit = prefabRoot.GetComponentInChildren<PlayerUnit>(true);
                if (aiUnit != null)
                {
                    rootList.Add(prefabRoot);
                    unitList.Add(aiUnit);
                }
            }
        }
    }


    // در TugOfWarController.cs

    public void ResetMatch()
    {
        Debug.Log("--- RESETTING TUG OF WAR MATCH (Called by Respawn or Start) ---");
        if (simulationCoroutine != null) StopCoroutine(simulationCoroutine);
        gameIsOver = false;

        // ۱. ریست کردن موقعیت‌های جهانی (طناب و Parent تیم‌ها)
        ropeParent.position = initialRopePosition;
        teamAParent.position = initialTeamAPosition;
        teamBParent.position = initialTeamBPosition;

        // ۲. فعال کردن بازیکن (و وصل کردن دوباره به Parent)
        if (player_Root != null)
        {
            // <<< --- اصلاح حیاتی اینجاست --- >>>
            // بازیکن را "قبل" از ریست کردن موقعیت، به تیمش برگردان
            if (isPlayerOnTeamA)
                player_Root.SetParent(teamAParent);
            else
                player_Root.SetParent(teamBParent);
            // <<< --- پایان اصلاح --- >>>
            
            ResetPlayer(player_Root, player_Controller); // حالا localPosition به درستی تنظیم می‌شود
            player_Root.transform.localScale = isPlayerOnTeamA ? Vector3.one : new Vector3(-1, 1, 1);
            player_Root.gameObject.SetActive(true);
        }

        // ۳. فعال کردن AI های تیم A (و وصل کردن دوباره به Parent)
        for (int i = 0; i < teamA_Roots.Count; i++)
        {
            Transform root = teamA_Roots[i];
            PlayerUnit unit = teamA_AI_Units[i];
            if (root == null || unit == null) continue;
            
            // <<< --- اصلاح حیاتی اینجاست --- >>>
            root.SetParent(teamAParent); 
            // <<< --- پایان اصلاح --- >>>

            ResetPlayer(root, unit);
            root.transform.localScale = Vector3.one;
            root.gameObject.SetActive(true); 
        }

        // ۴. فعال کردن AI های تیم B (و وصل کردن دوباره به Parent)
        for (int i = 0; i < teamB_Roots.Count; i++)
        {
            Transform root = teamB_Roots[i];
            PlayerUnit unit = teamB_AI_Units[i];
            if (root == null || unit == null) continue;

            // <<< --- اصلاح حیاتی اینجاست --- >>>
            root.SetParent(teamBParent);
            // <<< --- پایان اصلاح --- >>>

            ResetPlayer(root, unit);
            root.transform.localScale = new Vector3(-1, 1, 1);
            root.gameObject.SetActive(true); 
        }

        PopulateActivePlayerList(); 
        simulationCoroutine = StartCoroutine(SimulateTugOfWar());
    }
    
    // (بقیه توابع شما تا EndGameRoutine همگی درست و کامل هستند)
    // ... ResetPlayer, PopulateActivePlayerList, SimulateTugOfWar, PullTeams ...
    // ... CalculateTeamPower, CheckForFallenPlayers, TriggerFall, CheckForWinner ...

    void ResetPlayer(Transform prefabRoot, PlayerUnit unit) 
    {
        if (initialLocalPositions.ContainsKey(prefabRoot))
            prefabRoot.localPosition = initialLocalPositions[prefabRoot];
        if (unit != null) unit.ResetState();
    }
    void ResetPlayer(Transform prefabRoot, PlayerControlledUnit controlledUnit)
    {
        if (initialLocalPositions.ContainsKey(prefabRoot))
            prefabRoot.localPosition = initialLocalPositions[prefabRoot];
        if (controlledUnit != null) controlledUnit.ResetState();
    }
    void PopulateActivePlayerList()
    {
        teamA_ActivePlayers.Clear();
        teamB_ActivePlayers.Clear();
        if (player_Root != null && player_Root.gameObject.activeSelf)
        {
            if (isPlayerOnTeamA) teamA_ActivePlayers.Add(player_Root);
            else teamB_ActivePlayers.Add(player_Root);
        }
        foreach(var root in teamA_Roots)
            if(root.gameObject.activeSelf) teamA_ActivePlayers.Add(root);
        foreach(var root in teamB_Roots)
            if(root.gameObject.activeSelf) teamB_ActivePlayers.Add(root);
    }
    IEnumerator SimulateTugOfWar()
    {
        while (!gameIsOver)
        {
            CheckForFallenPlayers();
            if (gameIsOver) yield break;
            int totalPowerA = CalculateTeamPower(teamA_ActivePlayers);
            int totalPowerB = CalculateTeamPower(teamB_ActivePlayers);
            if (totalPowerA > totalPowerB)
                PullTeams(teamBParent, teamAParent, totalPowerA - totalPowerB);
            else if (totalPowerB > totalPowerA)
                PullTeams(teamAParent, teamBParent, totalPowerB - totalPowerA);
            yield return new WaitForSeconds(0.1f);
        }
    }
    void PullTeams(Transform loserTeam, Transform winnerTeam, float powerDifference)
    {
        float pullMagnitude = Mathf.Clamp(powerDifference / 500f, 0.05f, 1f);
        Vector3 direction = (winnerTeam.position - loserTeam.position).normalized;
        Vector3 moveVector = direction * pullMagnitude * moveSpeed * Time.deltaTime;
        loserTeam.position += moveVector;
        winnerTeam.position += moveVector;
        ropeParent.position += moveVector;
    }
    int CalculateTeamPower(List<Transform> playerList)
    {
        int total = 0;
        foreach (Transform playerRoot in playerList)
        {
            PlayerUnit unit = playerRoot.GetComponentInChildren<PlayerUnit>();
            if (unit != null && !unit.isFalling) { total += unit.power; continue; }
            PlayerControlledUnit controlledUnit = playerRoot.GetComponentInChildren<PlayerControlledUnit>();
            if (controlledUnit != null && !controlledUnit.isFalling) { total += controlledUnit.power; }
        }
        return total;
    }
    void CheckForFallenPlayers()
    {
        if (gameIsOver) return;
        if (teamA_ActivePlayers.Count > 0)
        {
            Transform frontPlayerA = teamA_ActivePlayers.OrderByDescending(p => p.position.x).FirstOrDefault();
            if (frontPlayerA != null && frontPlayerA.position.x > leftPlatformEdge.position.x)
            {
                TriggerFall(frontPlayerA);
                teamA_ActivePlayers.Remove(frontPlayerA);
                CheckForWinner();
            }
        }
        if (teamB_ActivePlayers.Count > 0)
        {
            Transform frontPlayerB = teamB_ActivePlayers.OrderBy(p => p.position.x).FirstOrDefault();
            if (frontPlayerB != null && frontPlayerB.position.x < rightPlatformEdge.position.x)
            {
                TriggerFall(frontPlayerB);
                teamB_ActivePlayers.Remove(frontPlayerB);
                CheckForWinner();
            }
        }
    }
    void TriggerFall(Transform playerRoot)
    {
        PlayerUnit unit = playerRoot.GetComponentInChildren<PlayerUnit>();
        if (unit != null) { unit.FallIntoPit(); return; }
        PlayerControlledUnit controlledUnit = playerRoot.GetComponentInChildren<PlayerControlledUnit>();
        if (controlledUnit != null) { controlledUnit.FallIntoPit(); }
    }
    void CheckForWinner()
    {
        bool teamAWon = teamB_ActivePlayers.Count == 0 && teamA_ActivePlayers.Count > 0;
        bool teamBWon = teamA_ActivePlayers.Count == 0 && teamB_ActivePlayers.Count > 0;
        if (teamAWon || teamBWon)
        {
            gameIsOver = true;
            StartCoroutine(EndGameRoutine(teamAWon));
        }
    }

    // <<< --- تابع EndGameRoutine (منطق نهایی بازی) --- >>>
    private IEnumerator EndGameRoutine(bool teamAWon)
    {
        yield return new WaitForSeconds(endGameDelay);

        if (music != null) music.PauseMusic();

        // ۱. تشخیص برد/باخت بازیکن
        bool didPlayerWin = false;
        bool playerWasInMatch = (player_Root != null); // آیا بازیکن اصلاً اسپاون شده بود؟

        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            if (playerWasInMatch && isPlayerOnTeamA && teamAWon) didPlayerWin = true;
            else if (playerWasInMatch && !isPlayerOnTeamA && !teamAWon) didPlayerWin = true;
        }
        else // حالت MainFlow
        {
            if (playerWasInMatch)
            {
                if (isPlayerOnTeamA && teamAWon) didPlayerWin = true;
                else if (!isPlayerOnTeamA && !teamAWon) didPlayerWin = true;
            }
            else if (GameManager.Instance.activePlayerIDs.Contains(0))
            {
                uiManager.uiAudioSource.PlayOneShot(uiManager.WinEffect);
    
                didPlayerWin = true; // بازیکن امن بوده
                
            }
        }

        // ۲. مدیریت حالت تمرینی
        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            if (didPlayerWin) uiManager.ShowWinPanel();
            else uiManager.ShowLosePanel();
            yield break;
        }

        // --- ۳. مدیریت حالت MainFlow ---

        List<Transform> losingTeamRoots = teamAWon ? teamB_Roots : teamA_Roots;
        
        Debug.Log($"تعداد {losingTeamRoots.Count} نفر بازنده از GameManager حذف می‌شوند.");
        foreach (Transform loserRoot in losingTeamRoots)
        {
            if (loserRoot == null) continue;
            CharacterIdentity identity = loserRoot.GetComponent<CharacterIdentity>();
            if (identity == null) continue;
            if (identity.playerID != 0)
            {
                GameManager.Instance.EliminatePlayer(identity.playerID);
            }
        }
        
        if (didPlayerWin) 
        {
            Debug.Log("تیم بازیکن برنده شد یا امن بود!");
            GameManager.Instance.StageComplete(); 
        }
        else
        {
            Debug.Log("تیم بازیکن باخت!");
            uiManager.ShowLosePanel(); 
        }
    }
}