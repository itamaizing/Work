using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunePlayer : MonoBehaviour
{
	[SerializeField] private float _runeRegenerationDelay = 3;
	[SerializeField] private float _runeRegenerationValue = 10;
	//[SerializeField] private Image _runeBar;
	private WaitForSeconds _waitForRegenRune;

	public float rune;
    public float regen;

	private void Start()
	{
		UpdateRuneBar();
		_waitForRegenRune = new WaitForSeconds(_runeRegenerationDelay);
		StartCoroutine(RegenirateHP());
	}

	public void UpdateRuneBar()
	{
		
	}
	public void AddRune(float runeValue)
	{
		
	}

	public void RemoveRune(float runeValue) 
	{

	}
	private IEnumerator RegenirateHP()
	{
		while (true)
		{
			yield return _waitForRegenRune;
			this.AddRune(_runeRegenerationValue);
		}
	}
}
