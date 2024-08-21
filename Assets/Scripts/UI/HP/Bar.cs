using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class Bar : MonoBehaviour
{
    [SerializeField]
    protected Slider _bar;

	[CanBeNull]
	[SerializeField]
	protected Slider _barMinus;

	[CanBeNull]
    [SerializeField]
    protected TextMeshPro _barText;
	
	public abstract void UpdateValue(float hp, float maxHp);
}
