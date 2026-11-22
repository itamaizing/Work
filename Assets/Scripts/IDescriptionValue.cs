using UnityEngine;

public interface IDescriptionValue
{
    public bool TryGetDescriptionValue(string key, out object value);
}
