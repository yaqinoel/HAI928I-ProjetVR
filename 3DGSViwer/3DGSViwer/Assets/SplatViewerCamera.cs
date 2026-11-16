using Gsplat;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public sealed class SplatViewerCamera : MonoBehaviour
{
    private Camera viewCamera;
    private Vector3 pivot;
    private float yaw;
    private float pitch;
    private float distance = 10f;
    private float sceneRadius = 5f;

    private void Start()
    {
        viewCamera = GetComponent<Camera>();
        pivot = transform.position + transform.forward * distance;
        yaw = transform.eulerAngles.y;
        pitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
        FrameSplat();
    }

    private void LateUpdate()
    {
        if (Keyboard.current?.fKey.wasPressedThisFrame == true)
            FrameSplat();

        var mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 delta = mouse.delta.ReadValue();
        if (mouse.leftButton.isPressed)
        {
            yaw += delta.x * 0.2f;
            pitch = Mathf.Clamp(pitch - delta.y * 0.2f, -85f, 85f);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        if (mouse.rightButton.isPressed)
        {
            float viewHeight = 2f * distance * Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float unitsPerPixel = viewHeight / Mathf.Max(viewCamera.pixelHeight, 1);
            pivot -= rotation * new Vector3(delta.x, delta.y, 0f) * unitsPerPixel;
        }

        float scroll = Mathf.Clamp(mouse.scroll.ReadValue().y, -1f, 1f);
        distance = Mathf.Clamp(distance * Mathf.Exp(-0.16f * scroll), sceneRadius * 0.02f, sceneRadius * 30f);
        transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
    }

    private void FrameSplat()
    {
        var splat = FindFirstObjectByType<GsplatRenderer>();
        if (splat == null || splat.GsplatAsset == null)
            return;

        Bounds bounds = splat.GsplatAsset.Bounds;
        Vector3 scale = splat.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        sceneRadius = Mathf.Max(bounds.extents.magnitude * maxScale, 0.01f);
        pivot = splat.transform.TransformPoint(bounds.center);

        float verticalHalfFov = viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        float horizontalHalfFov = Mathf.Atan(Mathf.Tan(verticalHalfFov) * viewCamera.aspect);
        distance = sceneRadius * 1.1f / Mathf.Sin(Mathf.Min(verticalHalfFov, horizontalHalfFov));
        viewCamera.nearClipPlane = Mathf.Max(sceneRadius * 0.001f, 0.0001f);
        viewCamera.farClipPlane = Mathf.Max(sceneRadius * 100f, 10f);
    }
}
