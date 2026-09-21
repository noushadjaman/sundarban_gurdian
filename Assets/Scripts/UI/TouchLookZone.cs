using UnityEngine;
using UnityEngine.EventSystems;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.UI
{
    /// <summary>
    /// Free Fire / PUBG Mobile touch-look scroll surface.
    /// Captures swipe and drag events across the screen to orbit the 3rd-person camera.
    /// In Unity Editor / Desktop, also supports right-click or middle-click drag to look around.
    /// </summary>
    public class TouchLookZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Sensitivity")]
        public float touchSensitivity = 0.18f;
        public float mouseSensitivity = 1.2f;

        private int activePointerId = -999;
        private Vector2 previousTouchPos;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId == -999)
            {
                activePointerId = eventData.pointerId;
                previousTouchPos = eventData.position;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                Vector2 delta = eventData.delta * touchSensitivity;
                MobileInputManager.LookDelta += delta;
                previousTouchPos = eventData.position;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                activePointerId = -999;
            }
        }

        private void Update()
        {
            // Desktop fallback: allow right-mouse button drag to orbit camera smoothly
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.isPressed)
                {
                    var delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue() * mouseSensitivity * 1.5f;
                    MobileInputManager.LookDelta += delta;
                    return;
                }
            }
            catch (System.Exception) { }
#endif
#if !ENABLE_INPUT_SYSTEM
            try
            {
                if (Input.GetMouseButton(1))
                {
                    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 10f;
                    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 10f;
                    MobileInputManager.LookDelta += new Vector2(mouseX, mouseY);
                }
            }
            catch (System.Exception) { }
#endif
        }
    }
}
