using UnityEngine;

public class FrostRunGame : MonoBehaviour
{
    public static FrostRunGame Instance { get; private set; }
    public float forwardSpeed = 11f;
    public float speedIncreasePerMinute = 0.55f;
    public float laneWidth = 2.6f;
    public int score, coins;
    public float distance;
    public bool IsPlaying { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }

    private FrostRunPlayer player;
    private FrostRunTrack track;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildWorld();
        IsPlaying = false;
    }

    private void Update()
    {
        if (IsGameOver) return;
        if (!IsPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) StartRun();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (IsPaused) return;
        forwardSpeed += speedIncreasePerMinute * Time.deltaTime / 60f;
        distance += forwardSpeed * Time.deltaTime;
        score = Mathf.FloorToInt(distance) + coins * 25;
    }

    private void BuildWorld()
    {
        RenderSettings.ambientLight = new Color(0.78f, 0.86f, 0.96f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.94f);
        RenderSettings.fogDensity = 0.0035f;

        var lightObj = new GameObject("Winter Sun");
        var sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        lightObj.transform.rotation = Quaternion.Euler(35f, -28f, 0f);

        var playerObj = new GameObject("Runner");
        playerObj.transform.position = new Vector3(0f, 1f, 2f);
        var controller = playerObj.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.38f;
        controller.center = Vector3.zero;
        controller.skinWidth = 0.04f;
        controller.stepOffset = 0.25f;
        playerObj.AddComponent<FrostRunVisual>();
        player = playerObj.AddComponent<FrostRunPlayer>();

        var cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        var cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.48f, 0.70f, 0.86f);
        cam.fieldOfView = 68f;
        cameraObj.AddComponent<AudioListener>();
        var follow = cameraObj.AddComponent<FrostRunCamera>();
        follow.target = playerObj.transform;
        cameraObj.transform.position = new Vector3(0f, 5.2f, -7.5f);
        cameraObj.transform.LookAt(playerObj.transform.position + Vector3.forward * 7f);

        CreateSnowfall(cameraObj.transform);
        track = gameObject.AddComponent<FrostRunTrack>();
        gameObject.AddComponent<FrostRunHUD>();
        track.Initialize(this);
    }

    private void CreateSnowfall(Transform cameraTransform)
    {
        var snowObj = new GameObject("Falling Snow");
        snowObj.transform.SetParent(cameraTransform, false);
        snowObj.transform.localPosition = new Vector3(0f, 3.5f, 13f);
        var ps = snowObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 7f;
        main.startSpeed = 0.6f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
        main.startColor = Color.white;
        main.maxParticles = 1400;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission;
        emission.rateOverTime = 170f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(24f, 12f, 32f);
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.8f, -0.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        var renderer = snowObj.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            var particleMat = new Material(particleShader);
            particleMat.color = Color.white;
            renderer.material = particleMat;
        }
        ps.Play();
    }

    public Material MakeMaterial(Color color, float metallic = 0f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
        return mat;
    }

    public void StartRun() { if (!IsGameOver) { IsPlaying = true; IsPaused = false; } }
    public void TogglePause() { if (IsPlaying && !IsGameOver) IsPaused = !IsPaused; }
    public void AddCoin() { coins++; score = Mathf.FloorToInt(distance) + coins * 25; }
    public void EndRun() { if (IsGameOver) return; IsGameOver = true; IsPlaying = false; IsPaused = false; }

    public void Restart()
    {
        score = 0; coins = 0; distance = 0f; forwardSpeed = 11f;
        IsGameOver = false; IsPlaying = false; IsPaused = false;
        if (track != null) track.ResetTrack();
        if (player != null) player.ResetPlayer();
    }
}
