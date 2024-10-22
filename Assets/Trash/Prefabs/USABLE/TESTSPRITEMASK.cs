using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTSPRITEMASK : MonoBehaviour
{
    public SpriteMask obj;
	public SelectComponent select;

	private void Update()
	{
		if(Input.GetKeyDown(KeyCode.T) && select.IsCurrentPlayer)
		{
			obj.enabled = !obj.enabled;
		}
	}
}
