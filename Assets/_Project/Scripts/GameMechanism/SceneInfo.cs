using UnityEngine;
using System;
using System.Collections.Generic;

public struct SceneInfo
{
    public List<GameObject> foods;
    public List<GameObject> vessels;
    public Vector3 position;
    public int type_of_scene; // 1 for stove, 2 for others

    // Constructor
    public SceneInfo(int type, Vector3 scenePosition)
    {
        type_of_scene = type;
        position = scenePosition;
        foods = new List<GameObject>();
        vessels = new List<GameObject>();
    }
}