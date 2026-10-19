using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Debug
{
    public class SpawnServiceUI : MonoBehaviour
    {
        #region Variables
        [SerializeField] private SpawnService _spawnService;
        [SerializeField] private List<GameObject> _spawnables;
        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _button;
        [SerializeField] private Texture2D _cursorPrepareTexture;
        [SerializeField] private Texture2D _cursorDefaultTexture;
        

        Vector2 _cursorPrepareHotspot;
        Vector2 _cursorDefaultHotspot = Vector2.zero;
        private MPNetworkManager _mpManager;
        private Coroutine _spawnRoutine;
        #endregion Variables

        //public List<GameObject> Spawnables => _mpManager.spawnPrefabs;
        public List<GameObject> Spawnables => _spawnables;


        private void Awake()
        {
            _cursorPrepareHotspot = new Vector2(_cursorPrepareTexture.width / 3.3f, _cursorPrepareTexture.height / 2.4f);
        }

        private void OnEnable()
        {
            _mpManager = MPNetworkManager.Instance;

            _dropdown.ClearOptions();

            var prefabNames = new List<string>();
            foreach (var prefab in Spawnables)
                prefabNames.Add(prefab != null ? prefab.name : "null");

            _dropdown.AddOptions(prefabNames);
            _dropdown.value = 0;

            _inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
            _button.onClick.AddListener(StartSpawnRoutine);
        }

        private void OnDisable()
        {
            _inputField.onEndEdit.RemoveListener(OnInputFieldEndEdit);
            _button.onClick.RemoveListener(StartSpawnRoutine);
        }

        private void OnInputFieldEndEdit(string arg0)
        {
            var filteredNames = new List<string>();
            foreach (var prefab in Spawnables)
            {
                var name = prefab != null ? prefab.name : "null";
                if (name.StartsWith(arg0, StringComparison.OrdinalIgnoreCase))
                    filteredNames.Add(name);
            }

            _dropdown.ClearOptions();
            _dropdown.AddOptions(filteredNames);
            _dropdown.value = 0;
        }

        private void StartSpawnRoutine()
        {
            if (_spawnRoutine != null)
                return;
            _spawnRoutine = StartCoroutine(SpawnJob());
        }

        private IEnumerator SpawnJob()
        {
            UnityEngine.Cursor.SetCursor(_cursorPrepareTexture, _cursorPrepareHotspot, CursorMode.Auto);
            Vector3? point = null;
            while (point == null)
            {
                if (Input.GetMouseButtonDown(0))
                    point = GetMousePoint();
                if (Input.GetMouseButtonDown(1))
                {
                    StopRoutine();
                    yield break;
                }
                yield return null;
            }
            string selectedName = _dropdown.options[_dropdown.value].text;
            _spawnService.CmdSpawn(selectedName, point.Value);
            StopRoutine();
        }

        private void StopRoutine()
        {
            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
            UnityEngine.Cursor.SetCursor(_cursorDefaultTexture, _cursorDefaultHotspot, CursorMode.Auto);

        }

        private Vector3 GetMousePoint()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            LayerMask mask = (LayerMask.GetMask("Default", "Ground", "Obstecls"));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
            {
                return hit.point;
            }
            return Vector3.zero;
        }
    }
}