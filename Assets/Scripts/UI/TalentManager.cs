using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TalentManager : MonoBehaviour
{
	[SerializeField] private RectTransform panelParent;
	[SerializeField] private RectTransform characterParent;
	[SerializeField] private TalentsPanel talentsPanel;
	[SerializeField] private Button button;
	
	public static TalentManager Instance;

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
		}
		else
		{
			Instance = this;
		}
	}

	public void AddPanel(TalentsComponent talents, PlayerIcon playerIcon)
	{
		var panel = Instantiate(talentsPanel, transform);
		panel.Init(talents);
		panel.transform.DOScale(0, 0);
		button.onClick.AddListener(panel.SwitchActiveUI);
		
		var ico = Instantiate(playerIcon, characterParent.transform);
		ico.Init(talents.CharacterData.Icon, panel.SwitchActiveUI);
	}
}
