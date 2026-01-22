using UnityEngine;

public class AttributeSystem : MonoBehaviour
{
    [SerializeField] private CharacterData _data;

    private Attributes _health;
    private Attributes _hpRegen;
    private Attributes _resourse;
    private Attributes _resourseRegen;
    private Attributes _moveSpeed;

    public Attributes Health => _health;
    public Attributes HpRegen => _hpRegen;
    public Attributes Resourse => _resourse;
    public Attributes ResourseRegen => _resourseRegen;
    public Attributes MoveSpeed => _moveSpeed;


    public void Awake()
    {
        _health = _data.GetAttribute(AttributeNames.Health);
        _hpRegen = _data.GetAttribute(AttributeNames.HpRegen);
        _resourse = _data.GetAttribute(AttributeNames.Mana);
        _resourseRegen = _data.GetAttribute(AttributeNames.ResourseRegen);
        _moveSpeed = _data.GetAttribute(AttributeNames.Speed);
    }
}
