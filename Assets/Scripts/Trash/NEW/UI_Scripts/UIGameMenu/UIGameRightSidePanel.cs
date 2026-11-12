using UnityEngine;

public class UIGameRightSidePanel : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private GameObject _rightPanelHolder;

    private HeroComponent _inspectedHero;

    private void Awake()
    {
        _rightPanelHolder.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerIconMain.HeroPortraitClicked += OnHeroPortraitClicked;
    }

    private void OnDisable()
    {
        PlayerIconMain.HeroPortraitClicked -= OnHeroPortraitClicked;
    }

    private void OnHeroPortraitClicked(HeroComponent hero, bool isAllied)
    {
        if (isAllied || hero == null) return;
        if (_inspectedHero == hero && _rightPanelHolder.activeSelf)
        {
            _inspectedHero = null;
            _rightPanelHolder.SetActive(false);
            return;
        }

        _inspectedHero = hero;
        _rightPanelHolder.SetActive(true);
        _attributesPanel.gameObject.SetActive(true);
        _talentsPanel.gameObject.SetActive(true);
        _attributesPanel.Show(hero, isMenu: false, isInteractable: false);
        _talentsPanel.Show(hero.TalentManager, isGameUI: true, isInteractable: false);
    }
}
