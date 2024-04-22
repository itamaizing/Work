using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.RuleTile.TilingRuleOutput;

// Интерфейс состояния
public interface ICharacterState
{
    void EnterState(CharacterState character);
    void UpdateState(CharacterState character);
    void ExitState(CharacterState character);
}


public class DefaultState : ICharacterState
{
    public void EnterState(CharacterState character)
    {
    }

    public void UpdateState(CharacterState character)
    {
    }

    public void ExitState(CharacterState character)
    {
    }
}


// Cостояние невидимость
public class InvisibleState : ICharacterState
{
    private Renderer[] childRenderers;
    private SelectObject _select;
    private GameObject _player;

    private List<GameObject> _enemies = new List<GameObject>();

    private float lastCheckTime;
    private float checkInterval = 1f;

    public void EnterState(CharacterState character)
    {
        Debug.Log("Entering Invisible State");

        _select = character.Select;
        _player = character.gameObject;
    }

    public void UpdateState(CharacterState character)
    {
        Debug.Log("Updating Invisible State");

        childRenderers = character.GetComponentsInChildren<Renderer>();
        
        if (_select.SelectedObject.CompareTag("Enemies") && character.gameObject.CompareTag("Allies") ||
            _select.SelectedObject.CompareTag("Allies") && character.gameObject.CompareTag("Enemies"))
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
                    _player.GetComponent<CharacterState>().ChangeState(new DefaultState());
                }
            }
        }
    }

    public void ExitState(CharacterState character)
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
    public void EnterState(CharacterState character)
    {
        Debug.Log("Entering Stunned State");
    }

    public void UpdateState(CharacterState character)
    {
        Debug.Log("Updating Stunned State");
    }

    public void ExitState(CharacterState character)
    {
        Debug.Log("Exiting Stunned State");
    }
}

// Cостояние ослепление
public class BlindnessState : ICharacterState
{
    public void EnterState(CharacterState character)
    {
        Debug.Log("Entering Stunned State");

        List<Toggle> toggles = character.gameObject.GetComponent<PlayerMove>().AbilitiesOnTargetToggles;

        foreach (Toggle toggle in toggles)
        {
            toggle.enabled = false;
        }
    }

    public void UpdateState(CharacterState character)
    {
        Debug.Log("Updating Stunned State");
    }

    public void ExitState(CharacterState character)
    {
        Debug.Log("Exiting Stunned State");

        List<Toggle> toggles = character.gameObject.GetComponent<PlayerMove>().AbilitiesOnTargetToggles;

        foreach (Toggle toggle in toggles)
        {
            toggle.enabled = true;
        }
    }
}

// Cостояние заморозки
public class FrozenState : ICharacterState
{
	private HealthPlayer _playerHP;
    private PlayerMove _playerMove;
    private float _duration; //переделать под разные спелы
    //remember how much hp, check incoming damage to unfreeze

	public void EnterState(CharacterState character)
	{
		Debug.Log("Entering Frozen State");
		_playerMove = character.gameObject.GetComponent<PlayerMove>();
		List<Toggle> toggles = _playerMove.AbilitiesOnTargetToggles;
		foreach (Toggle toggle in toggles)
		{
			toggle.enabled = false;
        }
		_playerMove.CanMove = false;
        character.GetAbilityManager().ToggleAbility(false);//turn off abilities

		if (character.gameObject.TryGetComponent<HealthPlayer>(out _playerHP))
        {
            //Какой дамаг получаем? физический или магический
            _playerHP.TakePhisicDamage(10 + character.energy.Energy / 4);
            _playerHP.sumDamageTaken = 0;
            _duration = 2 + character.energy.Energy / 20; //тут мана того кто стрелял

            character.energy.UseEnergy(character.energy.Energy); 
        }
        else
        {
            //error
        }
    }

    public void UpdateState(CharacterState character)
	{
		_duration -=Time.deltaTime;
		if (_playerHP.sumDamageTaken >= 30 || _duration < 0)
        {
			character.ChangeState(new DefaultState());
		}

	}

	public void ExitState(CharacterState character)
	{
		Debug.Log("Exiting Frozen State");
		List<Toggle> toggles = _playerMove.AbilitiesOnTargetToggles;

		foreach (Toggle toggle in toggles)
		{
			toggle.enabled = true;
		}

		character.GetAbilityManager().ToggleAbility(true);//turn on abilities
        _playerMove.CanMove = true;
	}
}


public class FrostingState : ICharacterState
{
	private HealthPlayer _playerHP;
	private PlayerMove _playerMove;
	private float _duration; //переделать под разные спелы

	public void EnterState(CharacterState character)
	{
		Debug.Log("Entering Frosting State");
		_playerMove = character.gameObject.GetComponent<PlayerMove>();

		_playerMove.CanMove = false;
        //decrease speed of attact
		if (character.gameObject.TryGetComponent<HealthPlayer>(out _playerHP))
		{
			//Какой дамаг получаем? физический или магический
			_playerHP.TakePhisicDamage(10 + character.energy.Energy / 4);
			_playerHP.sumDamageTaken = 0;
            _duration = character.duration;

			character.energy.UseEnergy(character.energy.Energy);
		}
		else
		{
			//error
		}
	}

	public void UpdateState(CharacterState character)
	{
		_duration -= Time.deltaTime;
		if (_playerHP.sumDamageTaken >= 30 || _duration < 0)
		{
			character.ChangeState(new DefaultState());
		}

	}

	public void ExitState(CharacterState character)
	{
		Debug.Log("Exiting Frozen State");
		_playerMove.CanMove = true;
        //return speed of attact
	}
}

// Класс персонажа, использующий состояния
public class CharacterState : MonoBehaviour
{
    [SerializeField] private ICharacterState currentState;
    [SerializeField] private AbilityManager _abilityManager;
    public SelectObject Select;
    
    
    [HideInInspector] public EnergyPlayer energy;//person who shoted
	[HideInInspector] public float duration;//duration of state

    private void Start()
    {
        currentState = new DefaultState();
        currentState.EnterState(this);
    }
    private void Update()
    {
        // Обновление текущего состояния
        if (currentState != null)
        {
            currentState.UpdateState(this);
        }
    }

    public void ChangeState(ICharacterState newState)
    {
        // Выход из текущего состояния
        if (currentState != null)
        {
            currentState.ExitState(this);
        }

        // Вход в новое состояние
        newState.EnterState(this);
        currentState = newState;
    }

    //проверка текущего состояния
    public ICharacterState CheckState()
    {
        return currentState;
    }

    public AbilityManager GetAbilityManager()
    {
        if( _abilityManager == null ) 
        {
            Debug.LogError("No ability manager!");
            return null;
        }
        return _abilityManager;
    }
}