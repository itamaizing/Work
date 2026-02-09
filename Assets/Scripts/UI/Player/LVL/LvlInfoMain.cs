using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LvlInfoMain : MonoBehaviour
{
    [SerializeField] private Slider _lvlBar;
    [SerializeField] private TMP_Text _LvlText;

    private int _lvlValue;
    private int _expValue;
    private int _maxExpValue;

    private void OnEnable()
    {
        Init();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Init()
    {
        var levelManager = LevelCharacterManager.Instance;

        _lvlValue = levelManager.GetCurrentLevel();
        _expValue = levelManager.GetCurrentExperience();
        _maxExpValue = levelManager.GetExperienceForNextLevel();

        UpdateInfo();

        levelManager.OnLevelChanged += HandleLevelChanged;
        //levelManager.OnExperienceChanged += HandleExpChanged;
        //levelManager.OnMaxExperienceChanged += HandleMaxExpChanged;
    }

    private void Unsubscribe()
    {
        var levelManager = LevelCharacterManager.Instance;

        levelManager.OnLevelChanged -= HandleLevelChanged;
        //levelManager.OnExperienceChanged -= HandleExpChanged;
        //levelManager.OnMaxExperienceChanged -= HandleMaxExpChanged;
    }

    private void HandleLevelChanged(int newLevel)
    {
        _lvlValue = newLevel;

        AnimateLevelText();
        UpdateInfo();
    }

    private void HandleExpChanged(int exp)
    {
        _expValue = exp;
        UpdateInfo();
    }

    private void HandleMaxExpChanged(int maxExp)
    {
        _maxExpValue = maxExp;
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
        _LvlText.transform.localScale = new Vector3(2f, 2f, 2f);
        _LvlText.transform.DOScale(1f, 1f).SetEase(Ease.OutBounce);
    }
}
