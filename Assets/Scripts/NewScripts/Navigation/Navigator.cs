using System;
using System.Collections.Generic;
using SaySuomi.Managers;
using SaySuomi.Modals;
using SaySuomi.Screens;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SaySuomi.Navigation
{
    /// <summary>
    /// The single owner of navigation: the current tab, the stack above it, and back.
    /// See docs/NEW/NAVIGATION.md.
    /// </summary>
    public sealed class Navigator : MonoBehaviour
    {
        private static Navigator instance;

        private readonly Dictionary<Type, AppScreen> screens = new Dictionary<Type, AppScreen>();
        private readonly List<AppScreen> stack = new List<AppScreen>();

        private TransitionRunner transition;
        private ModalHost modalHost;
        private AppScreen home;

        /// <summary>Raised after the new tab is active, so its OnEnable has already run.</summary>
        public event Action<AppScreen> TabChanged;

        public AppScreen CurrentTab { get; private set; }

        private AppScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : CurrentTab;

        private void Awake()
        {
            AppScreen[] found = FindObjectsByType<AppScreen>(FindObjectsInactive.Include);
            if (found.Length == 0)
            {
                // Disabled as well, because Destroy waits for the end of the frame and Update
                // would otherwise run once on a Navigator that was never set up.
                enabled = false;
                Destroy(gameObject);
                return;
            }

            instance = this;
            transition = new TransitionRunner(this);
            modalHost = gameObject.AddComponent<ModalHost>();

            foreach (AppScreen screen in found)
            {
                screens[screen.GetType()] = screen;
            }

            home = Get<FreestyleScreen>();

            BindAllScreens();
            AddBackButtons();

            foreach (AppScreen screen in found)
            {
                if (screen.IsTabRoot && screen != home)
                {
                    screen.gameObject.SetActive(false);
                }
            }

            NetworkManager.SurveyDue += ShowSurvey;

            CurrentTab = home;
            home.Prepare();

            // Off first, so its OnEnable runs now that popups can open. It may be active in the
            // scene, in which case that already ran before this object existed.
            home.gameObject.SetActive(false);
            home.gameObject.SetActive(true);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Back();
            }
        }

        private void OnDestroy()
        {
            NetworkManager.SurveyDue -= ShowSurvey;

            if (instance == this)
            {
                instance = null;
            }
        }

        public static Navigator GetManager()
        {
            return instance;
        }

        public T Get<T>()
            where T : AppScreen
        {
            if (screens.TryGetValue(typeof(T), out AppScreen screen))
            {
                return (T)screen;
            }

            Debug.LogError($"Navigator: no {typeof(T).Name} in the scene.");
            return null;
        }

        /// <summary>True when the screen is on the stack, or is the current tab.</summary>
        public bool IsOpen(AppScreen screen)
        {
            return screen == CurrentTab || stack.Contains(screen);
        }

        /// <summary>Opens a screen. Returns it, active, so the caller can give it data.</summary>
        public T Push<T>()
            where T : AppScreen
        {
            T screen = Get<T>();
            if (screen == null)
            {
                return null;
            }

            FinishTransition();

            if (stack.Contains(screen))
            {
                Debug.LogWarning($"Navigator: {typeof(T).Name} is already open.");
                return screen;
            }

            // Hiding the tab root would hide this too, leaving a blank screen with no other clue.
            if (
                screen.Layer == ScreenLayer.Screen
                && screen.transform.IsChildOf(CurrentTab.transform)
            )
            {
                Debug.LogError(
                    $"Navigator: {screen.name} is inside the tab root {CurrentTab.name}. "
                        + "Move it under Screens."
                );
            }

            AppScreen below = Top;
            below.Prepare();
            screen.Prepare();

            // On the stack before it activates, because its OnEnable may open a modal above it.
            stack.Add(screen);
            screen.gameObject.SetActive(true);

            below.Group.blocksRaycasts = false;
            screen.Group.blocksRaycasts = false;

            if (screen.Layer == ScreenLayer.Overlay)
            {
                transition.Play(
                    ScreenTransitions.ScreenSeconds,
                    ScreenTransitions.SlideVertical(screen, opening: true),
                    () =>
                    {
                        screen.ResetVisuals();
                        below.ResetVisuals();
                    }
                );
                return screen;
            }

            transition.Play(
                ScreenTransitions.ScreenSeconds,
                ScreenTransitions.Slide(below, screen, forward: true),
                () =>
                {
                    screen.ResetVisuals();
                    HideEverythingBelow(screen);
                }
            );
            return screen;
        }

        public T Open<T>()
            where T : ModalScreen
        {
            T modal = Get<T>();
            if (modal != null)
            {
                Open(modal);
            }

            return modal;
        }

        public void Open(ModalScreen modal)
        {
            FinishTransition();

            if (stack.Contains(modal))
            {
                return;
            }

            modal.Prepare();
            stack.Add(modal);
            modal.gameObject.SetActive(true);
            modalHost.Show(modal);
        }

        /// <summary>The one route for back, whether from a key press or a button.</summary>
        public void Back()
        {
            GoBack(isFromDim: false);
        }

        /// <summary>Returns to a screen on the stack, or to the tab root if T is the current tab.</summary>
        public void PopTo<T>()
            where T : AppScreen
        {
            T screen = Get<T>();
            if (screen == null)
            {
                return;
            }

            if (screen == CurrentTab)
            {
                PopToRoot();
                return;
            }

            int index = stack.IndexOf(screen);
            if (index < 0)
            {
                Debug.LogWarning($"Navigator: {typeof(T).Name} is not open.");
                return;
            }

            PopDownTo(index + 1);
        }

        public void PopToRoot()
        {
            PopDownTo(0);
        }

        /// <summary>Shows a tab root, clearing the stack.</summary>
        public void SelectTab(AppScreen tabRoot)
        {
            FinishTransition();

            // Cleared first, as the new tab may open a popup of its own in OnEnable.
            ClearStack();

            AppScreen previous = CurrentTab;
            if (previous == tabRoot)
            {
                // Hidden if the stack had a Screen layer entry, and left alone if not.
                tabRoot.gameObject.SetActive(true);
                TabChanged?.Invoke(tabRoot);
                return;
            }

            previous.Prepare();
            tabRoot.Prepare();
            CurrentTab = tabRoot;
            tabRoot.gameObject.SetActive(true);
            TabChanged?.Invoke(tabRoot);

            previous.Group.blocksRaycasts = false;
            tabRoot.Group.blocksRaycasts = false;

            transition.Play(
                ScreenTransitions.TabSeconds,
                ScreenTransitions.Crossfade(previous, tabRoot),
                () =>
                {
                    tabRoot.ResetVisuals();
                    Deactivate(previous);
                }
            );
        }

        internal void BackFromDim()
        {
            // The dim stays up while a modal closes, when the entry on top is already the screen
            // below. Going back from there would close that screen, or quit on the home tab.
            if (!modalHost.IsAnimating)
            {
                GoBack(isFromDim: true);
            }
        }

        // After scene load, so tab bars and screens exist. This runs in every scene, and Awake
        // removes it from any that has no AppScreen.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateAtLaunch()
        {
            var host = new GameObject(nameof(Navigator));
            DontDestroyOnLoad(host);
            host.AddComponent<Navigator>();
        }

        private static void Deactivate(AppScreen screen)
        {
            screen.ResetVisuals();
            screen.gameObject.SetActive(false);
        }

        private void GoBack(bool isFromDim)
        {
            FinishTransition();

            AppScreen top = Top;
            ModalScreen modal = top as ModalScreen;

            // First, so a popup waiting on the server does not reset its form on a back press.
            if (modal != null && !modal.Dismissible)
            {
                return;
            }

            if (top.HandleBack())
            {
                return;
            }

            if (modal != null && !(isFromDim && modal.HasTypedText))
            {
                modal.OnDiscard?.Invoke();
            }

            if (stack.Count > 0)
            {
                PopTop();
                return;
            }

            if (CurrentTab != home)
            {
                SelectTab(home);
                return;
            }

            // Does nothing in the Editor, so this line is how to tell it was reached there.
            Debug.Log("Navigator: back on the home tab, quitting.");
            Application.Quit();
        }

        /// <summary>
        /// Sets up every screen now, so all broken paths show up in the Console at launch. One
        /// failing does not stop the others, so they are all reported together, and the app
        /// still opens for whatever else needs testing.
        /// </summary>
        private void BindAllScreens()
        {
            foreach (AppScreen screen in screens.Values)
            {
                try
                {
                    screen.EnsureBound();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Navigator: could not set up {screen.GetType().Name}.", screen);
                    Debug.LogException(exception, screen);
                }
            }
        }

        private void AddBackButtons()
        {
            GameObject template = Get<ASATaskScreen>().BackButtonTemplate;

            foreach (AppScreen screen in screens.Values)
            {
                if (screen.ShowsBackButton)
                {
                    ScreenBackButton.AddTo(screen, template);
                }
            }
        }

        private void ShowSurvey()
        {
            Open<SurveyModal>();
        }

        private void FinishTransition()
        {
            transition.Snap();
            modalHost.Snap();
        }

        /// <summary>Closes everything above count entries, animating only the top one.</summary>
        private void PopDownTo(int count)
        {
            FinishTransition();

            if (stack.Count <= count)
            {
                return;
            }

            for (int i = stack.Count - 2; i >= count; i--)
            {
                Deactivate(stack[i]);
                stack.RemoveAt(i);
            }

            PopTop();
        }

        private void PopTop()
        {
            AppScreen top = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);

            // Before revealing, because the screen revealed may open a modal in OnEnable, and
            // this must still slide the screen rather than that modal.
            AppScreen below = Top;

            // Needed even when the top entry hid nothing, because PopDownTo may have just closed
            // a Screen layer entry from the middle of the stack.
            RevealVisibleEntries();

            if (top is ModalScreen modal)
            {
                modalHost.Hide(modal, Top as ModalScreen);
                return;
            }

            top.Group.blocksRaycasts = false;

            if (top.Layer == ScreenLayer.Overlay)
            {
                transition.Play(
                    ScreenTransitions.ScreenSeconds,
                    ScreenTransitions.SlideVertical(top, opening: false),
                    () => Deactivate(top)
                );
                return;
            }

            below.Group.blocksRaycasts = false;

            transition.Play(
                ScreenTransitions.ScreenSeconds,
                ScreenTransitions.Slide(top, below, forward: false),
                () =>
                {
                    below.ResetVisuals();
                    Deactivate(top);
                }
            );
        }

        /// <summary>
        /// Reactivates what a closing Screen layer entry was hiding: the nearest Screen layer
        /// entry and any overlays above it, bottom-up, so each OnEnable sees the one below.
        /// </summary>
        private void RevealVisibleEntries()
        {
            int first = stack.Count - 1;
            while (first >= 0 && stack[first].Layer != ScreenLayer.Screen)
            {
                first--;
            }

            AppScreen floor = first >= 0 ? stack[first] : CurrentTab;
            floor.Prepare();
            floor.gameObject.SetActive(true);

            for (int i = first + 1; i < stack.Count; i++)
            {
                stack[i].Prepare();
                stack[i].gameObject.SetActive(true);
            }
        }

        private void HideEverythingBelow(AppScreen screen)
        {
            for (int i = stack.IndexOf(screen) - 1; i >= 0; i--)
            {
                Deactivate(stack[i]);
            }

            Deactivate(CurrentTab);
        }

        private void ClearStack()
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                Deactivate(stack[i]);
            }

            stack.Clear();
            modalHost.HideDim();
        }
    }
}
