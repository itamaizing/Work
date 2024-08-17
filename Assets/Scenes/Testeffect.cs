using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testeffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particle;
    [SerializeField] private ParticleSystem _subparticle;
    [SerializeField] private Transform _target;

    private void Update()
    {
        var distance = Vector3.Distance(_target.position, transform.position);
        var module = _particle.velocityOverLifetime;
        //var module2 = _subparticle.shape;
        if (_subparticle != null)
        {
            var module2 = _subparticle.shape;
            module2.scale = new Vector3(0, 1, distance);
            var vector = new Vector3(_subparticle.transform.localPosition.x, _subparticle.transform.localPosition.y, distance / 2);
            _subparticle.transform.localPosition = vector;
        }


        module.zMultiplier = distance * 2;
        //module2.scale.Set(0, 1, distance);
        //module2.scale.Scale(new Vector3(0, 1, distance));
        //module2.boxThickness.Set(0, 1, distance);

        
     
        gameObject.transform.LookAt(_target, Vector3.up);
    }
}
