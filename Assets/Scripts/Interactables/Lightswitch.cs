using UnityEngine;

public class Lightswitch : InteractableBase
{

    [Header("Settings")]
    [SerializeField] private GameObject[] lightObjects;
    [SerializeField] private MeshRenderer[] lampObjects;
    [SerializeField] private Material onMaterial;
    [SerializeField] private Material offMaterial;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] audioClips;

    private bool isOn = true;

    public override void Interact()
    {
        audioSource.pitch = Random.Range(0.65f, 1.0f);
        audioSource.clip = audioClips[Random.Range(0, audioClips.Length)];
        audioSource.Play();

        if (isOn)
        {
            LightOff();
        }
        else
        {
            LightOn();
        }
    }
    public void LightOn()
    {
        transform.localPosition = new Vector3(0, -0.7f, 0);
        transform.localRotation = Quaternion.Euler(-13,0,0);

        isOn = true;

        foreach (var obj in lightObjects)
        {
            obj.SetActive(true);
        }
        foreach (var obj in lampObjects)
        {
            obj.material = onMaterial;
        }
    }
    public void LightOff()
    {

        transform.localPosition = new Vector3(0, 0, 0);
        transform.localRotation = Quaternion.Euler(0, 0, 0);

        isOn = false;

        foreach (var obj in lightObjects)
        {
            obj.SetActive(false);
        }
        foreach (var obj in lampObjects)
        {
            obj.material = offMaterial;
        }
    }
}
