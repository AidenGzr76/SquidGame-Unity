using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PlayerClickMove : MonoBehaviour
{
    #region Public Variables
    [Header("Sound & SFX")]
    public Gender gender;

    [Header("Move & Jump")]
    public float moveSpeed = 5f;
    public float jumpScale = 1.3f;
    public float scaleSmooth = 5f;
    public float maxJumpDistance = 3f;

    [Header("Win Condition")]
    public float winXPosition = 10f;

    [Header("Platform Check")]
    public LayerMask platformLayer;
    public float checkRadius = 0.1f;

    [Header("Queue Movement")]
    public float walkInQueueSpeed = 3f;
    public string walkingBoolName = "isWalking";

    [Header("System Connections")]
    public UIManager uiManager;
    public Animator animator;

    [Header("Respawn Settings")]
    public Transform resetPosition; // 🔹 پوزیشن دلخواه بازگشت
    #endregion

    #region Private Variables
    private AudioSource audioSource;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isFalling = false;
    private Vector3 startPosition;
    private Vector3 jumpStartPosition;
    private float journeyLength;
    private Vector3 normalScale;
    public float delay = 0.5f;
    private bool hasLeftStartingQueue = false;
    private bool isMyTurn = false;
    #endregion

    #region Unity Lifecycle
    void OnEnable()
    {
        GameManager.OnFeatureActivated += HandleFeatureActivation;
        GameManager.OnStageRespawn += Respawn;
    }

    void OnDisable()
    {
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
        GameManager.OnStageRespawn -= Respawn;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        normalScale = transform.localScale;
        startPosition = transform.position;
        targetPosition = startPosition;
        Respawn();
    }

    void Update()
    {
        if (isMoving || isFalling)
        {
            HandleJump();
            return;
        }

        Transform mySlot = QueueManager.Instance?.GetMyTargetSlot(this);
        if (!hasLeftStartingQueue && mySlot != null && Vector3.Distance(transform.position, mySlot.position) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, mySlot.position, walkInQueueSpeed * Time.deltaTime);
            if (animator) animator.SetBool(walkingBoolName, true);
            isMyTurn = false;
        }
        else
        {
            if (animator) animator.SetBool(walkingBoolName, false);
            isMyTurn = (!hasLeftStartingQueue && QueueManager.Instance?.characterQueue.Count > 0 &&
                        QueueManager.Instance.characterQueue[0] == this) || hasLeftStartingQueue;
        }

        if (isMyTurn && (Input.GetMouseButtonDown(0) ||
                         (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleClickInput();
        }
    }
    #endregion

    #region Feature Activation
    private void HandleFeatureActivation(FeatureData data)
    {
        if (!isMyTurn || isMoving || isFalling) return;

        switch (data.type)
        {
            case FeatureData.FeatureType.GlassBridge_AddTime:
                Simple_Timer timer = FindAnyObjectByType<Simple_Timer>();
                if (timer != null) timer.AddTime(data.value);
                break;
            case FeatureData.FeatureType.GlassBridge_RemoveWrong:
                ActivateRemoveWrongPlatform();
                break;
        }
    }

    private void ActivateRemoveWrongPlatform()
    {
        List<Transform> nextPlatforms = FindNextPlatformPair();
        if (nextPlatforms.Count != 2) return;

        Transform breakablePlatform = nextPlatforms.FirstOrDefault(p => p.CompareTag("Breakable"));
        if (breakablePlatform != null) StartCoroutine(MakePlatformFall(breakablePlatform));
    }

    private List<Transform> FindNextPlatformPair()
    {
        float currentX = transform.position.x;
        float lowestNextX = Mathf.Infinity;
        Collider2D[] potentialPlatforms = Physics2D.OverlapCircleAll(transform.position, maxJumpDistance + 1.0f, platformLayer);
        List<Transform> candidates = new List<Transform>();
        foreach (var p in potentialPlatforms)
        {
            if (p.gameObject.activeSelf && p.transform.position.x > currentX + 0.1f)
            {
                candidates.Add(p.transform);
                lowestNextX = Mathf.Min(lowestNextX, p.transform.position.x);
            }
        }

        if (candidates.Count == 0) return new List<Transform>();
        return candidates.Where(p => Mathf.Abs(p.position.x - lowestNextX) < 0.1f).ToList();
    }

    private IEnumerator MakePlatformFall(Transform platform)
    {
        if (platform == null || !platform.gameObject.activeSelf) yield break;
        platform.tag = "Untagged";
        Vector3 startPos = platform.position;
        Vector3 startScale = platform.localScale;
        float fallDuration = 0.8f;
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            platform.position = startPos + Vector3.down * 6f * elapsed;
            platform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        platform.gameObject.SetActive(false);
    }
    #endregion

    #region Jump Logic
    private void HandleClickInput()
    {
        Vector3 clickPos;
        if (Input.touchCount > 0)
            clickPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
        else
            clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        clickPos.z = 0f;

        Vector3 from = transform.position;
        Vector3 dir = clickPos - from;
        float dist = dir.magnitude;

        if (dist > maxJumpDistance)
            targetPosition = from + dir.normalized * maxJumpDistance;
        else
            targetPosition = clickPos;

        targetPosition.z = 0f;

        // 🔹 شرط جلو بودن حذف شد (اجازه کلیک در عقب)
        Collider2D targetCollider = Physics2D.OverlapPoint(targetPosition, platformLayer);
        if (targetCollider == null)
        {
            Debug.Log("Invalid jump target (no platform).");
            return;
        }

        jumpStartPosition = transform.position;
        journeyLength = Vector3.Distance(jumpStartPosition, targetPosition);
        isMoving = true;

        AudioClip jumpSound = GlassBridgeSoundManager.Instance.GetRandomJumpSound(gender);
        if (jumpSound != null) audioSource.PlayOneShot(jumpSound);

        if (!hasLeftStartingQueue)
        {
            QueueManager.Instance?.CharacterStartedJumping(this);
            hasLeftStartingQueue = true;
        }

        if (animator) animator.SetBool("jump", true);
    }

    void HandleJump()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        float distCovered = Vector3.Distance(jumpStartPosition, transform.position);
        float progress = journeyLength > 0.001f ? distCovered / journeyLength : 1f;
        float scaleFactor = Mathf.Sin(progress * Mathf.PI) * (jumpScale - 1f) + 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, normalScale * scaleFactor, Time.deltaTime * scaleSmooth);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            isMoving = false;
            if (animator) animator.SetBool("jump", false);
            transform.localScale = normalScale;
            CheckPlatform();
        }
    }
    #endregion

    #region Platform Checking / Respawn
    void CheckPlatform()
    {
        if (isFalling) return;
        Collider2D hit = Physics2D.OverlapCircle((Vector2)transform.position + Vector2.down * 0.3f, 0.2f, platformLayer);

        if (hit != null)
        {
            if (hit.CompareTag("Breakable"))
                StartCoroutine(CallWithDelayLose(transform, hit.transform, 1.0f, 4f));
            else if (hit.CompareTag("FinishPlatform"))
                StartCoroutine(CallWithDelayWin());
        }
        else
        {
            StartCoroutine(CallWithDelayLose(transform, transform, 1.0f, 4f));
        }
    }

    public void Respawn()
    {
        StopAllCoroutines();

        // 🔹 فعال‌سازی کامل پیش از ریست انیماتور
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (!enabled)
            enabled = true;

        // 🔹 اطمینان از وجود و فعال بودن انیماتور
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.speed = 1f; // بعد از توقف موقت دوباره فعالش کن

        // 🔹 ریست موقعیت و وضعیت
        transform.position = startPosition;
        transform.localScale = normalScale;
        isMoving = false;
        isFalling = false;
        hasLeftStartingQueue = false;
        isMyTurn = false;
        targetPosition = transform.position;

        // ✅ برگرداندن بازیکن به صف در QueueManager
        if (QueueManager.Instance != null)
        {
            // اگر در صف وجود ندارد، دوباره اضافه‌اش کن
            if (!QueueManager.Instance.characterQueue.Contains(this))
            {
                QueueManager.Instance.characterQueue.Add(this);

                // جایگاه هدف جدید
                Transform newSlot = QueueManager.Instance.GetMyTargetSlot(this);
                QueueWalker walker = GetComponent<QueueWalker>();
                if (walker != null && newSlot != null)
                    walker.MoveToSlot(newSlot);
            }
        }

        // 🔹 ریست ایمن انیماتور (الزاماً بعد از فعال بودن)
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.SetBool("jump", false);
            animator.SetBool("falling", false);
            animator.SetBool(walkingBoolName, false);
            animator.Play("Idle", 0, 0f);
        }

        // 🔹 تعیین مجدد موقعیت در صف (در صورت وجود QueueManager)
        Transform mySlot = QueueManager.Instance?.GetMyTargetSlot(this);
        if (mySlot != null)
        {
            transform.position = mySlot.position;
            Debug.Log($"Respawned player repositioned to slot: {mySlot.name}");
        }

        // 🔹 بررسی نوبت
        if (QueueManager.Instance != null && QueueManager.Instance.characterQueue.Count > 0)
        {
            isMyTurn = QueueManager.Instance.characterQueue[0] == this;
        }

        Debug.Log("✅ Player fully respawned and animator reactivated.");
    }




    #endregion

    #region Coroutines
    // IEnumerator CallWithDelayWin()
    // {
    //     this.enabled = false;
    //     yield return new WaitForSeconds(delay);
    //     uiManager.ShowWinPanel();
    // }

    IEnumerator CallWithDelayWin()
    {
        this.enabled = false;

        // --- <<< شروع تغییرات اصلی >>> ---
        if (GameManager.Instance.currentMode == GameManager.GameMode.Practice)
        {
            yield return new WaitForSeconds(1.5f);
            // در حالت تمرینی، فقط پنل برد رو نشون بده
            uiManager.ShowWinPanel();
        }
        else // در حالت بازی اصلی (MainFlow)
        {
            uiManager.uiAudioSource.PlayOneShot(uiManager.WinEffect);
            yield return new WaitForSeconds(delay);
        
            // ۱. از داور مرحله می‌پرسیم چند نفر حذف شدن
            int actualEliminations = 0;
            if (Stage5_Manager.Instance != null)
            {
                actualEliminations = Stage5_Manager.Instance.GetEliminatedCount();
            }

            Debug.Log($"Stage 5 Won. Reporting {actualEliminations} AI eliminations.");

            // ۲. تعداد واقعی رو به گیم منیجر گزارش می‌دیم
            GameManager.Instance.StageComplete();
        }
        // --- <<< پایان تغییرات اصلی >>> ---
    }


    IEnumerator CallWithDelayLose(Transform player, Transform platform, float fallDuration, float fallSpeed)
    {
        if (isFalling) yield break;
        isFalling = true; // <-- این فلگ برای متوقف کردن Update کافیه

        AudioClip screamSound = GlassBridgeSoundManager.Instance.GetRandomScream(gender);
        if (screamSound != null)
            audioSource.PlayOneShot(screamSound);

        if (platform != player && platform.CompareTag("Breakable"))
            StartCoroutine(MakePlatformFall(platform));

        yield return new WaitForSeconds(0.2f);

        if (animator != null)
            animator.SetBool("falling", true);

        Vector3 playerStartPos = player.position;
        Vector3 playerStartScale = player.localScale;
        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            player.position = playerStartPos + Vector3.down * fallSpeed * elapsed;
            player.localScale = Vector3.Lerp(playerStartScale, Vector3.zero, elapsed / fallDuration);
            yield return null;
        }

        // <<< --- اصلاح کلیدی اینجاست --- >>>
        // اسکریپت را دیگر غیرفعال نکن!
        // enabled = false; // <<<--- این خط را "پاک" یا "کامنت" کن

        isMoving = false; // این درسته
        if (animator != null)
            animator.speed = 0f; // این درسته، چون Respawn() ریستش می‌کنه

        // نمایش منوی باخت
        uiManager.ShowLosePanel();
    }


    #endregion
}
