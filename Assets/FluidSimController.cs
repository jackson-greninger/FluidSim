using UnityEngine;
using System.Collections.Generic;

public class FluidSimController : MonoBehaviour
{
    public GameObject particlePrefab;
    public int numParticles = 50;
    public float boundaryLeft = -5f;
    public float boundaryRight = 5f;
    public float boundaryBottom = -5f;
    public float boundaryTop = 5f;
    public float particleSize = 0.5f;
    public float particleSpacing = 0f;
    public float collisionFactor = 0.5f;

    private List<FluidParticle> particles = new List<FluidParticle>();

    void Start()
    {
        int particlesPerRow = Mathf.CeilToInt(Mathf.Sqrt(numParticles));
        float spacing = particleSize * 2f + particleSpacing;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        foreach (var p in particles)
        {
            p.SetRadius(particleSize);
            p.UpdateParticle(dt);
            p.ResolveCollisions(boundaryLeft, boundaryRight, boundaryTop, boundaryBottom, collisionFactor);
        }
    }

    void CreateParticles(int seed)
    {
        Random rng = new(seed);
        particleProperties = new Vector2[numParticles];

        for (int i = 0; i < numParticles; i++)
        {
            float x = (float)(rng.NextDouble() - 0.5) * (boundaryRight-boundaryLeft);
            float y = (float)(rng.NextDouble() - 0.5) * (boundaryTop-boundaryBottom);
            Vector2 pos = new Vector2(x, y);
            FluidParticle p = new FluidParticle(pos, particlePrefab, particleSize);
            particles.add(p);
            particleProperties[i] = ExampleFunction(particles[i]);
        }
    }

    float SmoothingKernel(float radius, float dst)
    {
        float volume = 3.14149 * Pow(radius, 8) / 4; // used to calculate the volume the density over the radius
        // square the radius and distance to smooth out the function
        float value = Max(0, radius * radius - dst * dst);
        // cube the kernel value to smooth out corners
        return value * value * value / volume;
    }

    // gizmos are wire objects that appear in the scene view
    // (this allows me to see the setup before i press play)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        int particlesPerRow = Mathf.CeilToInt(Mathf.Sqrt(numParticles));
        float spacing = particleSize * 2f + particleSpacing;

        for (int i = 0; i < numParticles; i++)
        {
            int row = i / particlesPerRow;
            int col = i % particlesPerRow;

            float x = (col - particlesPerRow / 2f) * spacing;
            float y = (row - particlesPerRow / 2f) * spacing;

            Gizmos.DrawWireSphere(new Vector3(x, y, 0f), particleSize);
        }
    }
}
