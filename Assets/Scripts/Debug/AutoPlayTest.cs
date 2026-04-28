using UnityEngine;

/// <summary>
/// Playモード自動テスト：戦闘フローとゲームオーバー画面を検証
/// WaitForSecondsなしで即座に実行（MCP互換）
/// runOnStart を true にした場合のみ自動実行（デフォルトはfalse）
/// </summary>
public class AutoPlayTest : MonoBehaviour
{
    [SerializeField] private bool runOnStart = false;

    private int testsPassed = 0;
    private int testsFailed = 0;
    private bool testDone = false;
    private int frameCount = 0;

    void Update()
    {
        if (!runOnStart) return;
        if (testDone) return;

        frameCount++;

        // 3フレーム目で実行（初期化完了を待つ）
        if (frameCount == 3)
        {
            RunAllTests();
            testDone = true;
        }
    }

    void RunAllTests()
    {
        Debug.Log("=== Playモード自動テスト開始 ===");
        
        var gm = GameManager.Instance;
        var bm = gm?.battleManager;
        if (gm == null || bm == null)
        {
            Debug.LogError("[AutoTest] GameManager/BattleManager が見つかりません");
            return;
        }

        // ========== テスト1: 1回目の戦闘 ==========
        Debug.Log("[AutoTest] === テスト1: 1回目の戦闘 ===");
        bm.StartRandomBattle();
        
        if (bm.battleUI != null && bm.battleUI.enemyArea != null)
        {
            LogResult("テスト1-1", "1回目の戦闘で敵エリアがアクティブ", 
                bm.battleUI.enemyArea.activeSelf);
        }
        
        LogResult("テスト1-2", $"敵データが正常 '{bm.currentEnemyData?.displayKanji}' HP={bm.enemyCurrentHP}", 
            bm.enemyCurrentHP > 0 && bm.currentEnemyData != null);

        // 1回目の戦闘を終了してフィールドに戻る
        gm.hand.Clear();
        gm.ChangeState(GameState.Field);

        // ========== テスト2: 2回目の戦闘（透明バグ検証） ==========
        Debug.Log("[AutoTest] === テスト2: 2回目の戦闘（透明バグ検証） ===");
        bm.StartRandomBattle();
        
        if (bm.battleUI != null && bm.battleUI.enemyArea != null)
        {
            LogResult("テスト2-1", "2回目の戦闘で敵エリアがアクティブ（透明バグ修正確認）", 
                bm.battleUI.enemyArea.activeSelf);
            
            if (bm.battleUI.enemyKanjiText != null)
            {
                float alpha = bm.battleUI.enemyKanjiText.alpha;
                LogResult("テスト2-2", $"敵漢字テキストのalpha={alpha:F2}", alpha >= 0.9f);
            }
            
            LogResult("テスト2-3", "敵エリアのタグが'Enemy'（ドラッグ攻撃可能）", 
                bm.battleUI.enemyArea.CompareTag("Enemy"));
        }

        // ========== テスト3: ゲームオーバー画面 ==========
        Debug.Log("[AutoTest] === テスト3: ゲームオーバー画面 ===");
        
        gm.playerHP = 0;
        gm.ChangeState(GameState.GameOver);
        
        LogResult("テスト3-1", "GameOverPanelが表示されている", 
            gm.gameOverPanel != null && gm.gameOverPanel.activeSelf);
        
        if (gm.gameOverPanel != null)
        {
            var defeatText = gm.gameOverPanel.transform.Find("DefeatText");
            LogResult("テスト3-2", "敗北テキストが存在", 
                defeatText != null && defeatText.gameObject.activeSelf);
            
            var retryBtn = gm.gameOverPanel.transform.Find("RetryButton");
            LogResult("テスト3-3", "最初からボタンが存在", 
                retryBtn != null && retryBtn.gameObject.activeSelf);
        }

        // ========== 結果サマリー ==========
        Debug.Log($"=== テスト完了: {testsPassed}件合格 / {testsFailed}件失敗 ===");
        
        if (testsFailed == 0)
            Debug.Log("[AutoTest] 全テスト合格！修正は正常に動作しています");
        else
            Debug.LogError($"[AutoTest] {testsFailed}件のテストが失敗");
    }

    void LogResult(string id, string desc, bool passed)
    {
        if (passed)
        {
            testsPassed++;
            Debug.Log($"PASS {id}: {desc}");
        }
        else
        {
            testsFailed++;
            Debug.LogError($"FAIL {id}: {desc}");
        }
    }
}
