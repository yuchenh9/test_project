using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject 
{
    public string itemName = "New Item";
    public Sprite icon = null;
    public int value = 0;
    
    public virtual void Use()
    {
        // Base use functionality
        Debug.Log("Using " + name);
    }
}