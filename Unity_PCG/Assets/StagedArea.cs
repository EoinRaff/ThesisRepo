﻿using MED10.Architecture.Events;
using MED10.Architecture.Variables;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MED10.PCG.TerrainGenerator;

public class StagedArea : MonoBehaviour
{
    public bool stagedAreaStarted = false;
    public bool stagedAreaEnded = false;

    //public IntVariable StagedAreaIndex;

    public Vector2 size;
    [Range(0, 1)]
    public float minHeight;
    [Range(0, 1)] 
    public float maxHeight;
    [Range(0, 90)]
    public float minSlope;
    [Range(0, 90)]
    public float maxSlope;
    [Range(0, 1)]
    public float flattenPower;

    public GameEvent EnteredStagedArea;
    public GameEvent ExitStagedArea;

    public FlattenType flattenType;
    public FloatVariable progress;
    public GameEvent candidatesDoneEvent;
    public GameEvent areaSpawned;
    
    public bool candidatesReady;
    public bool spawned;
    public bool DelaySpawn;

    public float minDistanceFromPath;
    public float minDistFromPlayer;
    public float maxDistFromPlayer;
    public float minAngle;

    private void OnTriggerEnter(Collider other)
    {
        if (!stagedAreaStarted)
        {
            stagedAreaStarted = true;
            EnteredStagedArea.Raise();
        }

    }
    private void OnTriggerExit(Collider other)
    {
        if (stagedAreaStarted && !stagedAreaEnded)
        {
            stagedAreaEnded = true;
            ExitStagedArea.Raise();
        }
    }
}
