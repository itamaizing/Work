using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TalentManager : MonoBehaviour
{
	[SerializeField] private RectTransform _panelsParent;
	[SerializeField] private TalentColumn _talentColumn;
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

		//Init();
	}

	public TalentColumn AddPanel(TalentSystem talent)
	{
		var panel = Instantiate(_talentColumn, _panelsParent);
		panel.Init(talent);
		panel.transform.DOScale(0, 0);
		button.onClick.AddListener(panel.SwitchActiveUI);
		//_panels.Add(panel);
		return panel;
	}
}
