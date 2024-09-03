using System.Collections.Generic;
using Mirror;

public class NetworkComponent : NetworkBehaviour
{
    public List<Character> controllableUnits;

    public override void OnStartServer()
    {
        Character.ServerOnUnitSpawned += ServerHandleUnitSpawn;
        Character.ServerOnUnitDeleted += ServerHandleUnitDelete;
    }

    public override void OnStopServer()
    {
        Character.ServerOnUnitSpawned -= ServerHandleUnitSpawn;
        Character.ServerOnUnitDeleted -= ServerHandleUnitDelete;
    }

    private void ServerHandleUnitSpawn(Character character)
    {
        if (character.connectionToClient.connectionId != connectionToClient.connectionId)
        {
            return;
        }
        
        controllableUnits.Add(character);
    }
    
    private void ServerHandleUnitDelete(Character character)
    {
        if (character.connectionToClient.connectionId != connectionToClient.connectionId)
        {
            return;
        }
        controllableUnits.Remove(character);
    }

    public override void OnStartClient()
    {
        if (!isClientOnly)
        {
            return;
        }

        Character.AuthorityOnUnitSpawned += AuthorityHandleUnitSpawn;
        Character.AuthorityOnUnitDeleted += AuthorityHandleUnitDelete;

    }

    public override void OnStopClient()
    {
        if (!isClientOnly)
        {
            return;
        }
        Character.AuthorityOnUnitSpawned -= AuthorityHandleUnitSpawn; 
        Character.AuthorityOnUnitDeleted -= AuthorityHandleUnitDelete;
    }
    
    private void AuthorityHandleUnitSpawn(Character character)
    {
        if (!isOwned)
        {
            return;
        }
        controllableUnits.Add(character);
    }
    
    private void AuthorityHandleUnitDelete(Character character)
    {
        if (!isOwned)
        {
            return;
        }
        controllableUnits.Remove(character);
    }
}
