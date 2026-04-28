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
    public float fusionDuration = 0.5f;
    public float shakeDuration = 0.25f;
    public float shakeMagnitude = 10f;

    [Header("References")]
    public TMP_FontAsset appFont;

    [Header("CFXR Battle Effects")]
    [Tooltip("通常攻撃のヒット用エフェクト")]
    public GameObject attackHitEffect;
    [Tooltip("相殺やマウント等の特大ダメージ用エフェクト")]
    public GameObject criticalHitEffect;
    [Tooltip("合体成功時用エフェクト")]
    public GameObject fusionCFXREffect;
    [Tooltip("敵討伐時用エフェクト")]
    public GameObject enemyDeathEffect;

    [Header("CFXR Card Effect VFX")]
    [Tooltip("回復用エフェクト（緑色の魔法陣など）")]
    public GameObject healEffect;
    [Tooltip("防御力アップ用エフェクト（青色の盾やオーラなど）")]
    public GameObject defenseEffect;
    [Tooltip("スタン用エフェクト（電撃など）")]
    public GameObject stunEffect;

    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        mainCamera = Camera.main;
    }

    // ===========================================
    // Canvas座標変換ヘルパー
    // ===========================================

    private Transform GetCanvasParent()
    {
        Transform canvasParent = transform.parent;
        if (canvasParent != null)
        {
            var parentCanvas = canvasParent.GetComponentInParent<Canvas>();
            if (parentCanvas != null) return parentCanvas.transform;
        }
        var canvas = FindFirstObjectByType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    private void SetLocalPositionFromWorld(RectTransform rect, Vector3 worldPosition, Transform canvasParent)
    {
        if (rect == null || canvasParent == null) return;
        Canvas canvas = canvasParent.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasParent.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;
        if (cam == null) cam = mainCamera;
        if (cam == null) cam = Camera.main;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
        RectTransform canvasRect = canvasParent as RectTransform;
        if (canvasRect == null) canvasRect = canvasParent.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out localPoint);
        rect.anchoredPosition = localPoint;
    }

    // ===========================================
    // 合体演出 (Fusion Sequence)
    // ===========================================

    public void PlayFusionSequence(CardController sourceCard, CardController targetCard, System.Action onComplete)
    {
        StartCoroutine(CoFusionSequence(sourceCard, targetCard, onComplete));
    }

    private IEnumerator CoFusionSequence(CardController source, CardController target, System.Action onComplete)
    {
        float elapsed = 0f;
        Vector3 startPosSource = source.transform.position;
        Vector3 startPosTarget = target.transform.position;
        Vector3 centerPos = (startPosSource + startPosTarget) * 0.5f;
        Quaternion startRotSource = source.transform.rotation;
        Quaternion startRotTarget = target.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, 720);
        source.transform.SetAsLastSibling();
        target.transform.SetAsLastSibling();

        while (elapsed < fusionDuration)
        {
            float t = elapsed / fusionDuration;
            float easeT = t * t * t;
            source.transform.position = Vector3.Lerp(startPosSource, centerPos, easeT);
            target.transform.position = Vector3.Lerp(startPosTarget, centerPos, easeT);
            source.transform.rotation = Quaternion.Lerp(startRotSource, targetRot, easeT);
            target.transform.rotation = Quaternion.Lerp(startRotTarget, targetRot, easeT);
            float scale = Mathf.Lerp(1f, 0.2f, easeT);
            source.transform.localScale = Vector3.one * scale;
            target.transform.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        source.gameObject.SetActive(false);
        target.gameObject.SetActive(false);
        PlayFlashEffectAt(centerPos);
        onComplete?.Invoke();
    }

    private void PlayFlashEffectAt(Vector3 position)
    {
        GameObject flashObj = new GameObject("FusionFlash");
        flashObj.transform.SetParent(transform.parent);
        flashObj.transform.position = position;
        var img = flashObj.AddComponent<Image>();
        img.sprite = null;
        img.color = new Color(1f, 1f, 0.8f, 1f);
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
    // 合体成功演出
    // ===========================================

    public void PlayFusionSuccessEffect(Vector3 position)
    {
        StartCoroutine(CoFusionSuccessEffect(position));
    }

    private IEnumerator CoFusionSuccessEffect(Vector3 position)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.2f;
        GameObject successObj = new GameObject("FusionSuccessHighlight");
        successObj.transform.SetParent(transform.parent);
        successObj.transform.position = position;
        var img = successObj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 0f, 0.5f);
        var rect = successObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 200f);
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1.0f, 5.0f, t);
            float alpha = Mathf.Lerp(0.5f, 0f, t);
            successObj.transform.localScale = Vector3.one * scale;
            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Destroy(successObj);
        Time.timeScale = originalTimeScale;
    }

    // ===========================================
    // 生成演出 (Spawn)
    // ===========================================

    public void PlaySpawnEffect(GameObject target)
    {
        if (target == null) return;
        StartCoroutine(CoSpawnEffect(target));
    }

    private IEnumerator CoSpawnEffect(GameObject target)
    {
        target.transform.localScale = Vector3.zero;
        float duration = 0.6f;
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
        if (target != null) target.transform.localScale = Vector3.one;
    }

    // ===========================================
    // 出現予約システム
    // ===========================================
    private HashSet<KanjiCardData> spawnEffectTargets = new HashSet<KanjiCardData>();

    public void RegisterSpawnEffect(KanjiCardData card)
    {
        if (card != null) spawnEffectTargets.Add(card);
    }

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

    public void PlayDamageEffect(GameObject target, int damage, bool isPlayer = false)
    {
        if (target != null)
        {
            StartCoroutine(CoShakeEffect(target));
            var graphic = target.GetComponent<Graphic>();
            if (graphic != null) StartCoroutine(CoDamageFlash(graphic));
            SpawnDamageText(target.transform.position, damage, isPlayer);
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
        target.color = new Color(1f, 0.3f, 0.3f, 1f);
        yield return new WaitForSeconds(0.05f);
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
        Transform canvasParent = GetCanvasParent();
        if (canvasParent == null) return;

        GameObject textObj = new GameObject($"DamageText_{damage}");
        textObj.transform.SetParent(canvasParent, false);
        textObj.transform.SetAsLastSibling();

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = damage.ToString();
        tmp.fontSize = isPlayer ? 40 : 50;
        tmp.color = isPlayer ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.8f, 0.2f);
        tmp.alignment = TextAlignmentOptions.Center;
        if (appFont != null) tmp.font = appFont;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        SetLocalPositionFromWorld(textObj.GetComponent<RectTransform>(), position, canvasParent);
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
            obj.transform.localPosition += velocity * Time.deltaTime;
            velocity += gravity * Time.deltaTime;
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
        Transform canvasParent = GetCanvasParent();
        if (canvasParent == null) return;

        GameObject textObj = new GameObject("ComboText");
        textObj.transform.SetParent(canvasParent, false);
        textObj.transform.SetAsLastSibling();

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 60;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (appFont != null) tmp.font = appFont;
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = Color.black;

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(400f, 100f);
        SetLocalPositionFromWorld(textRect, target.transform.position, canvasParent);
        textRect.anchoredPosition += new Vector2(0, 80f);

        StartCoroutine(CoComboText(textObj));
    }

    private IEnumerator CoComboText(GameObject obj)
    {
        float duration = 1.2f;
        float elapsed = 0f;
        Vector3 velocity = new Vector3(0f, 100f, 0f);
        var tmp = obj.GetComponent<TextMeshProUGUI>();
        obj.transform.localScale = Vector3.one * 0.1f;
        while (elapsed < duration)
        {
            if (obj == null) yield break;
            float t = elapsed / duration;
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
            obj.transform.localPosition += velocity * Time.deltaTime;
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

    // ===========================================
    // 「1 MORE」巨大演出 (Fusion Bonus)
    // ===========================================

    public void PlayOneMoreEffect()
    {
        Transform canvasParent = GetCanvasParent();
        if (canvasParent == null) canvasParent = transform;

        GameObject dimObj = new GameObject("OneMoreDim");
        dimObj.transform.SetParent(canvasParent, false);
        dimObj.transform.SetAsLastSibling();
        var dimRect = dimObj.AddComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        var dimImg = dimObj.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0f);
        dimImg.raycastTarget = false;

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
        tmp.color = new Color(1f, 0.84f, 0f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (appFont != null) tmp.font = appFont;
        tmp.outlineWidth = 0.4f;
        tmp.outlineColor = new Color(0.2f, 0f, 0f, 1f);
        tmp.enableVertexGradient = true;
        tmp.colorGradient = new VertexGradient(
            new Color(1f, 0.95f, 0.4f), new Color(1f, 0.95f, 0.4f),
            new Color(1f, 0.6f, 0.1f), new Color(1f, 0.6f, 0.1f));

        StartCoroutine(CoOneMoreEffect(dimObj, textObj));
    }

    private IEnumerator CoOneMoreEffect(GameObject dimObj, GameObject textObj)
    {
        float totalDuration = 1.8f;
        float elapsed = 0f;
        var dimImg = dimObj.GetComponent<Image>();
        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        var textRect = textObj.GetComponent<RectTransform>();
        textObj.transform.localScale = Vector3.zero;
        Vector2 startPos = textRect.anchoredPosition;

        while (elapsed < totalDuration)
        {
            if (textObj == null || dimObj == null) yield break;
            float t = elapsed / totalDuration;
            if (t < 0.15f)
            {
                float phase = t / 0.15f;
                textObj.transform.localScale = Vector3.one * Mathf.Lerp(0f, 2.0f, phase);
                dimImg.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.3f, phase));
            }
            else if (t < 0.25f)
            {
                textObj.transform.localScale = Vector3.one * Mathf.Lerp(2.0f, 0.9f, (t - 0.15f) / 0.1f);
            }
            else if (t < 0.35f)
            {
                textObj.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.1f, (t - 0.25f) / 0.1f);
            }
            else if (t < 0.65f)
            {
                textObj.transform.localScale = Vector3.one * 1.1f;
            }
            else
            {
                float phase = (t - 0.65f) / 0.35f;
                tmp.alpha = Mathf.Lerp(1f, 0f, phase);
                dimImg.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.3f, 0f, phase));
                textRect.anchoredPosition = startPos + new Vector2(0, Mathf.Lerp(0f, 80f, phase));
                textObj.transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1.3f, phase);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(dimObj);
        Destroy(textObj);
    }

    // ===========================================
    // 「合体不可」ポップアップ（Screen Space - Camera対応）
    // ===========================================

    public void PlayNoFusionPopup(Vector3 worldPosition)
    {
        Transform canvasParent = GetCanvasParent();
        if (canvasParent == null) return;

        GameObject textObj = new GameObject("NoFusionPopup");
        textObj.transform.SetParent(canvasParent, false);
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
        SetLocalPositionFromWorld(rect, worldPosition, canvasParent);
        rect.anchoredPosition += new Vector2(0, 60f);

        Debug.Log($"[VFXManager] 合体不可ポップアップ表示 at local={rect.anchoredPosition}");
        StartCoroutine(CoNoFusionPopup(textObj));
    }

    private IEnumerator CoNoFusionPopup(GameObject obj)
    {
        float duration = 1.0f;
        float elapsed = 0f;
        var tmp = obj.GetComponent<TextMeshProUGUI>();
        Vector3 startPos = obj.transform.localPosition;
        obj.transform.localScale = Vector3.one * 0.5f;

        while (elapsed < duration)
        {
            if (obj == null) yield break;
            float t = elapsed / duration;
            if (t < 0.15f)
                obj.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.15f, t / 0.15f);
            else if (t < 0.25f)
                obj.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1.0f, (t - 0.15f) / 0.1f);
            obj.transform.localPosition = startPos + new Vector3(0, Mathf.Lerp(0f, 50f, t), 0);
            if (t > 0.6f)
                tmp.alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }

    // ===========================================
    // CFXR パーティクルエフェクト (Cartoon FX Remaster)
    // ===========================================

    private Vector3 GetParticleWorldPosition(Vector3 uiWorldPosition)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return uiWorldPosition;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(uiWorldPosition);
        screenPos.z = 5f;
        return mainCamera.ScreenToWorldPoint(screenPos);
    }

    private GameObject SpawnCFXREffect(GameObject prefab, Vector3 uiWorldPosition)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[VFXManager] CFXR prefab is null — エフェクトをスキップ");
            return null;
        }
        Vector3 spawnPos = GetParticleWorldPosition(uiWorldPosition);
        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        Debug.Log($"[VFXManager] CFXR Effect Spawned: {prefab.name} at world={spawnPos}");
        Destroy(instance, 5f);
        return instance;
    }

    public void PlayAttackHitVFX(Vector3 uiWorldPosition) => SpawnCFXREffect(attackHitEffect, uiWorldPosition);

    public void PlayCriticalHitVFX(Vector3 uiWorldPosition)
    {
        SpawnCFXREffect(criticalHitEffect, uiWorldPosition);
        PlayCameraShake(20f, 0.4f);
    }

    public void PlayFusionCFXR(Vector3 uiWorldPosition) => SpawnCFXREffect(fusionCFXREffect, uiWorldPosition);

    /// <summary>回復エフェクト再生（プレイヤー座標）</summary>
    public void PlayHealVFX(Vector3 uiWorldPosition) => SpawnCFXREffect(healEffect, uiWorldPosition);

    /// <summary>防御エフェクト再生（プレイヤー座標）</summary>
    public void PlayDefenseVFX(Vector3 uiWorldPosition) => SpawnCFXREffect(defenseEffect, uiWorldPosition);

    /// <summary>スタンエフェクト再生（敵座標）</summary>
    public void PlayStunVFX(Vector3 uiWorldPosition) => SpawnCFXREffect(stunEffect, uiWorldPosition);

    public void PlayEnemyDeathVFX(Vector3 uiWorldPosition, System.Action onComplete = null)
    {
        var instance = SpawnCFXREffect(enemyDeathEffect, uiWorldPosition);
        if (instance != null) StartCoroutine(CoWaitAndCallback(1.2f, onComplete));
        else onComplete?.Invoke();
    }

    private IEnumerator CoWaitAndCallback(float delay, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    // ===========================================
    // カメラシェイク強化
    // ===========================================

    public void PlayCameraShake(float magnitude, float duration)
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
            StartCoroutine(CoCameraShake(canvas.GetComponent<RectTransform>(), magnitude, duration));
    }

    private IEnumerator CoCameraShake(RectTransform canvasRect, float magnitude, float duration)
    {
        if (canvasRect == null) yield break;
        Vector2 originalPos = canvasRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float decreaseFactor = 1f - (elapsed / duration);
            canvasRect.anchoredPosition = originalPos + Random.insideUnitCircle * magnitude * decreaseFactor;
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasRect.anchoredPosition = originalPos;
    }
}
