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

    // ランタイム用のキャッシュ（カード組み合わせ → レシピ）
    private Dictionary<string, KanjiFusionRecipe> _cache;

    /// <summary>
    /// キャッシュを構築（初回アクセス時に自動呼び出し）
    /// </summary>
    private void BuildCache()
    {
        _cache = new Dictionary<string, KanjiFusionRecipe>();
        foreach (var recipe in recipes)
        {
            if (recipe == null || recipe.material1 == null || recipe.material2 == null) continue;

            string key1 = GetKey(recipe.material1, recipe.material2);
            string key2 = GetKey(recipe.material2, recipe.material1);

            if (!_cache.ContainsKey(key1)) _cache[key1] = recipe;
            if (!_cache.ContainsKey(key2)) _cache[key2] = recipe;
        }
    }

    /// <summary>
    /// 2枚のカードから合成レシピを検索
    /// </summary>
    public KanjiFusionRecipe FindRecipe(KanjiCardData a, KanjiCardData b)
    {
        if (_cache == null) BuildCache();

        string key = GetKey(a, b);
        _cache.TryGetValue(key, out var recipe);
        return recipe;
    }

    /// <summary>
    /// キャッシュをクリア（レシピ追加後などに呼び出す）
    /// </summary>
    public void ClearCache()
    {
        _cache = null;
    }

    private string GetKey(KanjiCardData a, KanjiCardData b)
    {
        return $"{a.kanji}_{b.kanji}";
    }
}
