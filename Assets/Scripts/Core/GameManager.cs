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

    // 合成レシピDictionary（高速検索用）
    private Dictionary<(int, int), int> fusionRecipeDict = new Dictionary<(int, int), int>();
    // カードIDからカードデータへのマッピング
    private Dictionary<int, KanjiCardData> allCardsDict = new Dictionary<int, KanjiCardData>();

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
        allCardsDict.Clear();

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
                if (recipe.material1 != null && recipe.material2 != null && recipe.result != null)
                {
                    int id1 = recipe.material1.cardId;
                    int id2 = recipe.material2.cardId;
                    int resultId = recipe.result.cardId;

                    // 両方向で登録（id1+id2, id2+id1 どちらでも検索可能に）
                    var key1 = (Mathf.Min(id1, id2), Mathf.Max(id1, id2));
                    fusionRecipeDict[key1] = resultId;

                    // 結果カードも登録
                    if (!allCardsDict.ContainsKey(resultId))
                    {
                        allCardsDict[resultId] = recipe.result;
                    }
                }
            }
        }

        Debug.Log($"[GameManager] 合成レシピ初期化完了: {fusionRecipeDict.Count}レシピ, {allCardsDict.Count}カード");
    }

    /// <summary>
    /// 合成結果を高速検索（見つからなければ-1）
    /// </summary>
    public int FindFusionResult(int id1, int id2)
    {
        var key = (Mathf.Min(id1, id2), Mathf.Max(id1, id2));
        if (fusionRecipeDict.TryGetValue(key, out int resultId))
        {
            return resultId;
        }
        return -1;
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
