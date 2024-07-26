using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prefabs.USABLE
{
    public class SelectManager : MonoBehaviour
    { 
        [SerializeField] private DragBox _dragBox;

        private NetworkComponent _contoller;

        private List<Character> _canContollUnits = new List<Character>();

        public List<Character> SelectedControllableUnits { get; } = new();

        private int _currentUnitNumber;

        private void Awake()
        {
            _dragBox.gameObject.SetActive(false);
            _dragBox.SetSelectManager(this);
        }
        
        [ClientCallback]
        private void Update()
        {
            if (_contoller == null)
            {
                _contoller = NetworkClient.connection.identity.GetComponent<NetworkComponent>();
                _canContollUnits = _contoller.controllableUnits;
            }
        
            if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftShift))
            {
                _dragBox.gameObject.SetActive(true);
                _dragBox.StartDraw();
            }

            if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftShift))
            {
                _dragBox.Draw();
            }

            if (Input.GetMouseButtonUp(0) && Input.GetKey(KeyCode.LeftShift))
            {
                _dragBox.StopDraw();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if(SelectedControllableUnits.Count <= 0) return;
            
                foreach (var unit in SelectedControllableUnits)
                {
                    unit.SelectComponent.IsCurrentPlayer = false;
                }
            
                _currentUnitNumber = (_currentUnitNumber+1) % SelectedControllableUnits.Count;
                SelectedControllableUnits[_currentUnitNumber].SelectComponent.IsCurrentPlayer = true;
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                var center = CalculateCenterPoint();
                foreach (var character in SelectedControllableUnits)
                {
                    if(character is HeroComponent) continue; 
                    character.Move.SetOffset(DetermineOffset(character.transform.position, center));
                }
            }
        }

        public void SelectOnClick(Character character)
        {
            DeselectAll();
            
            if(!_canContollUnits.Contains(character)) return;
            
            SelectedControllableUnits.Add(character);
            character.SelectComponent.Select();
        }

        public void SelectInArea(Character character)
        {
            if(!_canContollUnits.Contains(character)) return;
            
            if (!SelectedControllableUnits.Contains(character))
            {
                SelectedControllableUnits.Add(character);
                character.SelectComponent.Select();
            }
            else
            {
                SelectedControllableUnits.Remove(character);
                character.SelectComponent.Deselect();
            }

            SelectedControllableUnits.FirstOrDefault()!.SelectComponent.IsCurrentPlayer = true;
            _currentUnitNumber = 0;
        }

        public void Deselect(Character character)
        {
            if(SelectedControllableUnits.Contains(character)) 
                SelectedControllableUnits.Remove(character);
        }
        public void DeselectAll()
        {
            foreach (var character in SelectedControllableUnits)
            {
                character.SelectComponent.Deselect();
            }
            SelectedControllableUnits.Clear();
        }
        
        private Vector3 CalculateCenterPoint()
        {
            if (SelectedControllableUnits.Count == 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            
            foreach (var character in SelectedControllableUnits)
            {
                if(character is HeroComponent) continue;
                
                sum += character.transform.position;
            }
            
            return sum / SelectedControllableUnits.Count;
        }
        
        private Vector3 DetermineOffset(Vector3 characterPosition, Vector3 centerPoint)
        {
            Vector3 direction = characterPosition - centerPoint;
            
            direction.Normalize();
            
            float distance = 3f;
            
            return direction * distance;
        }
        
    }
}
