using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// エディタから自動テストを実行するツール
/// バグ修正の検証用
/// </summary>
public class AutoBattleVerifier : EditorWindow
{
    [MenuItem("Tools/Verify Battle Fixes")]
    public static void VerifyBattleFixes()
    {
        Debug.Log("=== バトル修正検証テスト開始 ===");
        
        // GameManagerの参照チェック
        var gm = GameObject.Find("GameManager")?.GetComponent<GameManager>();
        if (gm == null) { Debug.LogError("GameManagerが見つかりません"); return; }
        
        var bm = GameObject.Find("BattleManager")?.GetComponent<BattleManager>();
        if (bm == null) { Debug.LogError("BattleManagerが見つかりません"); return; }
        
        // ✅ テスト1: GameOverPanelの参照チェック
        if (gm.gameOverPanel != null)
            Debug.Log("✅ テスト1: GameManager.gameOverPanel が正しく設定されている");
        else
            Debug.LogError("❌ テスト1: GameManager.gameOverPanel がnull");
        
        if (bm.gameOverPanel != null)
            Debug.Log("✅ テスト1b: BattleManager.gameOverPanel が正しく設定されている");
        else
            Debug.LogError("❌ テスト1b: BattleManager.gameOverPanel がnull");
        
        // ✅ テスト2: BattleUIのResetEnemyDisplayメソッド存在チェック
        if (bm.battleUI != null)
        {
            Debug.Log("✅ テスト2: BattleUI 参照が設定されている");
            
            // enemyAreaの確認
            if (bm.battleUI.enemyArea != null)
            {
                Debug.Log("✅ テスト2b: enemyArea 参照が設定されている");
                
                // Enemyタグチェック
                if (bm.battleUI.enemyArea.CompareTag("Enemy"))
                    Debug.Log("✅ テスト2c: enemyArea のタグが 'Enemy'");
                else
                    Debug.LogWarning("⚠ テスト2c: enemyArea のタグが 'Enemy' ではありません: " + bm.battleUI.enemyArea.tag);
            }
            else
                Debug.LogError("❌ テスト2b: enemyArea がnull");
        }
        else
            Debug.LogError("❌ テスト2: BattleUI 参照がnull");
        
        // ✅ テスト3: GameOverPanelの構造チェック
        if (gm.gameOverPanel != null)
        {
            var defeatText = gm.gameOverPanel.transform.Find("DefeatText");
            var retryButton = gm.gameOverPanel.transform.Find("RetryButton");
            
            if (defeatText != null)
                Debug.Log("✅ テスト3a: 「敗北」テキストが存在");
            else
                Debug.LogError("❌ テスト3a: 「敗北」テキストが見つからない");
            
            if (retryButton != null)
                Debug.Log("✅ テスト3b: 「最初から」ボタンが存在");
            else
                Debug.LogError("❌ テスト3b: 「最初から」ボタンが見つからない");
            
            // 初期状態が非表示であることを確認
            if (!gm.gameOverPanel.activeSelf)
                Debug.Log("✅ テスト3c: GameOverPanelが初期状態で非表示");
            else
                Debug.LogWarning("⚠ テスト3c: GameOverPanelが初期状態で表示されている");
        }
        
        // ✅ テスト4: ResetGameメソッドの存在チェック
        var resetMethod = typeof(GameManager).GetMethod("ResetGame");
        if (resetMethod != null)
            Debug.Log("✅ テスト4: GameManager.ResetGame() メソッドが存在");
        else
            Debug.LogError("❌ テスト4: GameManager.ResetGame() メソッドが見つからない");
        
        // ✅ テスト5: ResetEnemyDisplayメソッドの存在チェック
        var resetDisplayMethod = typeof(BattleUI).GetMethod("ResetEnemyDisplay");
        if (resetDisplayMethod != null)
            Debug.Log("✅ テスト5: BattleUI.ResetEnemyDisplay() メソッドが存在");
        else
            Debug.LogError("❌ テスト5: BattleUI.ResetEnemyDisplay() メソッドが見つからない");
        
        // ✅ テスト6: 敵データのnullチェック（土、水）
        if (bm.normalEnemies != null)
        {
            bool anyNull = false;
            foreach (var enemy in bm.normalEnemies)
            {
                if (enemy == null)
                {
                    anyNull = true;
                    Debug.LogError("❌ テスト6: normalEnemies配列にnullが含まれています");
                    break;
                }
                if (string.IsNullOrEmpty(enemy.displayKanji))
                {
                    anyNull = true;
                    Debug.LogError($"❌ テスト6: 敵 '{enemy.enemyName}' のdisplayKanjiが空です");
                    break;
                }
            }
            if (!anyNull)
                Debug.Log($"✅ テスト6: 全{bm.normalEnemies.Length}体の敵データが正常");
        }
        
        Debug.Log("=== バトル修正検証テスト完了 ===");
    }
}
