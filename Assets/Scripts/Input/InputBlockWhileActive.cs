using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum InputBlockMode
{
    /// <summary>Пока объект активен.</summary>
    WhileActive,

    /// <summary>Пока фокус на объекте: последний клик был внутри, или выбран его дочерний элемент.
    /// Клик мимо или Esc снимают фокус.</summary>
    WhileFocused,

    /// <summary>Пока курсор над объектом.</summary>
    WhileHovered
}

public class InputBlockWhileActive : MonoBehaviour
{
    [SerializeField] private InputLayer _layers = InputLayer.World;
    [SerializeField] private InputBlockMode _mode = InputBlockMode.WhileActive;
    [SerializeField] private bool _releaseOnEscape = true;
    [SerializeField] private string _reason;

    private IDisposable _handle;
    private bool _clicked;

    public bool IsBlocking => _handle != null;

    private void OnEnable()
    {
        _clicked = false;
        InputBlocker.PointerPressed += OnPointerPressed;
        Apply(_mode == InputBlockMode.WhileActive);
    }

    private void OnDisable()
    {
        InputBlocker.PointerPressed -= OnPointerPressed;
        _clicked = false;
        Apply(false);
    }

    private void Update()
    {
        if (_mode == InputBlockMode.WhileActive) return;

        if (_mode == InputBlockMode.WhileFocused && _clicked && _releaseOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            _clicked = false;
            DeselectInside();
        }

        Evaluate();
    }
    
    public void Unfocus()
    {
        _clicked = false;
        DeselectInside();
        Evaluate();
    }

    private void OnPointerPressed()
    {
        if (_mode != InputBlockMode.WhileFocused) return;

        _clicked = InputBlocker.IsPointerOver(transform);
        if (!_clicked)
            DeselectInside();

        Evaluate();
    }

    private void Evaluate()
    {
        bool block;
        switch (_mode)
        {
            case InputBlockMode.WhileFocused:
                block = _clicked || SelectionInside();
                break;
            case InputBlockMode.WhileHovered:
                block = InputBlocker.IsPointerOverUI() && InputBlocker.IsPointerOver(transform);
                break;
            default:
                block = true;
                break;
        }
        Apply(block);
    }

    private void Apply(bool block)
    {
        if (block && _handle == null)
            _handle = InputBlocker.Block(_layers, string.IsNullOrEmpty(_reason) ? name : _reason);
        else if (!block && _handle != null)
        {
            _handle.Dispose();
            _handle = null;
        }
    }

    private bool SelectionInside()
    {
        var es = EventSystem.current;
        if (es == null || es.currentSelectedGameObject == null) return false;
        return es.currentSelectedGameObject.transform.IsChildOf(transform);
    }

    private void DeselectInside()
    {
        if (SelectionInside())
            EventSystem.current.SetSelectedGameObject(null);
    }
}