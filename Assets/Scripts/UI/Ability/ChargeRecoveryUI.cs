using UnityEngine;
using UnityEngine.UI;

public class ChargeRecoveryUI : MonoBehaviour
{
    [SerializeField] private Image _fill;

    public void SetProgress(float progress)
    {
        if (_fill == null)
            return;

        _fill.gameObject.SetActive(true);
        _fill.fillAmount = Mathf.Clamp01(progress);
    }
}