using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 戦闘画面UI管理（CardControllerベースのドラッグ＆ドロップ対応）
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("プレイヤー情報")]
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerManaText;

    [Header("敵情報")]
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyHPText;
    public TextMeshProUGUI enemyKanjiText;
    public GameObject enemyArea;

    [Header("手札エリア")]
    public Transform handArea;

    [Header("操作ボタン")]
    public Button endTurnButton;
    public Button fusionButton;

    [Header("バトルログ")]
    public TextMeshProUGUI battleLogText;

    [Header("フォント")]
    public TMP_FontAsset appFont;

    private List<CardController> handCards = new List<CardController>();

    private void Start()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        if (fusionButton != null)
        {
            fusionButton.onClick.AddListener(OnFusionClicked);
        }
    }

    /// <summary>
    /// 手札UIを更新（CardControllerベース）
    /// </summary>
    public void UpdateHandUI()
    {
        // 既存のカードUIをクリア
        foreach (var card in handCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        handCards.Clear();

        var gm = GameManager.Instance;
        if (gm == null) return;

        // 手札のカードを表示
        for (int i = 0; i < gm.hand.Count; i++)
        {
            var card = gm.hand[i];
            var cardCtrl = CreateDraggableCard(card);
            if (cardCtrl != null) handCards.Add(cardCtrl);
        }
    }

    /// <summary>
    /// ドラッグ可能なカードUIを生成
    /// </summary>
    private CardController CreateDraggableCard(KanjiCardData data)
    {
        if (handArea == null || data == null) return null;

        // カードオブジェクト作成
        var go = new GameObject($"Card_{data.kanji}");
        go.transform.SetParent(handArea, false);
        go.tag = "Card";

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100f, 140f);

        // CanvasGroupを追加（ドラッグ時のレイキャスト制御用）
        var canvasGroup = go.AddComponent<CanvasGroup>();

        // 背景
        var bg = go.AddComponent<Image>();

        // CardControllerコンポーネント
        var cardCtrl = go.AddComponent<CardController>();
        cardCtrl.cardBackground = bg;
        cardCtrl.canvasGroup = canvasGroup;
        cardCtrl.appFont = appFont;

        // 漢字テキスト
        var kanjiGo = new GameObject("KanjiText");
        kanjiGo.transform.SetParent(go.transform, false);
        var kanjiText = kanjiGo.AddComponent<TextMeshProUGUI>();
        kanjiText.fontSize = 42;
        kanjiText.alignment = TextAlignmentOptions.Center;
        kanjiText.color = Color.white;
        kanjiText.raycastTarget = false;
        if (appFont != null) kanjiText.font = appFont;
        var kanjiRect = kanjiGo.GetComponent<RectTransform>();
        kanjiRect.anchorMin = new Vector2(0, 0.4f);
        kanjiRect.anchorMax = new Vector2(1, 0.9f);
        kanjiRect.offsetMin = Vector2.zero;
        kanjiRect.offsetMax = Vector2.zero;
        cardCtrl.kanjiText = kanjiText;

        // コストテキスト
        var costGo = new GameObject("CostText");
        costGo.transform.SetParent(go.transform, false);
        var costText = costGo.AddComponent<TextMeshProUGUI>();
        costText.fontSize = 18;
        costText.alignment = TextAlignmentOptions.TopLeft;
        costText.color = new Color(0.4f, 0.7f, 1f);
        costText.raycastTarget = false;
        if (appFont != null) costText.font = appFont;
        var costRect = costGo.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0, 0.85f);
        costRect.anchorMax = new Vector2(0.3f, 1f);
        costRect.offsetMin = new Vector2(5f, 0);
        costRect.offsetMax = new Vector2(0, -2f);
        cardCtrl.costText = costText;

        // 効果テキスト
        var descGo = new GameObject("DescText");
        descGo.transform.SetParent(go.transform, false);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 14;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = new Color(0.9f, 0.9f, 0.9f);
        descText.raycastTarget = false;
        if (appFont != null) descText.font = appFont;
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0.35f);
        descRect.offsetMin = new Vector2(3f, 3f);
        descRect.offsetMax = new Vector2(-3f, 0);
        cardCtrl.descriptionText = descText;

        // 合成プレビュー（頭上に半透明テキスト）
        var previewBgGo = new GameObject("FusionPreview");
        previewBgGo.transform.SetParent(go.transform, false);
        var previewBgRect = previewBgGo.AddComponent<RectTransform>();
        previewBgRect.anchorMin = new Vector2(0.15f, 0.92f);
        previewBgRect.anchorMax = new Vector2(0.85f, 1.35f);
        previewBgRect.offsetMin = Vector2.zero;
        previewBgRect.offsetMax = Vector2.zero;
        var previewBg = previewBgGo.AddComponent<Image>();
        previewBg.color = new Color(1f, 0.9f, 0.2f, 0.4f);
        previewBg.raycastTarget = false;

        var previewTextGo = new GameObject("PreviewText");
        previewTextGo.transform.SetParent(previewBgGo.transform, false);
        var previewText = previewTextGo.AddComponent<TextMeshProUGUI>();
        previewText.fontSize = 28;
        previewText.alignment = TextAlignmentOptions.Center;
        previewText.color = new Color(1f, 0.95f, 0.5f, 0.85f);
        previewText.raycastTarget = false;
        if (appFont != null) previewText.font = appFont;
        var previewTextRect = previewTextGo.GetComponent<RectTransform>();
        previewTextRect.anchorMin = Vector2.zero;
        previewTextRect.anchorMax = Vector2.one;
        previewTextRect.offsetMin = Vector2.zero;
        previewTextRect.offsetMax = Vector2.zero;

        cardCtrl.fusionPreviewObj = previewBgGo;
        cardCtrl.fusionPreviewText = previewText;

        // コールバック設定
        cardCtrl.onCardUsed = () => UpdateStatusUI();
        cardCtrl.onHandChanged = () =>
        {
            // 少し遅延してUI更新（Destroyの完了を待つ）
            StartCoroutine(DelayedUpdateHand());
        };

        // セットアップ
        cardCtrl.Setup(data);

        return cardCtrl;
    }

    private System.Collections.IEnumerator DelayedUpdateHand()
    {
        yield return null; // 1フレーム待つ
        UpdateHandUI();
        UpdateStatusUI();
    }

    /// <summary>
    /// ターン終了ボタン
    /// </summary>
    private void OnEndTurnClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.battleManager == null) return;

        gm.battleManager.EndPlayerTurn();
        UpdateHandUI();
        UpdateStatusUI();
    }

    /// <summary>
    /// 合体ボタン
    /// </summary>
    private void OnFusionClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Fusion);
        }
    }

    /// <summary>
    /// ステータスUIを更新
    /// </summary>
    public void UpdateStatusUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (playerHPText != null) playerHPText.text = $"HP: {gm.playerHP}/{gm.playerMaxHP}";
        if (playerManaText != null) playerManaText.text = $"マナ: {gm.playerMana}/{gm.playerMaxMana}";

        if (gm.battleManager != null && gm.battleManager.currentEnemyData != null)
        {
            var enemy = gm.battleManager.currentEnemyData;
            if (enemyNameText != null) enemyNameText.text = enemy.enemyName;
            if (enemyHPText != null) enemyHPText.text = $"HP: {gm.battleManager.enemyCurrentHP}/{enemy.maxHP}";
            if (enemyKanjiText != null) enemyKanjiText.text = enemy.displayKanji;
        }
    }
}
