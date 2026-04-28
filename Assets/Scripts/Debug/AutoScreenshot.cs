using System.Collections;
using UnityEngine;

/// <summary>
/// 起動後とリトライ後に自動でGameビューのスクリーンショットを撮影する
/// </summary>
public class AutoScreenshot : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float captureDelay = 2.0f;
    [SerializeField] private string screenshotDir = "Assets/Screenshots/";

    private static int captureCount = 0;

    private void Start()
    {
        StartCoroutine(CaptureAfterDelay());
    }

    private IEnumerator CaptureAfterDelay()
    {
        yield return new WaitForSeconds(captureDelay);

        captureCount++;
        string filename = $"{screenshotDir}autoshot_{captureCount:D2}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        ScreenCapture.CaptureScreenshot(filename);
        Debug.Log($"[AutoScreenshot] Gameビュースクリーンショット撮影: {filename}");
    }
}
