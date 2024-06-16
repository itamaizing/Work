using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
public class LightningMovement2 : Ability
{
    [Header("Ability properties")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _leapRange;
    [SerializeField] private float _durationOfLeap;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private GameObject pointForSecondLeap;
    [SerializeField] private VisualRender abilityRender;
    [SerializeField] private LayerMask _enemyLayerMask; // Новый слой для врагов

    private Vector2 _mousePosition;
    private bool _enabled = false;

    // Leap properties
    private LeapData _firstLeapData = new LeapData();
    private LeapData _secondLeapData = new LeapData();

    private Coroutine _currentCoroutine;

    private new void Start()
    {
        pointForSecondLeap.transform.position = _playerLinks.transform.position;
    }

    private void Update()
    {
        if (!_enabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
                _currentCoroutine = null;
            }
        }
    }

    protected override void Cast()
    {
        _enabled = true;
    }

    protected override void Cancel()
    {
        _enabled = false;
        abilityRender.StopDraw();
        ResetLeapData();
    }

    private void HandleMouseClick()
    {
        if (!_firstLeapData.IsSelected)
        {
            SelectLeapVector(_firstLeapData, _playerLinks.transform.position);

            if (CheckForEnemy(_playerLinks.transform.position, _firstLeapData.Position))
            {
                _firstLeapData.EnemyInPath = true;
            }
        }
        else if (!_secondLeapData.IsSelected && _firstLeapData.EnemyInPath)
        {
            SelectLeapVector(_secondLeapData, _firstLeapData.Position);

            if (_firstLeapData.IsSelected && _secondLeapData.IsSelected)
            {
                PayCost();
                ExecuteLeap();
                
            }
            else
            {
                Cancel();
            }
        }
        else
        {
            ExecuteLeap();
        }
    }

    private void SelectLeapVector(LeapData leapData, Vector2 startPosition)
    {
        if (!leapData.CanBeSelected) return;

        leapData.IsSelected = true;
        leapData.CanBeSelected = false;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        leapData.Direction = (mousePosition - startPosition).normalized;
        leapData.Range = _leapRange * GlobalVariable.cellSize;
        leapData.Position = (leapData.Direction * leapData.Range) + startPosition;

        if (CheckObstacleBetween(startPosition, leapData.Position, out Vector2 obstaclePosition))
        {
            leapData.Position = obstaclePosition;
        }

        abilityRender.StopDraw();

        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(TemporaryMoveToLeapPoint(leapData.Position));
    }

    private IEnumerator TemporaryMoveToLeapPoint(Vector2 leapPosition)
    {
        Vector2 originalPosition = pointForSecondLeap.transform.position;
        pointForSecondLeap.transform.position = leapPosition;
        abilityRender.Drawn(this);

        yield return new WaitUntil(() => _secondLeapData.IsSelected);

        pointForSecondLeap.transform.position = originalPosition;
    }

    private bool CheckObstacleBetween(Vector3 startPosition, Vector3 endPosition, out Vector2 obstaclePosition)
    {
        Vector2 direction = (endPosition - startPosition).normalized;
        float distance = Vector2.Distance(startPosition, endPosition);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(startPosition, new Vector2(2f, 2f), 0f, direction, distance, _obstacleLayerMask);
        if (hits.Length > 0)
        {
            obstaclePosition = hits[0].point - direction;
            return true;
        }

        obstaclePosition = Vector2.zero;
        return false;
    }

    private bool CheckForEnemy(Vector2 startPosition, Vector2 endPosition)
    {
        RaycastHit2D hit = Physics2D.Linecast(startPosition, endPosition, _enemyLayerMask);
        return hit.collider != null;
    }

    private void ExecuteLeap()
    {
        if (!_firstLeapData.IsSelected) return;

        _enabled = false;
        _playerLinks.Move.CanMove = false;

        DG.Tweening.Sequence leapSequence = DOTween.Sequence();

        leapSequence.Append(_playerLinks.Rb.DOMove(_firstLeapData.Position, _durationOfLeap * _firstLeapData.Range / GlobalVariable.cellSize).SetEase(Ease.Linear));

        if (_firstLeapData.EnemyInPath && _secondLeapData.IsSelected)
        {
            leapSequence.Append(_playerLinks.Rb.DOMove(_secondLeapData.Position, _durationOfLeap * _secondLeapData.Range / GlobalVariable.cellSize).SetEase(Ease.Linear));
        }

        leapSequence.OnComplete(AfterLeap);
        leapSequence.Play();
    }

    private void AfterLeap()
    {
        _playerLinks.Move.CanMove = true;
        ResetLeapData();
        _isReady = true;
    }

    private void ResetLeapData()
    {
        _firstLeapData.Reset();
        _secondLeapData.Reset();
    }

    [System.Serializable]
    private class LeapData
    {
        public bool IsSelected;
        public bool CanBeSelected = true;
        public bool EnemyInPath;
        public float Range;
        public Vector2 Direction;
        public Vector2 Position;

        public void Reset()
        {
            IsSelected = false;
            CanBeSelected = true;
            EnemyInPath = false;
        }
    }
}
