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


    [Tooltip("構成数（合体元の数。例: 木=1, 林=2, 森=3）")]
    public int componentCount = 1;
    [Tooltip("攻撃力")]
    public int attackPower = 5;

    [Tooltip("敵タイプ")]
    public EnemyType enemyType = EnemyType.Normal;

    [Header("ドロップ")]
    [Tooltip("撃破時にドロップする漢字カード")]
    public KanjiCardData dropCard;
}

public enum EnemyType
{
    Normal,
    Elite,
    Boss
}
