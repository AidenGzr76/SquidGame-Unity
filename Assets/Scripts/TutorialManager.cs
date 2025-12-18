using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTLTMPro;
// using System.Diagnostics;

[System.Serializable]
public class TutorialStep
{
    public string stepName; 
    
    [TextArea(3, 5)]
    public string description;
    
    [Tooltip("The target the arrow points to (can be UI or a world object)")]
    public Transform target;

    [Tooltip("If checked, the system will find the 'Player' tagged object at runtime as the target.")]
    public bool targetIsPlayer = false; 
    
    [Header("Custom Positions & Rotation")]
    public Vector2 textPosition;
    public Vector2 arrowPosition;
    public float arrowZRotation;
    public Vector2 buttonsPosition;

    [Header("Display Objects")]
    public GameObject[] objectsToActivateAndHighlight;
}

public class TutorialManager : MonoBehaviour
{
    [Header("Settings")]
    // public bool isTestMode = true; // اگر تیک داشته باشد، همیشه توتوریال پخش می‌شود (برای تست)
    public string TutorialSaveKey = "LevelTutorialCompleted";

    [Header("Main Tutorial UI Container")]
    public GameObject joystickObject; 
    public GameObject tutorialUIContainer;

    [Header("Core Tutorial Components")]
    public RTLTextMeshPro descriptionText;
    public RectTransform pointerArrow;
    public RectTransform buttonsGroup;
    public Button nextButton;
    public Button skipButton;

    [Header("Tutorial Steps")]
    public TutorialStep[] steps;
    
    private int currentStep = 0;
    
    // لیست‌های بازگردانی حالت اولیه
    private List<RectTransform> currentUiTargets = new List<RectTransform>();
    private List<Transform> originalParents = new List<Transform>();
    private List<int> originalSiblingIndexes = new List<int>();
    
    private List<SpriteRenderer> currentRenderers = new List<SpriteRenderer>();
    private List<string> originalSortingLayerNames = new List<string>();
    
    private GameObject current3DTarget;
    private int originalLayer;
    
    private List<GameObject> currentlyActivatedObjects = new List<GameObject>();
    private List<bool> activatedStates = new List<bool>();

    // --- تغییر مهم ۱: استفاده از Awake برای تنظیمات اولیه ---
    void Awake()
    {
        // اطمینان از اینکه بازی در لحظه شروع فریز نیست
        Time.timeScale = 1f;

        if (tutorialUIContainer != null)
            tutorialUIContainer.SetActive(false);
        
        nextButton.onClick.RemoveAllListeners(); // جلوگیری از انباشته شدن لیسنرها
        skipButton.onClick.RemoveAllListeners();

        nextButton.onClick.AddListener(GoToNextStep);
        skipButton.onClick.AddListener(EndTutorial);
    }

    void Start()
    {
        // --- تغییر مهم ۲: شروع با تاخیر برای حل مشکل موبایل ---
        // این تاخیر اجازه می‌دهد تمام UI و اسکریپت‌های دیگر کامل لود شوند
        Invoke(nameof(CheckAndStartTutorial), 0.5f);
    }

    private void CheckAndStartTutorial()
    {
        // اگر حالت تست است یا هنوز مرحله را ندیده
        if (PlayerPrefs.GetInt(TutorialSaveKey, 0) == 0)
        {
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        // فریز کردن بازی
        Time.timeScale = 0f;

        if (tutorialUIContainer != null)
            tutorialUIContainer.SetActive(true);
        
        // غیرفعال کردن جوی‌استیک
        if (joystickObject != null)
        {
            joystickObject.SetActive(false);
        }

        currentStep = 0;
        ShowStep(currentStep);
    }

    private void ShowStep(int stepIndex)
    {
        // ۱. چک کن که آرایه اصلا وجود داره و خالی نیست؟
        if (steps == null || steps.Length == 0) return;

        // ۲. چک کن که ایندکس معتبره؟
        if (stepIndex < 0 || stepIndex >= steps.Length) return;

        // ۳. حالا با خیال راحت بگیرش
        TutorialStep step = steps[stepIndex];

        descriptionText.text = step.description;
        descriptionText.rectTransform.anchoredPosition = step.textPosition;
        pointerArrow.anchoredPosition = step.arrowPosition;
        pointerArrow.localEulerAngles = new Vector3(0, 0, step.arrowZRotation);
        buttonsGroup.anchoredPosition = step.buttonsPosition;

        Transform actualTarget = null;

        // پیدا کردن تار겟 (بازیکن یا آبجکت مشخص شده)
        if (step.targetIsPlayer)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                actualTarget = playerObject.transform;
            }
            else
            {
                pointerArrow.gameObject.SetActive(false);
                return;
            }
        }
        else
        {
            actualTarget = step.target;
        }

        // فعال‌سازی آبجکت‌های هایلایت
        if (step.objectsToActivateAndHighlight != null)
        {
             foreach (GameObject obj in step.objectsToActivateAndHighlight)
             {
                 if (obj != null)
                 {
                     if (!obj.activeSelf) activatedStates.Add(false);
                     else activatedStates.Add(true);

                     obj.SetActive(true);
                     currentlyActivatedObjects.Add(obj);
                     HighlightObject(obj.transform);
                 }
             }
        }

        // نمایش فلش و هایلایت تارگت اصلی
        bool hasMainTarget = actualTarget != null;
        pointerArrow.gameObject.SetActive(hasMainTarget);

        if (hasMainTarget)
        {
            HighlightObject(actualTarget);
        }
    }
    
    private void HighlightObject(Transform objTransform)
    {
        RectTransform uiTarget = objTransform as RectTransform;
        if (uiTarget != null && objTransform.GetComponent<CanvasRenderer>() != null)
        {
            HighlightUIElement(uiTarget);
        }
        else
        {
            HighlightWorldObject(objTransform);
        }
    }

    private void HighlightUIElement(RectTransform uiTarget)
    {
        if (currentUiTargets.Contains(uiTarget)) return;

        currentUiTargets.Add(uiTarget);
        originalParents.Add(uiTarget.parent);
        originalSiblingIndexes.Add(uiTarget.GetSiblingIndex());
        
        uiTarget.SetParent(tutorialUIContainer.transform, true);
        uiTarget.SetAsLastSibling();
    }
    
    private void HighlightWorldObject(Transform worldTarget)
    {
        SpriteRenderer[] renderers = worldTarget.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length > 0)
        {
            foreach (SpriteRenderer renderer in renderers)
            {
                if (!currentRenderers.Contains(renderer))
                {
                    originalSortingLayerNames.Add(renderer.sortingLayerName);
                    currentRenderers.Add(renderer);
                    renderer.sortingLayerName = "Highlighted";
                }
            }
        }
        else if (worldTarget.GetComponent<MeshRenderer>() != null)
        {
            current3DTarget = worldTarget.gameObject;
            originalLayer = current3DTarget.layer;
            current3DTarget.layer = LayerMask.NameToLayer("Highlighted");
        }
    }

    private void RestoreTargetToNormal()
    {
        // بازگردانی آبجکت‌های فعال شده
        if (currentlyActivatedObjects.Count > 0)
        {
            for (int i = 0; i < currentlyActivatedObjects.Count; i++)
            {
                GameObject obj = currentlyActivatedObjects[i];
                if (obj != null)
                {
                    if (i < activatedStates.Count)
                        obj.SetActive(activatedStates[i]);
                    else
                        obj.SetActive(false); // Fallback
                }
            }
            currentlyActivatedObjects.Clear();
            activatedStates.Clear(); // لیست وضعیت‌ها هم باید خالی شود
        }
        
        // بازگردانی UI
        if (currentUiTargets.Count > 0)
        {
            for(int i = 0; i < currentUiTargets.Count; i++)
            {
                if (currentUiTargets[i] != null && originalParents[i] != null)
                {
                    currentUiTargets[i].SetParent(originalParents[i], true);
                    currentUiTargets[i].SetSiblingIndex(originalSiblingIndexes[i]);
                }
            }
            currentUiTargets.Clear();
            originalParents.Clear();
            originalSiblingIndexes.Clear();
        }
        
        // بازگردانی اسپرایت‌ها
        if (currentRenderers.Count > 0)
        {
            for (int i = 0; i < currentRenderers.Count; i++)
            {
                if(currentRenderers[i] != null) 
                    currentRenderers[i].sortingLayerName = originalSortingLayerNames[i];
            }
            currentRenderers.Clear();
            originalSortingLayerNames.Clear();
        }
        
        // بازگردانی 3D
        if (current3DTarget != null)
        {
            current3DTarget.layer = originalLayer;
            current3DTarget = null;
        }
    }

    private void GoToNextStep()
    {
        RestoreTargetToNormal();
        currentStep++;
        if (currentStep < steps.Length)
        {
            ShowStep(currentStep);
        }
        else
        {
            EndTutorial();
        }
    }

    private void EndTutorial()
    {
        RestoreTargetToNormal();
        if (tutorialUIContainer != null)
            tutorialUIContainer.SetActive(false);

        // فعال کردن دوباره جوی‌استیک
        if (joystickObject != null)
        {
            joystickObject.SetActive(true);
        }

        // آزاد کردن بازی
        Time.timeScale = 1f;

        // ذخیره کردن اینکه بازیکن مرحله را دیده
        PlayerPrefs.SetInt(TutorialSaveKey, 1);
        PlayerPrefs.Save();

        // ذخیره کردن اینکه بازیکن مرحله را دیده (فقط اگر حالت تست خاموش باشد)
        // if (!isTestMode)
        // {
        //     PlayerPrefs.SetInt(TutorialSaveKey, 1);
        //     PlayerPrefs.Save();
        // }
    }
}