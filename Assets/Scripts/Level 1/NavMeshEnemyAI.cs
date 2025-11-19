using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class NavMeshEnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    [Header("Human-like Error")]
    public float reactionTime = 0.25f;
    public float reactionTimeVariance = 0.15f;
    [Header("Animation")]
    public Animator animator;
    public string movingBoolName = "isMoving";
    [Header("Visuals")]
    public Transform visualRoot;
    public bool IsMoving { get; private set; }
    [Header("Push Attack Behaviour")]
    public float pushDetectionRadius = 1.5f;
    public float pushCheckInterval = 0.5f;
    public LayerMask characterLayer;
    [Range(0, 1)]
    public float pushChance = 0.3f;
    public float pushFOV = 90f;
    [Header("Push Attack")]
    public float hitForce = 15f;
    [Header("Risk Assessment Behaviour")]
    public float lowStaminaThreshold = 50f;
    public float dangerSenseRadius = 3f;
    public float fleeDistance = 4f;

    // --- Private ---
    private CharacterBase selfController;
    private float nextPushCheckTime = 0f;
    private bool isPushing = false;
    private NavMeshAgent agent;
    private static Transform finishLine;
    private Vector3 baseScale;
    private bool brainWantsToMove = true;
    private float reactionTimer = 0f;
    private Rigidbody2D rb;
    private LineRenderer lineRenderer;
    private bool isFleeing = false;
    private bool hasInitialDestination = false;
    private bool hasReachedDestination = false;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        lineRenderer = GetComponent<LineRenderer>();
        selfController = GetComponent<CharacterBase>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!visualRoot) visualRoot = transform;
        baseScale = visualRoot.localScale;
    }

    void Start()
    {
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = moveSpeed;

        if (finishLine == null)
        {
            GameObject finishObject = GameObject.FindGameObjectWithTag("Finish");
            if (finishObject != null)
                finishLine = finishObject.transform;
        }

        if (finishLine == null)
            Debug.LogError("FinishZone (با تگ 'Finish') در صحنه پیدا نشد!");

        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (hasReachedDestination)
        {
            return;
        }

        // ۱. ست کردن مقصد اولیه
        if (!hasInitialDestination)
        {
            if (agent.isOnNavMesh)
            {
                SetRandomDestination();
                hasInitialDestination = true;
            }
            else
            {
                // <<< اصلاح شد: return حذف شد تا در فریم بعدی دوباره تلاش کند >>>
                Debug.LogWarning($"Agent {gameObject.name} is not on NavMesh yet. Retrying...");
            }
        }

        // اگر هل داده شده، هیچ منطق حرکتی اجرا نکن
        if (isPushing || selfController.isPushed)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector2.zero;
            }
            if(animator) animator.SetBool(movingBoolName, false);
            return;
        }

        // --- بررسی برای هل دادن ---
        if (Time.time > nextPushCheckTime)
        {
            TryToPush();
            nextPushCheckTime = Time.time + pushCheckInterval;
        }

        // --- ارزیابی خطر (فرار کردن) ---
        AssessDanger();
        
        // --- منطق اصلی چراغ قرمز ---
        if (!isFleeing)
        {
            bool isRedLight = LightManager.Instance != null && LightManager.Instance.redLight.activeSelf;
            if (isRedLight && brainWantsToMove)
            {
                brainWantsToMove = false;
                reactionTimer = Random.Range(reactionTime - reactionTimeVariance, reactionTime + reactionTimeVariance);
            }
            else if (!isRedLight && !brainWantsToMove)
            {
                brainWantsToMove = true;
                reactionTimer = Random.Range(reactionTime - reactionTimeVariance, reactionTime + reactionTimeVariance);
            }

            if (reactionTimer > 0)
            {
                reactionTimer -= Time.deltaTime;
            }
            else
            {
                if (agent.isOnNavMesh)
                {
                    if (!brainWantsToMove)
                    {
                        if (!agent.isStopped)
                        {
                            agent.isStopped = true;
                            agent.velocity = Vector2.zero;
                        }
                    }
                    else
                    {
                        if (agent.isStopped)
                        {
                            agent.isStopped = false;
                        }
                    }
                }
            }
        }
        else
        {
            if(lineRenderer != null) lineRenderer.startColor = lineRenderer.endColor = Color.yellow;
        }
        
        // <<< --- "چک کردن مقصد" به اینجا منتقل شد --- >>>
        if (hasInitialDestination && agent.isOnNavMesh && agent.hasPath && !agent.pathPending && 
            agent.remainingDistance <= agent.stoppingDistance && 
            !isPushing && !selfController.isPushed)
        {
            hasReachedDestination = true;
            StartCoroutine(EnemyReachedDestinationRoutine());
            return;
        }
        // <<< --- پایان انتقال --- >>>

        IsMoving = agent.velocity.sqrMagnitude > 0.01f && !agent.isStopped;
        UpdateAnimation();
        UpdateFacingDirection();
        DrawPath();
    }

    // <<< --- LateUpdate از اینجا "پاک شد" --- >>>

    void DrawPath()
    {
        if (agent.hasPath && lineRenderer != null)
        {
            lineRenderer.positionCount = agent.path.corners.Length;
            lineRenderer.SetPositions(agent.path.corners);
            lineRenderer.enabled = true;
        }
        else if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    void AssessDanger()
    {
        if (isFleeing) return;
        if (selfController.currentStamina < lowStaminaThreshold)
        {
            Vector2 averageThreatPosition = Vector2.zero;
            int threatCount = 0;
            Collider2D[] nearbyCharacters = Physics2D.OverlapCircleAll(transform.position, dangerSenseRadius);
            foreach (var characterCollider in nearbyCharacters)
            {
                if (characterCollider.gameObject != this.gameObject)
                {
                    averageThreatPosition += (Vector2)characterCollider.transform.position;
                    threatCount++;
                }
            }
            if (threatCount > 0)
            {
                averageThreatPosition /= threatCount;
                Vector2 fleeDirection = ((Vector2)transform.position - averageThreatPosition).normalized;
                Vector3 fleeDestination = transform.position + (Vector3)(fleeDirection * fleeDistance);
                fleeDestination.z = 0f; // <<<--- اصلاح شد: Z باید صفر باشد
                StartCoroutine(FleeCoroutine(fleeDestination));
            }
        }
    }

    private IEnumerator FleeCoroutine(Vector3 temporaryDestination)
    {
        isFleeing = true;
        if(agent.isOnNavMesh) agent.isStopped = false;
        if(agent.isOnNavMesh) agent.SetDestination(temporaryDestination);
        yield return new WaitForSeconds(1.5f);
        isFleeing = false;
        SetRandomDestination();
    }

    void TryToPush()
    {
        if (agent.isStopped || !IsMoving) return;
        float cosThreshold = Mathf.Cos(pushFOV * 0.5f * Mathf.Deg2Rad);
        Vector2 facingDir = (visualRoot.localScale.x <= 0f) ? Vector2.left : Vector2.right;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pushDetectionRadius, characterLayer);

        foreach (Collider2D hit in hits)
        {
            if (Random.value > pushChance) continue;
            if (hit.gameObject == gameObject) continue;
            Rigidbody2D hitRb = hit.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                float dot = Vector2.Dot(facingDir, toTarget);
                if (dot < cosThreshold) continue;

                var enemy = hit.GetComponent<CharacterBase>() ?? hit.GetComponentInParent<CharacterBase>();
                if (enemy != null)
                {
                    isPushing = true;
                    float staminaDiff = selfController.currentStamina - enemy.currentStamina;
                    Vector2 direction = toTarget;
                    float force = hitForce + (Mathf.Abs(staminaDiff) * 0.1f);

                    if (selfController.animator)
                    {
                        selfController.animator.SetTrigger(selfController.pushingBoolName);
                    }

                    if (staminaDiff >= 0)
                    {
                        hitRb.linearVelocity = Vector2.zero;
                        enemy.FacePlayer(visualRoot.transform);
                        enemy.ApplyKnockback(direction, force);
                        // <<< اصلاح شد: استفاده از متغیر درست از CharacterBase >>>
                        enemy.TakeStaminaDamage(selfController.staminaDamageToOthers);
                        selfController.TakeStaminaDamage(selfController.staminaDamageToOthers / 2f);
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                        selfController.ApplyKnockback(-direction, force);
                        enemy.FacePlayer(visualRoot.transform);
                        // <<< اصلاح شد: استفاده از متغیر درست از CharacterBase >>>
                        enemy.TakeStaminaDamage(selfController.staminaDamageToOthers / 2f);
                        selfController.TakeStaminaDamage(selfController.staminaDamageToOthers);
                    }
                    StartCoroutine(waitToPush(0.5f));
                    continue;
                }
            }
        }
    }

    IEnumerator waitToPush(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        isPushing = false;
    }

    void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetBool(movingBoolName, IsMoving);
    }

    void UpdateFacingDirection()
    {
        if (visualRoot == null) return;
        if (agent.velocity.x > 0.1f)
            visualRoot.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else if (agent.velocity.x < -0.1f)
            visualRoot.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    void SetRandomDestination()
    {
        if (finishLine == null)
        {
            Debug.LogError("FinishLine is null, cannot set destination.");
            return;
        }
        BoxCollider2D finishCollider = finishLine.GetComponent<BoxCollider2D>();
        if (finishCollider != null)
        {
            Bounds bounds = finishCollider.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            Vector3 randomDestination = new Vector3(randomX, randomY, 0);
            if (agent.isOnNavMesh)
                agent.SetDestination(randomDestination);
        }
    }

    private IEnumerator EnemyReachedDestinationRoutine()
    {
        agent.isStopped = true;
        if (animator) animator.SetBool(movingBoolName, false);
        
        EnemyController ec = GetComponent<EnemyController>();
        if (ec != null)
        {
            ec.ReportDestinationReached();
        }
        yield return new WaitForSeconds(3.0f); 
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        agent.enabled = false;
        this.enabled = false;
    }
}