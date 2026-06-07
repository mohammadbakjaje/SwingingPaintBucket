using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRSimulationController : MonoBehaviour
{
    [Header("Core Simulation Links")]
    public ElasticRopeSimulator ropeSimulator;
    public SPHFluidSimulator fluidSimulator;
    public DynamicPixelCanvas canvasSimulator;

    [Header("VR Interaction Hand Anchors")]
    public Transform leftHandController;
    public Transform rightHandController;

    private Vector3 prevLeftHandPos;
    private Vector3 prevRightHandPos;
    private Vector3 leftHandVelocity;
    private Vector3 rightHandVelocity;

    void Start()
    {
        if (leftHandController != null) prevLeftHandPos = leftHandController.position;
        if (rightHandController != null) prevRightHandPos = rightHandController.position;
    }

    void Update()
    {
        CalculateManualControllerVelocities();
    }

    void CalculateManualControllerVelocities()
    {
        if (leftHandController != null)
        {
            leftHandVelocity = (leftHandController.position - prevLeftHandPos) / Time.deltaTime;
            prevLeftHandPos = leftHandController.position;
        }

        if (rightHandController != null)
        {
            rightHandVelocity = (rightHandController.position - prevRightHandPos) / Time.deltaTime;
            prevRightHandPos = rightHandController.position;
        }
    }

    // --- UI World Space Configuration Setters Functions ---

    public void UI_SetRopeElasticity(float value)
    {
        if (ropeSimulator != null)
        {
            ropeSimulator.springConstantK = value;
        }
    }

    public void UI_SetAirHumidity(float value)
    {
        if (ropeSimulator != null)
        {
            ropeSimulator.airHumidityResistance = value;
        }
    }

    public void UI_SetFluidViscosity(float value)
    {
        if (fluidSimulator != null)
        {
            fluidSimulator.viscosityMu = value;
        }
    }

    public void UI_SetSurfaceMaterial(int typeIndex)
    {
        if (canvasSimulator != null)
        {
            canvasSimulator.surfaceType = (DynamicPixelCanvas.SurfaceType)typeIndex;
        }
    }

    public void UI_ChangePaintColor(int colorIndex)
    {
        if (canvasSimulator == null) return;
        switch (colorIndex)
        {
            case 0: canvasSimulator.paintColor = Color.red; break;
            case 1: canvasSimulator.paintColor = Color.blue; break;
            case 2: canvasSimulator.paintColor = Color.green; break;
            case 3: canvasSimulator.paintColor = Color.yellow; break;
        }
    }

    // --- VR Hand Pushing Interaction System (No Rigidbody) ---

    public void VR_PushBucketWithLeftHand()
    {
        if (ropeSimulator != null && leftHandController != null)
        {
            Vector3 impulse = leftHandVelocity * 0.4f;
            ropeSimulator.ApplyExternalHandForce(impulse);
        }
    }

    public void VR_PushBucketWithRightHand()
    {
        if (ropeSimulator != null && rightHandController != null)
        {
            Vector3 impulse = rightHandVelocity * 0.4f;
            ropeSimulator.ApplyExternalHandForce(impulse);
        }
    }

    public void CheckAndApplyHandProximityPush(Vector3 handWorldPos, Vector3 handVelocity)
    {
        if (ropeSimulator == null) return;

        Transform bTransform = ropeSimulator.bucketTransform;
        if (bTransform != null)
        {
            float distance = Vector3.Distance(handWorldPos, bTransform.position);
            if (distance < 0.35f)
            {
                Vector3 impulse = handVelocity * 0.5f;
                ropeSimulator.ApplyExternalHandForce(impulse);
            }
        }
    }
}
