using UnityEngine;

public class SelectComponent : MonoBehaviour
{
    private bool isSelect=false;

    public bool IsSelect
    {
        get
        {
            return isSelect;
        }
        set
        {
            isSelect = value;
        }
    }
}
