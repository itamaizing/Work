using UnityEngine;
using UnityEngine.UI;

public class CarrygunControlledObjects : MonoBehaviour
{
    [SerializeField] private Toggle Toggle;
    [SerializeField] private GameObject IconAbility;
    [SerializeField] private GameObject PointPrefab;
    [HideInInspector] public Vector2 TargetPosition;
    [HideInInspector] public bool IsTargetSelect;
    [HideInInspector] public GameObject TargetObject;
    [HideInInspector]  public GameObject NewPrefabPoint;

    private SelectObject _select;
    private GameObject _player;


    void Start()
    {
        _player = transform.parent.gameObject;
        _select = GameObject.Find("Select").GetComponent<SelectObject>();
    }

    void Update()
    {

        if (_select.SelectedObject == _player && _select.ControlledObjects.Count > 0)
        {
            Toggle.gameObject.SetActive(true);


            if (Input.GetKeyDown(KeyCode.R))
            {
                Toggle.isOn = !Toggle.isOn;
            }
        }
        else
        {
            Toggle.isOn = false;
            Toggle.gameObject.SetActive(false);
        }

        if(Toggle.isOn)
        {
            IconAbility.GetComponent<SpriteRenderer>().enabled = true;
            Color newColor = IconAbility.GetComponent<SpriteRenderer>().color;
            newColor.a = 1f;
            IconAbility.GetComponent<SpriteRenderer>().color = newColor;

            _player.transform.Find("AbilityManager").GetComponent<LastAbility>().IsCanUseLastAbility = false;

            if(IsTargetSelect == false)
            {
                HandleTargetSelection();

                if(_select.SelectedObject == _player && Input.GetMouseButtonDown(0))
                {
                    Vector2 _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

                    if (hit.collider == null)
                    {
                        TargetPosition = _targetPosition;

                        if (NewPrefabPoint == null)
                        {
                            NewPrefabPoint = Instantiate(PointPrefab, TargetPosition, Quaternion.identity);
                        }
                        else
                        {
                            NewPrefabPoint.transform.position = TargetPosition;
                        }


                        TargetObject = null;
                        IsTargetSelect = true;
                    }
                }
            }
            else
            {
                _player.transform.Find("AbilityManager").GetComponent<LastAbility>().IsCanUseLastAbility = true;
            }
        }
        else if (!Toggle.isOn)
        {
            IconAbility.GetComponent<SpriteRenderer>().enabled = false;
            IsTargetSelect = false;
        }

        if (!Toggle.gameObject.activeSelf && NewPrefabPoint != null)
        {
            Destroy(NewPrefabPoint);
        }

        if(IsTargetSelect)
        {
            Toggle.isOn = false;
        }
    }

    private void HandleTargetSelection()
    {
        // Выбор врага
        Vector2 _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
        {
            TargetObject = hit.collider.gameObject;
            TargetPosition = TargetObject.transform.position;
            IsTargetSelect = true;

            if(NewPrefabPoint != null)
            {
                Destroy(NewPrefabPoint);
            }
        }
    }
}
