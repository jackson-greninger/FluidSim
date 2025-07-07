using UnityEngine;
using System.Collections.Generic;
using System;

public class FluidSimController : MonoBehaviour
{
    // particle settings
    public GameObject particlePrefab;
    public float mass = 1;
    public float particleRadius = 0.2f;
    public int numParticles = 50;
    public Vector2[] points;

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
    private Vector2[] predictedPositions;
    public Vector2[] velocities;
    private List<FluidParticle> particles = new List<FluidParticle>();
    private List<FluidParticle> particlesData = new List<FluidParticle>();
    private List<ParticleView> particlesView = new List<ParticleView>();
    public float[] densities;

    // particle lookup
    private struct Entry : IComparable<Entry> {
        public int index;    // particle index into positions[]
        public uint cellKey;  // hashed cell coordinate
        public Entry(int i, uint k) { index = i; cellKey = k; }
        public int CompareTo(Entry other) => cellKey.CompareTo(other.cellKey);
    }
    private Entry[] spatialLookup;
    private int[] startIndices;

    private static readonly Vector2Int[] cellOffsets = {
        new Vector2Int(-1, -1), new Vector2Int( 0, -1), new Vector2Int( 1, -1),
        new Vector2Int(-1,  0), new Vector2Int( 0,  0), new Vector2Int( 1,  0),
        new Vector2Int(-1,  1), new Vector2Int( 0,  1), new Vector2Int( 1,  1),
    };

    void Start()
    {
        CreateParticles(12345);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        SimulationStep(dt);
    }

    void SimulationStep(float deltaTime)
    {
        // apply gravity and predict positions
        for (int i = 0; i < numParticles; i++)
        {
            //velocities[i] += Vector2.down * deltaTime;
            predictedPositions[i] = positions[i] + velocities[i] * 1 / 120f;
        }

        UpdateSpatialLookup(predictedPositions, smoothingRadius);

        // 1) density
        for (int i = 0; i < numParticles; i++) {
            densities[i] = 0;
            ForeachPointWithinRadius(positions[i], j => {
                float dst = Vector2.Distance(predictedPositions[j], predictedPositions[i]);
                densities[i] += mass * FluidMath.SmoothingKernel(smoothingRadius, dst);
            });
        }

        // 2) pressure
        for (int i = 0; i < numParticles; i++) {
            Vector2 pressureForce = Vector2.zero;
            ForeachPointWithinRadius(positions[i], j => {
                if (j == i) return;
                Vector2 offset = predictedPositions[j] - predictedPositions[i];
                float dst       = offset.magnitude;
                if (dst <= 0) return;
                Vector2 dir     = offset / dst;
                float slope     = FluidMath.SmoothingDerivative(smoothingRadius, dst);
                float sharedP   = FluidMath.CalculateSharedPressure(densities[j], densities[i]);
                pressureForce  += -sharedP * dir * slope * mass / densities[j];
            });
            velocities[i] += - (pressureForce / densities[i]) * deltaTime;
        }

        // 3) collisions and position/velocity
        for (int i = 0; i < numParticles; i++)
        {
            positions[i] += velocities[i] * deltaTime;
            var p = particles[i];
            p.Position = positions[i];
            p.Velocity = velocities[i];
            p.ResolveCollisions(boundaryLeft, boundaryRight,
                                boundaryTop, boundaryBottom,
                                collisionFactor);
            positions[i] = p.Position;
            velocities[i] = p.Velocity;
            particlesView[i].Render(positions[i], velocities[i]);
        }
    }

    public void UpdateSpatialLookup(Vector2[] points, float radius)
    {
        this.points = points;

        for (int i = 0; i < points.Length; i++)
        {
            (int cellX, int cellY) = PositionCelltoCoord(points[i], radius);
            uint cellKey = GetKeyFromHash(HashCell(cellX, cellY));
            spatialLookup[i] = new Entry(i, cellKey);
            startIndices[i] = int.MaxValue;     // reset start index
        }

        Array.Sort(spatialLookup);

        for (int i = 0; i < points.Length; i++)
        {
            uint key = spatialLookup[i].cellKey;
            uint keyPrev = i == 0 ? uint.MaxValue : spatialLookup[i - 1].cellKey;
            if (key != keyPrev)
            {
                startIndices[key] = i;
            }
        }
    }

    // so this is the function that inits our paticles
    void CreateParticles(int seed)
    {
        // init our rng object
        System.Random rng = new(seed);

        // init our holding data structures
        positions = new Vector2[numParticles];
        predictedPositions = new Vector2[numParticles];
        spatialLookup = new Entry[numParticles];
        startIndices  = new int[numParticles];

        // set up our particle properties
        particleProperties = new float[numParticles];
        velocities = new Vector2[numParticles];
        densities = new float[numParticles];

        particles.Clear();
        particlesView.Clear();
        particlesData.Clear();

        // 1) Figure out grid dimensions
        int cols = Mathf.CeilToInt(Mathf.Sqrt(numParticles));
        int rows = Mathf.CeilToInt(numParticles / (float)cols);     

        // 2) Choose your spacing so particles don't overlap initially
        float spacing = particleRadius * 4f;        

        // 3) Compute grid size
        float gridWidth  = (cols - 1) * spacing;
        float gridHeight = (rows - 1) * spacing;     

        // 4) Find the center of the simulation area
        Vector2 center = new Vector2(
          (boundaryLeft + boundaryRight)   * 0.5f,
          (boundaryBottom + boundaryTop)   * 0.5f
        );      

        // 5) Compute the starting corner so the grid is centered
        float startX = center.x - gridWidth  * 0.5f;
        float startY = center.y - gridHeight * 0.5f;   

        // 6) Spawn in a neat rows×cols grid
        int idx = 0;
        for (int y = 0; y < rows && idx < numParticles; y++) {
          for (int x = 0; x < cols && idx < numParticles; x++) {
            Vector2 pos = new Vector2(
              startX + x * spacing,
              startY + y * spacing
            );      
            // create the particle & view
            FluidParticle p = new FluidParticle(pos, particlePrefab, particleRadius, mass);
            particles.Add(p);
            particlesView.Add(p._gameobject.GetComponent<ParticleView>());      
            // store into your arrays
            positions[idx]  = pos;
            velocities[idx] = Vector2.zero;     
            idx++;
          }
        }

        // for each particle
        //for (int i = 0; i < numParticles; i++)
        //{
        //    float x = (float)(rng.NextDouble() - 0.5) * (boundaryRight - boundaryLeft); // put it in a random spot
        //    float y = (float)(rng.NextDouble() - 0.5) * (boundaryTop - boundaryBottom);
        //    Vector2 pos = new Vector2(x, y);
        //    FluidParticle p = new FluidParticle(pos, particlePrefab, particleRadius, mass);     // then create it
        //    particlesData.Add(p);
        //    particlesView.Add(p._gameobject.GetComponent<ParticleView>());
        //    particles.Add(p);                                                           // add it to our list
        //    positions[i] = new Vector2(x, y);                                           // add position to list
        //    velocities[i] = new Vector2(0f, 0f);
        //}
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
            float sharedPressure = FluidMath.CalculateSharedPressure(density, densities[index]);
            pressure += -sharedPressure * direction * slope * mass / density;
        }
        return pressure;
    }

    public void ForeachPointWithinRadius(Vector2 samplePoint, System.Action<int>  action)
    {
        (int centerX, int centerY) = PositionCelltoCoord(samplePoint, smoothingRadius);
        float sqrRadius = smoothingRadius * smoothingRadius;

        foreach (var off in cellOffsets)
        {
            uint key = GetKeyFromHash(HashCell(centerX + off.x, centerY + off.y));
            int cellStartIndex = startIndices[key];

            for (int i = cellStartIndex; i < spatialLookup.Length; i++)
            {
                if (spatialLookup[i].cellKey != key) break;

                int particleIndex = spatialLookup[i].index;
                float sqrDst = (positions[particleIndex] - samplePoint).sqrMagnitude;

                if (sqrDst <= sqrRadius)
                {
                    action(particleIndex);
                }
            }
        }
    }

    // convert a position of a particle to a cell value
    public (int x, int y) PositionCelltoCoord(Vector2 point, float radius)
    {
        int cellX = (int)(point.x / radius);
        int cellY = (int)(point.y / radius);
        return (cellX, cellY);
    }

    // convert a cell coordinate to a single number
    // hash collisions are unavoidable, but this is ok for our purposes
    public uint HashCell(int cellX, int cellY) {
        uint a = (uint)cellX * 15823;
        uint b = (uint)cellY * 9737333;
        return a + b;
    }

    // wrap the hash value around the length of the array
    public uint GetKeyFromHash(uint hash)
    {
        return hash % (uint)spatialLookup.Length;
    }
}
