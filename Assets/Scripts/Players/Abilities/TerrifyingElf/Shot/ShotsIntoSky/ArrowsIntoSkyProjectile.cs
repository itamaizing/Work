using Mirror;
using UnityEngine;

public class ArrowsIntoSkyProjectile : NetworkBehaviour
{
    [SerializeField] private float impactLifeTime = 2;
    [SerializeField] private SphereCollider damageCollider;
    [SerializeField] private GameObject arrow;
    [SerializeField] private GameObject circle;

    protected HeroComponent _dad;
    protected Skill _skill;

    public GameObject Arrow { get => arrow; set => arrow = value; }
    public GameObject Circle { get => circle; set => circle = value; }
    public SphereCollider DamageCollider { get => damageCollider; set => damageCollider = value; }

    public virtual void Init(HeroComponent dad, Skill skill)
    {
        _dad = dad;
        _skill = skill;
    }
    public void Activate()
    {
        Arrow.SetActive(true);
        circle.SetActive(true);

        Destroy(gameObject, impactLifeTime);
    }
}
