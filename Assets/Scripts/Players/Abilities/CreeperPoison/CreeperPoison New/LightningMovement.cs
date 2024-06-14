using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class LightningMovement : Ability
{
    [Header("Ability properties")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _leapRange;
    [SerializeField] private float _durationOfLeap;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private GameObject pointForSecondLeap;
    [SerializeField] private VisualRender abilityRender;
    private Vector2 _mousePosition;
    private bool _enabled = false;
    
    // First leap
    private float actualFirstLeapRange;
    private Vector2 _leapPosition1;
    private Vector2 _leapPositionForFirstVector;
    private Vector2 _lookDirectionForFirstVector;
    private bool _canSelectedFirstVector = true;
    private bool _firstVectorSelected = false;
    private bool _isReadyFirstLeap = true;
    //Second leap
    private float actualSecondLeapRange;
    private Vector2 _secondLeapPosition1;
    private Vector2 _leapPositionForSecondVector;
    private Vector2 _lookDirectionForSecondVector;
    private bool _canSelectedSecondVector = true;
    private bool _secondVectorSelected = false;
    private bool _isReadySecondLeap = true;

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
            }
            else if (!_secondVectorSelected)
            {
                 SelectSecondVectorLeap();
            }
            if (_firstVectorSelected && _secondVectorSelected)
            {
                PayCost();
                
                ExecuteLeap();
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
        PlayerMove.CanMove = true;
        _firstVectorSelected = false;
        _secondVectorSelected = false;
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

        RaycastHit2D[] hits = Physics2D.BoxCastAll(startPosition, new Vector2(2f, 2f), 0f, direction, distance, _obstacleLayerMask);
        foreach (RaycastHit2D hit in hits )
        {
            _leapPosition1 = hits[0].point - direction;
            _secondLeapPosition1 = hits[0].point - direction; 
            return true;
        }
        return false;
    }

    private void SelectFirstVectorLeap()
    {
        if (_canSelectedFirstVector && _isReadyFirstLeap)
        {
            _firstVectorSelected = true;
            _canSelectedFirstVector = false;
            _isReadyFirstLeap = false;

            actualFirstLeapRange = _leapRange;

            Vector2 mousePositionFirstVector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _lookDirectionForFirstVector = (mousePositionFirstVector - _playerLinks.Rb.position).normalized;

            actualFirstLeapRange *= GlobalVariable.cellSize;
            _leapPositionForFirstVector = (_lookDirectionForFirstVector * actualFirstLeapRange) + (Vector2)PlayerMove.transform.position;
            // Проверка на препятствие для первого прыжка, если оно есть, то останавливаемся перед препятствием
            if (CheckObstacleBetween(PlayerMove.transform.position, _leapPositionForFirstVector))
            {
                _leapPositionForFirstVector = _leapPosition1;
            }
            //Остановка отрисовки первой позиции
            abilityRender.StopDraw();
            //Точка для отрисовки второго прыжка
            StartCoroutine(TemporaryMoveToFirstLeapPoint(_leapPositionForFirstVector));
            
        }
    }
    private IEnumerator TemporaryMoveToFirstLeapPoint(Vector2 firstLeapPosition)
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
    private void SelectSecondVectorLeap()
    {
        
        if (_canSelectedSecondVector && _isReadySecondLeap)
        {
            _secondVectorSelected = true;
            _canSelectedSecondVector = false;
            _isReadySecondLeap = false;

            actualSecondLeapRange = _leapRange;

            Vector2 mousePositionSecondVector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _lookDirectionForSecondVector = (mousePositionSecondVector - _leapPositionForFirstVector).normalized;

            actualSecondLeapRange *= GlobalVariable.cellSize;
            _leapPositionForSecondVector = _lookDirectionForSecondVector * actualSecondLeapRange + _leapPositionForFirstVector;
            // Проверка на препятствие для второго прыжка, если оно есть, то останавливаемся перед препятствием
            if (CheckObstacleBetween(_leapPositionForFirstVector, _leapPositionForSecondVector))
            {
                _leapPositionForSecondVector = _secondLeapPosition1;
            }
            abilityRender.StopDraw();
        }
    }

    private void ExecuteLeap()
    {
        if (_firstVectorSelected && _secondVectorSelected)
        {
            _enabled = true;
            PlayerMove.CanMove = false;

            DG.Tweening.Sequence leapSequence = DOTween.Sequence();
                       
            var firstLeap = leapSequence.Append(_playerLinks.Rb.DOMove(_leapPositionForFirstVector, _durationOfLeap * actualFirstLeapRange / GlobalVariable.cellSize).SetEase(Ease.Linear)); 
            var secondLeap = leapSequence.Append(_playerLinks.Rb.DOMove(_leapPositionForSecondVector, _durationOfLeap * actualSecondLeapRange / GlobalVariable.cellSize).SetEase(Ease.Linear));
            
            
            firstLeap.Play();
            secondLeap.Play();
            leapSequence.OnComplete(AfterLeap);
            
        }
    }
}
