using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// デッキ編成UI
/// インベントリ（全所持カード）から20枚を選んでデッキを構築する
/// </summary>
public class DeckManagementUI : MonoBehaviour
{
    [Header("UI参照: インベントリ側")]
    public Transform inventoryScrollContent;
    public TextMeshProUGUI inventoryCountText;

    [Header("UI参照: デッキ側")]
    public Transform deckScrollContent;
    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI deckValidationText;

    [Header("UI参照: その他")]
    public Button saveAndExitButton;
    public Button autoFillButton;
    public TMP_FontAsset appFont;

    private List<GameObject> inventoryItems = new List<GameObject>();
    private List<GameObject> deckItems = new List<GameObject>();

    private void Start()
    {
        if (saveAndExitButton != null) saveAndExitButton.onClick.AddListener(OnSaveAndExit);
        if (autoFillButton != null) autoFillButton.onClick.AddListener(OnAutoFill);
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        var gm = GameManager.Instance;
        var dm = DeckManager.Instance;
        if (gm == null || dm == null) return;

        // リストをクリア
        ClearList(inventoryItems, inventoryScrollContent);
        ClearList(deckItems, deckScrollContent);

        // インベントリの全カードを表示（デッキに入っていないものだけでなく、全枚数を表示）
        // ただし、デッキ構築は「インベントリから選ぶ」ので、枚数管理が必要
        
        // インベントリを表示
        foreach (var card in gm.inventory)
        {
            CreateInventoryItem(card);
        }

        // デッキを表示
        foreach (var card in dm.currentDeck)
        {
            CreateDeckItem(card);
        }

        UpdateStatusTexts();
    }

    private void ClearList(List<GameObject> list, Transform parent)
    {
        foreach (var item in list) if (item != null) Destroy(item);
        list.Clear();
        // 万が一残っている場合
        foreach (Transform child in parent) Destroy(child.gameObject);
    }

    private void UpdateStatusTexts()
    {
        var gm = GameManager.Instance;
        var dm = DeckManager.Instance;
        
        if (inventoryCountText != null) inventoryCountText.text = $"所持品: {gm.inventory.Count}枚";
        if (deckCountText != null) deckCountText.text = $"デッキ: {dm.currentDeck.Count} / {dm.maxDeckSize}枚";

        if (deckValidationText != null)
        {
            if (dm.currentDeck.Count < dm.minDeckSize)
            {
                deckValidationText.text = $"あと <color=red>{dm.minDeckSize - dm.currentDeck.Count}枚</color> 必要です";
                saveAndExitButton.interactable = false;
            }
            else if (dm.currentDeck.Count > dm.maxDeckSize)
            {
                deckValidationText.text = $"枚数が多すぎます！ (<color=red>{dm.currentDeck.Count}</color>)";
                saveAndExitButton.interactable = false;
            }
            else
            {
                deckValidationText.text = "<color=green>デッキ枚数OK!</color>";
                saveAndExitButton.interactable = true;
            }
        }
    }

    private void CreateInventoryItem(KanjiCardData card)
    {
        var go = CreateBaseCardUI(card, inventoryScrollContent);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => AddToDeck(card));
        inventoryItems.Add(go);
    }

    private void CreateDeckItem(KanjiCardData card)
    {
        var go = CreateBaseCardUI(card, deckScrollContent);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => RemoveFromDeck(card));
        deckItems.Add(go);
    }

    private GameObject CreateBaseCardUI(KanjiCardData data, Transform parent)
    {
        var go = new GameObject($"Card_{data.kanji}");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 110);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var kanjiGo = new GameObject("Kanji");
        kanjiGo.transform.SetParent(go.transform, false);
        var kanjiText = kanjiGo.AddComponent<TextMeshProUGUI>();
        kanjiText.text = data.kanji;
        kanjiText.fontSize = 32;
        kanjiText.alignment = TextAlignmentOptions.Center;
        kanjiText.color = Color.white;
        if (appFont != null) kanjiText.font = appFont;
        var kanjiRect = kanjiGo.GetComponent<RectTransform>();
        kanjiRect.anchorMin = Vector2.zero;
        kanjiRect.anchorMax = Vector2.one;
        kanjiRect.offsetMin = Vector2.zero;
        kanjiRect.offsetMax = Vector2.zero;

        return go;
    }

    private void AddToDeck(KanjiCardData card)
    {
        var dm = DeckManager.Instance;
        // インベントリに実際にその枚数があるかチェックが必要（ユニークIDがないので、現在のデッキ内の枚数と比較）
        int countInInventory = GameManager.Instance.inventory.Count(c => c.cardId == card.cardId);
        int countInDeck = dm.currentDeck.Count(c => c.cardId == card.cardId);

        if (countInDeck < countInInventory)
        {
            if (dm.AddCardToDeck(card))
            {
                RefreshUI();
            }
        }
        else
        {
            Debug.Log("[DeckManagementUI] そのカードはこれ以上持っていません");
        }
    }

    private void RemoveFromDeck(KanjiCardData card)
    {
        DeckManager.Instance.RemoveCardFromDeck(card);
        RefreshUI();
    }

    private void OnAutoFill()
    {
        DeckManager.Instance.AutoFillDeck(GameManager.Instance.inventory);
        RefreshUI();
    }

    private void OnSaveAndExit()
    {
        if (DeckManager.Instance.IsDeckValid())
        {
            GameManager.Instance.ChangeState(GameState.Field);
        }
    }
}
