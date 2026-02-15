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
        AppendDeckAndShopLimit(sb);

        // ファイル書き込み
        string fullPath = Path.Combine(Application.dataPath, "GameDesignDoc.md");
        // Application.dataPath は Assetsフォルダを指す
        // OUTPUT_PATHはAssets/GameDesignDoc.md なので、Application.dataPathの親から考えるか、
        // 単にFile.WriteAllText("Assets/GameDesignDoc.md", ...) でUnityプロジェクトルートからの相対パスでもいけるが、
        // 確実にするなら絶対パス
        
        // エディタ拡張ならプロジェクトルートがカレントディレクトリになることが多いが、念のため
        File.WriteAllText(OUTPUT_PATH, sb.ToString());
        
        AssetDatabase.ImportAsset(OUTPUT_PATH);
        var asset = AssetDatabase.LoadAssetAtPath<Object>(OUTPUT_PATH);
        EditorGUIUtility.PingObject(asset);

        Debug.Log($"ゲーム仕様書を更新しました: {OUTPUT_PATH}");
    }

    private static void AppendCardList(StringBuilder sb)
    {
        sb.AppendLine("## 1. Card List (全カード一覧)");
        sb.AppendLine("| ID | Kanji | Cost | Type | Value | Effect |");
        sb.AppendLine("|---|---|---|---|---|---|");

        string[] guids = AssetDatabase.FindAssets("t:KanjiCardData");
        List<KanjiCardData> cards = new List<KanjiCardData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            KanjiCardData card = AssetDatabase.LoadAssetAtPath<KanjiCardData>(path);
            if (card != null) cards.Add(card);
        }

        // ID順にソート
        cards.Sort((a, b) => a.cardId.CompareTo(b.cardId));

        foreach (KanjiCardData c in cards)
        {
            string effectText = c.description.Replace("\n", "<br>");
            sb.AppendLine($"| {c.cardId} | **{c.kanji}** | {c.cost} | {c.effectType} | {c.effectValue} | {effectText} |");
        }
        sb.AppendLine();
    }

    private static void AppendFusionRecipes(StringBuilder sb)
    {
        sb.AppendLine("## 2. Fusion Recipes (合体レシピ)");
        sb.AppendLine("| Material A | Material B | Result | Description |");
        sb.AppendLine("|---|---|---|---|");

        string[] guids = AssetDatabase.FindAssets("t:KanjiFusionRecipe");
        List<KanjiFusionRecipe> recipes = new List<KanjiFusionRecipe>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            KanjiFusionRecipe recipe = AssetDatabase.LoadAssetAtPath<KanjiFusionRecipe>(path);
            if (recipe != null) recipes.Add(recipe);
        }

        // 素材Aの漢字順などでソート
        recipes.Sort((a, b) => 
        {
            if (a.material1 == null) return -1;
            if (b.material1 == null) return 1;
            return string.Compare(a.material1.kanji, b.material1.kanji);
        });

        foreach (KanjiFusionRecipe r in recipes)
        {
            if (r.material1 == null || r.material2 == null || r.result == null) continue;
            sb.AppendLine($"| {r.material1.kanji} | {r.material2.kanji} | **{r.result.kanji}** | {r.result.description} |");
        }
        sb.AppendLine();
    }

    private static void AppendDeckAndShopLimit(StringBuilder sb)
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

                foreach (KeyValuePair<string, int> kvp in deckCounts)
                {
                    sb.AppendLine($"- **{kvp.Key}** x{kvp.Value}");
                }
            }
            else
            {
                sb.AppendLine("- (Scene上のGameManagerからデッキ情報を取得できませんでした)");
            }
        }
        else
        {
            sb.AppendLine("- (GameManagerオブジェクトが見つかりません - PlayモードまたはSetup直後に実行してください)");
        }
        sb.AppendLine();

        // ショップリスト
        sb.AppendLine("### Shop Lineup (商店ラインナップ)");
        sb.AppendLine("> `Resources`フォルダに含まれる全てのカードが出現対象です。");
        
        KanjiCardData[] shopCards = Resources.LoadAll<KanjiCardData>("");
        if (shopCards != null && shopCards.Length > 0)
        {
            System.Array.Sort(shopCards, (a, b) => string.Compare(a.kanji, b.kanji));

            sb.Append("List: ");
            for (int i = 0; i < shopCards.Length; i++)
            {
                sb.Append(shopCards[i].kanji);
                if (i < shopCards.Length - 1) sb.Append(", ");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("- (ResourcesフォルダにKanjiCardDataが見つかりません)");
        }
        sb.AppendLine();
    }
}
