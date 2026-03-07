using RobsonRocha.UnityCommon;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class SimplePrefabsManager : SingletonMonoBehaviour<SimplePrefabsManager>
{
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
            GameObject prefab = Instantiate(prefabInfo.Prefab, emitter.transform.position + (Vector3)prefabInfo.Offset, Quaternion.identity);

            Collider2D[] emitterColliders = emitter.GetComponents<Collider2D>();
            for (int i = 0, l =  emitterColliders.Length; i < l; i++)
            {
                Collider2D emitterCollider = emitterColliders[i];
                if (!prefabInfo.CanCollideWithEmitter && emitterCollider != null && prefab.TryGetComponent(out Collider2D prefabCollider))
                {
                    Physics2D.IgnoreCollision(prefabCollider, emitterCollider);
                }
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