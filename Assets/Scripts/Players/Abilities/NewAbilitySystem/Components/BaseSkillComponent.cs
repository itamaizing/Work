public abstract class BaseSkillComponent
{
    #region Dependencies and Init
    // Обратные ссылки на контекст.
    // По-хорошему вообще должны не пригодиться
    // Система зарядов не должна зависит от того, на чем она висит
    protected Skill _skill;
    protected Character _character;
    protected Resource _resource;
    protected StatsBuff _skillBuffs;
    protected SkillAttributes _skillAttributes;
    protected AttributeSystem _attributes;

    public virtual void Init(Skill skill)
    {
        _skill = skill;
        _character = skill.Hero;
        _resource = _character.Resource;
        _attributes = _character.AttributeSystem;
        _skillBuffs = _skill.Buff;
        _skillAttributes = _skill.Attributes;
    }
    #endregion
}