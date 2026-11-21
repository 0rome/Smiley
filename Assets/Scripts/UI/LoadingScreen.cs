using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject loadingPanel; // Панель, которая перекрывает меню во время загрузки
    [SerializeField] private Image blackScreenImage;

    [Header("Настройки")]
    [SerializeField] private string sceneToLoad = "GameScene";
    [SerializeField] private float minimumLoadTime = 1.0f;

    private bool isLoading = false;

    private void Start()
    {
        FadeImage(0,1);
    }
    public void StartLoading()
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        isLoading = true;
        if (loadingPanel != null) loadingPanel.SetActive(true);

        // 1. Загружаем новую сцену аддитивно (не выгружая текущую)
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        operation.allowSceneActivation = false; // Ждём, пока покажем полосу

        float timer = 0f;

        // 2. Обновляем полосу, пока сцена грузится
        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null) progressBar.value = progressValue;
            if (progressText != null) progressText.text = Mathf.RoundToInt(progressValue * 100f) + "%";

            if (operation.progress >= 0.9f && timer >= minimumLoadTime)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
        FadeImage(1, 1);

        yield return new WaitForSeconds(1f);

       
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.UnloadSceneAsync(currentScene);
    }

    private void FadeImage(float endAlpha, float duration = -1f)
    {   
        blackScreenImage.gameObject.SetActive(true);

        if (blackScreenImage == null) return;

        // Если длительность не задана, используем стандартную
        float time = duration > 0 ? duration : 1f;

        // Останавливаем предыдущую анимацию этого Image, чтобы избежать конфликтов
        blackScreenImage.DOKill();

        // Запускаем DOTween анимацию для alpha-канала цвета
        blackScreenImage.DOFade(endAlpha, time)
            .SetEase(Ease.InOutSine);
    }
}