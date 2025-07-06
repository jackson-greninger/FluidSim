using UnityEngine;

public static class FluidMath
{
    public static float targetDensity = 0.5f;
    public static float pressureMultiplier = 0.5f;
    
    public static float SmoothingKernel(float radius, float dst)
    {
        float volume = Mathf.PI * Mathf.Pow(radius, 8) / 4; // used to calculate the volume the density over the radius
        // square the radius and distance to smooth out the function
        float value = Mathf.Max(0f, radius * radius - dst * dst);
        // cube the kernel value to smooth out corners
        return value * value * value / volume;
    }

    public static float SmoothingDerivative(float radius, float dst)
    {
        if (dst >= radius) return 0;
        float f = radius * radius - dst * dst;
        float scale = -24 / (Mathf.PI * Mathf.Pow(radius, 8));
        return scale * dst * f * f;
    }

    public static float DensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = densityError * pressureMultiplier;
        return pressure;
    }
}