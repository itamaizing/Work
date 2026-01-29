using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.AI;

public class TentacleProjectile : NetworkBehaviour
{
    [SerializeField] private bool _isPreview = true;
    [SerializeField] private DrawCircleTentacle _drawCircle;
    [SerializeField] private GameObject tentacle;
    [SerializeField] private LayerMask obstecls;
    [SerializeField] private float basePsi = 1f;
    [SerializeField] private float grabDuration = 1.2f;
    [SerializeField] private float lifeTentacle = 4f;
    [SerializeField] private LineRenderer tentacleLine;
    [SerializeField] private Transform tentaclePoint;

    private Coroutine _lineCoroutine;
    private Character _player;
    private Character _target;
    private Vector3 _startPosition;
    private Vector3 _endPosition;

    private float _maxPullDistance;

    private bool _isAttackingPsiEnergyActive;
    private bool _isAttractionTentacleActive;
    private bool _isAttractionTentacle;
    private float _spentAttackingPsiEnergy;

    private float _radius = 4f;
    private bool _radiusView;
    private bool _isCollidedWithOtherCharacter = false;
    private bool _isPullTarget = false;

    private bool _isPsionicsTalentThree = false;

    private Coroutine _radiusUpdateCoroutine;

    private Skill _skill;

    public bool IsAttractionTentacle { get => _isAttractionTentacle; set => _isAttractionTentacle = value; }
    public bool IsPreview { get => _isPreview; set => _isPreview = value; }
    public GameObject Tentacle { get => tentacle; set => tentacle = value; }
    public float Radius => _radius;

    private void Awake()
    {
        _drawCircle = GetComponent<DrawCircleTentacle>();
    }

    private void Start()
    {
        Invoke(nameof(DrawCircleRadius), 0.1f);

        if (tentacleLine != null)
        {
            tentacleLine.positionCount = 2;
        }
    }

    private void OnDestroy()
    {
        if (_drawCircle != null) _drawCircle.Clear();
        if (_radiusUpdateCoroutine != null) StopCoroutine(_radiusUpdateCoroutine);
        if (isServer && _target.CharacterState.CheckForState(States.TentacleGrip)) _target.CharacterState.RemoveState(States.TentacleGrip);

    }

    public void Init(Character player, Character target, Vector3 startPosition, Vector3 endPosition,
        bool isAttackingPsiEnergyActive, bool isPsionicsTalentThree, bool isAttractionTentacleTalent, float currentDamage, Skill skill)
    {
        _isPsionicsTalentThree = isPsionicsTalentThree;
        _player = player;
        _target = target;
        _startPosition = startPosition;
        _endPosition = endPosition;
        _isAttackingPsiEnergyActive = isAttackingPsiEnergyActive;
        _isAttractionTentacleActive = isAttractionTentacleTalent;
        _spentAttackingPsiEnergy = currentDamage;
        _isPsionicsTalentThree =
        _skill = skill;

        transform.position = startPosition;

        _maxPullDistance = Vector3.Distance(endPosition, target.transform.position);

        Invoke(nameof(ReleaseTarget), lifeTentacle);
    }

    private IEnumerator DrawAndPullTarget()
    {
        if (_target == null || _isPreview) yield break;

        float elapsedTime = 0f;

        Vector3 startLinePos = tentaclePoint.position;
        Vector3 targetLinePos = _target.transform.position + Vector3.up * 0.5f;

        tentacleLine.SetPosition(0, startLinePos);
        tentacleLine.SetPosition(1, startLinePos);

        while (elapsedTime < grabDuration)
        {
            float progress = elapsedTime / grabDuration;
            Vector3 currentPos = Vector3.Lerp(startLinePos, targetLinePos, progress);

            tentacleLine.SetPosition(1, currentPos);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        tentacleLine.SetPosition(1, targetLinePos);

        if (tentacleLine != null) _lineCoroutine = StartCoroutine(UpdateTentacleLine());

        if (isServer) AttackTentacles();
        StartCoroutine(PullTarget());
    }

    public void StartTentaclesGrab()
    {
        if (_target != null && !_isPreview)
        {
            Vector3 direction = (_target.transform.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, _target.transform.position);

            float sphereRadius = 0.2f;

            if (Physics.SphereCast(transform.position, sphereRadius, direction, out RaycastHit hit, distance, obstecls)) return;

            _target.Move.IsMoveBlocked = true;
            _isPullTarget = true;

            if (tentacleLine != null) _lineCoroutine = StartCoroutine(DrawAndPullTarget());
        }
    }

    private void DrawCircleRadius()
    {
        if (_drawCircle != null)
        {
            if (_isPreview && _isAttractionTentacle)
            {
                _drawCircle.Draw(_radius);
                _drawCircle.SetColor(Color.red);
            }
            else _drawCircle.Clear();
        }
    }

    public void SetRadiusColor(Color color)
    {
        _drawCircle?.SetColor(color);
    }
    private IEnumerator PullTarget()
    {
        if (_target == null || tentaclePoint == null) yield break;

        Transform targetTransform = _target.transform;
        Vector3 start = targetTransform.position;
        Vector3 end = transform.position;
        var agent = _target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled) agent.enabled = false;

        float timer = 0f;

        if (isServer)
        {
            AddStateTentacleGrip();
            _target.Move.StopMoveAndAnimationMove();
        }

        if (tentacleLine != null)
        {
            tentacleLine.enabled = true;
        }

        while (timer < grabDuration)
        {
            timer += Time.deltaTime;
            float t = timer / grabDuration;

            Vector3 currentPos = Vector3.Lerp(start, end, t);
            targetTransform.position = currentPos;

            if (Vector3.Distance(currentPos, transform.position) <= _maxPullDistance)
            {
                break;
            }

            if (tentacleLine != null && tentaclePoint != null)
            {
                tentacleLine.SetPosition(0, tentaclePoint.position);
                tentacleLine.SetPosition(1, currentPos + Vector3.up * 0.5f);
            }

            yield return null;
        }

        if (tentacleLine != null)
            tentacleLine.enabled = false;

        if (_target != null && isServer)
        {
            _target.Move.StopMoveAndAnimationMove();

            if (agent != null && !agent.enabled) agent.enabled = true;

            Vector3 finalPosition = end;

            _target.Move.CancelMoveTowards();
            _target.Move.TargetRpcSetTransformPosition(finalPosition);
            _target.Move.TargetRpcStopMoveAndAnimationMove();
        }
    }

    private void AttackTentacles()
    {
        if (_isAttackingPsiEnergyActive && _spentAttackingPsiEnergy > 0)
        {
            float attackingPsiValue = _spentAttackingPsiEnergy;

            if (attackingPsiValue > 0)
            {
                DealAttackingPsiDamage(attackingPsiValue);
                if (_isPsionicsTalentThree) ApplyLowVoltageDebuff(attackingPsiValue);
            }
        }
    }

    private void ReleaseTarget()
    {
        if (_target != null) _target.Move.IsMoveBlocked = false;

        if (tentacleLine != null && _lineCoroutine != null) StopCoroutine(_lineCoroutine);

        Destroy(gameObject);
    }

    private void DealAttackingPsiDamage(float attackingPsiValue)
    {
        float damagePerPsi = 0.5f;
        float totalDamage = attackingPsiValue * damagePerPsi;

        var mainDamage = new Damage
        {
            Value = totalDamage,
            Type = DamageType.Magical,
            PhysicAttackType = AttackRangeType.MeleeAttack
        };

        _skill.ApplyDamage(mainDamage, _target.gameObject);

        Collider[] nearbyEnemies = Physics.OverlapSphere(_target.transform.position, 1f, _skill.TargetsLayers);

        foreach (var collider in nearbyEnemies)
        {
            if (collider.TryGetComponent<Character>(out var enemy) && enemy != _target)
            {
                var splashDamage = new Damage
                {
                    Value = totalDamage,
                    Type = DamageType.Magical,
                    PhysicAttackType = AttackRangeType.MeleeAttack
                };

                _skill.ApplyDamage(splashDamage, enemy.gameObject);
            }
        }
    }

    private void ApplyLowVoltageDebuff(float attackingPsiValue)
    {
        int stacks = 0;

        if (attackingPsiValue >= 30)
            stacks = 3;

        else if (attackingPsiValue >= 20)
            stacks = 2;

        else if (attackingPsiValue >= 10)
        {
            stacks = 1;
        }

        if (stacks > 0) for (int i = 0; i < stacks; i++) _target.CharacterState.AddState(States.LowVoltage, 6f, 0f, _player.gameObject, "Tentacles");
    }

    private IEnumerator UpdateTentacleLine()
    {
        while (_isPullTarget && _target != null && tentacleLine != null && tentaclePoint != null)
        {
            tentacleLine.SetPosition(0, tentaclePoint.position);
            tentacleLine.SetPosition(1, _target.transform.position + Vector3.up * 0.5f);
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Character>(out var character) && character != _target)
            _isCollidedWithOtherCharacter = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (_isAttractionTentacleActive && !_isPullTarget) StartTentaclesGrab();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Character>(out var character) && character != _target)
            _isCollidedWithOtherCharacter = false;
    }

    private void AddStateTentacleGrip() => _target.CharacterState.AddState(States.TentacleGrip, 999f, 0f, _player.gameObject, "Tentacles");

    //private void SetPhysicalSkillsDisactive(bool state)
    //{
    //    if (_target != null && _target.Abilities != null)
    //        foreach (Skill skill in _target.Abilities.Abilities) if (skill.AbilityForm == AbilityForm.Physical) skill.Disactive = state;
    //}
}