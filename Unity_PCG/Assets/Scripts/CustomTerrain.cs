﻿using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour
{
    public Vector2 randomHeightRange = new Vector2(0, 0.1f);
    public Texture2D heightMapImage;
    public Vector3 heightMapScale = new Vector3(2, 0.5f, 2);

    public bool resetTerrain = true;

    #region Perlin Noise
    public float perlinScaleX = 0.001f;
    public float perlinScaleY = 0.001f;
    public int perlinOffsetX = 0;
    public int perlinOffsetY = 0;
    public int perlinOctaves = 3;
    public float perlinPersistance = 0.5f;
    public float perlinHeightScale = 0.5f;
    #endregion    

    #region Multiple Perlin Noise
    [System.Serializable]
    public class PerlinParameters
    {
        public float xScale = 0.001f;
        public float yScale = 0.001f;
        public int xOffset = 0;
        public int yOffset = 0;
        public int octaves = 3;
        public float persistance = 0.5f;
        public float heightScale = 0.9f;
        public bool remove = false;
    }
    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
    {
        new PerlinParameters()
    };

    #endregion

    #region Voronoi
    public int voronoiPeakCount;
    public float voronoiFallOff;
    public float voronoiDropOff;
    public float voronoiMinHeight;
    public float voronoiMaxHeight;
    public enum VoronoiType { Linear, Power, Combined }
    public VoronoiType voronoiType = VoronoiType.Linear;

    #endregion
    public Terrain terrain;
    public TerrainData terrainData;

    private float[,] GetHeightMap()
    {
        if (!resetTerrain)
        {
            return terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        }
        else
        {
            return new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        }
    }

    public void Voronoi()
    {
        float[,] heightMap = GetHeightMap();
        for (int p = 0; p < voronoiPeakCount; p++)
        {
            // Random Peak
            Vector3 peak = new Vector3(
                 UnityEngine.Random.Range(0, terrainData.heightmapResolution),
                 UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight),
                 UnityEngine.Random.Range(0, terrainData.heightmapResolution));

            if (heightMap[(int)peak.x, (int)peak.z] < peak.y)
            {
                heightMap[(int)peak.x, (int)peak.z] = peak.y;
            }
            else
            {
                continue;
            }
            Vector2 peakLocation = new Vector2(peak.x, peak.z);
            float maxDistance = Vector2.Distance(Vector2.zero, new Vector2(terrainData.heightmapResolution, terrainData.heightmapResolution));

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (!(x == peakLocation.x && y == peakLocation.y))
                    {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance; //linear interpolate distance
                        float h;
                        switch (voronoiType)
                        {
                            case VoronoiType.Linear:
                                h = peak.y - distanceToPeak * voronoiFallOff;
                                break;
                            case VoronoiType.Power:
                                h = peak.y - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff;
                                break;
                            case VoronoiType.Combined:
                                h = peak.y - (distanceToPeak * voronoiFallOff)
                                           - Mathf.Pow(distanceToPeak, voronoiDropOff);
                                break;
                            default:
                                // Default is Linear, but this code should not be reached.
                                h = peak.y - distanceToPeak * voronoiFallOff;
                                break;
                        }
                        if (heightMap[x, y] < h)
                        {
                            heightMap[x, y] = h;
                        }
                    }
                }
            }
        }
        

        terrainData.SetHeights(0, 0, heightMap);
    }
    public void Perlin()
    {
        float[,] heightMap = GetHeightMap();
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                heightMap[x, y] += Utils.fBM(
                        (x + perlinOffsetX) * perlinScaleX,
                        (y + perlinOffsetY) * perlinScaleY,
                        perlinOctaves,
                        perlinPersistance)
                    * perlinHeightScale;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void MultiplePerlinTerrain()
    {
        float[,] heightMap = GetHeightMap();
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                foreach (PerlinParameters parameters in perlinParameters)
                {
                    heightMap[x, y] += Utils.fBM(
                        (x + parameters.xOffset) * parameters.xScale,
                        (y + parameters.yOffset) * parameters.yScale,
                        parameters.octaves,
                        parameters.persistance) * parameters.heightScale;
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void AddNewPerlin()
    {
        perlinParameters.Add(new PerlinParameters());
    }
    public void RemovePerlin()
    {
        // New list for parameters that should be kept
        List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();

        // Loop through current parameters, and check if they should be removed or not
        for (int i = 0; i < perlinParameters.Count; i++)
        {
            if (!perlinParameters[i].remove)
            {
                //if they should not be removed, add them to the kepy list
                keptPerlinParameters.Add(perlinParameters[i]);
            }
        }
        if (keptPerlinParameters.Count == 0)
        {
            //if none are to be kept, then add at least 1 item to the kept list so the table will work
            keptPerlinParameters.Add(perlinParameters[0]);
        }
        // set list to kept list
        perlinParameters = keptPerlinParameters;
    }

    public void RandomTerrain()
    {
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                heightMap[x, y] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
            }
        }
        terrainData.SetHeights(0, 0, heightMap);

    }

    public void LoadTexture()
    {
        float[,] heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                heightMap[x, y] += heightMapImage.GetPixel((int)(x * heightMapScale.x),
                                                          (int)(y * heightMapScale.z)).grayscale
                                                          * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    public void ResetTerrain()
    {
        float[,] heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                heightMap[x, y] = 0;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }
    private void AddTag(SerializedProperty tagsProp, string newTag)
    {
        bool found = false;

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            // stop if tag already exists
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag))
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            // create a new item in the tags array and give it the newTag value
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
    }

    void OnEnable()
    {
        Debug.Log("Initialising Terrain Data");
        terrain = this.GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData;
    }
    void Awake()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTag(tagsProp, "Terrain");
        AddTag(tagsProp, "Cloud");
        AddTag(tagsProp, "Shore");

        // update tags DB
        tagManager.ApplyModifiedProperties();

        // tag this object
        this.gameObject.tag = "Terrain";
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
