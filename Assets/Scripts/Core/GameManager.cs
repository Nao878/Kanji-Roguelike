using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体の状態管理（シングルトン）
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("ゲーム設定")]
    public int playerMaxHP = 50;
    public int playerStartMana = 3;
    public int initialHandSize = 5;
    public int startGold = 50;
    public int fusionCost = 20;

    [Header("現在の状態")]
    public GameState currentState = GameState.Map;
    public int playerHP;
    public int playerMana;
    public int playerMaxMana;
    public int playerAttackBuff = 0;
    public int playerDefenseBuff = 0;
    public int playerGold = 0;

    [Header("デッキ")]
    public List<KanjiCardData> deck = new List<KanjiCardData>();
    public List<KanjiCardData> hand = new List<KanjiCardData>();
    public List<KanjiCardData> discardPile = new List<KanjiCardData>();

    [Header("参照")]
    public KanjiFusionDatabase fusionDatabase;
    public BattleManager battleManager;
    public MapManager mapManager;
    public KanjiFusionEngine fusionEngine;

    [Header("UI参照")]
    public GameObject mapPanel;
    public GameObject battlePanel;
    public GameObject fusionPanel;
    public GameObject shopPanel;
    public GameObject dojoPanel;

    // 合成レシピDictionary（高速検索用、複数結果対応）
    private Dictionary<(int, int), List<int>> fusionRecipeDict = new Dictionary<(int, int), List<int>>();
    // 3枚合体用
    private Dictionary<(int, int, int), List<int>> fusionRecipeDict3 = new Dictionary<(int, int, int), List<int>>();
    // カードIDからカードデータへのマッピング
    private Dictionary<int, KanjiCardData> allCardsDict = new Dictionary<int, KanjiCardData>();
    // 分解用逆引き: 結果ID -> 素材IDList
    private Dictionary<int, List<int>> decomposeDict = new Dictionary<int, List<int>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeGame();
    }

    /// <summary>
    /// ゲーム初期化
    /// </summary>
    public void InitializeGame()
    {
        playerHP = playerMaxHP;
        playerMaxMana = playerStartMana;
        playerMana = playerMaxMana;
        playerAttackBuff = 0;
        playerDefenseBuff = 0;
        playerGold = startGold;

        Debug.Log($"[GameManager] ゲーム初期化完了 HP:{playerHP} マナ:{playerMana}");

        // 合成レシピDictionaryを初期化
        InitializeFusionRecipes();

        ChangeState(GameState.Map);
    }

    /// <summary>
    /// ゲームステートを変更
    /// </summary>
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GameManager] ステート変更: {newState}");

        // UIパネルの表示切替
        if (mapPanel != null) mapPanel.SetActive(newState == GameState.Map);
        if (battlePanel != null) battlePanel.SetActive(newState == GameState.Battle);
        if (fusionPanel != null) fusionPanel.SetActive(newState == GameState.Fusion);
        if (shopPanel != null) shopPanel.SetActive(newState == GameState.Shop);
        if (dojoPanel != null) dojoPanel.SetActive(newState == GameState.Dojo);

        switch (newState)
        {
            case GameState.Map:
                if (mapManager != null) mapManager.ShowMap();
                break;
            case GameState.Battle:
                // BattleManagerがStartBattleを呼び出す
                break;
            case GameState.Fusion:
                break;
            case GameState.GameOver:
                Debug.Log("[GameManager] ゲームオーバー！");
                break;
        }
    }

    /// <summary>
    /// デッキからカードを引く
    /// </summary>
    public void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                // 捨て札をデッキに戻してシャッフル
                if (discardPile.Count == 0) return;
                deck.AddRange(discardPile);
                discardPile.Clear();
                ShuffleDeck();
            }

            if (deck.Count > 0)
            {
                var card = deck[0];
                deck.RemoveAt(0);
                hand.Add(card);
                Debug.Log($"[GameManager] カードドロー: {card.kanji}");
            }
        }
    }

    /// <summary>
    /// デッキをシャッフル
    /// </summary>
    public void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
        Debug.Log("[GameManager] デッキシャッフル完了");
    }

    /// <summary>
    /// カードを使用（バトル中）
    /// </summary>
    public bool UseCard(KanjiCardData card)
    {
        if (playerMana < card.cost)
        {
            Debug.Log($"[GameManager] マナ不足！ 必要:{card.cost} 現在:{playerMana}");
            return false;
        }

        playerMana -= card.cost;
        hand.Remove(card);
        discardPile.Add(card);

        Debug.Log($"[GameManager] カード使用: {card.kanji}（{card.description}）");
        return true;
    }

    /// <summary>
    /// プレイヤーにダメージ
    /// </summary>
    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(0, damage - playerDefenseBuff);
        playerHP = Mathf.Max(0, playerHP - actualDamage);
        Debug.Log($"[GameManager] プレイヤーが{actualDamage}ダメージ受けた HP:{playerHP}");

        if (playerHP <= 0)
        {
            ChangeState(GameState.GameOver);
        }
    }

    /// <summary>
    /// ターン開始時のリセット
    /// </summary>
    public void StartPlayerTurn()
    {
        playerMana = playerMaxMana;
        playerDefenseBuff = 0;
        DrawCards(initialHandSize - hand.Count);
        Debug.Log($"[GameManager] プレイヤーターン開始 マナ:{playerMana}");
    }

    /// <summary>
    /// 合成レシピDictionaryを初期化
    /// </summary>
    public void InitializeFusionRecipes()
    {
        fusionRecipeDict.Clear();
        fusionRecipeDict3.Clear();
        allCardsDict.Clear();
        decomposeDict.Clear();

        // 全カードアセットを検索してDictionaryに登録
        var allCards = Resources.LoadAll<KanjiCardData>("");
        foreach (var card in allCards)
        {
            if (!allCardsDict.ContainsKey(card.cardId))
            {
                allCardsDict[card.cardId] = card;
            }
        }

        // デッキ内のカードも登録
        foreach (var card in deck)
        {
            if (!allCardsDict.ContainsKey(card.cardId))
            {
                allCardsDict[card.cardId] = card;
            }
        }

        // FusionDatabaseからレシピを読み込み
        if (fusionDatabase != null && fusionDatabase.recipes != null)
        {
            foreach (var recipe in fusionDatabase.recipes)
            {
                if (recipe.material1 == null || recipe.material2 == null || recipe.result == null) continue;

                int resultId = recipe.result.cardId;

                // 結果カードも登録
                if (!allCardsDict.ContainsKey(resultId))
                {
                    allCardsDict[resultId] = recipe.result;
                }

                if (recipe.IsTwoMaterial)
                {
                    int id1 = recipe.material1.cardId;
                    int id2 = recipe.material2.cardId;
                    var key = (Mathf.Min(id1, id2), Mathf.Max(id1, id2));
                    if (!fusionRecipeDict.ContainsKey(key)) fusionRecipeDict[key] = new List<int>();
                    fusionRecipeDict[key].Add(resultId);

                    // 分解用逆引き
                    decomposeDict[resultId] = new List<int> { id1, id2 };
                }
                else if (recipe.IsThreeMaterial)
                {
                    int id1 = recipe.material1.cardId;
                    int id2 = recipe.material2.cardId;
                    int id3 = recipe.material3.cardId;
                    var ids = new int[] { id1, id2, id3 };
                    System.Array.Sort(ids);
                    var key = (ids[0], ids[1], ids[2]);
                    if (!fusionRecipeDict3.ContainsKey(key)) fusionRecipeDict3[key] = new List<int>();
                    fusionRecipeDict3[key].Add(resultId);

                    decomposeDict[resultId] = new List<int> { id1, id2, id3 };
                }
            }
        }

        Debug.Log($"[GameManager] 合成レシピ初期化完了: 2枚:{fusionRecipeDict.Count} 3枚:{fusionRecipeDict3.Count} カード:{allCardsDict.Count}");
    }

    /// <summary>
    /// 2枚合成結果を高速検索（最初の1件、見つからなければ-1）
    /// </summary>
    public int FindFusionResult(int id1, int id2)
    {
        var key = (Mathf.Min(id1, id2), Mathf.Max(id1, id2));
        if (fusionRecipeDict.TryGetValue(key, out var results) && results.Count > 0)
        {
            return results[0];
        }
        return -1;
    }

    /// <summary>
    /// 2枚合成の全候補を検索（複数結果対応）
    /// </summary>
    public List<int> FindFusionResults(int id1, int id2)
    {
        var key = (Mathf.Min(id1, id2), Mathf.Max(id1, id2));
        if (fusionRecipeDict.TryGetValue(key, out var results))
        {
            return results;
        }
        return new List<int>();
    }

    /// <summary>
    /// 3枚合成の全候補を検索
    /// </summary>
    public List<int> FindFusionResults3(int id1, int id2, int id3)
    {
        var ids = new int[] { id1, id2, id3 };
        System.Array.Sort(ids);
        var key = (ids[0], ids[1], ids[2]);
        if (fusionRecipeDict3.TryGetValue(key, out var results))
        {
            return results;
        }
        return new List<int>();
    }

    /// <summary>
    /// 分解：結果IDから素材IDリストを取得
    /// </summary>
    public List<int> FindDecomposeMaterials(int resultCardId)
    {
        if (decomposeDict.TryGetValue(resultCardId, out var materials))
        {
            return materials;
        }
        return null;
    }

    /// <summary>
    /// カードIDからカードデータを取得
    /// </summary>
    public KanjiCardData GetCardById(int cardId)
    {
        if (allCardsDict.TryGetValue(cardId, out KanjiCardData card))
        {
            return card;
        }
        return null;
    }

    /// <summary>
    /// 分解実行：合体済みカードを素材に戻す
    /// </summary>
    public bool DecomposeCard(KanjiCardData card)
    {
        if (card == null || !card.isFusionResult) return false;

        var materialIds = FindDecomposeMaterials(card.cardId);
        if (materialIds == null || materialIds.Count == 0) return false;

        // 手札から合体カードを除去
        if (!hand.Remove(card))
        {
            if (!deck.Remove(card))
            {
                if (!discardPile.Remove(card)) return false;
            }
        }

        // 素材カードを手札に追加
        foreach (int matId in materialIds)
        {
            var matCard = GetCardById(matId);
            if (matCard != null)
            {
                hand.Add(matCard);
                Debug.Log($"[GameManager] 分解: 『{matCard.kanji}』を手札に追加");
            }
        }

        Debug.Log($"[GameManager] 『{card.kanji}』を分解しました");
        return true;
    }
}

/// <summary>
/// ゲームの状態
/// </summary>
public enum GameState
{
    Title,
    Map,
    Battle,
    Fusion,
    Shop,
    Event,
    Dojo,
    GameOver
}
