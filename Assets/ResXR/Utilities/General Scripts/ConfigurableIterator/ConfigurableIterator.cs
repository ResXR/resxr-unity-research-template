using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigurableIterator<T>
{
    public int CurrentIndex;
    private T[] _arrayToIterateOn;
    private EIterationOrder _iterationOrder;

    public ConfigurableIterator(T[] arrayToIterateOn, EIterationOrder iterationOrder)
    {
        _arrayToIterateOn = arrayToIterateOn;
        _iterationOrder = iterationOrder;
    }

    public T Next()
    {
        Debug.Log(_iterationOrder);
        int previousIndex = CurrentIndex;
        switch (_iterationOrder)
        {
            case EIterationOrder.InOrder:
                CurrentIndex = (previousIndex + 1) % _arrayToIterateOn.Length;
                break;
            case EIterationOrder.Random:
                CurrentIndex = Random.Range(0, _arrayToIterateOn.Length);
                break;
            case EIterationOrder.RandomNoRepeats:
                CurrentIndex = GetRandomNonRepeatingIndex(previousIndex);
                break;
        }

        return _arrayToIterateOn[CurrentIndex];
    }

    private int GetRandomNonRepeatingIndex(int previousIndex)
    {
        int nonRepeatingIndex;
        do
        {
            nonRepeatingIndex = Random.Range(0, _arrayToIterateOn.Length);
        } while (_arrayToIterateOn.Length > 1 && nonRepeatingIndex == previousIndex);


        return nonRepeatingIndex;
    }
}