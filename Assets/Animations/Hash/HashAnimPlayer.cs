using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HashAnimPlayer : MonoBehaviour
{
    // triggers
    public static readonly int AnimCancled = Animator.StringToHash("Cancled");
    public static readonly int TakeDamage = Animator.StringToHash("TakeDamage");

    //float
    public static readonly int VelocityX = Animator.StringToHash("X");
    public static readonly int VelocityZ = Animator.StringToHash("Y");

    public static readonly int CastSpeed = Animator.StringToHash("CastSpeed");
}
