using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability
{
    public class RotateAtMouse : MonoBehaviour
    {
        private void Update()
        {
            LookAtMouse();
        }

        private void LookAtMouse()
        {
            var dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}