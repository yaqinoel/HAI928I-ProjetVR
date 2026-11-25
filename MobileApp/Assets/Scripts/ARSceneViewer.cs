using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class ARSceneViewer : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private GameObject placementPrefab;
    [SerializeField] private TMP_Text trackingText; // text showing tracking state
    [SerializeField] private ARModelGestures gestures;

    private readonly List<ARRaycastHit> hits = new();
    private ARAnchor activeAnchor;
    private bool placing;


    // load AR Session
    private void OnEnable()
    {
        ARSession.stateChanged += OnTrackingChanged;    // registe callback for event(AR state changed)
        ShowTrackingState(ARSession.state);
    }

    private void OnDisable()
    {
        ARSession.stateChanged -= OnTrackingChanged;    // unregiste callback
    }

    private void OnTrackingChanged(ARSessionStateChangedEventArgs args)
    {
        ShowTrackingState(args.state);
    }

    // update AR status text
    private void ShowTrackingState(ARSessionState state)
    {
        if (state != ARSessionState.SessionTracking)
            trackingText.text = "AR session is loading";
        else if (placing)
            trackingText.text = "Placing an objet...";
        else if (activeAnchor != null)
            trackingText.text = "Objet placed, walk around to look.";
        else
            trackingText.text = "Touch a surface horizontal to place the objet.";
    }

    // click screen to place a objet
    public async void OnPointerClick(PointerEventData eventData)
    {   
        // check if is ready to place the objet
        if (eventData.button != PointerEventData.InputButton.Left
            || placing || activeAnchor != null || ARSession.state != ARSessionState.SessionTracking)
        {
            return;
        }

        // ray not touche any plane
        if (!raycastManager.Raycast(eventData.position, hits, TrackableType.PlaneWithinPolygon))
        {
            trackingText.text = "Move your phone slowly to scan the scene.";
            return;
        }

        // start to place the objet
        placing = true;
        ShowTrackingState(ARSession.state);
        try
        {
            Pose pose = new Pose(hits[0].pose.position, Quaternion.identity);
            var result = await anchorManager.TryAddAnchorAsync(pose);
            if (this == null)
                return;

            if (!result.status.IsSuccess() || result.value == null)
            {
                trackingText.text = "Can not to place the objet, try again.";
                return;
            }
            
            // anchor for objet in the scene
            activeAnchor = result.value;
            GameObject instance = Instantiate(placementPrefab, activeAnchor.transform, false);
            gestures.SetTarget(instance.transform);
            trackingText.text = "Objet placed, walk around to look.";
        }
        catch (Exception error)
        {
            if (this != null)
            {
                trackingText.text = "Can not to place the objet, try again.";
                Debug.LogException(error, this);
            }
        }
        finally
        {
            placing = false;
        }
    }

    // clear old objet and ready to put a new
    public void ResetPlacement()
    {
        if (placing)
            return;

        if (activeAnchor != null && !anchorManager.TryRemoveAnchor(activeAnchor))
        {
            trackingText.text = "Impossible to remove anchor, try again";
            return;
        }

        activeAnchor = null;
        gestures.SetTarget(null);
        ShowTrackingState(ARSession.state);
    }


    public void GoBackToLibrary()
    {
        SceneManager.LoadScene("SceneLibrary");
    }

}
