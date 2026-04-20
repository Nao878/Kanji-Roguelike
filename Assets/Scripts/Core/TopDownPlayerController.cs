using System.Collections;
using UnityEngine;

/// <summary>
/// 2D見下ろし型のプレイヤー移動コントローラー
/// WASD / 矢印キーでグリッド上を1マスずつ移動
/// </summary>
public class TopDownPlayerController : MonoBehaviour
{
    public FieldManager fieldManager;

    [Header("移動設定")]
    public float moveInterval = 0.15f; // 連続入力間隔

    private float moveTimer = 0f;
    private bool isMoving = false;

    private void Update()
    {
        // フィールドステート以外では入力を受け付けない
        var gm = GameManager.Instance;
        if (gm == null || gm.currentState != GameState.Field) return;
        if (fieldManager == null) return;
        
        // インベントリUIが開いている間は移動しない
        if (InventoryUIManager.Instance != null && InventoryUIManager.Instance.isInventoryOpen) return;

        moveTimer -= Time.deltaTime;

        Vector2Int dir = Vector2Int.zero;

        // Old Input System (もし有効なら)
        try
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(h) > 0.1f)
                dir = h > 0 ? Vector2Int.right : Vector2Int.left;
            else if (Mathf.Abs(v) > 0.1f)
                dir = v > 0 ? Vector2Int.up : Vector2Int.down;
        }
        catch (System.Exception)
        {
            // 旧InputがDisable設定例外を投げる場合は無視
        }

#if ENABLE_INPUT_SYSTEM
        // New Input System
        if (dir == Vector2Int.zero && UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir = Vector2Int.up;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir = Vector2Int.down;
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir = Vector2Int.left;
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir = Vector2Int.right;
        }
#endif

        if (dir != Vector2Int.zero && moveTimer <= 0f)
        {
            bool moved = fieldManager.TryMovePlayer(dir);
            if (moved)
            {
                moveTimer = moveInterval;
            }
        }
    }
}
