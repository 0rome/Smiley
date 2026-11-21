using UnityEngine;

public class Lamp : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private MeshRenderer lampMeshRenderer;
    [SerializeField] private Material offMaterial;
    [SerializeField] private Light lightObject;

    private Material defaultMaterial;
    private bool isActive = true;


    private void Start()
    {
        defaultMaterial = lampMeshRenderer.material;
    }

    public void OffLight()
    {
        lampMeshRenderer.material = offMaterial;
        lightObject.enabled = false;
        isActive = false;
    }
    public void OnLight()
    {
        lampMeshRenderer.material = defaultMaterial;
        lightObject.enabled = true;
        isActive = true;
    }
}
