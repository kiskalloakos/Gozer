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

    public Vector2 FacingVector
    {
        get
        {
            switch (CurrentFacing)
            {
                case FacingDirection.Right: return Vector2.right;
                case FacingDirection.Up: return Vector2.up;
                case FacingDirection.Left: return Vector2.left;
                default: return Vector2.down;
            }
        }
    }

    private Rigidbody2D body;
    private Vector2 movement;
    private float animationClock;
    private bool wasMoving;

    void Awake() => body = GetComponent<Rigidbody2D>();

    void Update()
    {
        if (GameSessionFlow.IsBlockingGameplay) { movement = Vector2.zero; return; }
        if (VillageTime.Instance && VillageTime.Instance.IsSleeping) { movement = Vector2.zero; return; }
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (visual)
        {
            if (movement.sqrMagnitude > 0f)
            {
                // Horizontal input owns the animation on diagonals. W+D therefore
                // moves northeast while displaying the right-facing walk cycle.
                FacingDirection nextFacing;
                if (movement.x > 0f) nextFacing = FacingDirection.Right;
                else if (movement.x < 0f) nextFacing = FacingDirection.Left;
                else if (movement.y > 0f) nextFacing = FacingDirection.Up;
                else nextFacing = FacingDirection.Down;

                bool changedDirection = nextFacing != CurrentFacing;
                CurrentFacing = nextFacing;
                var frames = FramesFor(CurrentFacing);
                if (frames != null && frames.Length > 0)
                {
                    if (!wasMoving || changedDirection)
                        animationClock = FirstWalkingFrameIndex(CurrentFacing) / walkFramesPerSecond;
                    else
                        animationClock += Time.deltaTime;
                    visual.sprite = frames[Mathf.FloorToInt(animationClock * walkFramesPerSecond) % frames.Length];
                }
                wasMoving = true;
            }
            else
            {
                wasMoving = false;
                animationClock = 0f;
                var frames = FramesFor(CurrentFacing);
                if (frames != null && frames.Length > 0)
                    visual.sprite = frames[Mathf.Min(IdleFrameIndex(CurrentFacing), frames.Length - 1)];
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

    static int IdleFrameIndex(FacingDirection direction)
    {
        // The left-facing row alternates walk/idle/walk/idle, unlike the
        // other rows whose first frame is their standing pose.
        return direction == FacingDirection.Left ? 1 : 0;
    }

    static int FirstWalkingFrameIndex(FacingDirection direction)
    {
        // A tap may last less than one animation interval. Start on a frame that
        // differs from the idle pose so every direction still shows one step.
        return direction == FacingDirection.Left ? 0 : 1;
    }
}
