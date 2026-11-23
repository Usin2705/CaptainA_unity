using UnityEngine;
using UnityEngine.UI;

namespace SaySuomi.Navigation
{
    /// <summary>One tab: the screen it opens and its icons. TabBar finds these by class.</summary>
    [RequireComponent(typeof(Button), typeof(Image))]
    public sealed class TabButton : MonoBehaviour
    {
        [Tooltip("The screen this tab opens.")]
        [SerializeField]
        private AppScreen root;

        [Tooltip("Shown while this tab is selected.")]
        [SerializeField]
        private Sprite activeIcon;

        [Tooltip("Shown while another tab is selected.")]
        [SerializeField]
        private Sprite inactiveIcon;

        public AppScreen Root => root;

        internal void Wire(Navigator navigator)
        {
            MissingReferences.Report(this);
            ButtonWiring.Replace(GetComponent<Button>(), () => navigator.SelectTab(root));
        }

        internal void ShowSelected(bool isSelected)
        {
            GetComponent<Image>().sprite = isSelected ? activeIcon : inactiveIcon;
        }
    }
}
