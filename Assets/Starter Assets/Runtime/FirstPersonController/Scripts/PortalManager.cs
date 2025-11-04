using UnityEngine;

/// <summary>
/// Manages the spawning and placement of portals in the scene.
/// Optimized with object pooling to avoid unnecessary instantiation and destruction.
/// </summary>
public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance;

    [Header("Portal Prefabs")]
    public Portal bluePortalPrefab;
    public Portal orangePortalPrefab;

    private Portal bluePortalInstance;
    private Portal orangePortalInstance;

    private readonly Vector3 portalHalfExtents = new Vector3(0.5f, 1f, 0.05f); // Match your portalSize
    private const float surfaceOffset = 0.01f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlacePortal(PortalShooter.PortalColor color, Vector3 position, Vector3 normal)
    {
        Quaternion rotation = Quaternion.LookRotation(-normal);
        Vector3 offsetPosition = position + normal * surfaceOffset;

        if (Physics.CheckBox(offsetPosition, portalHalfExtents, rotation, ~LayerMask.GetMask("PortalSurface")))
        {
            if (!TryAutoAdjust(position, normal, rotation, out Vector3 adjustedPosition))
            {
                Debug.Log("❌ No space for portal, even after auto-adjust.");
                return;
            }

            offsetPosition = adjustedPosition + normal * surfaceOffset;
        }

        // Spawn or reuse portal
        if (color == PortalShooter.PortalColor.Blue)
        {
            if (bluePortalInstance == null)
                bluePortalInstance = Instantiate(bluePortalPrefab);

            ActivatePortal(bluePortalInstance, offsetPosition, rotation);
        }
        else
        {
            if (orangePortalInstance == null)
                orangePortalInstance = Instantiate(orangePortalPrefab);

            ActivatePortal(orangePortalInstance, offsetPosition, rotation);
        }
    }

    private bool TryAutoAdjust(Vector3 origin, Vector3 normal, Quaternion rotation, out Vector3 validPosition)
    {
        float searchRadius = 0.5f;
        int maxTries = 16;

        for (int i = 0; i < maxTries; i++)
        {
            float angle = (360f / maxTries) * i;
            Vector2 offset2D = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * searchRadius;
            Vector3 localOffset = new Vector3(offset2D.x, offset2D.y, 0f);
            Vector3 worldOffset = rotation * localOffset;

            Vector3 checkPosition = origin + worldOffset + normal * surfaceOffset;

            if (!Physics.CheckBox(checkPosition, portalHalfExtents, rotation, ~LayerMask.GetMask("PortalSurface")))
            {
                validPosition = origin + worldOffset;
                return true;
            }
        }

        validPosition = Vector3.zero;
        return false;
    }

    private void ActivatePortal(Portal portal, Vector3 position, Quaternion rotation)
    {
        portal.transform.SetPositionAndRotation(position, rotation);

        // Link the portals if both exist
        if (portal == bluePortalInstance && orangePortalInstance != null)
        {
            portal.linkedPortal = orangePortalInstance;
            orangePortalInstance.linkedPortal = portal;
        }
        else if (portal == orangePortalInstance && bluePortalInstance != null)
        {
            portal.linkedPortal = bluePortalInstance;
            bluePortalInstance.linkedPortal = portal;
        }

        if (!portal.gameObject.activeSelf)
            portal.gameObject.SetActive(true);
    }

    public Portal GetBluePortal() => bluePortalInstance;
    public Portal GetOrangePortal() => orangePortalInstance;
}
