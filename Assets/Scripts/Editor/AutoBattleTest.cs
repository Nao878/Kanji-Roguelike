using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// テスト用：Playモード開始後に自動バトル＋カードクリックテスト
/// </summary>
[InitializeOnLoad]
public static class AutoBattleTest
{
    private const string AUTO_BATTLE_KEY = "AutoBattleTest_ShouldStart";
    private static float battleStartTime;
    private static float cardClickTime;
    private static float screenshotTime;
    private static bool waitingForBattle;
    private static bool waitingForClick;
    private static bool waitingForScreenshot;

    static AutoBattleTest()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/Test/Start Battle And Click Card")]
    public static void StartBattleTest()
    {
        EditorPrefs.SetBool(AUTO_BATTLE_KEY, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(AUTO_BATTLE_KEY, false))
        {
            EditorPrefs.SetBool(AUTO_BATTLE_KEY, false);
            battleStartTime = (float)EditorApplication.timeSinceStartup + 1.0f;
            waitingForBattle = true;
            waitingForClick = false;
            waitingForScreenshot = false;
            EditorApplication.update += UpdateLoop;
        }
    }

    private static void UpdateLoop()
    {
        float now = (float)EditorApplication.timeSinceStartup;

        // Phase 1: バトル開始
        if (waitingForBattle && now >= battleStartTime)
        {
            waitingForBattle = false;

            var gm = GameManager.Instance;
            if (gm == null || gm.battleManager == null)
            {
                Debug.LogError("[AutoBattleTest] GameManager/BattleManagerが見つかりません");
                EditorApplication.update -= UpdateLoop;
                return;
            }

            var enemies = Resources.LoadAll<EnemyData>("");
            if (enemies.Length > 0)
            {
                gm.InitializeBattleDeck();
                gm.ChangeState(GameState.Battle);
                gm.battleManager.StartBattle(enemies[0]);
                Debug.Log($"[AutoBattleTest] テストバトル開始！ 敵: {enemies[0].enemyName}");

                // 1.5秒後にカードクリック
                cardClickTime = now + 1.5f;
                waitingForClick = true;
            }
            else
            {
                Debug.LogError("[AutoBattleTest] 敵データが見つかりません");
                EditorApplication.update -= UpdateLoop;
            }
            return;
        }

        // Phase 2: カードクリック
        if (waitingForClick && now >= cardClickTime)
        {
            waitingForClick = false;

            var cards = Object.FindObjectsByType<CardController>(FindObjectsSortMode.None);
            if (cards.Length > 0)
            {
                // 合体不可のカードを見つけてクリック（例：民は民同士では合体不可）
                CardController targetCard = null;
                var gm = GameManager.Instance;
                
                foreach (var card in cards)
                {
                    if (card.cardData == null) continue;
                    
                    // 手札の中で合体可能なペアがないカードを探す
                    bool hasAnyFusion = false;
                    foreach (var other in cards)
                    {
                        if (other == card || other.cardData == null) continue;
                        var results = gm.FindFusionResults(card.cardData.cardId, other.cardData.cardId);
                        if (results.Count > 0)
                        {
                            hasAnyFusion = true;
                            break;
                        }
                    }

                    if (!hasAnyFusion)
                    {
                        targetCard = card;
                        break;
                    }
                }

                if (targetCard == null) targetCard = cards[0]; // フォールバック

                Debug.Log($"[AutoBattleTest] カード '{targetCard.cardData?.kanji}' をクリック");

                var eventData = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left
                };
                targetCard.OnPointerClick(eventData);

                // 0.3秒後にスクリーンショット（ポップアップが表示される瞬間）
                screenshotTime = now + 0.3f;
                waitingForScreenshot = true;
            }
            else
            {
                Debug.LogError("[AutoBattleTest] カードが見つかりません");
                EditorApplication.update -= UpdateLoop;
            }
            return;
        }

        // Phase 3: スクリーンショット
        if (waitingForScreenshot && now >= screenshotTime)
        {
            waitingForScreenshot = false;
            EditorApplication.update -= UpdateLoop;

            // Game Viewのスクリーンショットを撮影
            ScreenCapture.CaptureScreenshot("Assets/Screenshots/card_feedback_test.png");
            Debug.Log("[AutoBattleTest] スクリーンショット撮影完了: Assets/Screenshots/card_feedback_test.png");
        }
    }
}
