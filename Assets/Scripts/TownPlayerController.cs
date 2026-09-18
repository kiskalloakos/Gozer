using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TownPlayerController : MonoBehaviour
{
    public enum FacingDirection { Down, Right, Up, Left }

    public float speed = 4f;
    public SpriteRenderer visual;
    public float walkFramesPerSecond = PixelArtStandard.WalkFramesPerSecond;
    public Sprite[] walkDown;
    public Sprite[] walkRight;
    public Sprite[] walkUp;
    public Sprite[] walkLeft;

    public FacingDirection CurrentFacing { get; private set; } = FacingDirection.Down;

    private Rigidbody2D body;
    private Vector2 movement;
    private float animationClock;

    void Awake() => body = GetComponent<Rigidbody2D>();

    void Update()
    {
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (visual)
        {
            if (movement.sqrMagnitude > 0f)
            {
                // Horizontal input owns the animation on diagonals. W+D therefore
                // moves northeast while displaying the right-facing walk cycle.
                if (movement.x > 0f) CurrentFacing = FacingDirection.Right;
                else if (movement.x < 0f) CurrentFacing = FacingDirection.Left;
                else if (movement.y > 0f) CurrentFacing = FacingDirection.Up;
                else CurrentFacing = FacingDirection.Down;

                animationClock += Time.deltaTime;
                var frames = FramesFor(CurrentFacing);
                if (frames != null && frames.Length > 0)
                    visual.sprite = frames[Mathf.FloorToInt(animationClock * walkFramesPerSecond) % frames.Length];
            }
            else
            {
                animationClock = 0f;
                var frames = FramesFor(CurrentFacing);
                if (frames != null && frames.Length > 0) visual.sprite = frames[0];
            }
            visual.flipX = false;
            visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }
    }

    void FixedUpdate() => body.MovePosition(body.position + movement * speed * Time.fixedDeltaTime);

    Sprite[] FramesFor(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.Right: return walkRight;
            case FacingDirection.Up: return walkUp;
            case FacingDirection.Left: return walkLeft;
            default: return walkDown;
        }
    }
}
