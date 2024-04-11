using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunePlayer : MonoBehaviour
{
	[SerializeField] private float _runeRegenerationDelay = 1;
	[SerializeField] private float _runeResetDelay = 6;
	[SerializeField] private float _runeRegenerationValue = 10;
	[SerializeField] private float _maxRuneCount = 10;
	//[SerializeField] private Image _runeBar;
	[SerializeField] private float _runeValue;
	[SerializeField] private SpriteRenderer _runeSprite;

	private WaitForSeconds _waitForRegenRune;
	private AbilityBase _lastUsedAbility = null;
	private float _runeSpriteWidth = 3.7f;
	private int _multiplier = 1;
	private float _timer = 0;
	private bool _multiplyCost = false;

	private void Start()
	{
		_waitForRegenRune = new WaitForSeconds(_runeRegenerationDelay);
		StartCoroutine(RegenirateRune());
	}
	private void Update()
	{
		if (!_multiplyCost) return;

		_timer += Time.deltaTime;
		if (_timer > _runeResetDelay)
		{
			_timer = 0;
			_multiplyCost = false;
			_multiplier = 1;
			_lastUsedAbility = null;
		}
	}

	public void AddRune(float runeValue)
	{
		_runeValue += runeValue;
		if (_runeValue > _maxRuneCount)
		{
			_runeValue = _maxRuneCount;
		}
		_runeSprite.size = new Vector2(_runeSprite.size.x + _runeRegenerationValue / _maxRuneCount * _runeSpriteWidth, _runeSprite.size.y);
		if(_runeSprite.size.x > _runeSpriteWidth) 
		{
			_runeSprite.size = new Vector2(_runeSpriteWidth, _runeSprite.size.y);
		}
	}
	//≈сли используетс€ одна и та же способность подр€т, увеличиваетс€ потребление рун в два раза,
	//а так же запускаетс€ таймер, на сброс, через _runeResetDelay сек все сброситс€.
	public bool RemoveRune(float runeValue, AbilityBase usedAbility) 
	{
		if(_lastUsedAbility == usedAbility && _runeValue >= runeValue*_multiplier * 2)
		{
			_multiplier *= 2;
		}
		runeValue *= _multiplier;
		if(_runeValue >= runeValue)
		{
			_lastUsedAbility = usedAbility;
			_runeValue -= runeValue;
			_runeSprite.size = new Vector2(_runeSprite.size.x - runeValue / _maxRuneCount * _runeSpriteWidth, _runeSprite.size.y);
			_multiplyCost = true;
			_timer = 0;
			return true;
		}
		else
		{
			return false;
		}
	}
	private IEnumerator RegenirateRune()
	{
		while (true)
		{
			yield return _waitForRegenRune;
			if (_runeValue < _maxRuneCount)
			{
				AddRune(_runeRegenerationValue);
			}
		}
	}

}
