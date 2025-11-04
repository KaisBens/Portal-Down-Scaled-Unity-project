using UnityEngine;

/// <summary>
/// Handles user input to shoot and place portals intelligently on portalable surfaces.
/// Performs placement validation, proximity checks, and intelligent adjustment to avoid clipping or overlapping.
/// </summary>
public class PortalShooter : MonoBehaviour
{
    public enum PortalColor { Blue, Orange }

    [Header("Settings")]
    public PortalColor portalColor;
    public Transform firePoint;
    public float maxDistance = 100f;
    public LayerMask portalSurfaceLayer;
    public Vector2 portalSize = new Vector2(1f, 2f); // Width x Height

    private void Update()
    {
        bool fire = portalColor == PortalColor.Blue ? Input.GetMouseButtonDown(0) : Input.GetMouseButtonDown(1);
        if (!fire) return;

        Ray ray = new Ray(firePoint.position, firePoint.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, portalSurfaceLayer))
        {
            if (!hit.collider.CompareTag("Portalable")) return;

            if (IsSurfaceBigEnough(hit.point, hit.normal) &&
                !IsTooCloseToOtherPortal(hit.point))
            {
                PortalManager.Instance.PlacePortal(portalColor, hit.point, hit.normal);
                return;
            }

            if (FindNearbyValidSpot(hit.point, hit.normal, out Vector3 adjustedPoint))
            {
                PortalManager.Instance.PlacePortal(portalColor, adjustedPoint, hit.normal);
            }
            else
            {
                Debug.LogWarning("⚠️ No valid portal spot found nearby.");
            }
        }
    }

    private bool IsTooCloseToOtherPortal(Vector3 newPosition)
    {
        Portal otherPortal = portalColor == PortalColor.Blue
            ? PortalManager.Instance.GetOrangePortal()
            : PortalManager.Instance.GetBluePortal();

        if (otherPortal == null) return false;

        float minDistance = Mathf.Max(portalSize.x, portalSize.y) * 0.6f;
        float distance = Vector3.Distance(newPosition, otherPortal.transform.position);

        return distance < minDistance;
    }

    private bool IsSurfaceBigEnough(Vector3 point, Vector3 normal)
    {
        Quaternion rotation = Quaternion.LookRotation(-normal);
        Vector3 up = rotation * Vector3.up;
        Vector3 right = rotation * Vector3.right;

        float halfHeight = portalSize.y / 2f;
        float halfWidth = portalSize.x / 2f;

        Vector3[] corners = new Vector3[]
        {
            point + up * halfHeight + right * halfWidth,
            point + up * halfHeight - right * halfWidth,
            point - up * halfHeight + right * halfWidth,
            point - up * halfHeight - right * halfWidth
        };

        foreach (var corner in corners)
        {
            Vector3 origin = corner + normal * 0.1f;
            if (!Physics.Raycast(origin, -normal, out RaycastHit cornerHit, 0.2f, portalSurfaceLayer) ||
                !cornerHit.collider.CompareTag("Portalable"))
            {
                Debug.DrawRay(origin, -normal * 0.2f, Color.red, 1f);
                return false;
            }

            Debug.DrawRay(origin, -normal * 0.2f, Color.green, 1f);
        }

        return true;
    }

    private bool FindNearbyValidSpot(Vector3 origin, Vector3 normal, out Vector3 result)
    {
        float maxShift = 2f;
        float step = 0.1f;
        Quaternion rotation = Quaternion.LookRotation(-normal);
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;

        Portal otherPortal = portalColor == PortalColor.Blue
            ? PortalManager.Instance.GetOrangePortal()
            : PortalManager.Instance.GetBluePortal();

        Vector3 preferredDir = right;

        if (otherPortal != null)
        {
            Vector3 toOther = origin - otherPortal.transform.position;
            float horizontalDot = Vector3.Dot(toOther, right);
            float verticalDot = Vector3.Dot(toOther, up);

            preferredDir = Mathf.Abs(horizontalDot) > Mathf.Abs(verticalDot)
                ? (horizontalDot >= 0 ? right : -right)
                : (verticalDot >= 0 ? up : -up);
        }

        for (float offset = step; offset <= maxShift; offset += step)
        {
            Vector3 shifted = origin + preferredDir * offset;
            if (IsSurfaceBigEnough(shifted, normal) && !IsTooCloseToOtherPortal(shifted))
            {
                result = shifted;
                return true;
            }
        }

        for (float offset = step; offset <= maxShift; offset += step)
        {
            Vector3 shifted = origin - preferredDir * offset;
            if (IsSurfaceBigEnough(shifted, normal) && !IsTooCloseToOtherPortal(shifted))
            {
                result = shifted;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }
}
