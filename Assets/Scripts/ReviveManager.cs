// ReviveManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReviveManager : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject revivePanel;
    public Button reviveButton;
    public Button giveUpButton;
    public TextMeshProUGUI costText;

    [Header("External Canvases")]
    public GameObject joystickCanvas;
    private BackgroundMusic music;

    // <<< ارجاع به PlayerController به طور کامل حذف شد >>>

    void Start()
    {
        revivePanel.SetActive(false);
        music = FindFirstObjectByType<BackgroundMusic>();
    }

    public void ShowRevivePanel()
    {
        Time.timeScale = 0f;

        if (music != null) music.PauseMusic();
        
        revivePanel.SetActive(true);
        if (joystickCanvas != null) joystickCanvas.SetActive(false);

        int cost = GameManager.Instance.reviveCost;
        costText.text = cost.ToString();

        if (GameManager.Instance.HasEnoughCoins(cost))
        {
            reviveButton.interactable = true;
        }
        else
        {
            reviveButton.interactable = false;
        }
    }

    public void OnReviveButtonClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance.SpendCoins(GameManager.Instance.reviveCost);
        
        revivePanel.SetActive(false);
        if (joystickCanvas != null) joystickCanvas.SetActive(true);

        // <<< تغییر کلیدی و حیاتی: ارسال اعلان عمومی "ریست" >>>
        GameManager.OnStageRespawn?.Invoke();

        if (music != null) music.ResumeMusic();

        Debug.Log("Player chose to revive. Sent OnStageRespawn event.");
    }

    public void OnGiveUpButtonClicked()
    {
        Time.timeScale = 1f;
        revivePanel.SetActive(false);
        if (joystickCanvas != null) joystickCanvas.SetActive(true);
        
        GameManager.Instance.StageFailed();
    }
}