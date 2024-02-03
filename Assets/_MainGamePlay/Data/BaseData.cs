using System;

/// <summary>
/// Base class for all of the serialized objects within a Profile.
/// </summary>
[Serializable]
public class BaseData
{
    // Globally unique identifier for this data
    public int InstanceId;

    /// <summary>
    /// Constructor.  Generates a globally unique identifier
    /// </summary>
    public BaseData()
    {
        InstanceId = UniqueIdGenerator.GetNextUniqueId();
    }
}
