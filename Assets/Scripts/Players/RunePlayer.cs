using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunePlayer : MonoBehaviour
{
	[SerializeField] private float _runeRegenerationDelay = 1;
	[SerializeField] private float _runeRegenerationValue = 10;
	[SerializeField] private float _maxRuneCount = 10;
	//[SerializeField] private Image _runeBar;
	private WaitForSeconds _waitForRegenRune;
	[SerializeField] private float _runeValue;
	[SerializeField] private SpriteRenderer _runeSprite;

	private float _runeSpriteWidth = 3.7f;

    //public float regen;

	private void Start()
	{
		_waitForRegenRune = new WaitForSeconds(_runeRegenerationDelay);
		StartCoroutine(RegenirateRune());
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

	public bool RemoveRune(float runeValue) 
	{
		if(_runeValue >= runeValue)
		{
			_runeValue -= runeValue;
			_runeSprite.size = new Vector2(_runeSprite.size.x - runeValue / _maxRuneCount * _runeSpriteWidth, _runeSprite.size.y);
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
