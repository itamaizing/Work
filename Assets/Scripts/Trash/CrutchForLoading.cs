using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CrutchForLoading : MonoBehaviour
{
	[Client]
	private void Awake()
	{
		SceneManager.sceneLoaded += OnLoaded;
	}

	[Client]
	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnLoaded;
	}

	private void OnLoaded(Scene scene, LoadSceneMode loadSceneMode)
	{
		StartCoroutine(LoadedJob());
	}

	private IEnumerator LoadedJob()
    {
		yield return new WaitForFixedUpdate();
		transform.position = Vector3.up;
    }
}
