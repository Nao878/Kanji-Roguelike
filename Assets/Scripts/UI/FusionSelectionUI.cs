using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 複数の合体結果がある場合に、どの結果にするかを選択するUI
/// </summary>
public class FusionSelectionUI : MonoBehaviour
{
    [Header("UI参照")]
    public Transform cardListArea;
    public TMP_FontAsset appFont;

    private Action<int> onSelectedCallback;

    public void ShowSelection(List<int> resultIds, Action<int> onSelected)
    {
        onSelectedCallback = onSelected;
        
        foreach (Transform child in cardListArea)
        {
            Destroy(child.gameObject);
        }

        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (var id in resultIds)
        {
            var card = gm.GetCardById(id);
            if (card != null)
            {
                CreateCardUI(card);
            }
        }

        gameObject.SetActive(true);
    }

    private void CreateCardUI(KanjiCardData data)
    {
        var go = new GameObject($"SelectCard_{data.kanji}");
        go.transform.SetParent(cardListArea, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100f, 140f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f); // 選択画面なので少し落ち着いた色

        var btn = go.AddComponent<Button>();
        int capturedId = data.cardId;
        btn.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            onSelectedCallback?.Invoke(capturedId);
        });

        // 漢字
        var kanjiGo = new GameObject("Kanji");
        kanjiGo.transform.SetParent(go.transform, false);
        var kanjiText = kanjiGo.AddComponent<TextMeshProUGUI>();
        kanjiText.text = data.kanji;
        kanjiText.fontSize = 42;
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
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.8f, 0.8f, 0.8f);
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
        descText.fontSize = 12;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = new Color(0.9f, 0.9f, 0.9f);
        if (appFont != null) descText.font = appFont;
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.1f);
        descRect.anchorMax = new Vector2(0.95f, 0.3f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
    }
}
