using UnityEngine;

/// <summary>
/// 漢字カードのデータ構造（ScriptableObject）
/// </summary>
[CreateAssetMenu(fileName = "NewKanjiCard", menuName = "Kanji Roguelike/Kanji Card Data")]
public class KanjiCardData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("カードID（合成レシピ検索用）")]
    public int cardId;

    [Tooltip("漢字1文字")]
    public string kanji;

    [Tooltip("カード名")]
    public string cardName;

    [TextArea(2, 4)]
    [Tooltip("効果説明")]
    public string description;

    [Header("ステータス")]
    [Tooltip("マナコスト")]
    public int cost = 1;

    [Tooltip("効果値")]
    public int effectValue = 1;

    [Tooltip("効果タイプ")]
    public CardEffectType effectType = CardEffectType.Attack;

    [Header("合成情報")]
    [Tooltip("合成で生まれたカードかどうか")]
    public bool isFusionResult = false;

    [Header("鍛錬による強化値")]
    [Tooltip("攻撃力強化値（鍛錬で増加）")]
    public int attackModifier = 0;

    [Tooltip("防御力強化値（鍛錬で増加）")]
    public int defenseModifier = 0;

    /// <summary>
    /// 実効攻撃値（基本値＋強化値）
    /// </summary>
    public int GetEffectiveAttack()
    {
        return effectValue + attackModifier;
    }

    /// <summary>
    /// 実効防御値（基本値＋強化値）
    /// </summary>
    public int GetEffectiveDefense()
    {
        return effectValue + defenseModifier;
    }

    /// <summary>
    /// 強化済みかどうか
    /// </summary>
    public bool IsEnhanced => attackModifier > 0 || defenseModifier > 0;

    /// <summary>
    /// 表示名（強化済みなら＋付き）
    /// </summary>
    public string DisplayName => IsEnhanced ? $"{kanji}＋" : kanji;
}

/// <summary>
/// カード効果タイプ
/// </summary>
public enum CardEffectType
{
    Attack,   // 攻撃
    Defense,  // 防御
    Heal,     // 回復
    Buff,     // バフ
    Special,  // 特殊
    Draw      // ドロー
}
