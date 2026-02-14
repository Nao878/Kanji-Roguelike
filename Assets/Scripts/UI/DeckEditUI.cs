using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 道場 - 山札編集画面
/// デッキからカードを1枚追放（削除）できる
/// 「精神統一」の演出テキスト付き
/// </summary>
public class DeckEditUI : MonoBehaviour
{
    [Header("UI参照")]
    public Transform cardListArea;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI deckCountText;
    public Button backButton;

    [Header("確認ダイアログ")]
    public GameObject confirmPanel;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("フォント")]
    public TMP_FontAsset appFont;

    private List<GameObject> cardUIs = new List<GameObject>();
    private KanjiCardData selectedCard;
    private bool hasRemovedCard = false;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmRemove);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnCancelRemove);
    }

    private void OnEnable()
    {
        hasRemovedCard = false;
        selectedCard = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);

        // 精神統一演出
        if (statusText != null)
            statusText.text = "── 精神統一 ──\n心を静め、山札を見極めよ…";

        RefreshCardList();
    }

    /// <summary>
    /// デッキ全体のカード一覧を表示
    /// </summary>
    public void RefreshCardList()
    {
        foreach (var go in cardUIs)
        {
            if (go != null) Destroy(go);
        }
        cardUIs.Clear();

        var gm = GameManager.Instance;
        if (gm == null) return;

        // 全所持カードを表示（デッキ＋手札＋捨て札）
        var allCards = new List<KanjiCardData>();
        allCards.AddRange(gm.deck);
        allCards.AddRange(gm.hand);
        allCards.AddRange(gm.discardPile);

        foreach (var card in allCards)
        {
            CreateCardUI(card);
        }

        // デッキ枚数表示
        if (deckCountText != null)
            deckCountText.text = $"山札: {allCards.Count}枚";

        if (titleText != null)
            titleText.text = "⛩ 道場 ⛩";
    }

    private void CreateCardUI(KanjiCardData data)
    {
        if (cardListArea == null || data == null) return;

        var go = new GameObject($"DojoCard_{data.kanji}");
        go.transform.SetParent(cardListArea, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100f, 140f);

        // 背景
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.15f, 0.12f, 0.95f);

        var button = go.AddComponent<Button>();

        // 効果タイプ色アクセント（上部）
        var accentGo = new GameObject("Accent");
        accentGo.transform.SetParent(go.transform, false);
        var accentRect = accentGo.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0, 0.9f);
        accentRect.anchorMax = new Vector2(1, 1f);
        accentRect.offsetMin = Vector2.zero;
        accentRect.offsetMax = Vector2.zero;
        var accentImg = accentGo.AddComponent<Image>();
        accentImg.color = GetEffectColor(data.effectType);
        accentImg.raycastTarget = false;

        // 漢字
        var kanjiGo = new GameObject("Kanji");
        kanjiGo.transform.SetParent(go.transform, false);
        var kanjiText = kanjiGo.AddComponent<TextMeshProUGUI>();
        kanjiText.text = data.kanji;
        kanjiText.fontSize = 38;
        kanjiText.alignment = TextAlignmentOptions.Center;
        kanjiText.color = Color.white;
        kanjiText.raycastTarget = false;
        if (appFont != null) kanjiText.font = appFont;
        var kanjiRect = kanjiGo.GetComponent<RectTransform>();
        kanjiRect.anchorMin = new Vector2(0, 0.4f);
        kanjiRect.anchorMax = new Vector2(1, 0.88f);
        kanjiRect.offsetMin = Vector2.zero;
        kanjiRect.offsetMax = Vector2.zero;

        // カード名
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(go.transform, false);
        var nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.text = data.cardName;
        nameText.fontSize = 12;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.8f, 0.8f, 0.7f);
        nameText.raycastTarget = false;
        if (appFont != null) nameText.font = appFont;
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.25f);
        nameRect.anchorMax = new Vector2(1, 0.4f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // 説明
        var descGo = new GameObject("Desc");
        descGo.transform.SetParent(go.transform, false);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.text = data.description;
        descText.fontSize = 9;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = new Color(0.6f, 0.6f, 0.6f);
        descText.raycastTarget = false;
        if (appFont != null) descText.font = appFont;
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.02f);
        descRect.anchorMax = new Vector2(0.95f, 0.25f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;

        // クリック処理
        KanjiCardData capturedData = data;
        button.onClick.AddListener(() => OnCardClicked(capturedData));

        // 追放済みならクリック不可
        if (hasRemovedCard)
        {
            button.interactable = false;
        }

        cardUIs.Add(go);
    }

    /// <summary>
    /// カード選択 → 確認ダイアログ
    /// </summary>
    private void OnCardClicked(KanjiCardData card)
    {
        if (hasRemovedCard) return;

        selectedCard = card;

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }
        if (confirmText != null)
        {
            confirmText.text = $"『{card.kanji}』({card.cardName})\nを山札から追放しますか？\n\n{card.description}";
        }
    }

    /// <summary>
    /// 追放を確定
    /// </summary>
    private void OnConfirmRemove()
    {
        if (selectedCard == null) return;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            // 全リストから削除
            if (!gm.deck.Remove(selectedCard))
            {
                if (!gm.hand.Remove(selectedCard))
                {
                    gm.discardPile.Remove(selectedCard);
                }
            }

            Debug.Log($"[DeckEditUI] 『{selectedCard.kanji}』を追放！");

            if (statusText != null)
                statusText.text = $"── 座禅 ──\n『{selectedCard.kanji}』を山札から追放した…\n心が軽くなった。";
        }

        hasRemovedCard = true;
        selectedCard = null;

        if (confirmPanel != null) confirmPanel.SetActive(false);

        RefreshCardList();
    }

    private void OnCancelRemove()
    {
        selectedCard = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void OnBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Map);
        }
    }

    private Color GetEffectColor(CardEffectType type)
    {
        switch (type)
        {
            case CardEffectType.Attack: return new Color(0.85f, 0.25f, 0.25f, 0.9f);
            case CardEffectType.Defense: return new Color(0.25f, 0.5f, 0.85f, 0.9f);
            case CardEffectType.Heal: return new Color(0.25f, 0.8f, 0.35f, 0.9f);
            case CardEffectType.Buff: return new Color(0.85f, 0.7f, 0.2f, 0.9f);
            case CardEffectType.Special: return new Color(0.7f, 0.3f, 0.85f, 0.9f);
            default: return new Color(0.5f, 0.5f, 0.5f, 0.9f);
        }
    }
}
