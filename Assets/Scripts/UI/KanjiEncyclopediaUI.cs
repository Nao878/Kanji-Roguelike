using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 漢字図鑑UI - これまでに獲得・合体したカードを一覧表示
/// </summary>
public class KanjiEncyclopediaUI : MonoBehaviour
{
    [Header("UI参照")]
    public Transform cardListArea;
    public Button closeButton;
    public TextMeshProUGUI statusText;
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

    public void RefreshCardList()
    {
        foreach (Transform child in cardListArea)
        {
            Destroy(child.gameObject);
        }

        var allCards = new List<KanjiCardData>(Resources.LoadAll<KanjiCardData>(""));
        allCards.Sort((a, b) => a.cardId.CompareTo(b.cardId));

        int unlockedCount = 0;

        foreach (var card in allCards)
        {
            bool isUnlocked = false;
            // 基礎カードは手札に入った事があれば図鑑に登録、だが初期から持っているものは最初から登録扱いとしておく
            // 正確にはEncyclopediaManagerを通すが、ここではIsUnlockedを確認
            if (EncyclopediaManager.Instance != null && EncyclopediaManager.Instance.IsUnlocked(card.cardId))
            {
                isUnlocked = true;
                unlockedCount++;
            }
            else if (!card.isFusionResult)
            {
                // 初期状態から見える基礎カードは常に表示扱いにしても良い
                // ここではせっかくなのですべて統一して管理することにするが、未登録なら????表示
            }

            CreateEncyclopediaCard(card, isUnlocked);
        }

        if (statusText != null)
        {
            statusText.text = $"漢字収集率: {unlockedCount} / {allCards.Count}";
        }
    }

    private void CreateEncyclopediaCard(KanjiCardData data, bool isUnlocked)
    {
        if (data == null) return;

        var go = new GameObject($"EncycCard_{data.kanji}");
        go.transform.SetParent(cardListArea, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(90f, 130f);

        // 背景
        var bg = go.AddComponent<Image>();
        bg.color = isUnlocked ? GetEffectColor(data.effectType) : new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // 漢字
        var kanjiGo = new GameObject("Kanji");
        kanjiGo.transform.SetParent(go.transform, false);
        var kanjiText = kanjiGo.AddComponent<TextMeshProUGUI>();
        kanjiText.text = isUnlocked ? data.kanji : "？";
        kanjiText.fontSize = isUnlocked ? 36 : 48;
        kanjiText.alignment = TextAlignmentOptions.Center;
        kanjiText.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
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
        nameText.text = isUnlocked ? data.cardName : "???";
        nameText.fontSize = 12;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.9f, 0.9f, 0.9f);
        if (appFont != null) nameText.font = appFont;
        var nameRect = nameGo.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.3f);
        nameRect.anchorMax = new Vector2(1, 0.45f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
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
