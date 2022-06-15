using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

public class GamepadCursor : MonoBehaviour
{

    #region Statics

    // All objects that exist
    private static List<GamepadCursor> gamepadCursors = new List<GamepadCursor>();

    // Stored speed
    private static float storedSpeed = 100f;

    public static void ChangeSpeed(float speed)
    {
        if (speed < 100f) speed = 100f;
        if (speed > 2500f) speed = 2500;

        storedSpeed = speed;

        foreach (GamepadCursor cursor in gamepadCursors)
        {
            if (cursor == null) continue;

            cursor.cursorSpeed = speed;
        }
    }

    #endregion



    public enum GamepadStick
    {
        Left,
        Right,
        Both,
    }

    public enum GamepadButton
    {
        North,
        South,
        East,
        West,
        None,
    }

    public enum GamepadTrigger
    {
        LB,
        LT,
        RB,
        RT,
        LTRT,
        LBRB,
        None,
    }

    [Header("Needed")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private RectTransform cursorTransform;
    [SerializeField] private RectTransform canvasTransform;
    [SerializeField] private Canvas canvas;
    [Header("Settings")]
    [SerializeField] private float cursorSpeed = 1000f; // Modificable on settings
    [SerializeField] private AnimationCurve smoothFunction;
    [SerializeField] private float padding = 35f;
    [SerializeField] private bool hideRealMouse = true;
    [SerializeField] private Vector2 initialMouseAnchors = new Vector2(.5f, .5f);  // Values from 0 to 1
    [SerializeField] private float inactiveHideTime = 4f; // Less than 0, dont hide
    [Header("Gamepad")]
    [SerializeField] private GamepadStick useStick = GamepadStick.Left;
    [SerializeField] private GamepadButton useButton = GamepadButton.South;
    [SerializeField] private GamepadTrigger useTrigger = GamepadTrigger.RT;

    public string CurrentControlScheme => playerInput.currentControlScheme;
    public float CursorSpeed => cursorSpeed;

    private Mouse virtualMouse;
    private Mouse currentMouse;
    private bool prevMouseState;
    private bool updateRealMouseCursor = true;
    private Camera mainCamera;
    private string previousControlScheme = "";
    private float inactiveTime;
    private bool isVirtualCursorVisible = false;
    private bool isGamePadPressed = false;
    private Vector3 lastCursorSpritePosition;
    private Image cursorImage;

    // Schemas
    private const string gamepadScheme = "Gamepad";
    private const string mouseScheme = "Keyboard&Mouse";

    // Layouts
    private const string virtualMouseLayout = "VirtualMouse";
    private const string mouseLayout = "Mouse";


    private void OnEnable()
    {

        // Add to the list
        gamepadCursors.Add(this);
        cursorSpeed = storedSpeed;

        // Get the main Camera if exist
        mainCamera = Camera.main;

        // Get the virtual cursor image
        cursorImage = cursorTransform.GetComponent<Image>();
        lastCursorSpritePosition = cursorTransform.position;

        // Get the real mouse and hide it
        currentMouse = (Mouse)InputSystem.devices.Where(d => d.layout == mouseLayout).FirstOrDefault();
        if (hideRealMouse) Cursor.visible = false;

        // Show virtualCursor
        if (playerInput.currentControlScheme == gamepadScheme)
        {
            cursorImage.enabled = true;
            isVirtualCursorVisible = true;
        }
        else if (playerInput.currentControlScheme == mouseScheme && hideRealMouse)
        {
            cursorImage.enabled = true;
            isVirtualCursorVisible = true;
        }
        else
        {
            cursorImage.enabled = false;
            isVirtualCursorVisible = false;
        }

        // Create and add the virtualMouse
        if (virtualMouse == null)
        {
            // If exist a virtualMouse use it
            virtualMouse = (Mouse)InputSystem.devices.Where(d => d.layout == virtualMouseLayout).FirstOrDefault();

            // Create a new virtualMouse if dont exist
            if (virtualMouse == null)
            {
                virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
            }
        }

        // Add the device if no added
        if (!virtualMouse.added)
        {
            InputSystem.AddDevice(virtualMouse);
        }

        // Pair the virtual mouse with the player input component
        InputUser.PerformPairingWithDevice(virtualMouse, playerInput.user);

        // Set cursors to anchored position
        var initialPoss = new Vector2(Mathf.Lerp(0, Screen.width, initialMouseAnchors.x), Mathf.Lerp(0, Screen.height, initialMouseAnchors.y));
        // Set real cursor to anchored position
        if (currentMouse != null) currentMouse.WarpCursorPosition(initialPoss);
        // Set virtual cursor to anchored position
        InputState.Change(virtualMouse.position, initialPoss);
        // Update cursor sprite position
        AnchoredCursorUpdate(initialPoss);

        // Check if real mouse should update or not
        if (playerInput.currentControlScheme == mouseScheme) updateRealMouseCursor = true;
        else updateRealMouseCursor = false;

        // Subscribe to an update function of the mouse
        InputSystem.onAfterUpdate += UpdateMotion;
        playerInput.onControlsChanged += OnControlsChange;

        // Inactive time
        inactiveTime = inactiveHideTime;

    }


    private void OnDisable()
    {

        // Remove ftom the list
        try
        {
            gamepadCursors.Remove(this);
        }
        catch (System.Exception) { }

        // Remove virtual mouse and unsubscribe to events
        if (virtualMouse != null && virtualMouse.added) InputSystem.RemoveDevice(virtualMouse);
        InputSystem.onAfterUpdate -= UpdateMotion;
        playerInput.onControlsChanged -= OnControlsChange;

        // Hide virtual cursor sprite
        if (cursorImage != null) cursorImage.enabled = false;
        isVirtualCursorVisible = false;

        // Show real cursor
        Cursor.visible = true;
    }


    private void OnControlsChange(PlayerInput obj)
    {

        // Mouse Scheme
        if (playerInput.currentControlScheme == mouseScheme && previousControlScheme != mouseScheme)
        {
            // If no valid mouse
            if (currentMouse == null) return;

            // Hide virtual cursor
            if (!hideRealMouse)
            {
                cursorImage.enabled = false;
                isVirtualCursorVisible = false;
            }

            // Show real cursor
            if (!hideRealMouse) Cursor.visible = true;

            // Set real cursor to virtualcursor last position
            currentMouse.WarpCursorPosition(virtualMouse.position.ReadValue());

            // Update the real mouse
            updateRealMouseCursor = true;

            // Save control shceme
            previousControlScheme = mouseScheme;
        }
        // Gamepad Scheme
        else if (playerInput.currentControlScheme == gamepadScheme && previousControlScheme != gamepadScheme)
        {
            // Show virtual cursor
            cursorImage.enabled = true;
            isVirtualCursorVisible = true;

            // Hide real cursor
            Cursor.visible = false;

            // Set virtual cursor to real cursor last position
            if (currentMouse != null) InputState.Change(virtualMouse.position, currentMouse.position.ReadValue());

            // Set cursor sprite to cursor position
            if (currentMouse != null) AnchoredCursorUpdate(currentMouse.position.ReadValue());

            // Update the real mouse
            updateRealMouseCursor = false;

            // Save control scheme
            previousControlScheme = gamepadScheme;
        }
        // Other Scheme
        else if (playerInput.currentControlScheme != gamepadScheme &&
            playerInput.currentControlScheme != mouseScheme &&
            (previousControlScheme == mouseScheme || previousControlScheme == gamepadScheme))
        {
            // Hide virtual cursor
            cursorImage.enabled = false;
            isVirtualCursorVisible = false;

            // Update the real mouse
            updateRealMouseCursor = false;

            // Save control shceme
            previousControlScheme = playerInput.currentControlScheme;
        }

    }


    private void UpdateMotion()
    {

        // Read the value of the gamepad, pipe into the virtualMouse and change the position of the cursor image on the screen

        // If the virtual mouse dont exist or the gamepad isnot connected, return.
        if (virtualMouse == null || Gamepad.current == null) return;

        // Read the stickValue
        Vector2 deltaValue = new Vector2();
        if (useStick == GamepadStick.Left)
            deltaValue = Gamepad.current.leftStick.ReadValue();
        else if (useStick == GamepadStick.Right)
            deltaValue = Gamepad.current.rightStick.ReadValue();
        else if (useStick == GamepadStick.Both)
        {
            var leftValue = Gamepad.current.leftStick.ReadValue();
            if (leftValue.x != 0 || leftValue.y != 0) deltaValue = leftValue;
            else deltaValue = Gamepad.current.rightStick.ReadValue();
        }
        deltaValue = new Vector2(Smooth(deltaValue.x), Smooth(deltaValue.y));
        deltaValue *= cursorSpeed * Time.unscaledDeltaTime;

        // Read mouse position and add delta value
        Vector2 currentPosition = virtualMouse.position.ReadValue();
        Vector2 newPosition = currentPosition + deltaValue;

        // Dont go out of the screen
        newPosition.x = Mathf.Clamp(newPosition.x, padding, Screen.width - padding);
        newPosition.y = Mathf.Clamp(newPosition.y, padding, Screen.height - padding);

        // Change the virtual mouse position and the delta(movement)
        InputState.Change(virtualMouse.position, newPosition);
        InputState.Change(virtualMouse.delta, deltaValue);

        // Check buttons
        bool buttonIsPressed = false;
        if (useButton == GamepadButton.South)
            buttonIsPressed = Gamepad.current.buttonSouth.isPressed;
        else if (useButton == GamepadButton.North)
            buttonIsPressed = Gamepad.current.buttonNorth.isPressed;
        else if (useButton == GamepadButton.East)
            buttonIsPressed = Gamepad.current.buttonEast.isPressed;
        else if (useButton == GamepadButton.West)
            buttonIsPressed = Gamepad.current.buttonWest.isPressed;

        // Check triggers
        bool triggerIsPressed = false;
        if (useTrigger == GamepadTrigger.RB)
            triggerIsPressed = Gamepad.current.rightShoulder.ReadValue() == 1f;
        else if (useTrigger == GamepadTrigger.LB)
            triggerIsPressed = Gamepad.current.leftShoulder.ReadValue() == 1f;
        else if (useTrigger == GamepadTrigger.RT)
            triggerIsPressed = Gamepad.current.rightTrigger.ReadValue() == 1f;
        else if (useTrigger == GamepadTrigger.LT)
            triggerIsPressed = Gamepad.current.leftTrigger.ReadValue() == 1f;
        else if (useTrigger == GamepadTrigger.LTRT)
            triggerIsPressed = Gamepad.current.rightTrigger.ReadValue() == 1f || Gamepad.current.leftTrigger.ReadValue() == 1f;
        else if (useTrigger == GamepadTrigger.LBRB)
            triggerIsPressed = Gamepad.current.rightShoulder.ReadValue() == 1f || Gamepad.current.leftShoulder.ReadValue() == 1f;
        
        // If is pressed
        isGamePadPressed = (buttonIsPressed || triggerIsPressed);
        
        // Check state
        if (prevMouseState != (buttonIsPressed || triggerIsPressed))
        {
            virtualMouse.CopyState<MouseState>(out var mouseState);
            mouseState.WithButton(MouseButton.Left, (buttonIsPressed || triggerIsPressed));
            InputState.Change(virtualMouse, mouseState);
            prevMouseState = (buttonIsPressed || triggerIsPressed);
        }

        // Update cursor in screen
        AnchoredCursorUpdate(newPosition);
    }

    private void UpdateRealMouse()
    {
        // If no real mouse return
        if (currentMouse == null) return;

        // Read mouse position
        Vector2 newPosition = currentMouse.position.ReadValue();

        // Dont go out of the screen
        newPosition.x = Mathf.Clamp(newPosition.x, padding, Screen.width - padding);
        newPosition.y = Mathf.Clamp(newPosition.y, padding, Screen.height - padding);

        // Update cursor in screen
        AnchoredCursorUpdate(newPosition);
    }

    private void AnchoredCursorUpdate(Vector2 position)
    {
        // Transform to a valid position and set to the cursor
        Vector2 anchoredPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, position, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera, out anchoredPosition);
        cursorTransform.anchoredPosition = anchoredPosition;
    }


    // If not work or disabled OnCHangeControlsEvent
    private void Update()
    {
        // Check for schemas changes
        if (previousControlScheme != playerInput.currentControlScheme) OnControlsChange(playerInput);
        previousControlScheme = playerInput.currentControlScheme;

        // Only if mouse is in use execute the code
        if (updateRealMouseCursor) UpdateRealMouse();

        // If inactive time grather than 0
        //Debug.Log($"inactiveHideTime: {inactiveHideTime}  inactiveTime: {inactiveTime}");
        if (inactiveHideTime > 0)
        {
            // If real mouse is click
            var isCurrentMouseClick = false;
            if (currentMouse != null) isCurrentMouseClick = currentMouse.leftButton.isPressed;

            // If move
            if ((lastCursorSpritePosition != cursorTransform.position) || isGamePadPressed || isCurrentMouseClick)
            {
                inactiveTime = inactiveHideTime;
                cursorImage.enabled = isVirtualCursorVisible;
                lastCursorSpritePosition = cursorTransform.position;
            }
            // If dont move
            else
            {
                if (inactiveTime > 0)
                {
                    inactiveTime -= Time.unscaledDeltaTime;
                    //Debug.Log($"PP inactiveHideTime: {inactiveHideTime}  inactiveTime: {inactiveTime}");
                }
                else cursorImage.enabled = false;
            }
        }


    }

    private float Smooth(float stickValue)
    {
        float sign = 1f;
        if (stickValue < 0f) sign = -1f;

        return sign * smoothFunction.Evaluate(Mathf.Abs(stickValue));
    }

    public void CursorAlfa(float alfa)
    {
        var cc = cursorImage.color;
        cc.a = Mathf.Clamp01(alfa);
        cursorImage.color = cc;
    }

}
