using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter_ScorchedSoul_Baff : BaseEffect
{
    private int _currentStacks = 1;
    private int _maxStacks = 3;

    public int CurrentStacks {  get { return _currentStacks; } }

    public void AddStack()
    {
        _currentStacks++;
        if ( _currentStacks >= _maxStacks )
        {
            CastDebaff();
        }
    }

    private void CastDebaff()
    {
        // применяем на врага дебаф 

        // удаляем счетчик и баф после реализации эффекта с комбо
        Destroy(this.gameObject);
    }
}
