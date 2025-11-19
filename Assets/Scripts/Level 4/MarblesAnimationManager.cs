using UnityEngine;
using System.Collections;
using TMPro;

public class MarblesAnimationManager : MonoBehaviour
{
    public static MarblesAnimationManager Instance { get; private set; }

    [Header("Score Animation")]
    public float scoreTickSpeed = 0.01f;
    public GameObject marbleIconPrefab;
    public Transform animationCanvas;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void StartBreathingAnimation(GameObject button)
    {
        if(button != null)
            LeanTween.scale(button, Vector3.one * 1.05f, 1.5f).setLoopPingPong();
    }

    public void AnimateButtonClick(GameObject button)
    {
        if (button != null)
        {
            LeanTween.cancel(button);
            LeanTween.scale(button, Vector3.one * 0.9f, 0.1f).setEasePunch().setOnComplete(() => {
                LeanTween.scale(button, Vector3.one, 0.2f);
                StartBreathingAnimation(button);
            });
        }
    }

    public void AnimateScoreChange(TextMeshProUGUI playerText, int playerStart, int playerEnd,
                                     TextMeshProUGUI opponentText, int opponentStart, int opponentEnd,
                                     float finalDelay, System.Action onAnimationComplete)
    {
        StartCoroutine(ScoreChangeRoutine(playerText, playerStart, playerEnd, opponentText, opponentStart, opponentEnd, finalDelay, onAnimationComplete));
    }

    private IEnumerator ScoreChangeRoutine(TextMeshProUGUI playerText, int playerStart, int playerEnd,
                                         TextMeshProUGUI opponentText, int opponentStart, int opponentEnd,
                                         float finalDelay, System.Action onComplete)
    {
        bool playerWonRound = playerEnd > playerStart;
        Transform startTransform = playerWonRound ? opponentText.transform : playerText.transform;
        Transform endTransform = playerWonRound ? playerText.transform : opponentText.transform;

        AudioClip swooshSound = playerWonRound ? MarblesSoundManager.Instance.scoreSwooshSound : MarblesSoundManager.Instance.loseThudSound;
        MarblesSoundManager.Instance.PlaySound(swooshSound);

        StartCoroutine(FlyingMarblesEffect(startTransform, endTransform, Mathf.Abs(playerEnd - playerStart)));

        Coroutine playerAnim = StartCoroutine(AnimateNumberRoutine(playerText, playerStart, playerEnd));
        Coroutine opponentAnim = StartCoroutine(AnimateNumberRoutine(opponentText, opponentStart, opponentEnd));

        yield return playerAnim;
        yield return opponentAnim;

        AudioClip finalChime = playerWonRound ? MarblesSoundManager.Instance.winChimeSound : MarblesSoundManager.Instance.loseThudSound;
        MarblesSoundManager.Instance.PlaySound(finalChime);

        yield return new WaitForSeconds(finalDelay);
        onComplete?.Invoke();
    }

    private IEnumerator AnimateNumberRoutine(TextMeshProUGUI text, int from, int to)
    {
        int current = from;
        int step = (from < to) ? 1 : -1;

        if (from == to) yield break;

        while (current != to)
        {
            current += step;
            text.text = current.ToString();
            yield return new WaitForSeconds(scoreTickSpeed);
        }
        text.text = to.ToString();
    }

    private IEnumerator FlyingMarblesEffect(Transform start, Transform end, int count)
    {
        if (marbleIconPrefab == null || start == null || end == null) yield break;
        for (int i = 0; i < Mathf.Min(count, 10); i++)
        {
            GameObject marbleIcon = Instantiate(marbleIconPrefab, start.position, Quaternion.identity, animationCanvas);
            LeanTween.move(marbleIcon, end.position, 0.5f).setEase(LeanTweenType.easeOutQuad).setDestroyOnComplete(true);
            yield return new WaitForSeconds(0.05f);
        }
    }
}

