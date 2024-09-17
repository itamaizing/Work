using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

public struct PoisonBallInfo : NetworkMessage
{
    public int CountProjectiles;
    public int MaxCountProjectile;

    public float TimeBetweenAttack;
    public float StartTimeBetweenAttack;

    public bool IsProjectileCreate;
    public bool IsActiveTimer;
    public bool IsThreeProjectileOnOnetarget;
    public bool IsCanApplyInvisible;

    public bool IsOriginalTargetEnemy;
    public bool IsOriginalTargetAllies;
    public bool IsOriginalTargetPlayer;

    public bool IsActiveWitheringPoison;
    public bool IsActiveContinuationAmbush; 
    public bool IsActiveHealingPoisonBall;
    public bool IsActiveHealingPoisonCloud;
    public bool IsActiveEnlargedGlands;
    public bool IsActiveVoluminousBall;
    public bool IsActiveInertialGlands;
    public bool IsHealingPoisonCloud;
}

public class PoisonBall : Skill
{
    #region Variables

    [Header("Talents")]
    [SerializeField] private FootInstincts _footInstincts;
    [SerializeField] private HealingPoisonBall _healingPoisonBall;
    [SerializeField] private HealPoisonCloud _healPoisonCloud;
    [SerializeField] private WitheringPoison _witheringPoison;
    [SerializeField] private EnlargedGlands _enlargedGlands;
    [SerializeField] private ContinuationAmbush _continuationAmbush;
    [SerializeField] private VoluminousBall _voluminousBall;
    [SerializeField] private InertialGlands _inertialGlands;
    [SerializeField] private AssasinPoison _assasinPoison;

    [Header("Ability properties")]
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private PoisonCloudDisplay _prefabPoisonDamagingCloud;
    [SerializeField] private PoisonCloudDisplay _prefabPoisonHealingCloud;

    [SerializeField] private PoisonCloudDisplay _poisonHealingCloud;
    [SerializeField] private PoisonCloudDisplay _poisonDamagingCloud;

    [SerializeField] private Character _player;
    [SerializeField] private GameObject _arrowPrefab;

    private GameObject[] _arrowRenderers = new GameObject[4];
    private Character _currentTarget;
    private PoisonBallInfo _poisonBallInfo = new PoisonBallInfo();
    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;
    private Vector3 _thirdMousePosition;

    private float _fastTimeCast = 0.4f;
    private float _slowTimeCast = 1.8f;
    private float _originalChargeCooldown;
    private float _durationPoisonCloud = 6f;
    private float _currentStacksAsssasinPoison = 0;

    #region BoolVariables

    private bool _isPushTarget;
    private bool _isTarget;
    private bool _isFast;
    private bool _secondClickDone = false;
    private bool _thirdClickDone = false;
    private bool _firstClickCompleted = false;
    private bool _colorLockedAfterSecondClick = false;
    private bool _colorLockedAfterThirdClick = false;

    #endregion

    private Coroutine _secondClickCoroutine;
    private Coroutine _thirdClickCoroutine;

    public GameObject LastTarget { get; set; }
    public GameObject CurrentTarget { get; set; }
    public FootInstincts FootInstinctsTalent { get; set; }
    public ContinuationAmbush ContinuationAmbushTalent { get; set; }
    public int CurrentCharges { get => _currentChargers; set => _currentChargers = value; }

    #endregion

    private void Start()
    {
        _originalChargeCooldown = _chargeCooldown;

        _poisonBallInfo.StartTimeBetweenAttack = 3.0f;
        _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        _poisonBallInfo.MaxCountProjectile = _maxCharges;
    }
    
    private void Update()
    {
        UpdateMouseDetection();
        if (_poisonBallInfo.IsActiveTimer)
        {
            Timer();
        }
        CheckingActiveTalents();
    }

    #region PrepareAndStartJob

    protected override bool IsCanCast => CheckCanCast();

    public void PayCostPoisonBall()
    {
        TryPayCost();
    }

    protected override void ClearData()
    {
        _isTarget = false;
        _secondClickDone = false;
        _thirdClickDone = false;

        _currentTarget = null;

        _firstMousePosition = Vector3.positiveInfinity;
        _secondMousePosition = Vector3.zero;
        _thirdMousePosition = Vector3.zero;

        ClearArrows();

        _firstClickCompleted = false;
        _colorLockedAfterSecondClick = false;
        _colorLockedAfterThirdClick = false;

        if (_secondClickCoroutine != null)
        {
            StopCoroutine(_secondClickCoroutine);
            _secondClickCoroutine = null;
        }
        if (_thirdClickCoroutine != null)
        {
            StopCoroutine(_thirdClickCoroutine);
            _thirdClickCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        CheckingActiveTalents();

        while (_currentTarget == null && float.IsPositiveInfinity(_firstMousePosition.x))
        {
            if (Input.GetMouseButtonDown(0))
            {
                _currentTarget = GetRaycastTarget(true);
                CheckWhoTarget();
                _firstMousePosition = GetMousePoint();
                if (_currentTarget != null)
                {
                    CreateArrowsParallelToPlayer();
                    StopAutoDraw();
                }
                _firstClickCompleted = true;

            }
            CooldownChange();
            yield return null;
        }

        yield return _secondClickCoroutine = StartCoroutine(SecondClick());
        yield return _thirdClickCoroutine = StartCoroutine(ThirdClick());

        UseAbility();
    }

    protected override IEnumerator CastJob()
    {
        ChooseWhichProjectileCreate();

        ClearArrows();

        yield return null;
    }

    private void UseAbility()
    {
        if (_secondClickDone && _thirdClickDone)
        {
            _isTarget = IsTarget();
            ChooseMovementDependingOnCountProjectiles();
        }
    }

    #endregion

    #region ArrowManagement

    private void CreateArrowsParallelToPlayer()
    {
        if (_currentTarget == null || _arrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned or Target is null");
            return;
        }

        Vector3 targetPosition = _currentTarget.transform.position;
        Vector3 playerPosition = _player.transform.position;

        Vector3 directionToTarget = (targetPosition - playerPosition).normalized;

        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.forward).normalized;

        _arrowRenderers[0] = Instantiate(_arrowPrefab, targetPosition + directionToTarget, Quaternion.identity);
        _arrowRenderers[1] = Instantiate(_arrowPrefab, targetPosition - directionToTarget, Quaternion.identity);
        _arrowRenderers[2] = Instantiate(_arrowPrefab, targetPosition + directionToTarget * 1.5f, Quaternion.identity);
        _arrowRenderers[3] = Instantiate(_arrowPrefab, targetPosition - directionToTarget * 1.5f, Quaternion.identity);

        SetArrowDirections(perpendicularDirection);
        SetArrowColors(Color.red);
    }

    private void SetArrowDirections(Vector3 perpendicularDirection)
    {
        for (int i = 0; i < _arrowRenderers.Length; i++)
        {
            var arrow = _arrowRenderers[i];
            if (arrow != null)
            {
                var drawArrow = arrow.GetComponent<DrawArrow>();
                if (drawArrow != null)
                {
                    Vector3 startPoint = arrow.transform.position - perpendicularDirection * 0.5f;
                    Vector3 endPoint = arrow.transform.position + perpendicularDirection * 0.5f;

                    if (i % 2 == 0)
                    {
                        drawArrow.DrawCurvedArrow(startPoint, endPoint, true);
                    }
                    else
                    {
                        drawArrow.DrawCurvedArrow(startPoint, endPoint, false);
                    }
                }
            }
        }
    }

    private void SetArrowColors(Color color)
    {
        foreach (var arrow in _arrowRenderers)
        {
            if (arrow != null)
            {
                var lineRenderer = arrow.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.startColor = color;
                    lineRenderer.endColor = color;
                }
            }
        }
    }

    private void SetArrowColor(int arrowIndex, Color color)
    {
        if (_arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = _arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }
    }

    private void DarkenArrowColor(int arrowIndex, float alpha)
    {
        if (_arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = _arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Color startColor = lineRenderer.startColor;
                Color endColor = lineRenderer.endColor;

                startColor.a = alpha;
                endColor.a = alpha;

                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;
            }
        }
    }

    private void ClearArrows()
    {
        foreach (var arrow in _arrowRenderers)
        {
            if (arrow != null)
            {
                Destroy(arrow);
            }
        }
    }

    #endregion

    #region MouseClick

    private IEnumerator SecondClick()
    {
        while (!_secondClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _secondClickDone = true;
                _secondMousePosition = GetMousePoint();
                if (_currentTarget != null)
                {
                    if (_secondMousePosition.x < _firstMousePosition.x)
                    {
                        DarkenArrowColor(0, 0.8f);
                        DarkenArrowColor(1, 0f);
                    }
                    else
                    {
                        DarkenArrowColor(0, 0f);
                        DarkenArrowColor(1, 0.8f);
                    }
                }
            }
            yield return null;
        }
    }

    private IEnumerator ThirdClick()
    {
        while (!_thirdClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _thirdClickDone = true;
                _thirdMousePosition = GetMousePoint();

                if (_currentTarget != null)
                {
                    if (_thirdMousePosition.x < _secondMousePosition.x)
                    {
                        DarkenArrowColor(2, 0.8f);
                        DarkenArrowColor(3, 0f);
                    }
                    else
                    {
                        DarkenArrowColor(2, 0f);
                        DarkenArrowColor(3, 0.8f);
                    }
                }
                _colorLockedAfterThirdClick = true;
            }
            yield return null;
        }
    }

    #endregion

    #region Update Method for Mouse Movement Detection

    private void UpdateMouseDetection()
    {
        if (_firstClickCompleted && !_secondClickDone)
        {
            Vector3 currentMousePosition = GetMousePoint();
            if (currentMousePosition.x < _firstMousePosition.x)
            {
                SetArrowColor(0, Color.green);
                SetArrowColor(1, Color.red);
            }
            else
            {
                SetArrowColor(0, Color.red);
                SetArrowColor(1, Color.green);
            }
        }

        if (_secondClickDone && !_colorLockedAfterSecondClick && !_colorLockedAfterThirdClick)
        {
            Vector3 currentMousePosition = GetMousePoint();
            if (currentMousePosition.x < _secondMousePosition.x)
            {
                SetArrowColor(2, Color.green);
                SetArrowColor(3, Color.red);
            }
            else
            {
                SetArrowColor(2, Color.red);
                SetArrowColor(3, Color.green);
            }
        }
    }

    #endregion

    #region CheckingMethods

    private void CheckingActiveTalents()
    {
        if (_assasinPoison.IsActive)
        {
            _currentStacksAsssasinPoison = _assasinPoison.CurrentChargeAssasinPoison;
            Debug.Log("PoisonBall / CurrentStacksAssasinPoison == " + _currentStacksAsssasinPoison);
            for (int i = 0; i < _currentStacksAsssasinPoison; i++)
            {
                Debug.Log("CycleFor");
                if (CurrentCharges < _maxCharges)
                {
                    _currentStacksAsssasinPoison--;
                    Debug.Log("PoisonBall / CurrentStacksAssasinPoison == " + _currentStacksAsssasinPoison);
                    Debug.Log("PoisonBall / _chargeCooldown == " + _chargeCooldown);
                    float newCooldownTime = _chargeCooldown * 0;
                    Debug.Log("PoisonBall / newCooldownTime == " + newCooldownTime);
                    this.IncreaseSetCooldown(newCooldownTime);
                }
            }
        }

        _poisonBallInfo.IsActiveWitheringPoison = _witheringPoison.IsActive;
        _poisonBallInfo.IsActiveContinuationAmbush = _continuationAmbush.IsActive;
        _poisonBallInfo.IsActiveHealingPoisonBall = _healingPoisonBall.IsActive;
        _poisonBallInfo.IsActiveHealingPoisonCloud = _healPoisonCloud.IsActive;
        _poisonBallInfo.IsActiveEnlargedGlands = _enlargedGlands.IsActive;
        _poisonBallInfo.IsActiveVoluminousBall = _voluminousBall.IsActive;
        _poisonBallInfo.IsActiveInertialGlands = _inertialGlands.IsActive;

        #region EnlargedGlandTalentIsActive

        if (_poisonBallInfo.IsActiveEnlargedGlands && _maxCharges == 3)
        {
            AddMaxChargeCount();
            _poisonBallInfo.MaxCountProjectile = _maxCharges;
        }
        else if (!_poisonBallInfo.IsActiveEnlargedGlands && _maxCharges >= 4)
        {
            DeductMaxChargeCount();
            _poisonBallInfo.MaxCountProjectile = _maxCharges;
        }

        #endregion

        #region InertialGlandTalentIsActive

        if (_poisonBallInfo.IsActiveInertialGlands && _poisonBallInfo.IsThreeProjectileOnOnetarget)
        {
            float newRemainingTime = 0.0f;
            _spitPoison.ReductionSetCooldown(newRemainingTime);
        }

        #endregion

        #region ContinuationAmbushTalentIsActive

        if (_poisonBallInfo.IsActiveContinuationAmbush && _poisonBallInfo.IsCanApplyInvisible)
        {
            _continuationAmbush.CanApplyInvisible(true);
        }

        #endregion
    }

    private void CheckWhoTarget()
    {
        if (_currentTarget != null)
        {
            if (_currentTarget.gameObject == _player.gameObject)
            {
                Debug.Log("Target == Player");
                _poisonBallInfo.IsOriginalTargetPlayer = true;
                _poisonBallInfo.IsOriginalTargetAllies = false;
                _poisonBallInfo.IsOriginalTargetEnemy = false;

                if (_poisonBallInfo.IsActiveHealingPoisonCloud)
                {
                    _poisonBallInfo.IsHealingPoisonCloud = true;
                }
                else
                {
                    _poisonBallInfo.IsHealingPoisonCloud = false;
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                Debug.Log("Target == Allies");
                _poisonBallInfo.IsOriginalTargetPlayer = false;
                _poisonBallInfo.IsOriginalTargetAllies = true;
                _poisonBallInfo.IsOriginalTargetEnemy = false;

                if (_poisonBallInfo.IsActiveHealingPoisonCloud)
                {
                    _poisonBallInfo.IsHealingPoisonCloud = true;
                }
                else
                {
                    _poisonBallInfo.IsHealingPoisonCloud = false;
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Debug.Log("Target == Enemies");
                _poisonBallInfo.IsOriginalTargetPlayer = false;
                _poisonBallInfo.IsOriginalTargetAllies = false;
                _poisonBallInfo.IsOriginalTargetEnemy = true;
            }
        }
        else
        {
            _poisonBallInfo.IsOriginalTargetPlayer = false;
            _poisonBallInfo.IsOriginalTargetAllies = false;
            _poisonBallInfo.IsOriginalTargetEnemy = false;
        }
    }

    private void Timer()
    {
        _poisonBallInfo.TimeBetweenAttack -= Time.deltaTime;

        if (_poisonBallInfo.TimeBetweenAttack < 0)
        {
            _poisonBallInfo.CountProjectiles = 0;
            _poisonBallInfo.IsActiveTimer = false;
            _poisonBallInfo.IsProjectileCreate = false;
        }
        
    }

    private void CooldownChange()
    {
        if (_poisonBallInfo.IsActiveHealingPoisonBall && (_poisonBallInfo.IsOriginalTargetAllies || _poisonBallInfo.IsOriginalTargetPlayer))
        {
            _chargeCooldown = _originalChargeCooldown / 2;
        }
        else
        {
            _chargeCooldown = _originalChargeCooldown;
        }
    }

    #endregion

    #region BooleanMethods

    private bool CheckCanCast()
    {
        //Debug.Log("CheckCanCast PoisonBall");

        if (_currentTarget == null)
            return Vector3.Distance(_firstMousePosition, transform.position) <= Radius;

        return Vector3.Distance(_firstMousePosition, transform.position) <= Radius ||
               Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius;
    }

    private bool IsTarget()
    {
        return _currentTarget != null;
    }

    #endregion

    #region ChooseMoveSpeedProjectile

    private void ChooseMovementDependingOnCountProjectiles()
    {
        ChooseSpeed();
        ChooseDirectionPush();
        StartCoroutine(_isFast ? TimeCastForFastMoveProjectile() : TimeCastForSlowMoveProjectile());
    }

    private void ChooseSpeed()
    {
        _isFast = _isTarget
            ? Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _currentTarget.transform.position)
            : Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _firstMousePosition);
    }

    private void ChooseDirectionPush()
    {
        _isPushTarget = Vector2.Distance(_player.transform.position, _thirdMousePosition) > Vector2.Distance(_player.transform.position, _secondMousePosition);
    }

    private IEnumerator TimeCastForFastMoveProjectile()
    {
        _castDeley = _slowTimeCast;
        yield return null;
    }

    private IEnumerator TimeCastForSlowMoveProjectile()
    {
        _castDeley = _fastTimeCast;
        yield return null;
    }

    private void ChooseWhichProjectileCreate()
    {
        if (_isTarget)
        {
            CmdCreateProjectileForTarget(_currentTarget.gameObject, _currentTarget.transform.position, _poisonBallInfo.MaxCountProjectile, 
                _isFast, _isPushTarget,
                _poisonBallInfo.IsActiveHealingPoisonBall, _poisonBallInfo.IsActiveWitheringPoison, _poisonBallInfo.IsActiveVoluminousBall,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies);

            CmdApplyCloudPoison(_poisonBallInfo.IsHealingPoisonCloud);
        }
        else
        {
            CmdCreateProjectileForFlyingMaxDistance(_firstMousePosition, _poisonBallInfo.MaxCountProjectile, 
                _isFast, _isPushTarget,
                _poisonBallInfo.IsActiveHealingPoisonBall, _poisonBallInfo.IsActiveWitheringPoison, _poisonBallInfo.IsActiveVoluminousBall,
                _poisonBallInfo.IsOriginalTargetEnemy, _poisonBallInfo.IsOriginalTargetPlayer, _poisonBallInfo.IsOriginalTargetAllies);

            CmdApplyCloudPoison(_poisonBallInfo.IsHealingPoisonCloud);
        }
    }

    #endregion

    #region Command Methods

    [Command]
    private void CmdCreateProjectileForTarget(GameObject target, Vector3 targetOrPoint, int maxCountProjectiles,
        bool isFast, bool isPushTarget,
        bool isActiveHealingPoisonBall, bool isActiveWitheringPoison, bool isActiveVoluminousBall,
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies)
        
    { 
        CurrentTarget = target;
        FootInstinctsTalent = _footInstincts;

        if (LastTarget == CurrentTarget)
        {
            _poisonBallInfo.CountProjectiles += 1;
            _poisonBallInfo.IsProjectileCreate = true;
        }
        else
        {
            _poisonBallInfo.IsActiveTimer = false;
            _poisonBallInfo.IsThreeProjectileOnOnetarget = false;
            _poisonBallInfo.IsCanApplyInvisible = false;
            _poisonBallInfo.CountProjectiles = 1;
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        }

        if (_poisonBallInfo.CountProjectiles >= 3 && _poisonBallInfo.IsActiveInertialGlands)
        {
            _poisonBallInfo.IsThreeProjectileOnOnetarget = true;
        }

        if (_poisonBallInfo.CountProjectiles >= 4 && _poisonBallInfo.IsActiveContinuationAmbush)
        {
            _poisonBallInfo.IsCanApplyInvisible = true;
        }

        if (_poisonBallInfo.CountProjectiles < maxCountProjectiles && LastTarget == CurrentTarget)
        {
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
            _poisonBallInfo.IsActiveTimer = true;
        }
        else if (_poisonBallInfo.CountProjectiles >= maxCountProjectiles)
        {
            _poisonBallInfo.IsActiveTimer = false;

            _poisonBallInfo.CountProjectiles = 0; 
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        }

        //Debug.Log($"bool isTargetEnemy = {isTargetEnemy}, bool isTargetPlayer = {isTargetPlayer}, bool isTargetAllies = {isTargetAllies}");

       // Debug.Log($"_poisonBallInfo.HealingBall = {_poisonBallInfo.IsActiveHealingPoisonBall}");

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>(); 

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.CurrentValue, this, isActiveHealingPoisonBall,
            isTargetPlayer, isTargetEnemy, isTargetAllies, isActiveWitheringPoison, isPushTarget, isActiveVoluminousBall);

        poisonBallProjectile.MoveBallToTarget(targetOrPoint, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdCreateProjectileForFlyingMaxDistance(Vector3 point, int maxCountProjectiles,
        bool isFast, bool isPushTarget,
        bool isActiveHealingPoisonBall, bool isActiveWitheringPoison, bool isActiveVoluminousBall,
        bool isTargetEnemy, bool isTargetPlayer, bool isTargetAllies)
    {
        FootInstinctsTalent = _footInstincts;
        CurrentTarget = LastTarget;

        if (LastTarget == CurrentTarget)
        {
            _poisonBallInfo.CountProjectiles += 1;
            _poisonBallInfo.IsProjectileCreate = true;
        }
        else
        {
            _poisonBallInfo.IsActiveTimer = false;
            _poisonBallInfo.IsThreeProjectileOnOnetarget = false;
            _poisonBallInfo.IsCanApplyInvisible = false;
            _poisonBallInfo.CountProjectiles = 1;
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        }

        if (_poisonBallInfo.CountProjectiles >= 3 && _poisonBallInfo.IsActiveInertialGlands)
        {
            _poisonBallInfo.IsThreeProjectileOnOnetarget = true;
        }

        if (_poisonBallInfo.CountProjectiles >= 4 && _poisonBallInfo.IsActiveContinuationAmbush)
        {
            _poisonBallInfo.IsCanApplyInvisible = true;
        }

        if (_poisonBallInfo.CountProjectiles < maxCountProjectiles && LastTarget == CurrentTarget)
        {
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
            _poisonBallInfo.IsActiveTimer = true;
        }
        else if (_poisonBallInfo.CountProjectiles >= maxCountProjectiles)
        {
            _poisonBallInfo.IsActiveTimer = false;

            _poisonBallInfo.CountProjectiles = 0;
            _poisonBallInfo.TimeBetweenAttack = _poisonBallInfo.StartTimeBetweenAttack;
        }

        //Debug.Log($"bool isTargetEnemy = {isTargetEnemy}, bool isTargetPlayer = {isTargetPlayer}, bool isTargetAllies = {isTargetAllies}");

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);
        PoisonBallProjectile poisonBallProjectile = item.GetComponent<PoisonBallProjectile>(); 

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        poisonBallProjectile.InitializationProjectileForPoisonBall(_player, _player.Stamina.CurrentValue, this, isActiveHealingPoisonBall,
            isTargetPlayer, isTargetEnemy, isTargetAllies, isActiveWitheringPoison, isPushTarget, isActiveVoluminousBall);

        poisonBallProjectile.MoveBallOnMaxDistance(point, isFast);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdApplyCloudPoison(bool isHealingCloud)
    {
        Debug.Log($"CmdApplyCloudPoison / isHealingCloud = {isHealingCloud}");
        if (isHealingCloud)
        {
            Debug.Log("CmdHealing Cloud");
            if (_poisonHealingCloud.PoisonHealingCloud == null)
            {
             //   Debug.Log("Healing Cloud / if");
                _player.CharacterState.AddState(States.HealingPoisonCloud, _durationPoisonCloud, 0, _player.gameObject, Name);

                _poisonHealingCloud.PoisonHealingCloud = Instantiate(_prefabPoisonHealingCloud, transform.position, Quaternion.identity);

                SceneManager.MoveGameObjectToScene(_poisonHealingCloud.PoisonHealingCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonHealingCloud.PoisonHealingCloud.InitializationPrefab(_player, 6f, 3.5f, 5, isHealingCloud);
                _poisonHealingCloud.PoisonHealingCloud.AddStack();

                NetworkServer.Spawn(_poisonHealingCloud.PoisonHealingCloud.gameObject);
            }
            else
            {
                Debug.Log("CmdHealing Cloud / else");
                _player.CharacterState.AddState(States.HealingPoisonCloud, _durationPoisonCloud, 0, _player.gameObject, Name);
                _poisonHealingCloud.PoisonHealingCloud.AddStack();
            }
        }
        else
        {
            Debug.Log("CmdDamaging Cloud");
            if (_poisonDamagingCloud.PoisonDamagingCloud == null)
            {
              //  Debug.Log("Damaging Cloud / if");
                _player.CharacterState.AddState(States.PoisonCloud, _durationPoisonCloud, 0, _player.gameObject, Name);

                _poisonDamagingCloud.PoisonDamagingCloud = Instantiate(_prefabPoisonDamagingCloud, transform.position, Quaternion.identity);

                SceneManager.MoveGameObjectToScene(_poisonDamagingCloud.PoisonDamagingCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonDamagingCloud.PoisonDamagingCloud.InitializationPrefab(_player, 6f, 3.5f, 5, isHealingCloud);
                _poisonDamagingCloud.PoisonDamagingCloud.AddStack();

                NetworkServer.Spawn(_poisonDamagingCloud.PoisonDamagingCloud.gameObject);
            }
            else
            {
                Debug.Log("CmdDamaging Cloud / else");
                _player.CharacterState.AddState(States.PoisonCloud, _durationPoisonCloud, 0, _player.gameObject, Name);
                //Debug.Log("CmdPoisonDamagingCloud / Else / AddStack");
                _poisonDamagingCloud.PoisonDamagingCloud.AddStack();
            }
        }
        RpcApplyCloudPoisons(_poisonDamagingCloud, _poisonHealingCloud, isHealingCloud);
    }

    #endregion

    [ClientRpc]
    private void RpcApplyCloudPoisons(PoisonCloudDisplay damageCloud, PoisonCloudDisplay healCloud, bool isHealingCloud)
    {
        if (damageCloud != null)
        {
            damageCloud.InitializationPrefab(_player, 7f, 3.5f, 5, isHealingCloud);
            damageCloud.AddStack();
            Debug.Log("RpcPoisonDamagingCloud / if dmgCloud / AddStack");
        }
        if (healCloud != null)
        {
            healCloud.InitializationPrefab(_player, 7f, 3.5f, 5, isHealingCloud);
            healCloud.AddStack();
            Debug.Log("RpcPoisonHealingCloud / if healCloud / AddStack");
        }
    }
}
