using UnityEngine;

namespace SaySuomi.Navigation
{
    /// <summary>
    /// Presents modals: the one shared dim, and the open and close animations. Navigator owns
    /// the stack and decides which modal is on top. Added to Navigator's object from code.
    /// </summary>
    internal sealed class ModalHost : MonoBehaviour
    {
        private ModalDim dim;
        private TransitionRunner transition;

        public bool IsAnimating => transition.IsRunning;

        private void Awake()
        {
            transition = new TransitionRunner(this);

            dim = FindAnyObjectByType<ModalDim>(FindObjectsInactive.Include);
            if (dim == null)
            {
                Debug.LogError("ModalHost: no ModalDim in the scene, so modals have no dim.");
            }
        }

        public void Snap()
        {
            transition.Snap();
        }

        public void Show(ModalScreen modal)
        {
            bool dimAlreadyShowing = dim != null && dim.IsShowing;

            // The newest modal is always on top.
            modal.transform.SetAsLastSibling();
            PlaceDimBelow(modal);

            System.Action<float> animate = ScreenTransitions.Modal(modal, opening: true);
            modal.Group.blocksRaycasts = false;

            transition.Play(
                ScreenTransitions.ModalSeconds,
                t =>
                {
                    animate(t);
                    if (!dimAlreadyShowing)
                    {
                        SetDimAmount(t);
                    }
                },
                () =>
                {
                    modal.ResetVisuals();
                    SetDimAmount(1f);
                }
            );
        }

        /// <param name="next">The modal now on top, which keeps the dim, or null for none.</param>
        public void Hide(ModalScreen modal, ModalScreen next)
        {
            if (next != null)
            {
                PlaceDimBelow(next);
            }

            System.Action<float> animate = ScreenTransitions.Modal(modal, opening: false);
            modal.Group.blocksRaycasts = false;

            transition.Play(
                ScreenTransitions.ModalSeconds,
                t =>
                {
                    animate(t);
                    if (next == null)
                    {
                        SetDimAmount(1f - t);
                    }
                },
                () =>
                {
                    modal.ResetVisuals();
                    modal.gameObject.SetActive(false);
                    if (next == null)
                    {
                        HideDim();
                    }
                }
            );
        }

        public void HideDim()
        {
            if (dim != null)
            {
                dim.Hide();
            }
        }

        private void PlaceDimBelow(ModalScreen modal)
        {
            if (dim != null)
            {
                dim.ShowBelow(modal.transform);
            }
        }

        private void SetDimAmount(float amount)
        {
            if (dim != null)
            {
                dim.SetAmount(amount);
            }
        }
    }
}
