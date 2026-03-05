using RobsonRocha.UnityCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimplePrefabsManager : SingletonMonoBehaviour<SimplePrefabsManager>
{
    [Serializable]
    public class PrefabInfo
    {
        public string Name;
        public GameObject Prefab;
        public Vector2 Offset;
        public bool CanCollideWithEmitter;
    }

    [SerializeField] private List<PrefabInfo> Prefabs;
    private Dictionary<string, PrefabInfo> _prefabDict;

    protected override void Awake()
    {
        if(!CanAwake()) return;
        _prefabDict = Prefabs.ToDictionary(pp => pp.Name, pp => pp);
    }

    public void Emit(GameObject emitter, string name)
    {
        if (_prefabDict.TryGetValue(name, out var prefabInfo))
        {
            emitter.TryGetComponent(out Collider2D collider);

            GameObject prefab = Instantiate(prefabInfo.Prefab, emitter.transform.position + (Vector3)prefabInfo.Offset, Quaternion.identity);
            if (!prefabInfo.CanCollideWithEmitter && collider != null && prefab.TryGetComponent(out Collider2D prefabCollider))
            {
                Physics2D.IgnoreCollision(prefabCollider, collider);
            }
            if (prefab.TryGetComponent(out IEmittable emittable))
            {
                emittable.OnEmit(emitter);
            }
        }
        else
        {
            Debug.LogWarning($"No prefab found for name: {name}");
        }
    }
}