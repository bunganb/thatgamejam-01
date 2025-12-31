using System;
using UnityEngine;

public enum NoiseType { Puzzle, DogBark }

public struct NoiseInfo
{
    public NoiseType type;
    public Vector2 position;
    public float radius;

    public NoiseInfo(NoiseType type, Vector2 pos, float radius)
    {
        this.type = type;
        this.position = pos;
        this.radius = radius;
    }
}

public static class NoiseSystem
{
    public static event Action<NoiseInfo> OnNoise;

    public static void Emit(NoiseInfo noise)
    {
        Debug.Log($"[NoiseSystem] Emit {noise.type} at {noise.position} radius {noise.radius}");
        OnNoise?.Invoke(noise);
    }
}
