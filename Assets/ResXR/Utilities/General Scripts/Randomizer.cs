using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public static class Randomizer
{
    public static List<T> CreateListWithProportionOfEachValue<T>(Dictionary<T, float> proportionsByValues, int listLength,
        bool initializeListWithCapacity = true)
    {
        //TODO: add bool parameter for whether list should have a capacity
        List<T> resultList = initializeListWithCapacity ? new List<T>(listLength) : new List<T>();
        Dictionary<T, int> itemCounts = proportionsByValues.ToDictionary(pair => pair.Key, pair => (int)(listLength * pair.Value));

        int missingItemsDueToRounding = listLength - itemCounts.Values.Sum();
        AddMissingItems(itemCounts, missingItemsDueToRounding);

        foreach (KeyValuePair<T, int> itemByAmountInList in itemCounts)
        {
            resultList.AddRange(Enumerable.Repeat(itemByAmountInList.Key, itemByAmountInList.Value));
        }

        resultList.Shuffle();

        return resultList;
    }

    private static void AddMissingItems<T>(Dictionary<T, int> itemCounts, int missingItems)
    {
        List<T> items = new List<T>(itemCounts.Keys);
        for (int i = 0; i < missingItems; i++)
        {
            int randomIndex = Random.Range(0, items.Count);
            T item = items[randomIndex];
            items.RemoveAt(randomIndex);
            itemCounts[item]++;
        }
    }

    public static List<T> CreateListByChances<T>(Dictionary<T, float> chances, int listLength)
    {
        throw new NotImplementedException();
    }
}