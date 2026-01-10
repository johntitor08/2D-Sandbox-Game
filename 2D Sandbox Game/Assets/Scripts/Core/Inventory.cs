// Inventory.cs - Attach to player GameObject
using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [System.Serializable]
    public class InventorySlot
    {
        public Block.BlockType blockType;
        public int count;
    }
    
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int maxSlots = 10;
    public int selectedSlot = 0;
    
    [Header("Block Prefabs")]
    public GameObject dirtBlockPrefab;
    public GameObject stoneBlockPrefab;
    public GameObject grassBlockPrefab;
    public GameObject oreBlockPrefab;
    
    void Start()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            slots.Add(new InventorySlot());
        }
    }
    
    void Update()
    {
        // Hotbar selection with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedSlot = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedSlot = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedSlot = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedSlot = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) selectedSlot = 4;
        
        // Mouse wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
            selectedSlot = (selectedSlot - 1 + maxSlots) % maxSlots;
        else if (scroll < 0f)
            selectedSlot = (selectedSlot + 1) % maxSlots;
    }
    
    public void AddBlock(Block.BlockType type)
    {
        // Find existing slot with same type
        foreach (var slot in slots)
        {
            if (slot.blockType == type && slot.count > 0)
            {
                slot.count++;
                return;
            }
        }
        
        // Find empty slot
        foreach (var slot in slots)
        {
            if (slot.count == 0)
            {
                slot.blockType = type;
                slot.count = 1;
                return;
            }
        }
    }
    
    public bool RemoveBlock(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return false;
            
        if (slots[slotIndex].count > 0)
        {
            slots[slotIndex].count--;
            if (slots[slotIndex].count == 0)
            {
                slots[slotIndex].blockType = Block.BlockType.Dirt;
            }
            return true;
        }
        return false;
    }
    
    public GameObject GetSelectedBlockPrefab()
    {
        if (selectedSlot < 0 || selectedSlot >= slots.Count || slots[selectedSlot].count == 0)
            return null;
            
        switch (slots[selectedSlot].blockType)
        {
            case Block.BlockType.Dirt: return dirtBlockPrefab;
            case Block.BlockType.Stone: return stoneBlockPrefab;
            case Block.BlockType.Grass: return grassBlockPrefab;
            case Block.BlockType.Ore: return oreBlockPrefab;
            default: return null;
        }
    }
    
    void OnGUI()
    {
        // Simple inventory display
        float slotSize = 50;
        float spacing = 10;
        float startX = Screen.width / 2 - (maxSlots * (slotSize + spacing)) / 2;
        float startY = Screen.height - slotSize - 20;
        
        for (int i = 0; i < Mathf.Min(5, slots.Count); i++)
        {
            float x = startX + i * (slotSize + spacing);
            
            // Background
            Color bgColor = i == selectedSlot ? Color.yellow : Color.gray;
            GUI.backgroundColor = bgColor;
            GUI.Box(new Rect(x, startY, slotSize, slotSize), "");
            
            // Count
            if (slots[i].count > 0)
            {
                GUI.backgroundColor = Color.white;
                GUI.Label(new Rect(x + 5, startY + 5, slotSize - 10, slotSize - 10), 
                    slots[i].blockType.ToString() + "\n" + slots[i].count);
            }
        }
    }
}