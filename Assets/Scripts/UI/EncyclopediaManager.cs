using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 漢字図鑑（実績）の管理
/// </summary>
public class EncyclopediaManager : MonoBehaviour
{
    public static EncyclopediaManager Instance { get; private set; }

    private HashSet<int> unlockedCardIds = new HashSet<int>();
    private const string SAVE_KEY_PREFIX = "KanjiUnlocked_";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        LoadUnlockedData();
    }

    /// <summary>
    /// カードを図鑑に登録（取得時・合体成功時に呼ぶ）
    /// </summary>
    public void UnlockCard(int cardId)
    {
        if (!unlockedCardIds.Contains(cardId))
        {
            unlockedCardIds.Add(cardId);
            PlayerPrefs.SetInt(SAVE_KEY_PREFIX + cardId, 1);
            PlayerPrefs.Save();
            Debug.Log($"[Encyclopedia] 新しい漢字を図鑑に登録しました: ID {cardId}");
        }
    }

    /// <summary>
    /// カードがアンロック済みか確認
    /// </summary>
    public bool IsUnlocked(int cardId)
    {
        return unlockedCardIds.Contains(cardId);
    }

    /// <summary>
    /// 全カードデータからアンロック済みリストをまとめて読み込み
    /// </summary>
    public void LoadUnlockedData()
    {
        unlockedCardIds.Clear();
        var allCards = Resources.LoadAll<KanjiCardData>("");
        foreach (var card in allCards)
        {
            if (PlayerPrefs.GetInt(SAVE_KEY_PREFIX + card.cardId, 0) == 1)
            {
                unlockedCardIds.Add(card.cardId);
            }
        }
    }

    /// <summary>
    /// セーブデータ初期化（デバッグ用）
    /// </summary>
    public void ClearData()
    {
        unlockedCardIds.Clear();
        var allCards = Resources.LoadAll<KanjiCardData>("");
        foreach (var card in allCards)
        {
            PlayerPrefs.DeleteKey(SAVE_KEY_PREFIX + card.cardId);
        }
        PlayerPrefs.Save();
    }
}
