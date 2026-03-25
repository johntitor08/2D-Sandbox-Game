// BillboardTreeSystem.cs - GPU-instanced billboard trees for performance
using UnityEngine;
using System.Collections.Generic;

public class BillboardTreeSystem : MonoBehaviour
{
    [System.Serializable]
    public class BillboardTree
    {
        public Texture2D billboardTexture;
        public float width = 3f;
        public float height = 4f;
        public int instanceCount = 1000;
    }
    
    [Header("Billboard Settings")]
    public BillboardTree[] billboardTrees;
    public Material billboardMaterial;
    public bool alwaysFaceCamera = true;
    
    [Header("Placement")]
    public Terrain terrain;
    public float minHeight = 0.2f;
    public float maxHeight = 0.8f;
    public float maxSlope = 30f;
    public int seed = 42;
    
    [Header("Performance")]
    public float maxDrawDistance = 200f;
    public bool useFrustumCulling = true;
    
    private List<Matrix4x4[]> instanceMatrices = new List<Matrix4x4[]>();
    private List<MaterialPropertyBlock[]> propertyBlocks = new List<MaterialPropertyBlock[]>();
    private Mesh billboardMesh;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        CreateBillboardMesh();
        GenerateTrees();
    }
    
    void CreateBillboardMesh()
    {
        billboardMesh = new Mesh();
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(-0.5f, 1, 0),
            new Vector3(0.5f, 1, 0)
        };
        
        int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        billboardMesh.vertices = vertices;
        billboardMesh.triangles = triangles;
        billboardMesh.uv = uvs;
        billboardMesh.RecalculateNormals();
    }
    
    void GenerateTrees()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain not assigned!");
            return;
        }
        
        Random.InitState(seed);
        TerrainData terrainData = terrain.terrainData;
        
        foreach (var billboardTree in billboardTrees)
        {
            List<Matrix4x4> matrices = new List<Matrix4x4>();
            
            for (int i = 0; i < billboardTree.instanceCount; i++)
            {
                // Random position on terrain
                float x = Random.Range(0f, terrainData.size.x);
                float z = Random.Range(0f, terrainData.size.z);
                
                Vector3 worldPos = new Vector3(x, 0, z) + terrain.transform.position;
                float y = terrain.SampleHeight(worldPos);
                worldPos.y = y;
                
                // Check placement rules
                float xNorm = x / terrainData.size.x;
                float zNorm = z / terrainData.size.z;
                float height = y / terrainData.size.y;
                float steepness = terrainData.GetSteepness(xNorm, zNorm);
                
                if (height < minHeight || height > maxHeight || steepness > maxSlope)
                {
                    i--;
                    continue;
                }
                
                // Create transformation matrix
                Vector3 scale = new Vector3(billboardTree.width, billboardTree.height, 1);
                Matrix4x4 matrix = Matrix4x4.TRS(worldPos, Quaternion.identity, scale);
                matrices.Add(matrix);
            }
            
            // Split into batches of 1023 (Unity's instancing limit)
            int batchSize = 1023;
            for (int i = 0; i < matrices.Count; i += batchSize)
            {
                int count = Mathf.Min(batchSize, matrices.Count - i);
                Matrix4x4[] batch = new Matrix4x4[count];
                matrices.CopyTo(i, batch, 0, count);
                instanceMatrices.Add(batch);
                
                // Create property block for texture
                MaterialPropertyBlock[] blocks = new MaterialPropertyBlock[count];
                for (int j = 0; j < count; j++)
                {
                    blocks[j] = new MaterialPropertyBlock();
                    if (billboardTree.billboardTexture != null)
                    {
                        blocks[j].SetTexture("_MainTex", billboardTree.billboardTexture);
                    }
                }
                propertyBlocks.Add(blocks);
            }
        }
        
        Debug.Log($"Generated {instanceMatrices.Count} batches of billboard trees");
    }
    
    void Update()
    {
        if (billboardMesh == null || billboardMaterial == null) return;
        
        // Update billboard rotations to face camera
        if (alwaysFaceCamera && mainCamera != null)
        {
            UpdateBillboardRotations();
        }
        
        // Render all batches
        for (int i = 0; i < instanceMatrices.Count; i++)
        {
            Graphics.DrawMeshInstanced(
                billboardMesh,
                0,
                billboardMaterial,
                instanceMatrices[i],
                instanceMatrices[i].Length,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false,
                0,
                mainCamera,
                UnityEngine.Rendering.LightProbeUsage.Off
            );
        }
    }
    
    void UpdateBillboardRotations()
    {
        Vector3 cameraPos = mainCamera.transform.position;
        
        for (int i = 0; i < instanceMatrices.Count; i++)
        {
            Matrix4x4[] batch = instanceMatrices[i];
            
            for (int j = 0; j < batch.Length; j++)
            {
                Vector3 position = batch[j].GetColumn(3);
                
                // Skip if too far
                if (Vector3.Distance(position, cameraPos) > maxDrawDistance)
                    continue;
                
                // Calculate rotation to face camera
                Vector3 lookDir = (cameraPos - position).normalized;
                lookDir.y = 0; // Keep billboards upright
                
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion rotation = Quaternion.LookRotation(lookDir);
                    Vector3 scale = batch[j].lossyScale;
                    batch[j] = Matrix4x4.TRS(position, rotation, scale);
                }
            }
        }
    }
    
    [ContextMenu("Regenerate Trees")]
    public void RegenerateTrees()
    {
        instanceMatrices.Clear();
        propertyBlocks.Clear();
        GenerateTrees();
    }
    
    [ContextMenu("Create Simple Billboard Material")]
    public void CreateBillboardMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.name = "BillboardTreeMaterial";
        mat.enableInstancing = true;
        
        Debug.Log("Billboard material created. Assign textures and save as asset.");
    }
}