using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PressurePlate : MonoBehaviour
{
    public float massThreshold = 7f;          // Mass to fully press the plate
    public Transform plateTop;                 // The part that visually moves
    public Vector3 unpressedPosition;         // Local pos when unpressed
    public Vector3 halfwayPressedPosition;    // Local pos when halfway pressed
    public Vector3 fullyPressedPosition;      // Local pos when fully pressed

    public UnityEvent onFullyPressed;         // Event triggered on full press

    private HashSet<Rigidbody> objectsOnPlate = new HashSet<Rigidbody>();

    private void Start()
    {
        if (plateTop != null)
            unpressedPosition = plateTop.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && !objectsOnPlate.Contains(rb))
        {
            objectsOnPlate.Add(rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && objectsOnPlate.Contains(rb))
        {
            objectsOnPlate.Remove(rb);
        }
    }

    private void Update()
    {
        float totalMass = 0f;
        foreach (var rb in objectsOnPlate)
        {
            totalMass += rb.mass;
        }

        if (plateTop != null)
        {
            if (totalMass >= massThreshold)
            {
                plateTop.localPosition = Vector3.Lerp(plateTop.localPosition, fullyPressedPosition, Time.deltaTime * 5f);
                onFullyPressed.Invoke();
            }
            else if (totalMass > 0)
            {
                plateTop.localPosition = Vector3.Lerp(plateTop.localPosition, halfwayPressedPosition, Time.deltaTime * 5f);
            }
            else
            {
                plateTop.localPosition = Vector3.Lerp(plateTop.localPosition, unpressedPosition, Time.deltaTime * 5f);
            }
        }
    }
}
