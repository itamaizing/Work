using System.Collections.Generic;
using UnityEngine;

public class SpamGuard
{
    private readonly int _slots;
    private readonly float _window;
    private readonly Queue<float> _sent = new Queue<float>();
    private int _level;
    private float _blockedUntil;
    private float _lastNotice = float.NegativeInfinity;

    public SpamGuard(int slots = 3, float window = 3f)
    {
        _slots = Mathf.Max(1, slots);
        _window = Mathf.Max(0.1f, window);
    }

    public float Remaining => Mathf.Max(0f, _blockedUntil - Time.unscaledTime);
    public bool IsBlocked => Remaining > 0f;

    public string NoticeText => $"Слишком часто. Отправка недоступна ещё {Mathf.CeilToInt(Remaining)} с";

    public bool ShouldNotify()
    {
        float now = Time.unscaledTime;
        if (now >= _blockedUntil || now - _lastNotice < 1f) return false;

        _lastNotice = now;
        return true;
    }

    public bool TrySend()
    {
        float now = Time.unscaledTime;
        if (now < _blockedUntil) return false;

        if (_level > 0 && now >= _blockedUntil + _window)
        {
            _level = 0;
            _sent.Clear();
        }

        while (_sent.Count > 0 && now - _sent.Peek() >= _window)
            _sent.Dequeue();

        bool punish = _level > 0 || _sent.Count >= _slots;

        _sent.Enqueue(now);

        if (punish)
        {
            _level++;
            _blockedUntil = now + BlockDuration(_level);
        }
        return true;
    }

    public void Reset()
    {
        _sent.Clear();
        _level = 0;
        _blockedUntil = 0f;
        _lastNotice = float.NegativeInfinity;
    }

    private static float BlockDuration(int level) => level <= 1 ? 1f : 2f * (level - 1);
}