using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Diagnostics;

public class GlassBridgeAI : MonoBehaviour
{

    [Header("Sound & SFX")]
    public Gender gender; // Set the AI's gender in the Inspector
    private AudioSource audioSource;

    // این enum وضعیت‌های مختلف هوش مصنوعی را تعریف می‌کند
    private enum AIState { WaitingInQueue, MovingInQueue, DecidingToJump, Jumping, Fallen, Won }
    private AIState currentState = AIState.WaitingInQueue;

    [Header("Jump Behaviour")]
    public float moveSpeed = 5f;
    public float walkInQueueSpeed = 3f;
    public float jumpScale = 1.3f;
    public float scaleSmooth = 5f;

    [Header("AI Decision Making")]
    public float minThinkTime = 0.8f;
    public float maxThinkTime = 2.5f;

    [Header("Game State")]
    public Transform finishLine;
    public LayerMask platformLayer;
    public LayerMask characterLayer;

    [Header("Animation & Visuals")]
    public Animator animator;
    public string walkingBoolName = "isWalking";
    public string jumpBoolName = "jump";
    public string fallBoolName = "falling";
    private Vector3 normalScale;
    
    // --- Private ---
    private QueueManager queueManager;
    private Coroutine currentActionCoroutine;
    private bool isFalling = false;


    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // Add this line

        normalScale = transform.localScale;
        if (!animator) animator = GetComponentInChildren<Animator>();
        queueManager = QueueManager.Instance;

        // 👇 این خط باعث میشه در فریم اول همه‌چیز با سرعت طبیعی باشه
        StartCoroutine(EnsureNormalTimeScale());
    }

    private IEnumerator EnsureNormalTimeScale()
    {
        // صبر می‌کنه تا Unity کاملاً Scene رو راه‌اندازی کنه
        yield return new WaitForEndOfFrame();
        Time.timeScale = 1f;
    }


    void Update()
    {

        if (Time.timeScale < 0.5f) // (از 0.5 استفاده می‌کنیم تا مطمئن‌تر باشه)
        {
            if (animator) animator.SetBool(walkingBoolName, false); // انیمیشن راه رفتن رو هم متوقف کن
            return; // از تابع Update خارج شو
        }
        // <<< --- پایان بخش جدید --- >>>

        if (currentState == AIState.Fallen || currentState == AIState.Won) return;

        // --- مغز اصلی مبتنی بر وضعیت ---
        switch (currentState)
        {
            case AIState.WaitingInQueue:
                DecideNextActionInQueue();
                break;
            
            case AIState.DecidingToJump:
                if (currentActionCoroutine == null)
                {
                    currentActionCoroutine = StartCoroutine(DecideAndJumpCoroutine());
                }
                break;
        }
    }

    void DecideNextActionInQueue()
    {
        Transform targetSlot = queueManager.GetMyTargetSlot(this);
        if (targetSlot == null) return; 

        if (Vector3.Distance(transform.position, targetSlot.position) > 0.1f)
        {
            currentState = AIState.MovingInQueue;
            if (currentActionCoroutine != null) StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = StartCoroutine(MoveToTargetCoroutine(targetSlot.position, walkInQueueSpeed, walkingBoolName));
        }
        else
        {
            if (queueManager.GetMyTargetSlot(this) == queueManager.queueSlots[0])
            {
                currentState = AIState.DecidingToJump;
            }
        }
    }

    private IEnumerator MoveToTargetCoroutine(Vector3 targetPosition, float speed, string animBool)
    {
        if (animator) animator.SetBool(animBool, true);
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
        if (animator) animator.SetBool(animBool, false);

        currentState = AIState.WaitingInQueue;
        currentActionCoroutine = null;
    }

    private IEnumerator DecideAndJumpCoroutine()
    {
        float thinkTime = Random.Range(minThinkTime, maxThinkTime);
        yield return new WaitForSeconds(thinkTime);

        Transform targetPlatform = FindNextJumpTarget();

        if (targetPlatform != null)
        {
            queueManager.CharacterStartedJumping(this);
            currentState = AIState.Jumping;
            yield return StartCoroutine(JumpToTargetCoroutine(targetPlatform.position));
            CheckLanding();
        }
        else
        {
            yield return new WaitForSeconds(1f);
            currentState = AIState.DecidingToJump;
        }
        currentActionCoroutine = null;
    }

    

    // --- تابع CheckLanding اصلاح شد ---
    private void CheckLanding()
    {
        if (isFalling) return; // اگر در حال سقوط است، بررسی را متوقف کن    

        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.2f, platformLayer);

        if (hit != null)
        {
            // --- پخش صدای فرود با احتمال ۸۰ درصد ---
            if (hit.CompareTag("Breakable") || hit.CompareTag("Unbreakable"))
            {
                if (Random.value <= 0.8f) // 80% شانس پخش صدا
                {
                    AudioClip landingSound = GlassBridgeSoundManager.Instance.GetRandomLandingSound();
                    if (landingSound != null)
                    {
                        audioSource.PlayOneShot(landingSound);
                    }
                }
            }
            // ----------------------------------------
        
            if (hit.CompareTag("Breakable"))
            {
                StartCoroutine(FallCoroutine(hit.transform));
            }
            if (hit.CompareTag("FinishPlatform"))
            {
                currentState = AIState.Won;
                Debug.Log($"{gameObject.name} has won!");
                StartCoroutine(MoveToFinalPositionAndWin(hit.GetComponent<BoxCollider2D>()));
                return;
            }
            else
            {
                // --- این خط جدید و کلیدی است ---
                // اگر سالم فرود آمد، به حالت تصمیم‌گیری برای پرش بعدی برمی‌گردد
                currentState = AIState.DecidingToJump;
            }
        }
        else
        {
            StartCoroutine(FallCoroutine(null));
        }
    }



        // --- تابع جدید برای حرکت نهایی و برنده شدن ---
    private IEnumerator MoveToFinalPositionAndWin(BoxCollider2D finishCollider)
    {
        if (isFalling) yield break; // اگر در حال سقوط است، این کار را متوقف کن

        // ۱. یک نقطه تصادفی داخل کلایدر سکوی نهایی پیدا کن
        Bounds bounds = finishCollider.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        Vector3 finalPosition = new Vector3(randomX, randomY, transform.position.z);

        // ۲. با انیمیشن راه رفتن به سمت آن نقطه حرکت کن
        if (animator) animator.SetBool(walkingBoolName, true);
        while (Vector3.Distance(transform.position, finalPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, finalPosition, walkInQueueSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = finalPosition;
        if (animator) animator.SetBool(walkingBoolName, false);
        
        Debug.Log($"{gameObject.name} has won!");
    }


    private IEnumerator FallCoroutine(Transform platform)
    {
        currentState = AIState.Fallen;

        // <<< --- این خط جدید اضافه شد --- >>>
        // به مدیر مرحله خبر میده که این AI حذف شده
        // if (Stage5_Manager.Instance != null)
        // {
        //     Stage5_Manager.Instance.ReportAIFallen();
        // }
        // <<< --- پایان تغییر --- >>>

        int myID = GetComponent<CharacterIdentity>().playerID;
        GameManager.Instance.EliminatePlayer(myID);

        // --- Play Falling Scream ---
        AudioClip screamSound = GlassBridgeSoundManager.Instance.GetRandomScream(gender);
        if (screamSound != null)
        {
            audioSource.PlayOneShot(screamSound);
        }
        // -------------------------


        if (animator) animator.SetBool(fallBoolName, true);
        isFalling = true;

        Vector3 startScale = transform.localScale;
        Vector3 platformStartScale = (platform != null) ? platform.localScale : Vector3.one;
        float duration = 1.5f;
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.Translate(Vector3.down * 2f * Time.deltaTime, Space.World);
            if (platform != null)
            {
                platform.localScale = Vector3.Lerp(platformStartScale, Vector3.zero, t);
                platform.Translate(Vector3.down * 2f * Time.deltaTime, Space.World);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (platform != null) Destroy(platform.gameObject);
        Destroy(gameObject);
    }
    
    private IEnumerator JumpToTargetCoroutine(Vector3 targetPosition)
    {
        if (isFalling) yield break; // اگر در حال سقوط است، پرش را متوقف کن 

        targetPosition.z = transform.position.z;

        // توقف انیمیشن‌های دیگر قبل از پرش
        if (animator)
        {
            animator.SetBool(walkingBoolName, false); // توقف راه رفتن
            animator.SetBool(fallBoolName, false);
        }
    

        // --- پخش صدای پرش بر اساس جنسیت ---
        AudioClip jumpSound = GlassBridgeSoundManager.Instance.GetRandomJumpSound(gender);
        if (jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
        // ---------------------------------
    
    
        if (animator) animator.SetBool(jumpBoolName, true);
        Vector3 startPosition = transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            Debug.Log($"[AI {name}] Jumping with moveSpeed={moveSpeed}, deltaTime={Time.deltaTime}");

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            float distCovered = Vector3.Distance(startPosition, transform.position);
            float progress = journeyLength > 0.001f ? distCovered / journeyLength : 1f;
            float scaleFactor = Mathf.Sin(progress * Mathf.PI) * (jumpScale - 1f) + 1f;
            transform.localScale = normalScale * scaleFactor;
            yield return null;
        }
        
        transform.position = targetPosition;
        transform.localScale = normalScale;
        if(animator) animator.SetBool(jumpBoolName, false);
    }

    private Transform FindNextJumpTarget()
    {
        Collider2D[] allPlatformsInFront = Physics2D.OverlapCircleAll(transform.position + Vector3.right * 5f, 5f, platformLayer);
        List<Transform> nextRowPlatforms = new List<Transform>();
        foreach (var platformCollider in allPlatformsInFront)
        {
            if (platformCollider.transform.position.x > transform.position.x + 0.5f)
            {
                nextRowPlatforms.Add(platformCollider.transform);
            }
        }

        if (nextRowPlatforms.Count == 0) return null;
        nextRowPlatforms = nextRowPlatforms.OrderBy(p => Vector3.Distance(transform.position, p.position)).ToList();

        List<Transform> candidates = new List<Transform>();
        if (nextRowPlatforms.Count > 0) candidates.Add(nextRowPlatforms[0]);
        if (nextRowPlatforms.Count > 1 && Mathf.Abs(nextRowPlatforms[0].position.y - nextRowPlatforms[1].position.y) < 1.0f) 
            candidates.Add(nextRowPlatforms[1]);
        
        List<Transform> availableCandidates = new List<Transform>();
        foreach (Transform candidate in candidates)
        {
            Collider2D[] occupants = Physics2D.OverlapCircleAll(candidate.position, 0.3f, characterLayer);
            if (occupants.Length == 0)
            {
                availableCandidates.Add(candidate);
            }
        }

        if (availableCandidates.Count > 0)
        {
            return availableCandidates[Random.Range(0, availableCandidates.Count)];
        }

        return null;
    }
}