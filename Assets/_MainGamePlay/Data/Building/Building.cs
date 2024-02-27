using System;
using UnityEngine;

[Serializable]
public partial class BuildingData : BaseData, ILocation, IOccupiable, IConstructable, IPausable, IGeneratable
{
    public override string ToString() => Defn.FriendlyName + " (" + InstanceId + ")";

    private BuildingDefn _defn;
    public BuildingDefn Defn => _defn = _defn != null ? _defn : GameDefns.Instance.BuildingDefns[DefnId];
    public string DefnId;

    // The Town which this Building is in
    public TownData Town;

#if UNITY_INCLUDE_TESTS
    public string TestId;
#endif

    public bool IsDestroyed;

    // Which Tile the Building is in
    public int TileX;
    public int TileY;

    public BuildingCraftingMgr CraftingMgr;

    // Data Components
    [SerializeField] public Constructable Constructable { get; set; }
    [SerializeField] public Location Location { get; set; }
    [SerializeField] public Occupiable Occupiable { get; set; }
    [SerializeField] public Pausable Pausable { get; set; }
    [SerializeField] public Generatable Generatable { get; set; }

    [NonSerialized] public Action OnLocationChanged;
    [NonSerialized] public Action OnPositionInTileStackChanged;

    // Accessors
    public bool IsPaused => Pausable.IsPaused;

    // TODO: Track this in building instead of recalculating
    public int NumWorkers => Town.TownWorkerMgr.NumBuildingWorkers(this);

    public BuildingData(BuildingDefn buildingDefn, int tileX, int tileY)
    {
        DefnId = buildingDefn.Id;
        TileX = tileX;
        TileY = tileY;
        Location = new(Utilities.ConvertHexTileToWorldPos(new Vector2Int(tileX, tileY)));
    }

    public void Initialize(TownData town)
    {
        Town = town;

        Occupiable = new(Defn.Occupiable, this);
        Pausable = new(Defn.Pausable, this);
        Constructable = new(Defn.Constructable, this);
        if (Defn.Generatable.CanGenerate)
            Generatable = new(Defn.Generatable, this);

        // place at top of stack
        var countMinusThis = Town.GetTileStackForHexTile(new Vector2Int(TileX, TileY)).Buildings.Count - 1;
        Location.WorldLoc.y = countMinusThis * 1.77f;

        if (Defn.CanCraft)
        {
            CraftingMgr = new(this);
        }

        if (Defn.ResourcesCanBeGatheredFromHere)
        {
            for (int i = 0; i < Defn.GatheringSpots.Count; i++)
                GatheringSpots.Add(new(this, i));
        }

        if (Defn.WorkersCanRestHere)
        {
            for (int i = 0; i < Defn.SleepingSpots.Count; i++)
                SleepingSpots.Add(new(this, i));
        }

        if (Defn.CanStoreItems)
        {
            foreach (var areaDefn in Defn.StorageAreas)
                StorageAreas.Add(new(this, areaDefn));
        }

        // Create a unified list of storage spots since I don't want to iteratve over all areas.piles.spots every time
        foreach (var area in StorageAreas)
            foreach (var pile in area.StoragePiles)
                StorageSpots.AddRange(pile.StorageSpots);

        // Resources should generally only be gathered when there's a need for them e.g. crafting; however we also
        // want a persistent low-priority need for resource gathering so that it's done if nothing else is pending
        if (Defn.CanGatherResources)
        {
            foreach (var resource in Defn.GatherableResources)
                GatheringNeeds.Add(new NeedData(this, NeedType.GatherResource, resource));
            Needs.AddRange(GatheringNeeds);
        }

        if (Defn.CanStoreItems)
        {
            ClearOutStorageNeed = new NeedData(this, NeedType.ClearStorage) { NeedCoreType = NeedCoreType.Building };
            Needs.Add(ClearOutStorageNeed);
        }

        if (Defn.CanSellGoods)
        {
            foreach (var item in Defn.GoodsThatCanBeSold)
            {
                // add need to sell (that our assigned sellers can fulfill)
                Needs.Add(new NeedData(this, NeedType.SellItem, item, 1000));

                // add need for items to sell (that other buildings can fulfill)
                var needForItemToSell = new NeedData(this, NeedType.CraftingOrConstructionMaterial, item, NumStorageSpots);
                ItemNeeds.Add(needForItemToSell);
            }
            Needs.AddRange(ItemNeeds);
        }

        // Add need for construction and materials if the building needs to be constructed
        if (Defn.CanBeConstructed && !Constructable.IsConstructed)
        {
            // ConstructionNeeds.Add(new NeedData(this, NeedType.ConstructionWorker, null, Defn.NumConstructorSpots));
            // foreach (var resource in Defn.ResourcesNeededForConstruction)
            //     ConstructionNeeds.Add(new NeedData(this, NeedType.CraftingOrConstructionMaterial, resource));
            // Needs.AddRange(ConstructionNeeds);
        }
        UpdateWorldLoc();
    }

    /**
        returns true if this building supports gathering the required resource AND there's
        an available gathering spot
    */
    public bool ResourceCanBeGatheredFromHere(ItemDefn itemDefn)
    {
        return Defn.ResourcesCanBeGatheredFromHere &&
                Defn.ResourcesThatCanBeGatheredFromHere.Contains(itemDefn) &&
                HasAvailableGatheringSpot;
    }

    public void Update()
    {
        UpdateNeedPriorities();

        foreach (var spot in GatheringSpots)
            spot.Update();

        Generatable?.Update();
        // test - spit out log every N seconds 
        // if (Defn.Generatable.CanGenerate)
        //     if (GameTime.time - lastLogOutputTime > Defn.Generatable.BaseSecondsToGenerate)
        //     {
        //         Town.ConstructBuilding(Defn.Generatable.Building, TileX, TileY);
        //         lastLogOutputTime = GameTime.time;
        //     }
    }
    // float lastLogOutputTime = 0;

    public void OnGenerated()
    {
        Town.ConstructBuilding(Defn.Generatable.Building, TileX, TileY);
    }

    public void Destroy()
    {
        Debug.Assert(!IsDestroyed, "destroying building twice");
        Debug.Assert(this != Town.Camp, "Can't destroy camp");

        IsDestroyed = true;

        Occupiable?.EvictAllOccupants();
        foreach (var need in Needs) need.Cancel();
        foreach (var worker in Town.TownWorkerMgr.Workers) worker.OnBuildingDestroyed(this);
        foreach (var spot in GatheringSpots) spot.OnBuildingDestroyed();
        foreach (var area in StorageAreas) area.OnBuildingDestroyed();
        CraftingMgr?.Destroy();
    }

    public void MoveTo(Vector2Int hexTile)
    {
        Debug.Assert(!IsDestroyed, "destroying building twice");

        TileX = hexTile.x;
        TileY = hexTile.y;
        var hexTileWorldPos = Utilities.ConvertHexTileToWorldPos(hexTile);

        // place at top of stack
        var countMinusThis = Town.GetTileStackForHexTile(new Vector2Int(TileX, TileY)).Buildings.Count;
        hexTileWorldPos.y = countMinusThis * 1.77f;

        Location previousWorldLoc = new(Location.WorldLoc);
        Location.SetWorldLoc(hexTileWorldPos);
        UpdateWorldLoc();

        // Update Workers that are assigned to or have Tasks which involve this building.
        foreach (var worker in Town.TownWorkerMgr.Workers)
            worker.OnBuildingMoved(this, previousWorldLoc);

        OnLocationChanged?.Invoke();
    }

    public void UpdateWorldLoc()
    {
        // TODO: UGH
        foreach (var area in StorageAreas) area.UpdateWorldLoc();
        foreach (var spot in GatheringSpots) spot.UpdateWorldLoc();
        foreach (var spot in SleepingSpots) spot.UpdateWorldLoc();
        CraftingMgr?.UpdateWorldLoc();
    }

    public void OnPauseToggled()
    {
        foreach (var worker in Town.TownWorkerMgr.Workers)
            worker.OnBuildingPauseToggled(this);
    }
}
