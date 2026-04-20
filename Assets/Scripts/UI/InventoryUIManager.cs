using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// フィールド探索中のインベントリ（リュック）UI管理
/// </summary>
public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    [Header("UI要素")]
    public GameObject inventoryPanel;
    public Transform gridContent;
    public TextMeshProUGUI statusText;
    public TMP_FontAsset appFont;

    [Header("状態")]
    public bool isInventoryOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.currentState != GameState.Field) return;

        // Tabキー（または I キー）で開閉
        bool togglePressed = false;
        
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame))
        {
            togglePressed = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            togglePressed = true;
        }
#endif

        if (togglePressed)
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// インベントリの開閉を切り替える
    /// </summary>
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
            if (isInventoryOpen)
            {
                RefreshUI();
            }
        }
    }

    /// <summary>
    /// 表示を強制的に閉じる
    /// </summary>
    public void CloseInventory()
    {
        isInventoryOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    /// <summary>
    /// インベントリの内容を最新の状態に更新する
    /// </summary>
    public void RefreshUI()
    {
        if (!isInventoryOpen) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        // 既存のアイコンを削除
        foreach (Transform child in gridContent)
        {
            Destroy(child.gameObject);
        }

        // 新しく再生成
        foreach (var cardData in gm.inventory)
        {
            CreateInventoryItem(cardData);
        }

        if (statusText != null)
        {
            statusText.text = $"手荷物: {gm.inventory.Count} / {gm.inventoryMaxSize}";
        }

        // FieldManagerのステータスUIも更新する
        if (gm.fieldManager != null)
        {
            gm.fieldManager.UpdateStatusUI();
        }
    }

    /// <summary>
    /// 1つのインベントリアイテム（漢字アイコン）を生成
    /// </summary>
    private void CreateInventoryItem(KanjiCardData cardData)
    {
        var go = new GameObject($"InvItem_{cardData.kanji}");
        go.transform.SetParent(gridContent, false);
        
        // RectTransform
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80f, 100f);

        // アイテムコントローラー
        var invCard = go.AddComponent<InventoryCardController>();
        invCard.cardData = cardData;

        // 背景
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        invCard.bgImage = bg;

        // 漢字テキスト
        var kanjiGo = new GameObject("Kanji");
        kanjiGo.transform.SetParent(go.transform, false);
        var kanjiText = kanjiGo.AddComponent<TextMeshProUGUI>();
        kanjiText.text = cardData.kanji;
        kanjiText.fontSize = 42;
        kanjiText.alignment = TextAlignmentOptions.Center;
        kanjiText.color = Color.white;
        kanjiText.raycastTarget = false;
        if (appFont != null) kanjiText.font = appFont;
        var kanjiRect = kanjiGo.GetComponent<RectTransform>();
        kanjiRect.anchorMin = Vector2.zero;
        kanjiRect.anchorMax = Vector2.one;
        kanjiRect.offsetMin = Vector2.zero;
        kanjiRect.offsetMax = Vector2.zero;
        invCard.kanjiText = kanjiText;

        // 合成プレビュー表示用
        var previewGo = new GameObject("Preview");
        previewGo.transform.SetParent(go.transform, false);
        var previewText = previewGo.AddComponent<TextMeshProUGUI>();
        previewText.text = "";
        previewText.fontSize = 48;
        previewText.alignment = TextAlignmentOptions.Center;
        previewText.color = new Color(1f, 0.9f, 0.3f, 1f); // 黄色
        previewText.raycastTarget = false;
        if (appFont != null) previewText.font = appFont;
        var previewRect = previewGo.GetComponent<RectTransform>();
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;
        previewGo.SetActive(false);
        
        invCard.fusionPreviewObj = previewGo;
        invCard.fusionPreviewText = previewText;

        invCard.Setup();
    }
}
