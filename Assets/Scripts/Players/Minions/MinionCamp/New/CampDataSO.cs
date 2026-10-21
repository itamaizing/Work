using System.Collections.Generic;
using UnityEngine;

//Настройки лагеря: список волн, время до первого спавна и базовый порог сдачи
[CreateAssetMenu(menuName = "Game/Camps/Camp Data", fileName = "CampSO")]
public class CampDataSO : ScriptableObject
{
    [SerializeField] private List<CampWave> _waves = new() { new CampWave() };

    [Tooltip("Время до первого появления лагеря (если режим не задал своё)")]
    [SerializeField, Min(0f)] private float _timeToFirstSpawn = 30f;

    [Tooltip("Базовый порог сдачи: доля общего ХП лагеря. Рантайм-значение хранится в MinionCamp.")]
    [SerializeField, Range(0f, 1f)] private float _surrenderThreshold = 0.3f;

    public IReadOnlyList<CampWave> Waves => _waves;
    public float TimeToFirstSpawn => _timeToFirstSpawn;
    public float SurrenderThreshold => _surrenderThreshold;
    
    public CampWave GetWave(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, _waves.Count - 1);
        return _waves[index];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_waves == null || _waves.Count == 0)
        {
            Debug.LogWarning($"[{name}] Нет ни одной волны", this);
            return;
        }

        for (int i = 0; i < _waves.Count; i++)
        {
            var wave = _waves[i];
            if (wave == null) { Debug.LogWarning($"[{name}] Волна {i + 1} не создана", this); continue; }

            ValidateUnit(wave.Leader, $"Волна {i + 1}: лидер");

            if (wave.Creeps == null) continue;
            for (int c = 0; c < wave.Creeps.Count; c++)
            {
                var creep = wave.Creeps[c];
                ValidateUnit(creep.Unit, $"Волна {i + 1}: рядовой {c}");
                if (creep.Count <= 0)
                    Debug.LogWarning($"[{name}] Волна {i + 1}: рядовой {c} с Quantity <= 0", this);
            }
        }
    }

    private void ValidateUnit(UnitData unit, string label)
    {
        if (unit.Prefab == null)
            Debug.LogWarning($"[{name}] {label}: не задан префаб", this);
        else if (!unit.Prefab.TryGetComponent<MinionComponent>(out _))
            Debug.LogWarning($"[{name}] {label}: на префабе нет MinionComponent", this);
    }
#endif
}
