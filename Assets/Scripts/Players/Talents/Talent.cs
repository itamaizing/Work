using UnityEngine;

public abstract class Talent : MonoBehaviour
{
    [SerializeField]
    private TalentData _data;

    [SerializeReference, SubclassSelector]
    public OpenCondition OpenCondition = new EmptyCondition();

    public Character character;

    public TalentData Data => _data;
    
    public TalentSystem Owner { get; set; }
    
    private bool? _effectOn;
    private int _effectLevel;

    public void Init()
    {
        _data.Init();
        _data.Name = GetType().Name;
        if (OpenCondition == null)
        {
            OpenCondition = new EmptyCondition();
        }
        _data.condition = OpenCondition;
        _data.ConditionDescription = OpenCondition.ConditionDescription();
    }

    public abstract void Enter();

    public abstract void Exit();
    
    public bool ApplyEffect(bool on)
    {
        if (Owner != null && Owner.IsPreview) return false;

        int level = _data.Level;
        if (_effectOn == on && (!on || _effectLevel == level)) return false;

        if (on && _effectOn == true)
            Exit();

        _effectOn = on;
        _effectLevel = level;

        if (on) Enter();
        else Exit();
        return true;
    }
    
    public void SetActive(bool isActive, int lvl = 0)
    {
        _data.SetOpen(isActive);
        _data.SetLevel(lvl);
        ApplyEffect(isActive && OpenCondition.CanOpen);
    }


    public void AddDependendTalent(TalentData data)
    {
        if(data != null)
            _data.AddDependentTalent(data);
    }
}