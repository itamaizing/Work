using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttributeSystem : NetworkBehaviour
{
    //private CharacterData _data;

    //Переделать в Dictionary

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

    public void Init2(CharacterData data)
    {
        if (_isInited) return;
        //_data = data;
        _health = data.GetAttribute(AttributeNames.Health);
        _hpRegen = data.GetAttribute(AttributeNames.HpRegen);
        _resourse = data.GetAttribute(AttributeNames.Mana);
        _resourseRegen = data.GetAttribute(AttributeNames.ResourseRegen);
        _moveSpeed = data.GetAttribute(AttributeNames.Speed);
        _physicEvade = data.GetAttribute(AttributeNames.EvasionPhysical);
        _physicResist = data.GetAttribute(AttributeNames.PhysicResist);
        _magicResist = data.GetAttribute(AttributeNames.MagicResist);
        _magicEvade = data.GetAttribute(AttributeNames.MagicEvade);

        _attributes.Add(_health);
        _attributes.Add(_hpRegen);
        _attributes.Add(_resourse);
        _attributes.Add(_resourseRegen);
        _attributes.Add(_moveSpeed);
        _attributes.Add(_physicEvade);
        _attributes.Add(_physicResist);
        _attributes.Add(_magicResist);
        _attributes.Add(_magicEvade);
        Debug.Log("Init");

        foreach (var attribute in _attributes)
        {
            List<AttributeModifiers> modifs = SaveManager.Instance.LoadAttribute(attribute);
            Debug.Log(modifs.Count);
            foreach (var modifier in modifs)
            {
                Debug.Log(modifier.Value + attribute.Name);
                attribute.AddModifier(modifier);
            }
        }

        _isInited = true;
    }

    public void Init(CharacterData data)
    {
        if (_isInited) return;
        //_data = data;
        _health = data.GetAttribute(AttributeNames.Health);
        _hpRegen = data.GetAttribute(AttributeNames.HpRegen);
        _resourse = data.GetAttribute(AttributeNames.Resourse);
        _resourseRegen = data.GetAttribute(AttributeNames.ResourseRegen);
        _moveSpeed = data.GetAttribute(AttributeNames.Speed);
        _physicEvade = data.GetAttribute(AttributeNames.EvasionPhysical);
        _physicResist = data.GetAttribute(AttributeNames.PhysicResist);
        _magicResist = data.GetAttribute(AttributeNames.MagicResist);
        _magicEvade = data.GetAttribute(AttributeNames.MagicEvade);

        _attributes.Add(_health);
        _attributes.Add(_hpRegen);
        _attributes.Add(_resourse);
        _attributes.Add(_resourseRegen);
        _attributes.Add(_moveSpeed);
        _attributes.Add(_physicEvade);
        _attributes.Add(_physicResist);
        _attributes.Add(_magicResist);
        _attributes.Add(_magicEvade);
        Debug.Log("Init");

        foreach (var attribute in _attributes)
        {
            List<AttributeModifiers> modifs =  SaveManager.Instance.LoadAttribute(attribute);
            Debug.Log(modifs.Count + attribute.Name);
            foreach (var modifier in modifs)
            {
                Debug.Log(modifier.Value + attribute.Name);
                attribute.AddModifier(modifier);
            }
            if(isClient)
                Commands(attribute.Name, modifs);
        }

        _isInited = true;
    }

    public void AddPoints(int point)
    {
        _points += point;
    }

    [Command]
    private void Commands(string name, List<AttributeModifiers> modifs)
    {
        var attribute = _attributes.FirstOrDefault(n => n.Name == name);
        foreach (var modifier in modifs)
        {
            Debug.Log(modifier.Value + attribute.Name);
            attribute.AddModifier(modifier);
        }
    }
}
