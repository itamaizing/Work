using System;
using System.Collections.Generic;
using UnityEngine;

//Специфичный компонент. У чего-то есть только звук попадания, у чего-то +полет, +замах, +что-то еще.
//Можно сделать через список списков, по типу: List<SoundGroup>.
//SoundGroup { enum Type[windup, impact,..], List<AudioClip>}. Нужно ли?
[Serializable]
public class SoundComponent : BaseSkillComponent
{
    #region InspectorFields
    List<AudioClip> sounds;
    #endregion

    #region RuntimeVariables

    #endregion

    #region Properties
    public float Template {
        get { return 0; }
        set { value++; }
    }

    #endregion

    #region Methods

    #endregion
}
