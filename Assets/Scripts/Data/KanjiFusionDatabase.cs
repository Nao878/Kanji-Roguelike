using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全合成レシピを管理するデータベース
/// </summary>
[CreateAssetMenu(fileName = "FusionDatabase", menuName = "Kanji Roguelike/Fusion Database")]
public class KanjiFusionDatabase : ScriptableObject
{
    [Tooltip("合成レシピ一覧")]
    public List<KanjiFusionRecipe> recipes = new List<KanjiFusionRecipe>();

    // キャッシュ: 2枚素材キー → レシピリスト（複数結果対応）
    private Dictionary<string, List<KanjiFusionRecipe>> _cache2;
    // キャッシュ: 3枚素材キー → レシピリスト
    private Dictionary<string, List<KanjiFusionRecipe>> _cache3;
    // 逆引き: 結果カードID → レシピ
    private Dictionary<int, KanjiFusionRecipe> _reverseCache;

    private void BuildCache()
    {
        _cache2 = new Dictionary<string, List<KanjiFusionRecipe>>();
        _cache3 = new Dictionary<string, List<KanjiFusionRecipe>>();
        _reverseCache = new Dictionary<int, KanjiFusionRecipe>();

        foreach (var recipe in recipes)
        {
            if (recipe == null || recipe.material1 == null || recipe.material2 == null || recipe.result == null) continue;

            if (recipe.IsTwoMaterial)
            {
                // 2枚のキー（ソートして一意にする）
                string key = GetSortedKey2(recipe.material1, recipe.material2);
                if (!_cache2.ContainsKey(key)) _cache2[key] = new List<KanjiFusionRecipe>();
                _cache2[key].Add(recipe);
            }
            else if (recipe.IsThreeMaterial)
            {
                string key = GetSortedKey3(recipe.material1, recipe.material2, recipe.material3);
                if (!_cache3.ContainsKey(key)) _cache3[key] = new List<KanjiFusionRecipe>();
                _cache3[key].Add(recipe);
            }

            // 逆引き
            if (!_reverseCache.ContainsKey(recipe.result.cardId))
            {
                _reverseCache[recipe.result.cardId] = recipe;
            }
        }
    }

    /// <summary>
    /// 2枚合体の全候補レシピを検索（複数結果対応）
    /// </summary>
    public List<KanjiFusionRecipe> FindRecipes2(KanjiCardData a, KanjiCardData b)
    {
        if (_cache2 == null) BuildCache();
        string key = GetSortedKey2(a, b);
        if (_cache2.TryGetValue(key, out var list)) return list;
        return new List<KanjiFusionRecipe>();
    }

    /// <summary>
    /// 3枚合体の全候補レシピを検索
    /// </summary>
    public List<KanjiFusionRecipe> FindRecipes3(KanjiCardData a, KanjiCardData b, KanjiCardData c)
    {
        if (_cache3 == null) BuildCache();
        string key = GetSortedKey3(a, b, c);
        if (_cache3.TryGetValue(key, out var list)) return list;
        return new List<KanjiFusionRecipe>();
    }

    /// <summary>
    /// 2枚から最初のレシピを返す（後方互換）
    /// </summary>
    public KanjiFusionRecipe FindRecipe(KanjiCardData a, KanjiCardData b)
    {
        var results = FindRecipes2(a, b);
        return results.Count > 0 ? results[0] : null;
    }

    /// <summary>
    /// 結果カードIDからレシピを逆引き（分解用）
    /// </summary>
    public KanjiFusionRecipe FindRecipeByResult(int resultCardId)
    {
        if (_reverseCache == null) BuildCache();
        _reverseCache.TryGetValue(resultCardId, out var recipe);
        return recipe;
    }

    public void ClearCache()
    {
        _cache2 = null;
        _cache3 = null;
        _reverseCache = null;
    }

    private string GetSortedKey2(KanjiCardData a, KanjiCardData b)
    {
        int idA = a.cardId;
        int idB = b.cardId;
        return idA <= idB ? $"{idA}_{idB}" : $"{idB}_{idA}";
    }

    private string GetSortedKey3(KanjiCardData a, KanjiCardData b, KanjiCardData c)
    {
        var ids = new int[] { a.cardId, b.cardId, c.cardId };
        System.Array.Sort(ids);
        return $"{ids[0]}_{ids[1]}_{ids[2]}";
    }
}
