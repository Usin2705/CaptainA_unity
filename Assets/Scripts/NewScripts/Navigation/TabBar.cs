using UnityEngine;

namespace SaySuomi.Navigation
{
    /// <summary>
    /// The tab buttons, their icons and the new-feature dot. Navigator owns which tab is
    /// current, and this follows TabChanged, so back selecting a tab updates the icons too.
    ///
    /// To add a tab, add a child button with a TabButton component.
    /// </summary>
    public class TabBar : MonoBehaviour
    {
        private TabButton[] tabs;
        private NewFeatureDot newFeatureDot;

        // Start rather than Awake: Navigator is created after every Awake in the scene.
        private void Start()
        {
            Navigator navigator = Navigator.GetManager();

            tabs = GetComponentsInChildren<TabButton>(true);
            foreach (TabButton tab in tabs)
            {
                tab.Wire(navigator);
            }

            newFeatureDot = GetComponentInChildren<NewFeatureDot>(true);

            navigator.TabChanged += OnTabChanged;
            OnTabChanged(navigator.CurrentTab);
        }

        private void OnDestroy()
        {
            Navigator navigator = Navigator.GetManager();
            if (navigator != null)
            {
                navigator.TabChanged -= OnTabChanged;
            }
        }

        private void OnTabChanged(AppScreen current)
        {
            foreach (TabButton tab in tabs)
            {
                tab.ShowSelected(tab.Root == current);
            }

            // After the screen is up: it records itself as seen in its own OnEnable,which has
            // already run, so the dot goes in the same frame it was earned.
            newFeatureDot?.Refresh();
        }
    }
}
