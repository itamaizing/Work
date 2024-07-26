using Prefabs.USABLE;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragBox : MonoBehaviour
{
    [SerializeField] private Transform selectionAreaTransform; 
    private Vector3 _startPosition;
    
    private SelectManager _selectManager;

    private bool _isDrawing;

    public void SetSelectManager(SelectManager selectManager)
    {
        _selectManager = selectManager;
    }

    public void StartDraw()
    {
        _isDrawing = true;
        _startPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        _startPosition.z = 0f;
    }
    public void Draw()
    {
        Vector3 currentMousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        
        Vector3 lowerLeft = new Vector3(Mathf.Min(_startPosition.x, currentMousePosition.x),
                                       (Mathf.Min(_startPosition.y, currentMousePosition.y)));
        Vector3 upperRight = new Vector3(Mathf.Max(_startPosition.x, currentMousePosition.x), 
                                        (Mathf.Max(_startPosition.y, currentMousePosition.y)));
        
        selectionAreaTransform.position = lowerLeft;
        selectionAreaTransform.localScale = upperRight - lowerLeft;
    }

    public void StopDraw()
    {
        if (!_isDrawing)
        {
            _selectManager.DeselectAll();
            return;
        }
        
        var colliders =  Physics2D.OverlapAreaAll(_startPosition, Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
        
        _selectManager.DeselectAll();
        
        foreach (Collider2D collider in colliders)
        {
            var character = collider.GetComponent<Character>();
            
            if (character != null)
            {
                _selectManager.SelectInArea(character);
            }
        }
        
        gameObject.SetActive(false);
        
        _isDrawing = false;
    }
}
