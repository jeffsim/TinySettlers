using System;

[Serializable]
public class UniqueIdGenerator
{
    // used to ensure each Model (building, tile, entity, ...) has a unique id
    // TODO: Want this to be serialized, not public
    public int NextUniqueId = 1;

    // Nonpersisted reference to the current UniqueIdGenerator
    public static UniqueIdGenerator Instance;

    public UniqueIdGenerator()
    {
        Instance = this;
    }
    
    public static int GetNextUniqueId()
    {
        return Instance.getNextUniqueId();
    }

    int getNextUniqueId()
    {
        return ++NextUniqueId;
    }
}