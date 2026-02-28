using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 現在の山札（デッキ）を確認するUI
/// </summary>
public class DeckViewerUI : MonoBehaviour
{
    [Header("UI参照")]
    public Transform cardListArea;
    public Button closeButton;
    public TMP_FontAsset appFont;

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }

    private void OnEnable()
    {
        RefreshCardList();
    }

    /// <summary>
    /// デッキ全体のカード一覧を表示
    /// </summary>
    private void RefreshCardList()
    {
        foreach (Transform child in cardListArea)
        {
            Destroy(child.gameObject);
        }

        var gm = GameManager.Instance;
        if (gm == null || gm.deck == null) return;

        // デッキのカードをコピーして漢字名でソート
        var sortedDeck = new List<KanjiCardData>(gm.deck);
        sortedDeck.Sort((a, b) => string.Compare(a.kanji, b.kanji));

        foreach (var card in sortedDeck)
        {
            CreateCardUI(card);
        }
    }

    private void CreateCardUI(KanjiCardData data)
    {
        if (data == null) return;

        var go = new GameObject($"CardUI_{data.kanji}");
        go.transform.SetParent(cardListArea, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(90f, 130f);

        // 背景
        var bg = go.AddComponent<Image>();
        bg.color = GetEffectColor(data.effectType);

        // 漢字
        var kanjiGo = new GameObject("Kanji");
        kanjiGo.transform.SetParent(go.transform, false);
        var kanjiText = kanjiGo.AddComponent<TextMeshProUGUI>();
        kanjiText.text = data.kanji;
        kanjiText.fontSize = 36;
        kanjiText.alignment = TextAlignmentOptions.Center;
        kanjiText.color = Color.white;
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
        nameText.fontSize = 12;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.9f, 0.9f, 0.9f);
        if (appFont != null) nameText.font = appFont;
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.3f);
        nameRect.anchorMax = new Vector2(1, 0.45f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // 効果数値
        var descGo = new GameObject("Desc");
        descGo.transform.SetParent(go.transform, false);
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.text = $"{data.effectType} {data.effectValue}";
        descText.fontSize = 10;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = new Color(0.9f, 0.9f, 0.9f);
        if (appFont != null) descText.font = appFont;
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.1f);
        descRect.anchorMax = new Vector2(0.95f, 0.3f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
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
