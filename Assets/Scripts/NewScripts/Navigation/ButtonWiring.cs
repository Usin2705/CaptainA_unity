using UnityEngine.Events;
using UnityEngine.UI;

namespace SaySuomi.Navigation
{
    public static class ButtonWiring
    {
        /// <summary>
        /// Makes a handler the only thing a button does. Unlike ButtonUtils.Rewire this also
        /// switches off Inspector OnClick entries, so a migrated button cannot fire twice and
        /// its entries do not have to be cleared by hand.
        /// </summary>
        public static void Replace(Button button, UnityAction handler)
        {
            SwitchOffInspectorEntries(button.onClick);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(handler);
        }

        /// <summary>Stops an event's Inspector entries firing while the app runs.</summary>
        public static void SwitchOffInspectorEntries(UnityEventBase unityEvent)
        {
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                unityEvent.SetPersistentListenerState(i, UnityEventCallState.Off);
            }
        }
    }
}
