using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributePanel : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private AttributeItem[] _attributes;


    public void SwitchVisible(bool visible)
    {
        _content.SetActive(visible);
    }
	public void SwitchVisible()
	{
		_content.SetActive(!_content.activeSelf);
	}

	public void Init(HeroComponent character)
    {
        _attributes[0].Init(null, character.PlayerData.Health);
        _attributes[1].Init(null, character.PlayerData.Stamina);

        _attributes[2].Init(null, character.PlayerData.HealthInfo.DefaultPhysicsDamage);
        _attributes[3].Init(null, character.PlayerData.HealthInfo.DefaultMagicDamage);
        _attributes[4].Init(null, character.PlayerData.HealthInfo.EvadeMeleeDamage);
        _attributes[5].Init(null, character.PlayerData.HealthInfo.EvadeRangeDamage);
        _attributes[6].Init(null, character.PlayerData.HealthInfo.EvadeMagicDamage);

    }
}
