using UnityEngine;
using UnityEngine.EventSystems;

public class LastAbility : MonoBehaviour
{
    public SelectObject Select;
    public Collider2D[] colliders;
    /*[HideInInspector]*/ public AbilityBase LastUseAbility;
    [HideInInspector] public bool OneClick;
    [HideInInspector] public bool TwoClick;
    private Vector2 _clickPosition;
    [HideInInspector] public bool IsCanUseLastAbility = true;

   public void AddLastAbility(AbilityBase ability)
    {
        LastUseAbility = ability;
        OneClick = false;
        TwoClick = false;
    }

    private void Update()
    {
        if(IsCanUseLastAbility && LastUseAbility != null && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            var select = Select.SelectedObject.transform.Find("Abilities");

            if (select != null && select.TryGetComponent<FourMeleeAttack>(out var meleeAttack))
                if (meleeAttack.ToggleAbility.isOn && meleeAttack.TargetParent == null && OneClick == false)
           /* if (Select.SelectedObject != null && Select.SelectedObject.transform.Find("Abilities") != null && Select.SelectedObject.transform.Find("Abilities").GetComponent<FourMeleeAttack>() && Select.SelectedObject.transform.Find("Abilities").GetComponent<FourMeleeAttack>().ToggleAbility.isOn &&
                Select.SelectedObject.transform.Find("Abilities").GetComponent<FourMeleeAttack>().TargetParent == null && OneClick == false)*/
            {
                return;
            }
            else
            {
                ClickAtLastAbility();
            }
        }

        if(TwoClick)
        {
            float distance = LastUseAbility.Distance - (1.9f / 2f);
            colliders = Physics2D.OverlapCircleAll(Select.SelectedObject.transform.position, distance);

            if (colliders != null)
            {
                foreach (Collider2D collider in colliders)
                {
                    string targetTag = (LastUseAbility.AbilityType == AbilityType.DamageAbility) ? "Enemies" : "Allies";

                    if (collider.CompareTag(targetTag) && collider.gameObject != gameObject)
                    {
                        LastUseAbility.TargetParent = collider.gameObject;

                        break;
                    }
                }

                if (LastUseAbility.TargetParent != null)
                {
                    LastUseAbility.ChangeBoolAndValues();
                    TwoClick = false;
                }
            }
        }
    }

    public void ClickAtLastAbility()
    {
        _clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit = Physics2D.Raycast(_clickPosition, Vector2.zero);

        if (hit.collider == null && LastUseAbility.ToggleAbility.isOn == false)
        {
            LastUseAbility.ToggleAbility.isOn = true;
            OneClick = true;
        }
        else if (hit.collider == null && LastUseAbility.ToggleAbility.isOn == true && OneClick)
        {
            float distance = LastUseAbility.Distance - (1.9f / 2f); 
            colliders = Physics2D.OverlapCircleAll(Select.SelectedObject.transform.position, distance);

            if (colliders != null)
            {
                foreach (Collider2D collider in colliders)
                {
                    string targetTag = (LastUseAbility.AbilityType == AbilityType.DamageAbility) ? "Enemies" : "Allies";

                    if (collider.CompareTag(targetTag) && collider.gameObject != gameObject)
                    {
                        LastUseAbility.TargetParent = collider.gameObject;

                        break;
                    }
                }

                if (LastUseAbility.TargetParent != null)
                {
                    LastUseAbility.ChangeBoolAndValues();
                }
                else
                {
                    TwoClick = true;
                }
            }
            else
            {
                TwoClick = true;
            }
        }
    }
}

