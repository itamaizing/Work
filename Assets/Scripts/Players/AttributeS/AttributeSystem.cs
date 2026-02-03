using System.Collections.Generic;
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

    private List<Attributes> _attributes = new();
    private bool _isInited = false;
    private int _points = 0;

    public Attributes Health => _health;
    public Attributes HpRegen => _hpRegen;
    public Attributes Resourse => _resourse;
    public Attributes ResourseRegen => _resourseRegen;
    public Attributes MoveSpeed => _moveSpeed;
    public Attributes PhysicResist => _physicResist;
    public Attributes MagicResist => _magicResist;
    public Attributes PhysicEvade => _physicEvade;
    public Attributes MagicEvade => _magicEvade;

    public List<Attributes> Attributes => _attributes;

    public int Points => _points;

    public void Init(CharacterData data)
    {
        if (_isInited) return;
        _data = data;
        _health = _data.GetAttribute(AttributeNames.Health);
        _hpRegen = _data.GetAttribute(AttributeNames.HpRegen);
        _resourse = _data.GetAttribute(AttributeNames.Mana);
        _resourseRegen = _data.GetAttribute(AttributeNames.ResourseRegen);
        _moveSpeed = _data.GetAttribute(AttributeNames.Speed);
        _physicEvade = _data.GetAttribute(AttributeNames.EvasionPhysical);
        _physicResist = _data.GetAttribute(AttributeNames.PhysicResist);
        _magicResist = _data.GetAttribute(AttributeNames.MagicResist);
        _magicEvade = _data.GetAttribute(AttributeNames.MagicEvade);

        _attributes.Add(_health);
        _attributes.Add(_hpRegen);
        _attributes.Add(_resourse);
        _attributes.Add(_resourseRegen);
        _attributes.Add(_moveSpeed);
        _attributes.Add(_physicEvade);
        _attributes.Add(_physicResist);
        _attributes.Add(_magicResist);
        _attributes.Add(_magicEvade);

        _isInited = true;

        Debug.Log("Init");

        foreach (var attribute in _attributes)
        {
            List<AttributeModifiers> modifs =  SaveManager.Instance.LoadAttribute(attribute);
            Debug.Log(modifs.Count);
            foreach (var modifier in modifs)
            {
                Debug.Log(modifier.Value + attribute.Name);
                attribute.AddModifier(modifier);
            }
        }
    }

    public void AddPoints(int point)
    {
        _points += point;
    }
}
