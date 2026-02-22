using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimplePrefabsEmitter : MonoBehaviour
{
    [Serializable]
    public class PrefabInfo
    {
        public string Name;
        public GameObject Prefab;
        public Vector2 Offset;
    }

    [SerializeField] private List<PrefabInfo> Prefabs;
    private Dictionary<string, PrefabInfo> _prefabDict;

    private void Awake()
    {
        _prefabDict = Prefabs.ToDictionary(pp => pp.Name, pp => pp);
    }

    public void Emit(string name)
    {
        if (_prefabDict.TryGetValue(name, out var prefabInfo))
        {
            Instantiate(prefabInfo.Prefab, transform.position + (Vector3)prefabInfo.Offset, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"No prefab found for name: {name}");
        }
    }

}