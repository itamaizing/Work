using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class TentaclesPrefab : MonoBehaviour
{
    public DrawCircle drawCircle;

    void Start()
    {
        drawCircle.Draw(3f * 1.9f - 1.9f / 2f);
    }

    public void Clear()
    {
        drawCircle.Clear();
    }
}
