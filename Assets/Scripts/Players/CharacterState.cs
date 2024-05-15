using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Интерфейс состояния
public interface ICharacterState
{
    void EnterState(CharacterState character);
    void UpdateState();
    void ExitState();
}


public class DefaultState : ICharacterState
{
    public void EnterState(CharacterState character)
    {

    }

    public void UpdateState()
    {

    }

    public void ExitState()
    {

    }
}


// Cостояние невидимость
public class InvisibleState : ICharacterState
{
	private CharacterState _characterState;
	private Renderer[] childRenderers;
    private SelectObject _select;
    private GameObject _player;

    private List<GameObject> _enemies = new List<GameObject>();

    private float lastCheckTime;
    private float checkInterval = 1f;

    public void EnterState(CharacterState character)
    {
        Debug.Log("Entering Invisible State");
        _characterState = character;
        _select = character.Select;
        _player = character.gameObject;
    }

    public void UpdateState()
    {
        Debug.Log("Updating Invisible State");

        childRenderers = _characterState.GetComponentsInChildren<Renderer>();
        
        if (_select.SelectedObject.CompareTag("Enemies") && _characterState.gameObject.CompareTag("Allies") ||
            _select.SelectedObject.CompareTag("Allies") && _characterState.gameObject.CompareTag("Enemies"))
        {

            // Выключаем видимость каждого дочернего Renderer
            foreach (Renderer renderer in childRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }
        else
        {
            foreach (Renderer renderer in childRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }

        if (_player.GetComponent<PlayerMove>().IsMoving)
        {
            CheckEnemies();
            //Раз в секунду проверяем дистанцию и шанс быть увиденным
            if (_enemies.Count > 0 && Time.time - lastCheckTime >= checkInterval)
            {
                CheckDistance();
                lastCheckTime = Time.time;
            }
        }
    }

    private void CheckEnemies()
    {
        int otherPlayersLayer = LayerMask.NameToLayer("OtherPlayers");
        string enemiesTag = "Enemies";
        float radius = 3f * 1.94f;
       
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_player.transform.position, radius, 1 << otherPlayersLayer);

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(enemiesTag))
            {
                //Направление врага
                Vector2 enemyMovementDirection = collider.GetComponent<PlayerMove>().DirectionOfMovement * radius;

                // Вектор от врага до плеера
                Vector2 playerToEnemy = _player.transform.position - collider.transform.position;

                // Проверяем, находится ли игрок спереди врага
                float dotProduct = Vector3.Dot(playerToEnemy.normalized, enemyMovementDirection);

                if (dotProduct > 0)
                {
                    _enemies.Add(collider.gameObject);
                }
            }
        }
    }

    private void CheckDistance()
    {
        foreach(GameObject enemy in _enemies)
        {
            Vector2 enemyMovementDirection = enemy.GetComponent<PlayerMove>().DirectionOfMovement;
            Vector2 playerToEnemy = _player.transform.position - enemy.transform.position;

            // Находим перпендикулярный вектор к вектору направления врага и его длину
            Vector2 perpendicularVector = Vector3.ProjectOnPlane(playerToEnemy, enemyMovementDirection);
            float perpendicularDistance = perpendicularVector.magnitude;

            // Находим проекцию вектора playerToEnemy на вектор направления врага и ее длину
            float projection = Vector2.Dot(playerToEnemy, enemyMovementDirection);
            float projectionLength = Mathf.Abs(projection);

            float chanceToBeSeen = 0;

            if(projectionLength <= 1.94f * 1.5f)
            {
                if(perpendicularDistance <= 1.94f * 0.5f)
                {
                    chanceToBeSeen = 0.8f;
                }
                else if(perpendicularDistance <= 1.94f * 1.5f && perpendicularDistance > 1.94f * 0.5f)
                {
                    chanceToBeSeen = 0.7f;
                }
            }
            else if(projectionLength <= 1.94f * 2.5f && projectionLength > 1.94f * 1.5f)
            {
                if (perpendicularDistance <= 1.94f * 0.5f)
                {
                    chanceToBeSeen = 0.3f;
                }
                else if (perpendicularDistance <= 1.94f * 1.5f && perpendicularDistance > 1.94f * 0.5f)
                {
                    chanceToBeSeen = 0.2f;
                }
            }

            if (chanceToBeSeen > 0)
            {
                if(Random.value <= chanceToBeSeen)
                {
                    _player.GetComponent<CharacterState>().AddState(new DefaultState());
                }
            }
        }
    }

    public void ExitState()
    {
        Debug.Log("Exiting Invisible State");
        // При выходе из состояния возвращаем видимость дочерним Renderer
        if (childRenderers != null)
        {
            foreach (Renderer renderer in childRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }
    }
}


// Cостояние оглушение
public class StunnedState : ICharacterState
{
    public bool turnOff = false;

    private CharacterState _characterState;
    public PlayerMove _playerMove;
    private float _duration;
    public void EnterState(CharacterState character)
    {
        Debug.Log("Entering Stunned State");
        _characterState = character;
        _playerMove.CanMove = false;
        _duration = character.durationToExit;
        //ability off
    }

    public void UpdateState()
    {
        Debug.Log("Updating Stunned State");
        _duration -= Time.deltaTime;
        if (_duration < 0 || turnOff)
        {
            ExitState();
        }
    }

    public void ExitState()
    {
        Debug.Log("Exiting Stunned State");
        _playerMove.CanMove = true;
		//ability on
		_characterState.RemoveState(this);
	}
}

// Cостояние ослепление
public class BlindnessState : ICharacterState
{
    public bool turnOff = false;

	private CharacterState _characterState;
	private float _duration;

	public void EnterState(CharacterState character)
    {
        Debug.Log("Entering Stunned State");
        _characterState = character;
		//ability off
	}

	public void UpdateState()
    {
        Debug.Log("Updating Stunned State");
		_duration -= Time.deltaTime;
		if (_duration < 0 || turnOff)
		{
			ExitState();
		}
	}

    public void ExitState()
    {
        Debug.Log("Exiting Stunned State");
        _characterState.RemoveState(this);
		//ability on
	}
}

// Cостояние заморозки
public class FrozenState : ICharacterState
{
	public bool turnOff = false;

	private CharacterState _characterState;
	private HealthPlayer _playerHP;
    private PlayerMove _playerMove;
    private float _duration;
	public void EnterState(CharacterState character)
    {
        Debug.Log("Entering Frozen State");
        _characterState = character;
        _playerMove = _characterState.PlayerMove;
        _playerMove.CanMove = false;
        //character.GetAbilityManager().ToggleAbility(false);//turn off abilities

        _playerHP = _characterState.PlayerHp;
        _playerHP.TakePhisicDamage(10 + _characterState.energy.Value / 4);
        _playerHP.sumDamageTaken = 0;
        //_duration = character.durationToExit;
        _duration = 2 + _characterState.energy.Value / 20; //тут мана того кто стрелял

		_characterState.energy.Use(_characterState.energy.Value);

    }

    public void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_playerHP.sumDamageTaken >= 30 || _duration < 0 || turnOff)
        {
			ExitState();
		}

	}

	public void ExitState()
	{
		Debug.Log("Exiting Frozen State");

		//character.GetAbilityManager().ToggleAbility(true);//turn on abilities
        _playerMove.CanMove = true;
		_characterState.RemoveState(this);
	}
}


public class FrostingState : ICharacterState
{
	public bool turnOff = false;

	private CharacterState _characterState;
	private HealthPlayer _playerHP;
	private PlayerMove _targetMove;
	private float _duration; //переделать под разные спелы
	public void EnterState(CharacterState character)
    {
        Debug.Log("Entering Frosting State");
        _characterState = character;
        _targetMove = _characterState.PlayerMove;

        _targetMove.CanMove = false;
        //decrease speed of attact
        _playerHP = _characterState.PlayerHp;
        //Какой дамаг получаем? физический или магический
        _playerHP.TakePhisicDamage(10 + _characterState.energy.Value / 4);
        _playerHP.sumDamageTaken = 0;
        _duration = _characterState.durationToExit;

		_characterState.energy.Use(_characterState.energy.Value);
    }

	public void UpdateState()
	{
		_duration -= Time.deltaTime;
		if (_playerHP.sumDamageTaken >= 30 || _duration < 0 || turnOff)
		{
			ExitState();
		}
	}

	public void ExitState()
	{
		Debug.Log("Exiting Frozen State");
		_targetMove.CanMove = true;
        //return speed of attact
		_characterState.RemoveState(this);
	}
}

// Класс персонажа, использующий состояния
public class CharacterState : MonoBehaviour
{
    public SelectObject Select;    
    public HealthPlayer PlayerHp;
    public PlayerMove PlayerMove;

    [HideInInspector] public PlayerStamina energy;//person who shoted
	[HideInInspector] public float durationToExit;//duration of state
    [HideInInspector] public float damageToExit; // damaege needed to exit

    [SerializeField] private List<ICharacterState> currentStates = new List<ICharacterState>();

	private void Start()
	{
        if (Select == null || PlayerHp == null || PlayerMove == null)
        {
            Debug.LogError("No required component in " + gameObject.name);
        }
	}

	private void Update()
    {
        // Обновление текущего состояния
        if (currentStates.Count > 0)
        {
            for (int i = 0; i < currentStates.Count; i++) 
            {
                currentStates[i].UpdateState();
            }
        }
    }

    public void AddState(ICharacterState newState)
    {
        // переделать под лист
        //if already has, reset???

        // Вход в новое состояние
        currentStates.Add(newState);
        currentStates[currentStates.Count - 1].EnterState(this);
    }

    public bool IfHasState(ICharacterState newState) 
    {
        if(currentStates.Contains(newState))
        {
            return true;
        }
        else return false;

    }

	public void RemoveState(ICharacterState newState) 
    {
        //newState.ExitState(this);
        currentStates.Remove(newState);
    }

   /* public AbilityManager GetAbilityManager()
    {
        if( _abilityManager == null ) 
        {
            Debug.LogError("No ability manager!");
            return null;
        }
        return _abilityManager;
    }*/
}