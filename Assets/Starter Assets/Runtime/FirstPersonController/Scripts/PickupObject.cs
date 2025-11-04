using UnityEngine;

public class PickupObject : MonoBehaviour
{
    public Transform holdPoint;          // Where to hold the object (e.g., empty GameObject in front of camera)
    public float pickupRange = 3f;       // How far you can pick something
    public float moveForce = 250f;       // How fast object follows hand

    private Rigidbody heldObject;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, pickupRange))
                {
                    if (hit.collider.CompareTag("Pickup"))
                    {
                        Pickup(hit.collider.GetComponent<Rigidbody>());
                    }
                }
            }
            else
            {
                Drop();
            }
        }
    }

    void FixedUpdate()
    {
        if (heldObject != null)
        {
            Vector3 targetPos = holdPoint.position;
            Vector3 direction = (targetPos - heldObject.position);
            heldObject.velocity = direction * moveForce * Time.fixedDeltaTime;
        }
    }

    void Pickup(Rigidbody obj)
    {
        heldObject = obj;
        heldObject.useGravity = false;
        heldObject.drag = 10f;
    }

    void Drop()
    {
        heldObject.useGravity = true;
        heldObject.drag = 1f;
        heldObject = null;
    }
}
