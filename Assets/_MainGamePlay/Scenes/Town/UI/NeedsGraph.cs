using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class ValuesAtTime
{
    // stringkey = itemId
    public Dictionary<string, List<float>> demandForItemAcrossTime = new Dictionary<string, List<float>>();
    public Dictionary<string, List<float>> supplyOfItemAcrossTime = new Dictionary<string, List<float>>();
}

public class NeedsGraph : MonoBehaviour
{
    SceneWithMap scene;
    ValuesAtTime values = new ValuesAtTime();

    List<Color> colors = new List<Color>(new Color[] { Color.red, new Color(1, .5f, 0), Color.yellow, Color.green, Color.cyan, new Color(.5f, 0, .5f) });

    public void ShowForScene(SceneWithMap scene)
    {
        gameObject.SetActive(true);
        this.scene = scene;
    }

    public void Hide() => gameObject.SetActive(false);

    private void updateSupplyAndDemandHistoricalView()
    {
        foreach (var itemId in GameDefns.Instance.ItemDefns.Keys)
        {
            if (!values.demandForItemAcrossTime.ContainsKey(itemId)) values.demandForItemAcrossTime[itemId] = new List<float>();
            if (!values.supplyOfItemAcrossTime.ContainsKey(itemId)) values.supplyOfItemAcrossTime[itemId] = new List<float>();
            values.supplyOfItemAcrossTime[itemId].Add(scene.Map.Town.Chart_GetNumOfItemInTown(itemId));
            values.demandForItemAcrossTime[itemId].Add(scene.Map.Town.Chart_GetNeedForItem(itemId));
        }

        // cull if too many
        // UGH - can't figure out how to get first (any) key in dictionary (eyeroll)
        string testId = "";
        foreach (var itemId in GameDefns.Instance.ItemDefns.Keys) { testId = itemId; break; }
        var testValues = values.demandForItemAcrossTime[testId];
        if (testValues.Count > 5000)
        {
            var numToRemove = testValues.Count - 3000;
            foreach (var key in values.demandForItemAcrossTime.Keys) values.demandForItemAcrossTime[key].RemoveRange(0, numToRemove);
            foreach (var key in values.supplyOfItemAcrossTime.Keys) values.supplyOfItemAcrossTime[key].RemoveRange(0, numToRemove);
        }
    }

    void Update()
    {
        if (scene == null || scene.Map == null || scene.Map.Town == null) return;
        if (!GameTime.IsPaused)
            updateSupplyAndDemandHistoricalView();

        // var lowerLeft = transform.position + new Vector3(0, 0, -.1f) - transform.localScale / 2, upperRight = transform.position + new Vector3(0, 0, -.1f) + transform.localScale / 2;
        var baseLoc = transform.position - transform.localScale / 2 + new Vector3(.1f, .2f, -.1f);

        drawSupply(1400, 10, 0.15f, baseLoc);
        //drawDemand(1400, 10, 0.2f, baseLoc); 
    }

    private void drawDemand(int maxFramesToRender, int frameSkip, float stepLength, Vector3 baseLoc)
    {
        var maxDemandValue = getMaxValue(values.demandForItemAcrossTime, maxFramesToRender, frameSkip);
        var yMultiplier = maxDemandValue < 16f ? 16f : 15f / maxDemandValue;

        drawValues(values.demandForItemAcrossTime, yMultiplier, maxFramesToRender, 1, frameSkip, stepLength, baseLoc, (float x, float y, float previousY, Vector3 baseLoc) =>
                    {
                        Drawing.Draw.xy.Circle(new Vector3(x, y, baseLoc.z), .05f, 0, 460);
                        if (previousY < y)
                            for (var val = previousY; val <= y; val += stepLength)
                                Drawing.Draw.xy.Circle(new Vector3(x, val, baseLoc.z), .05f, 0, 460);
                        else if (previousY > y)
                            for (var val = y; val <= previousY + stepLength; val += stepLength)
                                Drawing.Draw.xy.Circle(new Vector3(x, val, baseLoc.z), .05f, 0, 460);
                    });
    }

    private void drawSupply(int maxFramesToRender, int frameSkip, float stepLength, Vector3 baseLoc)
    {
        var maxSupplyValue = getMaxValue(values.supplyOfItemAcrossTime, maxFramesToRender, frameSkip);
        var yMultiplier = maxSupplyValue < 6f ? 1f : 5f / maxSupplyValue;

        drawValues(values.supplyOfItemAcrossTime, yMultiplier, maxFramesToRender, 2, frameSkip, stepLength, baseLoc, (float x, float y, float previousY, Vector3 baseLoc) =>
                    {
                        Drawing.Draw.ingame.Line(new Vector3(x - stepLength / 2, previousY, baseLoc.z), new Vector3(x + stepLength / 2, y, baseLoc.z));
                    });
    }

    private void drawValues(Dictionary<string, List<float>> values, float yMultiplier, int maxFramesToRender, int lineWidth, int frameSkip, float stepLength,
                            Vector3 baseLoc, Action<float, float, float, Vector3> drawFunc)
    {
        using (Drawing.Draw.ingame.WithLineWidth(lineWidth))
        {
            int num = 0;
            foreach (var demandForItemOverTime in values)
            {
                var demandValues = demandForItemOverTime.Value;
                if (demandValues.Count == 0) continue;
                int startFrame = Math.Max(0, demandValues.Count - maxFramesToRender);
                float lastX = 0f, lastY = 0f, i = 0;
                var color = colors[num % colors.Count];
                using (Drawing.Draw.ingame.WithColor(color))
                {
                    for (int frame = startFrame; frame < demandValues.Count; frame += frameSkip)
                    {
                        float x = baseLoc.x + i * stepLength;
                        float y = baseLoc.y + demandValues[frame] * yMultiplier + num * .04f;

                        if (frame == startFrame) lastY = y;
                        drawFunc(x, y, lastY, baseLoc);
                        lastY = y;
                        lastX = x;
                        i++;
                    }
                    // Drawing.Draw.ingame.Label2D(new Vector3(lastX - supplyOfItemOverTime.Key.Length * .4f, lastY + .25f, z), supplyOfItemOverTime.Key + " (" + supplyValues[endFrame] + ")", 40, color);
                    Drawing.Draw.ingame.Label2D(new Vector3(lastX + .5f, lastY + .05f, baseLoc.z), demandForItemOverTime.Key + " (" + demandValues[demandValues.Count - 1] + ")", 20, color);
                }
                num++;
            }
        }
    }

    private float getMaxValue(Dictionary<string, List<float>> values, int maxFramesToRender, int frameSkip)
    {
        var maxValue = 0f;
        foreach (var valueOverTime in values)
        {
            int startFrame = Math.Max(0, valueOverTime.Value.Count - maxFramesToRender);
            for (int frame = startFrame; frame < valueOverTime.Value.Count; frame += frameSkip)
                maxValue = Math.Max(maxValue, valueOverTime.Value[frame]);
        }
        return maxValue;
    }
}
