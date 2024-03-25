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
        string[] targetTags = {"Enemies", "Allies"};

        if (_enemies == null)
        {
            _enemies = new List<GameObject>();
        }

        _enemies.Clear();

        foreach (Collider2D collider in colliders)
        {
            foreach (string targetTag in targetTags)
            {
                if (collider.CompareTag(targetTag) && collider.GetComponent<PlayerMove>())
                {
                    collider.GetComponent<PlayerMove>().CircleSelect.SetActive(true);
                    _enemies.Add(collider.gameObject);
                    collider.transform.GetChild(0).GetComponent<ControllerCircleBackgroundColor>()
                        .SetColorCircleBackgroundPlayer(collider);
                }
            }
        }

        // Создадим список для врагов, которые нужно удалить
        List<GameObject> enemiesToRemove = new List<GameObject>();

        foreach (GameObject enemy in _enemies)
        {
            float distanceToCollider = Vector2.Distance(transform.position, enemy.transform.position);

            if (distanceToCollider > _radiusCircle)
            {
                enemiesToRemove.Add(enemy);
            }
        }

        // Удаление врагов из списка
        foreach (GameObject enemyToRemove in enemiesToRemove)
        {
            enemyToRemove.transform.GetChild(0).gameObject.SetActive(false);
            enemyToRemove.GetComponent<PlayerMove>().CircleSelect.SetActive(false);
            _enemies.Remove(enemyToRemove);
        }

        if (_enemies.Count == 0)
        {
            drawCircle.lineColor = Color.red;
        }
        else if (_enemies.Count > 0)
        {
            drawCircle.lineColor = Color.green;
        }
    }
}