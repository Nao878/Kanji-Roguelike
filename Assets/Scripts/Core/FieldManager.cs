using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 2D見下ろし型フィールドを管理するマネージャー
/// グリッド上にプレイヤーと敵シンボル（漢字）を配置し、
/// シンボルエンカウント方式で戦闘に遷移する
/// </summary>
public class FieldManager : MonoBehaviour
{
    [Header("フィールド設定")]
    public int gridWidth = 10;
    public int gridHeight = 8;
    public float cellSize = 64f;

    [Header("UI参照")]
    public RectTransform fieldContent;
    public TextMeshProUGUI inventoryCountText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI goldText;
    public Button deckButton;

    [Header("フォント")]
    public TMP_FontAsset appFont;

    // プレイヤー
    private GameObject playerObject;
    private Vector2Int playerGridPos;
    private TopDownPlayerController playerController;

    // 敵シンボル
    private List<FieldEnemy> fieldEnemies = new List<FieldEnemy>();

    // 背景タイル
    private List<GameObject> backgroundTiles = new List<GameObject>();

    [System.Serializable]
    public class FieldEnemy
    {
        public Vector2Int gridPos;
        public EnemyData enemyData;
        public GameObject uiObject;
        public bool isDefeated;
    }

    /// <summary>
    /// フィールド表示
    /// </summary>
    public void ShowField()
    {
        Debug.Log("[FieldManager] フィールド再生成実行: マップとシンボルを生成します");
        ClearField();
        CreateBackground();
        SpawnPlayer();
        SpawnEnemies();
        UpdateStatusUI();
    }

    private void Start()
    {
        if (deckButton != null)
        {
            deckButton.onClick.AddListener(() => {
                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameState.DeckEdit);
            });
        }
    }

    /// <summary>
    /// フィールドをクリア
    /// </summary>
    private void ClearField()
    {
        foreach (var tile in backgroundTiles)
        {
            if (tile != null) Destroy(tile);
        }
        backgroundTiles.Clear();

        foreach (var enemy in fieldEnemies)
        {
            if (enemy.uiObject != null) Destroy(enemy.uiObject);
        }
        fieldEnemies.Clear();

        if (playerObject != null) Destroy(playerObject);
    }

    /// <summary>
    /// 全データを初期化（リセット時用）
    /// </summary>
    public void ClearData()
    {
        ClearField();
        fieldEnemies.Clear();
        currentEncounterIndex = -1;
    }

    /// <summary>
    /// 背景グリッドを描画（和風テイスト）
    /// </summary>
    private void CreateBackground()
    {
        if (fieldContent == null) return;

        string[] groundKanji = { "草", "原", "野", "道", "土", "石" };

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var go = new GameObject($"Tile_{x}_{y}");
                go.transform.SetParent(fieldContent, false);

                var rect = go.AddComponent<RectTransform>();
                rect.anchoredPosition = GridToUIPosition(x, y);
                rect.sizeDelta = new Vector2(cellSize, cellSize);

                var img = go.AddComponent<Image>();
                // 市松模様で和風感
                bool isLight = (x + y) % 2 == 0;
                img.color = isLight
                    ? new Color(0.15f, 0.18f, 0.12f, 0.6f)
                    : new Color(0.12f, 0.15f, 0.10f, 0.6f);
                img.raycastTarget = false;

                // 地形漢字を背景に薄く表示
                var textGo = new GameObject("TerrainText");
                textGo.transform.SetParent(go.transform, false);
                var text = textGo.AddComponent<TextMeshProUGUI>();
                text.text = groundKanji[Random.Range(0, groundKanji.Length)];
                text.fontSize = 14;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(0.3f, 0.35f, 0.25f, 0.15f);
                text.raycastTarget = false;
                if (appFont != null) text.font = appFont;
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                backgroundTiles.Add(go);
            }
        }
    }

    /// <summary>
    /// プレイヤーを配置
    /// </summary>
    private void SpawnPlayer()
    {
        if (fieldContent == null) return;

        playerGridPos = new Vector2Int(1, 1);

        playerObject = new GameObject("Player");
        playerObject.transform.SetParent(fieldContent, false);

        var rect = playerObject.AddComponent<RectTransform>();
        rect.anchoredPosition = GridToUIPosition(playerGridPos.x, playerGridPos.y);
        rect.sizeDelta = new Vector2(cellSize * 0.9f, cellSize * 0.9f);

        // プレイヤーの見た目：漢字「人」
        var bg = playerObject.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.4f, 0.8f, 0.85f);
        bg.raycastTarget = false;

        var textGo = new GameObject("PlayerText");
        textGo.transform.SetParent(playerObject.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = "人";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        if (appFont != null) text.font = appFont;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // コントローラー追加
        playerController = playerObject.AddComponent<TopDownPlayerController>();
        playerController.fieldManager = this;

        // 最前面に表示
        playerObject.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 敵シンボルを配置
    /// </summary>
    private void SpawnEnemies()
    {
        if (fieldContent == null) return;

        var bm = GameManager.Instance?.battleManager;
        if (bm == null || bm.normalEnemies == null || bm.normalEnemies.Length == 0) return;

        // 未撃破の敵を再配置（初回はランダム配置）
        if (fieldEnemies.Count == 0)
        {
            int enemyCount = Mathf.Min(6, bm.normalEnemies.Length * 2);

            for (int i = 0; i < enemyCount; i++)
            {
                Vector2Int pos;
                int attempts = 0;
                do
                {
                    pos = new Vector2Int(Random.Range(2, gridWidth - 1), Random.Range(1, gridHeight - 1));
                    attempts++;
                } while (IsOccupied(pos) && attempts < 50);

                if (attempts >= 50) continue;

                var enemyData = bm.normalEnemies[Random.Range(0, bm.normalEnemies.Length)];
                var fieldEnemy = new FieldEnemy
                {
                    gridPos = pos,
                    enemyData = enemyData,
                    isDefeated = false
                };

                fieldEnemy.uiObject = CreateEnemySymbol(fieldEnemy);
                fieldEnemies.Add(fieldEnemy);
            }

            // ボスを右端に配置
            if (bm.bossEnemy != null)
            {
                var bossPos = new Vector2Int(gridWidth - 2, gridHeight / 2);
                var bossEnemy = new FieldEnemy
                {
                    gridPos = bossPos,
                    enemyData = bm.bossEnemy,
                    isDefeated = false
                };
                bossEnemy.uiObject = CreateEnemySymbol(bossEnemy);
                fieldEnemies.Add(bossEnemy);
            }
        }
        else
        {
            // 復帰時は未撃破の敵のUIのみ再生成
            foreach (var enemy in fieldEnemies)
            {
                if (!enemy.isDefeated && enemy.uiObject == null)
                {
                    enemy.uiObject = CreateEnemySymbol(enemy);
                }
            }
        }
    }

    /// <summary>
    /// 敵シンボルUI生成
    /// </summary>
    private GameObject CreateEnemySymbol(FieldEnemy enemy)
    {
        var go = new GameObject($"Enemy_{enemy.enemyData.displayKanji}");
        go.transform.SetParent(fieldContent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = GridToUIPosition(enemy.gridPos.x, enemy.gridPos.y);
        rect.sizeDelta = new Vector2(cellSize * 0.85f, cellSize * 0.85f);

        var bg = go.AddComponent<Image>();
        bool isBoss = enemy.enemyData.enemyType == EnemyType.Boss;
        bg.color = isBoss
            ? new Color(0.8f, 0.15f, 0.15f, 0.85f)
            : new Color(0.6f, 0.2f, 0.2f, 0.75f);
        bg.raycastTarget = false;

        var textGo = new GameObject("EnemyText");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = enemy.enemyData.displayKanji;
        text.fontSize = isBoss ? 32 : 28;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        if (appFont != null) text.font = appFont;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return go;
    }

    /// <summary>
    /// プレイヤーの移動処理
    /// </summary>
    public bool TryMovePlayer(Vector2Int direction)
    {
        Vector2Int newPos = playerGridPos + direction;

        // 境界チェック
        if (newPos.x < 0 || newPos.x >= gridWidth || newPos.y < 0 || newPos.y >= gridHeight)
            return false;

        playerGridPos = newPos;

        // UI位置更新
        if (playerObject != null)
        {
            var rect = playerObject.GetComponent<RectTransform>();
            rect.anchoredPosition = GridToUIPosition(playerGridPos.x, playerGridPos.y);
            playerObject.transform.SetAsLastSibling();
        }

        // エンカウントチェック
        CheckEncounter();

        return true;
    }

    /// <summary>
    /// エンカウントチェック（プレイヤーと敵の座標一致）
    /// </summary>
    private void CheckEncounter()
    {
        for (int i = fieldEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = fieldEnemies[i];
            if (enemy.isDefeated) continue;

            if (enemy.gridPos == playerGridPos)
            {
                Debug.Log($"[FieldManager] エンカウント！ 敵: {enemy.enemyData.displayKanji}");
                currentEncounterIndex = i;

                // 戦闘開始
                var bm = GameManager.Instance?.battleManager;
                if (bm != null)
                {
                    bm.StartBattle(enemy.enemyData);
                }
                return;
            }
        }
    }

    // 現在エンカウント中の敵インデックス
    private int currentEncounterIndex = -1;

    /// <summary>
    /// 戦闘勝利後に呼ばれる：敵を撃破済みにする
    /// </summary>
    public void OnBattleWon()
    {
        if (currentEncounterIndex >= 0 && currentEncounterIndex < fieldEnemies.Count)
        {
            var enemy = fieldEnemies[currentEncounterIndex];
            enemy.isDefeated = true;
            if (enemy.uiObject != null)
            {
                Destroy(enemy.uiObject);
                enemy.uiObject = null;
            }
            Debug.Log($"[FieldManager] 敵撃破: {enemy.enemyData.displayKanji}");
        }
        currentEncounterIndex = -1;

        UpdateStatusUI();

        // 全敵撃破チェック
        bool allDefeated = true;
        foreach (var e in fieldEnemies)
        {
            if (!e.isDefeated) { allDefeated = false; break; }
        }
        if (allDefeated)
        {
            Debug.Log("[FieldManager] 全敵撃破！ フィールドクリア！");
        }
    }

    /// <summary>
    /// ステータスUI更新
    /// </summary>
    public void UpdateStatusUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (inventoryCountText != null)
            inventoryCountText.text = $"所持: {gm.inventory.Count}/{gm.inventoryMaxSize}";
        if (hpText != null)
            hpText.text = $"HP: {gm.playerHP}/{gm.playerMaxHP}";
        if (goldText != null)
            goldText.text = $"金: {gm.playerGold}G";
    }

    /// <summary>
    /// グリッド座標→UI座標変換
    /// </summary>
    private Vector2 GridToUIPosition(int x, int y)
    {
        float startX = -(gridWidth * cellSize) / 2f + cellSize / 2f;
        float startY = -(gridHeight * cellSize) / 2f + cellSize / 2f;
        return new Vector2(startX + x * cellSize, startY + y * cellSize);
    }

    /// <summary>
    /// 指定座標が占有されているか
    /// </summary>
    private bool IsOccupied(Vector2Int pos)
    {
        if (pos == playerGridPos) return true;
        foreach (var e in fieldEnemies)
        {
            if (!e.isDefeated && e.gridPos == pos) return true;
        }
        return false;
    }
}
