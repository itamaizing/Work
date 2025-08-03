using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContagionOfEmbrio : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;
    [SerializeField] private GameObject _embryoDebaffPrefab;
    public List <GameObject> controlledObjects = new List<GameObject>();
    public int _embryoCharge;
    private float _embryoChargeInterval = 7f;
    private float _timer;
    private float _debuffDuration = 3f;
    private GameObject _player;
    private GameObject _playerAbility;
    private GameObject _newPrefab;
    private SelectObject _select;

    private void Start()
    {
        _player = transform.parent.gameObject;
        _playerAbility = _player.transform.Find("Abilities").gameObject;
        _playerAbility.GetComponent<TwoMeleeAttack>().SecondAbilityEvent += damage => UseEmbryoCharge();
        _select = GameObject.Find("Select").GetComponent<SelectObject>();
    }

    private void Update()
    {
        if(_toggleTalent.isOn)
        {
            UpdateEmbryoCharge();
        }

        if(_select.SelectedObject == _player && _select.ControlledObjects.Count != controlledObjects.Count)
        {
            _select.ControlledObjects.Clear();

            foreach (GameObject obj in controlledObjects)
            {
                //_select.ControlledObjects.Add(obj);
            }
        }
    }

    private void UpdateEmbryoCharge()
    {
        _timer += Time.deltaTime;

        if (_timer >= _embryoChargeInterval)
        {
            _embryoCharge += 1;
            _timer = 0f;
        }
    }

    private void UseEmbryoCharge()
    {
        if(_embryoCharge > 0)
        {
            
            _newPrefab = Instantiate(_embryoDebaffPrefab);
            _newPrefab.GetComponent<EmbryonDebuff>().Player = _player;
            _newPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(_debuffDuration);
            _newPrefab.transform.SetParent(_playerAbility.GetComponent<TwoMeleeAttack>().TargetParent.transform);
            
            _embryoCharge -= 1;
        }
    }

    private void OnDisable()
    {
        _playerAbility.GetComponent<TwoMeleeAttack>().SecondAbilityEvent += damage => UseEmbryoCharge();
    }
}
