using UnityEngine;

public class InteractWithButton : MonoBehaviour
{
    public float interactDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactDistance))
            {
                PressableButton button = hit.collider.GetComponentInParent<PressableButton>();
                if (button != null)
                {
                    button.Press();
                }
            }
        }
    }
}
