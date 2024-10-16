using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CreeperInvisible : Skill
{
    #region Variables

    [Header("Talents")]
    [SerializeField] private ReleaseFromSecrecy _releaseFromSecrecy;
    [SerializeField] private DesireToHide _desireToHide;
    [SerializeField] private FirstStrike _firstStrike;
    [SerializeField] private ContinuationAmbush _continuationAmbush;
    [SerializeField] private TransparentPoisons _transparentPoisons;
    [SerializeField] private PreparingForFight _preparingForFight;
    [SerializeField] private ConcentratedPrecision _concentratedPrecision;

    [Header("Ability")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private ColdBlood _coldBlood;

    [Header("Ability Properties")]
    [SerializeField] private Character _player;
    [SerializeField] private SpriteRenderer _playerSprite;

    private float _maxHealth;
    private float _currentHealth;
    private float _timeWithoutDamage = 6.0f;
    private float _distanceWithoutEnemies = 6f;

    private bool _isInvisible;
    private bool _isCanCast;
    private bool _isEnemy;
    private bool _isPlayerSeen = true;
    private bool _isDamagedPlayer = false;
    private bool _isReadyToThreeHitForPreparingForFightTalent = false;
    private bool _isCreeperStrikeIsHit;

    private bool _isClickForExitInvisible = false;
    private bool _isClickForCastInvisibleSkill = false;

    private Coroutine _checkEnemiesCoroutine;
    private Coroutine _checkCurrentHealthPlayerWithTimerCoroutine;
    private Coroutine _checkCurrentHealthPlayerWithoutTimerCoroutine;
    private Coroutine _exitFromInvisibleCoroutine;
    private Coroutine _invisibleAbilitiesCoroutine;

    public bool IsReadyToThreeHitForPreparingForFightTalent { get => _isReadyToThreeHitForPreparingForFightTalent; set => _isReadyToThreeHitForPreparingForFightTalent = value; }
    
    public bool IsInvisible { get => _isInvisible; set => _isInvisible = value; }

    protected override bool IsCanCast => _isCanCast;

    #endregion

    #region PrepareAndCastJob

    protected override void ClearData()
    {
    }

    protected override IEnumerator PrepareJob()
    {
        _maxHealth = _player.Health.CurrentValue;
        switch (_isInvisible)
        {
            case false:
                if (_desireToHide.Data.IsOpen && _desireToHide.IsCanApply)
                {
                    CmdApplyInvisibleWithTalent(); 
                    CmdReducingTransparencySpritePlayer(_player.gameObject);
                    yield break;
                }
                else if (_continuationAmbush.Data.IsOpen && _continuationAmbush.IsCanApplyInvisible)
                {
                    CmdApplyInvisibleWithTalent(); 
                    CmdReducingTransparencySpritePlayer(_player.gameObject);
                    yield break;
                }

                if (_checkCurrentHealthPlayerWithTimerCoroutine == null)
                {
                    yield return _checkCurrentHealthPlayerWithTimerCoroutine = StartCoroutine(CheckCurrentHealthPlayerWithTimer());
                }
                if (_checkEnemiesCoroutine == null)
                {
                    yield return _checkEnemiesCoroutine = StartCoroutine(CheckEnemiesAround());
                }

                _checkCurrentHealthPlayerWithoutTimerCoroutine = StartCoroutine(CheckCurrentHealthPlayerWithoutTimer());

                if (!_isPlayerSeen && !_isDamagedPlayer && !_isInvisible)
                {
                    _isCanCast = true;
                }


                break;
            case true:
                _isCanCast = true;
                _exitFromInvisibleCoroutine = StartCoroutine(ExitFromInvisible());
                _invisibleAbilitiesCoroutine = StartCoroutine(InvisibleAbilities());
                break;
            default:
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_isInvisible && _isClickForExitInvisible)
        {
            CmdRemoveInvisible(_isCreeperStrikeIsHit);
            CmdIncreasingTransparencySpritePlayer(_player.gameObject);

        }
        else if (!_isInvisible)
        {
            EnteringInvisibleState();
            yield break;
        }

        if (_isInvisible && _transparentPoisons.Data.IsOpen)
        {
            //Метод для того, чтобы сделать способности невидымим
            _transparentPoisons.IncreaseManaCost(_isInvisible);
        }
    }

    public void EnteringInvisibleState()
    {
        CmdApplyInvis(_player.gameObject);
        CmdReducingTransparencySpritePlayer(_player.gameObject);
    }

    public void ExitingInvisibleState()
    {
        _isCreeperStrikeIsHit = _creeperStrike.IsHit;
        CmdRemoveInvisible(_isCreeperStrikeIsHit);
        CmdIncreasingTransparencySpritePlayer(_player.gameObject);
    }

    #endregion

    #region Coroutines

    private IEnumerator CheckEnemiesAround()
    {
        while (_isPlayerSeen)
        {
            _isEnemy = false;
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(_player.transform.position, _distanceWithoutEnemies, _targetsLayers);
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy != null && enemy.CompareTag("Enemies"))
                {
                    _isEnemy = true;
                    break;
                }
            }

            if (!_isEnemy)
            {
                _isPlayerSeen = false;
            }
            else
            {
                _isPlayerSeen = true;
            }

            hitEnemies = null;
            yield return null;
        }
    }

    private IEnumerator CheckCurrentHealthPlayerWithTimer()
    {
        float time = _timeWithoutDamage;
        
        while (time > 0f)
        {
            time -= Time.deltaTime;

            _currentHealth = _player.Health.CurrentValue;

            if (_currentHealth < _maxHealth)
            {
                _isDamagedPlayer = true;
                break;
            }

            yield return null;
        }
    }

    private IEnumerator CheckCurrentHealthPlayerWithoutTimer()
    {
        Debug.Log("CheckCurrentHealthWithTimer");
        while (_currentHealth == _maxHealth)
        {
            _currentHealth = _player.Health.CurrentValue;

            if (_currentHealth < _maxHealth)
            {
                Debug.Log("CheckCurrentHealthWithTimer / if (_currentHealth < _maxHealth)");
                ExitingInvisibleState();
                break;
            }
            yield return null;
        }
    }

    private IEnumerator ExitFromInvisible()
    {
        Debug.Log("CreeperInvisible / ExitFromInvisibleCoroutine");
        while (!_isClickForExitInvisible)
        {
            if (Input.GetMouseButton(0))
            {
                _isClickForExitInvisible = true;
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator InvisibleAbilities()
    {
        Debug.Log("CreeperInvisible / InvisibleAbilitiesCoroutine");
        while (!_isClickForCastInvisibleSkill)
        {
            if (_player.Abilities.SkillQueue.CurrentSkill != null && !(_player.Abilities.SkillQueue.CurrentSkill is CreeperInvisible))
            {
                _isClickForCastInvisibleSkill = true;
                _isClickForExitInvisible = false;
                Debug.Log($"CreeperInvisible / InvisibleAbilitiesCoroutine / isCastAbilitie = {_isClickForCastInvisibleSkill}, isClick = {_isClickForExitInvisible}");
            }
            yield return null;
        }
    }

    #endregion

    #region CommandMethods

    [Command]
    private void CmdReducingTransparencySpritePlayer(GameObject player)
    {
        RpcReducingTransparencySpritePlayer(player);
    }

    [Command]
    private void CmdIncreasingTransparencySpritePlayer(GameObject player)
    {
        RpcIncreasingTransparencySpritePlayer(player);
    }

    [Command]
    private void CmdApplyInvis(GameObject player)
    {
        _isInvisible = true;
       // _player.ChangedBool(false);

        RpcApplyInvis();

        _player.CharacterState.AddState(States.CreeperInvisible, 0, 0, _player.gameObject, Name);
    }

    [Command]
    private void CmdApplyInvisibleWithTalent()
    {
       _isInvisible = true; 

        RpcApplyInvisibleWithTalent();

        _player.CharacterState.AddState(States.CreeperInvisible, 0, 0, _player.gameObject, Name);
    }

    [Command]
    private void CmdRemoveInvisible(bool creeperStrikeIsHit)
    {
        if (_releaseFromSecrecy.Data.IsOpen && creeperStrikeIsHit)
        {
            _releaseFromSecrecy.ApplyBuff();
        }
        // Debug.Log("CreeperInvisible / CmdRemoveInvisible");
        _isInvisible = false;
        _isPlayerSeen = true;
        _isDamagedPlayer = false;

        RpcRemoveInvisible(creeperStrikeIsHit);
    }

    #endregion

    #region RpcMethods

    [ClientRpc]
    private void RpcReducingTransparencySpritePlayer(GameObject player)
    {
        player.GetComponent<Character>().IsInvisible = true;

        SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();

        Color newPlayerSpriteTransparency = playerSprite.color;

        int playerLayer = player.layer;

        if (playerLayer == LayerMask.NameToLayer("Allies"))
        {
            newPlayerSpriteTransparency.a = 0.5f;
            _playerSprite.color = new Color(1f, 1f, 1f, newPlayerSpriteTransparency.a);
        }
        else if (playerLayer == LayerMask.NameToLayer("Enemy"))
        {
            newPlayerSpriteTransparency.a = 0.0f;
            _playerSprite.color = new Color(1f, 1f, 1f, newPlayerSpriteTransparency.a);
        }
    }

    [ClientRpc]
    private void RpcIncreasingTransparencySpritePlayer(GameObject player)
    {
        player.GetComponent<Character>().IsInvisible = false;

        SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();

        Color newPlayerSpriteTransparency = playerSprite.color;

        int playerLayer = player.layer;

        if (playerLayer == LayerMask.NameToLayer("Allies"))
        {
            newPlayerSpriteTransparency.a = 1f;
            _playerSprite.color = new Color(1f, 1f, 1f, newPlayerSpriteTransparency.a);
        }
        else if (playerLayer == LayerMask.NameToLayer("Enemy"))
        {
            newPlayerSpriteTransparency.a = 1f;
            _playerSprite.color = new Color(1f, 1f, 1f, newPlayerSpriteTransparency.a);
        }
    }

    [ClientRpc]
    private void RpcApplyInvis()
    {
        //Debug.Log("CreeperInvisible / RpcApplyInvis");
        _isInvisible = true; 
    }

    [ClientRpc]
    private void RpcApplyInvisibleWithTalent()
    {
        _isInvisible = true;
    }

    [ClientRpc]
    private void RpcRemoveInvisible(bool creeperStrikeIsHit)
    {
        _isInvisible = false;
        if (_releaseFromSecrecy.Data.IsOpen)
        {
            _releaseFromSecrecy.ApplyBuff();
        }

        if (_firstStrike.Data.IsOpen && !_firstStrike.IsCanIncreaseCrit)
        {
            _firstStrike.SetBoolTrue();
        }

        if (_preparingForFight.Data.IsOpen)
        {
            _isReadyToThreeHitForPreparingForFightTalent = true;
        }

        if (_coldBlood.IsCanCritCreeperStrike)
        {
            if (_concentratedPrecision.Data.IsOpen)
            {
                _coldBlood.ReducingAbilityCooldown();
            }
            _coldBlood.IsCanCritCreeperStrike = false;
        }

        #region CancleCoroutines

        if (_checkEnemiesCoroutine != null)
        {
            StopCoroutine(CheckEnemiesAround());
            _checkEnemiesCoroutine = null;
        }
        if (_checkCurrentHealthPlayerWithTimerCoroutine != null)
        {
            StopCoroutine(CheckCurrentHealthPlayerWithTimer());
            _checkCurrentHealthPlayerWithTimerCoroutine = null;
        }
        if (_checkCurrentHealthPlayerWithoutTimerCoroutine != null)
        {
            StopCoroutine(CheckCurrentHealthPlayerWithoutTimer());
            _checkCurrentHealthPlayerWithoutTimerCoroutine = null;
        }
        if (_exitFromInvisibleCoroutine != null)
        {
            StopCoroutine(ExitFromInvisible());
            _exitFromInvisibleCoroutine = null;
        }
        if (_invisibleAbilitiesCoroutine != null)
        {
            StopCoroutine(InvisibleAbilities());
            _invisibleAbilitiesCoroutine = null;
        }

        #endregion

        _isPlayerSeen = true;
        _isDamagedPlayer = false;
        _isClickForCastInvisibleSkill = false;
        _isClickForExitInvisible = false;
    }

    #endregion
}
