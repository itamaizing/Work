using UnityEngine;

public abstract class Talent : MonoBehaviour
{
	[SerializeField]
	private TalentData _data;

	[SerializeReference, SubclassSelector]
	public OpenCondition OpenCondition;

    public Character character;

	public TalentData Data => _data;

	private void OnValidate()
	{
		_data.Name = GetType().Name;
		if(OpenCondition == null)
		{
			OpenCondition = new EmptyCondition();
		}
        _data.condition = OpenCondition;
        _data.ConditionDescription = OpenCondition.ConditionDescription();
		//Debug.Log("Open condition " + OpenCondition.CanOpen);
	}

	public abstract void Enter();

	public abstract void Exit();

	public void SetActive(bool isActive, int lvl = -1)
	{
		_data.IsOpen = isActive;
		_data.Level = lvl;
		if (isActive && OpenCondition.CanOpen)
		{
			Enter();
		}
		else
		{
			Exit();
		}
	}
}
