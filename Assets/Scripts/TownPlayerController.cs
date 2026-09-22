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
    [Header("Melee animation")]
    public Texture2D meleeAttackSheet;
    [Min(.01f)] public float meleeAttackFramesPerSecond = 12f;

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
    private Sprite[][] meleeAttackFrames;
    private FacingDirection meleeAttackDirection;
    private int meleeAttackFrame;
    private float nextMeleeAttackFrameTime;
    private bool playingMeleeAttack;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (!meleeAttackSheet)
            meleeAttackSheet = Resources.Load<Texture2D>("Player/melee_attacks");
        if (meleeAttackSheet) meleeAttackSheet.filterMode = FilterMode.Point;
        meleeAttackFrames = CreateMeleeAttackFrames(meleeAttackSheet);
    }

    void Update()
    {
        if (GameSessionFlow.IsBlockingGameplay) { movement = Vector2.zero; return; }
        if (VillageTime.Instance && VillageTime.Instance.IsSleeping) { movement = Vector2.zero; return; }
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (visual)
        {
            if (playingMeleeAttack)
            {
                AdvanceMeleeAttack();
                visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
                return;
            }
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

    public void PlayMeleeAttack(Vector2 direction)
    {
        if (meleeAttackFrames == null || meleeAttackFrames.Length == 0) return;
        meleeAttackDirection = DirectionFromVector(direction);
        CurrentFacing = meleeAttackDirection;
        meleeAttackFrame = 0;
        nextMeleeAttackFrameTime = 0f;
        playingMeleeAttack = true;
        if (visual && meleeAttackFrames[(int)meleeAttackDirection].Length > 0)
        {
            visual.sprite = meleeAttackFrames[(int)meleeAttackDirection][0];
            meleeAttackFrame = 1;
        }
    }

    void AdvanceMeleeAttack()
    {
        var frames = meleeAttackFrames[(int)meleeAttackDirection];
        if (frames == null || frames.Length == 0)
        {
            playingMeleeAttack = false;
            return;
        }
        if (Time.time < nextMeleeAttackFrameTime) return;
        if (meleeAttackFrame >= frames.Length)
        {
            playingMeleeAttack = false;
            animationClock = 0f;
            wasMoving = false;
            return;
        }
        visual.sprite = frames[meleeAttackFrame++];
        nextMeleeAttackFrameTime = Time.time + 1f / meleeAttackFramesPerSecond;
    }

    static FacingDirection DirectionFromVector(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x < 0f ? FacingDirection.Left : FacingDirection.Right;
        return direction.y > 0f ? FacingDirection.Up : FacingDirection.Down;
    }

    static Sprite[][] CreateMeleeAttackFrames(Texture2D sheet)
    {
        var result = new Sprite[4][];
        for (int direction = 0; direction < result.Length; direction++)
            result[direction] = System.Array.Empty<Sprite>();
        if (!sheet || sheet.width < 16 || sheet.height < 32) return result;

        const int frameWidth = 16;
        const int frameHeight = 32;
        int columns = sheet.width / frameWidth;
        int rows = sheet.height / frameHeight;
        int frameCount = Mathf.Min(columns, 3);
        for (int rowFromTop = 0; rowFromTop < Mathf.Min(rows, 4); rowFromTop++)
        {
            int y = (rows - rowFromTop - 1) * frameHeight;
            var frames = new Sprite[frameCount];
            for (int column = 0; column < frameCount; column++)
                frames[column] = Sprite.Create(sheet,
                    new Rect(column * frameWidth, y, frameWidth, frameHeight),
                    new Vector2(.5f, 0f), PixelArtStandard.PixelsPerUnit);
            result[rowFromTop] = frames;
        }
        return result;
    }

    void OnDestroy()
    {
        if (meleeAttackFrames == null) return;
        foreach (var direction in meleeAttackFrames)
            if (direction != null)
                foreach (var frame in direction)
                    if (frame) Destroy(frame);
    }

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
