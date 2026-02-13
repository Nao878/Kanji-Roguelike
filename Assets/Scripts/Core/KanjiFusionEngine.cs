using UnityEngine;

/// <summary>
/// 漢字合体エンジン - 漢字カードの合成ロジック
/// </summary>
public class KanjiFusionEngine : MonoBehaviour
{
    [Header("参照")]
    public KanjiFusionDatabase fusionDatabase;

    /// <summary>
    /// 2枚のカードを合成して新しいカードを取得
    /// </summary>
    /// <param name="card1">素材カード1</param>
    /// <param name="card2">素材カード2</param>
    /// <returns>合成結果カード（合成不可の場合はnull）</returns>
    public KanjiCardData TryFuse(KanjiCardData card1, KanjiCardData card2)
    {
        if (fusionDatabase == null)
        {
            Debug.LogError("[FusionEngine] FusionDatabaseが設定されていません！");
            return null;
        }

        if (card1 == null || card2 == null)
        {
            Debug.LogWarning("[FusionEngine] カードがnullです");
            return null;
        }

        var recipe = fusionDatabase.FindRecipe(card1, card2);
        if (recipe != null && recipe.result != null)
        {
            Debug.Log($"[FusionEngine] 合成成功！ 『{card1.kanji}』+『{card2.kanji}』=『{recipe.result.kanji}』");
            return recipe.result;
        }

        Debug.Log($"[FusionEngine] 合成失敗: 『{card1.kanji}』+『{card2.kanji}』に対応するレシピがありません");
        return null;
    }

    /// <summary>
    /// 合成可能かどうかチェック
    /// </summary>
    public bool CanFuse(KanjiCardData card1, KanjiCardData card2)
    {
        if (fusionDatabase == null || card1 == null || card2 == null) return false;
        return fusionDatabase.FindRecipe(card1, card2) != null;
    }
}
