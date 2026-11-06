using System.Collections.Generic;
using UnityEngine;

public class FrostRunTrack : MonoBehaviour
{
    private class Segment { public GameObject root; public float z; }

    private readonly Queue<Segment> segments = new Queue<Segment>();
    private FrostRunGame game;
    private Material snow, ice, pine, rock, gold, hazard, mountain, mountainSnow;
    private const float SegmentLength = 28f;
    private const int SegmentCount = 7;
    private float nextZ;

    public void Initialize(FrostRunGame owner)
    {
        game = owner;
        snow = game.MakeMaterial(new Color(0.90f, 0.96f, 1f));
        ice = game.MakeMaterial(new Color(0.42f, 0.72f, 0.86f), 0.25f);
        pine = game.MakeMaterial(new Color(0.035f, 0.22f, 0.19f));
        rock = game.MakeMaterial(new Color(0.28f, 0.38f, 0.47f));
        gold = game.MakeMaterial(new Color(1f, 0.72f, 0.08f), 0.65f);
        hazard = game.MakeMaterial(new Color(0.78f, 0.12f, 0.12f));
        mountain = game.MakeMaterial(new Color(0.43f, 0.57f, 0.68f));
        mountainSnow = game.MakeMaterial(new Color(0.96f, 0.99f, 1f));
        ResetTrack();
    }

    public void ResetTrack()
    {
        while (segments.Count > 0)
        {
            Segment old = segments.Dequeue();
            if (old.root != null) Destroy(old.root);
        }
        nextZ = 0f;
        for (int i = 0; i < SegmentCount; i++) SpawnSegment();
    }

    private void Update()
    {
        if (game == null || !game.IsPlaying || game.IsPaused || game.IsGameOver) return;
        while (segments.Count > 0 && segments.Peek().z + SegmentLength < game.distance - 12f)
            Destroy(segments.Dequeue().root);
        while (nextZ < game.distance + SegmentLength * 5f) SpawnSegment();
    }

    private void SpawnSegment()
    {
        var root = new GameObject("Snow Track Segment");
        root.transform.position = new Vector3(0f, 0f, nextZ);

        CreateCube(root.transform, "Snow Road (Walkable)", new Vector3(0f, -0.25f, SegmentLength / 2f),
            new Vector3(9.2f, 0.5f, SegmentLength), snow, true);
        CreateCube(root.transform, "Left Ice Rail", new Vector3(-4.75f, 0.05f, SegmentLength / 2f),
            new Vector3(0.25f, 0.7f, SegmentLength), ice, true);
        CreateCube(root.transform, "Right Ice Rail", new Vector3(4.75f, 0.05f, SegmentLength / 2f),
            new Vector3(0.25f, 0.7f, SegmentLength), ice, true);

        CreateMountain(root.transform, -1, 8f, 18f);
        CreateMountain(root.transform, 1, 19f, 23f);

        for (int row = 0; row < 4; row++)
        {
            float z = 4.5f + row * 6f;
            int coinLane = Random.Range(-1, 2);
            for (int c = 0; c < 4; c++)
                MakeCoin(root.transform, new Vector3(coinLane * game.laneWidth, 1.15f, z + c * 0.9f));

            for (int lane = -1; lane <= 1; lane++)
            {
                if (lane == coinLane || Random.value > 0.38f) continue;
                int type = Random.Range(0, 2);
                float x = lane * game.laneWidth;
                if (type == 0)
                    MakeObstacle(root.transform, new Vector3(x, 0.65f, z + 2f), new Vector3(1.05f, 1.3f, 0.8f));
                else
                    MakeObstacle(root.transform, new Vector3(x, 1.7f, z + 2f), new Vector3(1.25f, 0.25f, 0.75f), true);
            }
        }

        for (int i = 0; i < 6; i++)
            MakeTree(root.transform, Random.value < 0.5f ? -1 : 1, Random.Range(2f, SegmentLength - 2f));

        segments.Enqueue(new Segment { root = root, z = nextZ });
        nextZ += SegmentLength;
    }

    private void CreateMountain(Transform parent, int side, float z, float height)
    {
        float x = side * Random.Range(15f, 22f);
        CreatePyramid("Alpine Mountain", parent, new Vector3(x, height * 0.42f, z),
            new Vector3(Random.Range(10f, 15f), height, Random.Range(12f, 18f)), mountain);
        CreatePyramid("Snow Peak", parent, new Vector3(x, height * 0.78f, z),
            new Vector3(7f, height * 0.48f, 8f), mountainSnow);
    }

    private GameObject CreatePyramid(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;
        var filter = obj.AddComponent<MeshFilter>();
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0f, 1f, 0f)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2, 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4 };
        mesh.RecalculateNormals();
        filter.sharedMesh = mesh;
        return obj;
    }

    private void MakeObstacle(Transform parent, Vector3 pos, Vector3 scale, bool overhead = false)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = overhead ? "Low Snow Gate - Slide" : "Snow Rock - Dodge or Jump";
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = overhead ? ice : hazard;
        obj.AddComponent<FrostRunObstacle>();
    }

    private void MakeCoin(Transform parent, Vector3 pos)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = "Golden Snow Crystal";
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(0.34f, 0.48f, 0.16f);
        obj.GetComponent<Renderer>().material = gold;
        obj.GetComponent<SphereCollider>().isTrigger = true;
        obj.AddComponent<FrostRunCoin>();
    }

    private void MakeTree(Transform parent, int side, float z)
    {
        float x = side * Random.Range(6.2f, 9.5f);
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Pine Trunk";
        trunk.transform.SetParent(parent);
        trunk.transform.localPosition = new Vector3(x, 1.1f, z);
        trunk.transform.localScale = new Vector3(0.35f, 1.1f, 0.35f);
        trunk.GetComponent<Renderer>().material = rock;
        Destroy(trunk.GetComponent<Collider>());

        for (int tier = 0; tier < 3; tier++)
        {
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Snow Pine Crown";
            crown.transform.SetParent(parent);
            crown.transform.localPosition = new Vector3(x, 2.1f + tier * 0.65f, z);
            crown.transform.localScale = new Vector3(1.8f - tier * 0.35f, 1.5f, 1.8f - tier * 0.35f);
            crown.GetComponent<Renderer>().material = pine;
            Destroy(crown.GetComponent<Collider>());
        }
    }

    private void CreateCube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = mat;
        if (!keepCollider) Destroy(obj.GetComponent<Collider>());
    }
}
