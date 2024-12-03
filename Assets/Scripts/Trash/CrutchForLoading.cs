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
		if (this == null) return;
		StartCoroutine(LoadedJob());
	}

	private IEnumerator LoadedJob()
    {
		if (this != null && transform != null)
		{
			transform.position = Vector3.up;
		}
		yield return null;
	}
}
