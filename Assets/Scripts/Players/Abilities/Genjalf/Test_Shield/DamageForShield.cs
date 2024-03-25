using TMPro;
using UnityEngine;

namespace Players.Abilities.Genjalf.Test_Shield
{
    public class DamageForShield : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private float damage = 10f;
        public float health = 20f;

        private float _currentHealth;

        private void Start()
        {
            _currentHealth = health;
            _healthText.text = "Health: " + _currentHealth.ToString();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Damage(damage);
            }
        }

        private void Damage(float damage)
        {
            _currentHealth -= damage;
            _healthText.text = "Health: " + _currentHealth.ToString();
            //Debug.Log($"Çהמנמגüו + {_currentHealth} וה.");
            if (_currentHealth <= 0)
            {
                gameObject.GetComponent<SpriteRenderer>().color = Color.red;
            }
                
        }
    }
}