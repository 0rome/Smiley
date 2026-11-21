using System.Collections;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("Параметры взрыва")]
    [Tooltip("Радиус действия взрыва")]
    [SerializeField] private float radius = 5f;

    [Tooltip("Сила взрыва")]
    [SerializeField] private float force = 15f;

    [Tooltip("Сила, толкающая объекты вверх (для реалистичной дуги полета)")]
    [SerializeField] private float upwardsModifier = 2f;

    [Tooltip("Слои, которые взрыв игнорирует (например, земля или игрок)")]
    [SerializeField] private LayerMask ignoreLayers;

    private void Start()
    {
        Explode();
    }

    public void Explode()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, ~ignoreLayers);

        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.attachedRigidbody;

            if (rb != null)
            {
                rb.AddExplosionForce(force, transform.position, radius, upwardsModifier, ForceMode.Impulse);
            }
        }

        // 3. Визуал и звук (вызов компонентов)
        PlayVFX();
        PlaySound();

        // 4. Уничтожаем объект взрыва через секунду (или сразу, если это просто триггер)
        Destroy(gameObject, 1f);
    }

    private void PlayVFX()
    {
        // Здесь вы будете вызывать свой VFX Graph или Particle System
    }

    private void PlaySound()
    {
        // AudioSource.PlayClipAtPoint(explosionSound, transform.position);
    }

    // Визуализация радиуса в редакторе (когда объект выделен)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

