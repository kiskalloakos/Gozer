using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RogueController : MonoBehaviour
{
    public SpriteRenderer visual;
    public Animator stanceAnimator;
    public Sprite standing, firing;
    public float speed = 4f;
    [Range(0f, 1f)] public float firingMoveSpeedMultiplier = .45f;
    private Rigidbody2D body;
    private Vector2 move;
    private bool left;
    public float shotFrameSeconds = .055f;
    public float heldShotFrameSeconds = .13f;
    public Sprite[] shotFrames;
    private float shotClock = -1;
    private bool shotLeft;
    private int lastFiredStep = -1;
    private bool releaseRequested;
    private int finishAfterStep = -1;
    public int ShotsFired { get; private set; }
    private Sprite boltSprite;
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (!stanceAnimator && visual) stanceAnimator = visual.GetComponent<Animator>();
        // Hand-authored simplified firing sheet: stance, raise, aim, fire.
        shotFrames = Slice("ShootSimplified", 4, 1, 42f, new float[] { .5f, .5f, .5f, .5f }, .5f);
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
                float pointerWorldX = camera.ScreenToWorldPoint(Input.mousePosition).x;
                left = pointerWorldX < transform.position.x;
            }
            shotClock = 0;
            if (stanceAnimator) stanceAnimator.enabled = false;
            shotLeft = left;
            lastFiredStep = -1;
            releaseRequested = false;
            finishAfterStep = -1;
        }
        if (shotClock >= 0)
        {
            // A tap still completes a whole shot. Releasing only asks us to stop
            // after the next muzzle-flash frame has finished.
            if (!Input.GetMouseButton(0)) releaseRequested = true;

            shotClock += Time.deltaTime;
            int frame;
            int sustainedStep = -1;
            if (shotClock < shotFrameSeconds * 2)
            {
                // First click: stance, then raise the arm.
                frame = Mathf.FloorToInt(shotClock / shotFrameSeconds);
            }
            else
            {
                // Held fire: repeat the aim and muzzle-flash frames.
                sustainedStep = Mathf.FloorToInt((shotClock - shotFrameSeconds * 2) / heldShotFrameSeconds);
                frame = 2 + sustainedStep % 2;
                if (frame == 3 && sustainedStep != lastFiredStep)
                {
                    Fire();
                    lastFiredStep = sustainedStep;
                    if (releaseRequested) finishAfterStep = sustainedStep;
                }
            }
            visual.sprite = shotFrames.Length > 0 ? shotFrames[Mathf.Min(frame, shotFrames.Length-1)] : firing;
            visual.flipX = shotLeft;
            if (finishAfterStep >= 0 && sustainedStep > finishAfterStep) EndShooting();
        }
        else
        {
            // The Animator owns the five-frame breathing stance while not shooting.
            visual.flipX = left;
        }
        visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
    }
    void FixedUpdate()
    {
        float moveSpeed = speed * (shotClock >= 0 ? firingMoveSpeedMultiplier : 1f);
        body.MovePosition(body.position + move * moveSpeed * Time.fixedDeltaTime);
    }
    void Fire()
    {
        ShotsFired++;
        float direction = shotLeft ? -1 : 1;
        // The re-saved firing art has a lower weapon: start at its actual muzzle.
        var origin = transform.position + new Vector3(direction * 1.28f, .55f, 0);
        var bolt = new GameObject("Energy bolt"); bolt.transform.position = origin;
        var sr = bolt.AddComponent<SpriteRenderer>(); sr.sprite = boltSprite; sr.sortingOrder = visual.sortingOrder + 1;
        bolt.AddComponent<RogueProjectile>().Initialize(new Vector2(direction * 14,0), GetComponent<Collider2D>());
    }
    void EndShooting()
    {
        shotClock = -1;
        if (stanceAnimator)
        {
            stanceAnimator.enabled = true;
            stanceAnimator.Play("Rogue_Stance", 0, 0f);
        }
    }

}
