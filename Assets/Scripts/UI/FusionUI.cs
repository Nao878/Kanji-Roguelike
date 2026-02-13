using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 漢字合体画面UI
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
    }

    /// <summary>
    /// 手札のカード一覧を更新
    /// </summary>
    public void RefreshCardList()
    {
        // 既存UIクリア
        foreach (var ui in cardListUIs)
        {
            if (ui != null) Destroy(ui.gameObject);
        }
        cardListUIs.Clear();

        var gm = GameManager.Instance;
        if (gm == null) return;

        // デッキ全体のカードを表示（合体用）
        var allCards = new List<KanjiCardData>();
        allCards.AddRange(gm.deck);
        allCards.AddRange(gm.hand);

        foreach (var card in allCards)
        {
            CreateCardButton(card);
        }

        UpdateStatus();
    }

    /// <summary>
    /// カードボタンを作成
    /// </summary>
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

        // 漢字テキスト
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

    /// <summary>
    /// カードが選択された時
    /// </summary>
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

            // 合成可能かチェック
            CheckFusionPossible();
        }

        UpdateStatus();
    }

    /// <summary>
    /// スロットのUI更新
    /// </summary>
    private void UpdateSlot(Image slotImage, TextMeshProUGUI slotText, KanjiCardData card)
    {
        if (slotImage != null) slotImage.color = new Color(0.3f, 0.5f, 0.7f, 0.9f);
        if (slotText != null) slotText.text = card != null ? card.kanji : "?";
    }

    /// <summary>
    /// 合成可能かチェックしてプレビュー
    /// </summary>
    private void CheckFusionPossible()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.fusionEngine == null) return;

        if (selectedCard1 != null && selectedCard2 != null)
        {
            bool canFuse = gm.fusionEngine.CanFuse(selectedCard1, selectedCard2);
            if (fuseButton != null) fuseButton.interactable = canFuse;

            if (canFuse)
            {
                var result = gm.fusionEngine.TryFuse(selectedCard1, selectedCard2);
                if (result != null && resultText != null)
                {
                    resultText.text = result.kanji;
                    if (resultDescText != null) resultDescText.text = result.description;
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
    /// 合成実行ボタン
    /// </summary>
    private void OnFuseClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.fusionEngine == null) return;

        if (selectedCard1 == null || selectedCard2 == null) return;

        var result = gm.fusionEngine.TryFuse(selectedCard1, selectedCard2);
        if (result != null)
        {
            // デッキから素材カードを除去し、結果カードを追加
            gm.deck.Remove(selectedCard1);
            gm.hand.Remove(selectedCard1);
            gm.deck.Remove(selectedCard2);
            gm.hand.Remove(selectedCard2);
            gm.deck.Add(result);

            Debug.Log($"[FusionUI] 合成完了！ 『{selectedCard1.kanji}』+『{selectedCard2.kanji}』=『{result.kanji}』");

            if (statusText != null)
            {
                statusText.text = $"合成成功！ 『{result.kanji}』を獲得！";
            }

            ClearSlots();
            RefreshCardList();
        }
    }

    /// <summary>
    /// スロットクリア
    /// </summary>
    private void OnClearClicked()
    {
        ClearSlots();
    }

    /// <summary>
    /// スロットをクリア
    /// </summary>
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

    /// <summary>
    /// ステータステキストを更新
    /// </summary>
    private void UpdateStatus()
    {
        if (statusText == null) return;

        if (selectedCard1 == null)
            statusText.text = "1枚目のカードを選択してください";
        else if (selectedCard2 == null)
            statusText.text = $"『{selectedCard1.kanji}』選択中 — 2枚目を選択してください";
    }

    /// <summary>
    /// 戻るボタン
    /// </summary>
    private void OnBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Map);
        }
    }
}
