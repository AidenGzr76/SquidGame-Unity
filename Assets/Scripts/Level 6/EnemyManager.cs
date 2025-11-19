using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private List<SimpleEnemy> enemies = new List<SimpleEnemy>();

    public UIManager uiManager;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(SimpleEnemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(SimpleEnemy enemy)
    {
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);

        // اگر لیست خالی شد، یعنی همه مردن
        if (enemies.Count == 0)
        {
            Debug.Log("All enemies defeated! Player wins!");
            StartCoroutine(DelayPlayerWin());
        }
    }

    private IEnumerator DelayPlayerWin()
    {
        yield return new WaitForSeconds(2f); // ⏳ مکث قبل از برد
        PlayerSquidController player = FindFirstObjectByType<PlayerSquidController>();
        if (player != null)
            uiManager.ShowWinPanel();
            // player.PlayerWon();
    }
}
