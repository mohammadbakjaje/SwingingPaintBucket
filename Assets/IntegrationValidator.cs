using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Place this component in the scene (e.g., on an empty GameObject).
/// On Play it performs three checks:
/// 1) No Collider components present in the active scene(s).
/// 2) PhysicsEvents.OnPaintSplatter (or alternate) has subscribers.
/// 3) Fires a simulated paint event and verifies DynamicCanvas received it.
/// </summary>
public class IntegrationValidator : MonoBehaviour
{
    void Start()
    {
        Debug.Log("IntegrationValidator: Start running.");
        StartCoroutine(RunValidation());
    }

    IEnumerator RunValidation()
    {
        Debug.Log("IntegrationValidator: Running validation sequence.");
        // 1) Check for any Collider in loaded scenes (including inactive)
        var allColliders = Resources.FindObjectsOfTypeAll<Collider>();
        var sceneColliders = allColliders.Where(c => c != null && c.gameObject != null && c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded).ToArray();

        if (sceneColliders.Length > 0)
        {
            foreach (var c in sceneColliders)
            {
                Debug.LogError($"Collider found on GameObject '{c.gameObject.name}' in scene '{c.gameObject.scene.name}'. Colliders are forbidden.");
            }
        }
        else
        {
            Debug.Log("IntegrationValidator: No Colliders detected in loaded scenes.");
        }

        // 2) Check that PhysicsEvents has subscribers for the paint event
        bool hasSubscribers = false;
        var physicsType = typeof(PhysicsEvents);

        // try known field name first
        var fi = physicsType.GetField("OnPaintSplatter", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (fi != null)
        {
            var del = fi.GetValue(null) as MulticastDelegate;
            hasSubscribers = del != null && del.GetInvocationList().Length > 0;
        }

        // also check alternate name
        var fiAlt = physicsType.GetField("OnPaintSplatterSplatted", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (!hasSubscribers && fiAlt != null)
        {
            var del2 = fiAlt.GetValue(null) as MulticastDelegate;
            hasSubscribers = del2 != null && del2.GetInvocationList().Length > 0;
        }

        if (!hasSubscribers)
        {
            Debug.LogError("IntegrationValidator: Physics paint event has no subscribers. Ensure DynamicCanvas is present and subscribed.");
        }
        else
        {
            Debug.Log("IntegrationValidator: Physics paint event has subscribers.");
        }

        // 3) Simulation test: fire a dummy paint event and check DynamicCanvas responded
        // Reset counter
        DynamicCanvas.ReceivedPaintCount = 0;

        // Fire the event safely using the public trigger if available
        MethodInfo trigger = physicsType.GetMethod("TriggerPaintSplatter", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (trigger != null)
        {
            // send to world origin; color red; speed 1; viscosity 0.5
            trigger.Invoke(null, new object[] { transform.position, Color.red, 1f, 0.5f });
        }
        else
        {
            // fallback: try invoking delegate field(s)
            if (fi != null)
            {
                var del = fi.GetValue(null) as MulticastDelegate;
                del?.DynamicInvoke(new object[] { transform.position, Color.red, 1f, 0.5f });
            }

            if (fiAlt != null)
            {
                var del2 = fiAlt.GetValue(null) as MulticastDelegate;
                del2?.DynamicInvoke(new object[] { transform.position, Color.red, 1f, 0.5f });
            }
        }

        // wait one frame to let subscribers run
        yield return null;

        if (DynamicCanvas.ReceivedPaintCount > 0)
        {
            Debug.Log("IntegrationValidator: Simulation succeeded — DynamicCanvas received paint event.");
        }
        else
        {
            Debug.LogError("IntegrationValidator: Simulation failed — no DynamicCanvas reported receiving the paint event.");
        }
    }
}
