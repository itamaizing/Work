using System.Collections.Generic;
using UnityEngine;

public class UIPlayerComponents : MonoBehaviour
{
    public SelectedCircle CircleSelect;

    public MinimapMarker MarkersSelect;

    private List<AbilityToggle> AbilitiesOnTargetToggles;

    public void Initialize(PlayerAbilities playerAbilities,PlayerMove playerMove,PlayerStamina stamina , HealthPlayer healthPlayer)
    {
        ChangeSelection(false);
        playerAbilities.Initialize(playerMove, stamina, healthPlayer);
    }
    
    public void ChangeSelection(bool isSelect)
    {
        CircleSelect.IsActive = isSelect;
        MarkersSelect.IsActive = isSelect;
    }
}
