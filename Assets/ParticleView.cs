using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParticleView : MonoBehaviour {
    public Gradient speedGradient;
    SpriteRenderer _sr;
    MaterialPropertyBlock _mpb;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

  public void Render(Vector2 pos, Vector2 vel) {
    // 1) move
    transform.position = new Vector2(pos.x, pos.y);

    // 2) compute t = speed/maxSpeed, clamped to [0,1]
    float speed = vel.magnitude;
    float t = 0f;
    t = Mathf.Clamp01(speed/5f);  
    
    Color col = speedGradient.Evaluate(t);

    // 3) push that color into the shader's _Color property
    _sr.GetPropertyBlock(_mpb);
    _mpb.SetColor("_Color", col);
    _sr.SetPropertyBlock(_mpb);
  }
}
