using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability.Version_In_Genjalf_Fireworks
{
    public class TriggerEnemyAndDamageV2 : MonoBehaviour
    {
        [SerializeField] private Fireworks _fireworks;
        [SerializeField] private ShootFireworks shootFireworks;

        private Coroutine _damageCoroutine;
        private List<GameObject> _enemies = new List<GameObject>(); // Список противников
        private int _currentEnemyIndex = 0; // Индекс текущего противника

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.CompareTag("Enemies"))
            {
                _enemies.Add(col.gameObject); // Добавляем противника в список
                Debug.Log($"Противник найден: {col.gameObject}");
                if (_damageCoroutine == null)
                {
                    _damageCoroutine = StartCoroutine(DamageOverTime());
                }
                SortEnemiesByDistance();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Enemies"))
            {
                _enemies.Remove(other.gameObject); // Удаляем противника из списка
                if (_enemies.Count == 0 && _damageCoroutine != null)
                {
                    StopCoroutine(_damageCoroutine);
                    _damageCoroutine = null;
                }
                SortEnemiesByDistance();
            }
        }

        private void SortEnemiesByDistance()
        {
            _enemies.Sort(CompareDistanceToMe);
        }

        private int CompareDistanceToMe(GameObject b, GameObject a)
        {
            float squaredRangeA = (a.transform.position - transform.position).sqrMagnitude;
            float squaredRangeB = (b.transform.position - transform.position).sqrMagnitude;
            return squaredRangeA.CompareTo(squaredRangeB);
        }

        private IEnumerator DamageOverTime()
        {
            while (shootFireworks.gameObject.transform.parent.GetComponent<Mana>().Value > 0 && _enemies.Count > 0)
            {
                shootFireworks.gameObject.transform.parent.GetComponent<Mana>().Use(_fireworks.soFireworksData.ManaCost);

                for (int i = 0; i < _enemies.Count; i++)
                {
                    GameObject currentEnemy = _enemies[i];
                    float damageMultiplier = GetDamageMultiplier(i);
                    float damage = Random.Range(_fireworks.soFireworksData.DamageFireworksMin,
                            _fireworks.soFireworksData.DamageFireworksMax) * damageMultiplier;
                    HealthComponent healthComponent = currentEnemy.GetComponent<HealthComponent>();

                    if (healthComponent != null)
                    {
                        healthComponent.TryTakeDamage(damage, DamageType.Magical, AttackRangeType.RangeAttack);
                        Debug.Log(
                            $"Урон врагу {currentEnemy.name}: {damage}. Процент урона: {damageMultiplier * 100}%");
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Метод для определения множителя урона в зависимости от индекса противника в списке
        private float GetDamageMultiplier(int index)
        {
            // Первый получает 100% урона, второй 70%, третий 30%
            if (index == 0)
            {
                return _fireworks.soFireworksData.PercentageTargetOne;
            }
            else if (index == 1)
            {
                return _fireworks.soFireworksData.PercentageTargetTwo;
            }
            else
            {
                return _fireworks.soFireworksData.PercentageTargetThree;
            }
        }
    }
}