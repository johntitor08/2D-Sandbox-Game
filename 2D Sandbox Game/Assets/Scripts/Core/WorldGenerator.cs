using UnityEngine;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int worldWidth = 200;
    public int worldHeight = 100;
    public int surfaceHeight = 50;
    
    [Header("Prefabs")]
    public GameObject dirtBlockPrefab;
    public GameObject stoneBlockPrefab;
    public GameObject grassBlockPrefab;
    public GameObject oreBlockPrefab;
    
    [Header("Noise Settings")]
    public float terrainFrequency = 0.05f;
    public float terrainAmplitude = 10f;
    public float caveFrequency = 0.1f;
    public float caveThreshold = 0.5f;
    
    private Dictionary<Vector2Int, GameObject> blocks = new Dictionary<Vector2Int, GameObject>();
    private Transform worldParent;
    
    void Start()
    {
        worldParent = new GameObject("World").transform;
        GenerateWorld();
    }
    
    void GenerateWorld()
    {
        int seed = Random.Range(0, 10000);
        
        for (int x = 0; x < worldWidth; x++)
        {
            float surfaceNoise = Mathf.PerlinNoise(seed + x * terrainFrequency, seed) * terrainAmplitude;
            int currentSurfaceHeight = surfaceHeight + Mathf.RoundToInt(surfaceNoise);
            
            for (int y = 0; y < worldHeight; y++)
            {
                if (y > currentSurfaceHeight)
                    continue;
                
                // Cave generation
                float caveNoise = Mathf.PerlinNoise(
                    seed + x * caveFrequency, 
                    seed + y * caveFrequency
                );
                
                if (y < currentSurfaceHeight - 5 && caveNoise > caveThreshold)
                    continue;
                
                GameObject blockPrefab = GetBlockType(y, currentSurfaceHeight);
                if (blockPrefab != null)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    CreateBlock(pos, blockPrefab);
                }
            }
        }
    }
    
    GameObject GetBlockType(int y, int surfaceY)
    {
        if (y == surfaceY)
            return grassBlockPrefab;
        else if (y > surfaceY - 5)
            return dirtBlockPrefab;
        else if (y < surfaceY - 5 && Random.value < 0.02f)
            return oreBlockPrefab;
        else
            return stoneBlockPrefab;
    }
    
    void CreateBlock(Vector2Int pos, GameObject prefab)
    {
        GameObject block = Instantiate(prefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
        block.transform.parent = worldParent;
        block.name = $"Block_{pos.x}_{pos.y}";
        blocks[pos] = block;
        
        Block blockScript = block.GetComponent<Block>();
        if (blockScript != null)
        {
            blockScript.gridPosition = pos;
        }
    }
    
    public void DestroyBlock(Vector2Int pos)
    {
        if (blocks.ContainsKey(pos))
        {
            Destroy(blocks[pos]);
            blocks.Remove(pos);
        }
    }
    
    public void PlaceBlock(Vector2Int pos, GameObject blockPrefab)
    {
        if (!blocks.ContainsKey(pos))
        {
            CreateBlock(pos, blockPrefab);
        }
    }
    
    public bool IsBlockAt(Vector2Int pos)
    {
        return blocks.ContainsKey(pos);
    }
}