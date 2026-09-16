using UnityEngine;
using UnityEngine.UI;

public class UIGameRightSidePanel : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private GameObject _rightPanelHolder;

    private void Awake()
    {
        _rightPanelHolder.SetActive(false);
    }

    private void OnEnable()
    {
        if (TargetSelector.Instance != null)
            TargetSelector.Instance.TargetChanged += HandleTargetChanged;
    }

    private void OnDisable()
    {
        if (TargetSelector.Instance != null)
            TargetSelector.Instance.TargetChanged -= HandleTargetChanged;
    }

    private void HandleHeaderClicked()
    {
        TargetSelector.Instance?.ClearTarget();
    }

    private void HandleTargetChanged(Character target)
    {
        if (target == null)
        {
            _rightPanelHolder.SetActive(false);
            return;
        }

        _rightPanelHolder.SetActive(true);

        if (target is HeroComponent hero)
        {
            _attributesPanel.gameObject.SetActive(true);
            _attributesPanel.Show(hero, false);

            _talentsPanel.gameObject.SetActive(true);
            _talentsPanel.Show(hero.TalentManager, true, isInteractable: false);
        }
        else
        {
            _attributesPanel.gameObject.SetActive(false);
            _talentsPanel.gameObject.SetActive(false);
        }
    }
}