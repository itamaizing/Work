using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StunByTentacles : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;

    private GameObject _player;
    private GameObject _playerAbility;
    private GameObject _target;
    private bool _isTentacleRetention;
    private float _timer;
    private float _timerDuration = 1f;

    void Start()
    {
        _player = transform.parent.gameObject;
        _playerAbility = _player.transform.Find("Abilities").gameObject;
    }

    void Update()
    {
        if (_toggleTalent.isOn)
        {

            if (_playerAbility.GetComponent<FourMeleeAttack>().CanPull && !_isTentacleRetention)
            {
                _target = _playerAbility.GetComponent<FourMeleeAttack>().Target;
                _target.GetComponent<HealthComponent>().TryTakeDamage(20f, DamageType.Physical, AttackRangeType.MeleeAttack);
                //_target.GetComponent<CharacterState>().AddState(new StunnedState(),0,0,0);//TODO ADDVALUES
                _timer = Time.time;

                _isTentacleRetention = true;
            }
            else if (!_playerAbility.GetComponent<FourMeleeAttack>().CanPull && _isTentacleRetention)
            {
                //_target.GetComponent<CharacterState>().AddState(new DefaultState(),0,0,0);//TODO ADDVALUES
                _isTentacleRetention = false;
            }

            if (_target != null && _target.GetComponent<CharacterState>().IfHasState(new StunnedState()))
            {
                if (Time.time - _timer >= _timerDuration)
                {
                    GetComponent<DeepWounds>().Darts = Mathf.Min(GetComponent<DeepWounds>().Darts + 1, 2);
                    // ���������� ������
                    _timer = Time.time;
                }
            }
        }
    }
}
