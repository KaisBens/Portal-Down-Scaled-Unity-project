using UnityEngine;
using UnityEngine.Events;

public class PressableButton : MonoBehaviour
{
    public Transform buttonTop;           // Assign ButtonSphere here in Inspector
    public Vector3 pressedPosition;       // Local position when pressed
    public Vector3 unpressedPosition;     // Local position when unpressed
    public float pressDuration = 0.2f;    // How long it takes to press/release
    public UnityEvent onPressed;          // Events to invoke when pressed

    private bool isPressed = false;

    private void Start()
    {
        if (buttonTop != null)
            unpressedPosition = buttonTop.localPosition;
    }

    public void Press()
    {
        if (!isPressed)
        {
            isPressed = true;
            if (buttonTop != null)
                StopAllCoroutines(); // stop any current animation
            StartCoroutine(PressAnimation());
            onPressed.Invoke();
        }
    }

    private System.Collections.IEnumerator PressAnimation()
    {
        // Animate press down
        float elapsed = 0f;
        Vector3 startPos = buttonTop.localPosition;
        while (elapsed < pressDuration)
        {
            buttonTop.localPosition = Vector3.Lerp(startPos, pressedPosition, elapsed / pressDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        buttonTop.localPosition = pressedPosition;

        // Wait a bit (optional)
        yield return new WaitForSeconds(0.5f);

        // Animate release
        elapsed = 0f;
        while (elapsed < pressDuration)
        {
            buttonTop.localPosition = Vector3.Lerp(pressedPosition, unpressedPosition, elapsed / pressDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        buttonTop.localPosition = unpressedPosition;
        isPressed = false;
    }
}
