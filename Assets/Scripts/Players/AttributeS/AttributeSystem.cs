using UnityEngine;

public class AttributeSystem : MonoBehaviour
{
    private CharacterData _data;

    private Attributes _health;
    private Attributes _hpRegen;
    private Attributes _resourse;
    private Attributes _resourseRegen;
    private Attributes _moveSpeed;
    private Attributes _physicResist;
    private Attributes _magicResist;
    private Attributes _physicEvade;
    private Attributes _magicEvade;

    public Attributes Health => _health;
    public Attributes HpRegen => _hpRegen;
    public Attributes Resourse => _resourse;
    public Attributes ResourseRegen => _resourseRegen;
    public Attributes MoveSpeed => _moveSpeed;
    public Attributes PhysicResist => _physicResist;
    public Attributes MagicResist => _magicResist;
    public Attributes PhysicEvade => _physicEvade;
    public Attributes MagicEvade => _magicEvade;



    public void Init(CharacterData data)
    {
        _data = data;
        _health = _data.GetAttribute(AttributeNames.Health);
        _hpRegen = _data.GetAttribute(AttributeNames.HpRegen);
        _resourse = _data.GetAttribute(AttributeNames.Mana);
        _resourseRegen = _data.GetAttribute(AttributeNames.ResourseRegen);
        _moveSpeed = _data.GetAttribute(AttributeNames.Speed);
        _physicEvade = data.GetAttribute(AttributeNames.EvasionPhysical);
        _physicResist = data.GetAttribute(AttributeNames.PhysicResist);
        _magicResist = data.GetAttribute(AttributeNames.MagicResist);
        _magicEvade = data.GetAttribute(AttributeNames.MagicEvade);
    }
}
