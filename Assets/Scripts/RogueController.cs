using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RogueController : MonoBehaviour
{
    public SpriteRenderer visual;
    public Sprite standing, firing;
    public float speed = 4f;
    private Rigidbody2D body;
    private Vector2 move;
    private bool left;
    public float shotFrameSeconds = .055f;
    public Sprite[] shotFrames;
    private float shotClock = -1;
    private bool shotLeft, emitted;
    public int ShotsFired { get; private set; }
    private Sprite boltSprite;
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shotFrames = Slice("Shoot", 3, 2, 210f, new float[] { .46f,.46f,.42f,.46f,.46f,.42f });
        var texture = new Texture2D(8, 2); texture.filterMode = FilterMode.Point;
        var colors = new Color[16]; for (int i = 0; i < colors.Length; i++) colors[i] = new Color(1f,.65f,.1f);
        texture.SetPixels(colors); texture.Apply();
        boltSprite = Sprite.Create(texture, new Rect(0,0,8,2), new Vector2(.5f,.5f), 24);
    }
    Sprite[] Slice(string name, int columns, int rows, float ppu, float[] anchors, float groundPivot = .025f)
    {
        var texture = Resources.Load<Texture2D>("RogueAnimation/" + name);
        if (!texture) { Debug.LogError("Missing animation sheet: " + name); return new Sprite[0]; }
        int w = texture.width / columns, h = texture.height / rows;
        var frames = new Sprite[columns * rows];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Sprite.Create(texture, new Rect((i % columns)*w, texture.height-(i/columns+1)*h,w,h), new Vector2(anchors[i], groundPivot), ppu, 0, SpriteMeshType.FullRect);
        return frames;
    }
    void Update()
    {
        move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (move.x != 0) left = move.x < 0;
        if (shotClock < 0 && Input.GetMouseButton(0))
        {
            var camera = Camera.main;
            if (camera)
            {
                // Compare in screen space: aim follows the mouse, not movement.
                float playerScreenX = camera.WorldToScreenPoint(transform.position).x;
                left = Input.mousePosition.x < playerScreenX;
            }
            shotClock = 0;
            shotLeft = left;
            emitted = false;
        }
        if (shotClock >= 0)
        {
            shotClock += Time.deltaTime;
            int frame = Mathf.FloorToInt(shotClock / shotFrameSeconds);
            if (frame >= 3 && !emitted) { Fire(); emitted = true; }
            visual.sprite = shotFrames.Length > 0 ? shotFrames[Mathf.Min(frame, shotFrames.Length-1)] : firing;
            visual.flipX = shotLeft;
            if (shotClock >= shotFrameSeconds * 6) shotClock = -1;
        }
        else
        {
            // Until a hand-authored run exists, movement deliberately keeps the standing pose.
            visual.sprite = shotFrames.Length > 0 ? shotFrames[0] : standing;
            visual.flipX = left;
        }
        visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
    }
    void FixedUpdate() { body.MovePosition(body.position + move * speed * (shotClock >= 0 ? 0 : 1) * Time.fixedDeltaTime); }
    void Fire()
    {
        ShotsFired++;
        float direction = shotLeft ? -1 : 1;
        var origin = transform.position + new Vector3(direction * 1.06f, 1.82f, 0);
        var bolt = new GameObject("Energy bolt"); bolt.transform.position = origin;
        var sr = bolt.AddComponent<SpriteRenderer>(); sr.sprite = boltSprite; sr.sortingOrder = visual.sortingOrder + 1;
        bolt.AddComponent<RogueProjectile>().Initialize(new Vector2(direction * 14,0), GetComponent<Collider2D>());
    }

}
