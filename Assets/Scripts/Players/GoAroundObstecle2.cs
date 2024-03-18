using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class GoAroundObstecle2 : MonoBehaviour
{
    public float MoveSpeed = 5.0f;
    public LayerMask RaycastLayerMask;
    public List<Vector2> obstacleVertices = new List<Vector2>();
    public List<Vector2> intermediateTargets = new List<Vector2>();
    public float _maxDistance = 2.5f;
    public Vector2 targetPosition;
    public Collider2D Obstacle = null;

    public GameObject Prefab;
    public List<GameObject> PrefabList = new List<GameObject>();
    public GameObject PrefabIT;
    public List<GameObject> PrefabITList = new List<GameObject>();

    private float _distance;
    public Vector2 _clickPosition;
    private Rigidbody2D _rb;
    public bool _left = false;
    public bool _right = false;
    private Vector2 DistanceToObstacle;
    public bool SearchObstacle = false;

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        //float fps = 1.0f / Time.deltaTime;
        //Debug.Log("FPS: " + fps);
        

        if (GetComponent<PlayerMove>().IsSelect && GetComponent<PlayerMove>().CanMove)
        {
            if (Input.GetMouseButtonDown(1))
            {
                intermediateTargets.Clear();
                obstacleVertices.Clear();

                _clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else if(GetComponent<OneRangeAttack>() != null && GetComponent<OneRangeAttack>().ToggleAbility != null && GetComponent<OneRangeAttack>().ToggleAbility.isOn == true && !GetComponent<PlayerMove>().IsSelect) 
        {
            if (Input.GetMouseButtonDown(1))
            {
                intermediateTargets.Clear();
                obstacleVertices.Clear();
                _clickPosition = GetComponent<OneRangeAttack>().Target.transform.position;
            }
        }

        if (GetComponent<PlayerMove>().IsMoving || SearchObstacle)
        {
            RaycastHit2D hitObject = Physics2D.Raycast(_clickPosition, Vector2.zero);
            if (hitObject.collider != null && hitObject.collider.CompareTag("Allies") || hitObject.collider != null && hitObject.collider.CompareTag("Enemies"))
            {
                RaycastLayerMask &= ~(1 << LayerMask.NameToLayer("OtherPlayers"));
            }
            else
            {
                RaycastLayerMask |= (1 << LayerMask.NameToLayer("Player1"));
            }

            if (hitObject.collider != null && hitObject.collider.CompareTag("Obstacle"))
            {
                RaycastLayerMask &= ~(1 << LayerMask.NameToLayer("Obstecls"));
            }
            else
            {
                RaycastLayerMask |= (1 << LayerMask.NameToLayer("Obstecls"));
            }

            Vector2 playerPosition = transform.position;
            targetPosition = _clickPosition;
            Vector2 playerToTarget = targetPosition - playerPosition;
            _distance = playerToTarget.magnitude;


            RaycastHit2D hit = Physics2D.Raycast(playerPosition, playerToTarget, _distance, RaycastLayerMask);
            Obstacle = hit.collider;

            if (Obstacle != null)
            {
                intermediateTargets.Clear();
                obstacleVertices.Clear();

                Collider2D obstacleCollider = Obstacle.GetComponent<Collider2D>();
                obstacleVertices.Clear();
                obstacleVertices.AddRange(GetVerticesFromCollider(obstacleCollider));

                Vector2 newPoint = Vector2.zero;
                Vector2 newSecondPoint = Vector2.zero;

                foreach (Vector2 point in obstacleVertices)
                {

                    Vector2 furthestPointRight = Vector2.zero;
                    Vector2 furthestPointLeft = Vector2.zero;
                    float maxDistanceRight = 0.0f;
                    float maxDistanceLeft = 0.0f;

                    foreach (Vector2 dot in obstacleVertices)
                    {
                        Vector2 vectorToPoint = dot - playerPosition;
                        Vector2 perpendicularVector = new Vector2(-playerToTarget.y, playerToTarget.x);
                        Vector2 oppositePerpendicularVector = new Vector2(playerToTarget.y, -playerToTarget.x);

                        float distanceRight = Vector2.Dot(perpendicularVector, vectorToPoint);
                        float distanceLeft = Vector2.Dot(oppositePerpendicularVector, vectorToPoint);

                        if (distanceRight > maxDistanceRight)
                        {
                            maxDistanceRight = distanceRight;
                            furthestPointRight = dot;
                        }

                        if (distanceLeft > maxDistanceLeft)
                        {
                            maxDistanceLeft = distanceLeft;
                            furthestPointLeft = dot;
                        }
                    }
                    Vector2 secondPointRight = Vector2.zero;
                    Vector2 secondPointLeft = Vector2.zero;
                    float minDistanceRight = float.MaxValue;
                    float minDistanceLeft = float.MaxValue;

                    Vector2 obstacleCenter = Obstacle.transform.position;
                    DistanceToObstacle = obstacleCenter - playerPosition;
                    if (DistanceToObstacle.magnitude < _maxDistance)
                    {
                        Vector2 middlePointRight = (hit.point + furthestPointRight) / 2f;
                        Vector2 middlePointLeft = (hit.point + furthestPointLeft) / 2f;

                        foreach (Vector2 dot in obstacleVertices)
                        {
                            float distanceToMiddleRight = Vector2.Distance(middlePointRight, dot);
                            float distanceToMiddleLeft = Vector2.Distance(middlePointLeft, dot);

                            if (distanceToMiddleRight < minDistanceRight)
                            {
                                minDistanceRight = distanceToMiddleRight;
                                secondPointRight = dot;
                            }

                            if (distanceToMiddleLeft < minDistanceLeft)
                            {
                                minDistanceLeft = distanceToMiddleLeft;
                                secondPointLeft = dot;
                            }
                        }

                    }

                    float perpRight = ((furthestPointRight - playerPosition).magnitude + (targetPosition - furthestPointRight).magnitude);
                    float perpLeft = ((furthestPointLeft - playerPosition).magnitude + (targetPosition - furthestPointLeft).magnitude);

                    Vector2 chosenPoint = Vector2.zero;
                    Vector2 middlePoint = Vector2.zero;
                    if (hit.collider.CompareTag("Player"))
                    {
                        chosenPoint = furthestPointRight;
                        middlePoint = secondPointRight;
                        _right = true;
                        _left = false;
                    }
                    else if (perpRight < perpLeft || _left == false)
                    {
                        chosenPoint = furthestPointRight;
                        middlePoint = secondPointRight;
                        _right = true;
                        _left = false;
                    }
                    else if (perpRight >= perpLeft || _right == false)
                    {
                        chosenPoint = furthestPointLeft;
                        middlePoint = secondPointLeft;
                        _left = true;
                        _right = false;
                    }
                    else
                    {
                        chosenPoint = furthestPointLeft;
                        middlePoint = secondPointLeft;
                        _left = true;
                        _right = false;
                    }

                    float distance = 2.5f;

                    Vector2 direction = (chosenPoint - obstacleCenter).normalized;
                    newPoint = chosenPoint + direction * distance;

                    Vector2 secondDirection = (middlePoint - obstacleCenter).normalized;
                    newSecondPoint = middlePoint + secondDirection * distance;

                }
                intermediateTargets.Clear();

                if (DistanceToObstacle.magnitude < _maxDistance && newSecondPoint != Vector2.zero)
                {
                    intermediateTargets.Add(newSecondPoint);

                }
                if (newPoint != Vector2.zero)
                {
                    intermediateTargets.Add(newPoint);
                }

                /*foreach (Vector3 point in obstacleVertices)
                {
                    GameObject newPrefab = Instantiate(Prefab, point, Quaternion.identity);
                    PrefabList.Add(newPrefab);
                }*/

            }

            /*foreach (Vector2 item in intermediateTargets)
            {
                GameObject newPrefabIT = Instantiate(PrefabIT, item, Quaternion.identity);
                PrefabITList.Add(newPrefabIT);
            }*/



            if (intermediateTargets.Count > 0)
            {
                _rb.isKinematic = false;

                Vector2 nextTarget = intermediateTargets[0];
                Vector2 moveDirection = (nextTarget - (Vector2)transform.position).normalized;
                _rb.velocity = moveDirection * MoveSpeed;

                if (Vector2.Distance(transform.position, nextTarget) < 0.2f)
                {
                    // Плеер достиг промежуточной цели, удаляем ее из списка
                    intermediateTargets.RemoveAt(0);
                }
            }
            else
            {
                intermediateTargets.Clear();
                _left = false;
                _right = false;
            }
        }
        else
        {
            return;
        }
    }

    public List<Vector2> GetVerticesFromCollider(Collider2D collider)
    {
        List<Vector2> point = new List<Vector2>();
        if (collider is PolygonCollider2D polygonCollider)
        {
            Vector2[] list = polygonCollider.GetComponent<PolygonCollider2D>().GetPath(0);

            Vector3 position = polygonCollider.transform.position;
            Quaternion rotation = polygonCollider.transform.rotation;

            foreach (Vector2 vertex in list)
            {
                Vector3 localPosition = new Vector3(vertex.x, vertex.y, 0);
                Vector3 worldPosition = position + rotation * localPosition;
                point.Add(worldPosition);
            }
        }

        else
        {
            Debug.Log("Другой коллаидер");
        }

        return point;
    }
}







