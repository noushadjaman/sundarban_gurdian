using UnityEngine;
using UnityEngine.EventSystems;

namespace SundarbanGuardian.Core
{
    /// <summary>
    /// Direct instant-response pointer handler for action buttons (Jump, Fire, Auto-Run).
    /// Fires immediately on PointerDown (touch/mouse press) with zero drag lag or dropped clicks.
    /// </summary>
    public class ActionPointerHandler : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        public System.Action onAction;
        private float lastTriggerTime = -1f;

        public void OnPointerDown(PointerEventData eventData)
        {
            Trigger();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Trigger();
        }

        private void Trigger()
        {
            if (Time.time - lastTriggerTime < 0.15f) return;
            lastTriggerTime = Time.time;
            onAction?.Invoke();
        }
    }
}
