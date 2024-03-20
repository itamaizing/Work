using UnityEngine;

namespace Players.CircleBackgroundColor
{
    public class ControllerCircleBackgroundColor : MonoBehaviour
    {
        [SerializeField] private SoCircleColorBackgroundSettings _soCircleColorBackgroundSettings;
        [SerializeField] private SoCircleColorBackgroundSettings _soCircleColorBackgroundAttackSettings;

        public SoCircleColorBackgroundSettings soCircleColorBackgroundAttackSettings =>
            _soCircleColorBackgroundAttackSettings;

        public SoCircleColorBackgroundSettings soCircleColorBackgroundSettings => _soCircleColorBackgroundSettings;

        public void SetColorCircleBackgroundPlayer(Collider2D collider)
        {
            collider.transform.GetChild(0).gameObject.SetActive(true);
            SpriteRenderer spriteRenderer = collider.transform.GetChild(0).GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                Color newColor = soCircleColorBackgroundSettings.SpriteColor;
                newColor.a = soCircleColorBackgroundSettings.Alpha;
                spriteRenderer.color = newColor;
            }
        }

        public void SetColorCircleBackgroundPlayer(GameObject targetParent)
        {
            Debug.Log("Меняем на зелёный");
            targetParent.transform.GetChild(0).gameObject.SetActive(true);
            SpriteRenderer spriteRenderer = targetParent.transform.GetChild(0).GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                Color newColor = soCircleColorBackgroundSettings.SpriteColor;
                newColor.a = soCircleColorBackgroundSettings.Alpha;
                spriteRenderer.color = newColor;
            }
        }

        public void SetColorCircleBackgroundAttack(GameObject targetParent)
        {
            Debug.Log("Меняем цвет");
            targetParent.transform.GetChild(0).gameObject.SetActive(true);
            SpriteRenderer targetSpriteRenderer = targetParent.transform.GetChild(0).GetComponent<SpriteRenderer>();

            if (targetSpriteRenderer != null)
            {
                Color newColor = soCircleColorBackgroundAttackSettings.SpriteColor;
                newColor.a = soCircleColorBackgroundAttackSettings.Alpha;
                targetSpriteRenderer.color = newColor;
            }
        }

        public void DisableCircleBackground()
        {
            GetComponent<Collider>().transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}