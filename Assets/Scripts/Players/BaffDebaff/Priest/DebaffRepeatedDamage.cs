using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebaffRepeatedDamage : MonoBehaviour
{
    public bool IsRepeateDamage;

    public void CastDebaff(float TimeDebaff)
    {
        StartCoroutine(StartDebaff(TimeDebaff));
    }

    private IEnumerator StartDebaff(float time)
    {
        IsRepeateDamage = true;
        yield return new WaitForSeconds(time);
        IsRepeateDamage = false;

        Destroy(this);
    }
}
