using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Flags]
public enum InputLayer
{
    None     = 0,
    Gameplay = 1 << 0,   // движение, клики по миру, способности (Spell1..16), режимы Shift/Space/Alt
    Camera   = 1 << 1,   // клавиши и мышь камеры, зум колесом, края экрана
    Hotkeys  = 1 << 2,   // ShowMenu / ShowStatistics / ShowSource

    World = Gameplay | Camera,            // обычное окно поверх игры (настройки, таланты)
    All   = Gameplay | Camera | Hotkeys   // ввод текста: даже клавиши-переключатели UI не должны срабатывать
}

public static class InputBlocker
{
    private sealed class Handle : IDisposable
    {
        public readonly InputLayer Layers;
        public readonly string Reason;
        private bool _disposed;

        public Handle(InputLayer layers, string reason)
        {
            Layers = layers;
            Reason = reason;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Release(this);
        }
    }

    private static readonly List<Handle> Active = new List<Handle>();
    private static readonly List<RaycastResult> RaycastBuffer = new List<RaycastResult>();
    private static InputLayer _mask;
    private static int _lastPressFrame = -1;

    private static IDisposable _typingBlock;
    private static GameObject _lastSelected;
    private static TMP_InputField _tmpField;
    private static InputField _legacyField;

    public static event Action<InputLayer> Changed;

    public static event Action PointerPressed;

    public static InputLayer Mask => _mask;

    public static bool IsBlocked(InputLayer layer) => (_mask & layer) != 0;

    public static IDisposable Block(InputLayer layers, string reason = null)
    {
        var handle = new Handle(layers, reason);
        Active.Add(handle);
        Recalculate();
        return handle;
    }

    public static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public static bool IsPointerOver(Transform root)
    {
        var es = EventSystem.current;
        if (es == null || root == null) return false;

        var data = new PointerEventData(es) { position = Input.mousePosition };
        RaycastBuffer.Clear();
        es.RaycastAll(data, RaycastBuffer);

        foreach (var r in RaycastBuffer)
            if (r.gameObject != null && r.gameObject.transform.IsChildOf(root))
                return true;
        return false;
    }

    public static void NotifyPointerPress()
    {
        if (_lastPressFrame == Time.frameCount) return;
        _lastPressFrame = Time.frameCount;

        var es = EventSystem.current;
        if (es != null)
        {
            var selected = es.currentSelectedGameObject;
            if (selected != null && IsTextField(selected) && !IsPointerOver(selected.transform))
                es.SetSelectedGameObject(null);
        }

        UpdateTypingBlock();
        PointerPressed?.Invoke();
    }

    public static string Describe()
    {
        if (Active.Count == 0) return "Ввод не заблокирован";
        var sb = new StringBuilder("Блокировки ввода:");
        foreach (var h in Active)
            sb.Append("\n  ").Append(h.Layers).Append(" — ").Append(string.IsNullOrEmpty(h.Reason) ? "?" : h.Reason);
        return sb.ToString();
    }

    private static void Release(Handle handle)
    {
        Active.Remove(handle);
        Recalculate();
    }

    private static void Recalculate()
    {
        InputLayer mask = InputLayer.None;
        foreach (var h in Active)
            mask |= h.Layers;

        if (mask == _mask) return;
        _mask = mask;
        Changed?.Invoke(mask);
    }

    internal static void UpdateTypingBlock()
    {
        bool typing = IsTextFieldFocused();

        if (typing && _typingBlock == null)
            _typingBlock = Block(InputLayer.All, "Ввод текста");
        else if (!typing && _typingBlock != null)
        {
            _typingBlock.Dispose();
            _typingBlock = null;
        }
    }

    internal static void ReleaseTypingBlock()
    {
        _typingBlock?.Dispose();
        _typingBlock = null;
    }

    private static void CacheSelected(GameObject selected)
    {
        if (selected == _lastSelected) return;
        _lastSelected = selected;
        _tmpField = selected != null ? selected.GetComponent<TMP_InputField>() : null;
        _legacyField = selected != null ? selected.GetComponent<InputField>() : null;
    }

    private static bool IsTextField(GameObject selected)
    {
        CacheSelected(selected);
        return _tmpField != null || _legacyField != null;
    }

    private static bool IsTextFieldFocused()
    {
        var es = EventSystem.current;
        if (es == null) return false;

        CacheSelected(es.currentSelectedGameObject);

        if (_tmpField != null) return _tmpField.isFocused;
        if (_legacyField != null) return _legacyField.isFocused;
        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Active.Clear();
        _mask = InputLayer.None;
        _typingBlock = null;
        _lastSelected = null;
        _tmpField = null;
        _legacyField = null;
        _lastPressFrame = -1;
        Changed = null;
        PointerPressed = null;

        var go = new GameObject("[InputBlocker]") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<InputBlockerRunner>();
    }
}

internal class InputBlockerRunner : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            InputBlocker.NotifyPointerPress();

        InputBlocker.UpdateTypingBlock();
    }

    private void OnDisable()
    {
        InputBlocker.ReleaseTypingBlock();
    }
}