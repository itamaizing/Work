using DG.Tweening;
using Mirror;
using UnityEngine;

public class MenuSpawnPlayer : MonoBehaviour
{
    [SerializeField] private UIMenuMainCharactersPanel _characterPanel;
    [SerializeField] private Transform _placeToSpawn;
    [SerializeField] private float _scaleMultiplier = 2;

    private GameObject _currentHero;

    private void Awake()
    {
        _characterPanel.OnHeroChanged += SpawnPlayer;
        if (_currentHero != null)
        {
            Destroy(_currentHero.gameObject);
        }
    }

    private void OnDisable()
    {
        _characterPanel.OnHeroChanged -= SpawnPlayer;
        if(_currentHero != null)
        {
            Debug.Log("Destroyed");
            Destroy(_currentHero.gameObject);
        }
    }

    [Client]
    private void SpawnPlayer(HeroComponent hero)
    {
        if(_currentHero != null)
        {
            Destroy(_currentHero.gameObject);
        }

        GameObject spawnedHero = Instantiate(hero.MenuPreview, _placeToSpawn.position, Quaternion.Euler(0, 180, 0));
        spawnedHero.gameObject.transform.localScale = Vector3.one * _scaleMultiplier;
        _currentHero = spawnedHero;
    }


}
