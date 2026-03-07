using System;
using UnityEngine;

[Serializable]
public class PrefabInfo
{
    public string Name;
    public GameObject Prefab;
    public Vector2 Offset;
    public bool CanCollideWithEmitter;
}
