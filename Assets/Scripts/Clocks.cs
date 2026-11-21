using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Clocks : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject mitutesHand;
    [SerializeField] private GameObject hoursHand;

    [Header("Audio settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickClip;


    void Start()
    {
        SecondTick().Forget();
        MinuteTick().Forget();
    }

    private async UniTask SecondTick()
    {
        while (true)
        {
            mitutesHand.transform.DOLocalRotate(new Vector3(0, 0, 6), 0.1f, RotateMode.LocalAxisAdd);

            audioSource.PlayOneShot(tickClip);

            await UniTask.WaitForSeconds(1f);
        }
    }
    private async UniTask MinuteTick()
    {
        while (true)
        {
            mitutesHand.transform.DOLocalRotate(new Vector3(0, 0, 6), 0.1f, RotateMode.LocalAxisAdd);

            await UniTask.WaitForSeconds(60f);
        }
    }
}