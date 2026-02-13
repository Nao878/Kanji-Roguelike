using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 個々のカード表示用UIコンポーネント
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("UI要素")]
    public TextMeshProUGUI kanjiText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI effectText;
    public TextMeshProUGUI descriptionText;
    public Image cardBackground;
    public Button cardButton;

    [Header("データ")]
    public KanjiCardData cardData;

    // カード選択時のコールバック
    public System.Action<CardUI> onCardClicked;

    private bool isSelected = false;

    /// <summary>
    /// カードデータを設定してUIを更新
    /// </summary>
    public void Setup(KanjiCardData data, System.Action<CardUI> clickCallback = null)
    {
        cardData = data;
        onCardClicked = clickCallback;

        if (data == null) return;

        if (kanjiText != null) kanjiText.text = data.kanji;
        if (costText != null) costText.text = data.cost.ToString();
        if (effectText != null) effectText.text = data.effectValue.ToString();
        if (descriptionText != null) descriptionText.text = data.description;

        // 効果タイプに応じた背景色
        if (cardBackground != null)
        {
            cardBackground.color = GetEffectColor(data.effectType);
        }

        // ボタンクリック設定
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        onCardClicked?.Invoke(this);
    }

    /// <summary>
    /// 選択状態を設定
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (cardBackground != null)
        {
            var c = cardBackground.color;
            cardBackground.color = new Color(c.r, c.g, c.b, selected ? 1f : 0.8f);
        }

        // 選択時に少し上に移動
        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            var pos = rect.anchoredPosition;
            pos.y = selected ? 20f : 0f;
            rect.anchoredPosition = pos;
        }
    }

    /// <summary>
    /// 効果タイプに応じた色
    /// </summary>
    private Color GetEffectColor(CardEffectType type)
    {
        switch (type)
        {
            case CardEffectType.Attack: return new Color(0.85f, 0.25f, 0.25f, 0.8f);
            case CardEffectType.Defense: return new Color(0.25f, 0.5f, 0.85f, 0.8f);
            case CardEffectType.Heal: return new Color(0.25f, 0.8f, 0.35f, 0.8f);
            case CardEffectType.Buff: return new Color(0.85f, 0.7f, 0.2f, 0.8f);
            case CardEffectType.Special: return new Color(0.7f, 0.3f, 0.85f, 0.8f);
            default: return new Color(0.5f, 0.5f, 0.5f, 0.8f);
        }
    }
}
