using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// ターン制戦闘管理
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("戦闘状態")]
    public EnemyData currentEnemyData;
    public int enemyCurrentHP;
    public bool isPlayerTurn = true;
    public bool isAutoEndingTurn = false; 
    public bool enemyIsStunned = false; // スタンフラグ
    public BattleState battleState = BattleState.Idle;

    [Header("UI参照")]
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerManaText;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyHPText;
    public TextMeshProUGUI battleLogText;
    public Button endTurnButton;
    public Transform handArea;

    // コンボ関連
    private string lastPlayedKanji = "";
    private CardElement lastPlayedElement = CardElement.None;
    private int elementChainCount = 0;
    private float turnStartTime; // ターン開始時刻の記録
    
    // 熟語によるボーナスダメージ等
    private System.Collections.Generic.Dictionary<string, int> jukugoCombos = new System.Collections.Generic.Dictionary<string, int>()
    {
        { "火炎", 15 },
        { "森林", 15 },
        { "明暗", 20 },
        { "白黒", 10 },
        { "休眠", 10 },
        { "目口", 5 },
        { "人民", 5 }
    };
    
    [Header("バトルUI")]
    public EnemyData[] normalEnemies;
    public EnemyData bossEnemy;

    [Header("BattleUI参照")]
    public BattleUI battleUI;

    public enum BattleState
    {
        Idle,
        PlayerTurn,
        EnemyTurn,
        Won,
        Lost
    }

    void Start()
    {
        // 初期状態ではUIを非表示またはリセット
    }

    /// <summary>
    /// 戦闘開始
    /// </summary>
    public void StartBattle(EnemyData enemy)
    {
        if (enemy == null)
        {
            Debug.LogError("[BattleManager] 敵データがnullです！");
            return;
        }

        currentEnemyData = Instantiate(enemy);

        enemyCurrentHP = enemy.maxHP;
        isPlayerTurn = true;
        enemyIsStunned = false;
        
        lastPlayedKanji = "";
        lastPlayedElement = CardElement.None;
        elementChainCount = 0;
        battleState = BattleState.PlayerTurn;

        Debug.Log($"[BattleManager] 戦闘開始！ 敵:{enemy.enemyName}（HP:{enemy.maxHP}）");
        AddBattleLog($"『{enemy.displayKanji}』{enemy.enemyName}が現れた！");

        // GameManagerにステート変更を通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Battle);
            // 戦闘開始時にまずデッキを初期化（山札準備）
            GameManager.Instance.InitializeBattleDeck();
            GameManager.Instance.StartPlayerTurn();
        }

        turnStartTime = Time.time; // ターン開始時刻を記録
        UpdateUI();

        // BattleUIの手札を更新
        if (battleUI != null)
        {
            battleUI.UpdateHandUI();
            battleUI.UpdateStatusUI();
        }
    }

    /// <summary>
    /// カードを使用して攻撃/効果を適用
    /// </summary>
    public void PlayCard(KanjiCardData card)
    {
        if (battleState != BattleState.PlayerTurn)
        {
            Debug.Log("[BattleManager] プレイヤーのターンではありません");
            return;
        }

        var gm = GameManager.Instance;
        if (gm == null || !gm.UseCard(card)) return;

        // カード効果を適用
        ApplyCardEffect(card);
        UpdateUI();
        CheckBattleEnd();

        // BattleUI更新
        if (battleUI != null)
        {
            battleUI.UpdateHandUI();
            battleUI.UpdateStatusUI();
        }
    }

    /// <summary>
    /// カード効果を適用（漢字ブレイク対応）
    /// </summary>
    private void ApplyCardEffect(KanjiCardData card)
    {
        var gm = GameManager.Instance;
        // modifier対応：攻撃系はattackModifier、防御系はdefenseModifierを加算
        int attackValue = card.effectValue + card.attackModifier + (card.effectType == CardEffectType.Attack ? gm.playerAttackBuff : 0);
        int defenseValue = card.effectValue + card.defenseModifier;

        // 【タスク2】漢字ブレイク：相殺 (Mirror Clash)
        bool isMirrorClash = false;
        if (card.kanji == currentEnemyData.displayKanji)
        {
            isMirrorClash = true;
            attackValue *= 3; // 確定クリティカル（特大ダメージ）
            gm.playerMana += 1; // AP回復
            AddBattleLog("<color=#FF0000><b>相殺（Mirror Clash）発動！特大ダメージ ＆ AP+1！</b></color>");
            if (VFXManager.Instance != null && battleUI != null)
            {
                VFXManager.Instance.PlayComboEffect(battleUI.gameObject, "MIRROR CLASH!!", Color.red);
                // CFXR特大ダメージエフェクト + 強カメラシェイク
                if (battleUI.enemyKanjiText != null)
                    VFXManager.Instance.PlayCriticalHitVFX(battleUI.enemyKanjiText.transform.position);
            }
        }

        // 【タスク2】漢字ブレイク：構成数マウント (Overpower)
        if (!isMirrorClash && card.componentCount > currentEnemyData.componentCount)
        {
            attackValue = Mathf.CeilToInt(attackValue * 1.5f);
            AddBattleLog($"<color=#FFA500>構成数マウント（Overpower）！ ダメージ1.5倍！({card.componentCount} vs {currentEnemyData.componentCount})</color>");
            if (VFXManager.Instance != null && battleUI != null)
            {
                VFXManager.Instance.PlayComboEffect(battleUI.gameObject, "OVERPOWER!!", new Color(1f, 0.5f, 0f));
                // CFXR特大ダメージエフェクト + 強カメラシェイク
                if (battleUI.enemyKanjiText != null)
                    VFXManager.Instance.PlayCriticalHitVFX(battleUI.enemyKanjiText.transform.position);
            }
        }

        // コンボ判定：熟語
        string comboStr = lastPlayedKanji + card.kanji;
        int jukugoBonusDmg = 0;
        if (jukugoCombos.ContainsKey(comboStr))
        {
            jukugoBonusDmg = jukugoCombos[comboStr];
            attackValue += jukugoBonusDmg;
            AddBattleLog($"<color=#FFD700>熟語コンボ発動！『{comboStr}』ボーナス+{jukugoBonusDmg}</color>");
            if (VFXManager.Instance != null && battleUI != null)
            {
                VFXManager.Instance.PlayComboEffect(battleUI.gameObject, $"熟語コンボ\n{comboStr}!!", new Color(1f, 0.8f, 0f));
            }
        }

        // コンボ判定：属性チェイン

        if (card.element != CardElement.None && card.element == lastPlayedElement)
        {
            elementChainCount++;

            int elementBonus = elementChainCount * 2;
            attackValue += elementBonus;
            AddBattleLog($"<color=#00FFFF>属性チェイン！({card.element}) ボーナス+{elementBonus}</color>");
            if (VFXManager.Instance != null && battleUI != null)
            {
                VFXManager.Instance.PlayComboEffect(battleUI.gameObject, $"{elementChainCount} CHAIN!!\n{card.element}", new Color(0f, 1f, 1f));
            }
        }
        else
        {
            elementChainCount = 1;
        }

        // 次回コンボの布石を記憶
        lastPlayedKanji = card.kanji;
        lastPlayedElement = card.element;

        switch (card.effectType)
        {
            case CardEffectType.Attack:
                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - attackValue);
                AddBattleLog($"『{card.DisplayName}』で{attackValue}ダメージ！");
                if (battleUI != null && battleUI.enemyKanjiText != null && VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayDamageEffect(battleUI.enemyKanjiText.gameObject, attackValue);
                    // CFXR通常攻撃ヒットエフェクト（ブレイク発動時以外）
                    if (!isMirrorClash && card.componentCount <= (currentEnemyData != null ? currentEnemyData.componentCount : 999))
                        VFXManager.Instance.PlayAttackHitVFX(battleUI.enemyKanjiText.transform.position);
                }
                break;

            case CardEffectType.Defense:
                gm.playerDefenseBuff += defenseValue;
                AddBattleLog($"『{card.DisplayName}』で防御力+{defenseValue}！");
                break;

            case CardEffectType.Heal:
                int healVal = card.effectValue + card.defenseModifier;
                gm.playerHP = Mathf.Min(gm.playerMaxHP, gm.playerHP + healVal);
                AddBattleLog($"『{card.DisplayName}』でHP{healVal}回復！");
                break;

            case CardEffectType.Buff:
                int buffVal = card.effectValue + card.attackModifier;
                gm.playerAttackBuff += buffVal;
                AddBattleLog($"『{card.DisplayName}』で攻撃力+{buffVal}！");
                break;

            case CardEffectType.Special:
                int spAtkVal = attackValue;
                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - spAtkVal);
                int healAmount = Mathf.CeilToInt(spAtkVal * 0.6f);
                gm.playerHP = Mathf.Min(gm.playerMaxHP, gm.playerHP + healAmount);
                AddBattleLog($"『{card.DisplayName}』で{spAtkVal}ダメージ＋{healAmount}回復！");
                if (battleUI != null && battleUI.enemyKanjiText != null && VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayDamageEffect(battleUI.enemyKanjiText.gameObject, spAtkVal);
                    // CFXR通常攻撃ヒットエフェクト
                    if (!isMirrorClash)
                        VFXManager.Instance.PlayAttackHitVFX(battleUI.enemyKanjiText.transform.position);
                }
                break;

            case CardEffectType.Draw:
                gm.DrawFromDeck(card.effectValue);
                AddBattleLog($"『{card.DisplayName}』でカードを{card.effectValue}枚ドロー！");
                break;

            case CardEffectType.AttackAll:
                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - attackValue);
                AddBattleLog($"『{card.DisplayName}』で敵全体に{attackValue}ダメージ！");
                if (battleUI != null && battleUI.enemyKanjiText != null && VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayDamageEffect(battleUI.enemyKanjiText.gameObject, attackValue);
                    // CFXR通常攻撃ヒットエフェクト
                    VFXManager.Instance.PlayAttackHitVFX(battleUI.enemyKanjiText.transform.position);
                }
                break;

            case CardEffectType.Stun:
                enemyIsStunned = true;
                AddBattleLog($"『{card.DisplayName}』で敵をスタンさせた！");
                break;
        }
    }


    /// <summary>
    /// ターン終了
    /// </summary>
    public void EndPlayerTurn()
    {
        if (battleState != BattleState.PlayerTurn) return;

        battleState = BattleState.EnemyTurn;
        isPlayerTurn = false;
        Debug.Log("[BattleManager] プレイヤーターン終了");

        // 敵ターン実行
        ExecuteEnemyTurn();
    }

    /// <summary>
    /// 敵ターン実行
    /// </summary>
    private void ExecuteEnemyTurn()
    {
        if (currentEnemyData == null) return;

        // スタンチェック
        if (enemyIsStunned)
        {
            AddBattleLog($"敵はスタンしていて動けない！");
            enemyIsStunned = false; // スタン解除
            
            // プレイヤーのターンへ戻る
            Invoke(nameof(ReturnToPlayerTurn), 1.0f);
            return;
        }

        int damage = currentEnemyData.attackPower;
        AddBattleLog($"敵の攻撃！ {damage}ダメージ！");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(damage);
        }

        Debug.Log($"[BattleManager] 敵が{damage}ダメージの攻撃");

        if (battleUI != null && VFXManager.Instance != null)
        {
            // プレイヤーへのダメージ演出（HPテキストを揺らす）
            GameObject target = battleUI.playerHPText != null ? battleUI.playerHPText.gameObject : battleUI.gameObject;
            VFXManager.Instance.PlayDamageEffect(target, damage, true);
        }

        CheckBattleEnd();

        if (battleState == BattleState.EnemyTurn)
        {
            // 戦闘継続 → プレイヤーターンへ
            battleState = BattleState.PlayerTurn;
            isAutoEndingTurn = false;
            isPlayerTurn = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartPlayerTurn();
            }
            turnStartTime = Time.time; // プレイヤーターン開始時刻を記録
        }

        UpdateUI();

        // BattleUI更新
        if (battleUI != null)
        {
            battleUI.UpdateHandUI();
            battleUI.UpdateStatusUI();
        }
    }

    private void ReturnToPlayerTurn()
    {
        battleState = BattleState.PlayerTurn;
        isAutoEndingTurn = false;
        isPlayerTurn = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartPlayerTurn();
        }
        
        turnStartTime = Time.time; // ターン開始時刻を記録
        UpdateUI();
    }

    /// <summary>
    /// 戦闘終了チェック
    /// </summary>
    private void CheckBattleEnd()
    {
        if (enemyCurrentHP <= 0)
        {
            battleState = BattleState.Won;
            int goldReward = 15;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.playerGold += goldReward;

                // ドロップカード処理：敵の漢字をインベントリに追加
                if (currentEnemyData.dropCard != null)
                {
                    bool added = GameManager.Instance.AddToInventory(currentEnemyData.dropCard);
                    if (added)
                    {
                        AddBattleLog($"<color=#FFD700>『{currentEnemyData.dropCard.kanji}』を手に入れた！</color>");
                    }
                    else
                    {
                        AddBattleLog($"<color=#FF6666>インベントリが満杯…『{currentEnemyData.dropCard.kanji}』を諦めた</color>");
                    }
                }
            }
            AddBattleLog($"勝利！ {goldReward}G獲得！");
            Debug.Log($"[BattleManager] 戦闘勝利！ {goldReward}G獲得");

            // CFXR敵討伐エフェクト再生後にフィールドへ戻る
            if (VFXManager.Instance != null && battleUI != null && battleUI.enemyKanjiText != null)
            {
                VFXManager.Instance.PlayEnemyDeathVFX(battleUI.enemyKanjiText.transform.position, () =>
                {
                    // 敵表示を非表示にしてからフィールドに戻る
                    if (battleUI.enemyArea != null) battleUI.enemyArea.SetActive(false);
                    ReturnToField();
                });
            }
            else
            {
                Invoke(nameof(ReturnToField), 1.5f);
            }
        }
        else if (GameManager.Instance != null && GameManager.Instance.playerHP <= 0)
        {
            battleState = BattleState.Lost;
            AddBattleLog("敗北...");
            Debug.Log("[BattleManager] 戦闘敗北...");
        }
    }

    private void ReturnToField()
    {
        // フィールドマネージャーに勝利通知
        if (GameManager.Instance != null && GameManager.Instance.fieldManager != null)
        {
            GameManager.Instance.fieldManager.OnBattleWon();
        }

        // 手札クリア（戦闘終了）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.hand.Clear();
            GameManager.Instance.ChangeState(GameState.Field);
        }
    }

    /// <summary>
    /// バトルログにテキストを追加
    /// </summary>
    private void AddBattleLog(string message)
    {
        if (battleLogText != null)
        {
            battleLogText.text = message + "\n" + battleLogText.text;
            // ログが長すぎたら切り詰め
            if (battleLogText.text.Length > 500)
            {
                battleLogText.text = battleLogText.text.Substring(0, 500);
            }
        }
    }

    /// <summary>
    /// 敵との強制合体（Enemy Fusion Break）
    /// </summary>
    public bool TryEnemyFusion(KanjiCardData playerCard)
    {
        var gm = GameManager.Instance;
        if (gm == null || currentEnemyData == null || playerCard == null) return false;

        // 敵の漢字データを取得
        var enemyCardData = gm.GetCardByKanji(currentEnemyData.displayKanji);
        if (enemyCardData == null) return false;

        // 合体レシピを検索
        int resultId = gm.FindFusionResult(enemyCardData.cardId, playerCard.cardId);
        if (resultId != -1)
        {
            var resultCard = gm.GetCardById(resultId);
            if (resultCard != null)
            {
                // 【タスク2-3】敵の姿と名前を変化させる
                AddBattleLog($"<color=#FF00FF><b>敵との強制合体！『{currentEnemyData.displayKanji}』＋『{playerCard.kanji}』＝『{resultCard.kanji}』</b></color>");
                
                // 敵の状態を更新
                currentEnemyData.displayKanji = resultCard.kanji;
                currentEnemyData.enemyName = resultCard.cardName + "化した敵";
                currentEnemyData.componentCount = resultCard.componentCount;
                
                // 変化した敵はスタン ＆ AP+1
                enemyIsStunned = true;
                gm.playerMana += 1;
                
                AddBattleLog($"敵はスタン状態になり、APが1回復した！");

                if (VFXManager.Instance != null && battleUI != null)
                {
                    VFXManager.Instance.PlayComboEffect(battleUI.gameObject, "ENEMY FUSION BREAK!!", Color.magenta);
                }

                // カードを消費（捨て札へ）
                gm.UseCard(playerCard);

                UpdateUI();
                if (battleUI != null)
                {
                    battleUI.UpdateHandUI();
                    battleUI.UpdateStatusUI();
                }
                
                CheckBattleEnd();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// オートターンエンドのチェック
    /// </summary>
    public void CheckAutoTurnEnd()
    {
        if (battleState != BattleState.PlayerTurn || isAutoEndingTurn) return;

        // セーフティ：ターン開始から一定時間は判定を行わない
        if (Time.time - turnStartTime < 1.0f) return;

        var gm = GameManager.Instance;
        
        // フェイルセーフ：山札・捨て札・手札がすべて空の場合
        if (gm.hand.Count == 0 && gm.drawPile.Count == 0 && gm.discardPile.Count == 0)
        {
            Debug.LogError("[BattleManager] 致命的なエラー：デッキが完全に空です。無限ループを防止するため強制終了します。");
            AddBattleLog("<color=red>エラー：カードが1枚もありません。戦闘を継続できません。</color>");
            isAutoEndingTurn = true;
            CheckBattleEnd(); // 敗北判定等に流れる可能性を考慮
            return;
        }

        bool anyActionPossible = false;
        
        // 1. 通常のカード使用チェック
        foreach (var card in gm.hand)
        {
            if (card.cost <= gm.playerMana)
            {
                anyActionPossible = true;
                break;
            }
        }

        // 2. 敵との合体チェック
        if (!anyActionPossible)
        {
            var enemyCardData = gm.GetCardByKanji(currentEnemyData.displayKanji);
            if (enemyCardData != null)
            {
                foreach (var card in gm.hand)
                {
                    if (gm.FindFusionResult(enemyCardData.cardId, card.cardId) != -1)
                    {
                        anyActionPossible = true;
                        break;
                    }
                }
            }
        }

        // 3. 手札同士の合体チェック
        if (!anyActionPossible)
        {
            for (int i = 0; i < gm.hand.Count; i++)
            {
                for (int j = i + 1; j < gm.hand.Count; j++)
                {
                    if (gm.FindFusionResult(gm.hand[i].cardId, gm.hand[j].cardId) != -1)
                    {
                        anyActionPossible = true;
                        break;
                    }
                }
                if (anyActionPossible) break;
            }
        }

        if (!anyActionPossible)
        {
            isAutoEndingTurn = true;
            AddBattleLog("<color=#AAAAAA>行動不能のため、自動的にターンを終了します...</color>");
            Invoke(nameof(EndPlayerTurn), 1.5f);
        }
    }


    public void UpdateUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (playerHPText != null) playerHPText.text = $"HP: {gm.playerHP}/{gm.playerMaxHP}";
        if (playerManaText != null) playerManaText.text = $"マナ: {gm.playerMana}/{gm.playerMaxMana}";

        // オートターンエンドのチェック
        CheckAutoTurnEnd();

        if (currentEnemyData != null)
        {
            if (enemyNameText != null) enemyNameText.text = $"{currentEnemyData.displayKanji} {currentEnemyData.enemyName}";
            if (enemyHPText != null) enemyHPText.text = $"HP: {enemyCurrentHP}/{currentEnemyData.maxHP}";
        }
    }

    /// <summary>
    /// ランダムな通常敵で戦闘開始
    /// </summary>
    public void StartRandomBattle()
    {
        if (normalEnemies != null && normalEnemies.Length > 0)
        {
            var enemy = normalEnemies[Random.Range(0, normalEnemies.Length)];
            StartBattle(enemy);
        }
        else
        {
            Debug.LogWarning("[BattleManager] 敵データが設定されていません");
        }
    }
}
