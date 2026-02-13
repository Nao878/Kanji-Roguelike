using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 漢字ローグライクプロジェクトの自動セットアップツール
/// Tools > Setup Kanji Roguelike から実行
/// </summary>
public class ProjectSetupTool : EditorWindow
{
    private static TMP_FontAsset appFont;

    [MenuItem("Tools/Setup Kanji Roguelike")]
    public static void SetupProject()
    {
        Debug.Log("=== 漢字ローグライク セットアップ開始 ===");

        // AppFontをロード
        appFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/AppFont SDF.asset");
        if (appFont == null)
        {
            Debug.LogWarning("AppFont SDF.asset が見つかりません。デフォルトフォントを使用します。");
        }
        else
        {
            Debug.Log($"  フォントロード: {appFont.name}");
        }

        // タグを登録
        RegisterTags();

        // 既存オブジェクトを削除
        CleanupExistingObjects();

        // データフォルダ作成
        CreateFolders();

        // ScriptableObject生成
        var cards = CreateCardData();
        var recipes = CreateFusionRecipes(cards);
        var database = CreateFusionDatabase(recipes);
        var enemies = CreateEnemyData();

        // シーンオブジェクト生成
        var gameManager = CreateGameManager(database, cards);
        var battleManager = CreateBattleManager(enemies);
        var mapManager = CreateMapManager();
        var fusionEngine = CreateFusionEngine(database);
        var canvas = CreateCanvas();

        // UIパネル作成
        var mapPanel = CreateMapPanel(canvas.transform, mapManager);
        var battlePanel = CreateBattlePanel(canvas.transform, battleManager);
        var fusionPanel = CreateFusionPanel(canvas.transform);

        // 参照の割り当て
        AssignReferences(gameManager, battleManager, mapManager, fusionEngine, mapPanel, battlePanel, fusionPanel);

        // 初期デッキ設定
        SetupInitialDeck(gameManager, cards);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== 漢字ローグライク セットアップ完了 ===");
        Debug.Log("Playモードで実行してください！");
    }

    // ====================================
    // クリーンアップ
    // ====================================
    private static void CleanupExistingObjects()
    {
        string[] objectNames = { "GameManager", "BattleManager", "MapManager", "FusionEngine", "MainCanvas", "EventSystem" };
        foreach (var name in objectNames)
        {
            var obj = GameObject.Find(name);
            if (obj != null) DestroyImmediate(obj);
        }
    }

    // ====================================
    // タグ登録
    // ====================================
    private static void RegisterTags()
    {
        string[] tagsToAdd = { "Enemy", "Card" };
        var tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        var tagsProp = tagManager.FindProperty("tags");

        foreach (var tag in tagsToAdd)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                Debug.Log($"  タグ登録: {tag}");
            }
        }
        tagManager.ApplyModifiedProperties();
    }

    // ====================================
    // フォルダ作成
    // ====================================
    private static void CreateFolders()
    {
        string[] folders = {
            "Assets/Data",
            "Assets/Data/Cards",
            "Assets/Data/Recipes",
            "Assets/Data/Enemies"
        };

        foreach (var folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder).Replace("\\", "/");
                string folderName = System.IO.Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }

    // ====================================
    // カードデータ作成
    // ====================================
    private static Dictionary<string, KanjiCardData> CreateCardData()
    {
        var cards = new Dictionary<string, KanjiCardData>();

        // カード定義: (cardId, 漢字, カード名, 説明, コスト, 効果値, タイプ, 合成結果か)
        var cardDefs = new (int id, string kanji, string name, string desc, int cost, int value, CardEffectType type, bool fusion)[]
        {
            (1, "木", "木", "木の力で敵に3ダメージ", 1, 3, CardEffectType.Attack, false),
            (2, "林", "林", "林の力で敵に7ダメージ", 1, 7, CardEffectType.Attack, true),
            (3, "森", "森", "森の力で敵に15ダメージ", 2, 15, CardEffectType.Attack, true),
            (4, "日", "日", "太陽の光でHPを3回復", 1, 3, CardEffectType.Heal, false),
            (5, "月", "月", "月の守りで防御力+3", 1, 3, CardEffectType.Defense, false),
            (6, "明", "明", "日月の力で5ダメージ+3回復", 2, 5, CardEffectType.Special, true),
            (7, "力", "力", "力を高めて攻撃力+2", 1, 2, CardEffectType.Buff, false),
            (8, "火", "火", "炎の力で敵に4ダメージ", 1, 4, CardEffectType.Attack, false),
        };

        foreach (var def in cardDefs)
        {
            string path = $"Assets/Data/Cards/Card_{def.kanji}.asset";

            // 既存アセットがあれば削除
            var existing = AssetDatabase.LoadAssetAtPath<KanjiCardData>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var card = ScriptableObject.CreateInstance<KanjiCardData>();
            card.cardId = def.id;
            card.kanji = def.kanji;
            card.cardName = def.name;
            card.description = def.desc;
            card.cost = def.cost;
            card.effectValue = def.value;
            card.effectType = def.type;
            card.isFusionResult = def.fusion;

            AssetDatabase.CreateAsset(card, path);
            cards[def.kanji] = card;
            Debug.Log($"  カード作成: 『{def.kanji}』 {def.desc}");
        }

        return cards;
    }

    // ====================================
    // 合成レシピ作成
    // ====================================
    private static List<KanjiFusionRecipe> CreateFusionRecipes(Dictionary<string, KanjiCardData> cards)
    {
        var recipes = new List<KanjiFusionRecipe>();

        var recipeDefs = new (string mat1, string mat2, string result)[]
        {
            ("木", "木", "林"),
            ("林", "木", "森"),
            ("日", "月", "明"),
        };

        foreach (var def in recipeDefs)
        {
            if (!cards.ContainsKey(def.mat1) || !cards.ContainsKey(def.mat2) || !cards.ContainsKey(def.result))
            {
                Debug.LogWarning($"  レシピスキップ: {def.mat1}+{def.mat2}={def.result}（カードが見つかりません）");
                continue;
            }

            string path = $"Assets/Data/Recipes/Recipe_{def.mat1}_{def.mat2}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<KanjiFusionRecipe>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var recipe = ScriptableObject.CreateInstance<KanjiFusionRecipe>();
            recipe.material1 = cards[def.mat1];
            recipe.material2 = cards[def.mat2];
            recipe.result = cards[def.result];

            AssetDatabase.CreateAsset(recipe, path);
            recipes.Add(recipe);
            Debug.Log($"  レシピ作成: 『{def.mat1}』+『{def.mat2}』=『{def.result}』");
        }

        return recipes;
    }

    // ====================================
    // 合成データベース作成
    // ====================================
    private static KanjiFusionDatabase CreateFusionDatabase(List<KanjiFusionRecipe> recipes)
    {
        string path = "Assets/Data/FusionDatabase.asset";
        var existing = AssetDatabase.LoadAssetAtPath<KanjiFusionDatabase>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);

        var db = ScriptableObject.CreateInstance<KanjiFusionDatabase>();
        db.recipes = recipes;

        AssetDatabase.CreateAsset(db, path);
        Debug.Log($"  合成データベース作成: {recipes.Count}レシピ");
        return db;
    }

    // ====================================
    // 敵データ作成
    // ====================================
    private static EnemyData[] CreateEnemyData()
    {
        var enemyDefs = new (string name, string kanji, int hp, int atk, EnemyType type)[]
        {
            ("スライム漢字", "字", 15, 3, EnemyType.Normal),
            ("妖怪文字", "怪", 20, 5, EnemyType.Normal),
            ("鬼", "鬼", 30, 7, EnemyType.Elite),
            ("龍", "龍", 50, 10, EnemyType.Boss),
        };

        var enemies = new List<EnemyData>();

        foreach (var def in enemyDefs)
        {
            string path = $"Assets/Data/Enemies/Enemy_{def.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.enemyName = def.name;
            enemy.displayKanji = def.kanji;
            enemy.maxHP = def.hp;
            enemy.attackPower = def.atk;
            enemy.enemyType = def.type;

            AssetDatabase.CreateAsset(enemy, path);
            enemies.Add(enemy);
            Debug.Log($"  敵作成: {def.kanji} {def.name} HP:{def.hp} ATK:{def.atk}");
        }

        return enemies.ToArray();
    }

    // ====================================
    // シーンオブジェクト作成
    // ====================================
    private static GameManager CreateGameManager(KanjiFusionDatabase database, Dictionary<string, KanjiCardData> cards)
    {
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();
        gm.fusionDatabase = database;
        return gm;
    }

    private static BattleManager CreateBattleManager(EnemyData[] enemies)
    {
        var go = new GameObject("BattleManager");
        var bm = go.AddComponent<BattleManager>();

        // 通常敵とボスを分類
        var normalList = new List<EnemyData>();
        EnemyData boss = null;
        foreach (var e in enemies)
        {
            if (e.enemyType == EnemyType.Boss) boss = e;
            else normalList.Add(e);
        }
        bm.normalEnemies = normalList.ToArray();
        bm.bossEnemy = boss;

        return bm;
    }

    private static MapManager CreateMapManager()
    {
        var go = new GameObject("MapManager");
        var mm = go.AddComponent<MapManager>();
        return mm;
    }

    private static KanjiFusionEngine CreateFusionEngine(KanjiFusionDatabase database)
    {
        var go = new GameObject("FusionEngine");
        var fe = go.AddComponent<KanjiFusionEngine>();
        fe.fusionDatabase = database;
        return fe;
    }

    // ====================================
    // Canvas作成
    // ====================================
    private static Canvas CreateCanvas()
    {
        // EventSystem（Input System Package対応）
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        var canvasGo = new GameObject("MainCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    // ====================================
    // マップパネル作成
    // ====================================
    private static GameObject CreateMapPanel(Transform parent, MapManager mapManager)
    {
        var panel = CreatePanel(parent, "MapPanel", new Color(0.08f, 0.1f, 0.15f, 0.95f));

        // タイトル
        CreateText(panel.transform, "TitleText", "漢字の迷宮", 36,
            new Vector2(0.2f, 0.9f), new Vector2(0.8f, 0.99f), Color.white);

        // 階層テキスト
        var floorText = CreateText(panel.transform, "FloorText", "階層: 1 / 5", 22,
            new Vector2(0.3f, 0.84f), new Vector2(0.7f, 0.92f), new Color(0.7f, 0.8f, 0.9f));

        // マップコンテンツエリア
        var mapContent = new GameObject("MapContent");
        mapContent.transform.SetParent(panel.transform, false);
        var contentRect = mapContent.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.1f, 0.1f);
        contentRect.anchorMax = new Vector2(0.9f, 0.85f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // 合体ボタン
        CreateButton(panel.transform, "FusionButton", "合体", 22,
            new Vector2(0.75f, 0.91f), new Vector2(0.97f, 0.99f),
            new Color(0.6f, 0.3f, 0.8f), () =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameState.Fusion);
            });

        // MapManagerの参照設定
        mapManager.mapContent = contentRect;
        mapManager.floorText = floorText;

        return panel;
    }

    // ====================================
    // 戦闘パネル作成
    // ====================================
    private static GameObject CreateBattlePanel(Transform parent, BattleManager battleManager)
    {
        var panel = CreatePanel(parent, "BattlePanel", new Color(0.12f, 0.08f, 0.08f, 0.95f));
        panel.SetActive(false);

        // 敵ドロップエリア（Tag "Enemy"）
        var enemyDropArea = new GameObject("EnemyDropArea");
        enemyDropArea.transform.SetParent(panel.transform, false);
        enemyDropArea.tag = "Enemy";
        var enemyDropRect = enemyDropArea.AddComponent<RectTransform>();
        enemyDropRect.anchorMin = new Vector2(0.2f, 0.34f);
        enemyDropRect.anchorMax = new Vector2(0.8f, 0.92f);
        enemyDropRect.offsetMin = Vector2.zero;
        enemyDropRect.offsetMax = Vector2.zero;
        var enemyDropImage = enemyDropArea.AddComponent<Image>();
        enemyDropImage.color = new Color(0.5f, 0.1f, 0.1f, 0.15f);

        var enemyKanjiText = CreateText(enemyDropArea.transform, "EnemyKanjiText", "字", 80,
            new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.95f), new Color(0.9f, 0.3f, 0.3f));

        var enemyNameText = CreateText(enemyDropArea.transform, "EnemyNameText", "敵の名前", 24,
            new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.35f), Color.white);

        var enemyHPText = CreateText(enemyDropArea.transform, "EnemyHPText", "HP: 20/20", 22,
            new Vector2(0.2f, 0.0f), new Vector2(0.8f, 0.15f), new Color(1f, 0.4f, 0.4f));

        // プレイヤー情報
        var playerHPText = CreateText(panel.transform, "PlayerHPText", "HP: 50/50", 24,
            new Vector2(0.02f, 0.28f), new Vector2(0.25f, 0.38f), new Color(0.4f, 1f, 0.4f));

        var playerManaText = CreateText(panel.transform, "PlayerManaText", "マナ: 3/3", 22,
            new Vector2(0.02f, 0.2f), new Vector2(0.25f, 0.3f), new Color(0.4f, 0.6f, 1f));

        // 手札エリア
        var handArea = new GameObject("HandArea");
        handArea.transform.SetParent(panel.transform, false);
        var handRect = handArea.AddComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.1f, 0.02f);
        handRect.anchorMax = new Vector2(0.75f, 0.18f);
        handRect.offsetMin = Vector2.zero;
        handRect.offsetMax = Vector2.zero;
        var hlg = handArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // ターン終了ボタン
        var endTurnBtn = CreateButton(panel.transform, "EndTurnButton", "ターン終了", 20,
            new Vector2(0.78f, 0.02f), new Vector2(0.98f, 0.12f),
            new Color(0.3f, 0.5f, 0.7f), null);

        // バトルログ
        var battleLogText = CreateText(panel.transform, "BattleLogText", "", 16,
            new Vector2(0.72f, 0.14f), new Vector2(0.98f, 0.38f), new Color(0.8f, 0.8f, 0.6f));
        battleLogText.alignment = TextAlignmentOptions.TopLeft;
        battleLogText.overflowMode = TextOverflowModes.Truncate;

        // BattleUI コンポーネント追加
        var battleUI = panel.AddComponent<BattleUI>();
        battleUI.playerHPText = playerHPText;
        battleUI.playerManaText = playerManaText;
        battleUI.enemyNameText = enemyNameText;
        battleUI.enemyHPText = enemyHPText;
        battleUI.enemyKanjiText = enemyKanjiText;
        battleUI.enemyArea = enemyDropArea;
        battleUI.handArea = handRect;
        battleUI.endTurnButton = endTurnBtn.GetComponent<Button>();
        battleUI.battleLogText = battleLogText;

        // BattleManagerの参照設定
        battleManager.playerHPText = playerHPText;
        battleManager.playerManaText = playerManaText;
        battleManager.enemyNameText = enemyNameText;
        battleManager.enemyHPText = enemyHPText;
        battleManager.battleLogText = battleLogText;
        battleManager.handArea = handRect;
        battleManager.endTurnButton = endTurnBtn.GetComponent<Button>();

        return panel;
    }

    // ====================================
    // 合体パネル作成
    // ====================================
    private static GameObject CreateFusionPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "FusionPanel", new Color(0.1f, 0.08f, 0.15f, 0.95f));
        panel.SetActive(false);

        // タイトル
        CreateText(panel.transform, "FusionTitle", "漢字合体", 34,
            new Vector2(0.25f, 0.9f), new Vector2(0.75f, 0.99f), new Color(0.8f, 0.6f, 1f));

        // スロット1
        var slot1Bg = CreateUIPanel(panel.transform, "Slot1", new Color(0.2f, 0.2f, 0.35f),
            new Vector2(0.2f, 0.6f), new Vector2(0.35f, 0.85f));
        var slot1Text = CreateText(slot1Bg.transform, "Slot1Text", "?", 54,
            new Vector2(0, 0), new Vector2(1, 1), Color.white);

        // プラス記号
        CreateText(panel.transform, "PlusText", "+", 40,
            new Vector2(0.38f, 0.65f), new Vector2(0.45f, 0.8f), Color.white);

        // スロット2
        var slot2Bg = CreateUIPanel(panel.transform, "Slot2", new Color(0.2f, 0.2f, 0.35f),
            new Vector2(0.48f, 0.6f), new Vector2(0.63f, 0.85f));
        var slot2Text = CreateText(slot2Bg.transform, "Slot2Text", "?", 54,
            new Vector2(0, 0), new Vector2(1, 1), Color.white);

        // イコール記号
        CreateText(panel.transform, "EqualsText", "＝", 40,
            new Vector2(0.65f, 0.65f), new Vector2(0.72f, 0.8f), Color.white);

        // 結果スロット
        var resultBg = CreateUIPanel(panel.transform, "ResultSlot", new Color(0.35f, 0.25f, 0.45f),
            new Vector2(0.73f, 0.6f), new Vector2(0.88f, 0.85f));
        var resultText = CreateText(resultBg.transform, "ResultText", "?", 54,
            new Vector2(0, 0.3f), new Vector2(1, 1), new Color(1f, 0.9f, 0.4f));
        var resultDescText = CreateText(resultBg.transform, "ResultDescText", "カードを2枚選択", 14,
            new Vector2(0, 0), new Vector2(1, 0.35f), new Color(0.8f, 0.8f, 0.8f));

        // ステータステキスト
        var statusText = CreateText(panel.transform, "StatusText", "1枚目のカードを選択してください", 20,
            new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.6f), new Color(0.7f, 0.8f, 0.9f));

        // カード一覧エリア
        var cardListArea = new GameObject("CardListArea");
        cardListArea.transform.SetParent(panel.transform, false);
        var listRect = cardListArea.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.05f, 0.12f);
        listRect.anchorMax = new Vector2(0.95f, 0.5f);
        listRect.offsetMin = Vector2.zero;
        listRect.offsetMax = Vector2.zero;
        var gridLayout = cardListArea.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(90, 110);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.childAlignment = TextAnchor.UpperCenter;

        // 合成ボタン
        var fuseBtn = CreateButton(panel.transform, "FuseButton", "合成！", 24,
            new Vector2(0.3f, 0.02f), new Vector2(0.52f, 0.11f),
            new Color(0.6f, 0.3f, 0.8f), null);

        // クリアボタン
        var clearBtn = CreateButton(panel.transform, "ClearButton", "クリア", 20,
            new Vector2(0.54f, 0.02f), new Vector2(0.72f, 0.11f),
            new Color(0.5f, 0.5f, 0.5f), null);

        // 戻るボタン
        var backBtn = CreateButton(panel.transform, "BackButton", "戻る", 20,
            new Vector2(0.74f, 0.02f), new Vector2(0.92f, 0.11f),
            new Color(0.4f, 0.4f, 0.5f), null);

        // FusionUI コンポーネント追加
        var fusionUI = panel.AddComponent<FusionUI>();
        fusionUI.slot1Image = slot1Bg.GetComponent<Image>();
        fusionUI.slot1Text = slot1Text;
        fusionUI.slot2Image = slot2Bg.GetComponent<Image>();
        fusionUI.slot2Text = slot2Text;
        fusionUI.resultImage = resultBg.GetComponent<Image>();
        fusionUI.resultText = resultText;
        fusionUI.resultDescText = resultDescText;
        fusionUI.fuseButton = fuseBtn.GetComponent<Button>();
        fusionUI.clearButton = clearBtn.GetComponent<Button>();
        fusionUI.backButton = backBtn.GetComponent<Button>();
        fusionUI.cardListArea = listRect;
        fusionUI.statusText = statusText;

        return panel;
    }

    // ====================================
    // 参照の割り当て
    // ====================================
    private static void AssignReferences(GameManager gm, BattleManager bm, MapManager mm, KanjiFusionEngine fe,
        GameObject mapPanel, GameObject battlePanel, GameObject fusionPanel)
    {
        gm.battleManager = bm;
        gm.mapManager = mm;
        gm.fusionEngine = fe;
        gm.mapPanel = mapPanel;
        gm.battlePanel = battlePanel;
        gm.fusionPanel = fusionPanel;

        // ランタイムUIコンポーネントにAppFont参照を割り当て
        if (appFont != null)
        {
            mm.appFont = appFont;

            var battleUI = battlePanel.GetComponent<BattleUI>();
            if (battleUI != null)
            {
                battleUI.appFont = appFont;
                bm.battleUI = battleUI; // BattleManagerからBattleUIへの参照
            }

            var fusionUI = fusionPanel.GetComponent<FusionUI>();
            if (fusionUI != null) fusionUI.appFont = appFont;
        }
        else
        {
            // appFontがなくてもBattleUI参照は設定
            var battleUI = battlePanel.GetComponent<BattleUI>();
            if (battleUI != null) bm.battleUI = battleUI;
        }

        // EditorのDirtyフラグ設定
        EditorUtility.SetDirty(gm);
        EditorUtility.SetDirty(bm);
        EditorUtility.SetDirty(mm);
        EditorUtility.SetDirty(fe);

        Debug.Log("  全参照の割り当て完了");
    }

    // ====================================
    // 初期デッキ設定
    // ====================================
    private static void SetupInitialDeck(GameManager gm, Dictionary<string, KanjiCardData> cards)
    {
        gm.deck.Clear();

        // 初期デッキ: 木x4, 火x2, 日x2, 月x1, 力x1
        AddCardsToDeck(gm, cards, "木", 4);
        AddCardsToDeck(gm, cards, "火", 2);
        AddCardsToDeck(gm, cards, "日", 2);
        AddCardsToDeck(gm, cards, "月", 1);
        AddCardsToDeck(gm, cards, "力", 1);

        EditorUtility.SetDirty(gm);
        Debug.Log($"  初期デッキ設定完了: {gm.deck.Count}枚");
    }

    private static void AddCardsToDeck(GameManager gm, Dictionary<string, KanjiCardData> cards, string kanji, int count)
    {
        if (!cards.ContainsKey(kanji)) return;
        for (int i = 0; i < count; i++)
        {
            gm.deck.Add(cards[kanji]);
        }
    }

    // ====================================
    // UIヘルパー
    // ====================================
    private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = bgColor;

        return go;
    }

    private static GameObject CreateUIPanel(Transform parent, string name, Color bgColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = bgColor;

        return go;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (appFont != null) tmp.font = appFont;

        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor, System.Action onClick = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = bgColor;

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(
            Mathf.Min(1f, bgColor.r + 0.15f),
            Mathf.Min(1f, bgColor.g + 0.15f),
            Mathf.Min(1f, bgColor.b + 0.15f), 1f);
        colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, 1f);
        button.colors = colors;

        // ラベルテキスト
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (appFont != null) tmp.font = appFont;

        return go;
    }
}
