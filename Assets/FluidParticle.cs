using UnityEngine;

public class FluidParticle
{

    public Vector2 position;
    public Vector2 velocity;
    public GameObject particle;
    public float radius = 0.25f;

    public FluidParticle(Vector2 startPos, GameObject prefab, float particleRadius)
    {
        position = startPos;
        velocity = Vector2.zero;
        radius = particleRadius;

        particle = Object.Instantiate(prefab, new Vector2(startPos.x, startPos.y), Quaternion.identity);

        float defaultSize = particle.GetComponent<SpriteRenderer>().bounds.size.x / particle.transform.localScale.x;
        float targetScale = (radius * 2f) / defaultSize;
        particle.transform.localScale = new Vector3(targetScale, targetScale, 1f);
    }

    public void UpdateParticle(float deltaTime)
    {
        velocity += new Vector2(0, -9.81f) * deltaTime;
        position += velocity * deltaTime;
        particle.transform.position = position;
    }

    public void ResolveCollisions(float left, float right, float top, float bottom, float collisionFactor)
    {
        // check the x boundaries
        if (position.x - radius < left)
        {
            position.x = left + radius;
            velocity.x = -velocity.x * collisionFactor;
        }
        else if (position.x + radius > right)
        {
            position.x = right - radius;
            velocity.x = -velocity.x * collisionFactor;
        }

        // check the y boundaries
        if (position.y - radius < bottom)
        {
            position.y = bottom + radius;
            velocity.y = -velocity.y * collisionFactor;
        }
        else if (position.y + radius > top)
        {
            position.y = top - radius;
            velocity.y = -velocity.y * collisionFactor;
        }
    }

    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        float defaultSize = particle.GetComponent<SpriteRenderer>().bounds.size.x / particle.transform.localScale.x;
        float targetScale = (radius * 2f) / defaultSize;
        particle.transform.localScale = new Vector3(targetScale, targetScale, 1f);
    }

    public float CalculateDensity(List<FluidParticle> neighbors, float h)
    {
        float density = 0;
        const float mass = 1;

        foreach (var neighbor in neighbors)
        {
            float r = Vector2.Distance(this.position, neighbor.position);
            density += neighbor.mass * FluidMath.SmoothingKernel(r, h);
        }
        return density;
    }

    float calculateProperty(Vector2 samplePoint)
    {
        float property = 0;

        for (int i = 0; i < )
    }
}
