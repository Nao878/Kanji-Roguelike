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

    [Header("CFXR Battle Effects")]
    [Tooltip("通常攻撃のヒット用エフェクト")]
    public GameObject attackHitEffect;
    [Tooltip("相殺やマウント等の特大ダメージ用エフェクト")]
    public GameObject criticalHitEffect;
    [Tooltip("合体成功時用エフェクト")]
    public GameObject fusionCFXREffect;
    [Tooltip("敵討伐時用エフェクト")]
    public GameObject enemyDeathEffect;

    // パーティクル生成用カメラ参照
    private Camera mainCamera;

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

        mainCamera = Camera.main;
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
    // 合体成功演出 (Fusion Success - Slow-mo & Highlight)
    // ===========================================

    /// <summary>
    /// 合体成功時の強調演出（スローモーションなど）
    /// </summary>
    public void PlayFusionSuccessEffect(Vector3 position)
    {
        StartCoroutine(CoFusionSuccessEffect(position));
    }

    private IEnumerator CoFusionSuccessEffect(Vector3 position)
    {
        // 1. スローモーション開始
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.2f;

        // 2. 特殊エフェクト生成
        GameObject successObj = new GameObject("FusionSuccessHighlight");
        successObj.transform.SetParent(transform.parent);
        successObj.transform.position = position;
        
        var img = successObj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 0f, 0.5f); // 黄色い光
        
        var rect = successObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 200f);

        float duration = 0.5f; // 実時間（TimeScale無関係のWaitForSecondsRealtime用）
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 拡大しながらフェードアウト
            float scale = Mathf.Lerp(1.0f, 5.0f, t);
            float alpha = Mathf.Lerp(0.5f, 0f, t);

            successObj.transform.localScale = Vector3.one * scale;
            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);

            // TimeScaleが小さいので、RealtimeDeltaTimeを使う
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(successObj);

        // 3. スローモーション終了
        Time.timeScale = originalTimeScale;
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

    // ===========================================
    // コンボ演出 (Combo Feedback)
    // ===========================================

    public void PlayComboEffect(GameObject target, string text, Color color)
    {
        if (target == null) return;
        
        GameObject textObj = new GameObject($"ComboText");
        textObj.transform.SetParent(transform.parent); // Canvas直下
        // ターゲットより少し上に配置
        textObj.transform.position = target.transform.position + Vector3.up * 80f;
        textObj.transform.SetAsLastSibling();

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 60;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (appFont != null) tmp.font = appFont;
        
        // アウトライン
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = Color.black;

        StartCoroutine(CoComboText(textObj));
    }

    // ===========================================
    // 「1 MORE」巨大演出 (Fusion Bonus)
    // ===========================================

    /// <summary>
    /// 合体成功時に画面中央に大きく「1 MORE」を表示する演出
    /// </summary>
    public void PlayOneMoreEffect()
    {
        // Canvas直下に配置
        Transform canvasParent = transform.parent;
        if (canvasParent == null)
        {
            // VFXManagerがCanvas以下にない場合、シーンのCanvasを探す
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) canvasParent = canvas.transform;
            else canvasParent = transform;
        }

        // 背景暗転（半透明黒パネル）
        GameObject dimObj = new GameObject("OneMoreDim");
        dimObj.transform.SetParent(canvasParent, false);
        dimObj.transform.SetAsLastSibling();
        var dimRect = dimObj.AddComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        var dimImg = dimObj.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0f); // 初期は透明
        dimImg.raycastTarget = false;

        // テキストオブジェクト
        GameObject textObj = new GameObject("OneMoreText");
        textObj.transform.SetParent(canvasParent, false);
        textObj.transform.SetAsLastSibling();

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(600f, 200f);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "1 MORE";
        tmp.fontSize = 120;
        tmp.color = new Color(1f, 0.84f, 0f, 1f); // ゴールド (#FFD700)
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (appFont != null) tmp.font = appFont;

        // アウトライン（太め）
        tmp.outlineWidth = 0.4f;
        tmp.outlineColor = new Color(0.2f, 0f, 0f, 1f); // 暗い赤

        // 影（筆文字風の雰囲気）
        tmp.enableVertexGradient = true;
        tmp.colorGradient = new VertexGradient(
            new Color(1f, 0.95f, 0.4f),   // 左上
            new Color(1f, 0.95f, 0.4f),   // 右上
            new Color(1f, 0.6f, 0.1f),    // 左下
            new Color(1f, 0.6f, 0.1f)     // 右下
        );

        StartCoroutine(CoOneMoreEffect(dimObj, textObj));
    }

    private IEnumerator CoOneMoreEffect(GameObject dimObj, GameObject textObj)
    {
        float totalDuration = 1.8f;
        float elapsed = 0f;

        var dimImg = dimObj.GetComponent<Image>();
        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        var textRect = textObj.GetComponent<RectTransform>();

        // 初期状態：スケール0
        textObj.transform.localScale = Vector3.zero;
        Vector2 startPos = textRect.anchoredPosition;

        while (elapsed < totalDuration)
        {
            if (textObj == null || dimObj == null) yield break;

            float t = elapsed / totalDuration;

            // --- Phase 1: 出現（0〜0.15） ボヨヨンスケール ---
            if (t < 0.15f)
            {
                float phase = t / 0.15f;
                float scale = Mathf.Lerp(0f, 2.0f, phase);
                textObj.transform.localScale = Vector3.one * scale;
                // 暗転もフェードイン
                dimImg.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.3f, phase));
            }
            // --- Phase 2: バウンス戻し（0.15〜0.25） ---
            else if (t < 0.25f)
            {
                float phase = (t - 0.15f) / 0.1f;
                float scale = Mathf.Lerp(2.0f, 0.9f, phase);
                textObj.transform.localScale = Vector3.one * scale;
            }
            // --- Phase 3: 安定化（0.25〜0.35） ---
            else if (t < 0.35f)
            {
                float phase = (t - 0.25f) / 0.1f;
                float scale = Mathf.Lerp(0.9f, 1.1f, phase);
                textObj.transform.localScale = Vector3.one * scale;
            }
            // --- Phase 4: 最終スケール固定＋表示維持（0.35〜0.65） ---
            else if (t < 0.65f)
            {
                textObj.transform.localScale = Vector3.one * 1.1f;
            }
            // --- Phase 5: フェードアウト + 上昇（0.65〜1.0） ---
            else
            {
                float phase = (t - 0.65f) / 0.35f;
                float alpha = Mathf.Lerp(1f, 0f, phase);
                tmp.alpha = alpha;
                dimImg.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.3f, 0f, phase));

                // 上方向に移動
                float yOffset = Mathf.Lerp(0f, 80f, phase);
                textRect.anchoredPosition = startPos + new Vector2(0, yOffset);

                // 少しスケールアップ
                float scale = Mathf.Lerp(1.1f, 1.3f, phase);
                textObj.transform.localScale = Vector3.one * scale;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(dimObj);
        Destroy(textObj);
    }

    private IEnumerator CoComboText(GameObject obj)
    {
        float duration = 1.2f;
        float elapsed = 0f;
        
        Vector3 velocity = new Vector3(0f, 100f, 0f); // 真上にゆっくり
        var tmp = obj.GetComponent<TextMeshProUGUI>();

        // スケールアニメーション
        obj.transform.localScale = Vector3.one * 0.1f;

        while (elapsed < duration)
        {
            if (obj == null) yield break;

            float t = elapsed / duration;

            // スケール：ボヨヨン
            if (t < 0.2f)
            {
                float scale = Mathf.Lerp(0.1f, 1.5f, t / 0.2f);
                obj.transform.localScale = Vector3.one * scale;
            }
            else if (t < 0.3f)
            {
                float scale = Mathf.Lerp(1.5f, 1.0f, (t - 0.2f) / 0.1f);
                obj.transform.localScale = Vector3.one * scale;
            }

            // 移動
            obj.transform.localPosition += velocity * Time.deltaTime;

            // フェードアウト
            if (t > 0.7f)
            {
                float alpha = 1f - (t - 0.7f) / 0.3f;
                tmp.alpha = alpha;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }


/// <summary>
    /// 「合体不可」ポップアップテキストを指定位置の上に表示し、フェードアウトしながら上昇して消える
    /// </summary>
    public void PlayNoFusionPopup(Vector3 worldPosition)
    {
        Transform canvasParent = transform.parent;
        if (canvasParent == null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) canvasParent = canvas.transform;
            else return;
        }

        GameObject textObj = new GameObject("NoFusionPopup");
        textObj.transform.SetParent(canvasParent, false);
        textObj.transform.position = worldPosition + Vector3.up * 60f;
        textObj.transform.SetAsLastSibling();

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "合体不可";
        tmp.fontSize = 32;
        tmp.color = new Color(1f, 0.35f, 0.35f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (appFont != null) tmp.font = appFont;

        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color(0.2f, 0f, 0f, 1f);

        var rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 50f);

        StartCoroutine(CoNoFusionPopup(textObj));
    }

    private IEnumerator CoNoFusionPopup(GameObject obj)
    {
        float duration = 1.0f;
        float elapsed = 0f;

        var tmp = obj.GetComponent<TextMeshProUGUI>();
        Vector3 startPos = obj.transform.localPosition;

        // 初期: 小さく出現 → 拡大
        obj.transform.localScale = Vector3.one * 0.5f;

        while (elapsed < duration)
        {
            if (obj == null) yield break;

            float t = elapsed / duration;

            // スケール: ボヨヨン出現 (0~0.15)
            if (t < 0.15f)
            {
                float scale = Mathf.Lerp(0.5f, 1.15f, t / 0.15f);
                obj.transform.localScale = Vector3.one * scale;
            }
            else if (t < 0.25f)
            {
                float scale = Mathf.Lerp(1.15f, 1.0f, (t - 0.15f) / 0.1f);
                obj.transform.localScale = Vector3.one * scale;
            }

            // 上方向にゆっくり移動
            float yOffset = Mathf.Lerp(0f, 50f, t);
            obj.transform.localPosition = startPos + new Vector3(0, yOffset, 0);

            // フェードアウト (後半40%)
            if (t > 0.6f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
                tmp.alpha = alpha;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }

    // ===========================================
    // CFXR パーティクルエフェクト (Cartoon FX Remaster)
    // ===========================================

    /// <summary>
    /// UI座標（Screen Space - Camera モード）からパーティクル生成用の3Dワールド座標を計算
    /// パーティクルはCanvas（planeDistance=10）よりカメラ寄り（Z=5）に配置
    /// </summary>
    private Vector3 GetParticleWorldPosition(Vector3 uiWorldPosition)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return uiWorldPosition;

        // Screen Space - Camera モード: UI要素のワールド座標をスクリーン座標に変換
        Vector3 screenPos = mainCamera.WorldToScreenPoint(uiWorldPosition);
        // パーティクルをCanvasより手前（カメラ寄り）に配置
        // Canvas planeDistance=10 なので、カメラからの距離5にパーティクルを配置
        screenPos.z = 5f;
        return mainCamera.ScreenToWorldPoint(screenPos);
    }

    /// <summary>
    /// CFXRパーティクルを指定位置に生成し、自動破棄設定を行う
    /// </summary>
    private GameObject SpawnCFXREffect(GameObject prefab, Vector3 uiWorldPosition)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[VFXManager] CFXR prefab is null — エフェクトをスキップ");
            return null;
        }

        Vector3 spawnPos = GetParticleWorldPosition(uiWorldPosition);
        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        // スケールをリセット（親オブジェクトのスケーリングに影響されないように）
        instance.transform.localScale = Vector3.one;

        Debug.Log($"[VFXManager] CFXR Effect Spawned: {prefab.name} at world={spawnPos} (ui={uiWorldPosition})");

        // フォールバック自動破棄（CFXRのClearBehavior.Destroyが効かない場合の保険）
        Destroy(instance, 5f);

        return instance;
    }

    /// <summary>
    /// 通常攻撃ヒット時のCFXRエフェクト再生
    /// </summary>
    public void PlayAttackHitVFX(Vector3 uiWorldPosition)
    {
        SpawnCFXREffect(attackHitEffect, uiWorldPosition);
    }

    /// <summary>
    /// 特大ダメージ（相殺・マウント等）時のCFXRエフェクト再生 + 強カメラシェイク
    /// </summary>
    public void PlayCriticalHitVFX(Vector3 uiWorldPosition)
    {
        SpawnCFXREffect(criticalHitEffect, uiWorldPosition);
        PlayCameraShake(20f, 0.4f);
    }

    /// <summary>
    /// 合体成功時のCFXRパーティクルエフェクト再生
    /// </summary>
    public void PlayFusionCFXR(Vector3 uiWorldPosition)
    {
        SpawnCFXREffect(fusionCFXREffect, uiWorldPosition);
    }

    /// <summary>
    /// 敵討伐時のCFXRエフェクト再生（再生後コールバック付き）
    /// </summary>
    public void PlayEnemyDeathVFX(Vector3 uiWorldPosition, System.Action onComplete = null)
    {
        var instance = SpawnCFXREffect(enemyDeathEffect, uiWorldPosition);
        if (instance != null)
        {
            StartCoroutine(CoWaitAndCallback(1.2f, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private IEnumerator CoWaitAndCallback(float delay, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    // ===========================================
    // カメラシェイク強化 (Canvas全体を揺らす)
    // ===========================================

    /// <summary>
    /// Canvas全体を揺らすカメラシェイク演出
    /// </summary>
    public void PlayCameraShake(float magnitude, float duration)
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            StartCoroutine(CoCameraShake(canvas.GetComponent<RectTransform>(), magnitude, duration));
        }
    }

    private IEnumerator CoCameraShake(RectTransform canvasRect, float magnitude, float duration)
    {
        if (canvasRect == null) yield break;

        Vector2 originalPos = canvasRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float decreaseFactor = 1f - (elapsed / duration); // 徐々に弱まる
            Vector2 offset = Random.insideUnitCircle * magnitude * decreaseFactor;
            canvasRect.anchoredPosition = originalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasRect.anchoredPosition = originalPos;
    }
}
