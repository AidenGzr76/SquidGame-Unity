using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using RTLTMPro;

public class VotingManager : MonoBehaviour
{
    [Header("UI Elements")]
    public RTLTextMeshPro totalVotersText;
    public TextMeshProUGUI yesVotesText;
    public TextMeshProUGUI noVotesText;
    public TextMeshProUGUI timerText;

    public Button yesButton;
    public Button noButton;

    public GameObject RedShadowEffect;
    public GameObject GreenShadowEffect;
    
    private int totalVoters; 

    [Header("Voting Settings")]
    // <<< --- این بخش اصلاح شده است --- >>>
    [Tooltip("شانس رأی مثبت هر NPC (از 0 تا 100)")]
    [Range(0, 100)]
    public int passProbability = 80; // پیش‌فرض 80%
    // <<< --- پایان اصلاح --- >>>
    public float votingDuration = 20f;
    public float delayAfterVote = 3f;

    [Header("Audio Settings")]
    public AudioClip finalCountdownClip;
    private AudioSource audioSource;

    private bool playerHasVoted = false;
    private bool playerVotedYes;
    private int finalNpcYesVotes;
    private int finalNpcNoVotes;
    private float currentVotingTime;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            totalVoters = GameManager.Instance.remainingPlayers;
            Debug.Log(totalVoters);
        }
        else
        {
            Debug.LogError("GameManager پیدا نشد! از مقدار پیش‌فرض استفاده می‌شود.");
            totalVoters = 100; 
        }

        InitializeUI();

        yesButton.onClick.AddListener(OnYesButtonClicked);
        noButton.onClick.AddListener(OnNoButtonClicked);

        currentVotingTime = votingDuration;
        StartCoroutine(StartVotingSequence());
    }

    void InitializeUI()
    {
        totalVotersText.text = totalVoters.ToString();
        Debug.Log("Total Voters: " + totalVotersText.text);
        yesVotesText.text = "0";
        noVotesText.text = "0";
        timerText.text = votingDuration.ToString("F0");

        yesButton.interactable = true;
        noButton.interactable = true;
    }

    // <<< --- این تابع به طور کامل بازنویسی شده است --- >>>
    private IEnumerator StartVotingSequence()
    {
        int npcVoters = totalVoters > 0 ? totalVoters - 1 : 0;

        // <<< --- ۱. منطق جدید رأی‌گیری (بر اساس درصد) --- >>>
        finalNpcYesVotes = 0;
        finalNpcNoVotes = 0;

        // به ازای هر NPC، یک رأی رندوم بر اساس شانس قبولی می‌سازیم
        for (int i = 0; i < npcVoters; i++)
        {
            // یک عدد رندوم بین 0 تا 99 انتخاب کن
            if (Random.Range(0, 100) < passProbability)
            {
                finalNpcYesVotes++; // اگر کمتر از شانس قبولی بود -> رأی مثبت
            }
            else
            {
                finalNpcNoVotes++; // در غیر این صورت -> رأی منفی
            }
        }
        Debug.Log($"Votes Calculated: {finalNpcYesVotes} Yes, {finalNpcNoVotes} No (based on {passProbability}% chance)");
        // <<< --- پایان منطق جدید --- >>>


        float elapsedTime = 0f;
        bool finalCountdownStarted = false;

        // ۲. حلقه انیمیشن (بدون تغییر)
        // این حلقه حالا اعداد را به سمت مقادیر "جدید و رندوم" ما می‌شمارد
        while (currentVotingTime > 0)
        {
             elapsedTime += Time.deltaTime;
             currentVotingTime -= Time.deltaTime;
             timerText.text = Mathf.CeilToInt(currentVotingTime).ToString();
             
             if (currentVotingTime <= 3f && !finalCountdownStarted)
             {
                 if (finalCountdownClip != null)
                 {
                     audioSource.clip = finalCountdownClip;
                     audioSource.Play();
                 }
                 finalCountdownStarted = true;
             }

             float progress = elapsedTime / votingDuration;
             int currentNpcYes = (int)Mathf.Lerp(0, finalNpcYesVotes, progress);
             int currentNpcNo = (int)Mathf.Lerp(0, finalNpcNoVotes, progress);

             int displayYes = currentNpcYes + (playerHasVoted && playerVotedYes ? 1 : 0);
             int displayNo = currentNpcNo + (playerHasVoted && !playerVotedYes ? 1 : 0);

             yesVotesText.text = displayYes.ToString();
             noVotesText.text = displayNo.ToString();

             yield return null;
        }

        // ۳. نمایش نتایج نهایی (بدون تغییر)
        int finalYes = finalNpcYesVotes + (playerHasVoted ? (playerVotedYes ? 1 : 0) : 0);
        int finalNo = finalNpcNoVotes + (playerHasVoted ? (!playerVotedYes ? 1 : 0) : 0);
        
        if (!playerHasVoted)
        {
             finalYes = finalNpcYesVotes;
             finalNo = finalNpcNoVotes;
             DisableAllButtons();
        }

        yesVotesText.text = finalYes.ToString();
        noVotesText.text = finalNo.ToString();
        timerText.text = "0";
        
        Debug.Log($"Voting Finished! Yes: {finalYes}, No: {finalNo}");
        
        yield return new WaitForSeconds(delayAfterVote);
        
        bool votePassed = finalYes > finalNo;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.VotingComplete(votePassed);
        }
    }

    // ... (توابع OnYesButtonClicked, OnNoButtonClicked, DisableAllButtons بدون تغییر) ...
    public void OnYesButtonClicked()
    {
        if (playerHasVoted) return;
        playerHasVoted = true;
        playerVotedYes = true;
        
        int currentYesCount = int.Parse(yesVotesText.text.Replace(",", ""));
        yesVotesText.text = (currentYesCount + 1).ToString();

        RedShadowEffect.SetActive(true);

        DisableAllButtons();
    }
    public void OnNoButtonClicked()
    {
        if (playerHasVoted) return;
        playerHasVoted = true;
        playerVotedYes = false;
        
        int currentNoCount = int.Parse(noVotesText.text.Replace(",", ""));
        noVotesText.text = (currentNoCount + 1).ToString();

        GreenShadowEffect.SetActive(true);
         
        DisableAllButtons();
    }
    private void DisableAllButtons()
    {
        yesButton.interactable = false;
        noButton.interactable = false;
    }
}









// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections;
// using RTLTMPro;

// public class VotingManager : MonoBehaviour
// {
//     [Header("UI Elements")]
//     public RTLTextMeshPro totalVotersText;
//     public TextMeshProUGUI yesVotesText;
//     public TextMeshProUGUI noVotesText;
//     public TextMeshProUGUI timerText;

//     public Button yesButton;
//     public Button noButton;

//     public GameObject RedShadowEffect;
//     public GameObject GreenShadowEffect;
    
//     private int totalVoters; 

//     [Header("Voting Settings")]
//     public float votingDuration = 20f;
//     public float delayAfterVote = 3f;

//     [Header("Audio Settings")]
//     public AudioClip finalCountdownClip;
//     private AudioSource audioSource;

//     private bool playerHasVoted = false;
//     private bool playerVotedYes;
//     private int finalNpcYesVotes;
//     private int finalNpcNoVotes;
//     private float currentVotingTime;

//     void Awake()
//     {
//         audioSource = GetComponent<AudioSource>();
//         if (audioSource == null)
//         {
//             audioSource = gameObject.AddComponent<AudioSource>();
//         }
//         audioSource.playOnAwake = false;
//     }

//     void Start()
//     {
//         if (GameManager.Instance != null)
//         {
//             totalVoters = GameManager.Instance.remainingPlayers;
//             Debug.Log(totalVoters);
//         }
//         else
//         {
//             Debug.LogError("GameManager پیدا نشد! از مقدار پیش‌فرض استفاده می‌شود.");
//             totalVoters = 100; 
//         }

//         InitializeUI();

//         yesButton.onClick.AddListener(OnYesButtonClicked);
//         noButton.onClick.AddListener(OnNoButtonClicked);

//         currentVotingTime = votingDuration;
//         StartCoroutine(StartVotingSequence());
//     }

//     void InitializeUI()
//     {
//         totalVotersText.text = totalVoters.ToString();
//         Debug.Log("Total Voters: " + totalVotersText.text);
//         yesVotesText.text = "0";
//         noVotesText.text = "0";
//         timerText.text = votingDuration.ToString("F0");

//         yesButton.interactable = true;
//         noButton.interactable = true;
//     }

//     private IEnumerator StartVotingSequence()
//     {
//         int npcVoters = totalVoters > 0 ? totalVoters - 1 : 0;
//         finalNpcYesVotes = Random.Range(npcVoters / 2, npcVoters); 
//         finalNpcNoVotes = npcVoters - finalNpcYesVotes;

//         float elapsedTime = 0f;
//         bool finalCountdownStarted = false;

//         while (currentVotingTime > 0)
//         {
//              elapsedTime += Time.deltaTime;
//              currentVotingTime -= Time.deltaTime;
//              timerText.text = Mathf.CeilToInt(currentVotingTime).ToString();
             
//              if (currentVotingTime <= 3f && !finalCountdownStarted)
//              {
//                  if (finalCountdownClip != null)
//                  {
//                      audioSource.clip = finalCountdownClip;
//                      audioSource.Play();
//                  }
//                  finalCountdownStarted = true;
//              }

//              float progress = elapsedTime / votingDuration;
//              int currentNpcYes = (int)Mathf.Lerp(0, finalNpcYesVotes, progress);
//              int currentNpcNo = (int)Mathf.Lerp(0, finalNpcNoVotes, progress);

//              int displayYes = currentNpcYes + (playerHasVoted && playerVotedYes ? 1 : 0);
//              int displayNo = currentNpcNo + (playerHasVoted && !playerVotedYes ? 1 : 0);

//              yesVotesText.text = displayYes.ToString();
//              noVotesText.text = displayNo.ToString();

//              yield return null;
//         }

//         int finalYes = finalNpcYesVotes + (playerHasVoted ? (playerVotedYes ? 1 : 0) : 0);
//         int finalNo = finalNpcNoVotes + (playerHasVoted ? (!playerVotedYes ? 1 : 0) : 0);
        
//         if (!playerHasVoted)
//         {
//              finalYes = finalNpcYesVotes;
//              finalNo = finalNpcNoVotes;
//              DisableAllButtons();
//         }

//         yesVotesText.text = finalYes.ToString();
//         noVotesText.text = finalNo.ToString();
//         timerText.text = "0";
        
//         Debug.Log($"Voting Finished! Yes: {finalYes}, No: {finalNo}");
        
//         yield return new WaitForSeconds(delayAfterVote);
        
//         bool votePassed = finalYes > finalNo;
        
//         if (GameManager.Instance != null)
//         {
//             GameManager.Instance.VotingComplete(votePassed);
//         }
//     }

//     public void OnYesButtonClicked()
//     {
//         if (playerHasVoted) return;
//         playerHasVoted = true;
//         playerVotedYes = true;
        
//         int currentYesCount = int.Parse(yesVotesText.text.Replace(",", ""));
//         yesVotesText.text = (currentYesCount + 1).ToString();

//         RedShadowEffect.SetActive(true);

//         DisableAllButtons();
//     }
//     public void OnNoButtonClicked()
//     {
//         if (playerHasVoted) return;
//         playerHasVoted = true;
//         playerVotedYes = false;
        
//         int currentNoCount = int.Parse(noVotesText.text.Replace(",", ""));
//         noVotesText.text = (currentNoCount + 1).ToString();

//         GreenShadowEffect.SetActive(true);
         
//         DisableAllButtons();
//     }
//     private void DisableAllButtons()
//     {
//         yesButton.interactable = false;
//         noButton.interactable = false;
//     }
// }