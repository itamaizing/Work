using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class LightningMovement : Ability
{
    [Header("Ability properties")]
    [SerializeField] private float _leapRange;
    [SerializeField] private float _durationOfLeap;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private GameObject pointForSecondLeap;
    [SerializeField] private VisualRender abilityRender;
    private Vector2 _mousePosition;
    private bool _enabled = false;
    public LayerMask _enemyLayerMask;
    
    // First leap
    private float actualFirstLeapRange;
    private Vector2 _firstLeapPositionForObstacles;
    private Vector2 _leapPositionForFirstVector;
    private bool _canSelectedFirstVector = true;
    private bool _isReadyFirstLeap = true;
    private bool _firstVectorSelected = false;
    //Second leap
    private float actualSecondLeapRange;
    private Vector2 _secondLeapPositionForObstacles;
    private Vector2 _leapPositionForSecondVector;
    private bool _canSelectedSecondVector = true;
    private bool _isReadySecondLeap = true;
    private bool _secondVectorSelected = false;

    public bool isEnemy = false;

    private new void Start()
    {
        pointForSecondLeap.transform.position = PlayerMove.transform.position;
    }

    private void Update()
    {
        if (!_enabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!_firstVectorSelected)
            {
                SelectFirstVectorLeap();
                Debug.Log(isEnemy + " Update: is enemy param");
            }
            else if (!_secondVectorSelected && isEnemy) 
            {
                SelectSecondVectorLeap();
            }
            if (_firstVectorSelected && !isEnemy)
            {
                PayCost();
                SingleLeap();
            }
            else if (_firstVectorSelected && _secondVectorSelected)
            {
                PayCost();
                ExecuteDoubleLeap();
                StopCoroutine(TemporaryMoveToFirstLeapPoint(_leapPositionForSecondVector));
            }
        }
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
            StopCoroutine(TemporaryMoveToFirstLeapPoint(_leapPositionForSecondVector));
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
    }

    private void AfterLeap()
    {
        // false
        _playerLinks.Rb.isKinematic = false;
        _firstVectorSelected = false;
        _secondVectorSelected = false;
        isEnemy = false;
        // true
        _playerLinks.Move.enabled = true;
        _canSelectedFirstVector = true;
        _canSelectedSecondVector = true;
        _isReadyFirstLeap = true;
        _isReadySecondLeap = true;
        _isReady = true;
    }  
    private bool CheckObstacleBetween(Vector3 startPosition, Vector3 endPosition)
    {
        // проверка на наличие припятствия
        Vector2 direction = (endPosition - startPosition).normalized;
        float distance = Vector2.Distance(startPosition, endPosition);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(startPosition, new Vector2(0.5f, 4f), 0f, direction, distance, _obstacleLayerMask);
        foreach (RaycastHit2D hit in hits)
        {
            _firstLeapPositionForObstacles = hits[0].point - direction;
            _secondLeapPositionForObstacles = hits[0].point - direction; 
            return true;
        }
        return false;
    }

    private bool CheckEnemy(Vector3 startPosition, Vector3 endPosition)
    {
        RaycastHit2D hitEnemy = Physics2D.Linecast(startPosition, endPosition, _enemyLayerMask);
        return hitEnemy.collider != null;
    }

    private void SelectFirstVectorLeap()
    {
        if (_canSelectedFirstVector && _isReadyFirstLeap)
        {
            _playerLinks.Rb.isKinematic = true;
            _firstVectorSelected = true;
            _canSelectedFirstVector = false;
            _isReadyFirstLeap = false;

            actualFirstLeapRange = _leapRange;

            Vector2 mousePositionFirstVector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 _lookDirectionForFirstVector = (mousePositionFirstVector - _playerLinks.Rb.position).normalized;

            actualFirstLeapRange *= GlobalVariable.cellSize;
            _leapPositionForFirstVector = (_lookDirectionForFirstVector * actualFirstLeapRange) + (Vector2)PlayerMove.transform.position;
            // Проверка на препятствие для первого прыжка, если оно есть, то останавливаемся перед препятствием
            if (CheckObstacleBetween(PlayerMove.transform.position, _leapPositionForFirstVector))
            {
                _leapPositionForFirstVector = _firstLeapPositionForObstacles;
            }
            if (CheckEnemy(PlayerMove.transform.position, _leapPositionForFirstVector))
            {
                isEnemy = true;
            }
            //Остановка отрисовки первой позиции
            abilityRender.StopDraw();
            //Точка для отрисовки второго прыжка
            StartCoroutine(TemporaryMoveToFirstLeapPoint(_leapPositionForFirstVector));
        }
    }

    private IEnumerator TemporaryMoveToFirstLeapPoint(Vector2 firstLeapPosition)
    {
        if (isEnemy)
        {
            // Сохранение позиции точки первого прыжка
            Vector2 originalPosition = pointForSecondLeap.transform.position;
            // Точка для отрисовки второго прыжка
            pointForSecondLeap.transform.position = firstLeapPosition;

            // Отрисовка второй области
            abilityRender.Drawn(this);

            // Ожидание выбора второй точки прыжка
            yield return new WaitUntil(() => _secondVectorSelected);

            // Возвращение точки в исходное положение и прыжок игрока по двум точкам
            pointForSecondLeap.transform.position = originalPosition;
        }
    }

    private void SelectSecondVectorLeap()
    {
        Debug.Log(pointForSecondLeap.transform.position + " точка второго прыжка");
        if (_canSelectedSecondVector && _isReadySecondLeap)
        {
            _playerLinks.Rb.isKinematic = true;
            _secondVectorSelected = true;
            _canSelectedSecondVector = false;
            _isReadySecondLeap = false;

            actualSecondLeapRange = _leapRange;

            Vector2 mousePositionSecondVector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 _lookDirectionForSecondVector = (mousePositionSecondVector - _leapPositionForFirstVector).normalized;

            actualSecondLeapRange *= GlobalVariable.cellSize;
            _leapPositionForSecondVector = _lookDirectionForSecondVector * actualSecondLeapRange + _leapPositionForFirstVector;
            // Проверка на препятствие для второго прыжка, если оно есть, то останавливаемся перед препятствием
            if (CheckObstacleBetween(_leapPositionForFirstVector, _leapPositionForSecondVector))
            {
                _leapPositionForSecondVector = _secondLeapPositionForObstacles;
            }
            abilityRender.StopDraw();
        }
    }

    private void SingleLeap()
    {
        _playerLinks.Move.enabled = false;
        _enabled = true;
        _playerLinks.Rb.DOMove(_leapPositionForFirstVector, _durationOfLeap * actualFirstLeapRange / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(AfterLeap);
        Debug.Log("Single leap work");
    }

    private void ExecuteDoubleLeap()
    {
        if (_firstVectorSelected && _secondVectorSelected)
        {
            _playerLinks.Move.enabled = false;
            _enabled = true;
            DG.Tweening.Sequence leapSequence = DOTween.Sequence();
            leapSequence.Append(_playerLinks.Rb.DOMove(_leapPositionForFirstVector, _durationOfLeap * actualFirstLeapRange / GlobalVariable.cellSize).SetEase(Ease.Linear));
            leapSequence.AppendCallback(() =>
                {
                    if (!CheckEnemy(PlayerMove.transform.position, _leapPositionForFirstVector) && !isEnemy)
                    {
                        leapSequence.Kill();
                        AfterLeap();
                        Debug.Log(CheckEnemy(PlayerMove.transform.position, _leapPositionForFirstVector) + " Check enemy in leapCallback");
                    }
                });
            leapSequence.AppendCallback(() =>
            {
                if (IsEnemyBehindPlayer(_playerLinks.transform.position, _leapPositionForFirstVector) && isEnemy)
                {
                    leapSequence.Append(_playerLinks.Rb.DOMove(_leapPositionForSecondVector, _durationOfLeap * actualSecondLeapRange / GlobalVariable.cellSize).SetEase(Ease.Linear));
                    Debug.Log(IsEnemyBehindPlayer(_playerLinks.transform.position, _leapPositionForFirstVector) + " isEnemyBehind in leapCallback");
                }
                else
                {
                    leapSequence.Kill();
                    AfterLeap();
                }
            });
            leapSequence.OnComplete(AfterLeap);
        }
    }

    private bool IsEnemyBehindPlayer(Vector2 playerPosition, Vector2 firstLeapEndPosition)
    {
        Vector2 directionToFirstLeapEnd = (firstLeapEndPosition - playerPosition).normalized;
        Vector2 perpendicularDirection = new Vector2(-directionToFirstLeapEnd.y, directionToFirstLeapEnd.x);
        float checkDistance = actualFirstLeapRange * GlobalVariable.cellSize;

        RaycastHit2D hit = Physics2D.Raycast(playerPosition, -directionToFirstLeapEnd, checkDistance, _enemyLayerMask);
        return hit.collider != null;
    }
}
