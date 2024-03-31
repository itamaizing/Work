using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
	public List<AbilityBase> abilityQueue = new List<AbilityBase>();
	public List<AbilityBase> abilityQueueAutoattack = new List<AbilityBase>();

	private AbilityBase nextAbility;
	private PlayerMove _playerMove;


	private void Awake()
	{
		_playerMove = GetComponentInParent<PlayerMove>();
	}

	private void OnEnable()
	{
		InputHandler.OnAltClick += CancelSpellCast;
	}

	private void OnDisable()
	{
		InputHandler.OnAltClick -= CancelSpellCast;
	}

	public void AddAbilityToQueue(AbilityBase ability)
	{
		// если способность - автоатака, то добавить в очередь автоатак
		if (ability.AttackType == AttackType.Autoattack)
		{
			abilityQueueAutoattack.Add(ability);
		}
		else // если способность не автоатака, то добавить в очередь обычных способностей
		{
			// если есть текущая способность и число абилок в очереди = 0 и есть абилка-автоатака 
			if (nextAbility != null && abilityQueue.Count == 0 && abilityQueueAutoattack.Count > 0)
			{
                // если есть префаб абилки
                Debug.LogWarning("AutoAttack Paused dealing damage");
				ChangeAutoAttackState(); // выключаем, но не удаляем автоатаку
            }

			abilityQueue.Add(ability);
		}

		if (abilityQueue.Count == 1 || abilityQueueAutoattack.Count == 1) // Если это первая способность в очереди, начните ее выполнение
		{
			ExecuteNextAbility();
		}
	}

	private void DeleteCurrentAbility()
	{
		if (nextAbility.NewAbilityPrefab != null)
		{
			// выключить префаб, очистить круг радиуса атаки
			nextAbility.NewAbilityPrefab.SetActive(false);
			nextAbility.DrawCircle.Clear();
		}

		if(abilityQueue.Count == 1) // если удаляется последняя способность - включаем автоатаку
        {
			ChangeAutoAttackState();

        }
		// удаляем текущую абилку
		nextAbility.CanDoAbility = false;
		nextAbility.CancelAbilityOnClick();
		nextAbility = null;
	}

	private void ExecuteNextAbility()
	{
		if (abilityQueue.Count > 0 && abilityQueue[0] != null)
		{
			nextAbility = abilityQueue[0];
			nextAbility.CanDoAbility = true;
		}
		else if (abilityQueue.Count <= 0 && abilityQueueAutoattack.Count > 0 && abilityQueueAutoattack[0] != null)
		{
			nextAbility = abilityQueueAutoattack[0];
			nextAbility.CanDoAbility = true;
			nextAbility.CanDrawCircle = true;

			if (nextAbility.NewAbilityPrefab != null)
			{
				nextAbility.NewAbilityPrefab.SetActive(true);
			}
		}
	}

	private void Update()
	{
		List<AbilityBase> abilitiesToRemove = new List<AbilityBase>();

		abilityQueue.RemoveAll(item => item.ToggleAbility.isOn == false);
		abilityQueueAutoattack.RemoveAll(item => item.ToggleAbility.isOn == false);

		//if (nextAbility != null && nextAbility.ToggleAbility.isOn == false && abilityQueue.Count > 0)
		//{
		//	abilitiesToRemove.Add(abilityQueue[0]);
		//}
		//else if (nextAbility != null && nextAbility.ToggleAbility.isOn == false && abilityQueue.Count <= 0 && abilityQueueAutoattack.Count > 0)
		//{
		//	abilitiesToRemove.Add(abilityQueueAutoattack[0]);
		//}

		//foreach (var abilityToRemove in abilitiesToRemove)
		//{
		//	if (abilityQueue.Contains(abilityToRemove))
		//	{
		//		abilityQueue.Remove(abilityToRemove);
		//	}
		//	else if (abilityQueueAutoattack.Contains(abilityToRemove))
		//	{
		//		abilityQueueAutoattack.Remove(abilityToRemove);
		//	}

		//	abilityToRemove.DrawCircle.Clear();
		//}

		//abilitiesToRemove.Clear();

		if (abilityQueue.Count > 0 || abilityQueueAutoattack.Count > 0)
		{
			ExecuteNextAbility();
		}

		if (abilityQueue.Count <= 0 && abilityQueueAutoattack.Count <= 0)
		{
			nextAbility = null;
		}
	}

	// отмена текущего заклинания
	public void CancelSpellCast()
	{
		if (!_playerMove.IsSelect)
			return;

		Debug.Log("Cancel");

		if (nextAbility != null)
		{
			DeleteCurrentAbility();
			Debug.Log("removed next");
			return;
		}

		if (abilityQueueAutoattack.Count > 0)
		{
			abilityQueueAutoattack[0].DrawCircle.Clear();
			abilityQueueAutoattack[0].CancelAbilityOnClick();
			abilityQueueAutoattack.RemoveAt(0);

			Debug.Log("Removed autoattack");

			return;
		}

		if (abilityQueue.Count > 0)
		{
			ChangeAutoAttackState(); //при прерывании способности включаем автоатаку

			abilityQueue[0].DrawCircle.Clear();
			abilityQueue[0].CancelAbilityOnClick();
			abilityQueue.RemoveAt(0);

			Debug.Log("Removed ability");

			return;
		}
	}


    private void ChangeAutoAttackState()
    {
        if (abilityQueueAutoattack.Count > 0 && abilityQueueAutoattack[0] != null)
        {
            abilityQueueAutoattack[0].CanDoAbility = !abilityQueueAutoattack[0].CanDoAbility;
        }

    }

}
