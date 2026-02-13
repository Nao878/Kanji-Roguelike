using UnityEngine;

/// <summary>
/// 漢字合成レシピデータ
/// </summary>
[CreateAssetMenu(fileName = "NewFusionRecipe", menuName = "Kanji Roguelike/Fusion Recipe")]
public class KanjiFusionRecipe : ScriptableObject
{
    [Tooltip("素材カード1")]
    public KanjiCardData material1;

    [Tooltip("素材カード2")]
    public KanjiCardData material2;

    [Tooltip("合成結果カード")]
    public KanjiCardData result;

    /// <summary>
    /// 指定された2枚のカードがこのレシピに合致するか（順不同）
    /// </summary>
    public bool Matches(KanjiCardData a, KanjiCardData b)
    {
        return (a == material1 && b == material2) ||
               (a == material2 && b == material1);
    }
}
