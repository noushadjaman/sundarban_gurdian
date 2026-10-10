using UnityEngine;
using UnityEngine.EventSystems;

namespace SundarbanGuardian.Core
{
    /// <summary>
    /// Right-click pointer handler for dropping tools directly from the HUD slot.
    /// </summary>
    public class ToolSlotPointerDrop : MonoBehaviour, IPointerClickHandler
    {
        public System.Action onRightClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                onRightClick?.Invoke();
            }
        }
    }
}
