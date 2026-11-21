using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class ScreenClueManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clueTMP;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private CancellationTokenSource cts;
    private Color originalColor;

    private void Awake()
    {
        originalColor = clueTMP.color;
        SetAlpha(0f);
    }

    private void OnDestroy() => cts?.Cancel();

    public void ShowClue(string clueText)
    {
        clueTMP.text = clueText;
        cts?.Cancel();
        cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        RunAsync(cts.Token).Forget();
    }

    private async UniTaskVoid RunAsync(CancellationToken token)
    {
        await FadeAsync(0f, 1f, fadeInDuration, token);
        await UniTask.Delay(TimeSpan.FromSeconds(displayDuration), cancellationToken: token);
        await FadeAsync(1f, 0f, fadeOutDuration, token);
    }

    private async UniTask FadeAsync(float from, float to, float duration, CancellationToken token)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        var c = originalColor;
        c.a = a;
        clueTMP.color = c;
    }
}