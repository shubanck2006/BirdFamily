using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BirdFamilyDemo : MonoBehaviour
{
    private enum Phase { Briefing, Flying, Paused, Arriving, Complete }

    [SerializeField] private GameObject motherPrefab;
    [SerializeField] private GameObject chickPrefab;

    private static readonly Vector3 Home = new Vector3(-8f, 0f, -4f);
    private static readonly Vector3[] Perches =
    {
        new Vector3(-4f, 0f, -2f),
        new Vector3(-7f, 0f, 3f),
        new Vector3(-1f, 0f, 5f),
        new Vector3(6f, 0f, 3f),
        new Vector3(7f, 0f, -3f)
    };

    private const float FlightHeight = 0.95f;
    private const float FlightSpeed = 3.35f;
    private const float PickupRadius = 1.12f;
    private const float HomeRadius = 0.95f;
    private const float ParTime = 22f;
    private const float BoardWidth = 11f;
    private const float BoardDepth = 7f;

    private sealed class Bird
    {
        public Transform Pivot;
        public SpriteRenderer Shadow;
        public Vector3 Position;
        public Vector3 Heading;
    }

    private readonly List<Object> generatedAssets = new List<Object>();
    private readonly List<int> flock = new List<int>();
    private readonly Bird[] chicks = new Bird[Perches.Length];
    private readonly bool[] rescued = new bool[Perches.Length];

    private Phase phase;
    private Camera view;
    private DemoHud hud;
    private DemoMeadow meadow;
    private Bird mother;
    private SpriteRenderer destinationMarker;
    private AudioSource audioSource;
    private AudioClip[] pickupSounds;
    private AudioClip arrivalSound;
    private Vector3 destination;
    private Vector3 velocity;
    private bool hasDestination;
    private float runTime;
    private float arrivalTime;
    private float animationTime;

    private void Awake()
    {
        if (motherPrefab == null || chickPrefab == null)
        {
            Debug.LogError("Assign the mother and chick prefabs on the Bird Family demo.", this);
            enabled = false;
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif
        CreateCamera();
        meadow = new GameObject("The meadow").AddComponent<DemoMeadow>();
        meadow.transform.SetParent(transform, false);
        meadow.Build(Home, Perches);

        Sprite shadow = MakeGroundSprite(false);
        mother = CreateBird(motherPrefab, "Mother bird", 1.75f, shadow);
        for (int i = 0; i < chicks.Length; i++)
            chicks[i] = CreateBird(chickPrefab, "Chick " + (i + 1), 1.04f, shadow);

        destinationMarker = CreateGroundSprite("Flight destination", MakeGroundSprite(true), 0.7f);
        CreateSounds();
        hud = gameObject.AddComponent<DemoHud>();
        hud.Build(BeginRun, PauseRun, ResumeRun, BeginRun, ParTime);
        ResetCourse();
        phase = Phase.Briefing;
        hud.ShowBriefing();
    }

    private void Update()
    {
        FrameCourse();
        if (phase == Phase.Briefing)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                BeginRun();
            else
                AnimateBirds(Time.deltaTime);
            return;
        }

        if (phase == Phase.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ResumeRun();
            return;
        }

        if (phase == Phase.Complete)
        {
            AnimateBirds(Time.deltaTime);
            return;
        }

        if (phase == Phase.Arriving)
        {
            SettleFlock(Time.deltaTime);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseRun();
            return;
        }

        Fly(Time.deltaTime);
    }

    private void ResetCourse()
    {
        flock.Clear();
        runTime = 0f;
        arrivalTime = 0f;
        animationTime = 0f;
        hasDestination = false;
        velocity = Vector3.zero;
        destinationMarker.enabled = false;
        mother.Position = Home;
        mother.Heading = Vector3.back;
        meadow.SetHomeReady(false);

        for (int i = 0; i < chicks.Length; i++)
        {
            rescued[i] = false;
            chicks[i].Position = Perches[i];
            chicks[i].Heading = Vector3.back;
            meadow.SetChickWaiting(i, true);
        }

        AnimateBirds(0f, true);
        hud.UpdateRun(0, chicks.Length, 0f, false);
    }

    private void BeginRun()
    {
        ResetCourse();
        phase = Phase.Flying;
        hud.ClearNotification();
        hud.ShowRun();
    }

    private void PauseRun()
    {
        if (phase != Phase.Flying)
            return;

        phase = Phase.Paused;
        hud.ShowPause();
    }

    private void ResumeRun()
    {
        if (phase != Phase.Paused)
            return;

        phase = Phase.Flying;
        hud.ShowRun();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            PauseRun();
    }

    private void Fly(float deltaTime)
    {
        runTime += deltaTime;
        Vector3 steer = ReadKeyboard();
        if (steer.sqrMagnitude > 0f)
        {
            hasDestination = false;
        }
        else
        {
            ReadPointer();
            if (hasDestination)
            {
                Vector3 offset = destination - mother.Position;
                if (offset.sqrMagnitude <= 0.08f * 0.08f)
                    hasDestination = false;
                else
                    steer = offset.normalized * Mathf.Min(1f, offset.magnitude * 2.5f);
            }
        }

        velocity = Vector3.MoveTowards(velocity, steer * FlightSpeed, deltaTime * 20f);
        Vector3 next = mother.Position + velocity * deltaTime;
        if (hasDestination && Vector3.Dot(destination - mother.Position, destination - next) <= 0f)
        {
            next = destination;
            velocity = Vector3.zero;
            hasDestination = false;
        }

        mother.Position = ConstrainToCourse(next);
        if (velocity.sqrMagnitude > 0.15f)
            mother.Heading = velocity.normalized;

        destinationMarker.enabled = hasDestination;
        if (hasDestination)
        {
            float pulse = 0.72f + 0.15f * Mathf.Sin(animationTime * 7f);
            destinationMarker.transform.localScale = Vector3.one * pulse;
            destinationMarker.transform.position = destination + Vector3.up * 0.07f;
        }

        RecruitNearbyChicks();
        MoveFlock(deltaTime);
        AnimateBirds(deltaTime);
        bool returning = flock.Count == chicks.Length;
        hud.UpdateRun(flock.Count, chicks.Length, runTime, returning);
        if (returning && (mother.Position - Home).sqrMagnitude <= HomeRadius * HomeRadius)
        {
            phase = Phase.Arriving;
            destinationMarker.enabled = false;
            audioSource.PlayOneShot(arrivalSound);
            hud.Notify("Everyone made it home!");
        }
    }

    private static Vector3 ReadKeyboard()
    {
        float x = 0f;
        float z = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;
        return Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
    }

    private void ReadPointer()
    {
        Vector2 pointer;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
                return;
            pointer = touch.position;
        }
        else
        {
            if (!Input.GetMouseButtonDown(0))
                return;
            pointer = Input.mousePosition;
        }

        SetPointerDestination(pointer);
    }

    private void SetPointerDestination(Vector2 pointer)
    {
        if (phase != Phase.Flying || hud.IsPointerOverControl(pointer))
            return;

        Ray ray = view.ScreenPointToRay(pointer);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (!ground.Raycast(ray, out float distance))
            return;

        destination = ConstrainToCourse(ray.GetPoint(distance));
        float snapRadius = 0.66f * view.pixelHeight / (view.orthographicSize * 2f);
        for (int i = 0; i < chicks.Length; i++)
        {
            if (!rescued[i] && Vector2.Distance(pointer, view.WorldToScreenPoint(chicks[i].Pivot.position)) <= snapRadius)
            {
                destination = Perches[i];
                break;
            }
        }

        if (flock.Count == chicks.Length &&
            Vector2.Distance(pointer, view.WorldToScreenPoint(Home + Vector3.up * 0.4f)) <= snapRadius * 1.5f)
            destination = Home;

        hasDestination = true;
    }

    private static Vector3 ConstrainToCourse(Vector3 position)
    {
        return new Vector3(Mathf.Clamp(position.x, -BoardWidth, BoardWidth), 0f,
            Mathf.Clamp(position.z, -BoardDepth, BoardDepth));
    }

    private void RecruitNearbyChicks()
    {
        for (int i = 0; i < chicks.Length; i++)
        {
            if (rescued[i] || (mother.Position - Perches[i]).sqrMagnitude > PickupRadius * PickupRadius)
                continue;

            rescued[i] = true;
            flock.Add(i);
            meadow.SetChickWaiting(i, false);
            audioSource.PlayOneShot(pickupSounds[flock.Count - 1]);

            if (flock.Count == chicks.Length)
            {
                meadow.SetHomeReady(true);
                hud.Notify("The flock is together. Head back to the glowing nest.");
            }
            else
            {
                hud.Notify(flock.Count == 1 ? "First chick found!" : "Another chick joined the flock!");
            }
        }
    }

    private void MoveFlock(float deltaTime)
    {
        Vector3 right = Vector3.Cross(Vector3.up, mother.Heading);
        for (int i = 0; i < flock.Count; i++)
        {
            Bird chick = chicks[flock[i]];
            int row = i / 2;
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 slot = mother.Position - mother.Heading * (0.7f + row * 0.55f) +
                right * side * (0.53f + row * 0.2f);
            MoveChick(chick, ConstrainToCourse(slot), deltaTime * (FlightSpeed + 1.5f));
        }
    }

    private static void MoveChick(Bird chick, Vector3 target, float distance)
    {
        Vector3 offset = target - chick.Position;
        if (offset.sqrMagnitude > 0.012f)
            chick.Heading = offset.normalized;
        chick.Position = Vector3.MoveTowards(chick.Position, target, distance);
    }

    private void SettleFlock(float deltaTime)
    {
        arrivalTime += deltaTime;
        mother.Position = Vector3.MoveTowards(mother.Position, Home, deltaTime * FlightSpeed);
        mother.Heading = Vector3.back;
        for (int i = 0; i < flock.Count; i++)
        {
            Bird chick = chicks[flock[i]];
            float angle = (i - (flock.Count - 1) * 0.5f) * 0.72f;
            Vector3 slot = Home + new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle)) * 1.0f;
            MoveChick(chick, slot, deltaTime * (FlightSpeed + 2f));
            if ((chick.Position - slot).sqrMagnitude < 0.02f)
                chick.Heading = Vector3.back;
        }

        AnimateBirds(deltaTime);
        if (arrivalTime >= 1.4f)
        {
            phase = Phase.Complete;
            hud.ShowResults(runTime, ParTime);
        }
    }

    private void AnimateBirds(float deltaTime, bool resetRotation = false)
    {
        animationTime += deltaTime;
        AnimateBird(mother, 0f, deltaTime, resetRotation);
        for (int i = 0; i < chicks.Length; i++)
            AnimateBird(chicks[i], (i + 1) * 1.15f, deltaTime, resetRotation);
    }

    private void AnimateBird(Bird bird, float offset, float deltaTime, bool resetRotation)
    {
        float bob = Mathf.Sin(animationTime * 4.5f + offset);
        float bank = Mathf.Sin(animationTime * 3.1f + offset);
        bird.Pivot.position = bird.Position + Vector3.up * (FlightHeight + bob * 0.10f);
        Quaternion turn = Quaternion.LookRotation(bird.Heading) * Quaternion.Euler(0f, 0f, bank * 6f);
        bird.Pivot.rotation = resetRotation ? turn :
            Quaternion.Slerp(bird.Pivot.rotation, turn, 1f - Mathf.Exp(-9f * deltaTime));
        bird.Shadow.transform.position = bird.Position + Vector3.up * 0.023f;
        bird.Shadow.color = new Color(0.05f, 0.13f, 0.12f, 0.42f - bob * 0.05f);
    }

    private Bird CreateBird(GameObject prefab, string label, float size, Sprite shadow)
    {
        Transform pivot = new GameObject(label).transform;
        pivot.SetParent(transform, false);
        GameObject model = Instantiate(prefab, pivot);
        model.name = "Bird model";

        // The same prefab still uses its AR behaviour in the original scene.
        foreach (MonoBehaviour script in model.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = false;
        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>())
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }
        foreach (ParticleSystem particles in model.GetComponentsInChildren<ParticleSystem>())
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Renderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer mesh in renderers)
                bounds.Encapsulate(mesh.bounds);

            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest > 0.0001f)
            {
                float scale = size / largest;
                model.transform.localScale *= scale;
                Vector3 visualCenter = pivot.InverseTransformPoint(bounds.center);
                model.transform.localPosition = (model.transform.localPosition - visualCenter) * scale;
            }
        }

        SpriteRenderer groundShadow = CreateGroundSprite(label + " shadow", shadow, size * 1.05f);
        return new Bird { Pivot = pivot, Shadow = groundShadow, Heading = Vector3.back };
    }

    private SpriteRenderer CreateGroundSprite(string label, Sprite sprite, float size)
    {
        GameObject marker = new GameObject(label);
        marker.transform.SetParent(transform, false);
        marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        marker.transform.localScale = Vector3.one * size;
        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        return renderer;
    }

    private Sprite MakeGroundSprite(bool ring)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = ring ? "Destination ring texture" : "Bird shadow texture";
        texture.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 centered = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
            float radius = centered.magnitude * 2f;
            float opacity = ring ? Mathf.Clamp01(1f - Mathf.Abs(radius - 0.76f) / 0.10f) :
                Mathf.Pow(1f - Mathf.Clamp01(radius), 1.8f);
            Color tint = ring ? new Color(1f, 0.94f, 0.70f, opacity) : new Color(1f, 1f, 1f, opacity);
            pixels[y * size + x] = tint;
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        generatedAssets.Add(sprite);
        generatedAssets.Add(texture);
        return sprite;
    }

    private void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Meadow Camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 17f, -20f);
        cameraObject.transform.LookAt(Vector3.zero);
        view = cameraObject.AddComponent<Camera>();
        view.orthographic = true;
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = new Color(0.34f, 0.56f, 0.44f);
        view.nearClipPlane = 0.3f;
        view.farClipPlane = 90f;
        cameraObject.AddComponent<AudioListener>();
        FrameCourse();
    }

    private void FrameCourse()
    {
        view.orthographicSize = Mathf.Max(8.85f, 13.6f / Mathf.Max(0.1f, view.aspect));
    }

    private void CreateSounds()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
        pickupSounds = new AudioClip[chicks.Length];
        for (int i = 0; i < pickupSounds.Length; i++)
        {
            float root = 523.25f * Mathf.Pow(2f, i * 2f / 12f);
            pickupSounds[i] = MakeChime("Chick call " + (i + 1), 0.09f, root, root * 1.5f);
        }
        arrivalSound = MakeChime("Homecoming", 0.13f, 523.25f, 659.25f, 783.99f, 1046.50f);
    }

    private AudioClip MakeChime(string label, float spacing, params float[] notes)
    {
        const int SampleRate = 22050;
        const float NoteLength = 0.33f;
        float[] samples = new float[Mathf.CeilToInt(((notes.Length - 1) * spacing + NoteLength) * SampleRate)];
        for (int note = 0; note < notes.Length; note++)
        {
            int start = Mathf.RoundToInt(note * spacing * SampleRate);
            int length = Mathf.Min(samples.Length - start, Mathf.RoundToInt(NoteLength * SampleRate));
            for (int i = 0; i < length; i++)
            {
                float seconds = (float)i / SampleRate;
                float envelope = Mathf.Min(1f, seconds / 0.012f) * Mathf.Pow(1f - seconds / NoteLength, 2f);
                float pitch = notes[note] * seconds * Mathf.PI * 2f;
                samples[start + i] += (Mathf.Sin(pitch) + 0.18f * Mathf.Sin(pitch * 2f)) * envelope * 0.32f;
            }
        }

        AudioClip clip = AudioClip.Create(label, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        generatedAssets.Add(clip);
        return clip;
    }

    private void OnDestroy()
    {
        foreach (Object asset in generatedAssets)
            Destroy(asset);
    }
}
