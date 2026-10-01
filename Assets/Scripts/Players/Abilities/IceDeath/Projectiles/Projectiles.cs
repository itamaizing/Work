using Mirror;
using UnityEngine;

public enum Sfx_Projectile
{
    Start,
    FlyLoop,
    Hit,
    End,
    Custom,
}

public class Projectiles : NetworkBehaviour
{
    [SerializeField] protected GameObject _hitEffect;
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected Rigidbody _rb;
    [SerializeField] protected float _force = 0;
    [SerializeField] protected float _distance = 5;
    [SerializeField] protected SoundComponent<Sfx_Projectile> _sound = new();
    protected Character _dad;
    protected Skill _skill;
    protected Energy _energy;
    protected RuneComponent _rune;
    protected bool _initialized = false;
    protected float _energyDad = 0;
    protected bool _lastHit = false;

    private AudioHandle _flyHandle;

    public Skill Skill => _skill;
    public Rigidbody Rigidbody {get => _rb; set => _rb = value;}

    public virtual void Init(Character dad, float energy, bool lastHit, Skill skill)
    {
       _dad = dad;
       _energyDad = energy;
       _initialized = true;
       _lastHit = lastHit;
       _skill = skill;
       _rb.AddForce(transform.forward * _force, ForceMode.Impulse);
        if (_dad.Resources.TryGetValue(ResourceType.Energy, out var res))
          _energy = (Energy) res;
        if (_dad.Resources.TryGetValue(ResourceType.Rune, out res))
            _rune = (RuneComponent)res;
    }

    [ClientRpc]
    protected void TargetRpcDamageMake(float value)
    {
        _energy.SumDamageMake(value);
       _rune.SumDamageMake(value);
    }

    #region Sound

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        PlayLocalFollow(Sfx_Projectile.Start, SfxPlayMode.Once);

        _flyHandle = PlayLocalFollow(Sfx_Projectile.FlyLoop, SfxPlayMode.Loop);
        if (_flyHandle != null)
        {
            _flyHandle.Released -= OnFlyHandleReleased;
            _flyHandle.Released += OnFlyHandleReleased;
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        StopFlyLoop();
    }

    private AudioHandle PlayLocalFollow(Sfx_Projectile type, SfxPlayMode mode)
    {
        var local = AudioManager.Local;
        if (local == null) return null;

        var asset = _sound.GetRandom(type);
        if (asset == null) return null;

        var data = _sound.BuildAudioData(asset, mode: mode);
        return local.Play(data, followTarget: transform);
    }

    private void OnFlyHandleReleased(AudioHandle handle)
    {
        if (_flyHandle == handle) _flyHandle = null;
    }

    protected void StopFlyLoop()
    {
        if (_flyHandle == null) return;

        var handle = _flyHandle;
        _flyHandle = null;
        handle.Released -= OnFlyHandleReleased;
        handle.Stop();
    }

    protected void PlaySfx(Sfx_Projectile type, Vector3? pos = null)
    {
        if (AudioManager.Network == null) return;

        var asset = _sound.GetRandom(type);
        if (asset == null) return;

        AudioManager.Network.Play(_sound.BuildAudioData(asset, pos: pos ?? transform.position));
    }

    protected void PlayHitSound(Vector3? pos = null) => PlaySfx(Sfx_Projectile.Hit, pos);

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        _sound?.EditorValidate();
    }
#endif

    #endregion
}