using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 漢字マップシステム - Slay the Spire風ボトムからトップへ進むルートマップ
/// 背景に地形漢字を散りばめた和風デザイン
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
    public TextMeshProUGUI goldText;
    public Transform backgroundArea;

    [Header("フォント")]
    public TMP_FontAsset appFont;

    // マップデータ
    private List<List<MapNode>> mapData = new List<List<MapNode>>();
    private List<GameObject> backgroundKanjis = new List<GameObject>();

    [System.Serializable]
    public class MapNode
    {
        public NodeType nodeType;
        public bool isVisited = false;
        public bool isAccessible = false;
        public int layerIndex;
        public int nodeIndex;
        public List<int> connections = new List<int>();
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

        for (int layer = 0; layer < totalLayers; layer++)
        {
            var layerNodes = new List<MapNode>();
            int nodeCount = (layer == totalLayers - 1) ? 1 : nodesPerLayer;

            for (int n = 0; n < nodeCount; n++)
            {
                var node = new MapNode
                {
                    layerIndex = layer,
                    nodeIndex = n,
                    nodeType = DetermineNodeType(layer),
                    isAccessible = (layer == 0)
                };

                if (layer < totalLayers - 1)
                {
                    int nextLayerCount = (layer + 1 == totalLayers - 1) ? 1 : nodesPerLayer;
                    for (int c = Mathf.Max(0, n - 1); c <= Mathf.Min(nextLayerCount - 1, n + 1); c++)
                    {
                        node.connections.Add(c);
                    }
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

    private NodeType DetermineNodeType(int layer)
    {
        if (layer == totalLayers - 1) return NodeType.Boss;

        float rand = Random.value;
        if (rand < 0.4f) return NodeType.Battle;
        if (rand < 0.6f) return NodeType.Event;
        if (rand < 0.75f) return NodeType.Elite;
        return NodeType.Shop;
    }

    /// <summary>
    /// マップ表示
    /// </summary>
    public void ShowMap()
    {
        if (mapData.Count == 0) GenerateMap();
        CreateBackgroundKanji();
        UpdateMapUI();
        UpdateGoldDisplay();
    }

    /// <summary>
    /// 背景に地形漢字を散りばめる
    /// </summary>
    private void CreateBackgroundKanji()
    {
        // 既存の背景漢字をクリア
        foreach (var go in backgroundKanjis)
        {
            if (go != null) Destroy(go);
        }
        backgroundKanjis.Clear();

        if (backgroundArea == null) return;

        string[] terrainKanji = { "山", "川", "森", "草", "岩", "泉", "丘", "谷", "湖", "野", "峠", "崖", "沼", "原", "峰" };
        Color[] terrainColors = {
            new Color(0.25f, 0.22f, 0.18f, 0.35f),  // 山 - 茶系
            new Color(0.15f, 0.22f, 0.30f, 0.35f),  // 川 - 青系
            new Color(0.15f, 0.25f, 0.15f, 0.35f),  // 森 - 緑系
        };

        // 密集したテクスチャのように配置
        int cols = 14;
        int rows = 8;

        RectTransform bgRect = backgroundArea.GetComponent<RectTransform>();
        if (bgRect == null) return;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var go = new GameObject($"BgKanji_{row}_{col}");
                go.transform.SetParent(backgroundArea, false);

                var rect = go.AddComponent<RectTransform>();
                float xNorm = (col + 0.5f) / cols;
                float yNorm = (row + 0.5f) / rows;
                // 少しランダムにずらす
                xNorm += Random.Range(-0.02f, 0.02f);
                yNorm += Random.Range(-0.02f, 0.02f);

                rect.anchorMin = new Vector2(xNorm - 0.03f, yNorm - 0.05f);
                rect.anchorMax = new Vector2(xNorm + 0.03f, yNorm + 0.05f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var text = go.AddComponent<TextMeshProUGUI>();
                // 川を縦に並べて流れを表現
                if (col == 4 || col == 10)
                {
                    text.text = "川";
                    text.color = new Color(0.15f, 0.25f, 0.35f, 0.4f);
                }
                // 山を上部に集中
                else if (row >= rows - 2)
                {
                    text.text = Random.value > 0.3f ? "山" : "峰";
                    text.color = new Color(0.28f, 0.24f, 0.20f, 0.35f);
                }
                // 森を中央に
                else if (row >= 2 && row < rows - 2)
                {
                    string[] midKanji = { "森", "林", "草", "木", "野" };
                    text.text = midKanji[Random.Range(0, midKanji.Length)];
                    text.color = new Color(0.18f, 0.28f, 0.18f, 0.3f);
                }
                // 下部は草原
                else
                {
                    string[] lowKanji = { "草", "原", "野", "丘" };
                    text.text = lowKanji[Random.Range(0, lowKanji.Length)];
                    text.color = new Color(0.22f, 0.28f, 0.20f, 0.25f);
                }

                text.fontSize = 22;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                if (appFont != null) text.font = appFont;

                // ランダムに少し回転
                go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-8f, 8f));

                backgroundKanjis.Add(go);
            }
        }
    }

    /// <summary>
    /// マップUIを更新（ボトムからトップへ）
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

        // 各ノードを再生成（ボトムからトップへ）
        for (int layer = 0; layer < mapData.Count; layer++)
        {
            for (int n = 0; n < mapData[layer].Count; n++)
            {
                var node = mapData[layer][n];
                CreateNodeUI(node, layer, n);
            }
        }

        // 接続線を描画
        DrawConnections();

        if (floorText != null)
        {
            floorText.text = $"階層: {currentLayer + 1} / {totalLayers}";
        }
    }

    /// <summary>
    /// ノードUIを作成（漢字ラベル＋枠線デザイン）
    /// </summary>
    private void CreateNodeUI(MapNode node, int layer, int index)
    {
        if (mapContent == null) return;

        var go = new GameObject($"Node_{layer}_{index}");
        go.transform.SetParent(mapContent, false);

        var rect = go.AddComponent<RectTransform>();
        // ボトムからトップへ配置
        float nodeCount = mapData[layer].Count;
        float xOffset = (index - (nodeCount - 1) / 2f) * 140f;
        float yOffset = layer * 70f - ((totalLayers - 1) * 35f);
        rect.anchoredPosition = new Vector2(xOffset, yOffset);
        rect.sizeDelta = new Vector2(110f, 50f);

        // 外枠（Border）
        var borderImage = go.AddComponent<Image>();
        // 背景色と枠線色を設定
        Color nodeColor = GetNodeColor(node.nodeType);
        Color borderColor = new Color(nodeColor.r + 0.2f, nodeColor.g + 0.2f, nodeColor.b + 0.2f, 1f);

        if (node.isVisited)
        {
            borderImage.color = new Color(0.25f, 0.25f, 0.25f, 0.6f);
        }
        else if (node.isAccessible)
        {
            borderImage.color = borderColor;
        }
        else
        {
            borderImage.color = new Color(nodeColor.r * 0.3f, nodeColor.g * 0.3f, nodeColor.b * 0.3f, 0.5f);
        }

        // 内側背景
        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        var innerRect = innerGo.AddComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0.04f, 0.08f);
        innerRect.anchorMax = new Vector2(0.96f, 0.92f);
        innerRect.offsetMin = Vector2.zero;
        innerRect.offsetMax = Vector2.zero;
        var innerImage = innerGo.AddComponent<Image>();
        innerImage.color = node.isAccessible ?
            new Color(nodeColor.r * 0.5f, nodeColor.g * 0.5f, nodeColor.b * 0.5f, 0.9f) :
            new Color(0.1f, 0.1f, 0.12f, 0.8f);
        innerImage.raycastTarget = false;

        // ボタン
        var button = go.AddComponent<Button>();
        button.interactable = node.isAccessible && !node.isVisited;

        // ノードラベル（漢字表記）
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = GetNodeLabel(node.nodeType);
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.color = node.isAccessible ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        text.raycastTarget = false;
        if (appFont != null) text.font = appFont;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // クリック処理
        int capturedLayer = layer;
        int capturedIndex = index;
        button.onClick.AddListener(() => OnNodeClicked(capturedLayer, capturedIndex));

        node.uiButton = button;
        node.uiObject = go;
    }

    /// <summary>
    /// ノード間の接続線を描画
    /// </summary>
    private void DrawConnections()
    {
        if (mapContent == null) return;

        // 既存の接続線を削除
        foreach (Transform child in mapContent)
        {
            if (child.name.StartsWith("Line_"))
            {
                Destroy(child.gameObject);
            }
        }

        for (int layer = 0; layer < mapData.Count - 1; layer++)
        {
            for (int n = 0; n < mapData[layer].Count; n++)
            {
                var node = mapData[layer][n];
                if (node.uiObject == null) continue;

                var fromRect = node.uiObject.GetComponent<RectTransform>();
                Vector2 fromPos = fromRect.anchoredPosition + new Vector2(0, 25f);

                foreach (int connIdx in node.connections)
                {
                    if (connIdx >= mapData[layer + 1].Count) continue;
                    var targetNode = mapData[layer + 1][connIdx];
                    if (targetNode.uiObject == null) continue;

                    var toRect = targetNode.uiObject.GetComponent<RectTransform>();
                    Vector2 toPos = toRect.anchoredPosition - new Vector2(0, 25f);

                    CreateLine(fromPos, toPos, node.isVisited || targetNode.isAccessible);
                }
            }
        }
    }

    private void CreateLine(Vector2 from, Vector2 to, bool active)
    {
        var lineGo = new GameObject($"Line_{from}_{to}");
        lineGo.transform.SetParent(mapContent, false);
        lineGo.transform.SetAsFirstSibling();

        var lineRect = lineGo.AddComponent<RectTransform>();
        Vector2 midPoint = (from + to) / 2f;
        lineRect.anchoredPosition = midPoint;

        float distance = Vector2.Distance(from, to);
        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        lineRect.sizeDelta = new Vector2(distance, 2f);
        lineRect.rotation = Quaternion.Euler(0, 0, angle);

        var img = lineGo.AddComponent<Image>();
        img.color = active ?
            new Color(0.7f, 0.7f, 0.5f, 0.6f) :
            new Color(0.3f, 0.3f, 0.3f, 0.3f);
        img.raycastTarget = false;
    }

    /// <summary>
    /// ゴールド表示を更新
    /// </summary>
    public void UpdateGoldDisplay()
    {
        if (goldText != null && GameManager.Instance != null)
        {
            goldText.text = $"金: {GameManager.Instance.playerGold}G";
        }
    }

    // ノードタイプ → 漢字ラベル
    private string GetNodeLabel(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return "戦闘";
            case NodeType.Elite:  return "強敵";
            case NodeType.Shop:   return "商店";
            case NodeType.Event:  return "事件";
            case NodeType.Boss:   return "大将";
            default: return "？";
        }
    }

    // ノードタイプ → 色
    private Color GetNodeColor(NodeType type)
    {
        switch (type)
        {
            case NodeType.Battle: return new Color(0.3f, 0.5f, 0.8f);
            case NodeType.Elite:  return new Color(0.9f, 0.6f, 0.2f);
            case NodeType.Shop:   return new Color(0.3f, 0.8f, 0.4f);
            case NodeType.Event:  return new Color(0.6f, 0.4f, 0.8f);
            case NodeType.Boss:   return new Color(0.9f, 0.2f, 0.2f);
            default: return Color.gray;
        }
    }

    /// <summary>
    /// ノードクリック処理
    /// </summary>
    public void OnNodeClicked(int layer, int index)
    {
        if (layer >= mapData.Count || index >= mapData[layer].Count) return;

        var node = mapData[layer][index];
        if (!node.isAccessible || node.isVisited) return;

        Debug.Log($"[MapManager] ノード選択: 層{layer} ノード{index} タイプ:{node.nodeType}");

        node.isVisited = true;
        currentLayer = layer;
        currentNodeIndex = index;

        UpdateAccessibleNodes(layer, index);

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
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameState.Shop);
                }
                break;

            case NodeType.Event:
                Debug.Log("[MapManager] イベント発生！ HPを5回復 & 10G獲得");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.playerHP = Mathf.Min(
                        GameManager.Instance.playerMaxHP,
                        GameManager.Instance.playerHP + 5
                    );
                    GameManager.Instance.playerGold += 10;
                }
                UpdateMapUI();
                UpdateGoldDisplay();
                break;
        }
    }

    private void UpdateAccessibleNodes(int currentLayerIdx, int currentNodeIdx)
    {
        foreach (var layer in mapData)
        {
            foreach (var n in layer)
            {
                if (!n.isVisited) n.isAccessible = false;
            }
        }

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
