using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestTestt : MonoBehaviour
{
    [SerializeField, Scene] private string _scene;

    List<Scene> s = new List<Scene>();

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SceneManager.UnloadSceneAsync(s[1]);
            s.RemoveAt(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(LoadRoom());
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            foreach (var item in s)
            {
                Debug.Log(item);
            }  
        }
    }

    public IEnumerator LoadRoom()
    {
        yield return SceneManager.LoadSceneAsync(_scene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics2D });
        s.Add(SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
    }
}
