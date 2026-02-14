using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ゲーム内の全ビジュアルエフェクトを管理するマネージャー
/// Unity標準機能（Coroutine, Lerp, AnimationCurve）のみで実装
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Animation Curves")]
    [Tooltip("生成時のスケール変化（0 -> 1.2 -> 1.0 のようなボヨヨン演出）")]
    public AnimationCurve spawnCurve = new AnimationCurve(
        new Keyframe(0f, 0f), 
        new Keyframe(0.6f, 1.2f), 
        new Keyframe(1f, 1f));

    [Header("Settings")]
    public float fusionDuration = 0.5f; // 吸い寄せ時間
    public float shakeDuration = 0.25f; // シェイク時間
    public float shakeMagnitude = 10f; // シェイクの強さ（ピクセル）

    [Header("References")]
    public TMP_FontAsset appFont; // ダメージテキスト用フォント

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===========================================
    // 合体演出 (Fusion Sequence)
    // ===========================================

    /// <summary>
    /// 2枚のカードが合体して新しいカードになる演出
    /// </summary>
    public void PlayFusionSequence(CardController sourceCard, CardController targetCard, System.Action onComplete)
    {
        StartCoroutine(CoFusionSequence(sourceCard, targetCard, onComplete));
    }

    private IEnumerator CoFusionSequence(CardController source, CardController target, System.Action onComplete)
    {
        // 1. Convergence: 吸い寄せ
        float elapsed = 0f;
        Vector3 startPosSource = source.transform.position;
        Vector3 startPosTarget = target.transform.position;
        Vector3 centerPos = (startPosSource + startPosTarget) * 0.5f;

        // 回転の初期値と目標値
        Quaternion startRotSource = source.transform.rotation;
        Quaternion startRotTarget = target.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, 720); // 2回転させる

        // カードを手前に持ってくる（他のUIより上に）
        source.transform.SetAsLastSibling();
        target.transform.SetAsLastSibling();

        while (elapsed < fusionDuration)
        {
            float t = elapsed / fusionDuration;
            // イージング（EaseInBackっぽい動きで加速）
            float easeT = t * t * t; 

            source.transform.position = Vector3.Lerp(startPosSource, centerPos, easeT);
            target.transform.position = Vector3.Lerp(startPosTarget, centerPos, easeT);

            // 回転演出
            source.transform.rotation = Quaternion.Lerp(startRotSource, targetRot, easeT);
            target.transform.rotation = Quaternion.Lerp(startRotTarget, targetRot, easeT);

            // 縮小演出（近づくにつれて小さく）
            float scale = Mathf.Lerp(1f, 0.2f, easeT);
            source.transform.localScale = Vector3.one * scale;
            target.transform.localScale = Vector3.one * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. Shrink & Flash: 消滅と閃光
        // ここで一旦カードを非表示にする（まだDestoryはしない）
        source.gameObject.SetActive(false);
        target.gameObject.SetActive(false);

        // 閃光エフェクト
        PlayFlashEffectAt(centerPos);

        // 3. Callback: データ更新と新カード生成
        // ここでコールバックを呼び、GameManager側で古いカードの削除と新カードの作成を行わせる
        // 新カードの生成時に PlaySpawnEffect を呼んでもらう想定
        onComplete?.Invoke();
    }

    private void PlayFlashEffectAt(Vector3 position)
    {
        // シンプルな円形閃光を動的生成
        GameObject flashObj = new GameObject("FusionFlash");
        flashObj.transform.SetParent(transform.parent); // Canvas下へ
        flashObj.transform.position = position;
        
        var img = flashObj.AddComponent<Image>();
        img.sprite = null; // デフォルトの白四角（または円形Spriteがあればいいが、無ければ四角で良し）
        img.color = new Color(1f, 1f, 0.8f, 1f); // 少し黄色がかった白
        
        // 円形に見せるためマスク用スプライトがあればいいが、なければ標準リソースを使うか、単純にRectで表現
        // 今回は標準のKnobなどを使えると良いが、確実に存在するか不明なので、四角を回転させてごまかすか、単に四角で良い。
        
        StartCoroutine(CoFlashAnimation(flashObj));
    }

    private IEnumerator CoFlashAnimation(GameObject obj)
    {
        var rect = obj.GetComponent<RectTransform>();
        var img = obj.GetComponent<Image>();
        rect.sizeDelta = new Vector2(100f, 100f);

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 急激に拡大してフェードアウト
            float scale = Mathf.Lerp(0.5f, 3.0f, t);
            float alpha = Mathf.Lerp(1f, 0f, t * t);

            obj.transform.localScale = Vector3.one * scale;
            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }

    // ===========================================
    // 生成演出 (Spawn)
    // ===========================================

    /// <summary>
    /// オブジェクト出現時のボヨヨン演出
    /// </summary>
    public void PlaySpawnEffect(GameObject target)
    {
        if (target == null) return;
        StartCoroutine(CoSpawnEffect(target));
    }

    private IEnumerator CoSpawnEffect(GameObject target)
    {
        // 安全のため初期スケールを0に
        target.transform.localScale = Vector3.zero;

        float duration = 0.6f; // カーブ依存だが全体時間として
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;

            float t = elapsed / duration;
            float scale = spawnCurve.Evaluate(t);
            target.transform.localScale = Vector3.one * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null) target.transform.localScale = Vector3.one; // 最終的に1に戻す
    }

    // ===========================================
    // 出現予約システム
    // ===========================================
    private HashSet<KanjiCardData> spawnEffectTargets = new HashSet<KanjiCardData>();

    /// <summary>
    /// 次回生成時にボヨヨン演出を行うカードを予約
    /// </summary>
    public void RegisterSpawnEffect(KanjiCardData card)
    {
        if (card != null) spawnEffectTargets.Add(card);
    }

    /// <summary>
    /// 予約されていたらボヨヨン演出を実行
    /// </summary>
    public void CheckAndPlaySpawnEffect(CardController controller)
    {
        if (controller != null && controller.cardData != null && spawnEffectTargets.Contains(controller.cardData))
        {
            PlaySpawnEffect(controller.gameObject);
            spawnEffectTargets.Remove(controller.cardData);
        }
    }

    // ===========================================
    // 戦闘フィードバック (Combat Feedback)
    // ===========================================

    /// <summary>
    /// ダメージ演出（シェイク、フラッシュ、テキスト）
    /// </summary>
    public void PlayDamageEffect(GameObject target, int damage, bool isPlayer = false)
    {
        if (target != null)
        {
            // 1. シェイク
            StartCoroutine(CoShakeEffect(target));
            
            // 2. フラッシュ
            var graphic = target.GetComponent<Graphic>(); // Image or RawImage
            if (graphic != null)
            {
                StartCoroutine(CoDamageFlash(graphic));
            }

            // 3. テキスト
            // ターゲットの位置周辺から飛び散る
            SpawnDamageText(target.transform.position, damage, isPlayer);
        }
        else if (isPlayer)
        {
            // ターゲットオブジェクトがない場合（プレイヤーへの攻撃などUI全体を揺らすなどのケース）
            // 今回はスキップ、または画面全体フラッシュなどを入れる余地あり
        }
    }

    private IEnumerator CoShakeEffect(GameObject target)
    {
        var rect = target.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector2 originalPos = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // ランダムにずらす
            Vector2 offset = Random.insideUnitCircle * shakeMagnitude;
            rect.anchoredPosition = originalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = originalPos;
    }

    private IEnumerator CoDamageFlash(Graphic target)
    {
        Color originalColor = target.color;
        float duration = 0.2f;
        
        // 赤く光らせる
        target.color = new Color(1f, 0.3f, 0.3f, 1f);
        
        yield return new WaitForSeconds(0.05f);

        // 徐々に戻る
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            target.color = Color.Lerp(new Color(1f, 0.3f, 0.3f, 1f), originalColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        target.color = originalColor;
    }

    private void SpawnDamageText(Vector3 position, int damage, bool isPlayer)
    {
        GameObject textObj = new GameObject($"DamageText_{damage}");
        textObj.transform.SetParent(transform.parent); // Canvas直下
        textObj.transform.position = position;
        
        // 少し手前に
        textObj.transform.SetAsLastSibling();

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = damage.ToString();
        tmp.fontSize = isPlayer ? 40 : 50;
        tmp.color = isPlayer ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.8f, 0.2f); // プレイヤー被弾赤、敵被弾黄色
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        if (appFont != null) tmp.font = appFont;
        
        // アウトライン
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        StartCoroutine(CoFloatingText(textObj));
    }

    private IEnumerator CoFloatingText(GameObject obj)
    {
        float duration = 0.8f;
        float elapsed = 0f;
        
        Vector3 velocity = new Vector3(Random.Range(-50f, 50f), Random.Range(150f, 250f), 0f);
        Vector3 gravity = new Vector3(0, -800f, 0);

        var tmp = obj.GetComponent<TextMeshProUGUI>();

        while (elapsed < duration)
        {
            if (obj == null) yield break;

            // 物理挙動シミュレーション
            obj.transform.localPosition += velocity * Time.deltaTime;
            velocity += gravity * Time.deltaTime;

            // フェードアウト
            if (elapsed > duration * 0.6f)
            {
                float alpha = 1f - (elapsed - duration * 0.6f) / (duration * 0.4f);
                tmp.alpha = alpha;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }
}
