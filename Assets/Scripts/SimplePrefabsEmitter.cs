using UnityEngine;

public class SimplePrefabsEmitter : MonoBehaviour
{
    public void Emit(string name) => 
        SimplePrefabsManager.Instance.Emit(gameObject, name);
}