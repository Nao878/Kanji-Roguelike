using UnityEngine;
using UnityEditor;

public static class TestRunner {
    [MenuItem("Test/RunRetryTest")]
    public static void Run() {
        var gm = GameManager.Instance;
        if (gm != null) {
            Debug.Log("[TestRunner] Forcing Game Over...");
            gm.playerHP = 0;
            gm.ChangeState(GameState.GameOver);
            
            Debug.Log("[TestRunner] Invoking Retry Button...");
            var btn = gm.gameOverPanel.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (btn != null) {
                btn.onClick.Invoke();
                Debug.Log("[TestRunner] Retry Button Invoked!");
            } else {
                Debug.LogError("[TestRunner] Retry Button not found!");
            }
        } else {
            Debug.LogError("[TestRunner] GameManager instance not found!");
        }
    }
}
