using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterBase))]
public class EnemyController : MonoBehaviour
{
    [Header("Stamina UI")]
    public Slider staminaSlider;

    [Header("Red Light Kill")]
    public float pushedDetectThreshold = 0.001f;

    [Header("Death / Ragdoll")]
    public Ragdoll2D ragdoll;

    [Header("Blood")]
    public GameObject bloodPrefab;

    [Header("Game Logic")]
    public TimerController timerController;

    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private NavMeshEnemyAI aiController;
    private CharacterBase characterBase;
    private bool isAlive = true;
    private bool isTargetedByGun = false;
    private bool playerWon = false;

    void Awake() // <<<--- حتما Awake باشد
    {
        rb = GetComponent<Rigidbody2D>();

        aiController = GetComponent<NavMeshEnemyAI>();
        characterBase = GetComponent<CharacterBase>();

        if (characterBase != null)
        {
            characterBase.OnStaminaChanged += UpdateStaminaUI;
        }

        if (staminaSlider == null)
        {
            Debug.LogWarning($"اسلایدر استقامت روی {gameObject.name} تنظیم نشده است.");
        }
        else if (characterBase != null)
        {
            staminaSlider.gameObject.SetActive(true);
            staminaSlider.maxValue = characterBase.maxStamina;
        }
    }

    void Start()
    {
        if (rb != null)
        {
            lastPosition = rb.position;
        }
    }

    void OnDisable()
    {
        if (characterBase != null)
        {
            characterBase.OnStaminaChanged -= UpdateStaminaUI;
        }
    }

    void FixedUpdate()
    {
        // فقط چک کردن "هل داده شدن" در چراغ قرمز
        if (LightManager.Instance != null && LightManager.Instance.redLight.activeSelf && isAlive && !playerWon)
        {
            if (characterBase.isPushed)
            {
                float movementSinceLastFrame = (rb.position - lastPosition).sqrMagnitude;
                bool isMovingWhilePushed = movementSinceLastFrame > pushedDetectThreshold;

                if (isMovingWhilePushed && !isTargetedByGun)
                {
                    isTargetedByGun = true;
                    GunManager.Instance.ShootAtTarget(transform, () =>
                    {
                        isTargetedByGun = false;
                        Die();
                    });
                }
            }
        }
        lastPosition = rb.position;
    }

    void LateUpdate() // <<<--- تابع صفر کردن Z
    {
        if (rb != null)
        {
            Vector3 currentPosition = transform.position;
            if (currentPosition.z != 0f)
            {
                transform.position = new Vector3(currentPosition.x, currentPosition.y, 0f);
            }
        }
    }

    public void Die()
    {
        if (!isAlive) return;
        isAlive = false;

        if (staminaSlider != null)
        {
            staminaSlider.gameObject.SetActive(false);
        }

        if (GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
        {
            int myID = GetComponent<CharacterIdentity>().playerID;
            GameManager.Instance.EliminatePlayer(myID);
        }
        

        // <<< --- این خطوط جدید اضافه شده‌اند --- >>>
        // لایه دشمن و تمام بچه‌هایش (استخوان‌های رگدال) را به "DeadBody" تغییر بده
        int deadLayer = LayerMask.NameToLayer("DeadBody");
        SetLayerRecursively(this.gameObject, deadLayer);
        // <<< --- پایان بخش جدید --- >>>

        ShowBlood();
        if (ragdoll) ragdoll.Die(Vector2.left);

        // ۲. حالا که رگدال فعال شده، هر دو لایه را تغییر بده
        int deadPhysicsLayer = LayerMask.NameToLayer("DeadBody");
        SetLayerRecursively(this.gameObject, deadPhysicsLayer); 
        SetSortingLayerRecursively(this.gameObject, "DeadBody");
        // <<< --- پایان اصلاح --- >>>

        if (aiController != null)
        {
            aiController.enabled = false;
        }

        rb.bodyType = RigidbodyType2D.Dynamic; // موقع مرگ Dynamic بشه (این درسته)
        rb.linearVelocity = Vector2.zero;
    }

    public void ShowBlood()
    {
        if (!bloodPrefab) return;
        Vector3 pos = transform.position + (Vector3)(Random.insideUnitCircle * 0.1f);
        pos.z = -0.35f;
        var go = Instantiate(bloodPrefab, pos, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
        float s = Random.Range(0.9f, 1.4f);
        go.transform.localScale = new Vector3(s, s, 1f);
    }

    public void ReportDestinationReached()
    {
        playerWon = true;
    }

    private void UpdateStaminaUI(float current, float max)
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = current;
        }
    }

    // ===== این دو تابع رو به انتهای PlayerController و EnemyController اضافه کن =====

    /// <summary>
    /// لایه "فیزیکی" این آبجکت و تمام فرزندانش را تغییر می‌دهد
    /// </summary>
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer; // لایه فیزیکی پدر
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer); // لایه فیزیکی همه بچه‌ها
        }
    }

    /// <summary>
    /// لایه "رندر" (Sorting Layer) تمام اسپرایت‌های فرزند را تغییر می‌دهد
    /// </summary>
    void SetSortingLayerRecursively(GameObject obj, string layerName)
    {
        if (obj == null) return;
        
        // <<< --- این کدِ اصلاح شده و درست است --- >>>
        // تمام SpriteRenderer ها در "خود آبجکت" و "تمام فرزندانش" (استخوان‌های رگدال) را پیدا کن
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true); // true = شامل غیرفعال‌ها هم بشود

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.sortingLayerName = layerName; // لایه رندر هر اسپرایت را تغییر بده
            }
        }
    }
    
}