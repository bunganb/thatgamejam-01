using System;
using UnityEngine;

public enum NoiseType { Puzzle, DogBark }

public struct NoiseInfo
{
    public NoiseType type;
    public Vector2 position;
    public float radius;
    public string roomID; // ID room/level tempat noise terjadi

    public NoiseInfo(NoiseType type, Vector2 pos, float radius, string roomID = "")
    {
        this.type = type;
        this.position = pos;
        this.radius = radius;
        this.roomID = roomID;
    }
}

public static class NoiseSystem
{
    public static event Action<NoiseInfo> OnNoise;

    public static void Emit(NoiseInfo noise)
    {
        string roomInfo = string.IsNullOrEmpty(noise.roomID) ? "global" : noise.roomID;
        Debug.Log($"[NoiseSystem] Emit {noise.type} at {noise.position} radius {noise.radius} in room: {roomInfo}");
        OnNoise?.Invoke(noise);
    }
}