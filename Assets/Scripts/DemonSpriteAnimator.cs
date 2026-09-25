using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class DemonSpriteAnimator : MonoBehaviour
{
    const int FrameSize = 100;
    static readonly Dictionary<Texture2D, Sprite[]> sharedFrames = new Dictionary<Texture2D, Sprite[]>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSharedFrames() => sharedFrames.Clear();

    public Texture2D idleSheet;
    public Texture2D walkSheet;
    public Texture2D attack01Sheet;
    public Texture2D attack02Sheet;
    public Texture2D hurtSheet;
    public Texture2D deathSheet;
    [Min(.01f)] public float framesPerSecond = 8f;
    [Min(.01f)] public float actionFramesPerSecond = 12f;

    SpriteRenderer visual;
    Rigidbody2D body;
    Sprite[] idleFrames;
    Sprite[] walkFrames;
    Sprite[] attack01Frames;
    Sprite[] attack02Frames;
    Sprite[] hurtFrames;
    Sprite[] deathFrames;
    float nextFrameTime;
    int frameIndex;
    bool showingWalk;
    float damagedUntil;
    Vector2 previousPosition;
    Sprite[] oneShotFrames;
    int oneShotFrameIndex;
    float oneShotNextFrameTime;
    Action oneShotCompleted;

    void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        if (!idleSheet) idleSheet = Resources.Load<Texture2D>("Enemies/DemonA/idle");
        if (!walkSheet) walkSheet = Resources.Load<Texture2D>("Enemies/DemonA/walk");
        if (!attack01Sheet) attack01Sheet = Resources.Load<Texture2D>("Enemies/DemonA/attack01");
        if (!attack02Sheet) attack02Sheet = Resources.Load<Texture2D>("Enemies/DemonA/attack02");
        if (!hurtSheet) hurtSheet = Resources.Load<Texture2D>("Enemies/DemonA/hurt");
        if (!deathSheet) deathSheet = Resources.Load<Texture2D>("Enemies/DemonA/death");
        if (idleSheet) idleSheet.filterMode = FilterMode.Point;
        if (walkSheet) walkSheet.filterMode = FilterMode.Point;
        if (attack01Sheet) attack01Sheet.filterMode = FilterMode.Point;
        if (attack02Sheet) attack02Sheet.filterMode = FilterMode.Point;
        if (hurtSheet) hurtSheet.filterMode = FilterMode.Point;
        if (deathSheet) deathSheet.filterMode = FilterMode.Point;
        idleFrames = GetSharedFrames(idleSheet);
        walkFrames = GetSharedFrames(walkSheet);
        attack01Frames = GetSharedFrames(attack01Sheet);
        attack02Frames = GetSharedFrames(attack02Sheet);
        hurtFrames = GetSharedFrames(hurtSheet);
        deathFrames = GetSharedFrames(deathSheet);
        previousPosition = body.position;
        ShowFrame(idleFrames, 0);
    }

    void Update()
    {
        Vector2 travel = body.position - previousPosition;
        previousPosition = body.position;
        if (body.linearVelocity.sqrMagnitude > .0025f) travel = body.linearVelocity;

        bool shouldWalk = travel.sqrMagnitude > .00001f;
        // The source demon faces right. Mirror it only while travelling left.
        if (travel.x < -.0001f) visual.flipX = true;
        else if (travel.x > .0001f) visual.flipX = false;

        if (oneShotFrames != null)
        {
            if (Time.time >= oneShotNextFrameTime)
            {
                if (oneShotFrameIndex >= oneShotFrames.Length)
                {
                    var completed = oneShotCompleted;
                    oneShotFrames = null;
                    oneShotCompleted = null;
                    completed?.Invoke();
                    if (oneShotFrames == null) return;
                }
                ShowFrame(oneShotFrames, oneShotFrameIndex++);
                oneShotNextFrameTime = Time.time + 1f / actionFramesPerSecond;
            }
            return;
        }

        bool showingDamage = Time.time < damagedUntil;

        if (shouldWalk != showingWalk)
        {
            showingWalk = shouldWalk;
            frameIndex = 0;
            nextFrameTime = 0f;
        }

        if (Time.time < nextFrameTime) return;
        var frames = showingWalk ? walkFrames : idleFrames;
        if (frames.Length == 0) return;
        ShowFrame(frames, frameIndex++ % frames.Length);
        nextFrameTime = Time.time + 1f / (showingDamage ? framesPerSecond * 1.75f : framesPerSecond);
    }

    public void PlayDamage(float duration = .16f)
    {
        damagedUntil = Mathf.Max(damagedUntil, Time.time + duration);
        PlayHurt();
    }

    public void PlayAttack(bool alternate = false)
    {
        var frames = alternate && attack02Frames.Length > 0 ? attack02Frames : attack01Frames;
        if (frames.Length == 0) return;
        PlayOneShot(frames, null);
    }

    public void PlayHurt()
    {
        damagedUntil = Time.time + .18f;
        if (hurtFrames.Length > 0) PlayOneShot(hurtFrames, null);
    }

    public void PlayDeath(Action completed)
    {
        if (deathFrames.Length > 0) PlayOneShot(deathFrames, completed);
        else completed?.Invoke();
    }

    void PlayOneShot(Sprite[] frames, Action completed)
    {
        oneShotFrames = frames;
        oneShotFrameIndex = 0;
        oneShotNextFrameTime = 0f;
        oneShotCompleted = completed;
    }

    public Vector2 FacingDirection => visual && visual.flipX ? Vector2.left : Vector2.right;

    static Sprite[] GetSharedFrames(Texture2D sheet)
    {
        if (!sheet || sheet.height != FrameSize || sheet.width < FrameSize) return System.Array.Empty<Sprite>();
        if (sharedFrames.TryGetValue(sheet, out Sprite[] cached)) return cached;
        int frameCount = sheet.width / FrameSize;
        var frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
            frames[i] = Sprite.Create(sheet, new Rect(i * FrameSize, 0, FrameSize, FrameSize),
                new Vector2(.5f, .5f), PixelArtStandard.PixelsPerUnit);
        sharedFrames.Add(sheet, frames);
        return frames;
    }

    void ShowFrame(Sprite[] frames, int index)
    {
        if (frames.Length > 0) visual.sprite = frames[index];
    }

}
