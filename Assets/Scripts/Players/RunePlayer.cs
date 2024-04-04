using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunePlayer : MonoBehaviour
{
	[SerializeField] private float _runeRegenerationDelay = 1;
	[SerializeField] private float _runeRegenerationValue = 10;
	//[SerializeField] private Image _runeBar;
	private WaitForSeconds _waitForRegenRune;
	[SerializeField] private float _runeValue;

    public float regen;

	private void Start()
	{
		UpdateRuneBar();
		_waitForRegenRune = new WaitForSeconds(_runeRegenerationDelay);
		StartCoroutine(RegenirateRune());
	}

	public void UpdateRuneBar()
	{
		
	}
	public void AddRune(float runeValue)
	{
		_runeValue += runeValue;
	}

	public bool RemoveRune(float runeValue) 
	{
		if(_runeValue >= runeValue)
		{
			_runeValue -= runeValue;
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
			this.AddRune(_runeRegenerationValue);
		}
	}
}
