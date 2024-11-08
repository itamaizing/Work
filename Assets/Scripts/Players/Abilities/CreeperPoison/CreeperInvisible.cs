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

    [Header("Invisible Abilities")]
    [SerializeField] private List<Skill> _altAbilities = new();

    [Header("Ability")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private ColdBlood _coldBlood;

    [Header("Ability Properties")]
    [SerializeField] private Character _player;
    [SerializeField] private SpriteRenderer _playerSprite;

    private SpitPoison _spitPoison;
    private PoisonBall _poisonBall;

    private float _maxHealth;
    private float _currentHealth;
    private float _distanceWithoutEnemies = 6f;

    private bool _isInvisible = false;
    private bool _isPlayerSeen = true;
    private bool _isDamagedPlayer = false;
    private bool _isReadyToThreeHitForPreparingForFightTalent = false;
    private bool _isCanExitInvisible = false;
    private bool _isCreeperStrikeIsHit;
    private bool _isEnemy;

    private bool _isCanResetBools;

    private Coroutine _checkEnemiesCoroutine;
    private Coroutine _exitFromInvisibleCoroutine;

    public bool IsReadyToThreeHitForPreparingForFightTalent { get => _isReadyToThreeHitForPreparingForFightTalent; set => _isReadyToThreeHitForPreparingForFightTalent = value; }
    
    public bool IsInvisible { get => _isInvisible; set => _isInvisible = value; }

    protected override bool IsCanCast => _isPlayerSeen == false && _isDamagedPlayer == false;

    #endregion

    #region PrepareAndCastJob

    protected override void ClearData()
    {
    }

    protected override IEnumerator PrepareJob()
    {
        ResetAltAbility();

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
                if (_continuationAmbush.Data.IsOpen && _continuationAmbush.IsCanApplyInvisible)
                {
                    CmdApplyInvisibleWithTalent(); 
                    CmdReducingTransparencySpritePlayer(_player.gameObject);
                    yield break;
                }

                if (_checkEnemiesCoroutine == null)
                {
                    yield return _checkEnemiesCoroutine = StartCoroutine(CheckEnemiesAround());
                }
                break;

            case true:

                _exitFromInvisibleCoroutine = StartCoroutine(ExitFromInvisible());

                break;

            default:
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_isInvisible && _transparentPoisons.Data.IsOpen)
        {
            if (_altAbilities != null)
            {
                foreach (IAltAbility altAbility in _altAbilities)
                {
                    if (altAbility is SpitPoison spitPoison)
                    {
                        _spitPoison = spitPoison;
                        _spitPoison.IsAltAbility = true;
                        _spitPoison.ResetAbilityParameters += OnResetSpitPoison;
                    }
                    if (altAbility is PoisonBall poisonBall)
                    {
                        _poisonBall = poisonBall;
                        _poisonBall.IsAltAbility = true;
                        _poisonBall.ResetAbilityParameters += OnResetPoisonBall;
                    }
                }
            }
            _transparentPoisons.IncreaseManaCost(_isInvisible);
        }
        else if (!_isInvisible)
        {
            EnteringInvisibleState();
        }
        yield return null;
    }

    private void OnResetPoisonBall()
    {
        _poisonBall.IsAltAbility = false;
        _poisonBall.ResetAbilityParameters -= OnResetPoisonBall;
    }   

    private void OnResetSpitPoison()
    {
        _spitPoison.IsAltAbility = false;
        _spitPoison.ResetAbilityParameters -= OnResetPoisonBall;
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

    private void Update()
    {
        CheckCurrentHealthPlayer();
    }

    private void ResetAltAbility()
    {
        if (_spitPoison != null && _poisonBall != null)
        {
            _spitPoison.IsAltAbility = false;
            _poisonBall.IsAltAbility = false;
        }
    }

    private void CheckCurrentHealthPlayer()
    {
        _currentHealth = _player.Health.CurrentValue;

        if (_currentHealth < _maxHealth)
        {
            ExitingInvisibleState();
            return;
        }
    }

    #region Coroutines

    private IEnumerator CheckEnemiesAround()
    {
        while (_isPlayerSeen)
        {
            _isEnemy = false;
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(_player.transform.position, _distanceWithoutEnemies, _targetsLayers);
            Debug.Log("hitEnemies = " + hitEnemies.Length);
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy != null)
                {
                    Debug.Log("Enemy = " + enemy.name);
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

            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator ExitFromInvisible()
    {
        while (!_isCanExitInvisible)
        {
            if (Input.GetMouseButton(2))
            {
                _isCanExitInvisible = true;

                if (_isInvisible && _isCanExitInvisible)
                {
                    CmdRemoveInvisible(_isCreeperStrikeIsHit);
                    CmdIncreasingTransparencySpritePlayer(_player.gameObject);
                }

                yield break;
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
        if (_exitFromInvisibleCoroutine != null)
        {
            StopCoroutine(ExitFromInvisible());
            _exitFromInvisibleCoroutine = null;
        }

        #endregion

        _isPlayerSeen = true;
        _isDamagedPlayer = false;
        _isCanExitInvisible = false;
    }

    #endregion
}
