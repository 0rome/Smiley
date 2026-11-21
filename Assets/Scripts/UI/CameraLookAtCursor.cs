using UnityEngine;

public sealed class CameraLookAtCursor : MonoBehaviour
{
    [Header("Настройки чувствительности")]
    [Tooltip("Насколько сильно камера отклоняется за курсором (0 = нет, 1 = смотрит прямо на курсор)")]
    [Range(0f, 1f)]
    [SerializeField] private float lookStrength = 0.1f;

    [Header("Ограничения (в градусах)")]
    [Tooltip("Максимальный угол поворота по горизонтали")]
    [SerializeField] private float maxYaw = 10f;
    [Tooltip("Максимальный угол поворота по вертикали")]
    [SerializeField] private float maxPitch = 5f;

    [Header("Сглаживание")]
    [Tooltip("Скорость плавного поворота")]
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Ссылки")]
    [Tooltip("Камера, которая смотрит на ТВ. Если пусто — берется текущая.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Объект, на котором висит меню (ТВ). Камера будет смотреть в его сторону.")]
    [SerializeField] private Transform menuTarget;

    private Quaternion initialRotation;
    private Vector3 menuCenterPoint;

    private void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();

        // Запоминаем начальный поворот камеры
        initialRotation = transform.rotation;

        // Если цель (ТВ) не задана, берем точку перед камерой на расстоянии 5 метров
        if (menuTarget != null)
        {
            menuCenterPoint = menuTarget.position;
        }
        else
        {
            menuCenterPoint = transform.position + transform.forward * 5f;
        }
    }

    private void LateUpdate()
    {
        // 1. Получаем позицию курсора на экране
        Vector3 mouseScreenPos = Input.mousePosition;

        // 2. Создаем луч из камеры через курсор
        Ray ray = targetCamera.ScreenPointToRay(mouseScreenPos);

        // 3. Находим точку пересечения луча с плоскостью, проходящей через центр ТВ
        // (чтобы камера реагировала на движение мыши по экрану)
        Plane plane = new Plane(targetCamera.transform.forward, menuCenterPoint);
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);

            // 4. Вычисляем направление от камеры к точке попадания
            Vector3 directionToCursor = hitPoint - transform.position;

            // 5. Ограничиваем угол, чтобы камера не крутилась слишком сильно
            // (чтобы не сломать композицию кадра)
            Vector3 localDir = transform.InverseTransformDirection(directionToCursor);

            // Ограничиваем по горизонтали (Y)
            float yaw = Mathf.Clamp(localDir.x * lookStrength * 10f, -maxYaw, maxYaw);
            // Ограничиваем по вертикали (X)
            float pitch = Mathf.Clamp(localDir.y * lookStrength * 10f, -maxPitch, maxPitch);

            // 6. Формируем целевой поворот
            Quaternion targetRotation = initialRotation * Quaternion.Euler(-pitch, yaw, 0);

            // 7. Плавно применяем
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }
}