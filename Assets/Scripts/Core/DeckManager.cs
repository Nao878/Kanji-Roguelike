using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// デッキ管理システム
/// デッキの構築、保存、バリデーションを担当
/// </summary>
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("デッキ設定")]
    public int minDeckSize = 20;
    public int maxDeckSize = 20;
    public int maxDuplicateKanji = 3;

    [Header("現在のデッキ")]
    public List<KanjiCardData> currentDeck = new List<KanjiCardData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// デッキにカードを追加
    /// </summary>
    public bool AddCardToDeck(KanjiCardData card)
    {
        if (card == null) return false;

        // 同名カードの枚数チェック
        int count = currentDeck.Count(c => c.kanji == card.kanji);
        if (count >= maxDuplicateKanji)
        {
            Debug.Log($"[DeckManager] 同名漢字は最大{maxDuplicateKanji}枚までです: {card.kanji}");
            return false;
        }

        if (currentDeck.Count >= maxDeckSize)
        {
            Debug.Log($"[DeckManager] デッキサイズは最大{maxDeckSize}枚です");
            return false;
        }

        currentDeck.Add(card);
        return true;
    }

    /// <summary>
    /// デッキからカードを削除
    /// </summary>
    public void RemoveCardFromDeck(KanjiCardData card)
    {
        currentDeck.Remove(card);
    }

    /// <summary>
    /// デッキが有効かどうかチェック
    /// </summary>
    public bool IsDeckValid()
    {
        return currentDeck.Count >= minDeckSize && currentDeck.Count <= maxDeckSize;
    }

    /// <summary>
    /// デッキをクリア（デバッグ用または初期化用）
    /// </summary>
    public void ClearDeck()
    {
        currentDeck.Clear();
    }

    /// <summary>
    /// 初期デッキの自動生成（インベントリから有効な範囲で埋める）
    /// </summary>
    public void AutoFillDeck(List<KanjiCardData> inventory)
    {
        currentDeck.Clear();
        foreach (var card in inventory)
        {
            if (currentDeck.Count >= maxDeckSize) break;
            
            int count = currentDeck.Count(c => c.kanji == card.kanji);
            if (count < maxDuplicateKanji)
            {
                currentDeck.Add(card);
            }
        }
    }
}
