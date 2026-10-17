using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Debug
{
    public class SpawnServiceUI : MonoBehaviour
    {
        [SerializeField] private SpawnService _spawnService;
        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _button;

        private MPNetworkManager _mpManager;

        private void OnEnable()
        {
            _mpManager = MPNetworkManager.Instance;

            _dropdown.ClearOptions();

            var prefabNames = new List<string>();
            foreach (var prefab in _mpManager.spawnPrefabs)
                prefabNames.Add(prefab != null ? prefab.name : "null");

            _dropdown.AddOptions(prefabNames);
            _dropdown.value = 0;

            _inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
            _button.onClick.AddListener(Spawn);
        }

        private void OnDisable()
        {
            _inputField.onEndEdit.RemoveListener(OnInputFieldEndEdit);
            _button.onClick.RemoveListener(Spawn);
        }

        private void OnInputFieldEndEdit(string arg0)
        {
            var filteredNames = new List<string>();
            foreach (var prefab in _mpManager.spawnPrefabs)
            {
                var name = prefab != null ? prefab.name : "null";
                if (name.StartsWith(arg0, StringComparison.OrdinalIgnoreCase))
                    filteredNames.Add(name);
            }

            _dropdown.ClearOptions();
            _dropdown.AddOptions(filteredNames);
            _dropdown.value = 0;
        }

        private void Spawn()
        {
            string selectedName = _dropdown.options[_dropdown.value].text;
            _spawnService.CmdSpawn(selectedName);
        }
    }
}