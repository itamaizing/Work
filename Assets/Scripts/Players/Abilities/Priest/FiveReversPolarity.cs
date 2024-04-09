using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FiveReversPolarity : MonoBehaviour
{
	public GameObject IconAbility;
	public Toggle ToggleAbility;
	public GameObject CurrentAbilitiesPanel;
	public GameObject Abilities;
	public GameObject[] Toggles;
	public GameObject CastPrefab;
	public GameObject ManaCost;
	public GameObject BaffPrefab;

	private GameObject _baffPrefab;
	public bool _canCast = true;
	private Coroutine _coroutine;
	private bool isLight = false;


	void Update()
	{
		if (ToggleAbility.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Alpha5) && transform.parent.GetComponent<PlayerMove>().IsSelect)
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

                _coroutine = StartCoroutine(Cast(1.5f));
        }

    }
    private IEnumerator Cast(float castTime)
	{
		_canCast = false;
		transform.parent.GetComponent<PlayerMove>().CanMove = false;

		ManaCost.SetActive(true);
		ManaCost.GetComponent<VisualManaCost>().CheckManaCost();
		ManaCost.transform.localScale = new Vector2(2f, ManaCost.gameObject.transform.localScale.y);
        GetComponent<OneRangeAttack>().ReverseAbility(isLight);
        isLight = !isLight;
        yield return new WaitForSeconds(castTime);
        CreateBaffPrefab();

        transform.parent.GetComponent<ManaPlayer>().UseMana(20);
        ManaCost.SetActive(false);
		transform.parent.GetComponent<PlayerMove>().CanMove = true;
		ToggleAbility.enabled = true;
		_canCast= true;
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
