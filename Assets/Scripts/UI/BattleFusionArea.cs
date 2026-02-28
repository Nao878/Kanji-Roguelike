using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// バトル中の複数合体用スロット（画面下部）
/// ドラッグ＆ドロップでカードを受け付け、2～3枚の漢字を合体させる
/// </summary>
public class BattleFusionArea : MonoBehaviour
{
    public Transform slotContainer;
    public Button fuseButton;
    public Button clearButton;
    public TMP_FontAsset appFont;

    private List<KanjiCardData> slottedCards = new List<KanjiCardData>();

    private void Start()
    {
        if (fuseButton != null) fuseButton.onClick.AddListener(OnFuseClicked);
        if (clearButton != null) clearButton.onClick.AddListener(OnClearClicked);
        UpdateUI();
    }

    public bool ReceiveCard(CardController cardController)
    {
        if (cardController != null && slottedCards.Count < 3)
        {
            var data = cardController.cardData;

            // スロットに追加
            slottedCards.Add(data);
            
            // 手札から削除
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.hand.Remove(data);
                if (gm.battleManager != null && gm.battleManager.battleUI != null)
                {
                    gm.battleManager.battleUI.UpdateHandUI();
                }
            }

            // 元のカードオブジェクトは破棄（UIは再構築されるため）
            Destroy(cardController.gameObject);

            UpdateUI();
            return true;
        }
        return false;
    }

    private void UpdateUI()
    {
        // 既存のスロットUIをクリア
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        // 現在セットされているカードを描画
        for (int i = 0; i < slottedCards.Count; i++)
        {
            var data = slottedCards[i];
            var cardGo = CreateMiniCard(data);
            
            // クリックで手札に戻す機能
            var btn = cardGo.AddComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() =>
            {
                ReturnCardToHand(index);
            });
        }

        // ボタンの有効無効設定
        if (fuseButton != null)
        {
            fuseButton.interactable = slottedCards.Count >= 2;
        }
        if (clearButton != null)
        {
            clearButton.interactable = slottedCards.Count > 0;
        }
    }

    private GameObject CreateMiniCard(KanjiCardData data)
    {
        var go = new GameObject($"SlotCard_{data.kanji}");
        go.transform.SetParent(slotContainer, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(70f, 100f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

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

    private void ReturnCardToHand(int index)
    {
        if (index < 0 || index >= slottedCards.Count) return;

        var data = slottedCards[index];
        slottedCards.RemoveAt(index);

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.hand.Add(data);
            if (gm.battleManager != null && gm.battleManager.battleUI != null)
            {
                gm.battleManager.battleUI.UpdateHandUI();
            }
        }

        UpdateUI();
    }

    private void OnClearClicked()
    {
        while (slottedCards.Count > 0)
        {
            ReturnCardToHand(0);
        }
    }

    private void OnFuseClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        List<int> resultIds = new List<int>();

        if (slottedCards.Count == 2)
        {
            resultIds = gm.FindFusionResults(slottedCards[0].cardId, slottedCards[1].cardId);
        }
        else if (slottedCards.Count == 3)
        {
            resultIds = gm.FindFusionResults3(slottedCards[0].cardId, slottedCards[1].cardId, slottedCards[2].cardId);
        }

        if (resultIds.Count > 0)
        {
            if (resultIds.Count == 1)
            {
                ApplyFusion(resultIds[0]);
            }
            else
            {
                gm.ShowFusionSelectionUI(resultIds, (selectedId) =>
                {
                    ApplyFusion(selectedId);
                });
            }
        }
        else
        {
            Debug.Log("[BattleFusionArea] この組み合わせでの合体はできません");
            // 少し揺らすなどの失敗エフェクトを入れるとベター
        }
    }

    private void ApplyFusion(int resultId)
    {
        var gm = GameManager.Instance;
        var resultCard = gm.GetCardById(resultId);
        if (resultCard == null) return;

        // 手札に加える
        gm.hand.Add(resultCard);
        if (EncyclopediaManager.Instance != null) EncyclopediaManager.Instance.UnlockCard(resultId);

        // スロットをクリア
        slottedCards.Clear();
        UpdateUI();

        // UIを更新
        if (gm.battleManager != null && gm.battleManager.battleUI != null)
        {
            gm.battleManager.battleUI.UpdateHandUI();
        }

        Debug.Log($"[BattleFusionArea] 複合合体成功: 新たなカード『{resultCard.kanji}』を手に入れました");
    }
}
