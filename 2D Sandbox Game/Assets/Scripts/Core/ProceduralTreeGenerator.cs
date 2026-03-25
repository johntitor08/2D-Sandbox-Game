// ProceduralTreeGenerator.cs - Generates individual tree meshes
using UnityEngine;
using System.Collections.Generic;

public class ProceduralTreeGenerator : MonoBehaviour
{
    [System.Serializable]
    public class TreeSettings
    {
        [Header("Trunk Settings")]
        public float trunkHeight = 3f;
        public float trunkRadius = 0.15f;
        public int trunkSegments = 8;
        public int trunkVerticalSegments = 5;
        public float trunkTaper = 0.7f; // 1.0 = no taper
        
        [Header("Branch Settings")]
        public int branchLevels = 2;
        public int branchesPerLevel = 4;
        public float branchLength = 1.5f;
        public float branchAngle = 45f;
        public float branchLengthDecay = 0.6f;
        public float branchRadiusDecay = 0.7f;
        
        [Header("Foliage Settings")]
        public float foliageRadius = 2f;
        public int foliageSegments = 3;
        public float foliageHeight = 2f;
        
        [Header("Materials")]
        public Material trunkMaterial;
        public Material foliageMaterial;
        
        [Header("Randomization")]
        public float randomness = 0.3f;
    }
    
    public TreeSettings treeSettings;
    
    public GameObject GenerateTree()
    {
        GameObject tree = new GameObject("ProceduralTree");
        
        // Generate trunk
        GameObject trunk = GenerateTrunk();
        trunk.transform.parent = tree.transform;
        
        // Generate branches
        GenerateBranches(tree.transform, Vector3.up * treeSettings.trunkHeight, 
                        treeSettings.branchLevels, treeSettings.branchLength, 
                        treeSettings.trunkRadius * treeSettings.branchRadiusDecay);
        
        // Generate foliage
        GameObject foliage = GenerateFoliage();
        foliage.transform.parent = tree.transform;
        foliage.transform.position = Vector3.up * (treeSettings.trunkHeight + treeSettings.foliageHeight * 0.3f);
        
        return tree;
    }
    
    GameObject GenerateTrunk()
    {
        GameObject trunkObj = new GameObject("Trunk");
        MeshFilter meshFilter = trunkObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = trunkObj.AddComponent<MeshRenderer>();
        
        meshFilter.mesh = CreateCylinderMesh(
            treeSettings.trunkHeight,
            treeSettings.trunkRadius,
            treeSettings.trunkRadius * treeSettings.trunkTaper,
            treeSettings.trunkSegments,
            treeSettings.trunkVerticalSegments
        );
        
        meshRenderer.material = treeSettings.trunkMaterial;
        
        return trunkObj;
    }
    
    void GenerateBranches(Transform parent, Vector3 position, int level, float length, float radius)
    {
        if (level <= 0) return;
        
        int branchCount = treeSettings.branchesPerLevel;
        float angleStep = 360f / branchCount;
        
        for (int i = 0; i < branchCount; i++)
        {
            float angle = angleStep * i + Random.Range(-treeSettings.randomness * 30f, treeSettings.randomness * 30f);
            float branchAngle = treeSettings.branchAngle + Random.Range(-treeSettings.randomness * 20f, treeSettings.randomness * 20f);
            
            // Calculate branch direction
            Quaternion rotation = Quaternion.Euler(branchAngle, angle, 0);
            Vector3 direction = rotation * Vector3.up;
            
            // Create branch
            GameObject branch = new GameObject($"Branch_L{level}_{i}");
            branch.transform.parent = parent;
            branch.transform.position = position;
            
            MeshFilter meshFilter = branch.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = branch.AddComponent<MeshRenderer>();
            
            float actualLength = length * (1f + Random.Range(-treeSettings.randomness, treeSettings.randomness) * 0.5f);
            meshFilter.mesh = CreateCylinderMesh(actualLength, radius, radius * 0.5f, 6, 2);
            meshRenderer.material = treeSettings.trunkMaterial;
            
            branch.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0);
            
            // Recurse for sub-branches
            Vector3 branchEnd = position + direction * actualLength;
            GenerateBranches(
                parent,
                branchEnd,
                level - 1,
                length * treeSettings.branchLengthDecay,
                radius * treeSettings.branchRadiusDecay
            );
        }
    }
    
    GameObject GenerateFoliage()
    {
        GameObject foliageObj = new GameObject("Foliage");
        MeshFilter meshFilter = foliageObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = foliageObj.AddComponent<MeshRenderer>();
        
        // Create sphere-like foliage
        meshFilter.mesh = CreateSphereMesh(treeSettings.foliageRadius, treeSettings.foliageSegments);
        meshRenderer.material = treeSettings.foliageMaterial;
        
        return foliageObj;
    }
    
    Mesh CreateCylinderMesh(float height, float radiusBottom, float radiusTop, int segments, int heightSegments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Generate vertices
        for (int h = 0; h <= heightSegments; h++)
        {
            float t = h / (float)heightSegments;
            float y = height * t;
            float radius = Mathf.Lerp(radiusBottom, radiusTop, t);
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                
                vertices.Add(new Vector3(x, y, z));
                uvs.Add(new Vector2(i / (float)segments, t));
            }
        }
        
        // Generate triangles
        for (int h = 0; h < heightSegments; h++)
        {
            for (int i = 0; i < segments; i++)
            {
                int current = h * (segments + 1) + i;
                int next = current + segments + 1;
                
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);
                
                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    Mesh CreateSphereMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        // Simple icosphere approximation
        int rings = segments * 2;
        int sectorsPerRing = segments * 4;
        
        for (int ring = 0; ring <= rings; ring++)
        {
            float phi = Mathf.PI * ring / rings;
            float y = Mathf.Cos(phi) * radius;
            float ringRadius = Mathf.Sin(phi) * radius;
            
            for (int sector = 0; sector <= sectorsPerRing; sector++)
            {
                float theta = 2f * Mathf.PI * sector / sectorsPerRing;
                float x = Mathf.Cos(theta) * ringRadius;
                float z = Mathf.Sin(theta) * ringRadius;
                
                vertices.Add(new Vector3(x, y, z));
            }
        }
        
        // Generate triangles
        for (int ring = 0; ring < rings; ring++)
        {
            for (int sector = 0; sector < sectorsPerRing; sector++)
            {
                int current = ring * (sectorsPerRing + 1) + sector;
                int next = current + sectorsPerRing + 1;
                
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);
                
                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    [ContextMenu("Generate Test Tree")]
    void GenerateTestTree()
    {
        // Clear existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        GameObject tree = GenerateTree();
        tree.transform.parent = transform;
    }
}