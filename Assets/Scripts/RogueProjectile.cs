using UnityEngine;
public class RogueProjectile : MonoBehaviour
{
    Vector2 velocity;
    Collider2D owner;
    float age;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    public void Initialize(Vector2 v, Collider2D source) { velocity = v; owner = source; }
    void Update()
    {
        float distance = velocity.magnitude * Time.deltaTime;
        int count = Physics2D.RaycastNonAlloc(transform.position, velocity.normalized, hits, distance);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider == owner || hits[i].collider.isTrigger) continue;
            Destroy(gameObject); return;
        }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        age += Time.deltaTime;
        if (age > 2) Destroy(gameObject);
    }
}
