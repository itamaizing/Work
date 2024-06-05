using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FiveReversPolarity : MonoBehaviour
{
	public LastAbility _lastAbility;
	public GameObject IconAbility;
	public Toggle ToggleAbility;
	public GameObject ManaCost;
	public GameObject BaffPrefab;

	private GameObject _baffPrefab;
	private Coroutine _coroutine;
	private bool isDarkSide = false;


	void Update()
	{
		if (ToggleAbility.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Alpha5) && transform.parent.GetComponent<MoveComponent>().IsSelect)
		{
			if (ToggleAbility.isOn)
			{
				ToggleAbility.isOn = false;
            }
			else
			{
				ToggleAbility.isOn = true;
            }

            if (ToggleAbility.isOn == true)
            {

                IconAbility.GetComponent<SpriteRenderer>().enabled = true;
            }

			if(_lastAbility.LastUseAbility is ThreeRangeHeal||isDarkSide)
			{
				UseReversePolarity(0f);

            }
			else if(!isDarkSide)
			{
				UseReversePolarity(1.5f);
            }
        }

    }

	public void UseReversePolarity(float time)
	{
		_coroutine = StartCoroutine(Cast(time));
    }

    private IEnumerator Cast(float castTime)
	{
		transform.parent.GetComponent<MoveComponent>().CanMove = false;

		ManaCost.SetActive(true);
		ManaCost.GetComponent<VisualManaCost>().CheckManaCost();
		ManaCost.transform.localScale = new Vector2(2f, ManaCost.gameObject.transform.localScale.y);
        yield return new WaitForSeconds(castTime);

        GetComponent<OneRangeAttack>().ReverseAbility(isDarkSide);
		GetComponent<TwoRangeProtection>().ReverseAbility(isDarkSide);
		GetComponent<ThreeRangeHeal>().ReverseAbility(isDarkSide);
		GetComponent<FourRangeRecovery>().ReverseAbility(isDarkSide);
        isDarkSide = !isDarkSide;

        CreateBaffPrefab();

        transform.parent.GetComponent<Mana>().Use(20);
        ManaCost.SetActive(false);
		transform.parent.GetComponent<MoveComponent>().CanMove = true;
		ToggleAbility.enabled = true;
	}

	private void CreateBaffPrefab()
	{
		if(_baffPrefab==null)
		{
            _baffPrefab = Instantiate(BaffPrefab,transform.parent.GetChild(9));
        }
		else
		{
			Destroy(_baffPrefab);
		}
    }
}
