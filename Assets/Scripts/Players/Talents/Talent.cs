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
	}

	public abstract void Enter();

	public abstract void Exit();

	public void SetActive(bool isActive)
	{
		_data.IsOpen = isActive;

		if (isActive)
		{
			Enter();
		}
		else
		{
			Exit();
		}
	}
}
