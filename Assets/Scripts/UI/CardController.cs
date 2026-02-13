using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ドラッグ＆ドロップ操作を基本としたカードコントローラー
/// IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler を実装
/// </summary>
public class CardController : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("カードデータ")]
    public KanjiCardData cardData;

    [Header("UI要素")]
    public Image cardBackground;
    public TextMeshProUGUI kanjiText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public CanvasGroup canvasGroup;

    [Header("合体プレビュー")]
    public GameObject fusionPreviewObj;
    public TextMeshProUGUI fusionPreviewText;

    [Header("フォント")]
    public TMP_FontAsset appFont;

    // ドラッグ情報
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalPosition;
    private Canvas rootCanvas;
    private RectTransform rectTransform;

    // 合成プレビュー状態
    private bool isHighlighted = false;
    private Color originalColor;

    // コールバック
    public System.Action onCardUsed;
    public System.Action onHandChanged;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// カードデータをセットアップ
    /// </summary>
    public void Setup(KanjiCardData data)
    {
        cardData = data;
        if (data == null) return;

        if (kanjiText != null) kanjiText.text = data.kanji;
        if (costText != null) costText.text = data.cost.ToString();
        if (descriptionText != null) descriptionText.text = data.description;

        // 効果タイプに応じた背景色
        if (cardBackground != null)
        {
            cardBackground.color = GetEffectColor(data.effectType);
            originalColor = cardBackground.color;
        }

        // 合成プレビューは非表示
        if (fusionPreviewObj != null) fusionPreviewObj.SetActive(false);
    }

    // ============================================
    // ドラッグ＆ドロップ
    // ============================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.currentState != GameState.Battle) return;
        if (gm.battleManager == null || gm.battleManager.battleState != BattleManager.BattleState.PlayerTurn) return;

        // 元の親と位置を記憶
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalPosition = rectTransform.anchoredPosition;

        // Canvasを取得
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        // Canvas最前面に移動
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        // レイキャストをブロックしない（ドロップ先を検出するため）
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        Debug.Log($"[CardController] ドラッグ開始: {cardData?.kanji}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null) return;

        // マウス座標に追従
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

        // ドロップ先を判定
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool handled = false;

        foreach (var result in results)
        {
            if (result.gameObject == gameObject) continue;

            // Tag "Enemy" → 攻撃発動
            if (result.gameObject.CompareTag("Enemy"))
            {
                handled = HandleDropOnEnemy();
                break;
            }

            // Tag "Card" → 合成チェック
            if (result.gameObject.CompareTag("Card"))
            {
                var targetCard = result.gameObject.GetComponent<CardController>();
                if (targetCard == null) targetCard = result.gameObject.GetComponentInParent<CardController>();

                if (targetCard != null && targetCard != this)
                {
                    handled = HandleDropOnCard(targetCard);
                    break;
                }
            }
        }

        if (!handled)
        {
            // 元の位置に戻す
            ReturnToHand();
        }

        Debug.Log($"[CardController] ドラッグ終了: {cardData?.kanji} handled={handled}");
    }

    // ============================================
    // ドロップ処理
    // ============================================

    /// <summary>
    /// 敵にドロップ → カード効果発動
    /// </summary>
    private bool HandleDropOnEnemy()
    {
        var gm = GameManager.Instance;
        if (gm == null || cardData == null) return false;

        if (gm.playerMana < cardData.cost)
        {
            Debug.Log($"[CardController] マナ不足！ 必要:{cardData.cost} 現在:{gm.playerMana}");
            ReturnToHand();
            return false;
        }

        // カード効果発動
        gm.battleManager.PlayCard(cardData);

        // UIを更新
        onCardUsed?.Invoke();
        onHandChanged?.Invoke();

        // カードオブジェクトを削除
        Destroy(gameObject);
        return true;
    }

    /// <summary>
    /// 別のカードにドロップ → 合成チェック
    /// </summary>
    private bool HandleDropOnCard(CardController targetCard)
    {
        var gm = GameManager.Instance;
        if (gm == null || cardData == null || targetCard.cardData == null) return false;

        // Dictionaryベースの高速レシピ検索
        int resultId = gm.FindFusionResult(cardData.cardId, targetCard.cardData.cardId);

        if (resultId >= 0)
        {
            // 合成成功！
            var resultCard = gm.GetCardById(resultId);
            if (resultCard != null)
            {
                Debug.Log($"[CardController] 合成成功！ 『{cardData.kanji}』+『{targetCard.cardData.kanji}』=『{resultCard.kanji}』");

                // 手札から素材カードを除去
                gm.hand.Remove(cardData);
                gm.hand.Remove(targetCard.cardData);

                // 結果カードを手札に追加
                gm.hand.Add(resultCard);

                // 合成エフェクト（パーティクル）
                SpawnFusionEffect(targetCard.transform.position);

                // 対象カードのオブジェクトを削除
                Destroy(targetCard.gameObject);

                // 自分を削除
                Destroy(gameObject);

                // UIを更新
                onHandChanged?.Invoke();
                return true;
            }
        }

        // 合成不可 → 元に戻す
        Debug.Log($"[CardController] 合成不可: 『{cardData.kanji}』+『{targetCard.cardData.kanji}』");
        ReturnToHand();
        return false;
    }

    /// <summary>
    /// 合成エフェクトを生成
    /// </summary>
    private void SpawnFusionEffect(Vector3 position)
    {
        // パーティクルシステムを生成
        var effectGo = new GameObject("FusionEffect");
        effectGo.transform.position = position;

        var ps = effectGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.8f;
        main.startLifetime = 0.6f;
        main.startSpeed = 3f;
        main.startSize = 8f;
        main.startColor = new Color(1f, 0.85f, 0.2f, 1f);
        main.maxParticles = 30;
        main.loop = false;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        // レンダラー設定
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        // 自動削除
        Destroy(effectGo, 2f);
    }

    /// <summary>
    /// 元の手札位置に戻す
    /// </summary>
    private void ReturnToHand()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    // ============================================
    // ポインターイベント（合成プレビュー）
    // ============================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ドラッグ中のカードが重なった場合 → 合成プレビュー表示
        if (eventData.dragging && eventData.pointerDrag != null)
        {
            var draggedCard = eventData.pointerDrag.GetComponent<CardController>();
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
                            ShowFusionPreview(resultCard);
                            return;
                        }
                    }
                }
            }
        }

        // 通常のホバー → 少し持ち上げる
        if (!isHighlighted)
        {
            var pos = rectTransform.anchoredPosition;
            pos.y += 10f;
            rectTransform.anchoredPosition = pos;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideFusionPreview();

        // ホバー解除
        if (!isHighlighted)
        {
            var pos = rectTransform.anchoredPosition;
            pos.y -= 10f;
            rectTransform.anchoredPosition = pos;
        }
    }

    /// <summary>
    /// 合成プレビューを表示（黄色発光 ＋ 進化後の漢字表示）
    /// </summary>
    private void ShowFusionPreview(KanjiCardData resultCard)
    {
        isHighlighted = true;

        // カードを黄色く発光
        if (cardBackground != null)
        {
            cardBackground.color = new Color(1f, 0.9f, 0.3f, 1f);
        }

        // 合成プレビューテキストを表示
        if (fusionPreviewObj != null)
        {
            fusionPreviewObj.SetActive(true);
            if (fusionPreviewText != null)
            {
                fusionPreviewText.text = resultCard.kanji;
            }
        }
    }

    /// <summary>
    /// 合成プレビューを非表示
    /// </summary>
    private void HideFusionPreview()
    {
        isHighlighted = false;

        // 背景色を戻す
        if (cardBackground != null)
        {
            cardBackground.color = originalColor;
        }

        if (fusionPreviewObj != null)
        {
            fusionPreviewObj.SetActive(false);
        }
    }

    // ============================================
    // ユーティリティ
    // ============================================

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
