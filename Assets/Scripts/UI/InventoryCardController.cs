using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// インベントリ画面専用のドラッグ＆ドロップコントローラー
/// 2枚のカードの合成（クラフト）を実装
/// </summary>
public class InventoryCardController : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public KanjiCardData cardData;

    [Header("UI要素（外部割当）")]
    public Image bgImage;
    public TextMeshProUGUI kanjiText;
    public GameObject fusionPreviewObj;
    public TextMeshProUGUI fusionPreviewText;

    // ドラッグ情報
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalPosition;
    private Canvas rootCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // 状態
    private bool isHighlighted = false;
    private Color originalColor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup()
    {
        if (bgImage != null) originalColor = bgImage.color;
    }

    // ============================================
    // ドラッグ＆ドロップ
    // ============================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = rectTransform.anchoredPosition;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        // 最前面に移動して他の要素より前に描画
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            rootCanvas.worldCamera,
            out localPoint
        );
        rectTransform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        bool handled = false;

        foreach (var result in results)
        {
            if (result.gameObject == gameObject) continue;

            // 他のInventoryCardにドロップしたか
            var targetCard = result.gameObject.GetComponent<InventoryCardController>();
            if (targetCard == null) targetCard = result.gameObject.GetComponentInParent<InventoryCardController>();

            if (targetCard != null && targetCard != this)
            {
                handled = TryFuse(targetCard);
                break;
            }
        }

        if (!handled)
        {
            ReturnToSlot();
        }
    }

    private void ReturnToSlot()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    // ============================================
    // 合体処理
    // ============================================

    private bool TryFuse(InventoryCardController targetCard)
    {
        var gm = GameManager.Instance;
        if (gm == null || cardData == null || targetCard.cardData == null) return false;

        // 合成レシピを検索
        int resultId = gm.FindFusionResult(cardData.cardId, targetCard.cardData.cardId);

        if (resultId >= 0)
        {
            var resultCard = gm.GetCardById(resultId);
            if (resultCard != null)
            {
                Debug.Log($"[Inventory] クラフト成功: {cardData.kanji} + {targetCard.cardData.kanji} = {resultCard.kanji}");
                
                // データ更新（2枚消費、1枚獲得）
                gm.inventory.Remove(cardData);
                gm.inventory.Remove(targetCard.cardData);
                gm.AddToInventory(resultCard);

                // インベントリUI全体を再描画
                if (InventoryUIManager.Instance != null)
                {
                    InventoryUIManager.Instance.RefreshUI();
                }
                return true;
            }
        }

        Debug.Log($"[Inventory] 合成失敗: レシピが存在しません");
        return false;
    }

    // ============================================
    // プレビュー表示制御
    // ============================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 他のカードがドラッグされてきた場合の判定
        if (eventData.dragging && eventData.pointerDrag != null)
        {
            var draggedCard = eventData.pointerDrag.GetComponent<InventoryCardController>();
            if (draggedCard != null && draggedCard != this && draggedCard.cardData != null && cardData != null)
            {
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    int resultId = gm.FindFusionResult(draggedCard.cardData.cardId, cardData.cardId);
                    if (resultId >= 0)
                    {
                        var resultCard = gm.GetCardById(resultId);
                        if (resultCard != null)
                        {
                            ShowPreview(resultCard.kanji);
                            return;
                        }
                    }
                }
            }
        }

        // 通常のホバー（ちょっと拡大）
        if (!isHighlighted)
        {
            rectTransform.localScale = new Vector3(1.1f, 1.1f, 1f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HidePreview();
        
        if (!isHighlighted)
        {
            rectTransform.localScale = Vector3.one;
        }
    }

    private void ShowPreview(string resultKanji)
    {
        isHighlighted = true;
        if (bgImage != null) bgImage.color = new Color(0.8f, 0.7f, 0.2f, 1f);
        if (kanjiText != null) kanjiText.enabled = false;
        
        if (fusionPreviewObj != null)
        {
            fusionPreviewObj.SetActive(true);
            if (fusionPreviewText != null) fusionPreviewText.text = resultKanji;
        }
    }

    private void HidePreview()
    {
        isHighlighted = false;
        if (bgImage != null) bgImage.color = originalColor;
        if (kanjiText != null) kanjiText.enabled = true;
        
        if (fusionPreviewObj != null)
        {
            fusionPreviewObj.SetActive(false);
        }
    }
}
