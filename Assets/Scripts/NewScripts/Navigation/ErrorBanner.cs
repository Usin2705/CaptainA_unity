using UnityEngine;

namespace SaySuomi.Navigation
{
    /// <summary>
    /// The drop-down error message. It moves to the top of the root Canvas when shown, so it
    /// draws above the popup it belongs to and the shared dim.
    /// </summary>
    public static class ErrorBanner
    {
        private const string AnimationName = "Error Popup Animation";

        public static void Show(GameObject banner)
        {
            // false, because its animation sets positions relative to its parent's centre.
            banner.transform.SetParent(banner.transform.root, false);
            banner.transform.SetAsLastSibling();
            banner.SetActive(true);

            Animator animator = banner.GetComponent<Animator>();
            animator.enabled = true;
            animator.Play(AnimationName);
        }

        public static void Hide(GameObject banner)
        {
            banner.GetComponent<Animator>().enabled = false;
            banner.SetActive(false);
        }
    }
}
