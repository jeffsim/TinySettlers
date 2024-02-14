using System;
using System.Collections.Generic;
using UnityEngine;

public interface ILocationProvider
{
    LocationComponent Location { get; }
}

[Serializable]
public class LocationComponent
{
    public override string ToString() => WorldLoc.ToString();

    public Vector2 WorldLoc;

    public static LocationComponent operator -(LocationComponent loc1, LocationComponent loc2) => new(loc1.WorldLoc - loc2.WorldLoc);
    public static LocationComponent operator +(LocationComponent loc1, LocationComponent loc2) => new(loc1.WorldLoc + loc2.WorldLoc);

    public LocationComponent() => WorldLoc = Vector2.zero;
    public LocationComponent(Vector2 worldLoc) => WorldLoc = worldLoc;
    public LocationComponent(float localX, float localY) => WorldLoc = new(localX, localY);

    public void SetWorldLoc(Vector2 location) => WorldLoc = location;
    public void SetWorldLoc(LocationComponent location)
    {
        WorldLoc = location.WorldLoc;
    }
    public void SetWorldLoc(float x, float y) => WorldLoc.Set(x, y);

    internal float DistanceTo(LocationComponent location) => Vector2.Distance(WorldLoc, location.WorldLoc);
    public bool WithinDistanceOf(LocationComponent location, float closeEnough) => DistanceTo(location) <= closeEnough;

    public T GetClosest<T>(List<T> locsToCheck, Func<T, bool> isValidCallback = null) where T : ILocationProvider => GetClosest(locsToCheck, out _, isValidCallback);
    public T GetClosest<T>(List<T> locsToCheck, out float closestDist, Func<T, bool> isValidCallback = null) where T : ILocationProvider
    {
        T closest = default;
        closestDist = float.MaxValue;
        foreach (var locToCheck in locsToCheck)
            if (isValidCallback == null || isValidCallback(locToCheck))
            {
                float dist = DistanceTo(locToCheck.Location);
                if (dist < closestDist)
                {
                    closest = locToCheck;
                    closestDist = dist;
                }
            }
        return closest;
    }

    internal void MoveTowards(LocationComponent loc1, LocationComponent loc2, float t)
    {
        WorldLoc = Vector2.MoveTowards(loc1.WorldLoc, loc2.WorldLoc, t);
    }

    public void UpdateWorldLoc()
    {
        // WorldLoc = LocalLoc;
        // if (ParentLoc != null)
        // WorldLoc += ParentLoc.WorldLoc;
    }

    internal Vector3 GetWorldLocRelativeTo(LocationComponent location, float dz) => new(WorldLoc.x - location.WorldLoc.x, WorldLoc.y - location.WorldLoc.y, dz);
}