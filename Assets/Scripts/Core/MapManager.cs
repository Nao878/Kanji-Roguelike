using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 漢字マップシステム - Slay the Spire風ボトムからトップへ進むルートマップ
/// 背景に地形漢字を散りばめた和風デザイン
/// 選択可能ノードの点滅・大将拡大・川の流れアニメ対応
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
    private List<RectTransform> riverKanjis = new List<RectTransform>();
    private List<CanvasGroup> blinkingNodes = new List<CanvasGroup>();

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
        Boss,
        Dojo
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
        if (rand < 0.35f) return NodeType.Battle;
        if (rand < 0.55f) return NodeType.Event;
        if (rand < 0.70f) return NodeType.Elite;
        if (rand < 0.85f) return NodeType.Shop;
        return NodeType.Dojo;
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
        // 既存の背景漢字を削除
        foreach (var go in backgroundKanjis)
        {
            if (go != null) Destroy(go);
        }
        backgroundKanjis.Clear();
        riverKanjis.Clear();

        Transform parent = backgroundArea != null ? backgroundArea : (mapContent != null ? mapContent : transform);

        string[,] terrainMap = new string[14, 8]
        {
            {"山","峰","山","岩","山","峰","山","岩"},
            {"峰","山","岩","山","峰","山","岩","山"},
            {"森","林","川","森","林","森","川","森"},
            {"林","森","川","林","森","林","川","林"},
            {"森","草","川","森","草","森","川","森"},
            {"草","森","川","草","森","草","川","草"},
            {"草","原","川","草","原","草","川","草"},
            {"原","草","川","原","草","原","川","原"},
            {"草","野","川","草","野","草","川","草"},
            {"野","草","川","野","草","野","川","野"},
            {"丘","原","川","丘","原","丘","川","丘"},
            {"原","丘","川","原","丘","原","川","原"},
            {"草","原","川","草","原","草","川","草"},
            {"原","野","川","原","野","原","川","原"}
        };

        for (int row = 0; row < 14; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                string kanji = terrainMap[row, col];
                var go = new GameObject($"BgKanji_{row}_{col}");
                go.transform.SetParent(parent, false);

                var rect = go.AddComponent<RectTransform>();
                float xNorm = (col + 0.5f) / 8f;
                float yNorm = (row + 0.5f) / 14f;
                rect.anchorMin = new Vector2(xNorm - 0.06f, yNorm - 0.035f);
                rect.anchorMax = new Vector2(xNorm + 0.06f, yNorm + 0.035f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var text = go.AddComponent<TextMeshProUGUI>();
                text.text = kanji;
                text.fontSize = 16;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                if (appFont != null) text.font = appFont;

                // 地形に応じた色
                if (kanji == "山" || kanji == "峰" || kanji == "岩")
                    text.color = new Color(0.35f, 0.3f, 0.25f, 0.15f);
                else if (kanji == "川")
                    text.color = new Color(0.2f, 0.4f, 0.7f, 0.2f);
                else if (kanji == "森" || kanji == "林")
                    text.color = new Color(0.2f, 0.4f, 0.2f, 0.15f);
                else
                    text.color = new Color(0.3f, 0.35f, 0.25f, 0.12f);

                backgroundKanjis.Add(go);

                // 川の漢字をアニメーション対象に登録
                if (kanji == "川")
                {
                    riverKanjis.Add(rect);
                }
            }
        }
    }

    /// <summary>
    /// 川のスクロールアニメーション + 点滅ノードアニメーション
    /// </summary>
    private void Update()
    {
        // 川の流れ（上→下へゆっくりスクロール）
        float riverSpeed = 0.003f;
        foreach (var riverRect in riverKanjis)
        {
            if (riverRect == null) continue;
            float offsetY = Mathf.Sin(Time.time * 0.8f + riverRect.anchorMin.x * 10f) * riverSpeed;
            riverRect.anchorMin = new Vector2(riverRect.anchorMin.x, riverRect.anchorMin.y + offsetY);
            riverRect.anchorMax = new Vector2(riverRect.anchorMax.x, riverRect.anchorMax.y + offsetY);
        }

        // 選択可能ノードの点滅
        float alpha = 0.7f + Mathf.PingPong(Time.time * 0.8f, 0.3f);
        foreach (var cg in blinkingNodes)
        {
            if (cg != null) cg.alpha = alpha;
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
        blinkingNodes.Clear();

        // 接続線を先に削除
        foreach (Transform child in mapContent)
        {
            if (child.name.StartsWith("Line_"))
            {
                Destroy(child.gameObject);
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
    /// 大将は1.5倍サイズ、選択可能ノードは点滅
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

        // 大将ノードは拡大
        bool isBoss = node.nodeType == NodeType.Boss;
        float w = isBoss ? 140f : 110f;
        float h = isBoss ? 65f : 50f;
        rect.sizeDelta = new Vector2(w, h);

        // CanvasGroupで点滅制御
        var canvasGroup = go.AddComponent<CanvasGroup>();

        // 外枠（Border）
        var borderImage = go.AddComponent<Image>();
        Color nodeColor = GetNodeColor(node.nodeType);
        Color borderColor = new Color(
            Mathf.Min(1f, nodeColor.r + 0.2f),
            Mathf.Min(1f, nodeColor.g + 0.2f),
            Mathf.Min(1f, nodeColor.b + 0.2f), 1f);

        if (node.isVisited)
        {
            borderImage.color = new Color(0.25f, 0.25f, 0.25f, 0.6f);
            canvasGroup.alpha = 0.4f;
        }
        else if (node.isAccessible)
        {
            borderImage.color = borderColor;
            canvasGroup.alpha = 1f;
            // 点滅リストに追加
            blinkingNodes.Add(canvasGroup);
        }
        else
        {
            borderImage.color = new Color(nodeColor.r * 0.3f, nodeColor.g * 0.3f, nodeColor.b * 0.3f, 0.5f);
            canvasGroup.alpha = 0.3f;
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
        text.fontSize = isBoss ? 30 : 22;
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

        for (int layer = 0; layer < mapData.Count - 1; layer++)
        {
            for (int n = 0; n < mapData[layer].Count; n++)
            {
                var node = mapData[layer][n];
                if (node.uiObject == null) continue;

                var fromRect = node.uiObject.GetComponent<RectTransform>();
                float fromHalfH = fromRect.sizeDelta.y / 2f;
                Vector2 fromPos = fromRect.anchoredPosition + new Vector2(0, fromHalfH);

                foreach (int connIdx in node.connections)
                {
                    if (connIdx >= mapData[layer + 1].Count) continue;
                    var targetNode = mapData[layer + 1][connIdx];
                    if (targetNode.uiObject == null) continue;

                    var toRect = targetNode.uiObject.GetComponent<RectTransform>();
                    float toHalfH = toRect.sizeDelta.y / 2f;
                    Vector2 toPos = toRect.anchoredPosition - new Vector2(0, toHalfH);

                    bool isActive = node.isVisited || targetNode.isAccessible;
                    CreateLine(fromPos, toPos, isActive);
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
        lineRect.sizeDelta = new Vector2(distance, active ? 3f : 2f);
        lineRect.rotation = Quaternion.Euler(0, 0, angle);

        var img = lineGo.AddComponent<Image>();
        img.color = active ?
            new Color(0.85f, 0.75f, 0.3f, 0.7f) :
            new Color(0.3f, 0.3f, 0.3f, 0.2f);
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
            case NodeType.Dojo:   return "道場";
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
            case NodeType.Dojo:   return new Color(0.8f, 0.5f, 0.2f);
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

            case NodeType.Dojo:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameState.Dojo);
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
