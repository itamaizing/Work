using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability.Version_In_Genjalf_Fireworks
{
    public class TriggerFireWorksRight : MonoBehaviour
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
            }
        }

        private IEnumerator DamageOverTime()
        {
            while (shootFireworks.gameObject.transform.parent.GetComponent<Mana>().Value > 0 && _enemies.Count > 0)
            {
                if (_currentEnemyIndex < _enemies.Count)
                {
                    GameObject currentEnemy = _enemies[_currentEnemyIndex];

                    float damageMultiplier = GetDamageMultiplier(_currentEnemyIndex);

                    float damage = Random.Range(_fireworks.soFireworksData.DamageFireworksMin,
                        _fireworks.soFireworksData.DamageFireworksMax) * damageMultiplier;

                    // TestHealthEnemy healthComponent = currentEnemy.GetComponent<TestHealthEnemy>();
                    //
                    // if (healthComponent != null)
                    // {
                    //     healthComponent.TakeDamage(damage);
                    //     Debug.Log(
                    //         $"Урон врагу {currentEnemy.name}: {damage}. Процент урона: {damageMultiplier * 100}%");
                    // }
                    
                    HealthComponent healthComponent = currentEnemy.GetComponent<HealthComponent>();
                    
                    if (healthComponent != null)
                    {
                        healthComponent.TryTakeDamage(damage, DamageType.Magical, AttackRangeType.RangeAttack);
                        Debug.Log($"Урон врагу {currentEnemy.name}: {damage}. Процент урона: {damageMultiplier * 100}%");
                    }
                    
                    shootFireworks.gameObject.transform.parent.GetComponent<Mana>().Use(_fireworks.soFireworksData.ManaCost);
                    
                    // Переход к следующему противнику
                    _currentEnemyIndex = (_currentEnemyIndex + 1) % _enemies.Count;
                }

                else
                {
                    Debug.LogWarning("Индекс противника выходит за пределы списка");
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