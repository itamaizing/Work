using Pathfinding;
using UnityEngine;

public class Project : MonoBehaviour
{
    AstarPath astarPath;
    public GameObject target;
    private Vector2 targetPosition;

    private void Start()
    {
        astarPath = FindObjectOfType<AstarPath>();
        //InvokeRepeating("UpdateGraphs", 0f, 0.5f);
    }

    private void Update()
    {
        target.transform.position = targetPosition;
    
        if (Input.GetMouseButtonDown(1))
        {
            targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        }
    }

    void UpdateGraphs()
    {
        astarPath.Scan();
    }
}
