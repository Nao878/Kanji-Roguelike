using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ショップ画面UI - ランダムに選出されたカードをゴールドで購入
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("UI参照")]
    public Transform cardListArea;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public Button backButton;

    [Header("ショップ設定")]
    public int shopCardCount = 4;
    public int baseCardPrice = 15;

    [Header("フォント")]
    public TMP_FontAsset appFont;

    private List<GameObject> shopCards = new List<GameObject>();

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnEnable()
    {
        GenerateShopItems();
        UpdateGoldDisplay();
    }

    /// <summary>
    /// ショップの商品を生成
    /// </summary>
    public void GenerateShopItems()
    {
        // 既存商品をクリア
        foreach (var go in shopCards)
        {
            if (go != null) Destroy(go);
        }
        shopCards.Clear();

        if (cardListArea == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // 全カードからランダムに選出
        var allCards = new List<KanjiCardData>();
        foreach (var kvp in GetAllAvailableCards())
        {
            allCards.Add(kvp);
        }

        if (allCards.Count == 0) return;

        // 重複なしでランダム選出
        var selected = new List<KanjiCardData>();
        var indices = new List<int>();
        for (int i = 0; i < allCards.Count; i++) indices.Add(i);

        for (int i = 0; i < Mathf.Min(shopCardCount, allCards.Count); i++)
        {
            int randIdx = Random.Range(0, indices.Count);
            selected.Add(allCards[indices[randIdx]]);
            indices.RemoveAt(randIdx);
        }

        // 商品カードを生成
        foreach (var card in selected)
        {
            CreateShopCard(card);
        }

        if (statusText != null) statusText.text = "カードを選んで購入しよう";
    }

    private List<KanjiCardData> GetAllAvailableCards()
    {
        var result = new List<KanjiCardData>();
        var loaded = Resources.LoadAll<KanjiCardData>("");
        foreach (var card in loaded)
        {
            result.Add(card);
        }
        return result;
    }

    /// <summary>
    /// ショップカードUIを作成
    /// </summary>
    private void CreateShopCard(KanjiCardData data)
    {
        if (data == null) return;

        int price = CalculatePrice(data);

        var go = new GameObject($"ShopCard_{data.kanji}");
        go.transform.SetParent(cardListArea, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(110f, 160f);

        // 背景
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.2f, 0.15f, 0.95f);

        var button = go.AddComponent<Button>();

        // 漢字
        var kanjiGo = new GameObject("Kanji");
        kanjiGo.transform.SetParent(go.transform, false);
        var kanjiText = kanjiGo.AddComponent<TextMeshProUGUI>();
        kanjiText.text = data.kanji;
        kanjiText.fontSize = 42;
        kanjiText.alignment = TextAlignmentOptions.Center;
        kanjiText.color = Color.white;
        kanjiText.raycastTarget = false;
        if (appFont != null) kanjiText.font = appFont;
        var kanjiRect = kanjiGo.GetComponent<RectTransform>();
        kanjiRect.anchorMin = new Vector2(0, 0.45f);
        kanjiRect.anchorMax = new Vector2(1, 0.85f);
        kanjiRect.offsetMin = Vector2.zero;
        kanjiRect.offsetMax = Vector2.zero;

        // カード名
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(go.transform, false);
        var nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.text = data.cardName;
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.8f, 0.8f, 0.8f);
        nameText.raycastTarget = false;
        if (appFont != null) nameText.font = appFont;
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.3f);
        nameRect.anchorMax = new Vector2(1, 0.45f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // 効果説明
        var descGo = new GameObject("Desc");
        descGo.transform.SetParent(go.transform, false);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.text = data.description;
        descText.fontSize = 10;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = new Color(0.7f, 0.7f, 0.7f);
        descText.raycastTarget = false;
        if (appFont != null) descText.font = appFont;
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.12f);
        descRect.anchorMax = new Vector2(0.95f, 0.3f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;

        // 価格
        var priceGo = new GameObject("Price");
        priceGo.transform.SetParent(go.transform, false);
        var priceText = priceGo.AddComponent<TextMeshProUGUI>();
        priceText.text = $"{price}G";
        priceText.fontSize = 18;
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.color = new Color(1f, 0.85f, 0.2f);
        priceText.raycastTarget = false;
        if (appFont != null) priceText.font = appFont;
        var priceRect = priceGo.GetComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0, 0);
        priceRect.anchorMax = new Vector2(1, 0.14f);
        priceRect.offsetMin = Vector2.zero;
        priceRect.offsetMax = Vector2.zero;

        // 効果タイプに応じた上部アクセント
        var accentGo = new GameObject("Accent");
        accentGo.transform.SetParent(go.transform, false);
        var accentRect = accentGo.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0, 0.88f);
        accentRect.anchorMax = new Vector2(1, 1f);
        accentRect.offsetMin = Vector2.zero;
        accentRect.offsetMax = Vector2.zero;
        var accentImg = accentGo.AddComponent<Image>();
        accentImg.color = GetEffectColor(data.effectType);
        accentImg.raycastTarget = false;

        // クリック処理
        KanjiCardData capturedData = data;
        int capturedPrice = price;
        button.onClick.AddListener(() => OnBuyCard(capturedData, capturedPrice, go));

        shopCards.Add(go);
    }

    private int CalculatePrice(KanjiCardData data)
    {
        int price = baseCardPrice;
        price += data.effectValue * 3;
        if (data.isFusionResult) price += 10;
        if (data.effectType == CardEffectType.Special) price += 5;
        return price;
    }

    /// <summary>
    /// カード購入
    /// </summary>
    private void OnBuyCard(KanjiCardData card, int price, GameObject cardObj)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.playerGold < price)
        {
            if (statusText != null) statusText.text = $"ゴールドが足りない！（必要: {price}G）";
            Debug.Log($"[ShopUI] ゴールド不足 必要:{price} 所持:{gm.playerGold}");
            return;
        }

        gm.playerGold -= price;
        gm.deck.Add(card);

        Debug.Log($"[ShopUI] 『{card.kanji}』を{price}Gで購入！");

        if (statusText != null)
            statusText.text = $"『{card.kanji}』を購入！ デッキに追加しました";

        // 購入済み処理：ボタンを無効化しテキストを「売切」に
        if (cardObj != null)
        {
            var btn = cardObj.GetComponent<Button>(); // Button is on cardObj itself
            if (btn != null)
            {
                btn.interactable = false;
                // Find the price text specifically
                var priceTextTransform = cardObj.transform.Find("Price");
                if (priceTextTransform != null)
                {
                    var txt = priceTextTransform.GetComponent<TextMeshProUGUI>();
                    if (txt != null) txt.text = "売切";
                }
            }
        }

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
