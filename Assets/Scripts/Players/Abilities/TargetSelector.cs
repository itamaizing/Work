using System;
using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    public static TargetSelector Instance { get; private set; }

    public Character CurrentTarget { get; private set; }
    public event Action<Character> TargetChanged;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        InputHandler.OnClick += HandleGroundClick;
    }

    private void OnDisable()
    {
        InputHandler.OnClick -= HandleGroundClick;
    }

    public void SelectTarget(Character character)
    {
        if (CurrentTarget == character) return;

        if (CurrentTarget != null)
            CurrentTarget.Died -= OnTargetDied;

        CurrentTarget = character;

        if (CurrentTarget != null)
            CurrentTarget.Died += OnTargetDied;

        TargetChanged?.Invoke(character);
    }

    public void ClearTarget()
    {
        if (CurrentTarget == null) return;
        CurrentTarget = null;
        TargetChanged?.Invoke(null);
    }

    private void HandleGroundClick()
    {
        if (IsConfirmingCast())
            return;

        ClearTarget();
    }

    private bool IsConfirmingCast()
    {
        var hero = Character.Local;
        if (hero == null || hero.Abilities == null)
            return false;

        foreach (var skill in hero.Abilities.Skills)
        {
            if (skill.IsPreparing)
                return true;
        }
        return false;
    }
    
    private void OnTargetDied(Character character)
    {
        ClearTarget();
    }
}