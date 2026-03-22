using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public static class ListExtensions
{
    //Shuffles using the Fisher-Yates algorithm
    public static void Shuffle<T>(this IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public static void ShuffleWithMaximumOfSameTypeInARow<T>(this IList<T> roundsList,
        int maximumRoundsOfSameTypeInARow, Func<T, T, bool> differentTypeCondition)
    {
        while (!IsListValid(roundsList, maximumRoundsOfSameTypeInARow, differentTypeCondition))
        {
            roundsList.Shuffle();
        }
    }

    public static void ShuffleWithMaximumOfSameTypeInARow<T>(this IList<T> roundsList,
        int maximumRoundsOfSameTypeInARow, Func<T, T, bool> differentTypeCondition, Func<IList<T>, bool> additionalValidCondition)
    {
        while (!additionalValidCondition(roundsList) || !IsListValid(roundsList, maximumRoundsOfSameTypeInARow, differentTypeCondition))
        {
            roundsList.Shuffle();
        }
    }


    private static bool IsListValid<T>(IList<T> itemsList, int maximumRoundsOfSameTypeInARow,
        Func<T, T, bool> differentTypeCondition)
    {
        T previousItem = itemsList[0];
        int numberOfItemsOfSameTypeInARow = 1;
        for (int i = 1; i < itemsList.Count; i++)
        {
            T currentItem = itemsList[i];
            numberOfItemsOfSameTypeInARow =
                differentTypeCondition(previousItem, currentItem) ? 1 : numberOfItemsOfSameTypeInARow + 1;
            if (numberOfItemsOfSameTypeInARow > maximumRoundsOfSameTypeInARow)
            {
                return false;
            }

            previousItem = currentItem;
        }

        return true;
    }

    public static T GetRandomItem<T>(this IList<T> list)
    {
        int itemIndex = Random.Range(0, list.Count);
        return list[itemIndex];
    }

    public static void PrintItems<T>(this IList<T> list)
    {
        foreach (T item in list)
        {
            Debug.Log(item.ToString());
        }
    }
}