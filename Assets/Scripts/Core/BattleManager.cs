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
    public BattleState battleState = BattleState.Idle;

    [Header("UI参照")]
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI playerManaText;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyHPText;
    public TextMeshProUGUI battleLogText;
    public Button endTurnButton;
    public Transform handArea;

    [Header("敵データリスト")]
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

        currentEnemyData = enemy;
        enemyCurrentHP = enemy.maxHP;
        isPlayerTurn = true;
        battleState = BattleState.PlayerTurn;

        Debug.Log($"[BattleManager] 戦闘開始！ 敵:{enemy.enemyName}（HP:{enemy.maxHP}）");
        AddBattleLog($"『{enemy.displayKanji}』{enemy.enemyName}が現れた！");

        // GameManagerにステート変更を通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Battle);
            GameManager.Instance.StartPlayerTurn();
        }

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
    /// カード効果を適用
    /// </summary>
    private void ApplyCardEffect(KanjiCardData card)
    {
        var gm = GameManager.Instance;
        int value = card.effectValue + (card.effectType == CardEffectType.Attack ? gm.playerAttackBuff : 0);

        switch (card.effectType)
        {
            case CardEffectType.Attack:
                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - value);
                AddBattleLog($"『{card.kanji}』で{value}ダメージ！");
                Debug.Log($"[BattleManager] 敵に{value}ダメージ 残りHP:{enemyCurrentHP}");
                break;

            case CardEffectType.Defense:
                gm.playerDefenseBuff += card.effectValue;
                AddBattleLog($"『{card.kanji}』で防御力+{card.effectValue}！");
                break;

            case CardEffectType.Heal:
                gm.playerHP = Mathf.Min(gm.playerMaxHP, gm.playerHP + card.effectValue);
                AddBattleLog($"『{card.kanji}』でHP{card.effectValue}回復！");
                break;

            case CardEffectType.Buff:
                gm.playerAttackBuff += card.effectValue;
                AddBattleLog($"『{card.kanji}』で攻撃力+{card.effectValue}！");
                break;

            case CardEffectType.Special:
                // 特殊：ダメージ + 回復
                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - card.effectValue);
                int healAmount = Mathf.CeilToInt(card.effectValue * 0.6f);
                gm.playerHP = Mathf.Min(gm.playerMaxHP, gm.playerHP + healAmount);
                AddBattleLog($"『{card.kanji}』で{card.effectValue}ダメージ＋{healAmount}回復！");
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

        int damage = currentEnemyData.attackPower;
        AddBattleLog($"敵の攻撃！ {damage}ダメージ！");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(damage);
        }

        Debug.Log($"[BattleManager] 敵が{damage}ダメージの攻撃");

        CheckBattleEnd();

        if (battleState == BattleState.EnemyTurn)
        {
            // 戦闘継続 → プレイヤーターンへ
            battleState = BattleState.PlayerTurn;
            isPlayerTurn = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartPlayerTurn();
            }
        }

        UpdateUI();

        // BattleUI更新
        if (battleUI != null)
        {
            battleUI.UpdateHandUI();
            battleUI.UpdateStatusUI();
        }
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
            }
            AddBattleLog($"勝利！ {goldReward}G獲得！");
            Debug.Log($"[BattleManager] 戦闘勝利！ {goldReward}G獲得");

            // 少し待ってからマップに戻る
            Invoke(nameof(ReturnToMap), 1.5f);
        }
        else if (GameManager.Instance != null && GameManager.Instance.playerHP <= 0)
        {
            battleState = BattleState.Lost;
            AddBattleLog("敗北...");
            Debug.Log("[BattleManager] 戦闘敗北...");
        }
    }

    private void ReturnToMap()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Map);
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
    /// UI更新
    /// </summary>
    public void UpdateUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (playerHPText != null) playerHPText.text = $"HP: {gm.playerHP}/{gm.playerMaxHP}";
        if (playerManaText != null) playerManaText.text = $"マナ: {gm.playerMana}/{gm.playerMaxMana}";

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
