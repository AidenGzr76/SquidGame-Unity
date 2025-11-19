using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTLTMPro;

[System.Serializable]
public class TutorialStep
{
    public string stepName; // <<-- متغیر جدید برای عنوان در Inspector
    
    [TextArea(3, 5)]
    public string description;
    public bool isWorldObject = false;

    [Tooltip("The target the arrow points to (can be UI or a world object)")]
    public Transform target;


    // <<< --- این خط جدید اضافه شده است --- >>>
    [Tooltip("If checked, the system will find the 'Player' tagged object at runtime as the target.")]
    public bool targetIsPlayer = false; // آیا هدف این مرحله، بازیکن اصلی است؟
    

    [Header("Custom Positions & Rotation")]
    public Vector2 textPosition;
    public Vector2 arrowPosition;
    public float arrowZRotation;
    public Vector2 buttonsPosition;

    [Header("Display Objects")]
    [Tooltip("Objects that are normally inactive and will be temporarily activated and highlighted during this step")]
    public GameObject[] objectsToActivateAndHighlight;
}

public class TutorialManager : MonoBehaviour
{
    [Header("Main Tutorial UI Container")]
    [Tooltip("The parent object that contains all visual tutorial elements and will be enabled/disabled")]

    public GameObject joystickObject; // <<-- متغیر جدید برای جوی‌استیک

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
    
    // متغیرهای بازگردانی حالت اولیه
    private List<RectTransform> currentUiTargets = new List<RectTransform>();
    private List<Transform> originalParents = new List<Transform>();
    private List<int> originalSiblingIndexes = new List<int>();
    
    private List<SpriteRenderer> currentRenderers = new List<SpriteRenderer>();
    private List<string> originalSortingLayerNames = new List<string>();
    
    private GameObject current3DTarget;
    private int originalLayer;
    
    private List<GameObject> currentlyActivatedObjects = new List<GameObject>();

    // برای اینکه  بفهمیم آبجکت فعال هست یا نه
    private List<bool> activatedStates = new List<bool>();

    void Start()
    {
        if (tutorialUIContainer != null)
            tutorialUIContainer.SetActive(false);
        
        nextButton.onClick.AddListener(GoToNextStep);
        skipButton.onClick.AddListener(EndTutorial);
    }

    public void StartTutorial()
    {
        Time.timeScale = 0f;

        if (tutorialUIContainer != null)
            tutorialUIContainer.SetActive(true);
        
        // --- بخش جدید: غیرفعال کردن جوی‌استیک ---
        if (joystickObject != null)
        {
            joystickObject.SetActive(false);
        }

        currentStep = 0;
        ShowStep(currentStep);
    }

    private void ShowStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= steps.Length) return;

        TutorialStep step = steps[stepIndex];

        // ... (تنظیمات متن، فلش، دکمه‌ها مثل قبل) ...
        descriptionText.text = step.description;
        descriptionText.rectTransform.anchoredPosition = step.textPosition;
        pointerArrow.anchoredPosition = step.arrowPosition;
        pointerArrow.localEulerAngles = new Vector3(0, 0, step.arrowZRotation);
        buttonsGroup.anchoredPosition = step.buttonsPosition;

        // --- <<< این بخش کامل جایگزین می‌شود >>> ---

        Transform actualTarget = null; // متغیری برای نگه داشتن هدف واقعی

        // ۱. آیا هدف این مرحله بازیکن است؟
        if (step.targetIsPlayer)
        {
            // بله -> بازیکن رو با تگ "Player" پیدا کن
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                actualTarget = playerObject.transform;
                Debug.Log("Tutorial Step: Player found as target.");
            }
            else
            {
                // اگر بازیکن پیدا نشد، ارور بده و هایلایت نکن
                Debug.LogError("Tutorial Step Error: Target is set to Player, but no object with tag 'Player' found!");
                pointerArrow.gameObject.SetActive(false); // فلش رو مخفی کن چون هدفی نیست
                // می‌تونی اینجا EndTutorial() رو هم صدا بزنی یا مرحله رو رد کنی
                return; // از ادامه تابع خارج شو
            }
        }
        else
        {
            // خیر -> از هدفی که در Inspector تنظیم شده استفاده کن
            actualTarget = step.target;
        }

        // ۲. فعال کردن و هایلایت کردن آبجکت‌های نمایشی (مثل قبل)
        if (step.objectsToActivateAndHighlight != null)
        {
            // ... (کد فعال کردن objectsToActivateAndHighlight مثل قبل) ...
            // (مطمئن شو که HighlightObject رو برای هر کدوم صدا می‌زنی)
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

        // ۳. هایلایت کردن هدف اصلی (بازیکن یا هدف Inspector)
        bool hasMainTarget = actualTarget != null;
        pointerArrow.gameObject.SetActive(hasMainTarget); // فلش فقط اگر هدف اصلی وجود دارد نمایش داده شود

        if (hasMainTarget)
        {
            HighlightObject(actualTarget); // تابع هایلایت رو با هدف واقعی صدا بزن
        }
        // --- <<< پایان بخش جایگزین شده >>> ---
    }
    
    private void HighlightObject(Transform objTransform)
    {
        // بررسی می‌کند که آیا آبجکت یک المان UI است
        RectTransform uiTarget = objTransform as RectTransform;
        if (uiTarget != null && objTransform.GetComponent<CanvasRenderer>() != null)
        {
            HighlightUIElement(uiTarget);
        }
        else // در غیر این صورت، یک آبجکت دنیای بازی است
        {
            HighlightWorldObject(objTransform);
        }
    }

    private void HighlightUIElement(RectTransform uiTarget)
    {
        if (currentUiTargets.Contains(uiTarget)) return; // جلوگیری از اضافه شدن تکراری

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
        else if (worldTarget.GetComponent<MeshRenderer>() != null) // برای آبجکت‌های سه‌بعدی
        {
            current3DTarget = worldTarget.gameObject;
            originalLayer = current3DTarget.layer;
            current3DTarget.layer = LayerMask.NameToLayer("Highlighted");
        }
    }

    private void RestoreTargetToNormal()
    {
        // غیرفعال کردن آبجکت‌هایی که فعال کرده بودیم
        if (currentlyActivatedObjects.Count > 0)
        {
            foreach (GameObject obj in currentlyActivatedObjects)
            {
                // if (obj != null) obj.SetActive(false);

                if (obj != null)
                {
                    int index = currentlyActivatedObjects.IndexOf(obj);
                    if (index >= 0 && index < activatedStates.Count)
                    {
                        obj.SetActive(activatedStates[index]);
                    }
                }
            }
            currentlyActivatedObjects.Clear();
        }
        
        // بازگردانی حالت اولیه UI
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
        
        // بازگردانی حالت اولیه اسپرایت‌ها
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
        
        // بازگردانی حالت اولیه آبجکت سه‌بعدی
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

        // --- بخش جدید: فعال کردن دوباره جوی‌استیک ---
        if (joystickObject != null)
        {
            joystickObject.SetActive(true);
        }

        Time.timeScale = 1f;
    }
}







// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections.Generic;
// using RTLTMPro;

// [System.Serializable]
// public class TutorialStep
// {
//     public string stepName; // <<-- متغیر جدید برای عنوان در Inspector
    
//     [TextArea(3, 5)]
//     public string description;
//     public bool isWorldObject = false;

//     [Tooltip("The target the arrow points to (can be UI or a world object)")]
//     public Transform target;


//     [Header("Custom Positions & Rotation")]
//     public Vector2 textPosition;
//     public Vector2 arrowPosition;
//     public float arrowZRotation;
//     public Vector2 buttonsPosition;

//     [Header("Display Objects")]
//     [Tooltip("Objects that are normally inactive and will be temporarily activated and highlighted during this step")]
//     public GameObject[] objectsToActivateAndHighlight;
// }

// public class TutorialManager : MonoBehaviour
// {
//     [Header("Main Tutorial UI Container")]
//     [Tooltip("The parent object that contains all visual tutorial elements and will be enabled/disabled")]

//     public GameObject joystickObject; // <<-- متغیر جدید برای جوی‌استیک

//     public GameObject tutorialUIContainer;

//     [Header("Core Tutorial Components")]
//     public RTLTextMeshPro descriptionText;
//     public RectTransform pointerArrow;
//     public RectTransform buttonsGroup;
//     public Button nextButton;
//     public Button skipButton;

//     [Header("Tutorial Steps")]
//     public TutorialStep[] steps;
    
//     private int currentStep = 0;
    
//     // متغیرهای بازگردانی حالت اولیه
//     private List<RectTransform> currentUiTargets = new List<RectTransform>();
//     private List<Transform> originalParents = new List<Transform>();
//     private List<int> originalSiblingIndexes = new List<int>();
    
//     private List<SpriteRenderer> currentRenderers = new List<SpriteRenderer>();
//     private List<string> originalSortingLayerNames = new List<string>();
    
//     private GameObject current3DTarget;
//     private int originalLayer;
    
//     private List<GameObject> currentlyActivatedObjects = new List<GameObject>();

//     // برای اینکه  بفهمیم آبجکت فعال هست یا نه
//     private List<bool> activatedStates = new List<bool>();

//     void Start()
//     {
//         if (tutorialUIContainer != null)
//             tutorialUIContainer.SetActive(false);
        
//         nextButton.onClick.AddListener(GoToNextStep);
//         skipButton.onClick.AddListener(EndTutorial);
//     }

//     public void StartTutorial()
//     {
//         if (tutorialUIContainer != null)
//             tutorialUIContainer.SetActive(true);
        
//         // --- بخش جدید: غیرفعال کردن جوی‌استیک ---
//         if (joystickObject != null)
//         {
//             joystickObject.SetActive(false);
//         }

//         currentStep = 0;
//         ShowStep(currentStep);
//     }

//     private void ShowStep(int stepIndex)
//     {
//         if (stepIndex < 0 || stepIndex >= steps.Length) return;

//         TutorialStep step = steps[stepIndex];

//         // تنظیمات بصری UI آموزش
//         descriptionText.text = step.description;
//         descriptionText.rectTransform.anchoredPosition = step.textPosition;
//         pointerArrow.anchoredPosition = step.arrowPosition;
//         pointerArrow.localEulerAngles = new Vector3(0, 0, step.arrowZRotation);
//         buttonsGroup.anchoredPosition = step.buttonsPosition;

//         // --- منطق یکپارچه برای هایلایت کردن ---
        
//         bool hasAnyTarget = step.target != null || (step.objectsToActivateAndHighlight != null && step.objectsToActivateAndHighlight.Length > 0);
//         pointerArrow.gameObject.SetActive(hasAnyTarget);
        
//         // ۱. فعال کردن و هایلایت کردن آبجکت‌های نمایشی
//         if (step.objectsToActivateAndHighlight != null)
//         {
//             foreach (GameObject obj in step.objectsToActivateAndHighlight)
//             {
//                 if (obj != null)
//                 {
//                     if (obj.activeSelf == false)
//                         activatedStates.Add(false);
//                     else
//                         activatedStates.Add(true);

//                     obj.SetActive(true);
//                     currentlyActivatedObjects.Add(obj);
//                     HighlightObject(obj.transform); 
//                 }
//             }
//         }

//         // ۲. هایلایت کردن هدف اصلی
//         if (step.target != null)
//         {
//             HighlightObject(step.target);
//         }
//     }
    
//     private void HighlightObject(Transform objTransform)
//     {
//         // بررسی می‌کند که آیا آبجکت یک المان UI است
//         RectTransform uiTarget = objTransform as RectTransform;
//         if (uiTarget != null && objTransform.GetComponent<CanvasRenderer>() != null)
//         {
//             HighlightUIElement(uiTarget);
//         }
//         else // در غیر این صورت، یک آبجکت دنیای بازی است
//         {
//             HighlightWorldObject(objTransform);
//         }
//     }

//     private void HighlightUIElement(RectTransform uiTarget)
//     {
//         if (currentUiTargets.Contains(uiTarget)) return; // جلوگیری از اضافه شدن تکراری

//         currentUiTargets.Add(uiTarget);
//         originalParents.Add(uiTarget.parent);
//         originalSiblingIndexes.Add(uiTarget.GetSiblingIndex());
        
//         uiTarget.SetParent(tutorialUIContainer.transform, true);
//         uiTarget.SetAsLastSibling();
//     }
    
//     private void HighlightWorldObject(Transform worldTarget)
//     {
//         SpriteRenderer[] renderers = worldTarget.GetComponentsInChildren<SpriteRenderer>();
//         if (renderers.Length > 0)
//         {
//             foreach (SpriteRenderer renderer in renderers)
//             {
//                 if (!currentRenderers.Contains(renderer))
//                 {
//                     originalSortingLayerNames.Add(renderer.sortingLayerName);
//                     currentRenderers.Add(renderer);
//                     renderer.sortingLayerName = "Highlighted";
//                 }
//             }
//         }
//         else if (worldTarget.GetComponent<MeshRenderer>() != null) // برای آبجکت‌های سه‌بعدی
//         {
//             current3DTarget = worldTarget.gameObject;
//             originalLayer = current3DTarget.layer;
//             current3DTarget.layer = LayerMask.NameToLayer("Highlighted");
//         }
//     }

//     private void RestoreTargetToNormal()
//     {
//         // غیرفعال کردن آبجکت‌هایی که فعال کرده بودیم
//         if (currentlyActivatedObjects.Count > 0)
//         {
//             foreach (GameObject obj in currentlyActivatedObjects)
//             {
//                 // if (obj != null) obj.SetActive(false);

//                 if (obj != null)
//                 {
//                     int index = currentlyActivatedObjects.IndexOf(obj);
//                     if (index >= 0 && index < activatedStates.Count)
//                     {
//                         obj.SetActive(activatedStates[index]);
//                     }
//                 }
//             }
//             currentlyActivatedObjects.Clear();
//         }
        
//         // بازگردانی حالت اولیه UI
//         if (currentUiTargets.Count > 0)
//         {
//             for(int i = 0; i < currentUiTargets.Count; i++)
//             {
//                 if (currentUiTargets[i] != null && originalParents[i] != null)
//                 {
//                     currentUiTargets[i].SetParent(originalParents[i], true);
//                     currentUiTargets[i].SetSiblingIndex(originalSiblingIndexes[i]);
//                 }
//             }
//             currentUiTargets.Clear();
//             originalParents.Clear();
//             originalSiblingIndexes.Clear();
//         }
        
//         // بازگردانی حالت اولیه اسپرایت‌ها
//         if (currentRenderers.Count > 0)
//         {
//             for (int i = 0; i < currentRenderers.Count; i++)
//             {
//                 if(currentRenderers[i] != null) 
//                     currentRenderers[i].sortingLayerName = originalSortingLayerNames[i];
//             }
//             currentRenderers.Clear();
//             originalSortingLayerNames.Clear();
//         }
        
//         // بازگردانی حالت اولیه آبجکت سه‌بعدی
//         if (current3DTarget != null)
//         {
//             current3DTarget.layer = originalLayer;
//             current3DTarget = null;
//         }
//     }

//     private void GoToNextStep()
//     {
//         RestoreTargetToNormal();
//         currentStep++;
//         if (currentStep < steps.Length)
//         {
//             ShowStep(currentStep);
//         }
//         else
//         {
//             EndTutorial();
//         }
//     }

//     private void EndTutorial()
//     {
//         RestoreTargetToNormal();
//         if (tutorialUIContainer != null)
//             tutorialUIContainer.SetActive(false);

//         // --- بخش جدید: فعال کردن دوباره جوی‌استیک ---
//         if (joystickObject != null)
//         {
//             joystickObject.SetActive(true);
//         }
//     }
// }