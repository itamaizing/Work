using UnityEngine;

public class AttributeSystem : MonoBehaviour
{
    private Attributes _health;
    private Attributes _hpRegen;
    private Attributes _resourse;
    private Attributes _resourseRegen;
    private Attributes _moveSpeed;


    public void Init()
    {

    }

    public Attributes GetValue()
    {
        return _health; 
    }
}
