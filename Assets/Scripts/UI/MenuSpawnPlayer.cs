using DG.Tweening;
using UnityEngine;

public class MenuSpawnPlayer : MonoBehaviour
{
    [SerializeField] private UIMenuMainCharactersPanel _characterPanel;
    [SerializeField] private Transform _placeToSpawn;
    [SerializeField] private float _scaleMultiplier = 2;

    private HeroComponent _currentHero;

    private void Awake()
    {
        _characterPanel.OnHeroChanged += SpawnPlayer;
    }

    private void OnDisable()
    {
        _characterPanel.OnHeroChanged -= SpawnPlayer;
    }

    private void SpawnPlayer(HeroComponent hero)
    {
        if(_currentHero != null)
        {
            Destroy(_currentHero.gameObject);
        }

        HeroComponent spawnedHero = Instantiate(hero, _placeToSpawn.position, Quaternion.Euler(0, 180, 0));
        spawnedHero.gameObject.transform.localScale = Vector3.one * _scaleMultiplier;
        _currentHero = spawnedHero;
    }
}
