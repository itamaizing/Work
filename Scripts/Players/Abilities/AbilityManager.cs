using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public List<AbilityBase> abilityQueue = new List<AbilityBase>();
    public List<AbilityBase> abilityQueueAutoattack = new List<AbilityBase>();

    private AbilityBase nextAbility;

    public void AddAbilityToQueue(AbilityBase ability)
    {
        if (ability.AttackType == AttackType.Autoattack)
        {
            abilityQueueAutoattack.Add(ability);
        }
        else
        {
            if (nextAbility != null && abilityQueue.Count == 0 && abilityQueueAutoattack.Count > 0)
            {
                if (nextAbility.NewAbilityPrefab != null)
                {
                    nextAbility.NewAbilityPrefab.SetActive(false);
                    nextAbility.DrawCircle.Clear();
                }

                nextAbility.CanDoAbility = false;
                nextAbility = null;
            }

            abilityQueue.Add(ability);
        }

        if (abilityQueue.Count == 1 || abilityQueueAutoattack.Count == 1) // Если это первая способность в очереди, начните ее выполнение
        {

            ExecuteNextAbility();
        }
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

        if (nextAbility != null && nextAbility.ToggleAbility.isOn == false && abilityQueue.Count > 0)
        {
            abilitiesToRemove.Add(abilityQueue[0]);
        }
        else if (nextAbility != null && nextAbility.ToggleAbility.isOn == false && abilityQueue.Count <= 0 && abilityQueueAutoattack.Count > 0)
        {
            abilitiesToRemove.Add(abilityQueueAutoattack[0]);
        }

        foreach (var abilityToRemove in abilitiesToRemove)
        {
            if (abilityQueue.Contains(abilityToRemove))
            {
                abilityQueue.Remove(abilityToRemove);
            }
            else if (abilityQueueAutoattack.Contains(abilityToRemove))
            {
                abilityQueueAutoattack.Remove(abilityToRemove);
            }

            abilityToRemove.DrawCircle.Clear();
        }

        abilitiesToRemove.Clear();

        if (abilityQueue.Count > 0 || abilityQueueAutoattack.Count > 0)
        {
            ExecuteNextAbility();
        }

        if (abilityQueue.Count <= 0 && abilityQueueAutoattack.Count <= 0)
        {
            nextAbility = null;
        }
    }
}
