using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using RTLTMPro;

public class FeatureButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Feature Data")]
    public FeatureData featureData;

    [Header("UI Components")]
    // public Image iconImage;
    public Image cooldownImage;
    public GameObject featureNameLabel;

    // <<< --- این دو خط جدید اضافه شده‌اند --- >>>
    [Tooltip("کامپوننت TextMeshProUGUI که قیمت فیچر را نمایش می‌دهد")]
    public RTLTextMeshPro costText; // برای نمایش قیمت

    [Header("Alarm Manager Connection")]
    public AlarmManager alarmManager;

    // <<< --- این خط جدید اضافه شده است --- >>>
    [Tooltip("پیامی که وقتی سکه کافی نیست نمایش داده می‌شود.")]
    public string notEnoughCoinsMessage = "سکه کافی نداری!";

    [Header("Activation Settings")]
    public float holdDuration = 1.5f;
    
    private Button button;
    private AudioSource audioSource;
    private bool isPointerDown = false;
    private float pointerDownTimer = 0f;
    private bool isOnCooldown = false;

    #region Unity Lifecycle & Event Handling
    void OnEnable()
    {
        GameManager.OnStageReset += ResetButtonState;
    }

    void OnDisable()
    {
        GameManager.OnStageReset -= ResetButtonState;
    }

    void Start()
    {
        button = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        // در تابع Start، فقط یک بار ResetButtonState را صدا بزن
        // چون این تابع خودش تمام مقادیر اولیه را تنظیم می‌کند.
        ResetButtonState(); 
        
        if (alarmManager == null)
        {
            Debug.LogWarning("Warning: AlarmManager is not assigned on this button. Hold warnings will not be displayed.", gameObject);
        }
    }
    #endregion

    void TryActivateFeature()
    {
        if (GameManager.Instance.HasEnoughCoins(featureData.cost))
        {
            GameManager.Instance.SpendCoins(featureData.cost);
            
            // <<< تغییر کلیدی و حیاتی: ارسال اعلان عمومی >>>
            // دکمه دیگر کاری به منطق بازی ندارد، فقط خبر می‌دهد!
            GameManager.OnFeatureActivated?.Invoke(featureData);
            
            StartCoroutine(ActivateAndCooldown());
            Debug.Log($"Feature '{featureData.featureName}' activated!");
        }
        else
        {
            Debug.Log("Not enough coins!");

            // چک کن که آلارم منیجر وصل شده باشه
            if (alarmManager != null)
            {
                // آلارم رو با پیام جدید صدا بزن!
                alarmManager.ShowAlarm(notEnoughCoinsMessage); 
            }
        }
    }

    // ... (تمام کد شما از OnPointerDown تا ResetButtonState بدون تغییر باقی می‌ماند) ...
    // این بخش فقط مسئول ظاهر دکمه است.
    #region Full Untouched Code
    void Update()
    {
        if (isPointerDown && !isOnCooldown)
        {
            pointerDownTimer += Time.deltaTime;
            if (pointerDownTimer >= holdDuration)
            {
                TryActivateFeature();
                isPointerDown = false;
            }
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isOnCooldown) return;
        isPointerDown = true;
        pointerDownTimer = 0f;
        if (featureData.holdStartSound != null)
        {
            audioSource.PlayOneShot(featureData.holdStartSound);
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPointerDown && pointerDownTimer < holdDuration)
        {
            if (alarmManager != null)
            {
                alarmManager.ShowAlarm(featureData.holdWarningMessage);
            }
        }
        isPointerDown = false;
        pointerDownTimer = 0f;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointerDown && pointerDownTimer < holdDuration)
        {
            if (alarmManager != null)
            {
                alarmManager.ShowAlarm(featureData.holdWarningMessage);
            }
        }
        isPointerDown = false;
        pointerDownTimer = 0f;
    }
    private IEnumerator ActivateAndCooldown()
    {
        isOnCooldown = true;
        button.interactable = false;
        cooldownImage.fillAmount = 1;
        if (featureData.activationSound) audioSource.PlayOneShot(featureData.activationSound);
        StartCoroutine(AnimateLabel());
        float timer = featureData.cooldown;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            cooldownImage.fillAmount = timer / featureData.cooldown;
            yield return null;
        }
        isOnCooldown = false;
        button.interactable = true;
        cooldownImage.fillAmount = 0;
        if (featureData.cooldownFinishSound) audioSource.PlayOneShot(featureData.cooldownFinishSound);
    }
    private IEnumerator AnimateLabel()
    {
        RTLTextMeshPro labelText = featureNameLabel.GetComponentInChildren<RTLTextMeshPro>();
        if (labelText != null)
        {
            labelText.text = featureData.activationMessage;
        }
        else
        {
            Debug.LogError("No RTLTextMeshPro component found on the Feature Name Label!", gameObject);
            yield break;
        }
        featureNameLabel.SetActive(true);
        yield return new WaitForSeconds(2f);
        featureNameLabel.SetActive(false);
    }
    public void ResetButtonState()
    {
        StopAllCoroutines();
        isPointerDown = false;
        pointerDownTimer = 0f;
        isOnCooldown = false;
        if (button != null) button.interactable = true;
        if (cooldownImage != null) cooldownImage.fillAmount = 0;
        if (featureNameLabel != null) featureNameLabel.SetActive(false);
        // این خط برای اطمینان از تنظیم آیکون در ابتدای بازی اضافه شده
        // if (iconImage != null && featureData != null) iconImage.sprite = featureData.icon;
        // قیمت رو از FeatureData می‌خونه و به متن می‌ده
        if (costText != null && featureData != null) costText.text = featureData.cost.ToString();

        Debug.Log($"Button '{featureData.featureName}' has been reset.");
    }
    #endregion
}