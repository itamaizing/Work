using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LvlInfoMain : MonoBehaviour
{
    [SerializeField] private Slider _lvlBar;
    [SerializeField] private TMP_Text _LvlText;

    private HeroComponent _currentHero;

    private int _lvlValue;
    private int _expValue;
    private int _maxExpValue;

    private void OnEnable()
    {
        LevelCharacterManager.Instance.OnLevelChanged += HandleLevelChanged;
        LevelCharacterManager.Instance.OnExperienceChanged += HandleExperienceChanged;
        UpdateInfoIfCurrent();
    }

    private void OnDisable()
    {
        LevelCharacterManager.Instance.OnLevelChanged -= HandleLevelChanged;
        LevelCharacterManager.Instance.OnExperienceChanged -= HandleExperienceChanged;
    }

    public void SetInfo(int level, int exp, int maxExp, HeroComponent hero)
    {
        _lvlValue = level;
        _expValue = exp;
        _maxExpValue = maxExp;
        _currentHero = hero;

        UpdateInfo();
    }

    private void HandleExperienceChanged(int newExp, int maxExp)
    {
        if (LevelCharacterManager.Instance.TryGetCurrentHero(out var current) && current == _currentHero)
        {
            _expValue = newExp;
            _maxExpValue = maxExp;
            UpdateInfo();
        }
    }

    private void HandleLevelChanged(int newLevel)
    {
        if (LevelCharacterManager.Instance.TryGetCurrentHero(out var current) && current == _currentHero)
        {
            _lvlValue = newLevel;
            _expValue = LevelCharacterManager.Instance.GetCurrentExperience();
            _maxExpValue = LevelCharacterManager.Instance.GetExperienceForNextLevel();

            AnimateLevelText();
            UpdateInfo();
        }
    }

    private void UpdateInfoIfCurrent()
    {
        if (_currentHero == null) return;
        if (!LevelCharacterManager.Instance.TryGetCurrentHero(out var current)) return;
        if (current != _currentHero) return;

        _lvlValue = LevelCharacterManager.Instance.GetCurrentLevel();
        _expValue = LevelCharacterManager.Instance.GetCurrentExperience();
        _maxExpValue = LevelCharacterManager.Instance.GetExperienceForNextLevel();

        UpdateInfo();
    }

    private void UpdateInfo()
    {
        if (_maxExpValue <= 0) _maxExpValue = 1;
        _lvlBar.value = (float)_expValue / _maxExpValue;
        _LvlText.text = _lvlValue.ToString();
    }

    private void AnimateLevelText()
    {
        _LvlText.transform.DOKill();
        _LvlText.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        _LvlText.transform.DOScale(0.5f, 0.5f).SetEase(Ease.OutBounce);
    }
}
