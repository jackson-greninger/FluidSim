using UnityEngine;

public static class FluidMath
{
    public static float targetDensity = 0.5f;
    public static float pressureMultiplier = 10f;
    
    public static float SmoothingKernel(float radius, float dst)
    {
        if (dst >= radius) return 0;

        // this is our function for the influence of a particle given its smoothing radius
        float volume = Mathf.PI * Mathf.Pow(radius, 4) / 6;
        return (radius - dst) * (radius - dst) / volume;
    }

    public static float SmoothingDerivative(float radius, float dst)
    {
        if (dst <= 0f || dst >= radius) return 0f;
        float coef = -45f / (Mathf.PI * Mathf.Pow(radius, 6));
        return coef * (radius - dst) * (radius - dst);
    }

    public static float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = DensityToPressure(densityA);
        float pressureB = DensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    public static float DensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = densityError * pressureMultiplier;
        return pressure;
    }
}