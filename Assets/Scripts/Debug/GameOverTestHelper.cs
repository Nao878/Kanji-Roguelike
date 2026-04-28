using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// テスト用：Playモード中にGameOver→ResetGameの一連の流れをテストするヘルパー
/// </summary>
public class GameOverTestHelper : MonoBehaviour
{
    [Header("テスト設定")]
    [SerializeField] private bool triggerGameOverOnStart = false;
    [SerializeField] private float gameOverDelay = 0.5f;
    [SerializeField] private float resetDelay = 2.0f;

    // 静的フラグ：シーンリロード後の再実行を防ぐ
    private static bool hasAlreadyRun = false;

    private float timer = 0f;
    private bool gameOverTriggered = false;
    private bool resetTriggered = false;

    private void OnEnable()
    {
        // Playモード新規開始時にフラグをリセット（シーンリロード後は再実行しない）
        // Application.isPlaying が false→true になった瞬間だけリセット
    }

    private void Awake()
    {
        // Playモード開始直後（シーンリロード前）のみ実行フラグをリセット
        // Time.frameCount == 0 なら最初のフレームと判定
        if (Time.frameCount == 0) hasAlreadyRun = false;
    }

    private void Update()
    {
        if (!triggerGameOverOnStart) return;
        if (hasAlreadyRun) return;  // 1回実行済みなら以降スキップ

        timer += Time.deltaTime;

        if (!gameOverTriggered && timer >= gameOverDelay)
        {
            gameOverTriggered = true;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                Debug.Log("[GameOverTest] GameOver状態を強制発動");
                gm.playerHP = 0;
                gm.ChangeState(GameState.GameOver);
            }
        }

        if (gameOverTriggered && !resetTriggered && timer >= gameOverDelay + resetDelay)
        {
            resetTriggered = true;
            hasAlreadyRun = true;  // 以降のシーンリロードで再実行しない
            var gm = GameManager.Instance;
            if (gm != null)
            {
                Debug.Log("[GameOverTest] ResetGame呼び出し（シーンリロード）");
                gm.ResetGame();
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GameOverTestHelper))]
public class GameOverTestHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("今すぐGameOver発動"))
            {
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    gm.playerHP = 0;
                    gm.ChangeState(GameState.GameOver);
                    Debug.Log("[GameOverTest] GameOver発動！");
                }
            }
        }
    }
}
#endif
