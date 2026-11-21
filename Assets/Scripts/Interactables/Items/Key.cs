using UnityEngine;

public class Key : ItemBase
{
    [SerializeField] private string keyName = "defaultKey";

    public string KeyName => keyName;

    public override void UseItem()
    {
        
    }
}
