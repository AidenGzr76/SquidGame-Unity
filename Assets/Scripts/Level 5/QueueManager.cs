using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QueueManager : MonoBehaviour
{
    [Tooltip("لیست جایگاه‌های صف که از نزدیک به دور چیده شده‌اند")]
    public List<Transform> queueSlots;

    [HideInInspector]
    public List<MonoBehaviour> characterQueue = new List<MonoBehaviour>();

    public static QueueManager Instance { get; private set; }

    // (متغیر GlassBridgeAI حذف شد، چون دیگر به آن نیازی نیست)

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }

        // مطمئن می‌شیم که سرعت بازی در شروع این مرحله روی حالت عادی (1) هست
        Time.timeScale = 1.0f;
    }

    // <<< --- تابع Start() کاملاً حذف شد --- >>>
    // (Spawner تمام کارهای Start را انجام خواهد داد)

    // <<< --- این تابع "جدید" است --- >>>
    /// <summary>
    /// این تابع توسط StageCharacterSpawner صدا زده می‌شود
    /// تا لیست نهایی و مرتب‌شده بازیکنان را دریافت کند.
    /// </summary>
    public void InitializeQueue(List<MonoBehaviour> shuffledCharacters)
    {
        // لیست را مستقیماً از Spawner دریافت کن
        characterQueue = new List<MonoBehaviour>(shuffledCharacters);
        
        Debug.Log($"QueueManager با {characterQueue.Count} بازیکن توسط Spawner راه‌اندازی شد.");
    }

    // وقتی یک کاراکتر پرش خود را شروع می‌کند، این تابع را صدا می‌زند
    public void CharacterStartedJumping(MonoBehaviour character)
    {
        if (characterQueue.Count > 0 && characterQueue[0] == character)
        {
            characterQueue.RemoveAt(0);
        }
    }

    // این تابع به هر کاراکتر می‌گوید که جایگاه هدفش کجاست
    public Transform GetMyTargetSlot(MonoBehaviour character)
    {
        int index = characterQueue.IndexOf(character);
        if (index != -1 && index < queueSlots.Count)
        {
            return queueSlots[index];
        }
        return null; // اگر در صف نبود یا جایگاهی برایش نبود
    }
}