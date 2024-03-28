using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability
{
    public class TestHealthEnemy : MonoBehaviour
    {
        [SerializeField] private float _healthEnemy;

        // Метод для получения урона
        public void TakeDamage(float damage)
        {
            // Уменьшаем здоровье на количество урона
            _healthEnemy -= damage;

            // Проверяем, не стал ли уровень здоровья меньше или равным нулю
            if (_healthEnemy <= 0)
            {
                // Если здоровье меньше или равно нулю, уничтожаем объект
                Destroy(gameObject);
            }
        }


        public float HealthEnemy
        {
            get => _healthEnemy;
            set => _healthEnemy = value;
        }
    }
}