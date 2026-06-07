using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGeneration : MonoBehaviour
{
    public int worlSize = 512;
    public int chunkSize = 64;
    public float noiseScale = 20f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float seed = 42f;
    public Texture2D falloffMap;
    public Texture2D noiseTexture;

    void Start()
    {
        GenerateTerrain();

    }

    void GenerateTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;
        terrainData.heightmapResolution = worlSize + 1;
        terrainData.size = new Vector3(worlSize, 50, worlSize);

        // Unity may snap heightmapResolution to a valid value, so read it back
        // and size the array to match what SetHeights expects.
        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        bool hasFalloff = falloffMap != null;
        if (!hasFalloff)
            Debug.LogWarning("TerrainGeneration: falloffMap not assigned; generating terrain without falloff.");

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float noiseValue = GeneratePerlinNoise(x, y);
                float falloffValue = 0f;
                if (hasFalloff)
                    falloffValue = falloffMap.GetPixel(x * falloffMap.width / resolution, y * falloffMap.height / resolution).r;

                // TerrainData.SetHeights is indexed as [row (z), column (x)].
                heights[y, x] = Mathf.Clamp01(noiseValue - falloffValue);

                if (noiseTexture != null && x < noiseTexture.width && y < noiseTexture.height)
                {
                    noiseTexture.SetPixel(x, y, new Color(noiseValue, noiseValue, noiseValue));
                }

            }

        }

        if (noiseTexture != null)
            noiseTexture.Apply();

        terrainData.SetHeights(0, 0, heights);

    }

    float GeneratePerlinNoise(int x, int y)
    {
        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = x / noiseScale * frequency + seed;
            float sampleY = y / noiseScale * frequency + seed;
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            noiseHeight += perlinValue * amplitude;
            amplitude *= persistence;
            frequency *= 2;

        }

        return (noiseHeight + 1) / 2; // Normalize to [0,1]

    }

}
