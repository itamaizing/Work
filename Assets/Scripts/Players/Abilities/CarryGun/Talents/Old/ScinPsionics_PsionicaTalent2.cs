using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScinPsionics_PsionicaTalent2 : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;

    private GameObject _player;
    private float _originalAvoidanceChance;

    void Start()
    {
        _player = transform.parent.gameObject;
        _originalAvoidanceChance = _player.GetComponent<PsionicaMelee>().AbsorptionChance;
    }

    void Update()
    {
        if (_toggleTalent.isOn)
        {
            _player.GetComponent<PsionicaMelee>().AbsorptionChance += _originalAvoidanceChance + 0.3f;
            StartCoroutine(RechargeCoroutine());
        }
    }

    private IEnumerator RechargeCoroutine()
    {
        yield return new WaitForSeconds(12);
        _toggleTalent.isOn = false;
    }
}
