using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Playモード中にカードをクリックするテストツール（Editor）
/// </summary>
public static class PlayModeCardTest
{
    [MenuItem("Tools/Test/Click Card In Play Mode")]
    public static void ClickCard()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[PlayModeCardTest] Playモードで実行してください");
            return;
        }

        var cards = Object.FindObjectsByType<CardController>(FindObjectsSortMode.None);
        Debug.Log($"[PlayModeCardTest] カード検出数: {cards.Length}");

        if (cards.Length == 0)
        {
            Debug.LogError("[PlayModeCardTest] カードが見つかりません");
            return;
        }

        var gm = GameManager.Instance;
        CardController targetCard = null;

        // 合体不可のカードを優先的に選択
        foreach (var card in cards)
        {
            if (card.cardData == null) continue;

            bool hasAnyFusion = false;
            foreach (var other in cards)
            {
                if (other == card || other.cardData == null) continue;
                if (gm != null)
                {
                    var results = gm.FindFusionResults(card.cardData.cardId, other.cardData.cardId);
                    if (results.Count > 0)
                    {
                        hasAnyFusion = true;
                        break;
                    }
                }
            }

            if (!hasAnyFusion)
            {
                targetCard = card;
                break;
            }
        }

        if (targetCard == null) targetCard = cards[0];

        Debug.Log($"[PlayModeCardTest] カード '{targetCard.cardData?.kanji}' をクリック");

        var eventData = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left
        };
        targetCard.OnPointerClick(eventData);

        Debug.Log("[PlayModeCardTest] クリック完了。0.3秒後にスクリーンショット撮影...");

        // 0.3秒後にスクリーンショット
        float targetTime = (float)EditorApplication.timeSinceStartup + 0.3f;
        void TakeScreenshot()
        {
            if (EditorApplication.timeSinceStartup >= targetTime)
            {
                EditorApplication.update -= TakeScreenshot;
                ScreenCapture.CaptureScreenshot("Assets/Screenshots/card_feedback_test.png");
                Debug.Log("[PlayModeCardTest] スクリーンショット撮影完了!");
            }
        }
        EditorApplication.update += TakeScreenshot;
    }
}
