using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class ControlsSettings : MonoBehaviour
{
    [Header("UI Elements - Movement")]
    public Button forwardBtn;
    public Button backwardBtn;
    public Button leftBtn;
    public Button rightBtn;
    public Button sprintBtn;
    public Button crouchBtn;
    public Button interactBtn;
    public Button nightVisionBtn;

    [Header("UI Elements - Sensitivity")]
    public Slider mouseSensitivitySlider;
    public TMP_Text sensitivityValueText;

    // Словарь для удобной работы с UI (связываем строку с кнопкой)
    private Dictionary<string, Button> actionButtons = new Dictionary<string, Button>();

    // Словарь для чтения/записи значений KeyCode в SO
    private Dictionary<string, System.Action<KeyCode>> setKeyActions;
    private Dictionary<string, System.Func<KeyCode>> getKeyActions;

    private string actionToRebind = null;
    private bool isRebinding = false;

    [Inject] private InputSettings inputSettings; // Ссылка на наш ScriptableObject

    void Start()
    {
        // 1. Инициализация словарей
        actionButtons.Add("Forward", forwardBtn);
        actionButtons.Add("Backward", backwardBtn);
        actionButtons.Add("Left", leftBtn);
        actionButtons.Add("Right", rightBtn);
        actionButtons.Add("Sprint", sprintBtn);
        actionButtons.Add("Crouch", crouchBtn);
        actionButtons.Add("Interact", interactBtn);
        actionButtons.Add("NightVision", nightVisionBtn);

        // Настраиваем делегаты для доступа к полям ScriptableObject
        setKeyActions = new Dictionary<string, System.Action<KeyCode>>
        {
            { "Forward", (key) => inputSettings.forward = key },
            { "Backward", (key) => inputSettings.backward = key },
            { "Left", (key) => inputSettings.left = key },
            { "Right", (key) => inputSettings.right = key },
            { "Sprint", (key) => inputSettings.sprint = key },
            { "Crouch", (key) => inputSettings.crouch = key },
            { "Interact", (key) => inputSettings.interact = key },
            { "NightVision", (key) => inputSettings.nightVision = key }
        };

        getKeyActions = new Dictionary<string, System.Func<KeyCode>>
        {
            { "Forward", () => inputSettings.forward },
            { "Backward", () => inputSettings.backward },
            { "Left", () => inputSettings.left },
            { "Right", () => inputSettings.right },
            { "Sprint", () => inputSettings.sprint },
            { "Crouch", () => inputSettings.crouch },
            { "Interact", () => inputSettings.interact },
            { "NightVision", () => inputSettings.nightVision }
        };

        // 2. Подписка на клики
        foreach (var pair in actionButtons)
        {
            string action = pair.Key;
            pair.Value.onClick.AddListener(() => StartRebinding(action));
        }

        // 3. Настройка слайдера
        mouseSensitivitySlider.minValue = 0.1f;
        mouseSensitivitySlider.maxValue = 5f;
        mouseSensitivitySlider.value = inputSettings.mouseSensitivity;
        mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        OnSensitivityChanged(inputSettings.mouseSensitivity);

        UpdateUI();
    }

    void Update()
    {
        if (isRebinding && actionToRebind != null)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.Mouse0) continue; // Игнорируем ЛКМ (клик по кнопке UI)

                if (Input.GetKeyDown(key))
                {
                    if (key == KeyCode.Escape)
                    {
                        CancelRebind();
                        return;
                    }

                    // Записываем новую клавишу в ScriptableObject
                    setKeyActions[actionToRebind](key);
                    PlayerPrefs.SetInt("Key_" + actionToRebind, (int)key);
                    PlayerPrefs.Save(); // Записываем на диск сразу

                    isRebinding = false;
                    actionToRebind = null;
                    UpdateUI();
                    return;
                }
            }
        }
    }

    #region Логика переназначения

    private void StartRebinding(string action)
    {
        actionToRebind = action;
        isRebinding = true;
        actionButtons[action].GetComponentInChildren<TMP_Text>().text = "...";
    }

    private void CancelRebind()
    {
        isRebinding = false;
        actionToRebind = null;
        UpdateUI();
    }

    #endregion

    #region Чувствительность

    private void OnSensitivityChanged(float value)
    {
        inputSettings.mouseSensitivity = value;
        PlayerPrefs.SetFloat("MouseSensitivity", value); // Сохраняем
        PlayerPrefs.Save();

        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F2");
    }

    #endregion

    #region Сброс

    public void ResetToDefaults()
    {
        // Сбрасываем к стандартным значениям
        inputSettings.forward = KeyCode.W;
        inputSettings.backward = KeyCode.S;
        inputSettings.left = KeyCode.A;
        inputSettings.right = KeyCode.D;
        inputSettings.sprint = KeyCode.LeftShift;
        inputSettings.crouch = KeyCode.LeftControl;
        inputSettings.interact = KeyCode.E;
        inputSettings.nightVision = KeyCode.F;
        inputSettings.mouseSensitivity = 1.0f;

        mouseSensitivitySlider.value = 1.0f;
        OnSensitivityChanged(1.0f);

        UpdateUI();
    }

    #endregion

    #region Обновление UI

    private void UpdateUI()
    {
        foreach (var pair in actionButtons)
        {
            string action = pair.Key;
            Button btn = pair.Value;
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();

            if (btnText != null && getKeyActions.ContainsKey(action))
            {
                btnText.text = getKeyActions[action]().ToString();
            }
        }
    }

    #endregion
}