using SaySuomi.Navigation;
using UnityEngine;

namespace SaySuomi.Modals
{
    /// <summary>
    /// A popup waiting on the server, with a spinning icon. The spin lives here because the
    /// screen that opened it may be hidden while it waits.
    /// </summary>
    public class LoadingModal : ModalScreen
    {
        private const float DegreesPerFrame = -6f;

        [Tooltip("Spins while the modal waits on the server.")]
        [SerializeField]
        private Transform icon;

        private void Update()
        {
            if (icon.gameObject.activeSelf)
            {
                icon.Rotate(0, 0, DegreesPerFrame, Space.Self);
            }
        }
    }
}
