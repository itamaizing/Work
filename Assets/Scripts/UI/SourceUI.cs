using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SourceUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _teamText1;
    [SerializeField] private TMP_Text _teamText2;

    private string _teamName1 = "Light: ";
    private string _teamName2 = "Dark: ";

    public void UpdateSource(int teamIndex, int source)
    {
        switch (teamIndex)
        {
            case 1:
                _teamText1.text = _teamName1 + source;
                break;
            case 2:
                _teamText2.text = _teamName2 + source;
                break;

            default:
                Debug.LogError("team not founded");
                break;
        }
    }
}
