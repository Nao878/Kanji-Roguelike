using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 包括的ゲーム仕様書（Markdown）自動生成ツール
/// 静的な設計仕様 + 動的なデータリストを1つのドキュメントに統合
/// </summary>
public class GameDesignDocGenerator : EditorWindow
{
    private const string OUTPUT_PATH = "Assets/GameDesignDoc.md";

    [MenuItem("Tools/Update Game Design Doc")]
    public static void Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# 📜 漢字ローグライク — Game Design Document");
        sb.AppendLine($"> Last Updated: {System.DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // === Section 1: ゲーム概要 ===
        AppendGameOverview(sb);

        // === Section 2: 画面遷移とマップ仕様 ===
        AppendScreenFlow(sb);

        // === Section 3: 戦闘システム ===
        AppendBattleSystem(sb);

        // === Section 4: スクリプト・アーキテクチャ ===
        AppendArchitecture(sb);

        // === Section 5: カードデータベース（動的） ===
        AppendCardList(sb);

        // === Section 6: 合体レシピ（動的） ===
        AppendFusionRecipes(sb);

        // === Section 7: ゲーム設定（動的） ===
        AppendGameSettings(sb);

        // ファイル書き込み
        File.WriteAllText(OUTPUT_PATH, sb.ToString());
        AssetDatabase.ImportAsset(OUTPUT_PATH);

        var asset = AssetDatabase.LoadAssetAtPath<Object>(OUTPUT_PATH);
        if (asset != null) EditorGUIUtility.PingObject(asset);

        Debug.Log($"[GameDesignDoc] 仕様書を更新しました: {OUTPUT_PATH}");
    }

    // ================================================
    // Section 1: ゲーム概要とビジュアル
    // ================================================
    private static void AppendGameOverview(StringBuilder sb)
    {
        sb.AppendLine("## 1. 🎮 ゲーム概要");
        sb.AppendLine();
        sb.AppendLine("### コンセプト");
        sb.AppendLine("「漢字ローグライク」は、漢字の成り立ち（部首の合体）をメカニクスの中心に据えた");
        sb.AppendLine("デッキ構築型ローグライクカードゲームです。");
        sb.AppendLine();
        sb.AppendLine("### コアループ");
        sb.AppendLine("```");
        sb.AppendLine("マップ進行 → 戦闘/イベント → 報酬(Gold) → デッキ強化(商店/道場/合体) → ボス討伐");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### ビジュアルデザイン");
        sb.AppendLine("- **背景**: 墨色(#1A1A1A)ベースの和風デザイン");
        sb.AppendLine("- **マップ**: Slay the Spire風のボトムアップ型ルートマップ、背景に地形漢字が散りばめられた和紙風");
        sb.AppendLine("- **カード**: 効果タイプごとに色分け（攻撃=赤、防御=青、回復=緑、バフ=黄、特殊=紫）");
        sb.AppendLine("- **演出**: 合体時の吸い寄せ＆閃光、ダメージ時のシェイク＆ポップアップ（VFXManager管理）");
        sb.AppendLine();

        // 属性システム
        sb.AppendLine("### 属性（Element）システム");
        sb.AppendLine("各カードは以下の属性を持ちます：");
        sb.AppendLine();
        sb.AppendLine("| Element | 和名 | 代表カード |");
        sb.AppendLine("|---------|------|-----------|");
        sb.AppendLine("| None | 無属性 | 口, 力, 人 等 |");
        sb.AppendLine("| Wood | 木 | 木, 林, 森, 休, 柏, 松 |");
        sb.AppendLine("| Fire | 火 | 火, 炎, 畑 |");
        sb.AppendLine("| Earth | 土 | 田, 土, 圭 |");
        sb.AppendLine("| Sun | 日 | 日, 明, 早, 東, 晶 |");
        sb.AppendLine("| Moon | 月 | 月 |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ================================================
    // Section 2: 画面遷移とマップ仕様
    // ================================================
    private static void AppendScreenFlow(StringBuilder sb)
    {
        sb.AppendLine("## 2. 🗺️ 画面遷移とマップ仕様");
        sb.AppendLine();
        sb.AppendLine("### GameState（画面状態一覧）");
        sb.AppendLine("| State | 画面 | 説明 |");
        sb.AppendLine("|-------|------|------|");
        sb.AppendLine("| `Map` | マップ画面 | ルート選択、次のノードへ進行 |");
        sb.AppendLine("| `Battle` | 戦闘画面 | カードを使って敵と戦闘 |");
        sb.AppendLine("| `Fusion` | 合体所 | カード2枚を合体して進化カードを獲得 |");
        sb.AppendLine("| `Shop` | 商店 | ゴールドでカードを購入 |");
        sb.AppendLine("| `Dojo` | 道場 | カードの追放/鍛錬 |");
        sb.AppendLine("| `Event` | イベント | ランダムイベント |");
        sb.AppendLine("| `GameOver` | ゲームオーバー | リトライ選択 |");
        sb.AppendLine();

        sb.AppendLine("### マップ構造（Slay the Spire型）");
        sb.AppendLine("- **レイヤー数**: 7層（ボトム→トップ）");
        sb.AppendLine("- **各層ノード数**: 2〜3ノード（ランダム分岐）");
        sb.AppendLine("- **最終層**: 大将（Boss）固定");
        sb.AppendLine("- **選択可能ノード**: 点滅アニメーションで強調表示");
        sb.AppendLine("- **大将ノード**: 1.5倍サイズで表示");
        sb.AppendLine();

        sb.AppendLine("### ノードタイプ一覧");
        sb.AppendLine("| 漢字 | タイプ | 色 | 説明 |");
        sb.AppendLine("|------|--------|-----|------|");
        sb.AppendLine("| 戦闘 | `Battle` | 青 | 通常敵との戦闘。勝利でGold獲得 |");
        sb.AppendLine("| 強敵 | `Elite` | 橙 | 強力な敵。報酬が多い |");
        sb.AppendLine("| 商店 | `Shop` | 緑 | Goldを消費してカードを購入。3枚候補を表示 |");
        sb.AppendLine("| 事件 | `Event` | 紫 | ランダムイベント |");
        sb.AppendLine("| 大将 | `Boss` | 赤 | 各マップ最終ノード。討伐で次エリアへ |");
        sb.AppendLine("| 道場 | `Dojo` | 褐 | カードの追放(10G)または鍛錬(15G, 攻撃/防御+2) |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ================================================
    // Section 3: 戦闘システムと基本操作
    // ================================================
    private static void AppendBattleSystem(StringBuilder sb)
    {
        sb.AppendLine("## 3. ⚔️ 戦闘システムと操作仕様");
        sb.AppendLine();

        sb.AppendLine("### 基本パラメータ");

        // 動的にGameManager設定を取得
        GameObject gmGo = GameObject.Find("GameManager");
        if (gmGo != null)
        {
            GameManager gm = gmGo.GetComponent<GameManager>();
            if (gm != null)
            {
                sb.AppendLine($"- **初期HP**: {gm.playerMaxHP}");
                sb.AppendLine($"- **初期マナ**: {gm.playerStartMana}/ターン");
                sb.AppendLine($"- **手札上限**: {gm.initialHandSize}枚");
                sb.AppendLine($"- **初期Gold**: {gm.startGold}G");
                sb.AppendLine($"- **合体コスト**: {gm.fusionCost}G（道場での合体時）");
            }
        }
        else
        {
            sb.AppendLine("- *(Setup後に実行すると実際の値が表示されます)*");
        }
        sb.AppendLine();

        sb.AppendLine("### ターン進行");
        sb.AppendLine("1. **プレイヤーターン**: マナを消費してカードを使用");
        sb.AppendLine("2. **ターン終了**: 「終」ボタンを押す or マナが尽きたら任意で終了");
        sb.AppendLine("3. **敵ターン**: 敵が自動で攻撃（attackPower分のダメージ）");
        sb.AppendLine("4. **スタン状態**: 敵がスタン中は敵ターンをスキップし、スタンを解除");
        sb.AppendLine("5. **繰り返し**: 敵HPが0になるか、プレイヤーHPが0になるまで");
        sb.AppendLine();

        sb.AppendLine("### カード操作（Drag & Drop）");
        sb.AppendLine("カードは `CardController` で制御されるドラッグ＆ドロップ方式です。");
        sb.AppendLine();
        sb.AppendLine("| 操作 | ドロップ先 | 効果 |");
        sb.AppendLine("|------|-----------|------|");
        sb.AppendLine("| ドラッグ→敵 | Tag `Enemy` | カード効果発動（攻撃/回復/バフ等） |");
        sb.AppendLine("| ドラッグ→カード | Tag `Card` | 合体判定→成功で進化カード生成 |");
        sb.AppendLine("| ドラッグ→何もない | - | 手札に戻る |");
        sb.AppendLine();

        sb.AppendLine("### 合体判定フロー（バトル中）");
        sb.AppendLine("1. カードAをカードBにドラッグ＆ドロップ");
        sb.AppendLine("2. `GameManager.FindFusionResult(A.cardId, B.cardId)` でレシピ検索");
        sb.AppendLine("3. レシピが存在 → VFX演出（吸い寄せ→閃光→消滅→新カード出現）");
        sb.AppendLine("4. 手札からA,Bを除去し、結果カードを手札に追加");
        sb.AppendLine("5. レシピ未存在 → カードが元の位置に戻る");
        sb.AppendLine();

        sb.AppendLine("### ホバー時プレビュー");
        sb.AppendLine("- ドラッグ中のカードが別のカードに重なると、合体可能なら黄色発光＋結果漢字を表示");
        sb.AppendLine("- 通常ホバー時はカードが10px上に浮き上がる");
        sb.AppendLine();

        sb.AppendLine("### 効果タイプ別ダメージ計算");
        sb.AppendLine("| EffectType | 計算式 | 備考 |");
        sb.AppendLine("|-----------|--------|------|");
        sb.AppendLine("| `Attack` | `effectValue + attackModifier + playerAttackBuff` | 単体ダメージ |");
        sb.AppendLine("| `AttackAll` | 同上 | 全体ダメージ（将来の複数敵対応） |");
        sb.AppendLine("| `Defense` | `effectValue + defenseModifier` | 防御バフ加算 |");
        sb.AppendLine("| `Heal` | `effectValue` | HP回復（上限まで） |");
        sb.AppendLine("| `Buff` | `effectValue` | 攻撃バフ加算 |");
        sb.AppendLine("| `Draw` | `effectValue` 枚 | デッキからドロー |");
        sb.AppendLine("| `Stun` | - | 敵の次ターンをスキップ |");
        sb.AppendLine("| `Special` | `effectValue`ダメージ + `effectValue`回復 | 複合効果 |");
        sb.AppendLine();

        sb.AppendLine("### VFX演出（VFXManager）");
        sb.AppendLine("| 演出 | タイミング | 詳細 |");
        sb.AppendLine("|------|-----------|------|");
        sb.AppendLine("| 合体シーケンス | 合体成功時 | 2枚が中央に吸い寄せ→縮小→閃光→新カードSpawn |");
        sb.AppendLine("| ダメージ | 攻撃ヒット時 | 敵画像シェイク + 赤フラッシュ + ダメージ数値ポップ |");
        sb.AppendLine("| Spawn | 新カード出現時 | AnimationCurve適用のボヨヨン出現 |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ================================================
    // Section 4: アーキテクチャ
    // ================================================
    private static void AppendArchitecture(StringBuilder sb)
    {
        sb.AppendLine("## 4. 🏗️ スクリプト・アーキテクチャ");
        sb.AppendLine();

        // プロジェクト内スクリプトを動的にスキャン
        string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" });
        
        // カテゴリごとに分類
        var coreScripts = new List<(string name, string path)>();
        var dataScripts = new List<(string name, string path)>();
        var uiScripts = new List<(string name, string path)>();
        var editorScripts = new List<(string name, string path)>();

        foreach (var guid in scriptGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            if (path.Contains("/Core/")) coreScripts.Add((fileName, path));
            else if (path.Contains("/Data/")) dataScripts.Add((fileName, path));
            else if (path.Contains("/UI/")) uiScripts.Add((fileName, path));
            else if (path.Contains("/Editor/")) editorScripts.Add((fileName, path));
        }

        // Core
        sb.AppendLine("### Core（コアロジック）");
        sb.AppendLine("| Script | 役割 |");
        sb.AppendLine("|--------|------|");
        foreach (var s in coreScripts)
        {
            sb.AppendLine($"| `{s.name}` | {GetScriptDescription(s.name)} |");
        }
        sb.AppendLine();

        // Data
        sb.AppendLine("### Data（データ構造/ScriptableObject）");
        sb.AppendLine("| Script | 役割 |");
        sb.AppendLine("|--------|------|");
        foreach (var s in dataScripts)
        {
            sb.AppendLine($"| `{s.name}` | {GetScriptDescription(s.name)} |");
        }
        sb.AppendLine();

        // UI
        sb.AppendLine("### UI（画面・インタラクション）");
        sb.AppendLine("| Script | 役割 |");
        sb.AppendLine("|--------|------|");
        foreach (var s in uiScripts)
        {
            sb.AppendLine($"| `{s.name}` | {GetScriptDescription(s.name)} |");
        }
        sb.AppendLine();

        // Editor
        sb.AppendLine("### Editor（開発ツール）");
        sb.AppendLine("| Script | 役割 |");
        sb.AppendLine("|--------|------|");
        foreach (var s in editorScripts)
        {
            sb.AppendLine($"| `{s.name}` | {GetScriptDescription(s.name)} |");
        }
        sb.AppendLine();

        // シングルトン一覧
        sb.AppendLine("### シングルトン管理");
        sb.AppendLine("| Class | アクセス | 永続化 |");
        sb.AppendLine("|-------|---------|--------|");
        sb.AppendLine("| `GameManager` | `GameManager.Instance` | Scene内 |");
        sb.AppendLine("| `BattleManager` | `BattleManager.Instance` | Scene内 |");
        sb.AppendLine("| `VFXManager` | `VFXManager.Instance` | Scene内 |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    /// <summary>
    /// スクリプト名から説明文を返す（ハードコーディング辞書）
    /// </summary>
    private static string GetScriptDescription(string scriptName)
    {
        switch (scriptName)
        {
            // Core
            case "GameManager": return "ゲーム全体の状態管理（HP/マナ/Gold/デッキ/手札/合成辞書）。シングルトン";
            case "BattleManager": return "戦闘ロジック（カード効果適用、敵ターン、勝敗判定、スタン管理）。シングルトン";
            case "MapManager": return "Slay the Spire風ルートマップ生成/表示/ノードクリック処理。和風デザイン";
            case "KanjiFusionEngine": return "合体ロジックのラッパー（FusionDatabase経由でレシピ検索）";
            case "VFXManager": return "全VFX演出管理（合体・ダメージ・Spawn）。Coroutine + AnimationCurve。シングルトン";

            // Data
            case "KanjiCardData": return "漢字カードのデータ構造（ScriptableObject）。属性(Element)・効果タイプ・鍛錬modifier含む";
            case "KanjiFusionRecipe": return "合成レシピ定義（2枚/3枚対応）。素材→結果のマッピング";
            case "KanjiFusionDatabase": return "全レシピのデータベース。複数結果対応キャッシュ＋分解用逆引き搭載";
            case "EnemyData": return "敵キャラクターのデータ構造（HP/攻撃力/名前/説明）";

            // UI
            case "BattleUI": return "戦闘画面UI管理。CardController生成/手札更新/ステータス表示/ターン終了ボタン";
            case "CardController": return "カードのドラッグ＆ドロップ制御。敵へ攻撃、カードへ合体、ホバープレビュー実装";
            case "CardUI": return "カードUI要素保持（商店/道場用の軽量版）";
            case "FusionUI": return "合体所UI。2スロット選択→結果プレビュー→Gold消費で合体実行";
            case "ShopUI": return "商店UI。ランダム3枚表示→Gold消費で購入→売切表示";
            case "DeckEditUI": return "道場UI。「追放」(10G)でカード除去、「鍛錬」(15G)でAttack/Defense+2永続強化";

            // Editor
            case "ProjectSetupTool": return "ワンクリックでシーン全体を構築（カード/レシピ/敵/UI/VFX生成）";
            case "GameDesignDocGenerator": return "本ドキュメントを自動生成するエディタツール";

            default: return "(説明なし)";
        }
    }

    // ================================================
    // Section 5: 全カードリスト（動的）
    // ================================================
    private static void AppendCardList(StringBuilder sb)
    {
        sb.AppendLine("## 5. 🎴 全カード一覧（Card Database）");
        sb.AppendLine();

        var cards = LoadAllCards();

        // 基礎カード
        sb.AppendLine("### 基礎カード（素材）");
        sb.AppendLine("| ID | 漢字 | 属性 | コスト | タイプ | 効果値 | 説明 |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var c in cards)
        {
            if (c.isFusionResult) continue;
            string elem = c.element != CardElement.None ? c.element.ToString() : "-";
            string desc = c.description != null ? c.description.Replace("\n", " ") : "";
            sb.AppendLine($"| {c.cardId} | **{c.kanji}** | {elem} | {c.cost} | {c.effectType} | {c.effectValue} | {desc} |");
        }
        sb.AppendLine();

        // 合体結果カード
        sb.AppendLine("### 合体カード（Fusion Result）");
        sb.AppendLine("| ID | 漢字 | 属性 | コスト | タイプ | 効果値 | 説明 |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var c in cards)
        {
            if (!c.isFusionResult) continue;
            string elem = c.element != CardElement.None ? c.element.ToString() : "-";
            string desc = c.description != null ? c.description.Replace("\n", " ") : "";
            sb.AppendLine($"| {c.cardId} | **{c.kanji}** | {elem} | {c.cost} | {c.effectType} | {c.effectValue} | {desc} |");
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ================================================
    // Section 6: 合体レシピ（動的）
    // ================================================
    private static void AppendFusionRecipes(StringBuilder sb)
    {
        sb.AppendLine("## 6. 🔥 合体レシピ一覧（Fusion Recipes）");
        sb.AppendLine();

        string[] guids = AssetDatabase.FindAssets("t:KanjiFusionRecipe");
        var recipes2 = new List<KanjiFusionRecipe>();
        var recipes3 = new List<KanjiFusionRecipe>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            KanjiFusionRecipe recipe = AssetDatabase.LoadAssetAtPath<KanjiFusionRecipe>(path);
            if (recipe == null || recipe.material1 == null || recipe.material2 == null || recipe.result == null) continue;
            
            if (recipe.material3 != null) recipes3.Add(recipe);
            else recipes2.Add(recipe);
        }

        // 2枚合体
        sb.AppendLine("### 2枚合体");
        sb.AppendLine("| 素材A | + | 素材B | = | 結果 | 効果 |");
        sb.AppendLine("|-------|---|-------|---|------|------|");
        recipes2.Sort((a, b) => string.Compare(a.material1.kanji, b.material1.kanji));
        foreach (var r in recipes2)
        {
            string desc = r.result.description != null ? r.result.description : "";
            sb.AppendLine($"| {r.material1.kanji} | + | {r.material2.kanji} | → | **{r.result.kanji}** | {desc} |");
        }
        sb.AppendLine();

        // 3枚合体
        if (recipes3.Count > 0)
        {
            sb.AppendLine("### 3枚合体");
            sb.AppendLine("| 素材A | + | 素材B | + | 素材C | = | 結果 | 効果 |");
            sb.AppendLine("|-------|---|-------|---|-------|---|------|------|");
            foreach (var r in recipes3)
            {
                string desc = r.result.description != null ? r.result.description : "";
                sb.AppendLine($"| {r.material1.kanji} | + | {r.material2.kanji} | + | {r.material3.kanji} | → | **{r.result.kanji}** | {desc} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    // ================================================
    // Section 7: ゲーム設定（動的）
    // ================================================
    private static void AppendGameSettings(StringBuilder sb)
    {
        sb.AppendLine("## 7. ⚙️ ゲーム設定");
        sb.AppendLine();

        // 初期デッキ
        sb.AppendLine("### 初期デッキ構成");
        GameObject gmGo = GameObject.Find("GameManager");
        if (gmGo != null)
        {
            GameManager gm = gmGo.GetComponent<GameManager>();
            if (gm != null && gm.deck != null && gm.deck.Count > 0)
            {
                var deckCounts = new Dictionary<string, int>();
                foreach (var card in gm.deck)
                {
                    if (card == null) continue;
                    if (!deckCounts.ContainsKey(card.kanji)) deckCounts[card.kanji] = 0;
                    deckCounts[card.kanji]++;
                }
                sb.AppendLine("| カード | 枚数 |");
                sb.AppendLine("|--------|------|");
                foreach (var kvp in deckCounts)
                {
                    sb.AppendLine($"| {kvp.Key} | x{kvp.Value} |");
                }
            }
            else
            {
                sb.AppendLine("*(Setup後に再実行してください)*");
            }
        }
        else
        {
            sb.AppendLine("*(Setup後に再実行してください)*");
        }
        sb.AppendLine();

        // 敵データ
        sb.AppendLine("### 敵キャラクター");
        string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyData");
        if (enemyGuids.Length > 0)
        {
            sb.AppendLine("| Name | Kanji | HP | ATK | Type |");
            sb.AppendLine("|------|-------|-----|-----|------|");
            foreach (string guid in enemyGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (enemy != null)
                {
                    sb.AppendLine($"| {enemy.enemyName} | {enemy.displayKanji} | {enemy.maxHP} | {enemy.attackPower} | {enemy.enemyType} |");
                }
            }
        }
        else
        {
            sb.AppendLine("*(敵データなし)*");
        }
        sb.AppendLine();

        // ショップ対象
        sb.AppendLine("### 商店対象カード");
        sb.AppendLine("> 基礎カード（非合体結果）がランダムに3枚表示されます。");
        var allCards = LoadAllCards();
        var shopList = new List<string>();
        foreach (var c in allCards)
        {
            if (!c.isFusionResult) shopList.Add(c.kanji);
        }
        if (shopList.Count > 0)
        {
            sb.AppendLine($"対象: {string.Join(", ", shopList)}");
        }
        sb.AppendLine();
    }

    // ================================================
    // ユーティリティ
    // ================================================
    private static List<KanjiCardData> LoadAllCards()
    {
        string[] guids = AssetDatabase.FindAssets("t:KanjiCardData");
        var cards = new List<KanjiCardData>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var card = AssetDatabase.LoadAssetAtPath<KanjiCardData>(path);
            if (card != null) cards.Add(card);
        }
        cards.Sort((a, b) => a.cardId.CompareTo(b.cardId));
        return cards;
    }
}
