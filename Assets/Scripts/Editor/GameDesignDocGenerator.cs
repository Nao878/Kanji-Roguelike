using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ゲーム仕様書（Markdown）自動生成ツール
/// </summary>
public class GameDesignDocGenerator : EditorWindow
{
    private const string OUTPUT_PATH = "Assets/GameDesignDoc.md";

    [MenuItem("Tools/Update Game Design Doc")]
    public static void Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# Kanji Roguelike Game Design Document");
        sb.AppendLine($"Last Updated: {System.DateTime.Now}");
        sb.AppendLine();

        // 1. 全カードリスト
        AppendCardList(sb);

        // 2. 全合体レシピ
        AppendFusionRecipes(sb);

        // 3. 初期デッキとショップ
        AppendDeckAndShopInfo(sb);

        // ファイル書き込み
        File.WriteAllText(OUTPUT_PATH, sb.ToString());
        AssetDatabase.ImportAsset(OUTPUT_PATH);
        
        var asset = AssetDatabase.LoadAssetAtPath<Object>(OUTPUT_PATH);
        if (asset != null) EditorGUIUtility.PingObject(asset);

        Debug.Log($"[GameDesignDoc] 仕様書を更新しました: {OUTPUT_PATH}");
    }

    private static void AppendCardList(StringBuilder sb)
    {
        sb.AppendLine("## 1. Card List (全カード一覧)");
        sb.AppendLine("| ID | Kanji | Element | Cost | Type | Value | Effect |");
        sb.AppendLine("|---|---|---|---|---|---|---|");

        var cards = LoadAllCards();

        foreach (KanjiCardData c in cards)
        {
            string effectText = c.description != null ? c.description.Replace("\n", " ") : "";
            string elemStr = c.element != CardElement.None ? c.element.ToString() : "-";
            sb.AppendLine($"| {c.cardId} | **{c.kanji}** | {elemStr} | {c.cost} | {c.effectType} | {c.effectValue} | {effectText} |");
        }
        sb.AppendLine();
    }

    private static void AppendFusionRecipes(StringBuilder sb)
    {
        sb.AppendLine("## 2. Fusion Recipes (合体レシピ)");
        sb.AppendLine("| Materials | Result | Description |");
        sb.AppendLine("|---|---|---|");

        string[] guids = AssetDatabase.FindAssets("t:KanjiFusionRecipe");
        List<KanjiFusionRecipe> recipes = new List<KanjiFusionRecipe>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            KanjiFusionRecipe recipe = AssetDatabase.LoadAssetAtPath<KanjiFusionRecipe>(path);
            if (recipe != null) recipes.Add(recipe);
        }

        recipes.Sort((a, b) =>
        {
            if (a.material1 == null) return -1;
            if (b.material1 == null) return 1;
            return string.Compare(a.material1.kanji, b.material1.kanji);
        });

        foreach (KanjiFusionRecipe r in recipes)
        {
            if (r.material1 == null || r.material2 == null || r.result == null) continue;
            // 3枚合体対応
            string materials = $"{r.material1.kanji} + {r.material2.kanji}";
            if (r.material3 != null) materials += $" + {r.material3.kanji}";
            string desc = r.result.description != null ? r.result.description : "";
            sb.AppendLine($"| {materials} | **{r.result.kanji}** | {desc} |");
        }
        sb.AppendLine();
    }

    private static void AppendDeckAndShopInfo(StringBuilder sb)
    {
        sb.AppendLine("## 3. Game Settings");

        // 初期デッキ
        sb.AppendLine("### Initial Deck (初期デッキ)");
        
        GameObject gmGo = GameObject.Find("GameManager");
        if (gmGo != null)
        {
            GameManager gm = gmGo.GetComponent<GameManager>();
            if (gm != null && gm.deck != null && gm.deck.Count > 0)
            {
                Dictionary<string, int> deckCounts = new Dictionary<string, int>();
                foreach (KanjiCardData card in gm.deck)
                {
                    if (card == null) continue;
                    if (!deckCounts.ContainsKey(card.kanji)) deckCounts[card.kanji] = 0;
                    deckCounts[card.kanji]++;
                }
                foreach (var kvp in deckCounts)
                {
                    sb.AppendLine($"- **{kvp.Key}** x{kvp.Value}");
                }
            }
            else
            {
                sb.AppendLine("- (デッキ情報を取得できませんでした)");
            }
        }
        else
        {
            sb.AppendLine("- (Setup直後に実行してください)");
        }
        sb.AppendLine();

        // ショップリスト（AssetDatabase経由で全カードを取得）
        sb.AppendLine("### Shop Lineup (商店ラインナップ)");
        sb.AppendLine("> 全カードから基礎カード（非合体結果）が出現対象です。");
        
        var allCards = LoadAllCards();
        var shopCards = new List<KanjiCardData>();
        foreach (var c in allCards)
        {
            if (!c.isFusionResult) shopCards.Add(c);
        }

        if (shopCards.Count > 0)
        {
            sb.Append("基礎カード: ");
            for (int i = 0; i < shopCards.Count; i++)
            {
                sb.Append(shopCards[i].kanji);
                if (i < shopCards.Count - 1) sb.Append(", ");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("- (カードが見つかりません)");
        }
        sb.AppendLine();
    }

    /// <summary>
    /// AssetDatabase経由でプロジェクト全体のKanjiCardDataを取得しID順でソート
    /// </summary>
    private static List<KanjiCardData> LoadAllCards()
    {
        string[] guids = AssetDatabase.FindAssets("t:KanjiCardData");
        List<KanjiCardData> cards = new List<KanjiCardData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            KanjiCardData card = AssetDatabase.LoadAssetAtPath<KanjiCardData>(path);
            if (card != null) cards.Add(card);
        }

        cards.Sort((a, b) => a.cardId.CompareTo(b.cardId));
        return cards;
    }
}
