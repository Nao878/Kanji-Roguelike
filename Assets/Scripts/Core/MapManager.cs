using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Slay the Spireライクのマップ管理
/// </summary>
public class MapManager : MonoBehaviour
{
    [Header("マップ設定")]
    public int totalLayers = 5;
    public int nodesPerLayer = 3;
    public int currentLayer = 0;
    public int currentNodeIndex = -1;

    [Header("UI参照")]
    public Transform mapContent;
    public GameObject nodeButtonPrefab;
    public TextMeshProUGUI floorText;

    [Header("フォント")]
    public TMP_FontAsset appFont;

    // マップデータ
    private List<List<MapNode>> mapData = new List<List<MapNode>>();

    /// <summary>
    /// マップノードのデータ
    /// </summary>
    [System.Serializable]
    public class MapNode
    {
        public NodeType nodeType;
        public bool isVisited = false;
        public bool isAccessible = false;
        public int layerIndex;
        public int nodeIndex;
        public List<int> connections = new List<int>(); // 次の層への接続先インデックス
        public Button uiButton;
        public GameObject uiObject;
    }

    public enum NodeType
    {
        Battle,
        Elite,
        Shop,
        Event,
        Boss
    }

    /// <summary>
    /// マップを生成
    /// </summary>
    public void GenerateMap()
    {
        mapData.Clear();
        currentLayer = 0;
        currentNodeIndex = -1;

        // AppFontをロード（未設定の場合）
        if (appFont == null)
        {
            appFont = Resources.Load<TMP_FontAsset>("Fonts/AppFont SDF");
        }

        for (int layer = 0; layer < totalLayers; layer++)
        {
            var layerNodes = new List<MapNode>();
            int nodeCount = (layer == totalLayers - 1) ? 1 : nodesPerLayer; // 最終層はボス1つ

            for (int n = 0; n < nodeCount; n++)
            {
                var node = new MapNode
                {
                    layerIndex = layer,
                    nodeIndex = n,
                    nodeType = DetermineNodeType(layer),
                    isAccessible = (layer == 0) // 最初の層は全てアクセス可能
                };

                // 次の層への接続を設定
                if (layer < totalLayers - 1)
                {
                    int nextLayerCount = (layer + 1 == totalLayers - 1) ? 1 : nodesPerLayer;
                    // 自分のインデックスと隣接ノードに接続
                    for (int c = Mathf.Max(0, n - 1); c <= Mathf.Min(nextLayerCount - 1, n + 1); c++)
                    {
                        node.connections.Add(c);
                    }
                    // 最低1つの接続を保証
                    if (node.connections.Count == 0 && nextLayerCount > 0)
                    {
                        node.connections.Add(0);
                    }
                }

                layerNodes.Add(node);
            }

            mapData.Add(layerNodes);
        }

        Debug.Log($"[MapManager] マップ生成完了 {totalLayers}層");
    }

    /// <summary>
    /// ノードタイプをランダムに決定
    /// </summary>
    private NodeType DetermineNodeType(int layer)
    {
        if (layer == totalLayers - 1) return NodeType.Boss;

        float rand = Random.value;
        if (rand < 0.5f) return NodeType.Battle;
        if (rand < 0.7f) return NodeType.Event;
        if (rand < 0.85f) return NodeType.Elite;
        return NodeType.Shop;
    }

    /// <summary>
    /// マップUIを表示
    /// </summary>
    public void ShowMap()
    {
        if (mapData.Count == 0) GenerateMap();
        UpdateMapUI();
    }

    /// <summary>
    /// マップUIを更新
    /// </summary>
    private void UpdateMapUI()
    {
        if (mapContent == null) return;

        // 既存のノードUIをクリア
        foreach (var layer in mapData)
        {
            foreach (var node in layer)
            {
                if (node.uiObject != null) Destroy(node.uiObject);
            }
        }

        // 各ノードを再生成
        for (int layer = 0; layer < mapData.Count; layer++)
        {
            for (int n = 0; n < mapData[layer].Count; n++)
            {
                var node = mapData[layer][n];
                CreateNodeUI(node, layer, n);
            }
        }

        if (floorText != null)
        {
            floorText.text = $"階層: {currentLayer + 1} / {totalLayers}";
        }
    }

    /// <summary>
    /// ノードのUIを作成
    /// </summary>
    private void CreateNodeUI(MapNode node, int layer, int index)
    {
        if (mapContent == null) return;

        // ノードボタンを作成
        var go = new GameObject($"Node_{layer}_{index}");
        go.transform.SetParent(mapContent, false);

        var rect = go.AddComponent<RectTransform>();
        float xOffset = (index - (mapData[layer].Count - 1) / 2f) * 140f;
        float yOffset = (totalLayers - 1 - layer) * 80f - (totalLayers * 40f - 40f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(120f, 55f);

        var image = go.AddComponent<Image>();
        var button = go.AddComponent<Button>();

        // ノードタイプに応じた色設定
        Color nodeColor = GetNodeColor(node.nodeType);
        image.color = node.isAccessible ? nodeColor : new Color(nodeColor.r * 0.4f, nodeColor.g * 0.4f, nodeColor.b * 0.4f, 0.5f);

        if (node.isVisited)
        {
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        // ノードテキスト
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = GetNodeSymbol(node.nodeType);
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        if (appFont != null) text.font = appFont;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // ボタンクリック処理
        button.interactable = node.isAccessible && !node.isVisited;
        int capturedLayer = layer;
        int capturedIndex = index;
        button.onClick.AddListener(() => OnNodeClicked(capturedLayer, capturedIndex));

        node.uiButton = button;
        node.uiObject = go;
    }

    /// <summary>
    /// ノードタイプに応じた色を取得
    /// </summary>
    private Color GetNodeColor(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return new Color(0.2f, 0.6f, 0.9f);
            case NodeType.Elite: return new Color(0.9f, 0.6f, 0.2f);
            case NodeType.Shop: return new Color(0.2f, 0.9f, 0.4f);
            case NodeType.Event: return new Color(0.7f, 0.5f, 0.9f);
            case NodeType.Boss: return new Color(0.9f, 0.2f, 0.2f);
            default: return Color.gray;
        }
    }

    /// <summary>
    /// ノードタイプのシンボル文字を取得
    /// </summary>
    private string GetNodeSymbol(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return "⚔ 戦闘";
            case NodeType.Elite: return "★ 強敵";
            case NodeType.Shop: return "$ 商店";
            case NodeType.Event: return "？ 事件";
            case NodeType.Boss: return "☠ ボス";
            default: return "？";
        }
    }

    /// <summary>
    /// ノードがクリックされた時
    /// </summary>
    public void OnNodeClicked(int layer, int index)
    {
        if (layer >= mapData.Count || index >= mapData[layer].Count) return;

        var node = mapData[layer][index];
        if (!node.isAccessible || node.isVisited) return;

        Debug.Log($"[MapManager] ノード選択: 層{layer} ノード{index} タイプ:{node.nodeType}");

        // 現在ノードを訪問済みに
        node.isVisited = true;
        currentLayer = layer;
        currentNodeIndex = index;

        // 次の層のアクセス可能ノードを更新
        UpdateAccessibleNodes(layer, index);

        // ノードタイプに応じた処理
        switch (node.nodeType)
        {
            case NodeType.Battle:
            case NodeType.Elite:
            case NodeType.Boss:
                if (GameManager.Instance != null && GameManager.Instance.battleManager != null)
                {
                    GameManager.Instance.battleManager.StartRandomBattle();
                }
                break;

            case NodeType.Shop:
                Debug.Log("[MapManager] ショップ（未実装）");
                // 合体画面を開く代わりに
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameState.Fusion);
                }
                break;

            case NodeType.Event:
                Debug.Log("[MapManager] イベント発生！ HPを5回復");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.playerHP = Mathf.Min(
                        GameManager.Instance.playerMaxHP,
                        GameManager.Instance.playerHP + 5
                    );
                }
                UpdateMapUI();
                break;
        }
    }

    /// <summary>
    /// アクセス可能なノードを更新
    /// </summary>
    private void UpdateAccessibleNodes(int currentLayerIdx, int currentNodeIdx)
    {
        // 全ノードのアクセス可能性をリセット
        foreach (var layer in mapData)
        {
            foreach (var n in layer)
            {
                if (!n.isVisited) n.isAccessible = false;
            }
        }

        // 現在ノードの接続先をアクセス可能に
        var currentNode = mapData[currentLayerIdx][currentNodeIdx];
        int nextLayer = currentLayerIdx + 1;
        if (nextLayer < mapData.Count)
        {
            foreach (int connIdx in currentNode.connections)
            {
                if (connIdx < mapData[nextLayer].Count)
                {
                    mapData[nextLayer][connIdx].isAccessible = true;
                }
            }
        }
    }
}
