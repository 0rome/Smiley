using UnityEngine;

public class NoiseSoundEvent : MonoBehaviour
{
    [SerializeField] private float hearingRadius = 10f;
    [SerializeField] private LayerMask enemyLayer; // Укажите слой, на котором живет враг

    public void MakeSoundEvent()
    {
        // Ищем все коллайдеры врагов в радиусе слышимости
        Collider[] colliders = Physics.OverlapSphere(transform.position, hearingRadius, enemyLayer);

        if (colliders != null && colliders.Length > 0)
        {
            foreach (var col in colliders)
            {
                // Ищем SmileManVision на самом коллайдере или в его родителях
                SmileManVision vision = col.GetComponentInParent<SmileManVision>();
                if (vision != null)
                {
                    vision.HearSound(transform.position);
                    Debug.Log("Noise event triggered для врага: " + col.name);
                }
            }
        }
        else
        {
            Debug.Log("Врагов в радиусе слышимости не обнаружено.");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
#endif
}