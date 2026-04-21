using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SlidingMenu : MonoBehaviour
{
    [SerializeField] private Button _slideButton;
    [SerializeField] private GameObject _sideMenu;
    [SerializeField] private GameObject _slidingObjects;

    private bool _isActive = false;

    private void Awake()
    {
        _slideButton.onClick.AddListener(Slide);
        _sideMenu.SetActive(false);
    }

    private void Slide()
    {
        if(_sideMenu.activeSelf)
        {
            _slidingObjects.transform.DOLocalMoveX(0, .2f).OnComplete(() => _sideMenu.SetActive(false));
        }
        else
        {
            _sideMenu.SetActive(true);
            _slidingObjects.transform.DOLocalMoveX(-400, .2f);
        }
    }
}
