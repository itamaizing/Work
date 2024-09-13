using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillPanel : MonoBehaviour
{
    [SerializeField] private RebindUI _rebindUI;
    [SerializeField] private SkillIcon[] _skillIcons = new SkillIcon[15];
    private Character _currentCharacter;

    private void Start()
    {
        UpdateKey();
    }
        
    public void UpdateKey()
    {
        _skillIcons[0].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell1.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[1].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell2.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[2].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell3.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[3].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell4.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[4].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell5.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[5].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell6.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[6].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell7.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[7].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell8.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[8].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell9.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[9].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell10.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[10].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell11.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[11].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell12.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[12].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell13.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[13].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell14.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[14].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell15.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[15].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell16.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
    }

    public void UpdateKeys(RebindUI rebindUI, string key, string z, string x)
    {
        Debug.Log(key);
        Debug.Log(z);
        Debug.Log(x);
        _skillIcons[0].Key.text = key;
    }
}
