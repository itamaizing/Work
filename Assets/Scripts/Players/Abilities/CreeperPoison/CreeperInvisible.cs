using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CreeperInvisible : Skill
{
    #region Variables

    public bool IsInvisible = false;
    public bool IsReadyToThreeHitForPreparingForFightTalent = false;

    [Header("Talents")]
    [SerializeField] private ReleaseFromSecrecy _releaseFromSecrecy;
    [SerializeField] private DesireToHide _desireToHide;
    [SerializeField] private FirstStrike _firstStrike;
    [SerializeField] private ContinuationAmbush _continuationAmbush;
    [SerializeField] private TransparentPoisons _transparentPoisons;
    [SerializeField] private PreparingForFight _preparingForFight;
    [SerializeField] private ConcentratedPrecision _concentratedPrecision;

    [Header("Ability Properties")]
    [SerializeField] private AbsoluteAccuracy _absoluteAccuracy;
    [SerializeField] private Character _player;
    [SerializeField] private SpriteRenderer _playerSprite;
    [SerializeField] private SpriteRenderer _selectCircle;

    private float _maxHealth;
    private float _currentHealth;
    private float _timeWithoutDamage = 6.0f;
    private float _distanceWithoutEnemies = 6f;

    private bool _isCanCast;
    private bool _isEnemy;
    private bool _isPlayerSeen = true;
    private bool _isDamagedPlayer = false;

    private bool _isClickForExitInvisible = false;
    private bool _isClickForCastInvisibleSkill = false;

    private Coroutine _checkEnemiesCoroutine;
    private Coroutine _checkCurrentHealthPlayerCoroutine;
    private Coroutine _exitFromInvisibleCoroutine;
    private Coroutine _invisibleAbilitiesCoroutine;


    protected override bool IsCanCast => _isCanCast;

    #endregion

    #region PrepareAndCastJob

    protected override void ClearData()
    {
    }

    protected override IEnumerator PrepareJob()
    {
        _maxHealth = _player.Health.CurrentValue;
        switch (IsInvisible)
        {
            case false:
                if (_desireToHide.IsActive && _desireToHide.IsCanApply)
                {
                    CmdApplyInvisibleWithTalent(); 
                    CmdReducingTransparencySpritePlayer(_player.gameObject);
                    yield break;
                }
                else if (_continuationAmbush.IsActive && _continuationAmbush.IsCanApplyInvisible)
                {
                    CmdApplyInvisibleWithTalent(); 
                    CmdReducingTransparencySpritePlayer(_player.gameObject);
                    yield break;
                }

                _checkCurrentHealthPlayerCoroutine = StartCoroutine(CheckCurrentHealthPlayer());
                yield return _checkEnemiesCoroutine = StartCoroutine(CheckEnemiesAround());

                if (!_isPlayerSeen && !_isDamagedPlayer && !IsInvisible)
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
        if (IsInvisible && _isClickForExitInvisible)
        {
            CmdRemoveInvisible();
            CmdIncreasingTransparencySpritePlayer(_player.gameObject);

            yield break;
        }
        else if (IsInvisible)
        {
            //Метод для того, чтобы сделать способности невидымим
            if (_transparentPoisons.IsActive)
            {
                _transparentPoisons.IncreaseManaCost();
            }
            yield break;
        }
        else if (!IsInvisible)
        {
            EnteringInvisibleState();
        }
        yield return null;
    }

    public void EnteringInvisibleState()
    {
        CmdApplyInvis(_player.gameObject);
        CmdReducingTransparencySpritePlayer(_player.gameObject);
    }

    public void ExitingInvisibleState()
    {
        CmdRemoveInvisible();
        CmdIncreasingTransparencySpritePlayer(_player.gameObject);
    }

    #endregion

    #region Coroutines

    private IEnumerator CheckEnemiesAround()
    {
        while (_isPlayerSeen)
        {
            _isEnemy = false;
            Debug.Log($"CreeperInvisible / CheckEnemies");
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(_player.transform.position, _distanceWithoutEnemies, _targetsLayers);
            foreach (Collider2D enemy in hitEnemies)
            {
                Debug.Log($"CreeperInvisible / CheckEnemies / foreach cycle / enemy = {enemy.name}");
                if (enemy != null && enemy.CompareTag("Enemies"))
                {
                    _isEnemy = true;
                    Debug.Log($"CreeperInvisible / CheckEnemies / in FOR cycle / isEnemy = {_isEnemy}");
                    break;
                }
            }

            if (!_isEnemy)
            {
                _isPlayerSeen = false;
                Debug.Log($"CreeperInvisible / CheckEnemies / if (!isEnemy) = {_isEnemy} / isPlayerSeen = {_isPlayerSeen}");
            }
            else
            {
                _isPlayerSeen = true;
                Debug.Log($"CreeperInvisible / CheckEnemies / else (isEnemy) = {_isEnemy} / isPlayerSeen = {_isPlayerSeen}");
            }

            hitEnemies = null;
            yield return null;
        }
    }

    private IEnumerator CheckCurrentHealthPlayer()
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

    private IEnumerator ExitFromInvisible()
    {
        Debug.Log("CreeperInvisible / ExitFromInvisibleCoroutine");
        while (!_isClickForExitInvisible)
        {
            if (Input.GetMouseButtonDown(0))
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
        int teamIndex = player.GetComponentInParent<UserNetworkSettings>().TeamIndex;
        RpcReducingTransparencySpritePlayer(player, teamIndex);
    }

    [Command]
    private void CmdIncreasingTransparencySpritePlayer(GameObject player)
    {
        int teamIndex = player.GetComponentInParent<UserNetworkSettings>().TeamIndex;
        RpcIncreasingTransparencySpritePlayer(player, teamIndex);
    }
    [Command]
    private void CmdApplyInvis(GameObject player)
    {
        //RpcSpriteRenderer(player);
        //Debug.Log("CreeperInvisible / CmdApplyInvis");
        IsInvisible = true;
        //Debug.Log($"CreeperInvisible / CmdApplyInvis / IsInvisible = {IsInvisible}");
        RpcApplyInvis();

        _player.CharacterState.AddState(States.CreeperInvisible, 0, 0, _player.gameObject, Name);
    }

    [Command]
    private void CmdApplyInvisibleWithTalent()
    {
       // Debug.Log("CreeperInvisible / CmdApplyInvisibleWithTalent");
        IsInvisible = true;
        RpcApplyInvisibleWithTalent();

        _player.CharacterState.AddState(States.CreeperInvisible, 0, 0, _player.gameObject, Name);
    }

    [Command]
    private void CmdRemoveInvisible()
    {
       // Debug.Log("CreeperInvisible / CmdRemoveInvisible");
        IsInvisible = false;
        if (_releaseFromSecrecy.IsActive)
        {
            _releaseFromSecrecy.ApplyBuff();
        }
        //Debug.Log($"CreeperInvisible / CmdRemoveInvisible / IsInvisible = {IsInvisible}");

        if (_checkEnemiesCoroutine != null)
        {
            StopCoroutine(CheckEnemiesAround());
            _checkEnemiesCoroutine = null;
        }
        if (_checkCurrentHealthPlayerCoroutine != null)
        {
            StopCoroutine(CheckCurrentHealthPlayer());
            _checkCurrentHealthPlayerCoroutine = null;
        }

        _isPlayerSeen = true;
        _isDamagedPlayer = false;

        RpcRemoveInvisible();
    }

    #endregion

    #region RpcMethods

    [ClientRpc]
    private void RpcReducingTransparencySpritePlayer(GameObject player, int teamIndex)
    {
        player.GetComponent<Character>().OnPlayerEnterInvisible();

        SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();

        Color newPlayerSpriteTransparency = playerSprite.color;

        var localPlayer = NetworkClient.connection.identity.GetComponent<UserNetworkSettings>();
        bool isAlly = localPlayer.TeamIndex == teamIndex;
        
        if (isAlly)
        {
            newPlayerSpriteTransparency.a = 0.5f;
            _playerSprite.color = new Color(1f, 1f, 1f, newPlayerSpriteTransparency.a);
        }
        else
        {
            newPlayerSpriteTransparency.a = 0.0f;
            _playerSprite.color = new Color(1f, 1f, 1f, newPlayerSpriteTransparency.a);
        }
    }

    [ClientRpc]
    private void RpcIncreasingTransparencySpritePlayer(GameObject player, int teamIndex)
    {
        player.GetComponent<Character>().OnPlayerExitInvisible();

        SpriteRenderer playerSprite = player.GetComponentInChildren<SpriteRenderer>();

        Color newPlayerSpriteTransparency = playerSprite.color;

        var localPlayer = NetworkClient.connection.identity.GetComponent<UserNetworkSettings>();
        bool isAlly = localPlayer.TeamIndex == teamIndex;

        if (isAlly)
        {
            newPlayerSpriteTransparency.a = 1f;
            _playerSprite.color = new Color(1f, 1f, 1f, newPlayerSpriteTransparency.a);
        }
        else
        {
            newPlayerSpriteTransparency.a = 1f;
            _playerSprite.color = new Color(1f, 1f, 1f, newPlayerSpriteTransparency.a);
        }
    }

    [ClientRpc]
    private void RpcApplyInvis()
    {
        //Debug.Log("CreeperInvisible / RpcApplyInvis");
        IsInvisible = true;
        //Debug.Log($"CreeperInvisible / RpcApplyInvis / IsInvisible = {IsInvisible}");    
    }

    [ClientRpc]
    private void RpcApplyInvisibleWithTalent()
    {
        IsInvisible = true;
    }

    [ClientRpc]
    private void RpcRemoveInvisible()
    {
        IsInvisible = false;

        if (_releaseFromSecrecy.IsActive)
        {
            _releaseFromSecrecy.ApplyBuff();
        }

        if (_firstStrike.IsActive && !_firstStrike.IsCanIncreaseCrit)
        {
            _firstStrike.SetBoolTrue();
        }

        if (_preparingForFight.IsActive)
        {
            IsReadyToThreeHitForPreparingForFightTalent = true;
        }

        if (_absoluteAccuracy.IsCanCritCreeperStrike)
        {
            if (_concentratedPrecision.IsActive)
            {
                float newCooldownTime = _absoluteAccuracy.CooldownTime / _absoluteAccuracy.DecreaseCooldownTime;

                _absoluteAccuracy.IncreaseSetCooldown(newCooldownTime);
            }

            _absoluteAccuracy.IsCanCritCreeperStrike = false;
        }

        #region CancleCoroutines

        if (_checkEnemiesCoroutine != null)
        {
            StopCoroutine(CheckEnemiesAround());
            _checkEnemiesCoroutine = null;
        }
        if (_checkCurrentHealthPlayerCoroutine != null)
        {
            StopCoroutine(CheckCurrentHealthPlayer());
            _checkCurrentHealthPlayerCoroutine = null;
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
