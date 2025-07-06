using UnityEngine;

public class FluidParticle
{

    public Vector2 Position;
    public Vector2 Velocity;
    public float Mass;
    public float Radius = 0.25f;

    private GameObject _gameobject;

    public FluidParticle(Vector2 startPos, GameObject prefab, float radius, float mass)
    {
        Position = startPos;
        Velocity = Vector2.zero;
        Radius = radius;
        Mass = mass;

        _gameobject = Object.Instantiate(prefab, startPos, Quaternion.identity);
        UpdateScale();
    }

    public void UpdateParticle(float deltaTime)
    {
        _gameobject.transform.position = Position;
    }

    public void ResolveCollisions(float left, float right, float top, float bottom, float collisionFactor)
    {
        // check the x boundaries
        if (Position.x - Radius < left)
        {
            Position.x = left + Radius;
            Velocity.x = -Velocity.x * collisionFactor;
        }
        else if (Position.x + Radius > right)
        {
            Position.x = right - Radius;
            Velocity.x = -Velocity.x * collisionFactor;
        }

        // check the y boundaries
        if (Position.y - Radius < bottom)
        {
            Position.y = bottom + Radius;
            Velocity.y = -Velocity.y * collisionFactor;
        }
        else if (Position.y + Radius > top)
        {
            Position.y = top - Radius;
            Velocity.y = -Velocity.y * collisionFactor;
        }
    }

    public void SetRadius(float newRadius)
    {
        Radius = newRadius;
        UpdateScale();
    }

    private void UpdateScale()
    {
        var sr = _gameobject.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            return;
        }
        float defaultSize = sr.bounds.size.x / _gameobject.transform.localScale.x;
        float targetScale = (Radius * 2f) / defaultSize;
        _gameobject.transform.localScale = new Vector3(targetScale, targetScale, 1f);
    }

}
