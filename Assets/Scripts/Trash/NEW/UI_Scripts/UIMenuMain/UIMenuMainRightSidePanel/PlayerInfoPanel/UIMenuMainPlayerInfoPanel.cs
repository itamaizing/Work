using TMPro;
using UnityEngine;

public class UIMenuMainPlayerInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _bottleText;

    public static UIMenuMainPlayerInfoPanel Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (BottleUserManager.Instance != null)
        {
            BottleUserManager.Instance.OnBottlesChanged += SetBottleInfo;
            SetBottleInfo(BottleUserManager.Instance.GetCurrentBottles());
        }
    }

    private void OnDisable()
    {
        if (BottleUserManager.Instance != null)
        {
            BottleUserManager.Instance.OnBottlesChanged -= SetBottleInfo;
        }
    }

    public void SetBottleInfo(int count)
    {
        Debug.Log("SetBottleInfo");
        _bottleText.text = $"{count}";
    }
}
