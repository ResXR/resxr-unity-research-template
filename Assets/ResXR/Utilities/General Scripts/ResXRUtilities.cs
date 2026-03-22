using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class ResXRUtilities
{
    public static string GetFormattedDateTime(bool includeTime = false)
    {
        DateTime now = DateTime.Now;
        if (includeTime)
        {
            return now.ToString("yy.MM.dd_HH-mm");
        }
        else
        {
            return now.ToString("yyyy.MM.dd");
        }
    }

    // Gets a line and normilized target position on it, and returns this position as a world-position.
    public static Vector3 GetPointOnLineFromNormalizedValue(Vector3 lineStart, Vector3 lineEnd, float valueNormalized)
    {
        Vector3 lineVec = lineEnd - lineStart;
        valueNormalized = Mathf.Clamp01(valueNormalized);

        return lineStart + lineVec * valueNormalized;
    }

    // Gets a line and a point on it.
    // Returns the normalized value representing the distance of point from start / line length.
    public static float GetNormalizedValueFromPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineVector = lineEnd - lineStart;
        // assuming point is on line.
        Vector3 pointVector = point - lineStart;

        return pointVector.magnitude / lineVector.magnitude;
    }

    // Gets a line and a point in space, returns the closest point on line to point.
    public static Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float magnitudeMax = lineDirection.magnitude;
        lineDirection = lineDirection.normalized;

        Vector3 pointVector = point - lineStart;
        float projectionLength = Vector3.Dot(pointVector, lineDirection);
        projectionLength = Mathf.Clamp(projectionLength, 0, magnitudeMax);
        return lineStart + lineDirection * projectionLength;
    }

    // gets an object, return a dictionary<string,string> with all obj members names as keys and data as values.
    public static Dictionary<string, string> SerializeObject(object obj)
    {
        var json = JsonConvert.SerializeObject(obj);
        var jObject = JObject.Parse(json);

        var dictionary = new Dictionary<string, string>();

        foreach (var property in jObject.Properties())
        {
            dictionary[property.Name] = property.Value.ToString();
        }

        return dictionary;
    }
}


