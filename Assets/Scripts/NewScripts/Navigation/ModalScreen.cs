using System;
using TMPro;
using UnityEngine;

namespace SaySuomi.Navigation
{
    /// <summary>
    /// A popup on the navigation stack. ModalHost shows it above the shared dim. Subclass it for
    /// a popup with logic, or use Attach for one with no script of its own.
    /// </summary>
    public class ModalScreen : AppScreen
    {
        public override ScreenLayer Layer => ScreenLayer.Overlay;

        /// <summary>False ignores back, such as for a loading popup waiting on the server.</summary>
        public bool Dismissible { get; set; } = true;

        /// <summary>
        /// Runs on every way of closing through back, including the popup's own back button if
        /// it calls Navigator.Back. Return true to keep the popup from closing.
        /// </summary>
        public Func<bool> OnBack { get; set; }

        /// <summary>
        /// Clears what the popup holds when back closes it. A tap on the dim skips this while text
        /// is typed, so that tapping outside the keyboard does not lose it.
        /// </summary>
        public Action OnDiscard { get; set; }

        internal bool HasTypedText
        {
            get
            {
                foreach (TMP_InputField field in GetComponentsInChildren<TMP_InputField>(true))
                {
                    if (field.text.Length > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Makes a script-less popup openable, without needing any scene changes.</summary>
        public static ModalScreen Attach(GameObject popup, bool dismissible = true)
        {
            return Attach<ModalScreen>(popup, dismissible);
        }

        /// <summary>Adds T to a popup, and sets it up.</summary>
        public static T Attach<T>(GameObject popup, bool dismissible = true)
            where T : ModalScreen
        {
            if (!popup.TryGetComponent(out T modal))
            {
                modal = popup.AddComponent<T>();
            }

            modal.Dismissible = dismissible;
            modal.EnsureBound();
            return modal;
        }

        public override bool HandleBack()
        {
            return OnBack != null && OnBack();
        }
    }
}
