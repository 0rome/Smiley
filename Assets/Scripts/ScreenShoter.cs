
using System;
using System.IO;
using UnityEngine;

public class ScreenShoter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode screenshotKey = KeyCode.Space;
    [SerializeField] private int superSize = 1;

    private string ScreenshotFolder =>
        Path.Combine(Application.dataPath, "../Screenshots");

    private void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }
    }

    private void TakeScreenshot()
    {
        if (!Directory.Exists(ScreenshotFolder))
            Directory.CreateDirectory(ScreenshotFolder);

        string fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string filePath = Path.Combine(ScreenshotFolder, fileName);

        ScreenCapture.CaptureScreenshot(filePath, superSize);

        Debug.Log($"Screenshot saved: {filePath}");
    }
}
