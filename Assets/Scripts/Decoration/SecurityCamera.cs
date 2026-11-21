using Cysharp.Threading.Tasks;
using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private MeshRenderer pointRenderer;
    [SerializeField] private Material onMaterial;
    [SerializeField] private Material offMaterial;
    [SerializeField] private AudioClip beepClip;
    private AudioSource audioSource;
    private float soundDuration;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        soundDuration = beepClip.length / Mathf.Abs(audioSource.pitch);

        Beep().Forget();
    }
    private async UniTask Beep()
    {
        while (true)
        {
            audioSource.PlayOneShot(beepClip);
            pointRenderer.material = onMaterial;

           await UniTask.WaitForSeconds(soundDuration);

            pointRenderer.material = offMaterial;

            await UniTask.WaitForSeconds(1f);
        }
    }
}
