using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IceRolling : Skill
{
    [Header("Ability properties")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PhysicalAttack _physicalAttack;
    [SerializeField] private float _jumprange = 5f;
    [SerializeField] private float _durationOfJump = 0.3f;
    [SerializeField] private AudioClip audioClip;

    private AudioSource _audioSource;
    private Vector3 _mousePos = Vector3.positiveInfinity;
    private Vector3 _jumpPos;
    private Vector3 _lookDir;
    private Energy _energy;
    private bool _rollingPhysTalent = false;
    private float _jumpCount = 0;
    private bool _afterJump;
    private float _afterJumpDelay = 1;
    private Character _target;

    protected override bool IsCanCast
    {
        get
        {
            if (_target != null) return Vector3.Distance(_target.transform.position, transform.position) <= Radius;
            else return true;
        }
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();

        for (int i = 0; i < _playerLinks.Resources.Count; i++)
        {
            if (_playerLinks.Resources[i].Type == ResourceType.Energy)
            {
                _energy = (Energy)_playerLinks.Resources[i];
            }
        }
    }

    private void Update()
    {
        if (_afterJump)
        {
            TimerDelay();
        }
    }

    private void Jump()
    {
        float actualJumpRange = _jumprange;
        _lookDir = (_mousePos - _playerLinks.transform.position).normalized;
        Vector3 jumpPos = _lookDir * actualJumpRange + _playerLinks.transform.position;

        if (CheckObstacleBetween(_playerLinks.transform.position, jumpPos))
        {
            _jumpCount = 5;
            CmdPush(_jumpPos);
        }
        else
        {
            for (int i = 0; i < 10; i++)
            {
                _jumpCount += 0.2f;
                actualJumpRange += 0.2f;
                Vector3 jumpPos2 = _lookDir * actualJumpRange + _playerLinks.transform.position;
                if (_energy.CurrentValue >= 5 && !CheckObstacleBetween(_playerLinks.transform.position, jumpPos2))
                {
                    _energy.CmdUse(1);
                    jumpPos = jumpPos2;
                }
            }
            CmdPush(jumpPos);

            if (_rollingPhysTalent)
            {
                _physicalAttack.TalentRollingPhys(_afterJump, _jumpCount);
                _afterJump = true;
            }
        }

        _target = null;

        _mousePos = Vector3.positiveInfinity;
        _lookDir = Vector3.zero;
        _jumpPos = Vector3.zero;
        ClearData();
    }

    private bool CheckObstacleBetween(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit[] hits =
            Physics.BoxCastAll(start, new Vector2(2f, 2f), direction, Quaternion.identity, distance, _obstacle);

        foreach (RaycastHit hit in hits)
        {
            _jumpPos = hits[0].point - direction * 1.2f;
            return true;
        }

        return false;
    }

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_mousePos.x))
        {
            if (GetMouseButton)
            {
                if (GetTarget().isCharater)
                {
                    float distance = Vector3.Distance(_hero.transform.position, _mousePos);

                    if (distance <= Radius) _mousePos = GetTarget().character.transform.position;

                    else
                    {
                        _target = GetTarget().character;
                        _mousePos = _target.transform.position;
                    }
                }

                else _mousePos = GetTarget().Position;
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        Jump();
        yield return null;
    }

    protected override void ClearData()
    {

    }

    [Command]
    private void CmdPush(Vector3 force)
    {
        RpcPlayShotSound();
        _playerLinks.Move.TargetRpcDoMove(force, _durationOfJump);
    }

    public void TalentRollingPhys(bool value)
    {
        _rollingPhysTalent = value;
    }

    private void TimerDelay()
    {
        _afterJumpDelay -= Time.deltaTime;
        if (_afterJumpDelay < 0)
        {
            _afterJump = false;
            _physicalAttack.TalentRollingPhys(_afterJump, 0);
        }
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
    }
}
