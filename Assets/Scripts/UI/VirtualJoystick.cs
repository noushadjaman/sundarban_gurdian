using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SundarbanGuardian.Player;

namespace SundarbanGuardian.UI
{
    /// <summary>
    /// Free Fire / PUBG Mobile style on-screen virtual joystick.
    /// Clamps handle within base radius and feeds normalized 2D movement
    /// vector to MobileInputManager.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Components")]
        public RectTransform backgroundRing;
        public RectTransform handleKnob;

        [Header("Tuning")]
        public float maxRadius = 55f;
        public float deadZone = 0.05f;

        private Vector2 startPos;
        private Canvas parentCanvas;

        private void Awake()
        {
            if (backgroundRing == null) backgroundRing = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (backgroundRing == null || handleKnob == null) return;

            Camera cam = null;
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = parentCanvas.worldCamera;
            }

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRing, eventData.position, cam, out localPoint))
            {
                float radius = Mathf.Min(maxRadius, backgroundRing.sizeDelta.x * 0.45f);
                Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);

                handleKnob.anchoredPosition = clamped;

                Vector2 normalized = clamped / radius;
                if (normalized.magnitude < deadZone)
                {
                    normalized = Vector2.zero;
                }

                MobileInputManager.JoystickInput = normalized;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handleKnob != null)
            {
                handleKnob.anchoredPosition = Vector2.zero;
            }
            MobileInputManager.JoystickInput = Vector2.zero;
        }

        private void OnDisable()
        {
            if (handleKnob != null) handleKnob.anchoredPosition = Vector2.zero;
            MobileInputManager.JoystickInput = Vector2.zero;
        }
    }
}
