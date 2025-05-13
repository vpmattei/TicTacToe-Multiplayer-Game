using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseManager : MonoBehaviour
{
    public static MouseManager Instance { get; private set; }

    public event EventHandler OnUIButtonClicked;
    public event EventHandler OnNotUIButtonClicked;



    public event EventHandler OnEnterPressed;
    public event EventHandler OnSpacePressed;
    public event EventHandler OnAnyKeyboardPressed;

    public string buttonTag = "Button"; // The tag you want to check for

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one MouseManager Instance!");
        }
        Instance = this;
    }

    void Update()
    {
        // Left mouse button click
        if (Input.GetMouseButtonDown(0)) // Check for left mouse button click
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                GameObject clickedObject = result.gameObject;
                if (clickedObject.CompareTag(buttonTag))
                {
                    TriggerOnUIButtonClickedRpc();
                    return;
                }
            }

            if (!EventSystem.current.IsPointerOverGameObject())
            {
                TriggerOnNotUIButtonClickedRpc();
            }
        }

        // Enter keyboard typed
        else if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            TriggerOnEnterPressedRpc();
        }

        // Space keyboard typed
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerOnSpacePressedRpc();
        }

        // Any Keyboard typed
        else if (Input.anyKeyDown)
        {
            TriggerOnAnyKeyboardPressedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnUIButtonClickedRpc()
    {
        OnUIButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnNotUIButtonClickedRpc()
    {
        OnNotUIButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnEnterPressedRpc()
    {
        OnEnterPressed?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnSpacePressedRpc()
    {
        OnSpacePressed?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnAnyKeyboardPressedRpc()
    {
        OnAnyKeyboardPressed?.Invoke(this, EventArgs.Empty);
    }
}
