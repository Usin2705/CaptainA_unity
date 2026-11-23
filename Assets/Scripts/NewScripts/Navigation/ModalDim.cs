using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaySuomi.Navigation
{
    /// <summary>
    /// The one dim shared by all modals. Its Image sets how it looks when fully shown, and needs
    /// Raycast Target on, so that it blocks taps on everything below, including the tab bar, and
    /// a tap on it goes back.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ModalDim : MonoBehaviour, IPointerClickHandler
    {
        private Image image;
        private Color fullColor;

        internal bool IsShowing => gameObject.activeSelf;

        public void OnPointerClick(PointerEventData eventData)
        {
            Navigator.GetManager().BackFromDim();
        }

        /// <summary>
        /// Shows the dim directly below the modal, in the modal's own parent, because sibling
        /// order is what sets draw order.
        /// </summary>
        internal void ShowBelow(Transform modal)
        {
            transform.SetParent(modal.parent, false);
            transform.SetAsLastSibling();
            transform.SetSiblingIndex(modal.GetSiblingIndex());
            gameObject.SetActive(true);
        }

        /// <param name="amount">0 for invisible, up to 1 for the Image's own colour.</param>
        internal void SetAmount(float amount)
        {
            // Read here because Awake does not run on an object that starts inactive.
            if (image == null)
            {
                image = GetComponent<Image>();
                fullColor = image.color;
            }

            image.color = new Color(fullColor.r, fullColor.g, fullColor.b, fullColor.a * amount);
        }

        internal void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
