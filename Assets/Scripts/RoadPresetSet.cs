using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Road/Road Preset Set")]
public class RoadPresetSet : ScriptableObject
{
    public Vector3 straightLocalPositionOffset;
    public float straightLocalYawOffset;

    public List<TurnPreset> turnPresets = new List<TurnPreset>();
}

[Serializable]
public class TurnPreset
{
    public float angle;
    public TurnSide side;

    public Vector3 localPositionOffset;
    public float localYawOffset;
}

public enum TurnSide
{
    Left,
    Right
}