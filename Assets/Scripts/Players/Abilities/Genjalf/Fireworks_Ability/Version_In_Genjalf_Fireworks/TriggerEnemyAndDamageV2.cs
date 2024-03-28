using System.Collections;
using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability.Version_In_Genjalf_Fireworks
{
    public class TriggerEnemyAndDamageV2 : MonoBehaviour
    {
        [SerializeField] private Fireworks _fireworks;
        [SerializeField] private ShootV2 _shootV2;

        private Coroutine _damageCoroutine;

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.CompareTag("Enemies") && _damageCoroutine == null)
            {
                _damageCoroutine = StartCoroutine(DamageOverTime(col.gameObject));
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Enemies"))
            {
                if (_damageCoroutine != null)
                {
                    StopCoroutine(_damageCoroutine);
                    _damageCoroutine = null;
                }
            }
        }

        private IEnumerator DamageOverTime(GameObject enemy)
        {
            while (_shootV2.Mana > 0)
            {
                float damage = Random.Range(_fireworks.soFireworksData.DamageFireworksMin,
                    _fireworks.soFireworksData.DamageFireworksMax);

                TestHealthEnemy healthComponent = enemy.GetComponent<TestHealthEnemy>();

                if (healthComponent != null)
                {
                    healthComponent.TakeDamage(damage);
                    Debug.Log($"Урон врагу {enemy.name}: {damage}");
                }

                _shootV2.Mana -= 3f;

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}