using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRelay : MonoBehaviour
{
    public PortalTraveller traveller;

    private void Awake()
    {
        if (traveller == null)
            traveller = GetComponentInParent<PortalTraveller>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (traveller != null) traveller.ChildTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (traveller != null) traveller.ChildTriggerExit(other);
    }
}
