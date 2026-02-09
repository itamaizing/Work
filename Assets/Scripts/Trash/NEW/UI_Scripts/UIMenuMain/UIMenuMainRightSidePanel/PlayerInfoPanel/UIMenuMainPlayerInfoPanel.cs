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
            SetBottleInfo(BottleUserManager.Instance.GetCurrentBottles());
        }
    }

    private void OnEnable()
    {
        if (BottleUserManager.Instance != null)
        {
            BottleUserManager.Instance.OnBottlesChanged += SetBottleInfo;
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
