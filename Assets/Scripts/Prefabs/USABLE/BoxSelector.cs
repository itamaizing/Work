using System;
using UnityEngine;

public class BoxSelector : MonoBehaviour
{
    [SerializeField] private Transform selectionAreaTransform;
    
    private Vector3 startPosition;

    public void StartDraw()
    {
        startPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        startPosition.z = 0f;
    }
    public void Draw()
    {
        Vector3 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 lowerLeft = new Vector3(Mathf.Min(startPosition.x, currentMousePosition.x),
                                       (Mathf.Min(startPosition.y, currentMousePosition.y)));
        Vector3 upperRight = new Vector3(Mathf.Max(startPosition.x, currentMousePosition.x),
            (Mathf.Max(startPosition.y, currentMousePosition.y)));
        selectionAreaTransform.position = lowerLeft;
        selectionAreaTransform.localScale = upperRight - lowerLeft;
    }

    public void StopDraw()
    {
        Collider2D[] collider2DArray = Physics2D.OverlapAreaAll(startPosition, Camera.main.ScreenToWorldPoint(Input.mousePosition));
        
        SelectManager.Instance.DeselectAll();
        
        foreach (Collider2D collider in collider2DArray)
        {
            var character = collider.GetComponent<Character>();
            if (character != null)
            {
                SelectManager.Instance.SelectInArea(character);
            }
        }
        gameObject.SetActive(false);
    }
}
