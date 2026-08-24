using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuEvents : MonoBehaviour
{
    private UIDocument _document;
    private Button _button;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        if (_document == null) return;

        _button = _document.rootVisualElement.Q<Button>("StartGameButton");
        if (_button != null) _button.RegisterCallback<ClickEvent>(OnPlayGameClick);
    }

    private void OnDisable()
    {
        if (_button != null) _button.UnregisterCallback<ClickEvent>(OnPlayGameClick);
    }

    private void OnPlayGameClick(ClickEvent evt)
    {
        TwoStageExit.ResetProgress();
        SceneManager.LoadScene("Prison cell");
    }
}
