using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class AutoCastParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particle1;
    [SerializeField] private ParticleSystem _particle2;
    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Play()
    {
        //_particle1.Play();
        //_particle2.Play();

        //_particle1.GetComponent<SplineAnimate>().Play();
        //_particle2.GetComponent<SplineAnimate>().Play();

        _particle1.gameObject.SetActive(false);
        _particle2.gameObject.SetActive(false);

        _particle1.gameObject.SetActive(true);
        _particle2.gameObject.SetActive(true);

        _particle2.GetComponent<SplineAnimate>().StartOffset = 0.51f;
    }
}
