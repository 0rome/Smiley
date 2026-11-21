using UnityEngine;

public class MonsterVideo : MonoBehaviour
{
    [SerializeField] private GameObject obj;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (obj.activeSelf == false)
            {
                obj.SetActive(true);
            }
            else
            {
                obj.SetActive(false);
            }
        }
    }
}
