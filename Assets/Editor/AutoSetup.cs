using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using System.Collections.Generic;

[InitializeOnLoad]
public class AutoSetup
{
    private const string SETUP_DONE_KEY = "PhantomEdge_Setup_Done_v1";
    private const string SETUP_SCENE_PATH = "Assets/Scenes/PHANTOM EDGE_Arena.unity";

    static AutoSetup()
    {
        if (SessionState.GetBool(SETUP_DONE_KEY, false)) return;
        if (EditorPrefs.GetBool(SETUP_DONE_KEY, false)) return;

        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(SETUP_DONE_KEY, false)) return;
            if (!EditorUtility.DisplayDialog("PHANTOM EDGE",
                "PHANTOM EDGE arena setup.\n\nCreate arena + FPS player?", "OK", "Cancel"))
                return;
            RunFullSetup();
        };
    }

    [MenuItem("PHANTOM EDGE/Re-Setup Arena")]
    public static void MenuSetup()
    {
        if (EditorUtility.DisplayDialog("Re-Setup", "Rebuild PHANTOM EDGE arena?", "OK", "Cancel"))
        {
            EditorPrefs.DeleteKey(SETUP_DONE_KEY);
            SessionState.SetBool(SETUP_DONE_KEY, false);
            RunFullSetup();
        }
    }

    [MenuItem("PHANTOM EDGE/Reset Setup Flag")]
    public static void ResetFlag()
    {
        EditorPrefs.DeleteKey(SETUP_DONE_KEY);
        SessionState.SetBool(SETUP_DONE_KEY, false);
        Debug.Log("[PHANTOM EDGE] Flag reset. Reload Domain to trigger.");
    }

    [MenuItem("PHANTOM EDGE/Import Enemy Models")]
    public static void ImportEnemyModels()
    {
        string modelDir = "Assets/Models";
        if (!Directory.Exists(modelDir))
        {
            Directory.CreateDirectory(modelDir);
            AssetDatabase.Refresh();
        }

        string[] fbxFiles = Directory.GetFiles(modelDir, "*.fbx", SearchOption.AllDirectories);
        foreach (var fbx in fbxFiles)
        {
            string assetPath = fbx.Replace("\\", "/");
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) continue;

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            importer.SaveAndReimport();
            Debug.Log("[PHANTOM EDGE] Fixed FBX import: " + assetPath);
        }

        CopyModelsToResources();

        Debug.Log("[PHANTOM EDGE] Enemy models import complete. Found " + fbxFiles.Length + " FBX files.");
    }

    static void CopyModelsToResources()
    {
        string srcDir = "Assets/Models";
        string dstDir = "Assets/Resources/Models";
        
        if (!Directory.Exists(srcDir)) return;
        
        string[] fbxFiles = Directory.GetFiles(srcDir, "*.fbx", SearchOption.AllDirectories);
        foreach (var fbx in fbxFiles)
        {
            string fileName = Path.GetFileName(fbx);
            string dstPath = Path.Combine(dstDir, fileName).Replace("\\", "/");
            
            if (AssetDatabase.CopyAsset(fbx.Replace("\\", "/"), dstPath))
            {
                Debug.Log("[PHANTOM EDGE] Copied to Resources: " + dstPath);
            }
        }
        
        AssetDatabase.Refresh();
    }

    [MenuItem("PHANTOM EDGE/Copy Models to Resources")]
    public static void MenuCopyModelsToResources()
    {
        CopyModelsToResources();
        Debug.Log("[PHANTOM EDGE] Models copied to Resources/Models for runtime loading.");
    }

    public static void RunFullSetup()
    {
        Debug.Log("[PHANTOM EDGE] === Arena Setup Start ===");

        EnsureFolders();
        EnsureURPPipeline();
        FixFBXImport();
        FixEnemyModelImports();
        AssetDatabase.Refresh();

        var scene = CreateScene();
        BuildArena(scene);

        EditorPrefs.SetBool(SETUP_DONE_KEY, true);
        SessionState.SetBool(SETUP_DONE_KEY, true);

        Debug.Log("[PHANTOM EDGE] === Arena Setup Complete ===");
    }

    static void EnsureFolders()
    {
        string[] folders = { "Assets/Scenes", "Assets/Models", "Assets/Models/Enemies", "Assets/Materials", "Assets/Prefabs", "Assets/Resources", "Assets/Resources/Models" };
        foreach (var f in folders)
        {
            if (!Directory.Exists(f))
            {
                Directory.CreateDirectory(f);
                AssetDatabase.Refresh();
            }
        }
    }

    static void EnsureURPPipeline()
    {
        var guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
            {
                GraphicsSettings.defaultRenderPipeline = (RenderPipelineAsset)asset;
                QualitySettings.renderPipeline = (RenderPipelineAsset)asset;
                Debug.Log("[PHANTOM EDGE] URP assigned: " + path);
                return;
            }
        }
    }

    static void FixFBXImport()
    {
        var importer = AssetImporter.GetAtPath("Assets/katana.FBX") as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("[PHANTOM EDGE] katana.FBX not found");
            return;
        }

        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = false;

        importer.SaveAndReimport();
        Debug.Log("[PHANTOM EDGE] katana.FBX import fixed.");
    }

    static void FixEnemyModelImports()
    {
        string modelDir = "Assets/Models";
        if (!Directory.Exists(modelDir)) return;

        string[] fbxFiles = Directory.GetFiles(modelDir, "*.fbx", SearchOption.AllDirectories);
        foreach (var fbx in fbxFiles)
        {
            string assetPath = fbx.Replace("\\", "/");
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) continue;

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importCameras = false;
            importer.importLights = false;

            importer.SaveAndReimport();
            Debug.Log("[PHANTOM EDGE] Fixed enemy FBX import: " + assetPath);
        }
    }

    static UnityEngine.SceneManagement.Scene CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, SETUP_SCENE_PATH);
        return scene;
    }

    static void BuildArena(UnityEngine.SceneManagement.Scene scene)
    {
        var root = new GameObject("Arena");

        CreateSkyboxAndFog();
        CreateLighting(root);
        CreateFloor(root);
        CreateArenaStructure(root);
        CreatePlayer(root);
        CreateGameManager(root);
        CreateEnemySpawner(root);
        CreateGrapplePoints(root);
        CreateUI(root);
        CreateEventSystem(root);
        CreatePostProcessing(root);
        CreateEffectPrefabs();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PHANTOM EDGE] Arena saved.");
    }

    static void CreateSkyboxAndFog()
    {
        var skyMat = new Material(Shader.Find("Skybox/Procedural"));
        if (skyMat != null)
        {
            skyMat.SetFloat("_SunSize", 0.04f);
            skyMat.SetFloat("_AtmosphereThickness", 1.2f);
            skyMat.SetColor("_SkyTint", new Color(0.2f, 0.25f, 0.35f));
            skyMat.SetColor("_GroundColor", new Color(0.1f, 0.12f, 0.15f));
            skyMat.SetFloat("_Exposure", 1.8f);
            RenderSettings.skybox = skyMat;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.3f, 0.35f, 0.45f);
        RenderSettings.ambientEquatorColor = new Color(0.2f, 0.18f, 0.18f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.07f, 0.06f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.15f, 0.18f, 0.22f);
        RenderSettings.fogDensity = 0.004f;
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.reflectionIntensity = 1.0f;
    }

    static void CreateLighting(GameObject root)
    {
        var sunObj = new GameObject("Sun");
        sunObj.transform.parent = root.transform;
        sunObj.transform.rotation = Quaternion.Euler(45f, -40f, 0f);
        var sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.92f, 0.8f);
        sun.intensity = 2.5f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 1f;
        sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
        sun.shadowNearPlane = 0.1f;
        sun.shadowNormalBias = 0.5f;
        sun.shadowBias = 0.02f;

        var fillObj = new GameObject("FillLight");
        fillObj.transform.parent = root.transform;
        fillObj.transform.rotation = Quaternion.Euler(15f, 140f, 0f);
        var fill = fillObj.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.2f, 0.3f, 0.5f);
        fill.intensity = 0.6f;
        fill.shadows = LightShadows.None;

        var rimObj = new GameObject("RimLight");
        rimObj.transform.parent = root.transform;
        rimObj.transform.rotation = Quaternion.Euler(-30f, -70f, 0f);
        var rim = rimObj.AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.color = new Color(1f, 0.7f, 0.4f);
        rim.intensity = 0.8f;
        rim.shadows = LightShadows.None;

        var bounceObj = new GameObject("BounceLight");
        bounceObj.transform.parent = root.transform;
        bounceObj.transform.rotation = Quaternion.Euler(80f, 180f, 0f);
        var bounce = bounceObj.AddComponent<Light>();
        bounce.type = LightType.Directional;
        bounce.color = new Color(0.15f, 0.18f, 0.25f);
        bounce.intensity = 0.4f;
        bounce.shadows = LightShadows.None;

        for (int i = 0; i < 8; i++)
        {
            var pointObj = new GameObject("PointLight_" + i);
            pointObj.transform.parent = root.transform;
            float angle = i * 45f;
            float rad = angle * Mathf.Deg2Rad;
            pointObj.transform.position = new Vector3(Mathf.Cos(rad) * 28f, 10f, Mathf.Sin(rad) * 28f);
            var pl = pointObj.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.75f, 0.5f);
            pl.intensity = 5f;
            pl.range = 50f;
            pl.shadows = LightShadows.Soft;
            pl.shadowStrength = 0.8f;
        }

        for (int i = 0; i < 4; i++)
        {
            var accentObj = new GameObject("AccentLight_" + i);
            accentObj.transform.parent = root.transform;
            float angle = i * 90f + 45f;
            float rad = angle * Mathf.Deg2Rad;
            accentObj.transform.position = new Vector3(Mathf.Cos(rad) * 18f, 6f, Mathf.Sin(rad) * 18f);
            var al = accentObj.AddComponent<Light>();
            al.type = LightType.Spot;
            al.color = new Color(0.2f, 0.5f, 1f);
            al.intensity = 8f;
            al.range = 30f;
            al.spotAngle = 60f;
            al.shadows = LightShadows.Soft;
            al.innerSpotAngle = 30f;
        }
    }

    static void CreateFloor(GameObject root)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.parent = root.transform;
        floor.transform.position = new Vector3(0, -0.5f, 0);
        floor.transform.localScale = new Vector3(80, 1, 80);
        floor.GetComponent<Renderer>().material = LitMat(new Color(0.28f, 0.28f, 0.3f), 0.15f, 0.65f);

        var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.parent = root.transform;
        ceiling.transform.position = new Vector3(0, 12.5f, 0);
        ceiling.transform.localScale = new Vector3(80, 1, 80);
        Object.DestroyImmediate(ceiling.GetComponent<Collider>());
        ceiling.GetComponent<Renderer>().material = LitMat(new Color(0.22f, 0.22f, 0.25f), 0.1f, 0.4f);
    }

    static void CreateArenaStructure(GameObject root)
    {
        var s = new GameObject("Structures");
        s.transform.parent = root.transform;

        CreateWall(s, new Vector3(0, 3, -40), new Vector3(80, 6, 0.5f), new Color(0.4f, 0.38f, 0.35f));
        CreateWall(s, new Vector3(0, 3, 40), new Vector3(80, 6, 0.5f), new Color(0.4f, 0.38f, 0.35f));
        CreateWall(s, new Vector3(-40, 3, 0), new Vector3(0.5f, 6, 80), new Color(0.38f, 0.36f, 0.33f));
        CreateWall(s, new Vector3(40, 3, 0), new Vector3(0.5f, 6, 80), new Color(0.38f, 0.36f, 0.33f));

        CreatePlatform(s, new Vector3(-12, 1.5f, -12), new Vector3(6, 0.3f, 6));
        CreatePlatform(s, new Vector3(12, 1.5f, -12), new Vector3(6, 0.3f, 6));
        CreatePlatform(s, new Vector3(-12, 1.5f, 12), new Vector3(6, 0.3f, 6));
        CreatePlatform(s, new Vector3(12, 1.5f, 12), new Vector3(6, 0.3f, 6));
        CreatePlatform(s, new Vector3(0, 3f, 0), new Vector3(10, 0.3f, 10));
        CreatePlatform(s, new Vector3(-25, 2f, 0), new Vector3(5, 0.3f, 8));
        CreatePlatform(s, new Vector3(25, 2f, 0), new Vector3(5, 0.3f, 8));

        CreateRamp(s, new Vector3(-8, 0.75f, -12), new Vector3(4, 0.2f, 3), -20f);
        CreateRamp(s, new Vector3(8, 0.75f, -12), new Vector3(4, 0.2f, 3), 20f);
        CreateRamp(s, new Vector3(-8, 0.75f, 12), new Vector3(4, 0.2f, 3), -20f);
        CreateRamp(s, new Vector3(8, 0.75f, 12), new Vector3(4, 0.2f, 3), 20f);

        CreatePillar(s, new Vector3(-18, 0, -18));
        CreatePillar(s, new Vector3(18, 0, -18));
        CreatePillar(s, new Vector3(-18, 0, 18));
        CreatePillar(s, new Vector3(18, 0, 18));
        CreatePillar(s, new Vector3(0, 0, -25));
        CreatePillar(s, new Vector3(0, 0, 25));
        CreatePillar(s, new Vector3(-25, 0, 0));
        CreatePillar(s, new Vector3(25, 0, 0));

        CreateCoverBox(s, new Vector3(-6, 0.5f, -6));
        CreateCoverBox(s, new Vector3(6, 0.5f, 6));
        CreateCoverBox(s, new Vector3(-6, 0.5f, 6));
        CreateCoverBox(s, new Vector3(6, 0.5f, -6));
        CreateCoverBox(s, new Vector3(-22, 0.5f, 0));
        CreateCoverBox(s, new Vector3(22, 0.5f, 0));
        CreateCoverBox(s, new Vector3(0, 0.5f, -22));
        CreateCoverBox(s, new Vector3(0, 0.5f, 22));
        CreateCoverBox(s, new Vector3(-30, 0.5f, -15));
        CreateCoverBox(s, new Vector3(30, 0.5f, 15));
        CreateCoverBox(s, new Vector3(-15, 0.5f, -30));
        CreateCoverBox(s, new Vector3(15, 0.5f, 30));
    }

    static void CreateWall(GameObject parent, Vector3 pos, Vector3 scale, Color color)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = "Wall";
        w.transform.parent = parent.transform;
        w.transform.position = pos;
        w.transform.localScale = scale;
        w.GetComponent<Renderer>().material = LitMat(color, 0.1f, 0.5f);
    }

    static void CreatePlatform(GameObject parent, Vector3 pos, Vector3 scale)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name = "Platform";
        p.transform.parent = parent.transform;
        p.transform.position = pos;
        p.transform.localScale = scale;
        p.GetComponent<Renderer>().material = LitMat(new Color(0.32f, 0.3f, 0.28f), 0.12f, 0.55f);

        var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
        a.name = "Accent";
        a.transform.parent = p.transform;
        a.transform.localPosition = new Vector3(0, 0.16f, 0);
        a.transform.localScale = new Vector3(1.02f, 0.02f, 1.02f);
        Object.DestroyImmediate(a.GetComponent<Collider>());
        var accentMat = LitMat(new Color(0.15f, 0.5f, 1f), 0.6f, 0.85f);
        accentMat.EnableKeyword("_EMISSION");
        accentMat.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.8f) * 0.5f);
        a.GetComponent<Renderer>().material = accentMat;
    }

    static void CreateRamp(GameObject parent, Vector3 pos, Vector3 scale, float angle)
    {
        var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.name = "Ramp";
        r.transform.parent = parent.transform;
        r.transform.position = pos;
        r.transform.localScale = scale;
        r.transform.rotation = Quaternion.Euler(angle, 0, 0);
        r.GetComponent<Renderer>().material = LitMat(new Color(0.35f, 0.35f, 0.38f), 0.2f, 0.5f);
    }

    static void CreatePillar(GameObject parent, Vector3 pos)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = "Pillar";
        p.transform.parent = parent.transform;
        p.transform.position = pos + new Vector3(0, 3, 0);
        p.transform.localScale = new Vector3(1f, 3f, 1f);
        p.GetComponent<Renderer>().material = LitMat(new Color(0.42f, 0.4f, 0.38f), 0.25f, 0.55f);

        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "Cap";
        cap.transform.parent = p.transform;
        cap.transform.localPosition = new Vector3(0, 1f, 0);
        cap.transform.localScale = new Vector3(1.4f, 0.15f, 1.4f);
        Object.DestroyImmediate(cap.GetComponent<Collider>());
        var capMat = LitMat(new Color(0.15f, 0.45f, 1f), 0.6f, 0.85f);
        capMat.EnableKeyword("_EMISSION");
        capMat.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.8f) * 0.6f);
        cap.GetComponent<Renderer>().material = capMat;
    }

    static void CreateCoverBox(GameObject parent, Vector3 pos)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = "Cover";
        b.transform.parent = parent.transform;
        b.transform.position = pos;
        b.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
        b.GetComponent<Renderer>().material = LitMat(new Color(0.5f, 0.2f, 0.15f), 0.2f, 0.4f);
    }

    static void CreatePlayer(GameObject root)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.parent = root.transform;
        player.transform.position = new Vector3(0, 1.5f, -20);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0, 0.9f, 0);

        var pc = player.AddComponent<PlayerController>();
        var me = player.AddComponent<MovementEffects>();
        player.AddComponent<ArmAnimation>();

        var camObj = new GameObject("FPS_Camera");
        camObj.transform.parent = player.transform;
        camObj.transform.localPosition = new Vector3(0, 1.6f, 0);

        var cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 300f;
        cam.fieldOfView = 80f;
        cam.clearFlags = CameraClearFlags.Skybox;

        camObj.AddComponent<AudioListener>();

        var grapple = player.AddComponent<GrapplingHook>();
        var gp = player.AddComponent<GrapplePolish>();

        AssignEffectRefs(grapple, gp, me);

        CreateKatana(camObj);
    }

    static void AssignEffectRefs(GrapplingHook grapple, GrapplePolish gp, MovementEffects me)
    {
        gp.grappleParticles = Resources.Load<ParticleSystem>("Effects/GrappleParticles");
        gp.impactParticles = Resources.Load<ParticleSystem>("Effects/GrappleImpactParticles");
        gp.trailParticles = Resources.Load<ParticleSystem>("Effects/GrappleTrailParticles");

        me.dashParticles = Resources.Load<ParticleSystem>("Effects/DashParticles");
        me.slideParticles = Resources.Load<ParticleSystem>("Effects/SlideParticles");
        me.wallJumpParticles = Resources.Load<ParticleSystem>("Effects/WallJumpParticles");
        me.landParticles = Resources.Load<ParticleSystem>("Effects/LandParticles");

        var katanaTrail = grapple.GetComponentInChildren<KatanaTrail>();
        if (katanaTrail != null)
        {
            katanaTrail.hitFlashPrefab = Resources.Load<GameObject>("Effects/HitFlash");
        }
    }

    static void CreateKatana(GameObject cameraObj)
    {
        var katanaFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/katana.FBX");
        var handgripTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/handgrip_color.jpg");
        GameObject katana = null;

        if (katanaFbx != null)
        {
            katana = Object.Instantiate(katanaFbx, cameraObj.transform);
            Debug.Log("[PHANTOM EDGE] Loaded katana.FBX");
        }

        if (katana != null)
        {
            katana.name = "Katana";

            foreach (var r in katana.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;

                string objName = r.gameObject.name.ToLower();
                if (handgripTex != null && (objName.Contains("handle") || objName.Contains("grip") || objName.Contains("tsuka")))
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.mainTexture = handgripTex;
                    mat.SetFloat("_Metallic", 0.1f);
                    mat.SetFloat("_Smoothness", 0.3f);
                    r.material = mat;
                    Debug.Log("[PHANTOM EDGE] Applied handgrip texture to: " + r.gameObject.name);
                }
                else if (objName.Contains("blade") || objName.Contains("edge") || objName.Contains("sword"))
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.SetFloat("_Surface", 1);
                    mat.SetFloat("_Metallic", 1f);
                    mat.SetFloat("_Smoothness", 1f);
                    mat.SetFloat("_SpecularHighlights", 1f);
                    mat.SetFloat("_EnvironmentReflections", 1f);
                    mat.color = new Color(0.9f, 0.92f, 0.95f);

                    var metallicTex = new Texture2D(4, 4);
                    var pixels = new Color[16];
                    for (int i = 0; i < 16; i++) pixels[i] = Color.white;
                    metallicTex.SetPixels(pixels);
                    metallicTex.Apply();
                    mat.SetTexture("_MetallicGlossMap", metallicTex);

                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.5f, 0.6f, 0.7f));
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    r.material = mat;
                    Debug.Log("[PHANTOM EDGE] Applied chrome blade to: " + r.gameObject.name);
                }
                else if (objName.Contains("collar") || objName.Contains("habaki") || objName.Contains("bolster") || objName.Contains("guard") || objName.Contains("tsuba") || objName.Contains("fitting"))
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.SetFloat("_Metallic", 0.9f);
                    mat.SetFloat("_Smoothness", 0.85f);
                    mat.color = new Color(0.85f, 0.7f, 0.2f);

                    var metallicTex = new Texture2D(4, 4);
                    var pixels = new Color[16];
                    for (int i = 0; i < 16; i++) pixels[i] = new Color(0.9f, 0.9f, 0.9f);
                    metallicTex.SetPixels(pixels);
                    metallicTex.Apply();
                    mat.SetTexture("_MetallicGlossMap", metallicTex);

                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.8f, 0.6f, 0.1f) * 0.3f);
                    r.material = mat;
                    Debug.Log("[PHANTOM EDGE] Applied gold metallic to: " + r.gameObject.name);
                }

                Debug.Log("[PHANTOM EDGE] Katana renderer: " + r.gameObject.name + " | mat: " + (r.sharedMaterial != null ? r.sharedMaterial.name : "null"));
            }

            katana.transform.localPosition = new Vector3(0.35f, -0.25f, 0.4f);
            katana.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
            katana.transform.localScale = Vector3.one * 0.15f;
            katana.AddComponent<WeaponSway>();
            
            var auraMat = new Material(Shader.Find("Custom/KatanaAura"));
            auraMat.SetColor("_Color", new Color(1f, 0.7f, 0.2f));
            auraMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f));
            auraMat.SetFloat("_Intensity", 5f);
            auraMat.SetFloat("_PulseSpeed", 3f);
            auraMat.SetFloat("_Distortion", 0.15f);
            auraMat.SetFloat("_FresnelPower", 3f);
            auraMat.SetFloat("_FresnelIntensity", 2f);

            var auraObj = new GameObject("Aura");
            auraObj.transform.SetParent(katana.transform);
            auraObj.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            auraObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            auraObj.transform.localScale = Vector3.one * 1.35f;
            var auraRenderer = auraObj.AddComponent<MeshRenderer>();
            auraRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            auraRenderer.receiveShadows = false;
            var auraFilter = auraObj.AddComponent<MeshFilter>();
            auraFilter.sharedMesh = katana.GetComponentInChildren<MeshFilter>()?.sharedMesh;
            auraRenderer.material = auraMat;
            var aura = auraObj.AddComponent<KatanaAura>();
            aura.auraMaterial = auraMat;
            aura.auraIntensity = 3f;
            aura.pulseSpeed = 2f;
            aura.animateOnSwing = true;

            var trailObj = new GameObject("Trail");
            trailObj.transform.SetParent(katana.transform);
            trailObj.transform.localPosition = Vector3.zero;
            trailObj.transform.localRotation = Quaternion.identity;
            var trail = trailObj.AddComponent<KatanaTrail>();
            var trailRenderer = trailObj.GetComponent<TrailRenderer>();
            var trailMat = new Material(Shader.Find("Custom/KatanaTrail"));
            trailMat.SetColor("_Color", new Color(1f, 0.8f, 0.2f));
            trailMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f));
            trailMat.SetFloat("_Intensity", 4f);
            trail.trailMaterial = trailMat;
            trail.widthCurve = AnimationCurve.EaseInOut(0, 0.08f, 1, 0f);
            var swingGrad = new Gradient();
            swingGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.5f), new GradientColorKey(new Color(0.8f, 0.1f, 0.05f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            var idleGrad = new Gradient();
            idleGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.3f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = idleGrad;
            aura.trailRenderer = trailRenderer;
            aura.swingTrailGradient = swingGrad;
            aura.idleTrailGradient = idleGrad;

            katana.AddComponent<KatanaSwingDetector>();
            
            var pc = cameraObj.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                pc.katanaAura = aura;
                pc.katanaTrail = trail;
                pc.swingDetector = katana.GetComponent<KatanaSwingDetector>();
            }
            
            Debug.Log("[PHANTOM EDGE] Katana loaded with Aura + Trail. Renderers: " + katana.GetComponentsInChildren<Renderer>().Length);
        }
        else
        {
            katana = CreateFallbackKatana(cameraObj);
        }
    }

    static GameObject CreateFallbackKatana(GameObject parent)
    {
        var sword = new GameObject("Katana");

        var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Blade";
        blade.transform.SetParent(sword.transform);
        blade.transform.localPosition = new Vector3(0, 0.4f, 0);
        blade.transform.localScale = new Vector3(0.02f, 0.8f, 0.005f);
        Object.DestroyImmediate(blade.GetComponent<Collider>());
        blade.GetComponent<Renderer>().material = LitMat(new Color(0.85f, 0.85f, 0.9f), 0.95f, 0.95f);

        var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = "Edge";
        edge.transform.SetParent(sword.transform);
        edge.transform.localPosition = new Vector3(0.012f, 0.4f, 0);
        edge.transform.localScale = new Vector3(0.002f, 0.78f, 0.006f);
        Object.DestroyImmediate(edge.GetComponent<Collider>());
        var edgeMat = LitMat(new Color(0.95f, 0.95f, 0.98f), 1f, 1f);
        edgeMat.EnableKeyword("_EMISSION");
        edgeMat.SetColor("_EmissionColor", new Color(0.3f, 0.4f, 0.5f) * 0.3f);
        edge.GetComponent<Renderer>().material = edgeMat;

        var tsuba = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tsuba.name = "Tsuba";
        tsuba.transform.SetParent(sword.transform);
        tsuba.transform.localPosition = new Vector3(0, -0.01f, 0);
        tsuba.transform.localScale = new Vector3(0.06f, 0.01f, 0.04f);
        Object.DestroyImmediate(tsuba.GetComponent<Collider>());
        tsuba.GetComponent<Renderer>().material = LitMat(new Color(0.15f, 0.12f, 0.08f), 0.3f, 0.4f);

        var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(sword.transform);
        handle.transform.localPosition = new Vector3(0, -0.1f, 0);
        handle.transform.localScale = new Vector3(0.012f, 0.1f, 0.012f);
        Object.DestroyImmediate(handle.GetComponent<Collider>());
        handle.GetComponent<Renderer>().material = LitMat(new Color(0.25f, 0.15f, 0.1f), 0.1f, 0.3f);

        sword.transform.parent = parent.transform;
        sword.transform.localPosition = new Vector3(0.35f, -0.3f, 0.3f);
        sword.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
        sword.transform.localScale = Vector3.one;
        sword.AddComponent<WeaponSway>();

        var auraMat = new Material(Shader.Find("Custom/KatanaAura"));
        auraMat.SetColor("_Color", new Color(1f, 0.7f, 0.2f));
        auraMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f));
        auraMat.SetFloat("_Intensity", 5f);
        auraMat.SetFloat("_PulseSpeed", 3f);

        var auraObj = new GameObject("Aura");
        auraObj.transform.SetParent(sword.transform);
        auraObj.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        auraObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        auraObj.transform.localScale = Vector3.one * 1.35f;
        var auraRenderer = auraObj.AddComponent<MeshRenderer>();
        auraRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        auraRenderer.receiveShadows = false;
        var auraFilter = auraObj.AddComponent<MeshFilter>();
        auraFilter.sharedMesh = sword.GetComponentInChildren<MeshFilter>()?.sharedMesh;
        auraRenderer.material = auraMat;
        var aura = auraObj.AddComponent<KatanaAura>();
        aura.auraMaterial = auraMat;
        aura.auraIntensity = 3f;
        aura.pulseSpeed = 2f;
        aura.animateOnSwing = true;

            var trailObj = new GameObject("Trail");
            trailObj.transform.SetParent(sword.transform);
            trailObj.transform.localPosition = Vector3.zero;
            trailObj.transform.localRotation = Quaternion.identity;
            var trail = trailObj.AddComponent<KatanaTrail>();
            var trailRenderer = trailObj.GetComponent<TrailRenderer>();
        var trailMat = new Material(Shader.Find("Custom/KatanaTrail"));
        trailMat.SetColor("_Color", new Color(1f, 0.8f, 0.2f));
        trailMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f));
        trailMat.SetFloat("_Intensity", 4f);
        trail.trailMaterial = trailMat;
        trail.widthCurve = AnimationCurve.EaseInOut(0, 0.08f, 1, 0f);
        var swingGrad = new Gradient();
        swingGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.5f), new GradientColorKey(new Color(0.8f, 0.1f, 0.05f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        var idleGrad = new Gradient();
        idleGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.3f, 0.7f, 1f, 0.3f), 0f), new GradientColorKey(new Color(0.1f, 0.4f, 0.8f, 0.1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trail.colorGradient = idleGrad;
        aura.trailRenderer = trailRenderer;
        aura.swingTrailGradient = swingGrad;
        aura.idleTrailGradient = idleGrad;

        sword.AddComponent<KatanaSwingDetector>();

        var pc = parent.GetComponentInParent<PlayerController>();
        if (pc != null)
        {
            pc.katanaAura = aura;
            pc.katanaTrail = trail;
            pc.swingDetector = sword.GetComponent<KatanaSwingDetector>();
        }

        return sword;
    }

    static void CreateGameManager(GameObject root)
    {
        var gmObj = new GameObject("GameManager");
        gmObj.transform.parent = root.transform;
        gmObj.AddComponent<GameManager>();
        gmObj.AddComponent<HitPause>();
        gmObj.AddComponent<CameraShake>();
        gmObj.AddComponent<ComboSystem>();
        gmObj.AddComponent<AudioManager>();
        var pool = gmObj.AddComponent<ObjectPool>();
        pool.pools = new List<ObjectPool.Pool>
        {
            new ObjectPool.Pool { tag = "Spark", initialSize = 8 },
            new ObjectPool.Pool { tag = "Blood", initialSize = 4 },
            new ObjectPool.Pool { tag = "Gib", initialSize = 6 },
        };
        Debug.Log("[PHANTOM EDGE] GameManager created with HitPause, CameraShake, ComboSystem, AudioManager, ObjectPool.");
    }

    static void CreateEnemySpawner(GameObject root)
    {
        var spawnerObj = new GameObject("EnemySpawner");
        spawnerObj.transform.parent = root.transform;
        var spawner = spawnerObj.AddComponent<EnemySpawner>();
        spawner.spawnRadius = 30f;
        spawner.minSpawnDistance = 15f;
        spawner.maxEnemies = 12;
        spawner.spawnInterval = 3f;
        spawner.despawnDistance = 60f;
        spawner.enemiesPerWave = 5;
        spawner.waveCooldown = 4f;
        Debug.Log("[PHANTOM EDGE] EnemySpawner created.");
    }

    static void CreateGrapplePoints(GameObject root)
    {
        var points = new GameObject("GrapplePoints");
        points.transform.parent = root.transform;

        Material pointMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        pointMat.color = new Color(1f, 0.4f, 0.1f);
        pointMat.EnableKeyword("_EMISSION");
        pointMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.05f) * 2f);

        Vector3[] positions = {
            new Vector3(-38, 8, -38), new Vector3(38, 8, -38),
            new Vector3(-38, 8, 38), new Vector3(38, 8, 38),
            new Vector3(-38, 11, 0), new Vector3(38, 11, 0),
            new Vector3(-15, 8, -15), new Vector3(15, 8, -15),
            new Vector3(-15, 8, 15), new Vector3(15, 8, 15),
            new Vector3(0, 8, 0),
            new Vector3(-20, 6, -20), new Vector3(20, 6, -20),
            new Vector3(-20, 6, 20), new Vector3(20, 6, 20),
        };

        foreach (var pos in positions)
        {
            var gp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gp.name = "GrapplePoint";
            gp.transform.parent = points.transform;
            gp.transform.position = pos;
            gp.transform.localScale = Vector3.one * 0.4f;
            var gpCol = gp.GetComponent<SphereCollider>();
            if (gpCol != null) gpCol.isTrigger = true;
            var rend = gp.GetComponent<Renderer>();
            rend.material = pointMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        Debug.Log("[PHANTOM EDGE] " + positions.Length + " grapple points created.");
    }

    static void CreateUI(GameObject root)
    {
        var canvasGO = new GameObject("UICanvas");
        canvasGO.transform.parent = root.transform;
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var uiManager = canvasGO.AddComponent<UIManager>();

        var damageVignetteObj = new GameObject("DamageVignette");
        damageVignetteObj.transform.SetParent(canvasGO.transform, false);
        var dvRect = damageVignetteObj.AddComponent<RectTransform>();
        dvRect.anchorMin = Vector2.zero;
        dvRect.anchorMax = Vector2.one;
        dvRect.sizeDelta = Vector2.zero;
        var dvImg = damageVignetteObj.AddComponent<UnityEngine.UI.Image>();
        dvImg.color = Color.clear;
        uiManager.damageVignette = dvImg;

        var hpBarObj = CreateSlider(canvasGO.transform, "HPBar",
            new Vector2(20, -20), new Vector2(200, 20), new Color(0.8f, 0.15f, 0.15f));
        uiManager.hpBar = hpBarObj.GetComponent<UnityEngine.UI.Slider>();

        var hpTextObj = CreateTMPText(canvasGO.transform, "HPText", "100",
            new Vector2(225, -20), 16, Color.white, TextAlignmentOptions.MidlineLeft);
        uiManager.hpText = hpTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        var scoreTextObj = CreateTMPText(canvasGO.transform, "ScoreText", "0",
            new Vector2(-20, -20), 20, Color.white, TextAlignmentOptions.MidlineRight);
        uiManager.scoreText = scoreTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        scoreTextObj.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
        scoreTextObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        scoreTextObj.GetComponent<RectTransform>().pivot = new Vector2(1, 1);

        var waveTextObj = CreateTMPText(canvasGO.transform, "WaveText", "WAVE 1",
            new Vector2(0, -20), 18, new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Midline);
        uiManager.waveText = waveTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        waveTextObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1);
        waveTextObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1);
        waveTextObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);

        var killTextObj = CreateTMPText(canvasGO.transform, "KillText", "0",
            new Vector2(-20, -48), 16, new Color(1f, 0.4f, 0.4f), TextAlignmentOptions.MidlineRight);
        uiManager.killText = killTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        killTextObj.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
        killTextObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        killTextObj.GetComponent<RectTransform>().pivot = new Vector2(1, 1);

        var speedGaugeObj = new GameObject("SpeedGauge");
        speedGaugeObj.transform.SetParent(canvasGO.transform, false);
        var speedRect = speedGaugeObj.AddComponent<RectTransform>();
        speedRect.anchorMin = new Vector2(0.5f, 0);
        speedRect.anchorMax = new Vector2(0.5f, 0);
        speedRect.pivot = new Vector2(0.5f, 0);
        speedRect.anchoredPosition = new Vector2(0, 30);
        speedRect.sizeDelta = new Vector2(200, 8);
        var speedImg = speedGaugeObj.AddComponent<UnityEngine.UI.Image>();
        speedImg.color = new Color(0.3f, 0.3f, 0.3f);
        speedImg.type = UnityEngine.UI.Image.Type.Filled;
        speedImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        uiManager.speedGauge = speedImg;

        var speedTextObj = CreateTMPText(canvasGO.transform, "SpeedText", "0 km/h",
            new Vector2(0, 42), 12, new Color(0.5f, 1f, 0.5f), TextAlignmentOptions.Bottom);
        var stRect = speedTextObj.GetComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0.5f, 0);
        stRect.anchorMax = new Vector2(0.5f, 0);
        stRect.pivot = new Vector2(0.5f, 0);
        uiManager.speedText = speedTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        var dashPanelObj = new GameObject("DashIcons");
        dashPanelObj.transform.SetParent(canvasGO.transform, false);
        var dpRect = dashPanelObj.AddComponent<RectTransform>();
        dpRect.anchorMin = new Vector2(0, 1);
        dpRect.anchorMax = new Vector2(0, 1);
        dpRect.pivot = new Vector2(0, 1);
        dpRect.anchoredPosition = new Vector2(20, -55);
        dpRect.sizeDelta = new Vector2(80, 20);
        var dashLayout = dashPanelObj.AddComponent<HorizontalLayoutGroup>();
        dashLayout.spacing = 8;
        dashLayout.childAlignment = TextAnchor.MiddleLeft;
        uiManager.dashIcons = new UnityEngine.UI.Image[2];
        for (int i = 0; i < 2; i++)
        {
            var dashIcon = new GameObject("DashIcon_" + i);
            dashIcon.transform.SetParent(dashPanelObj.transform, false);
            var diRect = dashIcon.AddComponent<RectTransform>();
            diRect.sizeDelta = new Vector2(12, 12);
            var diImg = dashIcon.AddComponent<UnityEngine.UI.Image>();
            diImg.color = Color.white;
            uiManager.dashIcons[i] = diImg;
        }

        var grappleCooldownObj = new GameObject("GrappleCooldown");
        grappleCooldownObj.transform.SetParent(canvasGO.transform, false);
        var gcRect = grappleCooldownObj.AddComponent<RectTransform>();
        gcRect.anchorMin = new Vector2(0, 1);
        gcRect.anchorMax = new Vector2(0, 1);
        gcRect.pivot = new Vector2(0, 1);
        gcRect.anchoredPosition = new Vector2(20, -80);
        gcRect.sizeDelta = new Vector2(50, 50);
        var gcImg = grappleCooldownObj.AddComponent<UnityEngine.UI.Image>();
        gcImg.type = UnityEngine.UI.Image.Type.Filled;
        gcImg.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        gcImg.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
        gcImg.fillAmount = 1f;
        gcImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        uiManager.grappleCooldownFill = gcImg;

        var grappleKeyObj = CreateTMPText(grappleCooldownObj.transform, "GrappleKey", "Q",
            new Vector2(0, 0), 14, Color.white, TextAlignmentOptions.Center);
        var gkRect = grappleKeyObj.GetComponent<RectTransform>();
        gkRect.anchorMin = new Vector2(0.5f, 0.5f);
        gkRect.anchorMax = new Vector2(0.5f, 0.5f);
        gkRect.pivot = new Vector2(0.5f, 0.5f);
        gkRect.sizeDelta = new Vector2(50, 50);
        uiManager.grappleKeyText = grappleKeyObj.GetComponent<TMPro.TextMeshProUGUI>();

        var damageDirObj = new GameObject("DamageDirectionContainer");
        damageDirObj.transform.SetParent(canvasGO.transform, false);
        var ddRect = damageDirObj.AddComponent<RectTransform>();
        ddRect.anchorMin = new Vector2(0.5f, 0.5f);
        ddRect.anchorMax = new Vector2(0.5f, 0.5f);
        ddRect.sizeDelta = new Vector2(400, 400);
        uiManager.damageDirectionContainer = ddRect;

        var crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(canvasGO.transform, false);
        var chRect = crosshairObj.AddComponent<RectTransform>();
        chRect.anchorMin = new Vector2(0.5f, 0.5f);
        chRect.anchorMax = new Vector2(0.5f, 0.5f);
        chRect.sizeDelta = new Vector2(12, 12);
        var chImg = crosshairObj.AddComponent<UnityEngine.UI.Image>();
        chImg.color = new Color(1f, 1f, 1f, 0.8f);
        uiManager.crosshair = chImg;
        uiManager.crosshairRect = chRect;

        var gameOverPanel = CreatePanel(canvasGO.transform, "GameOverPanel",
            new Color(0, 0, 0, 0.7f));
        uiManager.gameOverPanel = gameOverPanel;

        var gameOverText = CreateTMPText(gameOverPanel.transform, "GameOverText", "YOU DIED",
            Vector2.zero, 48, new Color(1f, 0.2f, 0.2f), TextAlignmentOptions.Center);
        var gotRect = gameOverText.GetComponent<RectTransform>();
        gotRect.anchorMin = new Vector2(0.5f, 0.6f);
        gotRect.anchorMax = new Vector2(0.5f, 0.6f);
        gotRect.sizeDelta = new Vector2(600, 60);
        uiManager.gameOverText = gameOverText.GetComponent<TMPro.TextMeshProUGUI>();

        var finalScoreText = CreateTMPText(gameOverPanel.transform, "FinalScoreText", "SCORE: 0",
            Vector2.zero, 28, Color.white, TextAlignmentOptions.Center);
        var fsRect = finalScoreText.GetComponent<RectTransform>();
        fsRect.anchorMin = new Vector2(0.5f, 0.45f);
        fsRect.anchorMax = new Vector2(0.5f, 0.45f);
        fsRect.sizeDelta = new Vector2(400, 40);
        uiManager.finalScoreText = finalScoreText.GetComponent<TMPro.TextMeshProUGUI>();

        var restartHint = CreateTMPText(gameOverPanel.transform, "RestartHint", "ESC to Restart",
            Vector2.zero, 18, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
        var rhRect = restartHint.GetComponent<RectTransform>();
        rhRect.anchorMin = new Vector2(0.5f, 0.35f);
        rhRect.anchorMax = new Vector2(0.5f, 0.35f);
        rhRect.sizeDelta = new Vector2(300, 30);

        var wavePanel = CreatePanel(canvasGO.transform, "WaveAnnouncePanel",
            new Color(0, 0, 0, 0.5f));
        uiManager.waveAnnouncePanel = wavePanel;

        var waveAnnounceText = CreateTMPText(wavePanel.transform, "WaveAnnounceText", "WAVE 1",
            Vector2.zero, 56, new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Center);
        var waRect = waveAnnounceText.GetComponent<RectTransform>();
        waRect.anchorMin = new Vector2(0.5f, 0.5f);
        waRect.anchorMax = new Vector2(0.5f, 0.5f);
        waRect.sizeDelta = new Vector2(600, 80);
        uiManager.waveAnnounceText = waveAnnounceText.GetComponent<TMPro.TextMeshProUGUI>();

        CreateComboUI(canvasGO.transform, uiManager);
        CreateMainMenu(canvasGO.transform, root);

        Debug.Log("[PHANTOM EDGE] UI created with UIManager.");
    }

    static void CreateComboUI(Transform parent, UIManager uiManager)
    {
        var comboPanelObj = new GameObject("ComboPanel");
        comboPanelObj.transform.SetParent(parent, false);
        var comboRect = comboPanelObj.AddComponent<RectTransform>();
        comboRect.anchorMin = new Vector2(0.5f, 0.5f);
        comboRect.anchorMax = new Vector2(0.5f, 0.5f);
        comboRect.pivot = new Vector2(0.5f, 0.5f);
        comboRect.anchoredPosition = new Vector2(0, 80);
        comboRect.sizeDelta = new Vector2(200, 60);
        comboPanelObj.SetActive(false);
        uiManager.comboPanel = comboRect;

        var comboTextObj = CreateTMPText(comboPanelObj.transform, "ComboText", "0",
            new Vector2(0, 10), 48, Color.white, TextAlignmentOptions.Center);
        var ctRect = comboTextObj.GetComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0.5f, 0.5f);
        ctRect.anchorMax = new Vector2(0.5f, 0.5f);
        ctRect.pivot = new Vector2(0.5f, 0.5f);
        ctRect.sizeDelta = new Vector2(200, 60);
        uiManager.comboText = comboTextObj.GetComponent<TMPro.TextMeshProUGUI>();
        comboTextObj.GetComponent<TMPro.TextMeshProUGUI>().enableAutoSizing = true;
        comboTextObj.GetComponent<TMPro.TextMeshProUGUI>().fontSizeMax = 64;

        var comboRankObj = CreateTMPText(comboPanelObj.transform, "ComboRankText", "",
            new Vector2(0, -40), 20, new Color(1f, 0.8f, 0.2f), TextAlignmentOptions.Center);
        var crRect = comboRankObj.GetComponent<RectTransform>();
        crRect.anchorMin = new Vector2(0.5f, 0.5f);
        crRect.anchorMax = new Vector2(0.5f, 0.5f);
        crRect.pivot = new Vector2(0.5f, 0.5f);
        crRect.sizeDelta = new Vector2(200, 30);
        uiManager.comboRankText = comboRankObj.GetComponent<TMPro.TextMeshProUGUI>();
        comboRankObj.SetActive(false);
    }

static void CreateMainMenu(Transform canvasTransform, GameObject root)
    {
        var menuObj = new GameObject("MenuManager");
        menuObj.transform.parent = root.transform;
        var menuManager = menuObj.AddComponent<MenuManager>();

        Color panelBg = new Color(0.03f, 0.03f, 0.05f, 0.98f);
        Color panelBgLight = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        Color accentGold = new Color(1f, 0.85f, 0.3f);
        Color accentOrange = new Color(1f, 0.55f, 0.15f);
        Color buttonBg = new Color(0.1f, 0.08f, 0.06f);
        Color buttonHover = new Color(0.22f, 0.18f, 0.1f);
        Color buttonPressed = new Color(0.3f, 0.24f, 0.12f);
        Color textColor = new Color(0.95f, 0.9f, 0.85f);
        Color textDim = new Color(0.6f, 0.55f, 0.5f);

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasTransform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.03f, 1f);
        menuManager.backgroundImage = bgImg;

        // Menu particles
        var particlesObj = CreateMenuParticles(canvasTransform);
        menuManager.menuParticles = particlesObj.GetComponent<ParticleSystem>();

        var mainMenuPanel = CreateStyledPanel(canvasTransform, "MainMenuPanel", panelBg);
        var pauseMenuPanel = CreateStyledPanel(canvasTransform, "PauseMenuPanel", panelBg);
        pauseMenuPanel.SetActive(false);
        var gameOverMenuPanel = CreateStyledPanel(canvasTransform, "GameOverMenuPanel", panelBg);
        gameOverMenuPanel.SetActive(false);
        var settingsPanel = CreateStyledPanel(canvasTransform, "SettingsPanel", panelBgLight);
        settingsPanel.SetActive(false);

        menuManager.mainMenuPanel = mainMenuPanel;
        menuManager.pauseMenuPanel = pauseMenuPanel;
        menuManager.gameOverMenuPanel = gameOverMenuPanel;
        menuManager.settingsPanel = settingsPanel;

        // CanvasGroups for fading
        menuManager.mainMenuCanvasGroup = mainMenuPanel.GetComponent<CanvasGroup>() ?? mainMenuPanel.AddComponent<CanvasGroup>();
        menuManager.pauseMenuCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>() ?? pauseMenuPanel.AddComponent<CanvasGroup>();
        menuManager.gameOverCanvasGroup = gameOverMenuPanel.GetComponent<CanvasGroup>() ?? gameOverMenuPanel.AddComponent<CanvasGroup>();
        menuManager.settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>() ?? settingsPanel.AddComponent<CanvasGroup>();

        // ===== MAIN MENU =====
        var titleObj = CreateTMPText(mainMenuPanel.transform, "TitleText", "PHANTOM EDGE",
            new Vector2(0, 180), 84, accentGold, TextAlignmentOptions.Center);
        var titleTmp = titleObj.GetComponent<TMPro.TextMeshProUGUI>();
        titleTmp.enableAutoSizing = true;
        titleTmp.fontSizeMax = 100;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.colorGradient = new VertexGradient(
            new Color(1f, 0.9f, 0.5f), new Color(1f, 0.7f, 0.2f),
            new Color(1f, 0.5f, 0.1f), new Color(0.8f, 0.3f, 0.05f));

        var subtitleObj = CreateTMPText(mainMenuPanel.transform, "SubtitleText", "KATANA ARENA",
            new Vector2(0, 100), 22, accentOrange, TextAlignmentOptions.Center);
        subtitleObj.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = FontStyles.UpperCase;
        subtitleObj.GetComponent<TMPro.TextMeshProUGUI>().characterSpacing = 300;

        var startBtnObj = CreateStyledButton(mainMenuPanel.transform, "StartButton", "START GAME",
            new Vector2(0, 10), new Vector2(320, 70), 32, buttonBg, buttonHover, buttonPressed, textColor, accentGold);
        menuManager.startButton = startBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(startBtnObj, accentGold, buttonBg);

        var settingsBtnObj = CreateStyledButton(mainMenuPanel.transform, "SettingsButton", "SETTINGS",
            new Vector2(0, -75), new Vector2(320, 55), 24, buttonBg, buttonHover, buttonPressed, textColor, Color.white);
        menuManager.settingsButton = settingsBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(settingsBtnObj, accentOrange, buttonBg);

        var quitBtnObj = CreateStyledButton(mainMenuPanel.transform, "QuitButton", "QUIT",
            new Vector2(0, -145), new Vector2(320, 55), 24, buttonBg, buttonHover, buttonPressed, new Color(0.9f, 0.4f, 0.3f), new Color(1f, 0.3f, 0.2f));
        menuManager.quitButton = quitBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(quitBtnObj, new Color(1f, 0.3f, 0.2f), buttonBg);

        var versionObj = CreateTMPText(mainMenuPanel.transform, "VersionText", "v1.0.0",
            new Vector2(0, -210), 14, textDim, TextAlignmentOptions.Center);
        menuManager.versionText = versionObj.GetComponent<TMPro.TextMeshProUGUI>();

        // ===== PAUSE MENU =====
        var pauseTitle = CreateTMPText(pauseMenuPanel.transform, "PauseTitle", "PAUSED",
            new Vector2(0, 160), 60, accentGold, TextAlignmentOptions.Center);
        pauseTitle.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var resumeBtnObj = CreateStyledButton(pauseMenuPanel.transform, "ResumeButton", "RESUME",
            new Vector2(0, 60), new Vector2(300, 65), 28, buttonBg, buttonHover, buttonPressed, textColor, accentGold);
        menuManager.resumeButton = resumeBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(resumeBtnObj, accentGold, buttonBg);

        var restartBtnObj = CreateStyledButton(pauseMenuPanel.transform, "RestartButton", "RESTART",
            new Vector2(0, -10), new Vector2(300, 55), 24, buttonBg, buttonHover, buttonPressed, textColor, Color.white);
        menuManager.restartButton = restartBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(restartBtnObj, accentOrange, buttonBg);

        var pauseSettingsBtnObj = CreateStyledButton(pauseMenuPanel.transform, "PauseSettingsButton", "SETTINGS",
            new Vector2(0, -80), new Vector2(300, 55), 24, buttonBg, buttonHover, buttonPressed, textColor, Color.white);
        menuManager.pauseSettingsButton = pauseSettingsBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(pauseSettingsBtnObj, accentOrange, buttonBg);

        var pauseQuitBtnObj = CreateStyledButton(pauseMenuPanel.transform, "PauseQuitButton", "QUIT TO MENU",
            new Vector2(0, -150), new Vector2(300, 55), 24, buttonBg, buttonHover, buttonPressed, new Color(0.9f, 0.4f, 0.3f), new Color(1f, 0.3f, 0.2f));
        menuManager.pauseQuitButton = pauseQuitBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(pauseQuitBtnObj, new Color(1f, 0.3f, 0.2f), buttonBg);

        // ===== GAME OVER MENU =====
        var goTitle = CreateTMPText(gameOverMenuPanel.transform, "GameOverTitle", "YOU DIED",
            new Vector2(0, 180), 72, new Color(1f, 0.25f, 0.15f), TextAlignmentOptions.Center);
        goTitle.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        goTitle.GetComponent<TMPro.TextMeshProUGUI>().enableAutoSizing = true;
        goTitle.GetComponent<TMPro.TextMeshProUGUI>().fontSizeMax = 90;

        var scoreTextObj = CreateTMPText(gameOverMenuPanel.transform, "GameOverScoreText", "SCORE: 0",
            new Vector2(0, 90), 36, Color.white, TextAlignmentOptions.Center);
        menuManager.gameOverScoreText = scoreTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        var waveTextObj = CreateTMPText(gameOverMenuPanel.transform, "GameOverWaveText", "WAVE REACHED: 1",
            new Vector2(0, 45), 28, accentGold, TextAlignmentOptions.Center);
        menuManager.gameOverWaveText = waveTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        var killsTextObj = CreateTMPText(gameOverMenuPanel.transform, "GameOverKillsText", "KILLS: 0",
            new Vector2(0, 0), 24, new Color(1f, 0.6f, 0.3f), TextAlignmentOptions.Center);
        menuManager.gameOverKillsText = killsTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        var retryBtnObj = CreateStyledButton(gameOverMenuPanel.transform, "RetryButton", "RETRY",
            new Vector2(0, -60), new Vector2(320, 70), 32, buttonBg, buttonHover, buttonPressed, textColor, accentGold);
        menuManager.retryButton = retryBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(retryBtnObj, accentGold, buttonBg);

        var mainMenuBtnObj = CreateStyledButton(gameOverMenuPanel.transform, "MainMenuButton", "MAIN MENU",
            new Vector2(0, -145), new Vector2(320, 55), 24, buttonBg, buttonHover, buttonPressed, textColor, Color.white);
        menuManager.mainMenuButton = mainMenuBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(mainMenuBtnObj, accentOrange, buttonBg);

        var gameOverQuitBtnObj = CreateStyledButton(gameOverMenuPanel.transform, "GameOverQuitButton", "QUIT",
            new Vector2(0, -210), new Vector2(320, 55), 24, buttonBg, buttonHover, buttonPressed, new Color(0.9f, 0.4f, 0.3f), new Color(1f, 0.3f, 0.2f));
        menuManager.gameOverQuitButton = gameOverQuitBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(gameOverQuitBtnObj, new Color(1f, 0.3f, 0.2f), buttonBg);

        // ===== SETTINGS PANEL =====
        var settingsTitle = CreateTMPText(settingsPanel.transform, "SettingsTitle", "SETTINGS",
            new Vector2(0, 200), 50, accentGold, TextAlignmentOptions.Center);
        settingsTitle.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Volume
        var volumeObj = CreateSliderSetting(settingsPanel.transform, "Volume", "MASTER VOLUME",
            new Vector2(0, 80), new Vector2(400, 60), 0f, 1f, PlayerPrefs.GetFloat("MasterVolume", 1f));
        menuManager.volumeSlider = volumeObj.GetComponentInChildren<UnityEngine.UI.Slider>();

        // Sensitivity
        var sensObj = CreateSliderSetting(settingsPanel.transform, "Sensitivity", "MOUSE SENSITIVITY",
            new Vector2(0, 0), new Vector2(400, 60), 0.5f, 5f, PlayerPrefs.GetFloat("MouseSensitivity", 2f));
        menuManager.sensitivitySlider = sensObj.GetComponentInChildren<UnityEngine.UI.Slider>();

        // Fullscreen
        var fsObj = CreateToggleSetting(settingsPanel.transform, "Fullscreen", "FULLSCREEN",
            new Vector2(0, -80), new Vector2(400, 50), Screen.fullScreen);
        menuManager.fullscreenToggle = fsObj.GetComponentInChildren<UnityEngine.UI.Toggle>();

        var settingsBackBtnObj = CreateStyledButton(settingsPanel.transform, "SettingsBackButton", "BACK",
            new Vector2(0, -180), new Vector2(280, 55), 24, buttonBg, buttonHover, buttonPressed, textColor, accentOrange);
        menuManager.settingsBackButton = settingsBackBtnObj.GetComponent<UnityEngine.UI.Button>();
        AddButtonHoverEffect(settingsBackBtnObj, accentOrange, buttonBg);

        Debug.Log("[PHANTOM EDGE] Enhanced Main Menu created with MenuManager.");
    }

    static GameObject CreateMenuParticles(Transform parent)
    {
        var obj = new GameObject("MenuParticles");
        obj.transform.SetParent(parent, false);
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 3f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.7f, 0.2f, 0.15f),
            new Color(1f, 0.4f, 0.1f, 0.05f));
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.rateOverTime = 8f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(1920, 1080, 1);
        shape.position = new Vector3(0, 0, 50);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(-1f, -3f);
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-10f, 10f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.EnableKeyword("_EMISSION");
        ps.GetComponent<ParticleSystemRenderer>().material = mat;
        return obj;
    }

    static void AddButtonHoverEffect(GameObject btnObj, Color hoverColor, Color normalColor)
    {
        var btn = btnObj.GetComponent<UnityEngine.UI.Button>();
        var colors = btn.colors;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = Color.Lerp(hoverColor, normalColor, 0.5f);
        colors.colorMultiplier = 1.2f;
        btn.colors = colors;
        
        var scaleAnim = btnObj.AddComponent<ButtonScaleAnim>();
        scaleAnim.normalScale = Vector3.one;
        scaleAnim.hoverScale = Vector3.one * 1.05f;
        scaleAnim.pressScale = Vector3.one * 0.97f;
        scaleAnim.animationSpeed = 15f;
    }
    
    public class ButtonScaleAnim : MonoBehaviour
    {
        public Vector3 normalScale = Vector3.one;
        public Vector3 hoverScale = Vector3.one * 1.05f;
        public Vector3 pressScale = Vector3.one * 0.97f;
        public float animationSpeed = 15f;
        private UnityEngine.UI.Button btn;
        private bool isPressed;

        void Awake()
        {
            btn = GetComponent<UnityEngine.UI.Button>();
            var eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            
            AddTrigger(eventTrigger, UnityEngine.EventSystems.EventTriggerType.PointerEnter, (data) => { isPressed = false; });
            AddTrigger(eventTrigger, UnityEngine.EventSystems.EventTriggerType.PointerExit, (data) => { isPressed = false; });
            AddTrigger(eventTrigger, UnityEngine.EventSystems.EventTriggerType.PointerDown, (data) => { isPressed = true; });
            AddTrigger(eventTrigger, UnityEngine.EventSystems.EventTriggerType.PointerUp, (data) => { isPressed = false; });
        }

        void AddTrigger(UnityEngine.EventSystems.EventTrigger trigger, UnityEngine.EventSystems.EventTriggerType type, System.Action<UnityEngine.EventSystems.BaseEventData> action)
        {
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
            entry.callback.AddListener((data) => action(data));
            trigger.triggers.Add(entry);
        }

        void Update()
        {
            Vector3 target = isPressed ? pressScale : (btn.IsHighlighted() ? hoverScale : normalScale);
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    static GameObject CreateStyledPanel(Transform parent, string name, Color bgColor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var img = obj.AddComponent<UnityEngine.UI.Image>();
        img.color = bgColor;
        img.raycastTarget = true;
        obj.SetActive(false);
        return obj;
    }

    static GameObject CreateStyledButton(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, float fontSize,
        Color normalColor, Color hoverColor, Color pressedColor, Color textColor, Color accentColor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var btn = obj.AddComponent<UnityEngine.UI.Button>();
        var img = obj.AddComponent<UnityEngine.UI.Image>();
        img.color = normalColor;
        img.type = UnityEngine.UI.Image.Type.Sliced;

        var colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = hoverColor;
        colors.disabledColor = new Color(normalColor.r * 0.5f, normalColor.g * 0.5f, normalColor.b * 0.5f, normalColor.a);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.transition = UnityEngine.UI.Selectable.Transition.ColorTint;

        var textObj = CreateTMPText(obj.transform, "Text", text,
            Vector2.zero, fontSize, textColor, TextAlignmentOptions.Center);
        textObj.GetComponent<RectTransform>().sizeDelta = size;
        textObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        textObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        textObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        textObj.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1, -1);

        return obj;
    }

    static GameObject CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor)
    {
        var sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        var sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 1);
        sliderRect.anchorMax = new Vector2(0, 1);
        sliderRect.pivot = new Vector2(0, 1);
        sliderRect.anchoredPosition = pos;
        sliderRect.sizeDelta = size;

        var bgImg = sliderObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;
        var fillImg = fillObj.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = fillColor;

        var slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
        slider.fillRect = fillRect;
        slider.targetGraphic = bgImg;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;

        return sliderObj;
    }

    static GameObject CreateTMPText(Transform parent, string name, string text,
        Vector2 pos, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(300, 40);

        var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return obj;
    }

    static GameObject CreatePanel(Transform parent, string name, Color bgColor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var img = obj.AddComponent<UnityEngine.UI.Image>();
        img.color = bgColor;
        obj.SetActive(false);
        return obj;
    }

    static GameObject CreateButton(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, float fontSize)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var btn = obj.AddComponent<UnityEngine.UI.Button>();
        var img = obj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f);
        var textObj = CreateTMPText(obj.transform, "Text", text,
            Vector2.zero, fontSize, Color.white, TextAlignmentOptions.Center);
        textObj.GetComponent<RectTransform>().sizeDelta = size;
        textObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        textObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
        textObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        return obj;
    }

    static GameObject CreateSliderSetting(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, float minVal, float maxVal, float defaultVal)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var labelObj = CreateTMPText(obj.transform, "Label", label,
            new Vector2(0, 35), 16, new Color(0.8f, 0.7f, 0.6f), TextAlignmentOptions.Center);
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, 30);

        var sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(obj.transform, false);
        var sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.1f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.9f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0, -20);
        sliderRect.sizeDelta = new Vector2(size.x * 0.8f, 20);

        var bgImg = sliderObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.1f, 0.08f, 0.06f);
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;
        var fillImg = fillObj.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = new Color(1f, 0.6f, 0.15f);
        var handleObj = new GameObject("Handle Slide Area");
        handleObj.transform.SetParent(sliderObj.transform, false);
        var handleAreaRect = handleObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;
        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleObj.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 28);
        var handleImg = handle.AddComponent<UnityEngine.UI.Image>();
        handleImg.color = new Color(1f, 0.85f, 0.3f);
        
        var slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.minValue = minVal;
        slider.maxValue = maxVal;
        slider.value = defaultVal;
        slider.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        var colors = slider.colors;
        colors.normalColor = new Color(1f, 0.85f, 0.3f);
        colors.highlightedColor = new Color(1f, 0.7f, 0.2f);
        colors.pressedColor = new Color(1f, 0.5f, 0.1f);
        slider.colors = colors;

        var valueText = CreateTMPText(obj.transform, "ValueText", defaultVal.ToString("F1"),
            new Vector2(size.x * 0.45f, 35), 14, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Right);
        valueText.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 30);
        slider.onValueChanged.AddListener((v) => valueText.GetComponent<TMPro.TextMeshProUGUI>().text = v.ToString("F1"));

        return obj;
    }

    static GameObject CreateToggleSetting(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, bool defaultVal)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(obj.transform, false);
        var toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0, 0.5f);
        toggleRect.anchorMax = new Vector2(0, 0.5f);
        toggleRect.pivot = new Vector2(0, 0.5f);
        toggleRect.anchoredPosition = new Vector2(-size.x * 0.4f, 0);
        toggleRect.sizeDelta = new Vector2(50, 30);

        var bgImg = toggleObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.15f, 0.12f, 0.1f);
        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(toggleObj.transform, false);
        var checkRect = checkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(30, 30);
        var checkImg = checkObj.AddComponent<UnityEngine.UI.Image>();
        checkImg.color = new Color(1f, 0.85f, 0.3f);
        checkImg.enabled = defaultVal;

        var labelObj = CreateTMPText(obj.transform, "Label", label,
            new Vector2(20, 0), 18, new Color(0.9f, 0.85f, 0.8f), TextAlignmentOptions.MidlineLeft);
        labelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x - 80, 40);
        labelObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        labelObj.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
        labelObj.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);

        var toggle = toggleObj.AddComponent<UnityEngine.UI.Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = defaultVal;
        toggle.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        var colors = toggle.colors;
        colors.normalColor = new Color(0.15f, 0.12f, 0.1f);
        colors.highlightedColor = new Color(0.25f, 0.2f, 0.15f);
        colors.pressedColor = new Color(0.1f, 0.08f, 0.06f);
        colors.colorMultiplier = 1.2f;
        toggle.colors = colors;

        return obj;
    }

    static Material LitMat(Color color, float metallic, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    static void CreateEventSystem(GameObject root)
    {
        var esObj = new GameObject("EventSystem");
        esObj.transform.parent = root.transform;
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        Debug.Log("[PHANTOM EDGE] EventSystem created.");
    }

    static void CreatePostProcessing(GameObject root)
    {
        var ppObj = new GameObject("PostProcessing");
        ppObj.transform.parent = root.transform;
        var pp = ppObj.AddComponent<PostProcessingSetup>();
        pp.enableBloom = true;
        pp.bloomIntensity = 0.8f;
        pp.bloomThreshold = 1.0f;
        pp.enableColorGrading = true;
        pp.postExposure = 0.3f;
        pp.contrast = 15f;
        pp.saturation = 10f;
        pp.enableVignette = true;
        pp.vignetteIntensity = 0.35f;
        pp.enableChromaticAberration = true;
        pp.chromaticIntensity = 0.15f;
        pp.enableFilmGrain = true;
        pp.grainIntensity = 0.12f;
        pp.enableLensDistortion = true;
        pp.lensDistortion = -0.12f;
        Debug.Log("[PHANTOM EDGE] Post-processing setup complete.");
    }

    static void CreateEffectPrefabs()
    {
        string prefabsDir = "Assets/Resources/Effects";
        if (!Directory.Exists(prefabsDir)) Directory.CreateDirectory(prefabsDir);

        CreateHitFlashPrefab(prefabsDir);
        CreateDashParticles(prefabsDir);
        CreateSlideParticles(prefabsDir);
        CreateLandParticles(prefabsDir);
        CreateWallJumpParticles(prefabsDir);
        CreateDeathParticles(prefabsDir);
        CreateBloodMist(prefabsDir);
        CreateGibs(prefabsDir);
        CreateGrappleParticles(prefabsDir);
        CreateGrappleImpactParticles(prefabsDir);
        CreateGrappleTrailParticles(prefabsDir);

        AssetDatabase.Refresh();
        Debug.Log("[PHANTOM EDGE] Effect prefabs created in Resources/Effects.");
    }

    static void CreateHitFlashPrefab(string dir)
    {
        var go = new GameObject("HitFlash");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 0.8f, 1f), new Color(1f, 0.5f, 0.1f, 0f));
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 20) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(0f, -5f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.white * 10f);
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/HitFlash.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateDashParticles(string dir)
    {
        var go = new GameObject("DashParticles");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 2f;
        main.startSize = 0.2f;
        main.startColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.rateOverTime = 50f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.2f;
        shape.position = new Vector3(0, 0, -0.5f);
        shape.rotation = new Vector3(180, 0, 0);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.z = new ParticleSystem.MinMaxCurve(-5f, -10f);
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.cyan * 5f);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/DashParticles.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateSlideParticles(string dir)
    {
        var go = new GameObject("SlideParticles");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.2f;
        main.startSpeed = 1f;
        main.startSize = 0.1f;
        main.startColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        main.gravityModifier = 1f;
        var emission = ps.emission;
        emission.rateOverTime = 30f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(0.5f, 0.05f, 0.5f);
        shape.position = new Vector3(0, -0.9f, 0);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(-2f, 0f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/SlideParticles.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateLandParticles(string dir)
    {
        var go = new GameObject("LandParticles");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        main.gravityModifier = 2f;
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15, 25) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.3f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/LandParticles.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateWallJumpParticles(string dir)
    {
        var go = new GameObject("WallJumpParticles");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.25f;
        main.startSpeed = 5f;
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.8f, 0.3f, 1f);
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20, 30) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.1f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f) * 5f);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/WallJumpParticles.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateDeathParticles(string dir)
    {
        var go = new GameObject("DeathParticles");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.3f, 0.1f, 1f),
            new Color(1f, 0.1f, 0.05f, 0f)
        );
        main.gravityModifier = 1f;
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30, 50) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(-2f, 5f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.1f) * 3f);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/DeathParticles.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateBloodMist(string dir)
    {
        var go = new GameObject("BloodMist");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor = new Color(0.8f, 0.1f, 0.1f, 0.3f);
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 10) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2f)));
        var color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(
            new Color(0.8f, 0.1f, 0.1f, 0.3f),
            new Color(0.5f, 0.05f, 0.05f, 0f)
        );
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.renderQueue = 3000;
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/BloodMist.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateGibs(string dir)
    {
        var go = new GameObject("Gib");
        go.transform.localScale = Vector3.one * 0.2f;
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.parent = go.transform;
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = Vector3.one;
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.6f, 0.15f, 0.1f);
        cube.GetComponent<Renderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/Gib.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateGrappleParticles(string dir)
    {
        var go = new GameObject("GrappleParticles");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 0.3f;
        main.startSpeed = 0f;
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.5f, 0.1f, 0.8f);
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.rateOverTime = 20f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.radial = new ParticleSystem.MinMaxCurve(-1f, 1f);
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 5f);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/GrappleParticles.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateGrappleImpactParticles(string dir)
    {
        var go = new GameObject("GrappleImpactParticles");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.1f, 1f),
            new Color(1f, 0.3f, 0.05f, 0f)
        );
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20, 30) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 60f;
        shape.radius = 0.1f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 8f);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/GrappleImpactParticles.prefab");
        Object.DestroyImmediate(go);
    }

    static void CreateGrappleTrailParticles(string dir)
    {
        var go = new GameObject("GrappleTrailParticles");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 0.2f;
        main.startSpeed = 0f;
        main.startSize = 0.05f;
        main.startColor = new Color(1f, 0.5f, 0.1f, 0.6f);
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.rateOverTime = 30f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = 0.02f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.z = new ParticleSystem.MinMaxCurve(-2f, -5f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 3f);
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/GrappleTrailParticles.prefab");
        Object.DestroyImmediate(go);
    }
}
