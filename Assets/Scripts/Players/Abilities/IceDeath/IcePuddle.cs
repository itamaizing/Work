using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class IcePuddle : Skill, IEnergyDamagable, IComboSeriesParticipatingSkill
{
    [Header("Puddle prefabs & preview")]
    [SerializeField] private IcePuddleObject _puddle;
    [SerializeField] private IcePuddleObject _puddleBig;
    [SerializeField] private GameObject preViewPuddlePrefab;
    [SerializeField] private GameObject preViewBigPuddlePrefab;

    [Header("Ability settings")]
    [SerializeField] private float _timeToDestroy = 3f;
    [SerializeField] private float _maxLifePuddleTime = 7f;
    [SerializeField] private MoveComponent _move;
    [SerializeField] private AudioClip _audioClip;

    [Header("Raycast")]
    [SerializeField] private LayerMask _groundLayer;

    private AudioSource _audioSource;
    private Energy _energy;
    private GameObject _preViewPuddle;

    private Vector3 _placedPosition;
    private float _placedAngleDeg;

    private bool _shooted = false;
    private bool _lastHit = false;

    private bool _talentPuddleSize = false;
    private bool _talentFrostingFrozen = false;
    private bool _talentEvadeDadBoost = false;
    private bool _iceDeathInIcePudleTalent;

    private void OnEnable()
    {
        OnSkillCanceled += ClearData;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= ClearData;
    }

    protected override bool IsCanCast
    {
        get
        {
            return Vector3.Distance(_placedPosition, transform.position) <= AreaInfo.Radius;
        }
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("IcePuddle");

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void RefreshPreview()
    {
        GameObject prefab = _isSeriesPotentialFinal ? preViewBigPuddlePrefab : preViewPuddlePrefab;

        if (_preViewPuddle == null)
        {
            _preViewPuddle = Instantiate(prefab);
            return;
        }

        bool needBig =
            _preViewPuddle.name.Contains(preViewBigPuddlePrefab.name);

        if (needBig != _isSeriesPotentialFinal)
        {
            Destroy(_preViewPuddle);
            _preViewPuddle = Instantiate(prefab);
        }
    }
    
    private void UpdatePreviewAtMouse()
    {
        Vector3 mousePos = GetMousePointOnGround();
        if (float.IsPositiveInfinity(mousePos.x)) return;

        if (_preViewPuddle && !_preViewPuddle.activeSelf)
            _preViewPuddle.SetActive(true);

        if (_preViewPuddle)
        {
            _preViewPuddle.transform.position = mousePos;

            Vector3 dir = _hero.transform.position - mousePos;
            dir.y = 0f;

            if (dir != Vector3.zero)
            {
                Quaternion rotation = Quaternion.LookRotation(dir);
                _preViewPuddle.transform.rotation = Quaternion.Euler(-90f, rotation.eulerAngles.y, 0f);
            }
        }
    }

    private Vector3 GetMousePointOnGround()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
        {
            return hit.point;
        }
        return Vector3.positiveInfinity;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
        {
            _placedPosition = (Vector3)targetInfo.Points[0];
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        if (_energy == null)
            _energy = (Energy)Hero.Resources[ResourceType.Energy]; ;

        if (_preViewPuddle == null)
        {
            _preViewPuddle = Instantiate(
                _isSeriesPotentialFinal
                    ? preViewBigPuddlePrefab
                    : preViewPuddlePrefab);
        }
        
        while (true)
        {
            if (_previewDirty)
            {
                RefreshPreview();
                _previewDirty = false;
            }
            
            UpdatePreviewAtMouse();


            if (GetMouseButton)
            {
                Vector3 clickPoint = GetMousePointOnGround();
                if (float.IsPositiveInfinity(clickPoint.x))
                {
                    yield return null;
                    continue;
                }

                float dist = Vector3.Distance(_hero.transform.position, clickPoint);
                if (dist > AreaInfo.Radius)
                {
                    yield return null;
                    continue;
                }
                Vector3 direction = (_hero.transform.position - _placedPosition).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
                    _placedAngleDeg = lookRot.eulerAngles.y;
                }
                var info = new TargetInfo();
                info.Points.Add(clickPoint);
                callbackDataSaved?.Invoke(info);

                break;
            }


            yield return null;
        }


        if (_preViewPuddle) _preViewPuddle.SetActive(false);
    }

    protected override IEnumerator CastJob()
    {
        var dir = _placedPosition - _hero.transform.position;
        _placedAngleDeg = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg - 90f;
        
        ShootAtPosition(_placedPosition,_placedAngleDeg);

        yield return null;
    }

    private void ShootAtPosition(Vector3 position,float angle)
    {
        int timeToAdd = (int)_energy.CurrentValue / 5;
        if (timeToAdd > 4) timeToAdd = 4;

        float energyToSpend = timeToAdd * 5f;
        float lifeTime = LifeTimePuddle(timeToAdd);

        OnSeriesDamaged?.Invoke(null, this);
        _energy.CmdUse(energyToSpend);

        bool isBig = _lastHit && _talentPuddleSize;

        if (isBig)
            CmdCreateProjecttileBig(angle, position, lifeTime, _lastHit, _talentEvadeDadBoost, _talentFrostingFrozen);
        else
            CmdCreateProjecttile(angle, position, lifeTime, _lastHit, _talentEvadeDadBoost, _talentFrostingFrozen);

        _lastHit = false;
    }

    protected override void ClearData()
    {
        _move?.StopLookAt();

        _shooted = false;
        _placedPosition = Vector3.positiveInfinity;
        //_placedAngleDeg = 0f;

        if (_preViewPuddle)
        {
            Destroy(_preViewPuddle);
            _preViewPuddle = null;
        }
    }

    private float LifeTimePuddle(float timeToAdd)
    {
       return Mathf.Min(_maxLifePuddleTime, _timeToDestroy + timeToAdd);
    }

    [Command]
    private void CmdCreateProjecttile(float angle, Vector3 position, float timeToDestroy, bool lastHit, bool talentEvade, bool talentFrostingFrozen)
    {
        IcePuddleObject puddle = Instantiate(_puddle, position, Quaternion.Euler(-90, -angle, 0));
        puddle.Init(Hero, timeToDestroy, lastHit, this);
        puddle.SetTalents(talentEvade, talentFrostingFrozen);
        puddle.IceDeathInIcePudleTalentActive(_iceDeathInIcePudleTalent);

        NetworkServer.Spawn(puddle.gameObject);
        RpcPlayShotSound();
    }

    [Command]
    private void CmdCreateProjecttileBig(float angle, Vector3 position, float timeToDestroy, bool lastHit, bool talentEvade, bool talentFrostingFrozen)
    {
        IcePuddleObject projectile = Instantiate(_puddleBig, position, Quaternion.Euler(-90, -angle, 0));
        //SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
        projectile.Init(Hero, timeToDestroy, lastHit, this);
        projectile.SetTalents(talentEvade, talentFrostingFrozen);

        NetworkServer.Spawn(projectile.gameObject);
        RpcPlayShotSound();
    }

    [ClientRpc]
    private void RpcInit(GameObject obj, float manaValue, bool lastHit)
    {
        var puddle = obj.GetComponent<IcePuddleObject>();
        if (!puddle) return;

        puddle.Init(Hero, manaValue, lastHit, this);
        puddle.IceDeathInIcePudleTalentActive(_iceDeathInIcePudleTalent);
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && _audioClip != null)
            _audioSource.PlayOneShot(_audioClip);
    }

    public void SetTalentPuddleSize(bool active) => _talentPuddleSize = active;
    public void SetTalentFrostingFrozen(bool value) => _talentFrostingFrozen = value;
    public void SetTalentEvadeDadBoost(bool value) => _talentEvadeDadBoost = value;
    public void IceDeathInIcePudleTalentActive(bool value)
    {
        _iceDeathInIcePudleTalent = value;
    }

    public void IcePuddleCast() => AnimStartCastCoroutine();
    public void IcePuddleEnd()
    {
        AnimCastEnded();
        if (_move) _move.SetCanMove(true);
    }

    public void StopMoveIcePuddle()
    {
        if (_move) _move.SetCanMove(false);
    }

    public bool IsStreamSkill { get; }
    public bool IsFrostEnergyApplied => true;
    
    #region Series
    
    private bool _isSeriesPotentialFinal;
    private bool _previewDirty;
    
    public event IComboSeriesParticipatingSkill.OnBeforeApplyDamageDelegate OnBeforeApplySeriesDamage;
    public event Action<GameObject, Skill> OnSeriesDamaged;
    public float EnergyCostOnHit { get; }
    public void OnSeriesHit(int hitCountInCurrentSeries, Character target)
    {
    }

    public void OnSeriesCompleted(Character target, int totalHits, float totalEnergySpent)
    {
        _lastHit = true;
    }

    public void OnSeriesBroken(Character target)
    {
        _isSeriesPotentialFinal = false;
        _lastHit = false;
    }

    public void OnSeriesPotentialFinal(Skill skill, bool isPotentialFinal)
    {
        _isSeriesPotentialFinal = isPotentialFinal;
        _previewDirty = true;
    }

    #endregion

}
