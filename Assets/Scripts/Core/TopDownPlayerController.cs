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

        moveTimer -= Time.deltaTime;
        if (moveTimer > 0f) return;

        Vector2Int dir = Vector2Int.zero;

        // WASD + 矢印キー
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            dir = Vector2Int.up;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            dir = Vector2Int.down;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            dir = Vector2Int.left;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            dir = Vector2Int.right;

        if (dir != Vector2Int.zero)
        {
            bool moved = fieldManager.TryMovePlayer(dir);
            if (moved)
            {
                moveTimer = moveInterval;
            }
        }
    }
}
