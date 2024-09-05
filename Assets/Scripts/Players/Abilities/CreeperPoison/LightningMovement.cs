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
    [SerializeField] private Vector2 _pointForSecondLeap;
    [SerializeField] private VisualRender _abilityRender;
    [SerializeField] private LayerMask _enemyLayerMask;

    private bool _enabled = false;
    private bool _isEnemy = false;
    
    // First leap
    private float _actualFirstLeapRange;
    private Vector2 _firstLeapPositionForObstacles;
    private Vector2 _leapPositionForFirstVector;
    private bool _canSelectedFirstVector = true;
    private bool _isReadyFirstLeap = true;
    private bool _firstVectorSelected = false;
    //Second leap
    private float _actualSecondLeapRange;
    private Vector2 _secondLeapPositionForObstacles;
    private Vector2 _leapPositionForSecondVector;
    private bool _canSelectedSecondVector = true;
    private bool _isReadySecondLeap = true;
    private bool _secondVectorSelected = false;

    private new void Start()
    {
        _pointForSecondLeap = Vector2.zero;
    }

    private void Update()
    {
        if (!_enabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!_firstVectorSelected)
            {
                SelectFirstVectorLeap();
                Debug.Log(_isEnemy + " Update: is enemy param");
            }
            else if (!_secondVectorSelected && _isEnemy) 
            {
                SelectSecondVectorLeap();
            }
            if (_firstVectorSelected && !_isEnemy)
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
        _abilityRender.StopDraw();
    }

    private void AfterLeap()
    {
		// false
		Debug.LogError("fix");
		//_playerLinks.Rigidbody2D.isKinematic = false;

        _firstVectorSelected = false;
        _secondVectorSelected = false;
        _isEnemy = false;
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
        // �������� �� ������� �����������
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
			Debug.LogError("fix");
			//_playerLinks.Rigidbody2D.isKinematic = true;
            _firstVectorSelected = true;
            _canSelectedFirstVector = false;
            _isReadyFirstLeap = false;

            _actualFirstLeapRange = _leapRange;

            Vector2 mousePositionFirstVector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Debug.LogError("fix");
			//Vector2 _lookDirectionForFirstVector = (mousePositionFirstVector - _playerLinks.Rigidbody2D.position).normalized;

            _actualFirstLeapRange *= GlobalVariable.cellSize;
           // _leapPositionForFirstVector = (_lookDirectionForFirstVector * _actualFirstLeapRange) + (Vector2)PlayerMove.transform.position;
            // �������� �� ����������� ��� ������� ������, ���� ��� ����, �� ��������������� ����� ������������
            if (CheckObstacleBetween(PlayerMove.transform.position, _leapPositionForFirstVector))
            {
                _leapPositionForFirstVector = _firstLeapPositionForObstacles;
            }
            if (CheckEnemy(PlayerMove.transform.position, _leapPositionForFirstVector))
            {
                _isEnemy = true;
            }
            //��������� ��������� ������ �������
            _abilityRender.StopDraw();
            //����� ��� ��������� ������� ������
            StartCoroutine(TemporaryMoveToFirstLeapPoint(_leapPositionForFirstVector));
        }
    }

    private IEnumerator TemporaryMoveToFirstLeapPoint(Vector2 firstLeapPosition)
    {
        if (_isEnemy)
        {
            // ���������� ������� ����� ������� ������
            Vector2 originalPosition = _pointForSecondLeap;
            // ����� ��� ��������� ������� ������
            _pointForSecondLeap = firstLeapPosition;
            // ��������� ������ �������
            _abilityRender.Drawn(this);

            // �������� ������ ������ ����� ������
            yield return new WaitUntil(() => _secondVectorSelected);

            // ����������� ����� � �������� ��������� � ������ ������ �� ���� ������
            _pointForSecondLeap = originalPosition;
        }
    }

    private void SelectSecondVectorLeap()
    {
        Debug.Log(_pointForSecondLeap + " ����� ������� ������");
        if (_canSelectedSecondVector && _isReadySecondLeap)
        {
			Debug.LogError("fix");
			//_playerLinks.Rigidbody2D.isKinematic = true;

            _secondVectorSelected = true;
            _canSelectedSecondVector = false;
            _isReadySecondLeap = false;

            _actualSecondLeapRange = _leapRange;

            Vector2 mousePositionSecondVector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 _lookDirectionForSecondVector = (mousePositionSecondVector - _leapPositionForFirstVector).normalized;

            _actualSecondLeapRange *= GlobalVariable.cellSize;
            _leapPositionForSecondVector = _lookDirectionForSecondVector * _actualSecondLeapRange + _leapPositionForFirstVector;
            // �������� �� ����������� ��� ������� ������, ���� ��� ����, �� ��������������� ����� ������������
            if (CheckObstacleBetween(_leapPositionForFirstVector, _leapPositionForSecondVector))
            {
                _leapPositionForSecondVector = _secondLeapPositionForObstacles;
            }
            _abilityRender.StopDraw();
        }
    }

    private void SingleLeap()
    {
        _playerLinks.Move.enabled = false;
        _enabled = true;
		Debug.LogError("fix");
		//_playerLinks.Rigidbody2D.DOMove(_leapPositionForFirstVector, _durationOfLeap * _actualFirstLeapRange / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(AfterLeap);
        Debug.Log("Single leap work");
    }

    private void ExecuteDoubleLeap()
    {
        if (_firstVectorSelected && _secondVectorSelected)
        {
            _playerLinks.Move.enabled = false;
            _enabled = true;
            DG.Tweening.Sequence leapSequence = DOTween.Sequence();
			Debug.LogError("fix");
			//leapSequence.Append(_playerLinks.Rigidbody2D.DOMove(_leapPositionForFirstVector, _durationOfLeap * _actualFirstLeapRange / GlobalVariable.cellSize).SetEase(Ease.Linear));
            leapSequence.AppendCallback(() =>
                {
                    if (!CheckEnemy(PlayerMove.transform.position, _leapPositionForFirstVector) && !_isEnemy)
                    {
                        leapSequence.Kill();
                        AfterLeap();
                        Debug.Log(CheckEnemy(PlayerMove.transform.position, _leapPositionForFirstVector) + " Check enemy in leapCallback");
                    }
                });
            leapSequence.AppendCallback(() =>
            {
                if (IsEnemyBehindPlayer(_playerLinks.transform.position, _leapPositionForFirstVector) && _isEnemy)
                {
					Debug.LogError("fix");
					//leapSequence.Append(_playerLinks.Rigidbody2D.DOMove(_leapPositionForSecondVector, _durationOfLeap * _actualSecondLeapRange / GlobalVariable.cellSize).SetEase(Ease.Linear));
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
        float checkDistance = _actualFirstLeapRange * GlobalVariable.cellSize;

        RaycastHit2D hit = Physics2D.Raycast(playerPosition, -directionToFirstLeapEnd, checkDistance, _enemyLayerMask);
        return hit.collider != null;
    }
}
