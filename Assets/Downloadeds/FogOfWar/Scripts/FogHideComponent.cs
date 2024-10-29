using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogHideComponent : MonoBehaviour
{
    [SerializeField]
    private Renderer[] m_RenderersToHide;
    [SerializeField]
    private GameObject[] m_GameObjectsToHide;

    private void Start()
    {
        float updateinterval = 0.1f;
        InvokeRepeating("CheckIfInFog", updateinterval, updateinterval);
    }

    void CheckIfInFog() {
        bool infog = FogOfWarManager.INSTANCE.IsPositionInFog(new Vector3(transform.position.x, transform.position.z, transform.position.y));

        foreach (Renderer renderer in m_RenderersToHide)
        {
            renderer.enabled = !infog;
        }
        foreach (GameObject gameobject in m_GameObjectsToHide)
        {
            gameobject.SetActive(!infog);
        }
    }

}
