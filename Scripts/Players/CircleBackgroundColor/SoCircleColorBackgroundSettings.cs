using UnityEngine;

namespace Players.CircleBackgroundColor
{
    [CreateAssetMenu(menuName = "Create Circle Color Background Settings",fileName = "Circle Color Background Settings")]
    public class SoCircleColorBackgroundSettings:ScriptableObject
    {
        [SerializeField]private  Color _spriteColor = Color.white; // Цвет спрайта по умолчанию - белый
        [SerializeField][Range(0f, 1f)] private float _alpha = 1f; // Прозрачность (альфа-значение)

        public Color SpriteColor => _spriteColor;

        public float Alpha => _alpha;
    }
}