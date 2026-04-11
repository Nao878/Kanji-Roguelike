using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体の状態管理（シングルトン）
/// 2D見下ろし型フィールド探索 + 消費型インベントリ方式
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
    public int inventoryMaxSize = 30;

    [Header("現在の状態")]
    public GameState currentState = GameState.Field;
    public int playerHP;
    public int playerMana;
    public int playerMaxMana;
    public int playerAttackBuff = 0;
    public int playerDefenseBuff = 0;
    public int playerGold = 0;

    [Header("インベントリ（消費型）")]
    public List<KanjiCardData> inventory = new List<KanjiCardData>();
    public List<KanjiCardData> hand = new List<KanjiCardData>();

    [Header("参照")]
    public KanjiFusionDatabase fusionDatabase;
    public BattleManager battleManager;
    public MapManager mapManager;
    public FieldManager fieldManager;
    public KanjiFusionEngine fusionEngine;

    [Header("UI参照")]
    public GameObject mapPanel;
    public GameObject battlePanel;
    public GameObject fusionPanel;
    public GameObject shopPanel;
    public GameObject dojoPanel;
    public GameObject fieldPanel;
    public FusionSelectionUI fusionSelectionUI;

    public void ShowFusionSelectionUI(List<int> resultIds, System.Action<int> onSelected)
    {
        if (fusionSelectionUI != null)
        {
            fusionSelectionUI.ShowSelection(resultIds, onSelected);
        }
    }

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

        Debug.Log($"[GameManager] ゲーム初期化完了 HP:{playerHP} マナ:{playerMana} インベントリ:{inventory.Count}枚");

        // 合成レシピDictionaryを初期化
        InitializeFusionRecipes();

        // 初期インベントリを図鑑に登録
        if (EncyclopediaManager.Instance != null && inventory != null)
        {
            foreach (var card in inventory)
            {
                EncyclopediaManager.Instance.UnlockCard(card.cardId);
            }
        }

        ChangeState(GameState.Field);
    }

    /// <summary>
    /// ゲームステートを変更
    /// </summary>
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[GameManager] ステート変更: {newState}");

        // UIパネルの表示切替
        if (fieldPanel != null) fieldPanel.SetActive(newState == GameState.Field);
        if (mapPanel != null) mapPanel.SetActive(false); // マップは常に非表示
        if (battlePanel != null) battlePanel.SetActive(newState == GameState.Battle);
        if (fusionPanel != null) fusionPanel.SetActive(newState == GameState.Fusion);
        if (shopPanel != null) shopPanel.SetActive(newState == GameState.Shop);
        if (dojoPanel != null) dojoPanel.SetActive(newState == GameState.Dojo);

        switch (newState)
        {
            case GameState.Field:
                if (fieldManager != null) fieldManager.ShowField();
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
    /// インベントリからランダムに手札を引く（消費はUseCard時）
    /// </summary>
    public void DrawFromInventory(int count)
    {
        hand.Clear();

        if (inventory.Count == 0) return;

        // インベントリをシャッフルしてから先頭N枚を手札にコピー
        var shuffled = new List<KanjiCardData>(inventory);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        int drawCount = Mathf.Min(count, shuffled.Count);
        for (int i = 0; i < drawCount; i++)
        {
            hand.Add(shuffled[i]);
        }

        Debug.Log($"[GameManager] 手札引き: {hand.Count}枚（インベントリ残:{inventory.Count}枚）");
    }

    /// <summary>
    /// カードを使用（消費型：インベントリからも完全削除）
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
        inventory.Remove(card); // インベントリからも完全消費

        Debug.Log($"[GameManager] カード消費: {card.kanji}（残インベントリ:{inventory.Count}枚）");
        return true;
    }

    /// <summary>
    /// インベントリにカードを追加（上限チェック付き）
    /// </summary>
    public bool AddToInventory(KanjiCardData card)
    {
        if (card == null) return false;
        if (inventory.Count >= inventoryMaxSize)
        {
            Debug.Log($"[GameManager] インベントリが満杯（{inventoryMaxSize}枚）！ 『{card.kanji}』を追加できません");
            return false;
        }
        inventory.Add(card);
        Debug.Log($"[GameManager] インベントリに『{card.kanji}』を追加（{inventory.Count}/{inventoryMaxSize}）");

        // 図鑑に登録
        if (EncyclopediaManager.Instance != null)
        {
            EncyclopediaManager.Instance.UnlockCard(card.cardId);
        }

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
        DrawFromInventory(initialHandSize);
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

        // インベントリ内のカードも登録
        foreach (var card in inventory)
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
    /// 分解実行：合体済みカードを素材に戻す（インベントリに戻す）
    /// </summary>
    public bool DecomposeCard(KanjiCardData card)
    {
        if (card == null || !card.isFusionResult) return false;

        var materialIds = FindDecomposeMaterials(card.cardId);
        if (materialIds == null || materialIds.Count == 0) return false;

        // 手札とインベントリから合体カードを除去
        hand.Remove(card);
        inventory.Remove(card);

        // 素材カードを手札とインベントリに追加
        foreach (int matId in materialIds)
        {
            var matCard = GetCardById(matId);
            if (matCard != null)
            {
                hand.Add(matCard);
                AddToInventory(matCard);
                Debug.Log($"[GameManager] 分解: 『{matCard.kanji}』をインベントリに追加");
            }
        }

        Debug.Log($"[GameManager] 『{card.kanji}』を分解しました");
        return true;
    }

    // ==== 後方互換用（他スクリプトからの参照用）====
    // deck を inventory のエイリアスとして公開
    public List<KanjiCardData> deck
    {
        get { return inventory; }
        set { inventory = value; }
    }

    // discardPile は廃止。参照が残っている場合のための空リスト
    private List<KanjiCardData> _dummyDiscard = new List<KanjiCardData>();
    public List<KanjiCardData> discardPile
    {
        get { return _dummyDiscard; }
        set { } // no-op
    }
}

/// <summary>
/// ゲームの状態
/// </summary>
public enum GameState
{
    Title,
    Field,   // 2D見下ろしフィールド探索
    Map,     // (旧) Slay the Spire型マップ - 後方互換用に残す
    Battle,
    Fusion,
    Shop,
    Event,
    Dojo,
    GameOver
}
