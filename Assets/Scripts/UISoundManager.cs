using System;
using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    [SerializeField] private Transform emptyClickSfxPrefab;
    [SerializeField] private Transform UIButtonClickSfxPrefab;
    [SerializeField] private Transform anyKeyboardPressedSfxPrefab;
    [SerializeField] private Transform enterPressedSfxPrefab;
    [SerializeField] private Transform spacePressedSfxPrefab;

    void Start()
    {
        MouseManager.Instance.OnUIButtonClicked += MouseManager_OnUIButtonClicked;
        MouseManager.Instance.OnEnterPressed += MouseManager_OnEnterPressed;
        MouseManager.Instance.OnSpacePressed += MouseManager_OnSpacePressed;
        MouseManager.Instance.OnAnyKeyboardPressed += MouseManager_OnAnyKeyboardPressed;
    }

    private void MouseManager_OnUIButtonClicked(object sender, EventArgs e)
    {
        Debug.Log("Left mouse button clicked!");
        Transform sfxTransform = Instantiate(UIButtonClickSfxPrefab);
        Destroy(sfxTransform.gameObject, .1f);
    }

    private void MouseManager_OnEnterPressed(object sender, EventArgs e)
    {
        Transform sfxTransform = Instantiate(enterPressedSfxPrefab);
        Destroy(sfxTransform.gameObject, .5f);
    }

    private void MouseManager_OnSpacePressed(object sender, EventArgs e)
    {
        Transform sfxTransform = Instantiate(spacePressedSfxPrefab);
        Destroy(sfxTransform.gameObject, .5f);
    }

    private void MouseManager_OnAnyKeyboardPressed(object sender, EventArgs e)
    {
        Transform sfxTransform = Instantiate(anyKeyboardPressedSfxPrefab);
        Destroy(sfxTransform.gameObject, .5f);
    }
}
