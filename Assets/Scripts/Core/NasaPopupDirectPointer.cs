using UnityEngine;
using UnityEngine.EventSystems;

namespace SundarbanGuardian.Core
{
    /// <summary>
    /// Direct click handler for opening the NASA Learning Modal.
    /// </summary>
    public class NasaPopupDirectPointer : MonoBehaviour, IPointerClickHandler
    {
        public System.Action onTrigger;

        public void OnPointerClick(PointerEventData eventData)
        {
            onTrigger?.Invoke();
        }
    }
}
