using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Zenject;

public class SmileManHeadAim : MonoBehaviour
{
    [SerializeField] private MultiAimConstraint headMultiAimConstraint;
    [SerializeField] private float transitionSpeed = 2f;

    [Inject] private SmileManController smileManController;

    private void Update()
    {
        float targetWeight = smileManController.smileManState == SmileManState.ChasingPlayer? 1f: 0f;

        headMultiAimConstraint.weight = Mathf.Lerp(headMultiAimConstraint.weight, targetWeight, transitionSpeed * Time.deltaTime);
    }
}
