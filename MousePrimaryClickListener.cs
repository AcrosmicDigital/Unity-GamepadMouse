using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MousePrimaryClickListener : MonoBehaviour
{

    [SerializeField] private bool listenOverUI = false;
    [SerializeField] private bool listenOutsideScreen = false;
    [Space(10)]
    [SerializeField] public UnityEvent OnClick = new UnityEvent();
    [SerializeField] public UnityEvent OnHold = new UnityEvent();
    [SerializeField] public UnityEvent<Vector2> OnUpdate = new UnityEvent<Vector2>();


    private Exxp_MouseWithGamePad controls;
    private int clickCounter = 0;


    public bool IsInsideScreen { get; private set; }  // True if is inside screen
    public bool IsClick { get; private set; } // True only in update frame that start click
    public bool IsHold { get; private set; } // True while mouse button is down
    public Vector2 CurrentMousePoss { get; private set; }
    public Vector2 LastClickPoss { get; private set; }


    private void Awake()
    {
        controls = new Exxp_MouseWithGamePad();
    }


    private void Update()
    {

        // Reset values
        IsHold = false;
        IsClick = false;

        // Get mouse Position
        Vector2 mousePossInScren = controls.UI.Point.ReadValue<Vector2>();

        // Ckeck if mouse is in screen or return and not update anything
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
        IsInsideScreen = screenRect.Contains(mousePossInScren);
        if (!IsInsideScreen && !listenOutsideScreen) return;

        // UPDATE MOUSE POSITION
        CurrentMousePoss = Camera.main.ScreenToWorldPoint(mousePossInScren);

        // OnUpdate
        try
        {
            OnUpdate?.Invoke(CurrentMousePoss);
        }
        catch (System.Exception e)
        {
            Debug.LogError("PrimaryClickListener.OnClick: Exception in listener, " + e);
        }

        // Check for clicks
        if (controls.UI.Click.ReadValue<float>() == 1f)
        {
            // Dont liset over UI
            if (!listenOverUI)
            {
                if (EventSystem.current != null) if (EventSystem.current.IsPointerOverGameObject() && !listenOverUI) return;
                //Debug.Log("Mouseclicked");
            }

            IsHold = true;
        }


        // CHECK FOR FIRST CLICK
        if (IsHold) clickCounter++;
        else clickCounter = 0;


        // Set true only in the first click
        if (clickCounter == 1)
        {
            IsClick = true;
            LastClickPoss = CurrentMousePoss;
        }



        // Trigger events
        if (IsClick)
        {
            try
            {
                OnClick?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError("PrimaryClickListener.OnClick: Exception in listener, " + e);
            }
        }
        if (IsHold)
        {
            try
            {
                OnHold?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError("PrimaryClickListener.OnHold: Exception in listener, " + e);
            }
        }


        //var mousePosition = controls.UI.Point.ReadValue<Vector2>();
        //var isClicked = controls.UI.Click.ReadValue<float>();

        ////if(isClicked) Debug.Log($"Position: {mousePosition} Click: {isClicked}");
        //Debug.Log($"Position: {mousePosition} Click: {isClicked}");
    }


    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

}
