using SaySuomi.Modals;
using SaySuomi.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace SaySuomi.Screens
{
    public class InfoScreen : AppScreen
    {
        [SerializeField]
        private Button backButton;

        [Tooltip("Opens the secret code modal.")]
        [SerializeField]
        private Button secretButton;

        public override ScreenLayer Layer => ScreenLayer.Overlay;

        protected override void Bind()
        {
            ButtonWiring.Replace(backButton, () => Navigator.GetManager().Back());
            ButtonWiring.Replace(secretButton, () => Navigator.GetManager().Open<SecretModal>());
        }
    }
}
