using UnityEngine;

namespace SaySuomi.Navigation
{
    /// <summary>The new-feature dot on the Advanced tab. See NewFeatureBadge for the rule.</summary>
    public sealed class NewFeatureDot : MonoBehaviour
    {
        public void Refresh()
        {
            gameObject.SetActive(
                NewFeatureBadge.ShouldShow(Const.PREF_SEEN_ADVANCED, Const.VER_MAX_SHOW_ADVANCED)
            );
        }
    }
}
