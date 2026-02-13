using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 合体所（道場）- デッキ構築型
/// デッキから2枚選択して消費し、進化カードを1枚獲得
/// ゴールド（合体コスト）が必要
/// </summary>
public class FusionUI : MonoBehaviour
{
    [Header("スロット")]
    public Image slot1Image;
    public TextMeshProUGUI slot1Text;
    public Image slot2Image;
    public TextMeshProUGUI slot2Text;

    [Header("結果")]
    public Image resultImage;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI resultDescText;

    [Header("ボタン")]
    public Button fuseButton;
    public Button backButton;
    public Button clearButton;

    [Header("カード一覧")]
    public Transform cardListArea;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI costText;

    [Header("フォント")]
    public TMP_FontAsset appFont;

    private KanjiCardData selectedCard1;
    private KanjiCardData selectedCard2;
    private List<CardUI> cardListUIs = new List<CardUI>();

    private void Start()
    {
        if (fuseButton != null) fuseButton.onClick.AddListener(OnFuseClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        if (clearButton != null) clearButton.onClick.AddListener(OnClearClicked);
    }

    private void OnEnable()
    {
        RefreshCardList();
        ClearSlots();
        UpdateGoldDisplay();
    }

    /// <summary>
    /// デッキ全体のカード一覧を表示
    /// </summary>
    public void RefreshCardList()
    {
        foreach (var ui in cardListUIs)
        {
            if (ui != null) Destroy(ui.gameObject);
        }
        cardListUIs.Clear();

        var gm = GameManager.Instance;
        if (gm == null) return;

        // デッキ＋手札＋捨て札の全所持カードを表示
        var allCards = new List<KanjiCardData>();
        allCards.AddRange(gm.deck);
        allCards.AddRange(gm.hand);
        allCards.AddRange(gm.discardPile);

        foreach (var card in allCards)
        {
            CreateCardButton(card);
        }

        UpdateStatus();
    }

    private void CreateCardButton(KanjiCardData data)
    {
        if (cardListArea == null || data == null) return;

        var go = new GameObject($"FusionCard_{data.kanji}");
        go.transform.SetParent(cardListArea, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(90f, 110f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

        var button = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = $"{data.kanji}\n<size=14>{data.cardName}</size>";
        text.fontSize = 32;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        if (appFont != null) text.font = appFont;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var cardUI = go.AddComponent<CardUI>();
        cardUI.cardData = data;
        cardUI.cardBackground = bg;
        cardUI.cardButton = button;

        KanjiCardData capturedData = data;
        button.onClick.AddListener(() => OnCardSelected(capturedData));

        cardListUIs.Add(cardUI);
    }

    private void OnCardSelected(KanjiCardData card)
    {
        if (selectedCard1 == null)
        {
            selectedCard1 = card;
            UpdateSlot(slot1Image, slot1Text, card);
            Debug.Log($"[FusionUI] スロット1に『{card.kanji}』をセット");
        }
        else if (selectedCard2 == null)
        {
            selectedCard2 = card;
            UpdateSlot(slot2Image, slot2Text, card);
            Debug.Log($"[FusionUI] スロット2に『{card.kanji}』をセット");
            CheckFusionPossible();
        }

        UpdateStatus();
    }

    private void UpdateSlot(Image slotImage, TextMeshProUGUI slotText, KanjiCardData card)
    {
        if (slotImage != null) slotImage.color = new Color(0.3f, 0.5f, 0.7f, 0.9f);
        if (slotText != null) slotText.text = card != null ? card.kanji : "?";
    }

    private void CheckFusionPossible()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.fusionEngine == null) return;

        if (selectedCard1 != null && selectedCard2 != null)
        {
            bool canFuse = gm.fusionEngine.CanFuse(selectedCard1, selectedCard2);
            bool canAfford = gm.playerGold >= gm.fusionCost;

            if (fuseButton != null) fuseButton.interactable = canFuse && canAfford;

            if (canFuse)
            {
                var result = gm.fusionEngine.TryFuse(selectedCard1, selectedCard2);
                if (result != null && resultText != null)
                {
                    resultText.text = result.kanji;
                    if (resultDescText != null) resultDescText.text = result.description;
                }

                if (!canAfford)
                {
                    if (statusText != null) statusText.text = $"ゴールド不足！（必要: {gm.fusionCost}G）";
                }
            }
            else
            {
                if (resultText != null) resultText.text = "×";
                if (resultDescText != null) resultDescText.text = "合成できない組み合わせです";
            }
        }
    }

    /// <summary>
    /// 合成実行（ゴールド消費）
    /// </summary>
    private void OnFuseClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.fusionEngine == null) return;
        if (selectedCard1 == null || selectedCard2 == null) return;

        // ゴールドチェック
        if (gm.playerGold < gm.fusionCost)
        {
            if (statusText != null) statusText.text = $"ゴールドが足りない！（必要: {gm.fusionCost}G）";
            return;
        }

        var result = gm.fusionEngine.TryFuse(selectedCard1, selectedCard2);
        if (result != null)
        {
            // ゴールド消費
            gm.playerGold -= gm.fusionCost;

            // デッキから素材カードを除去（全リストから探す）
            RemoveCardFromAllPiles(gm, selectedCard1);
            RemoveCardFromAllPiles(gm, selectedCard2);

            // 結果カードをデッキに追加
            gm.deck.Add(result);

            Debug.Log($"[FusionUI] 合体完了！ 『{selectedCard1.kanji}』+『{selectedCard2.kanji}』=『{result.kanji}』 ({gm.fusionCost}G消費)");

            if (statusText != null)
            {
                statusText.text = $"合体成功！ 『{result.kanji}』を獲得！ (-{gm.fusionCost}G)";
            }

            ClearSlots();
            RefreshCardList();
            UpdateGoldDisplay();
        }
    }

    private void RemoveCardFromAllPiles(GameManager gm, KanjiCardData card)
    {
        if (gm.deck.Remove(card)) return;
        if (gm.hand.Remove(card)) return;
        gm.discardPile.Remove(card);
    }

    private void OnClearClicked()
    {
        ClearSlots();
    }

    private void ClearSlots()
    {
        selectedCard1 = null;
        selectedCard2 = null;
        UpdateSlot(slot1Image, slot1Text, null);
        UpdateSlot(slot2Image, slot2Text, null);
        if (resultText != null) resultText.text = "?";
        if (resultDescText != null) resultDescText.text = "カードを2枚選択してください";
        if (fuseButton != null) fuseButton.interactable = false;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (statusText == null) return;

        var gm = GameManager.Instance;
        int cost = gm != null ? gm.fusionCost : 0;

        if (selectedCard1 == null)
            statusText.text = $"合体コスト: {cost}G — 1枚目のカードを選択";
        else if (selectedCard2 == null)
            statusText.text = $"『{selectedCard1.kanji}』選択中 — 2枚目を選択（{cost}G）";

        if (costText != null)
            costText.text = $"合体コスト: {cost}G";

        UpdateGoldDisplay();
    }

    private void UpdateGoldDisplay()
    {
        if (goldText != null && GameManager.Instance != null)
        {
            goldText.text = $"所持金: {GameManager.Instance.playerGold}G";
        }
    }

    private void OnBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Map);
        }
    }
}
