using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Spawning")]
    public float spawnRadius = 30f;
    public float minSpawnDistance = 15f;
    public int maxEnemies = 12;
    public float spawnInterval = 3f;
    public float despawnDistance = 60f;

    [Header("Waves")]
    public int enemiesPerWave = 5;
    public float waveCooldown = 4f;

    [Header("Prefabs (Auto-assigned from Resources)")]
    public GameObject rusherPrefab;
    public GameObject shooterPrefab;

    private List<GameObject> aliveEnemies = new List<GameObject>();
    private Transform player;
    private float lastSpawnTime;
    private int spawnedThisWave;
    private bool waveActive;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += StopSpawning;
        }

        LoadEnemyPrefabs();
        StartCoroutine(StartFirstWave());
    }

    void LoadEnemyPrefabs()
    {
        if (rusherPrefab == null)
            rusherPrefab = Resources.Load<GameObject>("Models/Rusher");
        if (shooterPrefab == null)
            shooterPrefab = Resources.Load<GameObject>("Models/Shooter");

        if (rusherPrefab == null)
            Debug.Log("[FPS] No rusher prefab in Resources/Models/, using primitive fallback.");
        if (shooterPrefab == null)
            Debug.Log("[FPS] No shooter prefab in Resources/Models/, using primitive fallback.");
    }

    IEnumerator StartFirstWave()
    {
        yield return new WaitForSeconds(1f);
        StartWave();
    }

    void StartWave()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NextWave();
        }

        waveActive = true;
        spawnedThisWave = 0;
        lastSpawnTime = Time.time;
    }

    void Update()
    {
        if (!waveActive || player == null) return;

        aliveEnemies.RemoveAll(e => e == null);

        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(aliveEnemies[i].transform.position, player.position) > despawnDistance)
            {
                Destroy(aliveEnemies[i]);
                aliveEnemies.RemoveAt(i);
            }
        }

        if (spawnedThisWave < enemiesPerWave && aliveEnemies.Count < maxEnemies && Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnEnemy();
            lastSpawnTime = Time.time;
        }

        if (spawnedThisWave >= enemiesPerWave && aliveEnemies.Count == 0)
        {
            waveActive = false;
            StartCoroutine(WaveCooldown());
        }
    }

    IEnumerator WaveCooldown()
    {
        yield return new WaitForSeconds(waveCooldown);
        enemiesPerWave += 3;
        spawnInterval = Mathf.Max(1f, spawnInterval - 0.1f);
        StartWave();
    }

    void SpawnEnemy()
    {
        Vector3 basePos = player.position + Random.insideUnitSphere * spawnRadius;
        basePos.y = player.position.y;

        if (Vector3.Distance(basePos, player.position) < minSpawnDistance)
        {
            basePos = (basePos - player.position).normalized * minSpawnDistance + player.position;
        }

        basePos.y = 0f;

        EnemyController.EnemyType type = Random.value > 0.6f
            ? EnemyController.EnemyType.Shooter
            : EnemyController.EnemyType.Rusher;

        var enemy = CreateEnemy(type);
        enemy.transform.position = basePos;
        aliveEnemies.Add(enemy);
        spawnedThisWave++;
    }

    GameObject CreateEnemy(EnemyController.EnemyType type)
    {
        GameObject prefab = type == EnemyController.EnemyType.Rusher ? rusherPrefab : shooterPrefab;
        GameObject enemy;

        if (prefab != null)
        {
            enemy = Instantiate(prefab);
            enemy.name = type == EnemyController.EnemyType.Rusher ? "Rusher" : "Shooter";
            SetupFBXEnemy(enemy, type);
        }
        else
        {
            enemy = CreatePrimitiveEnemy(type);
        }

        enemy.tag = "Enemy";

        var cc = enemy.GetComponent<CharacterController>();
        if (cc == null) cc = enemy.AddComponent<CharacterController>();
        cc.height = 2.2f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0, 1.1f, 0);

        var ec = enemy.GetComponent<EnemyController>();
        if (ec == null) ec = enemy.AddComponent<EnemyController>();
        ec.enemyType = type;
        ec.maxHP = type == EnemyController.EnemyType.Rusher ? 60 : 40;
        ec.attackDamage = type == EnemyController.EnemyType.Rusher ? 15 : 10;
        ec.moveSpeed = type == EnemyController.EnemyType.Rusher ? 7f : 4f;
        ec.attackCooldown = type == EnemyController.EnemyType.Rusher ? 1f : 2f;

        var deathEffect = enemy.GetComponent<EnemyDeathEffect>();
        if (deathEffect == null) deathEffect = enemy.AddComponent<EnemyDeathEffect>();
        
        deathEffect.deathParticles = Resources.Load<GameObject>("Effects/DeathParticles");
        deathEffect.bloodMist = Resources.Load<GameObject>("Effects/BloodMist");
        deathEffect.gibs = Resources.Load<GameObject>("Effects/Gib");
        deathEffect.dissolveMaterial = new Material(Shader.Find("Custom/Dissolve"));

        return enemy;
    }

    void SetupFBXEnemy(GameObject enemy, EnemyController.EnemyType type)
    {
        foreach (var r in enemy.GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            Color baseColor = type == EnemyController.EnemyType.Rusher
                ? new Color(0.7f, 0.12f, 0.12f)
                : new Color(0.12f, 0.35f, 0.8f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0.3f);
            mat.SetFloat("_Smoothness", 0.4f);
            r.material = mat;
        }

        var animator = enemy.GetComponent<Animator>();
        if (animator != null) Destroy(animator);

        enemy.transform.localScale = Vector3.one * 1f;
    }

    GameObject CreatePrimitiveEnemy(EnemyController.EnemyType type)
    {
        var enemy = new GameObject(type == EnemyController.EnemyType.Rusher ? "Rusher" : "Shooter");

        Color bodyColor = type == EnemyController.EnemyType.Rusher
            ? new Color(0.8f, 0.15f, 0.15f)
            : new Color(0.15f, 0.4f, 0.9f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.parent = enemy.transform;
        body.transform.localPosition = new Vector3(0, 1f, 0);
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        var bodyRend = body.GetComponent<Renderer>();
        bodyRend.material = LitMaterial(bodyColor, 0.3f, 0.4f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.parent = enemy.transform;
        head.transform.localPosition = new Vector3(0, 2f, 0);
        head.transform.localScale = Vector3.one * 0.45f;
        Destroy(head.GetComponent<Collider>());
        var headRend = head.GetComponent<Renderer>();
        headRend.material = LitMaterial(bodyColor * 1.2f, 0.4f, 0.5f);

        var eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeL.name = "Eye_L";
        eyeL.transform.parent = head.transform;
        eyeL.transform.localPosition = new Vector3(-0.12f, 0.08f, 0.4f);
        eyeL.transform.localScale = Vector3.one * 0.12f;
        Destroy(eyeL.GetComponent<Collider>());
        eyeL.GetComponent<Renderer>().material = LitMaterial(Color.yellow, 0.9f, 1f);

        var eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeR.name = "Eye_R";
        eyeR.transform.parent = head.transform;
        eyeR.transform.localPosition = new Vector3(0.12f, 0.08f, 0.4f);
        eyeR.transform.localScale = Vector3.one * 0.12f;
        Destroy(eyeR.GetComponent<Collider>());
        eyeR.GetComponent<Renderer>().material = LitMaterial(Color.yellow, 0.9f, 1f);

        if (type == EnemyController.EnemyType.Shooter)
        {
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gun.name = "Gun";
            gun.transform.parent = enemy.transform;
            gun.transform.localPosition = new Vector3(0.4f, 1.2f, 0.3f);
            gun.transform.localScale = new Vector3(0.06f, 0.25f, 0.06f);
            gun.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Destroy(gun.GetComponent<Collider>());
            gun.GetComponent<Renderer>().material = LitMaterial(new Color(0.2f, 0.2f, 0.3f), 0.8f, 0.7f);
        }

        return enemy;
    }

    void StopSpawning()
    {
        waveActive = false;
        StopAllCoroutines();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= StopSpawning;
        }
    }

    static Material LitMaterial(Color color, float metallic, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }
}
