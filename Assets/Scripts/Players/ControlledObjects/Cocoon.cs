using UnityEngine;

public class Cocoon : MonoBehaviour
{
    [SerializeField] private GameObject Prefab;
    [HideInInspector] public GameObject Player;

    private float _evasionAttack = 0.5f;
    [SerializeField] private float _interval = 5;
    private float _timer;

    private GameObject _newPrefab;

    void Start()
    {
        GetComponent<HealthComponent>().OnTakePhisicDamage += DamageEvasion;
        GetComponent<HealthComponent>().OnTakeMagicDamage += DamageEvasion;

    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _interval)
        {
            _newPrefab = Instantiate(Prefab, gameObject.transform.position, Quaternion.identity);
            if (_newPrefab.GetComponent<Spisnacider>())
            {
                _newPrefab.GetComponent<Spisnacider>().Player = Player;
            }
            Destroy(gameObject);
            _timer = 0f;
        }
    }

    private void DamageEvasion(HealthComponent.DamageInfo damageInfo)
    {
        damageInfo.ModifiedDamage -= damageInfo.ModifiedDamage * _evasionAttack;
    }
}
