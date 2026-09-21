using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class ExpeditionFieldOfView : MonoBehaviour
{
    const int MeshSegments = 96;
    const int MeshRings = 3;
    const float LevelTwoClearRadius = 6.5f;
    const float MinimumClearRadius = 4.75f;
    const float EdgeSoftness = 2.25f;
    const float DarknessCoverRadius = 200f;

    Transform player;
    Camera viewCamera;
    GameObject darknessObject;
    MeshRenderer darknessRenderer;
    Mesh darknessMesh;
    Material darknessMaterial;
    float targetClearRadius;
    float revealProgress = 1f;
    float meshAspect;
    Vector3[] darknessVertices;

    public static void Install(Transform player, int completedRuns, bool startUnrestricted = false)
    {
        var camera = Camera.main;
        if (!camera || !player) return;

        var fieldOfView = camera.GetComponent<ExpeditionFieldOfView>();
        if (!fieldOfView) fieldOfView = camera.gameObject.AddComponent<ExpeditionFieldOfView>();
        fieldOfView.Configure(player, completedRuns, startUnrestricted);
    }

    void Configure(Transform target, int completedRuns, bool startUnrestricted)
    {
        player = target;
        viewCamera = GetComponent<Camera>();
        float difficultyDecay = Mathf.Exp(-Mathf.Max(0, completedRuns) * .12f);
        targetClearRadius = MinimumClearRadius
            + (LevelTwoClearRadius - MinimumClearRadius) * difficultyDecay;
        revealProgress = startUnrestricted ? 0f : 1f;
        BuildDarknessMesh();
        enabled = true;
    }

    public void SetRevealProgress(float amount)
    {
        float clamped = Mathf.Clamp01(amount);
        if (Mathf.Approximately(revealProgress, clamped)) return;
        revealProgress = clamped;
        UpdateDarknessGeometry();
    }

    void LateUpdate()
    {
        if (!darknessRenderer || !viewCamera) return;
        darknessRenderer.enabled = player;
        if (player)
        {
            Vector3 localPlayerPosition = transform.InverseTransformPoint(player.position);
            darknessObject.transform.localPosition = new Vector3(
                localPlayerPosition.x, localPlayerPosition.y, 1f);
        }
        if (!Mathf.Approximately(meshAspect, viewCamera.aspect)) BuildDarknessMesh();
    }

    void OnDisable()
    {
        if (darknessRenderer) darknessRenderer.enabled = false;
    }

    void BuildDarknessMesh()
    {
        if (!viewCamera) return;
        if (!darknessObject)
        {
            darknessObject = new GameObject("Expedition FOV Darkness")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            darknessObject.transform.SetParent(transform, false);
            // The mesh sits between the camera and all world sprites. IMGUI HUD
            // elements render afterward and therefore remain fully readable.
            darknessObject.transform.localPosition = new Vector3(0f, 0f, 1f);
            darknessObject.AddComponent<MeshFilter>();
            darknessRenderer = darknessObject.AddComponent<MeshRenderer>();
            darknessRenderer.sortingOrder = short.MaxValue;
            var shader = Shader.Find("Sprites/Default");
            if (!shader)
            {
                Debug.LogError("The built-in sprite shader required for expedition visibility was not found.");
                darknessObject.SetActive(false);
                return;
            }
            darknessMaterial = new Material(shader)
            {
                name = "Expedition FOV Darkness",
                color = Color.white,
                mainTexture = Texture2D.whiteTexture,
                hideFlags = HideFlags.HideAndDontSave
            };
            darknessRenderer.sharedMaterial = darknessMaterial;
        }

        darknessVertices = new Vector3[(MeshSegments + 1) * MeshRings];
        var colors = new Color32[darknessVertices.Length];
        var uv = new Vector2[darknessVertices.Length];
        byte[] alpha = { 0, 255, 255 };
        for (int segment = 0; segment <= MeshSegments; segment++)
        {
            for (int ring = 0; ring < MeshRings; ring++)
            {
                int index = segment * MeshRings + ring;
                colors[index] = new Color32(0, 1, 2, alpha[ring]);
                uv[index] = Vector2.zero;
            }
        }

        var triangles = new int[MeshSegments * (MeshRings - 1) * 6];
        int triangle = 0;
        for (int segment = 0; segment < MeshSegments; segment++)
        for (int ring = 0; ring < MeshRings - 1; ring++)
        {
            int current = segment * MeshRings + ring;
            int next = (segment + 1) * MeshRings + ring;
            triangles[triangle++] = current;
            triangles[triangle++] = next;
            triangles[triangle++] = current + 1;
            triangles[triangle++] = current + 1;
            triangles[triangle++] = next;
            triangles[triangle++] = next + 1;
        }

        if (darknessMesh) Destroy(darknessMesh);
        darknessMesh = new Mesh
        {
            name = "Expedition FOV Darkness",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = darknessVertices,
            colors32 = colors,
            uv = uv,
            triangles = triangles
        };
        darknessMesh.MarkDynamic();
        darknessObject.GetComponent<MeshFilter>().sharedMesh = darknessMesh;
        UpdateDarknessGeometry();
        meshAspect = viewCamera.aspect;
    }

    void UpdateDarknessGeometry()
    {
        if (!darknessMesh || darknessVertices == null) return;

        // At 0% both inner rings sit at the darkness cover radius, leaving the
        // overhead map unrestricted. They ease inward to the gameplay radius.
        float clearRadius = Mathf.Lerp(DarknessCoverRadius, targetClearRadius, revealProgress);
        float outerRadius = Mathf.Lerp(
            DarknessCoverRadius, targetClearRadius + EdgeSoftness, revealProgress);
        for (int segment = 0; segment <= MeshSegments; segment++)
        {
            float angle = segment / (float)MeshSegments * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (int ring = 0; ring < MeshRings; ring++)
            {
                float radius = ring == 0 ? clearRadius
                    : ring == 1 ? outerRadius
                    : DarknessCoverRadius;
                darknessVertices[segment * MeshRings + ring] = direction * radius;
            }
        }

        darknessMesh.vertices = darknessVertices;
        darknessMesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        if (darknessMesh) Destroy(darknessMesh);
        if (darknessMaterial) Destroy(darknessMaterial);
        if (darknessObject) Destroy(darknessObject);
    }
}
