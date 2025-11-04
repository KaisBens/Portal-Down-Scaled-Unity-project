using UnityEngine;

public class PortalTraveller : MonoBehaviour
{
    public Portal currentPortal;
    private bool isOverlapping;
    private float teleportCooldown = 0.1f;
    private float lastTeleportTime = -999f;

    // collider caches
    private Collider _col;
    private CapsuleCollider _capsule;
    private CharacterController _charController;

    // last-frame signed distances for capsule sphere centers (or fallback closest point)
    private float lastSignedA = float.PositiveInfinity;
    private float lastSignedB = float.PositiveInfinity;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"PortalTraveller: OnTriggerEnter {other.name}");
        Portal portal = other.GetComponentInParent<Portal>();
        if (portal == null) return;

        currentPortal = portal;
        isOverlapping = true;

        _col = _col ?? GetComponentInParent<Collider>();
        _capsule = _capsule ?? GetComponentInParent<CapsuleCollider>();
        _charController = _charController ?? GetComponentInParent<CharacterController>();

        // initialize samples
        UpdateLastSignedDistances();

        // IMMEDIATE CHECK: if any sample is already behind the portal plane, teleport now
        float a, b;
        if (_capsule != null || _charController != null)
            ComputeCapsuleSignedDistances(out a, out b);
        else
            a = b = ComputeClosestSignedDistance();

        Debug.Log($"PortalTraveller: Immediate check on enter signedA={a:F3} signedB={b:F3}");
        if (currentPortal != null && currentPortal.linkedPortal != null && (a <= 0f || b <= 0f))
        {
            // small safety: avoid teleport spam
            if (Time.time - lastTeleportTime >= teleportCooldown)
                Teleport();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Portal portal = other.GetComponentInParent<Portal>();
        if (portal != null && portal == currentPortal)
        {
            currentPortal = null;
            isOverlapping = false;
            lastSignedA = lastSignedB = float.PositiveInfinity;
        }
    }

    private void Update()
    {
        if (!isOverlapping || currentPortal == null || currentPortal.linkedPortal == null) return;
        if (Time.time - lastTeleportTime < teleportCooldown) return;
        if (_col == null) _col = GetComponent<Collider>();

        // compute current signed distances depending on collider type
        float currentA, currentB;
        if (_capsule != null)
        {
            ComputeCapsuleSignedDistances(out currentA, out currentB);
        }
        else
        {
            // fallback: use closest point (single sample)
            float s = ComputeClosestSignedDistance();
            currentA = currentB = s;
        }

        // detect crossing: either capsule end-sphere surface moved from front (>0) to back (<=0)
        if ((lastSignedA > 0f && currentA <= 0f) || (lastSignedB > 0f && currentB <= 0f))
        {
            Teleport();
        }

        lastSignedA = currentA;
        lastSignedB = currentB;
    }

    private void UpdateLastSignedDistances()
    {
        if (_capsule != null)
        {
            ComputeCapsuleSignedDistances(out lastSignedA, out lastSignedB);
        }
        else
        {
            float s = ComputeClosestSignedDistance();
            lastSignedA = lastSignedB = s;
        }
    }

    // run physics-aligned checks here to avoid missing fast/physics movement
    private void FixedUpdate()
    {
        // try to find portal trigger if we never received OnTriggerEnter (hierarchy issues)
        if (!isOverlapping)
            TryFindNearbyPortalTrigger();

        if (!isOverlapping || currentPortal == null || currentPortal.linkedPortal == null) return;
        if (Time.time - lastTeleportTime < teleportCooldown) return;

        if (_col == null) _col = GetComponentInParent<Collider>();
        if (_capsule == null) _capsule = GetComponentInParent<CapsuleCollider>();
        if (_charController == null) _charController = GetComponentInParent<CharacterController>();

        float currentA, currentB;
        if (_capsule != null || _charController != null)
        {
            ComputeCapsuleSignedDistances(out currentA, out currentB);

            // DEBUG: log world sample points and signed distances
            Debug.Log($"PortalTraveller: samples A_signed={currentA:F3} B_signed={currentB:F3}");
        }
        else
        {
            float s = ComputeClosestSignedDistance();
            currentA = currentB = s;
            Debug.Log($"PortalTraveller: closestSigned={s:F3}");
        }

        // crossing detection (front > 0 => back <= 0)
        if ((lastSignedA > 0f && currentA <= 0f) || (lastSignedB > 0f && currentB <= 0f))
        {
            Debug.Log("PortalTraveller: detected crossing -> Teleport()");
            Teleport();
        }

        lastSignedA = currentA;
        lastSignedB = currentB;
    }

    // helper: return a point on the portal plane (prefer the trigger collider's bounds centre)
    private Vector3 GetPortalPlanePoint(out Vector3 portalNormal)
    {
        portalNormal = currentPortal != null ? currentPortal.transform.forward : Vector3.forward;

        if (currentPortal == null) return transform.position;

        // prefer a child trigger collider (the portal face) if available
        Collider portalTrigger = currentPortal.GetComponentInChildren<Collider>();
        if (portalTrigger != null && portalTrigger.isTrigger)
        {
            portalNormal = portalTrigger.transform.forward;
            return portalTrigger.bounds.center;
        }

        // fallback to portal transform position
        portalNormal = currentPortal.transform.forward;
        return currentPortal.transform.position;
    }

    // Works for either a real CapsuleCollider/CharacterController (capsule-like)
    private void ComputeCapsuleSignedDistances(out float signedA, out float signedB)
    {
        signedA = signedB = float.PositiveInfinity;
        if (currentPortal == null) return;

        // prepare capsule/char data
        Transform sampleRoot;
        Vector3 localCenter;
        float height, radius;
        int direction;

        if (_capsule != null)
        {
            sampleRoot = _capsule.transform;
            localCenter = _capsule.center;
            height = _capsule.height;
            radius = _capsule.radius;
            direction = _capsule.direction;
        }
        else if (_charController != null)
        {
            sampleRoot = _charController.transform;
            localCenter = _charController.center;
            height = _charController.height;
            radius = _charController.radius;
            direction = 1;
        }
        else
        {
            return;
        }

        Vector3 axis;
        switch (direction)
        {
            case 0: axis = sampleRoot.right; break;
            case 2: axis = sampleRoot.forward; break;
            default: axis = sampleRoot.up; break;
        }

        float halfSeg = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 worldA = sampleRoot.TransformPoint(localCenter + axis * halfSeg);
        Vector3 worldB = sampleRoot.TransformPoint(localCenter - axis * halfSeg);

        // find the portal trigger collider (prefer a trigger child)
        Collider portalTrigger = null;
        foreach (var c in currentPortal.GetComponentsInChildren<Collider>(true))
        {
            if (c.isTrigger)
            {
                portalTrigger = c;
                break;
            }
        }

        Vector3 planePointA, planePointB;
        Vector3 portalNormal;

        if (portalTrigger != null)
        {
            // use per-sample closest points on the actual trigger collider
            planePointA = portalTrigger.ClosestPoint(worldA);
            planePointB = portalTrigger.ClosestPoint(worldB);
            portalNormal = portalTrigger.transform.forward.normalized;
        }
        else
        {
            // fallback to portal transform
            planePointA = planePointB = currentPortal.transform.position;
            portalNormal = currentPortal.transform.forward.normalized;
        }

        signedA = Vector3.Dot(portalNormal, worldA - planePointA) - radius;
        signedB = Vector3.Dot(portalNormal, worldB - planePointB) - radius;

        Debug.DrawLine(worldA, worldA + Vector3.up * 0.1f, Color.yellow, 0.2f);
        Debug.DrawLine(worldB, worldB + Vector3.up * 0.1f, Color.cyan, 0.2f);
        Debug.DrawLine(planePointA, planePointA + portalNormal * 0.25f, Color.red, 0.2f);
        Debug.DrawLine(planePointB, planePointB + portalNormal * 0.25f, Color.magenta, 0.2f);
        Debug.Log($"PortalTraveller: worldA={worldA} worldB={worldB} signedA={signedA:F3} signedB={signedB:F3} portalNormal={portalNormal}");
    }

    // fallback: signed distance of closest point on portal trigger to collider center
    private float ComputeClosestSignedDistance()
    {
        if (currentPortal == null) return float.PositiveInfinity;
        if (_col == null) _col = GetComponentInParent<Collider>();
        if (_col == null) return float.PositiveInfinity;

        // find portal trigger collider
        Collider portalTrigger = null;
        foreach (var c in currentPortal.GetComponentsInChildren<Collider>(true))
            if (c.isTrigger) { portalTrigger = c; break; }

        Vector3 probe = _col.bounds.center;
        Vector3 planePoint;
        Vector3 portalNormal;
        if (portalTrigger != null)
        {
            planePoint = portalTrigger.ClosestPoint(probe);
            portalNormal = portalTrigger.transform.forward.normalized;
        }
        else
        {
            planePoint = currentPortal.transform.position;
            portalNormal = currentPortal.transform.forward.normalized;
        }

        Vector3 closest = _col.ClosestPoint(planePoint);
        return Vector3.Dot(portalNormal, closest - planePoint);
    }

    private void Teleport()
    {
        Debug.Log("PortalTraveller: Teleport()");
        Transform entry = currentPortal.transform;
        Transform exit = currentPortal.linkedPortal.transform;

        // --- POSITION ---
        Vector3 relativePos = entry.InverseTransformPoint(transform.position);
        Vector3 newWorldPos = exit.TransformPoint(relativePos);
        transform.position = newWorldPos - exit.forward * 0.4f; // push slightly forward

        // --- ROTATION (reflect camera across portal plane) ---
        // get camera transform (child or fallback to main)
        Transform camT = GetComponentInChildren<Camera>()?.transform ?? Camera.main?.transform;
        if (camT == null)
        {
            Debug.LogWarning("PortalTraveller: camera not found, using simple rotation fallback.");
            Quaternion rotDelta = exit.rotation * Quaternion.Euler(0f, 180f, 0f) * Quaternion.Inverse(entry.rotation);
            Quaternion finalWorldRot = rotDelta * transform.rotation;
            transform.rotation = Quaternion.Euler(0f, finalWorldRot.eulerAngles.y, 0f);
            var ctrlf = GetComponentInParent<StarterAssets.FirstPersonController>();
            if (ctrlf != null) ctrlf.SetAbsoluteView(finalWorldRot);
        }
        else
        {
            // world camera forward/up
            Vector3 camF = camT.forward;
            Vector3 camU = camT.up;

            // bring into entry portal local space
            Quaternion invEntry = Quaternion.Inverse(entry.rotation);
            Vector3 localF = invEntry * camF;
            Vector3 localU = invEntry * camU;

            // reflect across portal plane (plane normal is +Z in portal local space)
            localF.z = -localF.z;
            localU.z = -localU.z;

            // transform reflected vectors into exit world space
            Vector3 newF = exit.rotation * localF;
            Vector3 newU = exit.rotation * localU;

            Quaternion finalWorldRot = Quaternion.LookRotation(newF, newU);

            // apply yaw-only to the body root so feet face the camera forward
            Transform bodyRoot = GetComponentInParent<Rigidbody>()?.transform ?? transform;
            bodyRoot.rotation = Quaternion.Euler(0f, finalWorldRot.eulerAngles.y, 0f);

            // sync camera/controller with full rotated view
            var controller = GetComponentInParent<StarterAssets.FirstPersonController>();
            if (controller != null)
                controller.SetAbsoluteView(finalWorldRot);
        }

        lastTeleportTime = Time.time;
        isOverlapping = false;

        // re-init to avoid immediate re-teleport
        _col = GetComponent<Collider>();
        _capsule = GetComponent<CapsuleCollider>();
        UpdateLastSignedDistances();
    }

    public void ChildTriggerEnter(Collider other)
    {
        Portal portal = other.GetComponentInParent<Portal>();
        if (portal == null) return;

        currentPortal = portal;
        isOverlapping = true;

        _col = _col ?? GetComponentInParent<Collider>();
        _capsule = _capsule ?? GetComponentInParent<CapsuleCollider>();
        _charController = _charController ?? GetComponentInParent<CharacterController>();

        UpdateLastSignedDistances();
    }

    public void ChildTriggerExit(Collider other)
    {
        Portal portal = other.GetComponentInParent<Portal>();
        if (portal != null && portal == currentPortal)
        {
            currentPortal = null;
            isOverlapping = false;
            lastSignedA = lastSignedB = float.PositiveInfinity;
        }
    }

    // small probe to detect overlapping portal triggers when OnTriggerEnter wasn't fired
    private void TryFindNearbyPortalTrigger()
    {
        // ensure we have a collider reference on parent/child
        _col = _col ?? GetComponentInParent<Collider>();
        _capsule = _capsule ?? GetComponentInParent<CapsuleCollider>();
        _charController = _charController ?? GetComponentInParent<CharacterController>();

        Vector3 probeCenter;
        float probeRadius = 0.6f; // default, tune if needed

        if (_col != null)
        {
            // use collider bounds center and a radius based on extents
            probeCenter = _col.bounds.center;
            probeRadius = Mathf.Max(0.3f, Mathf.Max(_col.bounds.extents.x, _col.bounds.extents.z));
            probeRadius = Mathf.Max(probeRadius, _col.bounds.extents.y * 0.5f);
        }
        else
        {
            probeCenter = transform.position;
        }

        // widen the probe slightly so it reliably overlaps trigger volumes
        probeRadius += 0.1f;

        Collider[] hits = Physics.OverlapSphere(probeCenter, probeRadius);
        foreach (var hit in hits)
        {
            if (!hit.isTrigger) continue;

            var p = hit.GetComponentInParent<Portal>();
            if (p == null) continue;

            // accept only if portal has a linked portal (prevents accidental picks)
            if (p.linkedPortal == null) continue;

            currentPortal = p;
            isOverlapping = true;

            // initialize cached references and distance samples
            _col = _col ?? GetComponentInParent<Collider>();
            _capsule = _capsule ?? GetComponentInParent<CapsuleCollider>();
            _charController = _charController ?? GetComponentInParent<CharacterController>();

            UpdateLastSignedDistances();
            Debug.Log($"PortalTraveller: TryFindNearbyPortalTrigger found portal {p.name}");
            return;
        }
    }
}
