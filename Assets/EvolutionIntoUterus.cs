using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionIntoUterus : MonoBehaviour
{
    [SerializeField] private Toggle Toggle;
    [SerializeField] private GameObject UterusCoconPrefab;

    private FourMeleeAttack _tentacleAttack;
    private GameObject _player;
    private GameObject _newPrefab;

    void Start()
    {
        _player = transform.parent.gameObject;
        _tentacleAttack = _player.transform.Find("Abilities").gameObject.GetComponent<FourMeleeAttack>();
    }


    void Update()
    {
        if (Toggle.isOn)
        {
            if (_tentacleAttack.ToggleAbility.isOn && _tentacleAttack.FixPrefab && _tentacleAttack.Target == null && Input.GetMouseButtonDown(0))
            {
                Vector2 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(cursorPosition, Vector2.zero);

                string TentacleName = "TentaclePrefab(Clone)";

                if (hit.collider != null && hit.collider.gameObject.name == TentacleName)
                {
                    
                    StartCoroutine(StartEvolution(hit.collider.gameObject, _tentacleAttack.NewAbilityPrefab.transform.position));
                   // _tentacleAttack.ToggleAbility.isOn = false;
                    _tentacleAttack.Recharge();
                }
                else if (hit.collider != null && hit.collider.GetComponent<Uterus>())
                {
                    _tentacleAttack.ToggleAbility.isOn = false;
                    StartCoroutine(AddHealthUterus(hit.collider.gameObject));
                }
            }
        }
    }

    private IEnumerator StartEvolution(GameObject tentacle, Vector2 position)
    {
        yield return new WaitForSeconds(2);

        if (_newPrefab == null)
        {
            _newPrefab = Instantiate(UterusCoconPrefab, position, Quaternion.identity);
        }

        Destroy(tentacle);
    }
    private IEnumerator AddHealthUterus(GameObject uterus)
    {
        yield return new WaitForSeconds(1);
        uterus.GetComponent<HealthPlayer>().Health += 33;

        yield return new WaitForSeconds(1);
        uterus.GetComponent<HealthPlayer>().Health += 33;

        yield return new WaitForSeconds(1);
        uterus.GetComponent<HealthPlayer>().Health += 34;
    }
}
