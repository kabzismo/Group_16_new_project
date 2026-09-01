using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class MainMenuEvents : MonoBehaviour
{
    private UIDocument _document;

    private Button _button;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();

        _button = _document.rootVisualElement.Q<Button>("StartGameButton") as Button;
       
        _button.RegisterCallback<ClickEvent>(OnPlayGameClick);
    }
    
    private void OnDisable()
    {
        _button.UnregisterCallback<ClickEvent>(OnPlayGameClick);
    }


    private void OnPlayGameClick(ClickEvent evt)
    {
        Debug.Log("You pressed the Play Game button!");
    }
}
