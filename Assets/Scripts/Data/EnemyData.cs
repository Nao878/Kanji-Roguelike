using UnityEngine;

/// <summary>
/// 敵キャラクターのデータ構造
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Kanji Roguelike/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("敵の名前")]
    public string enemyName;

    [Tooltip("敵の漢字表現")]
    public string displayKanji;

    [Header("ステータス")]
    [Tooltip("最大HP")]
    public int maxHP = 20;

    [Tooltip("攻撃力")]
    public int attackPower = 5;

    [Tooltip("敵タイプ")]
    public EnemyType enemyType = EnemyType.Normal;
}

public enum EnemyType
{
    Normal,
    Elite,
    Boss
}
