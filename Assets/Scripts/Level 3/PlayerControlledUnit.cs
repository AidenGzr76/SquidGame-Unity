using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerControlledUnit : MonoBehaviour
{
    [Header("Power Settings")]
    public int power = 50;
    public float increaseMultiplier = 1f;
    public float decayBaseRate = 5f;

    [Header("UI & Animation")]
    public Slider powerSlider;
    public Animator animator;
    
    [Header("Sound & Identity")]
    public Gender gender;

    #region Feature State Variables
    private float originalIncreaseMultiplier;
    private float originalDecayBaseRate;
    private bool isPowerLocked = false;
    private Coroutine powerBoostCoroutine;
    private Coroutine decayResistCoroutine;
    private Coroutine powerLockCoroutine;
    #endregion

    private float pullingTimer = 0f;
    public bool isFalling = false;
    private AudioSource audioSource;
    
    #region Unity Lifecycle & Event Handling
    void OnEnable()
    {
        GameManager.OnFeatureActivated += HandleFeatureActivation;
    }
    void OnDisable()
    {
        GameManager.OnFeatureActivated -= HandleFeatureActivation;
    }
    #endregion

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (powerSlider != null)
        {
            powerSlider.minValue = 0;
            powerSlider.maxValue = 100;
        }

        originalIncreaseMultiplier = increaseMultiplier;
        originalDecayBaseRate = decayBaseRate;
        
        // مقادیر اولیه را در اولین فریم تنظیم می‌کنیم
        ResetState(true);
    }

    void Update()
    {
        if (isFalling) return;
        HandleInput();
        HandleDecay();
        UpdateUI();
    }

    #region Feature Activation Logic
    private void HandleFeatureActivation(FeatureData data)
    {
        switch (data.type)
        {
            case FeatureData.FeatureType.TugOfWar_PowerBoost:
                ActivatePowerBoost(data.value, data.duration);
                break;
            case FeatureData.FeatureType.TugOfWar_PowerLock:
                ActivatePowerLock(data.duration);
                break;
            case FeatureData.FeatureType.TugOfWar_DecayResist:
                ActivateDecayResist(data.value, data.duration);
                break;
        }
    }
    public void ActivatePowerBoost(float multiplier, float duration)
    {
        if (powerBoostCoroutine != null) StopCoroutine(powerBoostCoroutine);
        powerBoostCoroutine = StartCoroutine(PowerBoostRoutine(multiplier, duration));
    }
    private IEnumerator PowerBoostRoutine(float multiplier, float duration)
    {
        increaseMultiplier = originalIncreaseMultiplier * multiplier;
        Debug.Log($"Power Boost Activated! New multiplier: {increaseMultiplier}");
        yield return new WaitForSeconds(duration);
        increaseMultiplier = originalIncreaseMultiplier;
        Debug.Log("Power Boost Finished.");
    }
    public void ActivatePowerLock(float duration)
    {
        if (powerLockCoroutine != null) StopCoroutine(powerLockCoroutine);
        powerLockCoroutine = StartCoroutine(PowerLockRoutine(duration));
    }
    private IEnumerator PowerLockRoutine(float duration)
    {
        isPowerLocked = true;
        power = 100;
        Debug.Log("Power Lock Activated!");
        yield return new WaitForSeconds(duration);
        isPowerLocked = false;
        Debug.Log("Power Lock Finished.");
    }
    public void ActivateDecayResist(float divisor, float duration)
    {
        if (decayResistCoroutine != null) StopCoroutine(decayResistCoroutine);
        decayResistCoroutine = StartCoroutine(DecayResistRoutine(divisor, duration));
    }
    private IEnumerator DecayResistRoutine(float divisor, float duration)
    {
        decayBaseRate = originalDecayBaseRate / divisor;
        Debug.Log($"Decay Resist Activated! New decay rate: {decayBaseRate}");
        yield return new WaitForSeconds(duration);
        decayBaseRate = originalDecayBaseRate;
        Debug.Log("Decay Resist Finished.");
    }
    #endregion
    
    #region Core Gameplay Logic
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            IncreasePower();
            if (animator != null) animator.SetBool("Pulling", true);
            pullingTimer = 1f;
        }
        if (animator != null && animator.GetBool("Pulling"))
        {
            pullingTimer -= Time.deltaTime;
            if (pullingTimer <= 0f)
            {
                animator.SetBool("Pulling", false);
            }
        }
    }
    void IncreasePower()
    {
        if (power >= 100) return;
        float difficulty = Mathf.Lerp(1f, 4f, power / 100f);
        float gain = 2f / difficulty;
        power += Mathf.CeilToInt(gain * increaseMultiplier);
        power = Mathf.Clamp(power, 0, 100);
    }
    void HandleDecay()
    {
        if (isPowerLocked)
        {
            power = 100;
            return;
        }
        float decayMultiplier = Mathf.Lerp(0.5f, 2f, power / 100f);
        float decayAmount = decayBaseRate * decayMultiplier * Time.deltaTime;
        power -= Mathf.RoundToInt(decayAmount);
        power = Mathf.Clamp(power, 0, 100);
    }
    void UpdateUI()
    {
        if (powerSlider != null)
            powerSlider.value = power;
    }
    public void FallIntoPit()
    {
        if (isFalling) return;
        isFalling = true;
        power = 0;
        transform.SetParent(null);
        StartCoroutine(FallAnimationRoutine());
        AudioClip screamSound = TugOfWarSoundManager.Instance.GetRandomScream(gender);
        if (screamSound != null) audioSource.PlayOneShot(screamSound);
    }
    private IEnumerator FallAnimationRoutine()
    {
        if (animator != null) animator.SetTrigger("Fall");
        float duration = 1.5f;
        Vector3 startScale = transform.localScale;
        float timer = 0f;
        while (timer < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
    IEnumerator PlayStrainingSoundLoop()
    {
        AudioClip mySound = TugOfWarSoundManager.Instance.GetRandomStrainingSound();
        if (mySound == null) yield break;
        while (!isFalling)
        {
            float randomDelay = Random.Range(1f, 10f);
            yield return new WaitForSeconds(randomDelay);
            if (!isFalling)
            {
                audioSource.PlayOneShot(mySound);
            }
        }
    }

    public void ResetState(bool isFirstTime = false)
    {
        StopAllCoroutines();

        increaseMultiplier = originalIncreaseMultiplier;
        decayBaseRate = originalDecayBaseRate;
        isPowerLocked = false;
        
        power = 50;
        isFalling = false;
        
        if (!isFirstTime) gameObject.SetActive(true);
        
        if (animator != null)
        {
            animator.ResetTrigger("Fall");
            animator.SetBool("Pulling", false);

            animator.Play("Idle", 0, 0f);
        }
        
        UpdateUI();
        StartCoroutine(PlayStrainingSoundLoop());
        Debug.Log("PlayerControlledUnit has been reset.");
    }
    #endregion
}

