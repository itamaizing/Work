using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SourceUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _teamText1;
    [SerializeField] private TMP_Text _teamText2;
    [SerializeField] private Image _winImage;
    [SerializeField] private TMP_Text _winText;


    private string _teamName1 = "Light";
    private string _teamName2 = "Dark";

    private void Start()
    {
        _teamText1.text = _teamName1 + ": " + 0;
        _teamText2.text = _teamName2 + ": " + 0;
    }

    public void SetSource(int teamIndex, int source)
    {
        //Debug.LogError(teamIndex);
        //Debug.LogError(source);
        switch (teamIndex)
        {
            case 1:
                //Debug.LogError(_teamName1 + source);
                _teamText1.text = _teamName1 + ": " + source;
                //Debug.LogError(_teamText1.text);
                break;
            case 2:
                //Debug.LogError(_teamName2 + source);
                _teamText2.text = _teamName2 + ": " + source;
                //Debug.LogError(_teamText2.text);
                break;

            default:
                Debug.LogError("team not founded");
                break;
        }
    }

    public void ShowWinner(int teamIndex)
    {
        _winImage.gameObject.SetActive(true);

        switch (teamIndex)
        {
            case 1:
                _winText.text = _teamName1 + " team WIN";
                break;
            case 2:
                _winText.text = _teamName2 + " team WIN";
                break;

            default:
                Debug.LogError("team not founded");
                break;
        }
    }
}
