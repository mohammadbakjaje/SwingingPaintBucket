using System;
using UnityEngine;

/// <summary>
/// Dynamic canvas that listens to physics paint events and paints into a Texture2D
/// without using Colliders or Raycasts. Subscribes to PhysicsEvents.OnPaintSplatter
/// (and attempts to subscribe to OnPaintSplatterSplatted if present via reflection).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class DynamicCanvas : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;
    public Color clearColor = Color.white;

    [Header("Paint Settings")]
    public Color currentPaintColor = Color.red;

    Texture2D dynamicTexture;
    Color32[] pixelData;
    Renderer quadRenderer;
    bool needsApply = false;

    // Public for validator test to check that we received the simulated event
    public static int ReceivedPaintCount { get; set; } = 0;

    void Awake()
    {
        Debug.Log("DynamicCanvas: Awake");
        quadRenderer = GetComponent<Renderer>();
        RemoveColliderFromQuad();
        CreateDynamicTexture();
        ApplyTextureToRenderer();
    }

    void OnEnable()
    {
        Debug.Log("DynamicCanvas: OnEnable subscribing to physics events.");
        // Subscribe to the known event signature in the project
        try
        {
            PhysicsEvents.OnPaintSplatter += OnPaintSplatter;
        }
        catch (Exception)
        {
            // ignore if not present/compatible at compile-time
        }

        // Also attempt to subscribe to an alternate event name if it exists (reflection)
        TrySubscribeAlternateEvent("OnPaintSplatterSplatted");
    }

    void OnDisable()
    {
        try
        {
            PhysicsEvents.OnPaintSplatter -= OnPaintSplatter;
        }
        catch (Exception)
        {
        }

        TryUnsubscribeAlternateEvent("OnPaintSplatterSplatted");
    }

    void LateUpdate()
    {
        if (needsApply && dynamicTexture != null && pixelData != null)
        {
            dynamicTexture.SetPixels32(pixelData);
            dynamicTexture.Apply();
            needsApply = false;
        }
    }

    void CreateDynamicTexture()
    {
        dynamicTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        dynamicTexture.filterMode = FilterMode.Bilinear;
        dynamicTexture.wrapMode = TextureWrapMode.Clamp;

        pixelData = new Color32[textureWidth * textureHeight];
        Color32 clearColor32 = clearColor;
        for (int i = 0; i < pixelData.Length; i++)
            pixelData[i] = clearColor32;

        dynamicTexture.SetPixels32(pixelData);
        dynamicTexture.Apply();
    }

    void ApplyTextureToRenderer()
    {
        if (quadRenderer == null)
            quadRenderer = GetComponent<Renderer>();

        if (quadRenderer == null)
            return;

        quadRenderer.sharedMaterial = new Material(quadRenderer.sharedMaterial);
        quadRenderer.sharedMaterial.mainTexture = dynamicTexture;
        quadRenderer.sharedMaterial.mainTextureScale = Vector2.one;
        quadRenderer.sharedMaterial.mainTextureOffset = Vector2.zero;
    }

    void RemoveColliderFromQuad()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }
    }

    // Handler matching PhysicsEvents.OnPaintSplatter signature: (Vector3 position, Color color, float speed, float viscosity)
    void OnPaintSplatter(Vector3 position, Color color, float speed, float viscosity)
    {
        Debug.Log($"DynamicCanvas: OnPaintSplatter received position={position} speed={speed} viscosity={viscosity}");
        // choose radius based on viscosity (tunable) and speed may affect alpha
        float radius = Mathf.Clamp01(0.05f + viscosity * 0.2f);
        currentPaintColor = color;
        // perform painting; use speed as intensity
        PaintAtPoint(position, radius, Mathf.Max(0.01f, speed));
        ReceivedPaintCount++;
    }

    // Convert world position to pixel coordinates without colliders and paint
    public void PaintAtPoint(Vector3 worldPos, float radius, float currentSpeed)
    {
        if (dynamicTexture == null || pixelData == null || quadRenderer == null)
            return;

        // convert to local space of the canvas
        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        // Use the renderer's local bounds to map local position to texture UVs.
        // This handles meshes that are not exactly -0.5..0.5 and supports non-square boards.
        Bounds localBounds = quadRenderer.localBounds;
        Vector3 min = localBounds.min;
        Vector3 size = localBounds.size;

        float u = size.x > 1e-6f ? (localPos.x - min.x) / size.x : 0.5f;
        float v = size.y > 1e-6f ? (localPos.y - min.y) / size.y : 0.5f;

        // clamp UVs strictly to [0,1] range
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        int centerX = Mathf.Clamp(Mathf.FloorToInt(u * textureWidth), 0, textureWidth - 1);
        int centerY = Mathf.Clamp(Mathf.FloorToInt(v * textureHeight), 0, textureHeight - 1);

        // Convert world-space radius to pixel radius using world-scaled bounds.
        // Use the minimum pixel scale of X and Y so the spot remains circular even for stretched rectangles.
        Vector3 worldSize = quadRenderer.bounds.size;
        float worldWidth = Mathf.Max(worldSize.x, 1e-6f);
        float worldHeight = Mathf.Max(worldSize.y, 1e-6f);
        float pixelScaleX = textureWidth / worldWidth;
        float pixelScaleY = textureHeight / worldHeight;
        int pixelRadius = Mathf.CeilToInt(radius * Mathf.Min(pixelScaleX, pixelScaleY));
        pixelRadius = Mathf.Max(pixelRadius, 1);
        int radiusSqr = pixelRadius * pixelRadius;

        Color32 paintColor32 = currentPaintColor;

        int startX = Mathf.Clamp(centerX - pixelRadius, 0, textureWidth - 1);
        int endX = Mathf.Clamp(centerX + pixelRadius, 0, textureWidth - 1);
        int startY = Mathf.Clamp(centerY - pixelRadius, 0, textureHeight - 1);
        int endY = Mathf.Clamp(centerY + pixelRadius, 0, textureHeight - 1);

        for (int x = startX; x <= endX; x++)
        {
            int dx = x - centerX;
            int dxSqr = dx * dx;
            for (int y = startY; y <= endY; y++)
            {
                int dy = y - centerY;
                int distSqr = dxSqr + dy * dy;
                if (distSqr > radiusSqr)
                    continue;

                float strength = 1f - (Mathf.Sqrt(distSqr) / pixelRadius);
                float alpha = Mathf.Clamp01(strength * currentSpeed * Time.deltaTime * 10f);

                int index = y * textureWidth + x;
                if (index < 0 || index >= pixelData.Length)
                    continue; // defensive clamp

                Color existing = pixelData[index];
                Color blended = Color.Lerp(existing, paintColor32, alpha);
                pixelData[index] = blended;
            }
        }

        needsApply = true;
    }

    void OnDestroy()
    {
        if (dynamicTexture != null)
            Destroy(dynamicTexture);

        if (quadRenderer != null && quadRenderer.sharedMaterial != null)
            Destroy(quadRenderer.sharedMaterial);
    }

    #region Reflection Helpers for alternate event name
    void TrySubscribeAlternateEvent(string name)
    {
        var t = typeof(PhysicsEvents);
        var ev = t.GetEvent(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (ev != null)
        {
            // create a delegate of the correct type pointing to OnPaintSplatter
            try
            {
                var handler = Delegate.CreateDelegate(ev.EventHandlerType, this, "OnPaintSplatter");
                ev.AddEventHandler(null, handler);
            }
            catch (Exception)
            {
                // ignore if signature mismatch
            }
            return;
        }

        // fallback: check for a delegate field with that name and combine
        var fi = t.GetField(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (fi != null)
        {
            try
            {
                var existing = fi.GetValue(null) as MulticastDelegate;
                var handler = Delegate.CreateDelegate(fi.FieldType, this, "OnPaintSplatter");
                var combined = Delegate.Combine(existing, handler);
                fi.SetValue(null, combined);
            }
            catch (Exception)
            {
            }
        }
    }

    void TryUnsubscribeAlternateEvent(string name)
    {
        var t = typeof(PhysicsEvents);
        var ev = t.GetEvent(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (ev != null)
        {
            try
            {
                var handler = Delegate.CreateDelegate(ev.EventHandlerType, this, "OnPaintSplatter");
                ev.RemoveEventHandler(null, handler);
            }
            catch (Exception)
            {
            }
            return;
        }

        var fi = t.GetField(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (fi != null)
        {
            try
            {
                var existing = fi.GetValue(null) as MulticastDelegate;
                var handler = Delegate.CreateDelegate(fi.FieldType, this, "OnPaintSplatter");
                var removed = Delegate.Remove(existing, handler);
                fi.SetValue(null, removed);
            }
            catch (Exception)
            {
            }
        }
    }
    #endregion
}
