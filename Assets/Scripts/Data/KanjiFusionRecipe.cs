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

    [Tooltip("素材カード3（3枚合体時のみ）")]
    public KanjiCardData material3;

    [Tooltip("合成結果カード")]
    public KanjiCardData result;

    /// <summary>
    /// 2枚合体レシピか？
    /// </summary>
    public bool IsTwoMaterial => material3 == null;

    /// <summary>
    /// 3枚合体レシピか？
    /// </summary>
    public bool IsThreeMaterial => material3 != null;

    /// <summary>
    /// 指定された2枚のカードがこのレシピに合致するか（順不同、2枚合体のみ）
    /// </summary>
    public bool Matches(KanjiCardData a, KanjiCardData b)
    {
        if (!IsTwoMaterial) return false;
        return (a == material1 && b == material2) ||
               (a == material2 && b == material1);
    }

    /// <summary>
    /// 指定された3枚のカードがこのレシピに合致するか（順不同）
    /// </summary>
    public bool Matches3(KanjiCardData a, KanjiCardData b, KanjiCardData c)
    {
        if (!IsThreeMaterial) return false;
        var mats = new[] { material1, material2, material3 };
        var inputs = new[] { a, b, c };
        // 全順列チェック
        return MatchPermutation(mats, inputs);
    }

    private bool MatchPermutation(KanjiCardData[] mats, KanjiCardData[] inputs)
    {
        bool[] used = new bool[3];
        for (int i = 0; i < 3; i++)
        {
            bool found = false;
            for (int j = 0; j < 3; j++)
            {
                if (!used[j] && mats[i] == inputs[j])
                {
                    used[j] = true;
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }
}
