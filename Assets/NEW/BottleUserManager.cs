using UnityEngine;
using UnityEngine.SceneManagement;

public class BottleUserManager : MonoBehaviour
{
    private static BottleUserManager _instance;
    public static BottleUserManager Instance => _instance;

    private const int MaxBottles = 99;
    private const float BottleFillStep = 1f / 3f;

    private int _currentBottles = 0;
    private float _currentBottleVolume = 0f;
    private string _currentUser;
    private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBottleData();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            LogBottleInfoOnClient();
        }
    }

    public void SetUser(string user)
    {
        _currentUser = user;
    }

    public void AddBottleVolume()
    {
        _currentBottleVolume += BottleFillStep;

        if (_currentBottleVolume >= 1f)
        {
            _currentBottles = Mathf.Min(_currentBottles + 1, MaxBottles);
            _currentBottleVolume = 0f;
        }

        SaveBottleData();
    }

    public int GetCurrentBottles() => _currentBottles;
    public float GetCurrentBottleVolume() => _currentBottleVolume;

    private void SaveBottleData()
    {
        PlayerPrefs.SetInt(_currentUser + "_Bottles", _currentBottles);
        PlayerPrefs.SetFloat(_currentUser + "_BottleVolume", _currentBottleVolume);
        PlayerPrefs.Save();
    }

    private void LoadBottleData()
    {
        _currentBottles = PlayerPrefs.GetInt(_currentUser + "_Bottles", 0);
        _currentBottleVolume = PlayerPrefs.GetFloat(_currentUser + "_BottleVolume", 0f);
    }

    public void LogBottleInfoOnClient()
    {
        Debug.Log($"Количество бутылей: {_currentBottles}");
        Debug.Log($"Объем текущего бутыля: {_currentBottleVolume * 100}%");
    }
}
