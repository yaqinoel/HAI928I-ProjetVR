using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;

public sealed class ARModelGestures : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private Camera arCamera;

    private readonly Dictionary<int, Vector2> fingers = new();
    private Transform model;
    private Vector3 baseScale;
    private float scaleFactor;

    private Plane dragPlane;
    private Vector3 dragOffset;
    private bool dragReady;

    private float pinchStartDistance;
    private float pinchStartScale;
    private Vector2 previousSpan;

    public void SetTarget(Transform target)
    {
        fingers.Clear();
        model = target;
        baseScale = target != null ? target.localScale : Vector3.one;
        scaleFactor = 1f;
        ResetGestureBaseline();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (model == null || e.button != PointerEventData.InputButton.Left || fingers.Count >= 2)
            return;

        fingers[e.pointerId] = e.position;
        ResetGestureBaseline();
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (fingers.Remove(e.pointerId))
            ResetGestureBaseline();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!fingers.ContainsKey(e.pointerId))
            return;

        fingers[e.pointerId] = e.position;
        if (model == null || ARSession.state != ARSessionState.SessionTracking)
            return;

        if (fingers.Count == 1)
        {
            if (dragReady && TryProject(e.position, out var point))
                model.position = point + dragOffset;
            return;
        }

        ApplyPinchAndRotation();
    }

    private void ResetGestureBaseline()
    {
        dragReady = false;
        pinchStartDistance = 0f;

        if (model == null || fingers.Count != 1)
            return;

        var pointer = fingers.Values.GetEnumerator();
        pointer.MoveNext();
        dragPlane = new Plane(Vector3.up, model.position);
        dragReady = TryProject(pointer.Current, out var hit);
        dragOffset = model.position - hit;
    }

    private bool TryProject(Vector2 screenPoint, out Vector3 point)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPoint);
        if (dragPlane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = default;
        return false;
    }

    private void ApplyPinchAndRotation()
    {
        var pointer = fingers.GetEnumerator();
        pointer.MoveNext();
        var first = pointer.Current;
        pointer.MoveNext();
        var second = pointer.Current;

        // Keep the vector direction consistent between events.
        Vector2 span = first.Key < second.Key
            ? second.Value - first.Value
            : first.Value - second.Value;
        float distance = span.magnitude;

        if (distance < 8f)
        {
            pinchStartDistance = 0f;
            return;
        }

        if (pinchStartDistance == 0f)
        {
            pinchStartDistance = distance;
            pinchStartScale = scaleFactor;
            previousSpan = span;
        }

        scaleFactor = Mathf.Clamp(pinchStartScale * distance / pinchStartDistance, 0.5f, 3f);
        model.localScale = baseScale * scaleFactor;

        float twist = Vector2.SignedAngle(previousSpan, span);
        model.Rotate(Vector3.up, -twist, Space.World);
        previousSpan = span;
    }

    private void OnDisable()
    {
        fingers.Clear();
        ResetGestureBaseline();
    }
}
