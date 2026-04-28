using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// デバッグ用：ゲーム開始直後に自動で戦闘を開始するテストヘルパー
/// Bキーで戦闘開始、Fキーで1 MORE演出テスト
/// </summary>
public class DebugBattleHelper : MonoBehaviour
{
    void Update()
    {
        // Bキーでランダムバトル開始
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.battleManager != null && gm.currentState == GameState.Field)
            {
                gm.battleManager.StartRandomBattle();
                Debug.Log("[DebugHelper] ランダムバトル開始！");
            }
        }

        // Fキーで1 MORE演出テスト
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.currentState == GameState.Battle)
            {
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayOneMoreEffect();
                    gm.playerMana += 1;
                    Debug.Log($"[DebugHelper] 1 MORE演出テスト！ AP+1 (現在AP:{gm.playerMana})");
                    // ステータスUI更新
                    if (gm.battleManager != null && gm.battleManager.battleUI != null)
                    {
                        gm.battleManager.battleUI.UpdateStatusUI();
                    }
                }
            }
        }
    }
}
