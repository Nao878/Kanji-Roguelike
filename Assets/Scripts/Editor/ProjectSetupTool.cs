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
        var enemies = CreateEnemyData(cards);

        // シーンオブジェクト生成
        var gameManager = CreateGameManager(database, cards);
        var battleManager = CreateBattleManager(enemies);
        var mapManager = CreateMapManager();
        var fieldManager = CreateFieldManager();
        var fusionEngine = CreateFusionEngine(database);
        var vfxManager = CreateVFXManager();
        var canvas = CreateCanvas();

        // UIパネル作成
        var fieldPanel = CreateFieldPanel(canvas.transform, fieldManager);
        var mapPanel = CreateMapPanel(canvas.transform, mapManager);
        mapPanel.SetActive(false); // 旧マップは非表示
        var battlePanel = CreateBattlePanel(canvas.transform, battleManager);
        var fusionPanel = CreateFusionPanel(canvas.transform);
        var shopPanel = CreateShopPanel(canvas.transform);
        var dojoPanel = CreateDojoPanel(canvas.transform);
        var deckViewerPanel = CreateDeckViewerPanel(canvas.transform);
        var encyclopediaPanel = CreateEncyclopediaPanel(canvas.transform);
        var fusionSelectionPanel = CreateFusionSelectionPanel(canvas.transform);

        // フィールドパネルにボタンを接続
        var fieldDeckBtn = fieldPanel.transform.Find("FieldDeckBtn")?.GetComponent<Button>();
        if (fieldDeckBtn != null) fieldDeckBtn.onClick.AddListener(() => deckViewerPanel.SetActive(true));
        var fieldEncycBtn = fieldPanel.transform.Find("FieldEncycBtn")?.GetComponent<Button>();
        if (fieldEncycBtn != null) fieldEncycBtn.onClick.AddListener(() => encyclopediaPanel.SetActive(true));

        // 参照の割り当て
        AssignReferences(gameManager, battleManager, mapManager, fieldManager, fusionEngine, fieldPanel, mapPanel, battlePanel, fusionPanel, shopPanel, dojoPanel);

        // 初期インベントリ設定
        SetupInitialInventory(gameManager, cards);

        // 仕様書生成
        GameDesignDocGenerator.Generate();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== 漢字ローグライク セットアップ完了 ===");
        Debug.Log("PlayモードでWASDキーでフィールドを移動してください！");
    }

    // ====================================
    // クリーンアップ
    // ====================================
    private static void CleanupExistingObjects()
    {
        string[] objectNames = { "GameManager", "BattleManager", "MapManager", "FieldManager", "FusionEngine", "VFXManager", "MainCanvas", "EventSystem" };
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
        string[] folders = new string[]
        {
            "Assets/Resources",
            "Assets/Resources/CardData",
            "Assets/Resources/Recipes",
            "Assets/Resources/Enemies"
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

        // カード定義: (cardId, 漢字, カード名, 説明, コスト, 効果値, タイプ, 合成結果か, 属性)
        var cardDefs = new (int id, string kanji, string name, string desc, int cost, int value, CardEffectType type, bool fusion, CardElement elem)[]
        {
            // --- 既存カード ---
            (1, "木", "木", "木の力で敵に3ダメージ", 1, 3, CardEffectType.Attack, false, CardElement.Wood),
            (2, "林", "林", "林の力で敵に7ダメージ", 1, 7, CardEffectType.Attack, true, CardElement.Wood),
            (3, "森", "森", "森の力で敵に15ダメージ", 2, 15, CardEffectType.Attack, true, CardElement.Wood),
            (4, "日", "日", "太陽の光でHPを3回復", 1, 3, CardEffectType.Heal, false, CardElement.Sun),
            (5, "月", "月", "月の守りで防御力+3", 1, 3, CardEffectType.Defense, false, CardElement.Moon),
            (6, "明", "明", "日月の力で5ダメージ+3回復", 2, 5, CardEffectType.Special, true, CardElement.Sun),
            (7, "力", "力", "力を高めて攻撃力+2", 1, 2, CardEffectType.Buff, false, CardElement.None),
            (8, "火", "火", "炎の力で敵に4ダメージ", 1, 4, CardEffectType.Attack, false, CardElement.Fire),

            // --- 基礎漢字 ---
            (9, "田", "田", "田んぼの土で防御+2", 1, 2, CardEffectType.Defense, false, CardElement.Earth),
            (10, "口", "口", "口を開けてカードを1枚引く", 1, 1, CardEffectType.Draw, false, CardElement.None),
            (11, "十", "十", "十字斬りで敵に2ダメージ", 1, 2, CardEffectType.Attack, false, CardElement.None),
            (12, "大", "大", "大きく構えて敵に5ダメージ", 2, 5, CardEffectType.Attack, false, CardElement.None),
            (13, "土", "土", "土壁を作って防御+3", 1, 3, CardEffectType.Defense, false, CardElement.Earth),
            
            // --- 木偏素材 ---
            (22, "人", "人", "人の手で微弱な3ダメージ", 1, 3, CardEffectType.Attack, false, CardElement.None),
            (23, "目", "目", "目を凝らして2枚ドロー", 1, 2, CardEffectType.Draw, false, CardElement.None),
            (24, "白", "白", "白壁で防御+4", 1, 4, CardEffectType.Defense, false, CardElement.None),
            (25, "公", "公", "公平に4ダメージ", 2, 4, CardEffectType.Attack, false, CardElement.None),

            // --- 新規基礎 ---
            (26, "民", "民", "民の結束で防御+3", 1, 3, CardEffectType.Defense, false, CardElement.None),

            // --- 合体漢字 ---
            (14, "畑", "畑", "火と土の恵みで3ダメージ+3防御", 2, 3, CardEffectType.Special, true, CardElement.Fire),
            (15, "加", "加", "力を加えて攻撃力+2", 2, 2, CardEffectType.Buff, true, CardElement.None),
            (16, "男", "男", "畑仕事の力で8ダメージ", 2, 8, CardEffectType.Attack, true, CardElement.None),
            (17, "回", "回", "ぐるぐる回して2枚ドロー", 2, 2, CardEffectType.Draw, true, CardElement.None),
            (18, "古", "古", "古の知恵でHP4回復", 1, 4, CardEffectType.Heal, true, CardElement.None),
            (19, "本", "本", "基本に立ち返り攻撃力+3", 2, 3, CardEffectType.Buff, true, CardElement.Wood),
            (20, "圭", "圭", "土を重ねて防御+6", 2, 6, CardEffectType.Defense, true, CardElement.Earth),
            (21, "早", "早", "早業で1ドロー", 2, 1, CardEffectType.Draw, true, CardElement.Sun),

            // --- 木偏合体 ---
            (27, "休", "休", "木陰で休んでHP8回復", 2, 8, CardEffectType.Heal, true, CardElement.Wood),
            (28, "相", "相", "相手を見て3枚ドロー", 2, 3, CardEffectType.Draw, true, CardElement.Wood),
            (29, "柏", "柏", "柏の盾で防御+12", 2, 12, CardEffectType.Defense, true, CardElement.Wood),
            (30, "松", "松", "松の棘で敵全体に10ダメージ", 3, 10, CardEffectType.AttackAll, true, CardElement.Wood),
            (31, "果", "果", "果実を食べてHP6回復", 2, 6, CardEffectType.Heal, true, CardElement.Wood),
            (32, "東", "東", "日が昇り攻撃力+3", 2, 3, CardEffectType.Buff, true, CardElement.Sun),
            (33, "困", "困", "囲んで敵を1ターンスタン", 2, 1, CardEffectType.Stun, true, CardElement.None),

            // --- 新規合体 ---
            (34, "炎", "炎", "二つの炎で敵に12ダメージ", 2, 12, CardEffectType.Attack, true, CardElement.Fire),
            (35, "眠", "眠", "催眠で敵を1ターンスタン", 2, 1, CardEffectType.Stun, true, CardElement.None),
            (36, "品", "品", "三つの口で3枚ドロー", 2, 3, CardEffectType.Draw, true, CardElement.None),
            (37, "晶", "晶", "三つの太陽でHP15回復", 3, 15, CardEffectType.Heal, true, CardElement.Sun),
        };

        foreach (var def in cardDefs)
        {
            string path = $"Assets/Resources/CardData/Card_{def.kanji}.asset";

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
            card.element = def.elem;
            card.attackModifier = 0;
            card.defenseModifier = 0;

            AssetDatabase.CreateAsset(card, path);
            cards[def.kanji] = card;
            Debug.Log($"  カード作成: 『{def.kanji}』 [{def.elem}] {def.desc}");
        }

        return cards;
    }

    // ====================================
    // 合成レシピ作成
    // ====================================
    private static List<KanjiFusionRecipe> CreateFusionRecipes(Dictionary<string, KanjiCardData> cards)
    {
        var recipes = new List<KanjiFusionRecipe>();

        // 2枚合体レシピ: (素材1, 素材2, 結果)
        var recipeDefs2 = new (string mat1, string mat2, string result)[]
        {
            // --- 既存レシピ ---
            ("木", "木", "林"),
            ("林", "木", "森"),
            ("日", "月", "明"),

            // --- 基本レシピ ---
            ("火", "田", "畑"),
            ("力", "口", "加"),
            ("力", "田", "男"),
            ("口", "口", "回"),
            ("十", "口", "古"),
            ("大", "木", "本"),
            ("土", "土", "圭"),
            ("日", "十", "早"),
            
            // --- 木偏レシピ ---
            ("人", "木", "休"),
            ("木", "目", "相"),
            ("木", "白", "柏"),
            ("木", "公", "松"),
            ("田", "木", "果"),
            ("日", "木", "東"),
            ("口", "木", "困"),

            // --- 新規レシピ ---
            ("火", "火", "炎"),
            ("目", "民", "眠"),
        };

        // 3枚合体レシピ: (素材1, 素材2, 素材3, 結果)
        var recipeDefs3 = new (string mat1, string mat2, string mat3, string result)[]
        {
            ("口", "口", "口", "品"),
            ("日", "日", "日", "晶"),
        };

        // 2枚レシピ生成
        foreach (var def in recipeDefs2)
        {
            if (!cards.ContainsKey(def.mat1) || !cards.ContainsKey(def.mat2) || !cards.ContainsKey(def.result))
            {
                Debug.LogWarning($"  レシピスキップ: {def.mat1}+{def.mat2}={def.result}（カード未定義）");
                continue;
            }

            string path = $"Assets/Data/Recipes/Recipe_{def.mat1}_{def.mat2}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<KanjiFusionRecipe>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var recipe = ScriptableObject.CreateInstance<KanjiFusionRecipe>();
            recipe.material1 = cards[def.mat1];
            recipe.material2 = cards[def.mat2];
            recipe.material3 = null; // 2枚合体
            recipe.result = cards[def.result];

            AssetDatabase.CreateAsset(recipe, path);
            recipes.Add(recipe);
            Debug.Log($"  レシピ作成: 『{def.mat1}』+『{def.mat2}』=『{def.result}』");
        }

        // 3枚レシピ生成
        foreach (var def in recipeDefs3)
        {
            if (!cards.ContainsKey(def.mat1) || !cards.ContainsKey(def.mat2) ||
                !cards.ContainsKey(def.mat3) || !cards.ContainsKey(def.result))
            {
                Debug.LogWarning($"  レシピスキップ: {def.mat1}+{def.mat2}+{def.mat3}={def.result}（カード未定義）");
                continue;
            }

            string path = $"Assets/Resources/Recipes/Recipe_{def.mat1}_{def.mat2}_{def.mat3}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<KanjiFusionRecipe>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var recipe = ScriptableObject.CreateInstance<KanjiFusionRecipe>();
            recipe.material1 = cards[def.mat1];
            recipe.material2 = cards[def.mat2];
            recipe.material3 = cards[def.mat3];
            recipe.result = cards[def.result];

            AssetDatabase.CreateAsset(recipe, path);
            recipes.Add(recipe);
            Debug.Log($"  レシピ作成: 『{def.mat1}』+『{def.mat2}』+『{def.mat3}』=『{def.result}』");
        }

        return recipes;
    }

    // ====================================
    // 合成データベース作成
    // ====================================
    private static KanjiFusionDatabase CreateFusionDatabase(List<KanjiFusionRecipe> recipes)
    {
        string dbPath = "Assets/Resources/Recipes/KanjiFusionDatabase.asset";
        var existing = AssetDatabase.LoadAssetAtPath<KanjiFusionDatabase>(dbPath);
        if (existing != null) AssetDatabase.DeleteAsset(dbPath);

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/CardData")) AssetDatabase.CreateFolder("Assets/Resources", "CardData");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Recipes")) AssetDatabase.CreateFolder("Assets/Resources", "Recipes");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Enemies")) AssetDatabase.CreateFolder("Assets/Resources", "Enemies");

        var db = ScriptableObject.CreateInstance<KanjiFusionDatabase>();
        db.recipes = recipes;

        AssetDatabase.CreateAsset(db, dbPath);
        Debug.Log($"  合成データベース作成: {recipes.Count}レシピ");
        return db;
    }

    // ====================================
    // 敵データ作成
    // ====================================
    private static EnemyData[] CreateEnemyData(Dictionary<string, KanjiCardData> cards)
    {
        // (名前, 漢字, HP, ATK, タイプ, ドロップ漢字キー)
        var enemyDefs = new (string name, string kanji, int hp, int atk, EnemyType type, string dropKanji)[]
        {
            ("木の精", "木", 15, 3, EnemyType.Normal, "木"),
            ("火の精", "火", 18, 4, EnemyType.Normal, "火"),
            ("土の精", "土", 20, 3, EnemyType.Normal, "土"),
            ("日の精", "日", 15, 2, EnemyType.Normal, "日"),
            ("月の精", "月", 15, 2, EnemyType.Normal, "月"),
            ("水の精", "水", 15, 3, EnemyType.Normal, "水"),
            ("人の精", "人", 12, 5, EnemyType.Normal, "人"),
            ("エリート鬼", "力", 30, 7, EnemyType.Elite, "力"),
            ("ボス龍", "大", 50, 10, EnemyType.Boss, "大"),
        };

        var enemies = new List<EnemyData>();

        foreach (var def in enemyDefs)
        {
            string path = $"Assets/Resources/Enemies/Enemy_{def.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.enemyName = def.name;
            enemy.displayKanji = def.kanji;
            enemy.maxHP = def.hp;
            enemy.attackPower = def.atk;
            enemy.enemyType = def.type;

            // ドロップカード設定
            if (!string.IsNullOrEmpty(def.dropKanji) && cards.ContainsKey(def.dropKanji))
            {
                enemy.dropCard = cards[def.dropKanji];
            }

            AssetDatabase.CreateAsset(enemy, path);
            enemies.Add(enemy);
            Debug.Log($"  敵作成: {def.kanji} {def.name} HP:{def.hp} ATK:{def.atk} Drop:{def.dropKanji}");
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
        go.AddComponent<EncyclopediaManager>();
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

    private static FieldManager CreateFieldManager()
    {
        var go = new GameObject("FieldManager");
        var fm = go.AddComponent<FieldManager>();
        return fm;
    }

    private static KanjiFusionEngine CreateFusionEngine(KanjiFusionDatabase database)
    {
        var go = new GameObject("FusionEngine");
        var fe = go.AddComponent<KanjiFusionEngine>();
        fe.fusionDatabase = database;
        return fe;
    }

    // ====================================
    // VFXManager作成
    // ====================================
    private static GameObject CreateVFXManager()
    {
        var go = new GameObject("VFXManager");
        var vfx = go.AddComponent<VFXManager>();
        
        // フォント参照設定
        if (appFont != null) vfx.appFont = appFont;

        // AnimationCurve設定 (コードでボヨヨン設定)
        // 0 -> 1.2 -> 1.0
        var curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0f, 0f, 0f, 6f));    // 開始点 (接線傾き急)
        curve.AddKey(new Keyframe(0.6f, 1.2f, 0f, 0f)); // ピーク
        curve.AddKey(new Keyframe(1f, 1f, -1f, 0f));    // 終点 (少し戻る)
        vfx.spawnCurve = curve;

        return go;
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

        // 背景（墨色パネル）
        var bgPanel = new GameObject("BackgroundPanel");
        bgPanel.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 1f); // #1A1A1A
        // 背景は最背面（Hierarchyの一番上）にあるべきだが、
        // CreateMapPanelなどが後から追加されるので、canvasGoの子要素の最初に追加すればOK。
        // 今はまだ誰も子供がいないので、最初に追加される。


        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    // ====================================
    // フィールドパネル作成（2D見下ろし探索画面）
    // ====================================
    private static GameObject CreateFieldPanel(Transform parent, FieldManager fieldManager)
    {
        var panel = CreatePanel(parent, "FieldPanel", new Color(0.05f, 0.08f, 0.05f, 0.98f));

        // タイトル
        CreateText(panel.transform, "FieldTitle", "漢字の迷宮 ─ フィールド探索", 24,
            new Vector2(0.1f, 0.93f), new Vector2(0.9f, 0.99f), new Color(0.9f, 0.95f, 0.85f));

        // HP表示
        var hpText = CreateText(panel.transform, "FieldHPText", "HP: 50/50", 20,
            new Vector2(0.02f, 0.88f), new Vector2(0.2f, 0.93f), new Color(0.4f, 0.9f, 0.4f));

        // 所持数表示
        var inventoryText = CreateText(panel.transform, "FieldInvText", "所持: 30/30", 20,
            new Vector2(0.22f, 0.88f), new Vector2(0.42f, 0.93f), new Color(0.8f, 0.8f, 0.5f));

        // ゴールド表示
        var goldText = CreateText(panel.transform, "FieldGoldText", "金: 50G", 20,
            new Vector2(0.78f, 0.88f), new Vector2(0.98f, 0.93f), new Color(1f, 0.85f, 0.2f));

        // デッキ確認ボタン
        CreateButton(panel.transform, "FieldDeckBtn", "🎴 所持品", 16,
            new Vector2(0.44f, 0.88f), new Vector2(0.58f, 0.93f), new Color(0.3f, 0.45f, 0.35f), null);

        // 図鑑ボタン
        CreateButton(panel.transform, "FieldEncycBtn", "📖 図鑑", 16,
            new Vector2(0.6f, 0.88f), new Vector2(0.74f, 0.93f), new Color(0.35f, 0.35f, 0.45f), null);

        // 操作ヒント
        CreateText(panel.transform, "FieldHint", "WASD / 矢印キーで移動　敵に触れると戦闘", 14,
            new Vector2(0.1f, 0.01f), new Vector2(0.9f, 0.05f), new Color(0.5f, 0.5f, 0.5f, 0.7f));

        // フィールドコンテンツエリア（グリッド描画領域）
        var fieldContent = new GameObject("FieldContent");
        fieldContent.transform.SetParent(panel.transform, false);
        var fieldRect = fieldContent.AddComponent<RectTransform>();
        fieldRect.anchorMin = new Vector2(0.02f, 0.06f);
        fieldRect.anchorMax = new Vector2(0.98f, 0.87f);
        fieldRect.offsetMin = Vector2.zero;
        fieldRect.offsetMax = Vector2.zero;

        // FieldManagerの参照を設定
        fieldManager.fieldContent = fieldRect;
        fieldManager.hpText = hpText;
        fieldManager.inventoryCountText = inventoryText;
        fieldManager.goldText = goldText;

        return panel;
    }

    // ====================================
    // マップパネル作成（漢字地形背景）- 旧Slay the Spire型（非アクティブ）
    // ====================================
    private static GameObject CreateMapPanel(Transform parent, MapManager mapManager)
    {
        var panel = CreatePanel(parent, "MapPanel", new Color(0.05f, 0.07f, 0.10f, 0.98f));

        // 背景漢字エリア（地形テクスチャ）
        var bgArea = new GameObject("BackgroundKanjiArea");
        bgArea.transform.SetParent(panel.transform, false);
        var bgRect = bgArea.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // タイトル
        CreateText(panel.transform, "TitleText", "漢字の迷宮", 36,
            new Vector2(0.2f, 0.9f), new Vector2(0.8f, 0.99f), Color.white);

        // 階層テキスト
        var floorText = CreateText(panel.transform, "FloorText", "階層: 1 / 5", 22,
            new Vector2(0.02f, 0.91f), new Vector2(0.2f, 0.99f), new Color(0.7f, 0.8f, 0.9f));

        // ゴールド表示
        var goldText = CreateText(panel.transform, "GoldText", "金: 50G", 22,
            new Vector2(0.8f, 0.91f), new Vector2(0.98f, 0.99f), new Color(1f, 0.85f, 0.2f));

        // 山札確認ボタン
        CreateButton(panel.transform, "MapDeckBtn", "🎴 山札", 18,
            new Vector2(0.3f, 0.91f), new Vector2(0.45f, 0.98f), new Color(0.4f, 0.5f, 0.4f), null);

        // 漢字図鑑ボタン
        CreateButton(panel.transform, "MapEncycBtn", "📖 図鑑", 18,
            new Vector2(0.5f, 0.91f), new Vector2(0.65f, 0.98f), new Color(0.4f, 0.4f, 0.5f), null);

        // マップコンテンツエリア
        var mapContent = new GameObject("MapContent");
        mapContent.transform.SetParent(panel.transform, false);
        var contentRect = mapContent.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.05f, 0.03f);
        contentRect.anchorMax = new Vector2(0.95f, 0.88f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // MapManagerの参照設定
        mapManager.mapContent = contentRect;
        mapManager.floorText = floorText;
        mapManager.goldText = goldText;
        mapManager.backgroundArea = bgRect.transform;

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

        var enemyHPText = CreateText(enemyDropArea.transform, "EnemyHPText", "HP: 10/10", 20,
            new Vector2(0.1f, -0.1f), new Vector2(0.9f, 0.1f), new Color(1f, 0.4f, 0.4f));

        // プレイヤー情報
        var playerHPText = CreateText(panel.transform, "PlayerHPText", "HP: 50/50", 24,
            new Vector2(0.02f, 0.38f), new Vector2(0.25f, 0.48f), new Color(0.4f, 1f, 0.4f));

        var playerManaText = CreateText(panel.transform, "PlayerManaText", "マナ: 3/3", 22,
            new Vector2(0.02f, 0.30f), new Vector2(0.25f, 0.40f), new Color(0.4f, 0.6f, 1f));

        // バトル専用合体エリア（3枚合体対応）
        var battleFusionArea = new GameObject("BattleFusionArea");
        battleFusionArea.transform.SetParent(panel.transform, false);
        var bfaRect = battleFusionArea.AddComponent<RectTransform>();
        bfaRect.anchorMin = new Vector2(0.1f, 0.19f);
        bfaRect.anchorMax = new Vector2(0.65f, 0.34f);
        bfaRect.offsetMin = Vector2.zero;
        bfaRect.offsetMax = Vector2.zero;
        var bfaImage = battleFusionArea.AddComponent<Image>();
        bfaImage.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

        var bfaTitle = CreateText(battleFusionArea.transform, "BFTitle", "合体エリア\n(ここにドロップ)", 14,
            new Vector2(0.02f, 0.6f), new Vector2(0.25f, 0.9f), new Color(0.7f, 0.7f, 0.8f));

        var bfaSlotContainer = new GameObject("SlotContainer");
        bfaSlotContainer.transform.SetParent(battleFusionArea.transform, false);
        var bfaSlotRect = bfaSlotContainer.AddComponent<RectTransform>();
        bfaSlotRect.anchorMin = new Vector2(0.28f, 0.05f);
        bfaSlotRect.anchorMax = new Vector2(0.85f, 0.95f);
        bfaSlotRect.offsetMin = Vector2.zero;
        bfaSlotRect.offsetMax = Vector2.zero;
        var bfaHlg = bfaSlotContainer.AddComponent<HorizontalLayoutGroup>();
        bfaHlg.spacing = 10;
        bfaHlg.childAlignment = TextAnchor.MiddleCenter;
        bfaHlg.childControlWidth = false;
        bfaHlg.childControlHeight = false;

        var bfaActionArea = new GameObject("ActionArea");
        bfaActionArea.transform.SetParent(battleFusionArea.transform, false);
        var bfaActionRect = bfaActionArea.AddComponent<RectTransform>();
        bfaActionRect.anchorMin = new Vector2(0.86f, 0.05f);
        bfaActionRect.anchorMax = new Vector2(0.98f, 0.95f);
        bfaActionRect.offsetMin = Vector2.zero;
        bfaActionRect.offsetMax = Vector2.zero;

        var bfaFuseBtn = CreateButton(bfaActionArea.transform, "FuseBtn", "合体", 14,
            new Vector2(0, 0.55f), new Vector2(1, 1), new Color(0.8f, 0.5f, 0.2f), null);
        var bfaClearBtn = CreateButton(bfaActionArea.transform, "ClearBtn", "戻す", 14,
            new Vector2(0, 0), new Vector2(1, 0.45f), new Color(0.4f, 0.4f, 0.5f), null);

        var bfaComponent = battleFusionArea.AddComponent<BattleFusionArea>();
        bfaComponent.slotContainer = bfaSlotRect;
        bfaComponent.fuseButton = bfaFuseBtn.GetComponent<Button>();
        bfaComponent.clearButton = bfaClearBtn.GetComponent<Button>();

        // プレイヤー情報は上で作成済みなので削除

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
    // 合体所（道場）パネル作成
    // ====================================
    private static GameObject CreateFusionPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "FusionPanel", new Color(0.1f, 0.08f, 0.15f, 0.95f));
        panel.SetActive(false);

        // タイトル
        CreateText(panel.transform, "FusionTitle", "⚔ 合体の道場 ⚔", 34,
            new Vector2(0.2f, 0.9f), new Vector2(0.8f, 0.99f), new Color(0.8f, 0.6f, 1f));

        // ゴールド表示
        var fusionGoldText = CreateText(panel.transform, "FusionGoldText", "所持金: 50G", 22,
            new Vector2(0.02f, 0.91f), new Vector2(0.2f, 0.99f), new Color(1f, 0.85f, 0.2f));

        // コスト表示
        var fusionCostText = CreateText(panel.transform, "FusionCostText", "合体コスト: 20G", 18,
            new Vector2(0.78f, 0.91f), new Vector2(0.98f, 0.99f), new Color(1f, 0.6f, 0.3f));

        // スロット1
        var slot1Bg = CreateUIPanel(panel.transform, "Slot1", new Color(0.2f, 0.2f, 0.35f),
            new Vector2(0.2f, 0.6f), new Vector2(0.35f, 0.85f));
        var slot1Text = CreateText(slot1Bg.transform, "Slot1Text", "?", 54,
            new Vector2(0, 0), new Vector2(1, 1), Color.white);

        CreateText(panel.transform, "PlusText", "+", 40,
            new Vector2(0.38f, 0.65f), new Vector2(0.45f, 0.8f), Color.white);

        // スロット2
        var slot2Bg = CreateUIPanel(panel.transform, "Slot2", new Color(0.2f, 0.2f, 0.35f),
            new Vector2(0.48f, 0.6f), new Vector2(0.63f, 0.85f));
        var slot2Text = CreateText(slot2Bg.transform, "Slot2Text", "?", 54,
            new Vector2(0, 0), new Vector2(1, 1), Color.white);

        CreateText(panel.transform, "EqualsText", "＝", 40,
            new Vector2(0.65f, 0.65f), new Vector2(0.72f, 0.8f), Color.white);

        // 結果スロット
        var resultBg = CreateUIPanel(panel.transform, "ResultSlot", new Color(0.35f, 0.25f, 0.45f),
            new Vector2(0.73f, 0.6f), new Vector2(0.88f, 0.85f));
        var resultText = CreateText(resultBg.transform, "ResultText", "?", 54,
            new Vector2(0, 0.3f), new Vector2(1, 1), new Color(1f, 0.9f, 0.4f));
        var resultDescText = CreateText(resultBg.transform, "ResultDescText", "カードを2枚選択", 14,
            new Vector2(0, 0), new Vector2(1, 0.35f), new Color(0.8f, 0.8f, 0.8f));

        // ステータス
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

        var fuseBtn = CreateButton(panel.transform, "FuseButton", "合体！", 24,
            new Vector2(0.3f, 0.02f), new Vector2(0.52f, 0.11f),
            new Color(0.6f, 0.3f, 0.8f), null);

        var clearBtn = CreateButton(panel.transform, "ClearButton", "クリア", 20,
            new Vector2(0.54f, 0.02f), new Vector2(0.72f, 0.11f),
            new Color(0.5f, 0.5f, 0.5f), null);

        var backBtn = CreateButton(panel.transform, "BackButton", "戻る", 20,
            new Vector2(0.74f, 0.02f), new Vector2(0.92f, 0.11f),
            new Color(0.4f, 0.4f, 0.5f), null);

        // FusionUI コンポーネント
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
        fusionUI.goldText = fusionGoldText;
        fusionUI.costText = fusionCostText;

        return panel;
    }

    // ====================================
    // ショップパネル作成
    // ====================================
    private static GameObject CreateShopPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "ShopPanel", new Color(0.08f, 0.12f, 0.08f, 0.95f));
        panel.SetActive(false);

        var titleText = CreateText(panel.transform, "ShopTitle", "🏪 商店 🏪", 34,
            new Vector2(0.25f, 0.88f), new Vector2(0.75f, 0.98f), new Color(0.4f, 1f, 0.5f));

        var shopGoldText = CreateText(panel.transform, "ShopGoldText", "所持金: 50G", 24,
            new Vector2(0.02f, 0.88f), new Vector2(0.25f, 0.98f), new Color(1f, 0.85f, 0.2f));

        var shopStatusText = CreateText(panel.transform, "ShopStatusText", "カードを選んで購入しよう", 20,
            new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.87f), new Color(0.8f, 0.9f, 0.8f));

        var cardArea = new GameObject("ShopCardArea");
        cardArea.transform.SetParent(panel.transform, false);
        var cardRect = cardArea.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.05f, 0.15f);
        cardRect.anchorMax = new Vector2(0.95f, 0.76f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        var hlg = cardArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var shopBackBtn = CreateButton(panel.transform, "ShopBackButton", "戻る", 22,
            new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.12f),
            new Color(0.4f, 0.5f, 0.4f), null);

        var shopUI = panel.AddComponent<ShopUI>();
        shopUI.cardListArea = cardRect;
        shopUI.goldText = shopGoldText;
        shopUI.titleText = titleText;
        shopUI.statusText = shopStatusText;
        shopUI.backButton = shopBackBtn.GetComponent<Button>();

        return panel;
    }

    // ====================================
    // 道場パネル（山札編集画面）作成
    // ====================================
    private static GameObject CreateDojoPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "DojoPanel", new Color(0.15f, 0.12f, 0.1f, 0.98f));

        // タイトル
        var titleText = CreateText(panel.transform, "Title", "⛩ 道場 ⛩", 50, new Vector2(0, 0.85f), new Vector2(1, 1), TextAlignmentOptions.Center);
        
        // --- モード切替ボタンエリア ---
        var modeArea = new GameObject("ModeArea");
        modeArea.transform.SetParent(panel.transform, false);
        var modeRect = modeArea.AddComponent<RectTransform>();
        modeRect.anchorMin = new Vector2(0.2f, 0.78f);
        modeRect.anchorMax = new Vector2(0.8f, 0.85f);
        modeRect.offsetMin = Vector2.zero;
        modeRect.offsetMax = Vector2.zero;

        // Gridで横並び
        var modeGrid = modeArea.AddComponent<HorizontalLayoutGroup>();
        modeGrid.childAlignment = TextAnchor.MiddleCenter;
        modeGrid.spacing = 20;
        modeGrid.childControlWidth = false;
        modeGrid.childControlHeight = false;

        // 追放ボタン
        var removeBtn = CreateSimpleButton(modeArea.transform, "RemoveModeBtn", "追放モード", new Vector2(0,0), new Vector2(0,0));
        var removeRect = removeBtn.GetComponent<RectTransform>();
        removeRect.sizeDelta = new Vector2(160, 40);
        
        // 鍛錬ボタン
        var enhanceBtn = CreateSimpleButton(modeArea.transform, "EnhanceModeBtn", "鍛錬モード (15G)", new Vector2(0,0), new Vector2(0,0));
        var enhanceRect = enhanceBtn.GetComponent<RectTransform>();
        enhanceRect.sizeDelta = new Vector2(160, 40);

        // 状態テキスト
        var statusText = CreateText(panel.transform, "Status", "精神統一…", 28, new Vector2(0, 0.65f), new Vector2(1, 0.75f), TextAlignmentOptions.Center);

        // デッキ枚数
        var deckCountText = CreateText(panel.transform, "DojoDeckCount", "山札: 10枚", 20, new Vector2(0.05f, 0.9f), new Vector2(0.3f, 0.95f), TextAlignmentOptions.Left);

        // スクロールビュー（カード一覧）
        var scrollView = new GameObject("CardScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        var svRect = scrollView.AddComponent<RectTransform>();
        svRect.anchorMin = new Vector2(0.1f, 0.15f);
        svRect.anchorMax = new Vector2(0.9f, 0.62f); // 少し狭める
        svRect.offsetMin = Vector2.zero;
        svRect.offsetMax = Vector2.zero;
        
        var scrollRect = scrollView.AddComponent<ScrollRect>();
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        var vpMask = viewport.AddComponent<Image>(); // Mask用
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 300); // 動的に変わるが初期値

        // GridLayout
        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100, 140);
        grid.spacing = new Vector2(15, 15);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.childAlignment = TextAnchor.UpperCenter;
        
        // Content Size Fitter
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = vpRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20;

        // 戻るボタン
        var backBtn = CreateButton(panel.transform, "BackBtn", "戻る", 22, new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.12f), new Color(0.5f, 0.4f, 0.3f), null);

        // 確認ダイアログ
        var confirmPanel = CreatePanel(panel.transform, "ConfirmPanel", new Color(0, 0, 0, 0.9f));
        confirmPanel.SetActive(false);
        var confirmText = CreateText(confirmPanel.transform, "ConfirmText", "本当に追放しますか？", 32, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.8f), TextAlignmentOptions.Center);
        var yesBtn = CreateButton(confirmPanel.transform, "YesBtn", "はい", 22, new Vector2(0.2f, 0.2f), new Vector2(0.4f, 0.3f), new Color(0.8f, 0.2f, 0.2f), null);
        var noBtn = CreateButton(confirmPanel.transform, "NoBtn", "いいえ", 22, new Vector2(0.6f, 0.2f), new Vector2(0.8f, 0.3f), new Color(0.4f, 0.4f, 0.5f), null);

        // コンポーネント設定
        var deckEditUI = panel.AddComponent<DeckEditUI>();
        deckEditUI.cardListArea = contentRect;
        deckEditUI.titleText = titleText;
        deckEditUI.statusText = statusText;
        deckEditUI.deckCountText = deckCountText;
        deckEditUI.backButton = backBtn.GetComponent<Button>();
        deckEditUI.removeModeButton = removeBtn.GetComponent<Button>();
        deckEditUI.enhanceModeButton = enhanceBtn.GetComponent<Button>();
        
        deckEditUI.confirmPanel = confirmPanel;
        deckEditUI.confirmText = confirmText;
        deckEditUI.confirmYesButton = yesBtn.GetComponent<Button>();
        deckEditUI.confirmNoButton = noBtn.GetComponent<Button>();

        // 非表示初期化
        panel.SetActive(false);
        return panel;
    }

    // ====================================
    // デッキ確認パネル作成
    // ====================================
    private static GameObject CreateDeckViewerPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "DeckViewerPanel", new Color(0.1f, 0.15f, 0.1f, 0.98f));

        var titleText = CreateText(panel.transform, "Title", "🎴 山札確認 🎴", 40, new Vector2(0, 0.85f), new Vector2(1, 0.95f), TextAlignmentOptions.Center);
        var closeBtn = CreateButton(panel.transform, "CloseBtn", "閉じる", 22, new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.12f), new Color(0.5f, 0.3f, 0.3f), null);

        var scrollView = new GameObject("CardScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        var svRect = scrollView.AddComponent<RectTransform>();
        svRect.anchorMin = new Vector2(0.1f, 0.15f);
        svRect.anchorMax = new Vector2(0.9f, 0.8f);
        svRect.offsetMin = Vector2.zero;
        svRect.offsetMax = Vector2.zero;
        
        var scrollRect = scrollView.AddComponent<ScrollRect>();
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        var vpMask = viewport.AddComponent<Image>();
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 300);

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(90, 130);
        grid.spacing = new Vector2(15, 15);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.childAlignment = TextAnchor.UpperCenter;
        
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = vpRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20;

        var ui = panel.AddComponent<DeckViewerUI>();
        ui.cardListArea = contentRect;
        ui.closeButton = closeBtn.GetComponent<Button>();

        panel.SetActive(false);
        return panel;
    }

    // ====================================
    // 漢字図鑑パネル作成
    // ====================================
    private static GameObject CreateEncyclopediaPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "EncyclopediaPanel", new Color(0.1f, 0.1f, 0.15f, 0.98f));

        var titleText = CreateText(panel.transform, "Title", "📖 漢字図鑑 📖", 40, new Vector2(0, 0.85f), new Vector2(1, 0.95f), TextAlignmentOptions.Center);
        var statusText = CreateText(panel.transform, "Status", "漢字収集率: 0 / 0", 24, new Vector2(0.05f, 0.88f), new Vector2(0.3f, 0.95f), TextAlignmentOptions.Left);
        var closeBtn = CreateButton(panel.transform, "CloseBtn", "閉じる", 22, new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.12f), new Color(0.5f, 0.3f, 0.3f), null);

        var scrollView = new GameObject("CardScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        var svRect = scrollView.AddComponent<RectTransform>();
        svRect.anchorMin = new Vector2(0.1f, 0.15f);
        svRect.anchorMax = new Vector2(0.9f, 0.8f);
        svRect.offsetMin = Vector2.zero;
        svRect.offsetMax = Vector2.zero;
        
        var scrollRect = scrollView.AddComponent<ScrollRect>();
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        var vpMask = viewport.AddComponent<Image>();
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 300);

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(90, 130);
        grid.spacing = new Vector2(15, 15);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.childAlignment = TextAnchor.UpperCenter;
        
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = vpRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20;

        var ui = panel.AddComponent<KanjiEncyclopediaUI>();
        ui.cardListArea = contentRect;
        ui.closeButton = closeBtn.GetComponent<Button>();
        ui.statusText = statusText;

        panel.SetActive(false);
        return panel;
    }

    // ====================================
    // 合体結果選択パネル作成
    // ====================================
    private static GameObject CreateFusionSelectionPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "FusionSelectionPanel", new Color(0.1f, 0.1f, 0.1f, 0.95f));

        var titleText = CreateText(panel.transform, "Title", "合体結果を選択", 36, new Vector2(0, 0.8f), new Vector2(1, 0.95f), TextAlignmentOptions.Center);
        
        var content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.1f, 0.2f);
        contentRect.anchorMax = new Vector2(0.9f, 0.7f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        var ui = panel.AddComponent<FusionSelectionUI>();
        ui.cardListArea = content.transform;

        panel.SetActive(false);
        return panel;
    }

    // ====================================
    // 参照の割り当て
    // ====================================
    private static void AssignReferences(GameManager gm, BattleManager bm, MapManager mm, FieldManager fm, KanjiFusionEngine fe,
        GameObject fieldPanel, GameObject mapPanel, GameObject battlePanel, GameObject fusionPanel, GameObject shopPanel, GameObject dojoPanel)
    {
        gm.battleManager = bm;
        gm.mapManager = mm;
        gm.fieldManager = fm;
        gm.fusionEngine = fe;
        gm.fieldPanel = fieldPanel;
        gm.mapPanel = mapPanel;
        gm.battlePanel = battlePanel;
        gm.fusionPanel = fusionPanel;
        gm.shopPanel = shopPanel;
        gm.dojoPanel = dojoPanel;
        
        var canvasTransform = fieldPanel.transform.parent;
        gm.fusionSelectionUI = canvasTransform.Find("FusionSelectionPanel")?.GetComponent<FusionSelectionUI>();

        // ランタイムUIコンポーネントにAppFont参照を割り当て
        if (appFont != null)
        {
            mm.appFont = appFont;
            fm.appFont = appFont;

            var battleUI = battlePanel.GetComponent<BattleUI>();
            if (battleUI != null)
            {
                battleUI.appFont = appFont;
                bm.battleUI = battleUI;
            }

            var bfa = battlePanel.transform.Find("BattleFusionArea")?.GetComponent<BattleFusionArea>();
            if (bfa != null) bfa.appFont = appFont;

            var fusionUI = fusionPanel.GetComponent<FusionUI>();
            if (fusionUI != null) fusionUI.appFont = appFont;

            var shopUI = shopPanel.GetComponent<ShopUI>();
            if (shopUI != null) shopUI.appFont = appFont;

            var deckEditUI = dojoPanel.GetComponent<DeckEditUI>();
            if (deckEditUI != null) deckEditUI.appFont = appFont;

            var deckViewer = canvasTransform.Find("DeckViewerPanel")?.GetComponent<DeckViewerUI>();
            if (deckViewer != null) deckViewer.appFont = appFont;
            
            var encycViewer = canvasTransform.Find("EncyclopediaPanel")?.GetComponent<KanjiEncyclopediaUI>();
            if (encycViewer != null) encycViewer.appFont = appFont;
            
            if (gm.fusionSelectionUI != null) gm.fusionSelectionUI.appFont = appFont;
        }
        else
        {
            var battleUI = battlePanel.GetComponent<BattleUI>();
            if (battleUI != null) bm.battleUI = battleUI;
        }

        EditorUtility.SetDirty(gm);
        EditorUtility.SetDirty(bm);
        EditorUtility.SetDirty(mm);
        EditorUtility.SetDirty(fm);
        EditorUtility.SetDirty(fe);

        Debug.Log("  全参照の割り当て完了");
    }

    // ====================================
    // 初期デッキ設定
    // ====================================
    private static void SetupInitialInventory(GameManager gm, Dictionary<string, KanjiCardData> cards)
    {
        gm.inventory.Clear();

        // 初期インベントリ: 基本漢字 各2枚ずつ
        string[] basicKanjis = { "木", "日", "月", "力", "火", "田", "口", "十", "大", "土", "人", "目", "白", "公", "民" };
        
        foreach (string k in basicKanjis)
        {
            AddCardsToInventory(gm, cards, k, 2);
        }

        EditorUtility.SetDirty(gm);
        Debug.Log($"  初期インベントリ設定完了: {gm.inventory.Count}枚");
    }

    private static void AddCardsToInventory(GameManager gm, Dictionary<string, KanjiCardData> cards, string kanji, int count)
    {
        if (!cards.ContainsKey(kanji)) return;
        for (int i = 0; i < count; i++)
        {
            gm.inventory.Add(cards[kanji]);
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

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment)
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
        tmp.color = Color.white;
        tmp.alignment = alignment;
        if (appFont != null) tmp.font = appFont;

        return tmp;
    }

    private static GameObject CreateSimpleButton(Transform parent, string name, string label, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        // LayoutGroup管理下を想定する場合もあるが、RectTransformは必要
        var rect = go.AddComponent<RectTransform>();
        // sizeが0ならLayoutで制御される想定
        if (size != Vector2.zero) rect.sizeDelta = size;
        
        var image = go.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.35f);

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.45f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.25f);
        button.colors = colors;

        // ラベル
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (appFont != null) tmp.font = appFont;

        return go;
    }


}
