using UnityEngine;

public class Portal : MonoBehaviour
{
    [Tooltip("The portal this one connects to.")]
    public Portal linkedPortal;

    [Header("Portal Rendering")]
    public Camera portalCamera;
    public MeshRenderer screenRenderer;

    private RenderTexture viewTexture;
    private int lastTextureWidth = 0;
    private int lastTextureHeight = 0;

    private void Awake()
    {
        portalCamera = GetComponentInChildren<Camera>(true);
        screenRenderer = GetComponentInChildren<MeshRenderer>(true);

        if (portalCamera != null)
        {
            portalCamera.transform.SetParent(transform, false);
            portalCamera.transform.localPosition = Vector3.zero;
            portalCamera.transform.localRotation = Quaternion.identity; // look forward by default
        }
        else
        {
            Debug.LogWarning($"{name}: No child camera found.");
        }

        if (screenRenderer != null)
        {
            Material mat = new Material(screenRenderer.sharedMaterial);
            screenRenderer.material = mat;
        }

        EnsureRenderTexture();
    }

    private void LateUpdate()
    {
        if (linkedPortal == null || portalCamera == null || screenRenderer == null) return;
        if (Screen.width != lastTextureWidth || Screen.height != lastTextureHeight)
        EnsureRenderTexture();

        // Keep the portal camera at the portal
        portalCamera.transform.localPosition = Vector3.zero;

        // Make the camera look forward relative to this portal, but flipped 180° around Y
        portalCamera.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        // Assign camera's render texture to linked portal
        if (linkedPortal.screenRenderer != null && portalCamera.targetTexture != null)
        {
            Material mat = linkedPortal.screenRenderer.material;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", portalCamera.targetTexture);
            else
                mat.mainTexture = portalCamera.targetTexture;
        }
    }
    private void EnsureRenderTexture()
    {
        int w = Mathf.Max(16, Screen.width);
        int h = Mathf.Max(16, Screen.height);

        if (viewTexture != null && viewTexture.width == w && viewTexture.height == h) return;

        if (viewTexture != null)
        {
            portalCamera.targetTexture = null;
            viewTexture.Release();
            Destroy(viewTexture);
        }

        viewTexture = new RenderTexture(w, h, 24);
        viewTexture.name = $"{name}_PortalTexture_{GetInstanceID()}";
        viewTexture.Create();

        portalCamera.targetTexture = viewTexture;

        lastTextureWidth = w;
        lastTextureHeight = h;
    }

    private void OnDisable()
    {
        if (viewTexture != null)
        {
            if (portalCamera != null) portalCamera.targetTexture = null;
            viewTexture.Release();
            Destroy(viewTexture);
            viewTexture = null;
        }
    }


}
