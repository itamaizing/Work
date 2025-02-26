using UnityEngine;

public class TeamSource : MonoBehaviour
{
    [SerializeField] private HeroInfoUI[] _heroInfoUI = new HeroInfoUI[2];

    private void OnEnable()
    {
        UpdateInfo();
    }

    public void SwitchEnable()
    {
        if (gameObject.activeSelf == true)
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
    }

    public void AddInFirstTeam(Character character)
    {
        _heroInfoUI[0].SetHero(character);
    }

    public void AddInSecondTeam(Character character)
    {
        _heroInfoUI[1].SetHero(character);
    }

    private void UpdateInfo()
    {
        foreach (var item in _heroInfoUI)
        {
            item.UpdateInfo();
        }
    }
}
