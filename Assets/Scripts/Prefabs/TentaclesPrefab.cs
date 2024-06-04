using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Players.CircleBackgroundColor;
using UnityEngine;
using Color = UnityEngine.Color;

public class TentaclesPrefab : MonoBehaviour
{
	public DrawCircle drawCircle;

	private float _radiusCircle = 3f * 1.9f - 1.9f / 2f;
	private List<GameObject> _enemies = new List<GameObject>();

	// void Start()
	// {
	//     drawCircle.Draw(_radiusCircle);
	//     
	// }
	//
	// public void Clear()
	// {
	//     drawCircle.Clear();
	// }
	//
	// private void Update()
	// {
	//     FindEnemy();
	// }

	private void FindEnemy()
	{
		Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _radiusCircle);
		string[] targetTags = { "Enemies", "Allies" };

		if (_enemies == null)
		{
			_enemies = new List<GameObject>();
		}

		_enemies.Clear();

		foreach (Collider2D collider in colliders)
		{
			foreach (string targetTag in targetTags)
			{
				if (collider.CompareTag(targetTag) && collider.GetComponent<MoveComponent>())
				{
					//collider.GetComponent<SelectComponent>().Highlight();
					_enemies.Add(collider.gameObject);
					collider.transform.GetChild(0).GetComponent<ControllerCircleBackgroundColor>()
						.SetColorCircleBackgroundPlayer(collider);
				}
			}
		}

		// �������� ������ ��� ������, ������� ����� �������
		List<GameObject> enemiesToRemove = new List<GameObject>();

		foreach (GameObject enemy in _enemies)
		{
			float distanceToCollider = Vector2.Distance(transform.position, enemy.transform.position);

			if (distanceToCollider > _radiusCircle)
			{
				enemiesToRemove.Add(enemy);
			}
		}

		// �������� ������ �� ������
		foreach (GameObject enemyToRemove in enemiesToRemove)
		{
			enemyToRemove.transform.GetChild(0).gameObject.SetActive(false);
			//enemyToRemove.GetComponent<SelectComponent>().Highlight();
			_enemies.Remove(enemyToRemove);
		}

		if (_enemies.Count == 0)
		{
			drawCircle.SetColor(Color.red);
		}
		else if (_enemies.Count > 0)
		{
			drawCircle.SetColor(Color.green);
        }
	}
}