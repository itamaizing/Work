using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSLocker : MonoBehaviour
{
    [Tooltip("Enable/Disable FPS locking")]
    public bool lockFPS = true;

    [Tooltip("Target FPS when locked")]
    public int targetFPS = 60;

    private void Awake()
    {
        // Make this object persistent between scenes
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (lockFPS)
        {
            // If FPS is locked and current target doesn't match, set it
            if (Application.targetFrameRate != targetFPS)
            {
                Application.targetFrameRate = targetFPS;
            }
        }
        else
        {
            // If FPS is unlocked and target isn't reset (-1 means no limit), reset it
            if (Application.targetFrameRate != -1)
            {
                Application.targetFrameRate = -1;
            }
        }
    }

    // Optional: Add this if you want to change FPS at runtime
    public void SetTargetFPS(int fps)
    {
        targetFPS = fps;
        if (lockFPS)
        {
            Application.targetFrameRate = targetFPS;
        }
    }

    // Optional: Add this if you want to toggle FPS lock at runtime
    public void ToggleFPSLock(bool state)
    {
        lockFPS = state;
        if (lockFPS)
        {
            Application.targetFrameRate = targetFPS;
        }
        else
        {
            Application.targetFrameRate = -1;
        }
    }
}
