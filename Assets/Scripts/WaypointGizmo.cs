using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class WaypointGizmo : MonoBehaviour
{
    [SerializeField] private float Radius = 0.2f;
    [SerializeField] private Vector3 LabelOffset = new(0f, 0.3f, 0f);

    [SerializeField][HideInInspector] private int WaypointDisplayIndex = -1;
    [SerializeField][HideInInspector] private Color WaypointColor = default;

    public void SetWaypointMetadata(int displayIndex, Color color)
    {
        WaypointDisplayIndex = displayIndex;
        WaypointColor = color;
    }

    private void OnDrawGizmos()
    {
        Color gizmoColor = WaypointColor.a > 0f ? WaypointColor : Color.cyan;
        Color previousColor = Gizmos.color;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, Radius);
        Gizmos.color = previousColor;

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = gizmoColor },
            alignment = TextAnchor.MiddleCenter
        };

        string label = WaypointDisplayIndex > 0 ? $"#{WaypointDisplayIndex}" : "#?";
        Handles.Label(transform.position + LabelOffset, label, style);
#endif
    }
}
