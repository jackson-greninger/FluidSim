using UnityEngine;
using System.Collections.Generic;

public class FluidSimController : MonoBehaviour
{
    // particle settings
    public GameObject particlePrefab;
    public float mass = 1;
    public float particleRadius = 0.2f;
    public int numParticles = 50;

    // SPH settings
    public float collisionFactor = 0.5f;
    public float smoothingRadius = 0.2f;
    public Vector2 gravity = new Vector2(0f, -9.81f);

    // simulation bounds
    public float boundaryLeft = -5f;
    public float boundaryRight = 5f;
    public float boundaryBottom = -5f;
    public float boundaryTop = 5f;

    // miscellaneous
    public float[] particleProperties;
    public Vector2[] positions;
    public Vector2[] velocities;
    private List<FluidParticle> particles = new List<FluidParticle>();
    public float[] densities;

    void Start()
    {
        CreateParticles(12345);
    }

    void Update()
    { 
        float dt = Time.deltaTime;
        SimulationStep(dt);

        // update visuals from simulation state
        for (int i = 0; i < particles.Count; i++) {
            var p = particles[i];
            p.Position = positions[i];
            p.Velocity = velocities[i];
            p.SetRadius(particleRadius);
            p.UpdateParticle(dt);
        }
    }

    // so this is the function that inits our paticles
    void CreateParticles(int seed)
    {
        // init our rng object
        System.Random rng = new(seed);
        positions = new Vector2[numParticles];
        // set up our particle properties
        particleProperties = new float[numParticles];
        velocities = new Vector2[numParticles];
        densities  = new float  [numParticles];

        // for each particle
        for (int i = 0; i < numParticles; i++)
        {
            float x = (float)(rng.NextDouble() - 0.5) * (boundaryRight - boundaryLeft); // put it in a random spot
            float y = (float)(rng.NextDouble() - 0.5) * (boundaryTop - boundaryBottom);
            Vector2 pos = new Vector2(x, y);
            FluidParticle p = new FluidParticle(pos, particlePrefab, particleRadius, mass);     // then create it
            particles.Add(p);                                                           // add it to our list
            positions[i] = new Vector2(x, y);                                           // add position to list
            velocities[i] = new Vector2(0f, 0f);
        }
    }

    void SimulationStep(float deltaTime)
    {
        for (int i = 0; i < numParticles; i++)
        {
            // velocities[i] += gravity * deltaTime;
            densities[i] = CalculateDensity(positions[i]);
        }

        for (int i = 0; i < numParticles; i++)
        {
            Vector2 pressureForce = CalculatePressureForce(i);
            Vector2 pressureAcceleration = pressureForce / densities[i];
            velocities[i] += -pressureAcceleration * deltaTime;
        }

        for (int i = 0; i < numParticles; i++)
        {
            positions[i] += velocities[i] * deltaTime;

            // sync into particle, resolve, then sync back
            var p = particles[i];
            p.Position = positions[i];
            p.Velocity = velocities[i];

            p.ResolveCollisions(boundaryLeft, boundaryRight,
                                boundaryTop,  boundaryBottom,
                                collisionFactor);

            positions[i] = p.Position;
            velocities[i] = p.Velocity;
        }
    }

    public float CalculateDensity(Vector2 samplePoint)
    {
        float density = 0;

        foreach (Vector2 position in positions)
        {
            float dst = Vector2.Distance(position, samplePoint);
            density += mass * FluidMath.SmoothingKernel(smoothingRadius, dst);
        }

        return density;
    }

    Vector2 CalculatePressureForce(int index)
    {   
        // start with a blank property
        Vector2 pressure = Vector2.zero;
        // loop over all particles
        for (int i = 0; i < numParticles; i++)
        {
            if (index == i) continue;

            Vector2 offset = positions[i] - positions[index];
            // get the distance from the samplePoint
            float dst = offset.magnitude;
            Vector2 direction = offset / dst;
            // calculate the influence given the particles distance and the radius
            float slope = FluidMath.SmoothingDerivative(smoothingRadius, dst);
            // find the density of the particle
            float density = densities[i];
            pressure += -FluidMath.DensityToPressure(density) * direction * slope * mass / density;
        }
        return pressure;
    }
}
