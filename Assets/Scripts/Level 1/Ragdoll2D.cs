using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation; // SpriteSkin
using UnityEngine.U2D.IK;        // IKManager2D, Solver2D

public class Ragdoll2D : MonoBehaviour
{
    [Header("Optional Refs (auto-filled if left empty)")]
    public Animator animator;          // می‌تونه روی فرزند (Visual) باشه
    public IKManager2D ikManager;      // اگر نداری، خالی بذار
    public SpriteSkin spriteSkin;      // توصیه میشه ست باشه (روی Visual)

    [Header("Bones (auto-filled if empty)")]
    public Rigidbody2D[] boneRBs;      // RBهای همه‌ی استخوان‌ها (غیر از Root)
    public Joint2D[]     boneJoints;   // Hinge/Distance روی بون‌ها
    public Collider2D[]  boneCols;     // Colliderهای روی بون‌ها

    [Header("Death Impulse")]
    public Vector2 initialHitDir = Vector2.right;
    public float   hitForce = 8f;
    public float   hitTorque = 15f;

    [Header("Top-Down Mode")]
    public bool  topDownNoGravity   = true;  // جاذبه صفر برای تاپ‌داون
    public float ragdollDrag        = 6f;    // کم‌سر خوردن
    public float ragdollAngularDrag = 6f;    // کم‌چرخیدن
    public bool  freezeAfterSettle  = true;  // بعد از کمی زمان، فریز شود
    public float settleTime         = 0.35f; // مدت تا فریز

    [Header("Lifecycle")]
    public bool startAsAlive = true;

    // <<< این متغیرهای جدید اضافه شده‌اند >>>
    private Vector3[] initialBonePositions;
    private Quaternion[] initialBoneRotations;

    bool isAlive;

    void Awake()
    {
        AutoWire();

        // <<< این خط جدید اضافه شده است >>>
        // موقعیت اولیه تمام استخوان‌ها را یک بار برای همیشه ذخیره کن
        CacheInitialBoneTransforms();

        SetAliveState(startAsAlive);
    }

    void AutoWire()
    {
        // رفرنس‌ها اگر خالی‌اند، خودکار پیدا شوند
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!spriteSkin) spriteSkin = GetComponentInChildren<SpriteSkin>(true);
        if (!ikManager) ikManager = GetComponentInChildren<IKManager2D>(true);

        if (boneRBs == null || boneRBs.Length == 0)
        {
            var all = GetComponentsInChildren<Rigidbody2D>(true);
            var list = new List<Rigidbody2D>();
            foreach (var rb in all) if (rb.gameObject != this.gameObject) list.Add(rb);
            boneRBs = list.ToArray();
        }
        if (boneJoints == null || boneJoints.Length == 0)
            boneJoints = GetComponentsInChildren<Joint2D>(true);

        if (boneCols == null || boneCols.Length == 0)
        {
            var all = GetComponentsInChildren<Collider2D>(true);
            var list = new List<Collider2D>();
            foreach (var c in all) if (c.gameObject != this.gameObject) list.Add(c);
            boneCols = list.ToArray();
        }
    }
    
    // <<< این تابع کاملاً جدید است >>>
    void CacheInitialBoneTransforms()
    {
        initialBonePositions = new Vector3[boneRBs.Length];
        initialBoneRotations = new Quaternion[boneRBs.Length];
        for (int i = 0; i < boneRBs.Length; i++)
        {
            if (boneRBs[i] != null)
            {
                initialBonePositions[i] = boneRBs[i].transform.localPosition;
                initialBoneRotations[i] = boneRBs[i].transform.localRotation;
            }
        }
    }

    public void SetAliveState(bool alive)
    {
        isAlive = alive;

        // همه Animatorهای زیرمجموعه را خاموش/روشن کن
        var anims = GetComponentsInChildren<Animator>(true);
        foreach (var a in anims) a.enabled = alive;

        // همه Solverهای IK را خاموش/روشن کن
        var solvers = GetComponentsInChildren<Solver2D>(true);
        foreach (var s in solvers) s.enabled = alive;
        if (ikManager) ikManager.enabled = alive;

        // بون‌ها: زنده→Kinematic و غیرفعال؛ مرگ→Dynamic و فعال
        foreach (var rb in boneRBs)
        {
            if (!rb) continue;
            if (alive)
            {
                rb.simulated = false;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.None;
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.simulated = true;
                rb.gravityScale = topDownNoGravity ? 0f : 1f;
                rb.linearDamping = ragdollDrag;
                rb.angularDamping = ragdollAngularDrag;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.constraints = RigidbodyConstraints2D.None;
            }
        }
        
        // <<< این بخش کلیدی جدید اضافه شده است >>>
        // اگر بازیکن زنده می‌شود، استخوان‌ها را به حالت اولیه ریست کن
        if (alive)
        {
            for (int i = 0; i < boneRBs.Length; i++)
            {
                if (boneRBs[i] != null)
                {
                    boneRBs[i].transform.localPosition = initialBonePositions[i];
                    boneRBs[i].transform.localRotation = initialBoneRotations[i];
                }
            }
        }


        foreach (var j in boneJoints) if (j) j.enabled = !alive;
        foreach (var c in boneCols)   if (c) c.enabled = !alive;

        // Root فیزیک فقط وقتی زنده است
        var rootRb  = GetComponent<Rigidbody2D>();
        var rootCol = GetComponent<Collider2D>();
        if (rootRb)
        {
            rootRb.simulated = alive;
            rootRb.linearVelocity = Vector2.zero;
            rootRb.angularVelocity = 0f;
        }
        if (rootCol) rootCol.enabled = alive;

        // برای اطمینان از رندر صحیح بعد از خاموش شدن انیماتور
        if (spriteSkin && !alive)
        {
            spriteSkin.alwaysUpdate = true; // UpdateBounds لازم نیست
        }
    }

    public void Die(Vector2 hitDir)
    {
        if (!isAlive) return;
        SetAliveState(false);

        // هدف برای ضربه
        var rb = ChooseTorsoOrFirst();
        if (rb)
        {
            var dir = (hitDir.sqrMagnitude > 0.0001f ? hitDir : initialHitDir).normalized;

            // برای تاپ‌داون نیروی ملایم‌تر بزن، اما چون drag رو کم کردیم، مقدار پیش‌فرض هم اوکیه
            float f = topDownNoGravity ? hitForce : hitForce;
            float t = topDownNoGravity ? hitTorque : hitTorque;

            rb.WakeUp();
            rb.AddForce(dir * f, ForceMode2D.Impulse);
            rb.AddTorque(Mathf.Sign(dir.x) * t, ForceMode2D.Impulse);

            // اگر می‌خواهی پخش‌تر بپاشه، به چند بون هم کمی نیرو بده:
            for (int i = 0; i < boneRBs.Length; i++)
            {
                if (boneRBs[i] == null || boneRBs[i] == rb) continue;
                // ضربه‌ی کوچیک و تصادفی
                var jitter = (dir + Random.insideUnitCircle * 0.25f).normalized;
                boneRBs[i].WakeUp();
                boneRBs[i].AddForce(jitter * (f * 0.25f), ForceMode2D.Impulse);
            }
        }

        if (freezeAfterSettle) StartCoroutine(FreezeRagdollAfter(settleTime));
    }


    IEnumerator FreezeRagdollAfter(float t)
    {
        yield return new WaitForSeconds(t);
        foreach (var rb in boneRBs)
        {
            if (!rb) continue;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic; // فریز در همان پوز
            // اگر کاملاً مجسمه می‌خواهی:
            // rb.simulated = false;
        }
    }

    Rigidbody2D ChooseTorsoOrFirst()
    {
        foreach (var rb in boneRBs)
            if (rb && (rb.name.ToLower().Contains("bone_1") || rb.name.ToLower().Contains("bone_2")))
                return rb;
        foreach (var rb in boneRBs)
            if (rb) return rb;
        return null;
    }

    // // دکمهٔ تست در Inspector (از منوی سه‌نقطهٔ کامپوننت)
    // [ContextMenu("TEST DIE")]
    // void _TestDie() => Die(initialHitDir);
}
