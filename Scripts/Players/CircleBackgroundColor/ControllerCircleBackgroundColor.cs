using UnityEngine;

namespace Players.CircleBackgroundColor
{
    public class ControllerCircleBackgroundColor : MonoBehaviour
    {
        [SerializeField] private SoCircleColorBackgroundSettings _soCircleColorBackgroundSettings;

        public void SetColorCircleBackgroundPlayer(Collider2D collider)
        {
            collider.transform.GetChild(0).gameObject.SetActive(true);
            SpriteRenderer spriteRenderer = collider.transform.GetChild(0).GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                Color newColor = _soCircleColorBackgroundSettings.SpriteColor;
                newColor.a = _soCircleColorBackgroundSettings.Alpha;
                spriteRenderer.color = newColor;
            }
        }

        public void DisableCircleBackgroundPlayer()
        {
            GetComponent<Collider>().transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}