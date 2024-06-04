using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//using static UnityEditor.Progress;

public class AbilityManager : MonoBehaviour
{
	public List<AbilityBase> abilityQueue = new List<AbilityBase>();
	public List<AbilityBase> abilityQueueAutoattack = new List<AbilityBase>();

	private AbilityBase nextAbility;
	private MoveComponent _playerMove;

    private void FixedUpdate()
    {
        if(Input.GetKeyDown(KeyCode.L))
		{
			if(abilityQueueAutoattack.Count > 0 )
			{
				SwapAutoattacks();
                abilityQueueAutoattack[0].CanDoAbility = !abilityQueueAutoattack[0].CanDoAbility;
            }
		}
    }
    private void Awake()
	{
		_playerMove = GetComponentInParent<MoveComponent>();
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
				//DeleteCurrentAbility();
            }

			abilityQueue.Add(ability);
		}

		if (abilityQueue.Count == 1 || abilityQueueAutoattack.Count == 1) // Если это первая способность в очереди, начните ее выполнение
		{
			ExecuteNextAbility();
		}
	}

	private void SwapAutoattacks()
	{
		if (abilityQueueAutoattack.Count > 1)
		{
			if (abilityQueueAutoattack[0].TargetParent == abilityQueueAutoattack[1].TargetParent)
			{
				if (abilityQueueAutoattack[0].isInRadius) // первая автоатака дотягивается
				{
                    abilityQueueAutoattack[0].CanDealDamageOrHeal = true;
                    abilityQueueAutoattack[1].CanDealDamageOrHeal = false;
                    nextAbility = abilityQueueAutoattack[0];
                }
				else if (abilityQueueAutoattack[0].Distance < abilityQueueAutoattack[1].Distance) // первая автоатака не дотягивается и ее дальность < второй
				{
                    abilityQueueAutoattack[0].CanDealDamageOrHeal = false;
                    abilityQueueAutoattack[1].CanDealDamageOrHeal = true;
                    nextAbility = abilityQueueAutoattack[1];
                }
			}

			else if (abilityQueueAutoattack[1].TargetParent != null) // разные цели и у второй атаки выбрана цель
			{
				abilityQueueAutoattack[0].CanDoAbility = true;
				abilityQueueAutoattack[1].CanDoAbility = true;
			}
		}
		else if (abilityQueueAutoattack.Count == 1)
		{
			nextAbility = abilityQueueAutoattack[0];
		}
	}

	private void SwapAutoattacksVisualisation() // костыль - отображение круга, отдельно чтобы можно было менять во время паузы (когда есть способность и выбрана цель)
	{
		if (abilityQueueAutoattack.Count > 1)
		{
			if (abilityQueueAutoattack[0].TargetParent == abilityQueueAutoattack[1].TargetParent)
			{
				if (abilityQueueAutoattack[0].isInRadius)
				{
					abilityQueueAutoattack[0].CanDrawCircle = true;
					abilityQueueAutoattack[1].CanDrawCircle = false;
				}
				else if (abilityQueueAutoattack[0].Distance < abilityQueueAutoattack[1].Distance)
				{
					abilityQueueAutoattack[0].CanDrawCircle = false;
					abilityQueueAutoattack[1].CanDrawCircle = true;
				}
			}
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

		// удаляем текущую абилку
		nextAbility.CanDoAbility = false;
		nextAbility.CancelAbilityOnClick();
		nextAbility = null;

    }

	private void ExecuteNextAbility()
	{
		if (abilityQueue.Count > 0 && abilityQueue[0] != null)
		{
			if (abilityQueue[0].TargetParent != null) // если выбрали цель для способности, и есть автоатака, останавливаем атаку
			{
                ChangeAutoAttackStateToFalse();
            }

            nextAbility = abilityQueue[0];
			nextAbility.CanDoAbility = true;
        }
		else if (abilityQueue.Count <= 0 && abilityQueueAutoattack.Count > 0 && abilityQueueAutoattack[0] != null)
		{
			ChangeAutoAttackStateToTrue();
			SwapAutoattacks();

			//nextAbility = abilityQueueAutoattack[0];
			nextAbility.CanDoAbility = true;
			nextAbility.CanDrawCircle = true;

			if (nextAbility.NewAbilityPrefab != null)
			{
				nextAbility.NewAbilityPrefab.SetActive(true);
			}
		}
		
		if(abilityQueueAutoattack.Count > 0 && abilityQueueAutoattack[0] != null)
		{
            SwapAutoattacksVisualisation();
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
			ChangeAutoAttackStateToTrue(); //при прерывании способности включаем автоатаку
			abilityQueue[0].DrawCircle.Clear();
			abilityQueue[0].CancelAbilityOnClick();
			abilityQueue.RemoveAt(0);

            Debug.Log("Removed ability");

			return;
		}
    }

    private void ChangeAutoAttackStateToTrue()
    {
        if (abilityQueueAutoattack.Count > 0 && abilityQueueAutoattack[0] != null)
        {
			for(int i = 0; i < abilityQueueAutoattack.Count; i++)
			{
				abilityQueueAutoattack[i].CanDealDamageOrHeal = true;
			}

            //Debug.LogWarningFormat("ChangeAutoAttackStateToTrue");
        }

    }

    private void ChangeAutoAttackStateToFalse()
    {
        if (abilityQueueAutoattack.Count > 0 && abilityQueueAutoattack[0] != null)
        {
            for (int i = 0; i < abilityQueueAutoattack.Count; i++)
            {
                abilityQueueAutoattack[i].CanDealDamageOrHeal = false;
            }

            //Debug.LogWarningFormat("ChangeAutoAttackStateToFalse");
        }

    }
}
