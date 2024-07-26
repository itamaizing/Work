using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ability/AbilityLineRenderer", fileName = "NewLine")]
public class AbilityLineRenderer : ScriptableObject
{
    [SerializeField] private SpriteRenderer _startSprite;
    [SerializeField] private SpriteRenderer _endSprite;

    public SpriteRenderer Start => _startSprite;
    public SpriteRenderer End => _endSprite;
}
