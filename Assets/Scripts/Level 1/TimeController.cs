using UnityEngine;
using System.Collections;
using TMPro;

public class TimerController : MonoBehaviour
{
    public float startMinutes = 2f;
    public TextMeshProUGUI timerText;

    public Transform startLine;
    public Transform finishLine;

    private float remainingTime;
    private bool timerRunning = false;
    public GameObject gameOverPanel;

    public UIManager uiManager;
    public ReviveManager reviveManager;

    void Start()
    {
        remainingTime = startMinutes * 60f;
        timerRunning = true;
    }

    void Update()
    {
        if (!timerRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
        {
            remainingTime = 0;
            timerRunning = false;
            OnTimerEnd();
        }

        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void OnTimerEnd()
    {
        // چراغ‌ها خاموش
        LightManager.Instance.SetLightsOff();

        // عروسک چرخه فعلی‌اش را تمام می‌کند و سپس متوقف می‌شود
        DollController.Instance.StopDollAfterCurrentTurn();

        // بازیکنان بین startLine و finishLine حذف شوند
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.transform.position.y > finishLine.position.y) continue;
            if (player.transform.position.y < startLine.position.y) continue;

            // شلیک از اسلحه‌ها
            GunManager.Instance.ShootAtTarget(player.transform, () =>
            {
                if (player != null)// && player.deathShadow != null)
                {
                    player.ShowBlood();
                    StartCoroutine(DeathWait());
                }
            });
        }
    }

    private IEnumerator DeathWait()
    {
        yield return new WaitForSeconds(2f);
        PlayerDied();
    }

    public void PlayerDied()
    {
        // Time.timeScale = 0f;
        // gameOverPanel.SetActive(true);

        if (GameManager.Instance.currentMode == GameManager.GameMode.MainFlow)
        {
            GameManager.Instance.StageFailed();
        }
        else
        {
            uiManager.ShowLosePanel();
        }
    }
}
