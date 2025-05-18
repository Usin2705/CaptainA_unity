using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    #region Events
    public delegate void StartTouch(Vector2 position, float time);
    public event StartTouch OnStartTouch;
    public delegate void EndTouch(Vector2 position, float time);
    public event EndTouch OnEndTouch;
    #endregion

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        inputActions.UI.PrimaryContact.started += ctx => StartTouchPrimary(ctx);
        inputActions.UI.PrimaryContact.canceled += ctx => EndTouchPrimary(ctx);
    }

    private void StartTouchPrimary(InputAction.CallbackContext context)
    {
        // Handle touch start
        if (OnStartTouch != null)
        {
            Vector2 touchPosition = context.ReadValue<Vector2>();
            float touchTime = (float)context.startTime;
            OnStartTouch(touchPosition, touchTime);
        }
    }

    private void EndTouchPrimary(InputAction.CallbackContext context)
    {
        // Handle touch start
        if (OnEndTouch != null)
        {
            Vector2 touchPosition = context.ReadValue<Vector2>();
            float touchTime = (float)context.startTime;
            OnEndTouch(touchPosition, touchTime);
        }
    }


}