using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SmartFootstepSystem : MonoBehaviour
{
    public AudioClip baseSound;
    [HideInInspector] public Texture2D currentTexture;

    public AudioSource footstepAudio;
    public float groundCheckDistance = 0.25f;
    public List<GroundType> groundTypes = new();
    private RaycastHit hit;
    [HideInInspector] public bool onTerrain;
    private TerrainLayer[] splatPrototypes;
    private Terrain terrain;
    private TerrainData terrainData;

    private void Start()
    {
        GetTerrainInfo();
    }

    private void GetTerrainInfo()
    {
        if (Terrain.activeTerrain)
        {
            terrain = Terrain.activeTerrain;
            terrainData = terrain.terrainData;
            splatPrototypes = terrain.terrainData.terrainLayers;// .splatPrototypes;
        }
    }

    private void Update()
    {
        var ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);

        //check if the character is currently on a terrain or renderer and get the tecture at that position
        if (Physics.Raycast(ray, out hit, groundCheckDistance))
        {
            if (hit.collider.GetComponent<Terrain>())
            {
                currentTexture = splatPrototypes[GetMainTexture(transform.position)].diffuseTexture;
                onTerrain = true;
            }

            if (hit.collider.GetComponent<Renderer>())
            {
                currentTexture = GetRendererTexture();
                onTerrain = false;
            }
        }

        //helper to visualize the ground checker ray
#if UNITY_EDITOR
        Debug.DrawLine(transform.position + Vector3.up * 0.1f,
            transform.position + Vector3.up * 0.1f + Vector3.down * groundCheckDistance, Color.green);
#endif
    }

    public void Footstep()
    {
        var found = false;
        footstepAudio.volume = Random.Range(0.06f, 0.12f);
        footstepAudio.pitch = Random.Range(0.95f, 1.05f);
        footstepAudio.Stop();

        foreach (var groundType in groundTypes)
            foreach (var texture in groundType.textures)
                if (currentTexture == texture)
                {
                    footstepAudio.clip = groundType.sounds[Random.Range(0, groundType.sounds.Length)];
                    found = true;
                }

        if (!found) footstepAudio.PlayOneShot(baseSound);

        footstepAudio.Play();
    }

    /*returns an array containing the relative mix of textures
       on the main terrain at this world position.*/
    public float[] GetTextureMix(Vector3 worldPos)
    {
        terrain = Terrain.activeTerrain;
        terrainData = terrain.terrainData;
        var terrainPos = terrain.transform.position;

        var mapX = (int)((worldPos.x - terrainPos.x) / terrainData.size.x * terrainData.alphamapWidth);
        var mapZ = (int)((worldPos.z - terrainPos.z) / terrainData.size.z * terrainData.alphamapHeight);

        var splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        var cellMix = new float[splatmapData.GetUpperBound(2) + 1];
        for (var n = 0; n < cellMix.Length; ++n) cellMix[n] = splatmapData[0, 0, n];

        return cellMix;
    }

    /*returns the zero-based index of the most dominant texture
       on the main terrain at this world position.*/
    public int GetMainTexture(Vector3 worldPos)
    {
        var mix = GetTextureMix(worldPos);
        float maxMix = 0;
        var maxIndex = 0;

        for (var n = 0; n < mix.Length; ++n)
            if (mix[n] > maxMix)
            {
                maxIndex = n;
                maxMix = mix[n];
            }

        return maxIndex;
    }

    //returns the mainTexture of a renderer's material at this position
    public Texture2D GetRendererTexture()
    {
        Texture2D texture = null;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
            if (hit.collider.gameObject.GetComponent<Renderer>())
            {
                var meshFilter = (MeshFilter)hit.collider.GetComponent(typeof(MeshFilter));
                var mesh = meshFilter.mesh;
                var totalSubMeshes = mesh.subMeshCount;
                var subMeshes = new int[totalSubMeshes];
                for (var i = 0; i < totalSubMeshes; i++) subMeshes[i] = mesh.GetTriangles(i).Length / 3;

                var hitSubMesh = 0;
                var maxVal = 0;

                for (var i = 0; i < totalSubMeshes; i++)
                {
                    maxVal += subMeshes[i];
                    if (hit.triangleIndex <= maxVal - 1)
                    {
                        hitSubMesh = i + 1;
                        break;
                    }
                }

                texture = (Texture2D)hit.collider.gameObject.GetComponent<Renderer>().materials[hitSubMesh - 1]
                    .mainTexture;
            }

        return texture;
    }

    [Serializable]
    public class GroundType
    {
        public string name;
        public AudioClip[] sounds;
        public Texture2D[] textures;
    }
}