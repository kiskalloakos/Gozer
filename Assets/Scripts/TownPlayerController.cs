using System;
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
    public int CurrentAnimationFrameIndex { get; private set; }
    public bool IsWalking { get; private set; }
    public bool IsPlayingMeleeAttack => playingMeleeAttack;
    public int CurrentMeleeAttackFrameIndex { get; private set; }

    public void SetEditorPreviewFacing(FacingDirection facing)
        => SetEditorPreviewAnimation(facing, false, 0);

    public void SetEditorPreviewAnimation(FacingDirection facing, bool walking, int frameIndex)
    {
        if (Application.isPlaying) return;

        CurrentFacing = facing;
        IsWalking = walking;
        wasMoving = walking;
        animationClock = 0f;
        if (!visual) return;

        var frames = FramesFor(CurrentFacing);
        if (frames != null && frames.Length > 0)
        {
            CurrentAnimationFrameIndex = walking
                ? Mathf.Clamp(frameIndex, 0, frames.Length - 1)
                : Mathf.Min(IdleFrameIndex(CurrentFacing), frames.Length - 1);
            visual.sprite = frames[CurrentAnimationFrameIndex];
        }
        visual.flipX = false;
        visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
    }

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
    private bool ownsMeleeAttackFrames;
    private FacingDirection meleeAttackDirection;
    private int meleeAttackFrame;
    private float nextMeleeAttackFrameTime;
    private bool playingMeleeAttack;
    private Action<int> meleeAttackFrameStarted;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        // The HUD used to rely solely on a scene-load callback. Unity can
        // reload scripts while Play Mode is running without replaying that
        // callback, leaving the town with no quickbar, health, or energy UI.
        // The player is present in every gameplay scene, so it is the stable
        // place to guarantee the HUD.
        EnsureGameplayHUD();
        if (meleeAttackSheet) meleeAttackSheet.filterMode = FilterMode.Point;
        meleeAttackFrames = CreateMeleeAttackFrames(meleeAttackSheet);
        ownsMeleeAttackFrames = true;
    }

    void Update()
    {
        EnsureGameplayHUD();
        if (GameSessionFlow.IsBlockingGameplay) { movement = Vector2.zero; IsWalking = false; return; }
        if (VillageTime.Instance && VillageTime.Instance.IsSleeping)
        {
            movement = Vector2.zero;
            IsWalking = false;
            return;
        }
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        IsWalking = movement.sqrMagnitude > 0f && !playingMeleeAttack;
        if (visual)
        {
            if (playingMeleeAttack)
            {
                // Keep the player root stable for the duration of the swing.
                // Otherwise FixedUpdate can move the body while the attack
                // sprite is playing, making the character appear to hop.
                movement = Vector2.zero;
                IsWalking = false;
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
                    CurrentAnimationFrameIndex = Mathf.FloorToInt(animationClock * walkFramesPerSecond) % frames.Length;
                    visual.sprite = frames[CurrentAnimationFrameIndex];
                }
                wasMoving = true;
            }
            else
            {
                wasMoving = false;
                animationClock = 0f;
                var frames = FramesFor(CurrentFacing);
                if (frames != null && frames.Length > 0)
                {
                    CurrentAnimationFrameIndex = Mathf.Min(IdleFrameIndex(CurrentFacing), frames.Length - 1);
                    visual.sprite = frames[CurrentAnimationFrameIndex];
                }
            }
            visual.flipX = false;
            visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }
    }

    void EnsureGameplayHUD()
    {
        if (!GetComponent<ExpeditionHUD>()) gameObject.AddComponent<ExpeditionHUD>();
    }

    void FixedUpdate()
    {
        if (playingMeleeAttack) return;
        body.MovePosition(body.position + movement * speed * Time.fixedDeltaTime);
    }

    public void PlayMeleeAttack(Vector2 direction)
    {
        if (meleeAttackFrames == null || meleeAttackFrames.Length == 0) return;
        PlayMeleeAttack(direction, null, 16, 32, 3, meleeAttackFramesPerSecond);
    }

    public void PlayMeleeAttack(Vector2 direction, Texture2D sheet, int frameWidth,
        int frameHeight, int frameCount, float framesPerSecond)
        => PlayMeleeAttack(direction, sheet, frameWidth, frameHeight, frameCount,
            framesPerSecond, null, null, null, null);

    public void PlayMeleeAttack(Vector2 direction, Texture2D sheet, int frameWidth,
        int frameHeight, int frameCount, float framesPerSecond,
        int[] downOrder, int[] rightOrder, int[] upOrder, int[] leftOrder,
        Action<int> onFrameStarted = null)
    {
        if (sheet)
        {
            DestroyMeleeAttackFrames();
            meleeAttackFrames = CreateMeleeAttackFrames(sheet, frameWidth, frameHeight, frameCount,
                downOrder, rightOrder, upOrder, leftOrder);
            ownsMeleeAttackFrames = true;
        }
        BeginMeleeAttack(direction, framesPerSecond, onFrameStarted);
    }

    public void PlayMeleeAttack(Vector2 direction, Sprite[] downFrames, Sprite[] rightFrames,
        Sprite[] upFrames, Sprite[] leftFrames, float framesPerSecond,
        Action<int> onFrameStarted = null)
    {
        DestroyMeleeAttackFrames();
        meleeAttackFrames = new[]
        {
            downFrames ?? System.Array.Empty<Sprite>(),
            rightFrames ?? System.Array.Empty<Sprite>(),
            upFrames ?? System.Array.Empty<Sprite>(),
            leftFrames ?? System.Array.Empty<Sprite>()
        };
        ownsMeleeAttackFrames = false;
        BeginMeleeAttack(direction, framesPerSecond, onFrameStarted);
    }

    void BeginMeleeAttack(Vector2 direction, float framesPerSecond, Action<int> onFrameStarted)
    {
        if (meleeAttackFrames == null || meleeAttackFrames.Length == 0) return;
        meleeAttackFramesPerSecond = Mathf.Max(.01f, framesPerSecond);
        meleeAttackDirection = DirectionFromVector(direction);
        CurrentFacing = meleeAttackDirection;
        IsWalking = false;
        meleeAttackFrame = 0;
        CurrentMeleeAttackFrameIndex = 0;
        // The first frame is assigned immediately below, so give it a full
        // frame interval before AdvanceMeleeAttack moves on to frame 1.
        nextMeleeAttackFrameTime = Time.time + 1f / meleeAttackFramesPerSecond;
        playingMeleeAttack = true;
        meleeAttackFrameStarted = onFrameStarted;
        if (visual && meleeAttackFrames[(int)meleeAttackDirection].Length > 0)
        {
            visual.sprite = meleeAttackFrames[(int)meleeAttackDirection][0];
            meleeAttackFrame = 1;
            meleeAttackFrameStarted?.Invoke(0);
        }
        else
        {
            meleeAttackFrameStarted = null;
        }
    }

    void AdvanceMeleeAttack()
    {
        var frames = meleeAttackFrames[(int)meleeAttackDirection];
        if (frames == null || frames.Length == 0)
        {
            playingMeleeAttack = false;
            meleeAttackFrameStarted = null;
            return;
        }
        if (Time.time < nextMeleeAttackFrameTime) return;
        if (meleeAttackFrame >= frames.Length)
        {
            playingMeleeAttack = false;
            meleeAttackFrameStarted = null;
            animationClock = 0f;
            wasMoving = false;
            return;
        }
        CurrentMeleeAttackFrameIndex = meleeAttackFrame;
        visual.sprite = frames[meleeAttackFrame++];
        nextMeleeAttackFrameTime = Time.time + 1f / meleeAttackFramesPerSecond;
        meleeAttackFrameStarted?.Invoke(CurrentMeleeAttackFrameIndex);
    }

    static FacingDirection DirectionFromVector(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x < 0f ? FacingDirection.Left : FacingDirection.Right;
        return direction.y > 0f ? FacingDirection.Up : FacingDirection.Down;
    }

    static Sprite[][] CreateMeleeAttackFrames(Texture2D sheet, int frameWidth = 16,
        int frameHeight = 32, int requestedFrameCount = 3,
        int[] downOrder = null, int[] rightOrder = null, int[] upOrder = null, int[] leftOrder = null)
    {
        var result = new Sprite[4][];
        for (int direction = 0; direction < result.Length; direction++)
            result[direction] = System.Array.Empty<Sprite>();
        if (!sheet || frameWidth < 1 || frameHeight < 1
            || sheet.width < frameWidth || sheet.height < frameHeight) return result;
        int columns = sheet.width / frameWidth;
        int rows = sheet.height / frameHeight;
        int frameCount = Mathf.Min(columns, Mathf.Max(1, requestedFrameCount));
        for (int rowFromTop = 0; rowFromTop < Mathf.Min(rows, 4); rowFromTop++)
        {
            int y = (rows - rowFromTop - 1) * frameHeight;
            int[] order = rowFromTop switch
            {
                1 => rightOrder,
                2 => upOrder,
                3 => leftOrder,
                _ => downOrder
            };
            var frames = new Sprite[frameCount];
            for (int column = 0; column < frameCount; column++)
            {
                int sourceColumn = order != null && column < order.Length
                    ? Mathf.Clamp(order[column], 0, columns - 1)
                    : column;
                frames[column] = Sprite.Create(sheet,
                    new Rect(sourceColumn * frameWidth, y, frameWidth, frameHeight),
                    new Vector2(.5f, rowFromTop == (int)FacingDirection.Left
                        ? 1f / frameHeight
                        : 0f), PixelArtStandard.PixelsPerUnit);
            }
            result[rowFromTop] = frames;
        }
        return result;
    }

    void OnDestroy()
    {
        DestroyMeleeAttackFrames();
    }

    void DestroyMeleeAttackFrames()
    {
        meleeAttackFrameStarted = null;
        if (meleeAttackFrames == null) return;
        if (ownsMeleeAttackFrames)
            foreach (var direction in meleeAttackFrames)
                if (direction != null)
                    foreach (var frame in direction)
                        if (frame) Destroy(frame);
        meleeAttackFrames = null;
        ownsMeleeAttackFrames = false;
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

    static int IdleFrameIndex(FacingDirection direction) => 0;

    static int FirstWalkingFrameIndex(FacingDirection direction)
    {
        // A tap may last less than one animation interval. Start on a frame that
        // differs from the idle pose so every direction still shows one step.
        return 1;
    }

}
