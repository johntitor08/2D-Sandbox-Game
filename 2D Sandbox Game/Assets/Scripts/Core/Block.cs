// Block.cs - Attach to each block prefab
using UnityEngine;

public class Block : MonoBehaviour
{
    public enum BlockType { Dirt, Stone, Grass, Ore }
    public BlockType blockType;
    public int durability = 3;
    public Vector2Int gridPosition;
    
    private int currentDurability;
    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentDurability = durability;
    }
    
    public bool TakeDamage()
    {
        currentDurability--;
        
        // Visual feedback
        if (spriteRenderer != null)
        {
            float alpha = (float)currentDurability / durability;
            spriteRenderer.color = new Color(1, 1, 1, Mathf.Clamp(alpha, 0.3f, 1f));
        }
        
        return currentDurability <= 0;
    }
    
    public void ResetDurability()
    {
        currentDurability = durability;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }
}