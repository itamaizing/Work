using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class AutoAttackSkill : Skill
{
    [Header("AutoAttack settings")]
    [SerializeField] private float _attackZoneSize;
    [SerializeField] protected float _attackSpeed = 1f;
    [SerializeField] protected float _chargeAttackDelay;

    protected Character _target;
    private bool _isAutoattackMode = true;
    private Coroutine _autoAttackCoroutine;
    private bool _isAttacking = false;
    private Vector2 _lastTargetPosition;


	float time = 0;
	float duration = 1f;

    Color startColor = Color.green;
    Color endColor = new Color(0, 1, 0, 0f);

	public float AttackSpeed { get => Buff.AttackSpeed.GetBuffedValue(_attackSpeed); }
    public Character Target { get => _target; }
    public Vector2 LastTargetPosition { get => _lastTargetPosition; }
    public override bool IsPayCostStartCooldown { get => false; }
    public bool IsAutoattackMode { get => _isAutoattackMode; }
    protected override bool IsCanCast
    {
        get
        {
            if (Target == null)
                return false;

            return NoObstacles(Target.transform.position, _obstacle) && IsTargetInRadius(Radius, Target.transform); ;
        }
    }

    private void Update()
    {
        if (_target == null)
        {
            return;
        }

		if (_isAttacking)
		{
			startColor = Color.green;
			endColor = new Color(0, 1, 0, 0f);
		}
        else
        {
			startColor = Color.red;
			endColor = new Color(1, 0, 0, 0f);
		}
		float t = Mathf.PingPong(Time.time * duration, 1f);
		Color currentColor = Color.Lerp(startColor, endColor, t);
		//_target.SelectedCircle.Circle.color = currentColor;
		_skillRender.DrawRadiusColor(Radius, currentColor);
	}


	protected abstract void CastAction();

    protected override IEnumerator CastJob()
    {
        yield return _autoAttackCoroutine = StartCoroutine(AutoAttackJob());
    }

    protected override void ClearData()
    {
        if (_target != null)
        {
            //_target.SelectedCircle.Circle.color = Color.green;
			_target.SelectedCircle.IsActive = false;
		}
		_skillRender.SetColor(Color.green);

		
        if (_autoAttackCoroutine != null)
        {
            StopCoroutine(_autoAttackCoroutine);
            _autoAttackCoroutine = null;
        }
        _isAttacking = false;
        _target = null;
    }

    protected override IEnumerator PrepareJob()
    {
        do
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if(_target != null)
				    _target.SelectedCircle.IsActive = true;
			}
            yield return null;
        }
        while (Target == null);
    }

    public void SwitchAutoMode()
    {
        _isAutoattackMode = !_isAutoattackMode;
    }

    public void Pause()
    {
        if (_autoAttackCoroutine != null)
        {
            StopCoroutine(_autoAttackCoroutine);
            _autoAttackCoroutine = null;
        }
        _isAttacking = false;
    }

    public void Continue()
    {
        if (_autoAttackCoroutine == null && Target != null)
        {
            _autoAttackCoroutine = StartCoroutine(AutoAttackJob());
        }
    }

    protected virtual IEnumerator AutoAttackJob()
    {
		while (Target != null)
        {
            if (IsTargetInRadius(Radius + _attackZoneSize, Target.transform))
            {
                if (IsTargetInRadius(Radius, Target.transform))
                    _isAttacking = true;

                if (_isAttacking && NoObstacles(Target.transform.position, _obstacle))
                {
                    _lastTargetPosition = Target.transform.position;
                    yield return StartCastDeleyCoroutine(_chargeAttackDelay);
                    yield return new WaitForSeconds(AttackSpeed);
                    if (IsTargetInRadius(Radius + _attackZoneSize, Target.transform) && NoObstacles(Target.transform.position, _obstacle) && IsCooldowned)
                    {
                        if (TryPayCost(true))
                        {
                            CastAction();

                            if (_isAutoattackMode == false)
                            {
                                ClearData();
                                yield break;
                            }

                        }
                    }
                    yield return new WaitForSeconds(AttackSpeed);
                }
			}
            else
            {
                _isAttacking = false;
            }
            yield return null;
        }
        _autoAttackCoroutine = null;
    }

	Color ColourChanging(float fadeStart, float fadeTime, Color objectColor, Color fadeColor)
	{
		if (fadeStart < fadeTime)
		{
			fadeStart += Time.deltaTime * fadeTime;

			return Color.Lerp(objectColor, fadeColor, fadeStart);
		}
        else
        {
            return Color.Lerp(objectColor, fadeColor, fadeStart);
        }
	}
}
