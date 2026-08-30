using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DungeonEntranceSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/DungeonEntrance_Level01.unity";
    private const string ProjectileFolder = "Assets/Prefab/DungeonThrowItems";
    private const string Dungeon = "Assets/KayKit/Packs/KayKit - Dungeon Remastered Pack (for Unity)/Prefabs/";
    private const string Adventurers = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Characters/";
    private const string Skeletons = "Assets/KayKit/Characters/KayKit - Skeletons (for Unity)/Prefabs/Characters/";
    private const string SkeletonAccessories = "Assets/KayKit/Characters/KayKit - Skeletons (for Unity)/Prefabs/Accessories/";

    [MenuItem("Tools/Throwing3D/Create Dungeon Entrance Level 01")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefab");
        EnsureFolder(ProjectileFolder);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "DungeonEntrance_Level01";

        SetupLighting();

        var environmentRoot = new GameObject("Environment_DungeonEntrance");
        BuildFloor(environmentRoot.transform);
        BuildBackWall(environmentRoot.transform);
        BuildProps(environmentRoot.transform);

        var skeleton = SpawnCharacter("Enemy_Skeleton", Skeletons + "Skeleton_Warrior.prefab", new Vector3(-3.6f, 0f, 0f), 90f, "Skeleton");
        var adventurer = SpawnCharacter("Player_Adventurer", Adventurers + "Knight.prefab", new Vector3(3.6f, 0f, 0f), -90f, "Adventurer");
        var enemyHand = CreateMarker("EnemyThrowPoint", skeleton.transform, new Vector3(0.35f, 1.25f, 0f), "PlayerRight_Hand");
        var playerHand = CreateMarker("PlayerThrowPoint", adventurer.transform, new Vector3(-0.35f, 1.25f, 0f), "PlayerLeft_Hand");

        var playerProjectiles = new[]
        {
            CreateProjectilePrefab(Dungeon + "coin.prefab", "P_Coin"),
            CreateProjectilePrefab(Dungeon + "crate_small.prefab", "P_CrateSmall"),
            CreateProjectilePrefab(Dungeon + "barrel_small.prefab", "P_BarrelSmall")
        };
        var enemyProjectiles = new[]
        {
            CreateProjectilePrefab(SkeletonAccessories + "Skeleton_Axe.prefab", "P_SkeletonAxe"),
            CreateProjectilePrefab(SkeletonAccessories + "Skeleton_Mace.prefab", "P_SkeletonMace"),
            CreateProjectilePrefab(SkeletonAccessories + "Skeleton_Dagger.prefab", "P_SkeletonDagger"),
            CreateProjectilePrefab(SkeletonAccessories + "Skeleton_Blade.prefab", "P_SkeletonBlade")
        };

        var camera = CreateCamera();
        CreateWarmKeyLight();
        CreateBlueFillLight();

        var systems = new GameObject("GameSystems");
        var gameManager = systems.AddComponent<GameManager>();
        var windManager = systems.AddComponent<WindManager>();
        var throwManager = systems.AddComponent<ThrowManager>();
        var levelManager = systems.AddComponent<LevelManager>();
        systems.AddComponent<AdManager>();

        var ui = BuildUi(gameManager, windManager);

        SetObjectField(throwManager, "_humanHandSpawnPoint", playerHand.transform);
        SetObjectField(throwManager, "_zombieHandSpawnPoint", enemyHand.transform);
        SetObjectArrayField(throwManager, "_humanItemPrefabs", playerProjectiles);
        SetObjectArrayField(throwManager, "_zombieItemPrefabs", enemyProjectiles);
        SetObjectField(throwManager, "_powerBarUI", ui.powerBar);
        SetStringField(throwManager, "_playerTag", "Adventurer");
        SetStringField(throwManager, "_enemyTag", "Skeleton");
        SetStringField(throwManager, "_playerThrowPointTag", "PlayerLeft_Hand");
        SetStringField(throwManager, "_enemyThrowPointTag", "PlayerRight_Hand");
        SetFloatField(throwManager, "_minThrowPower", 2.4f);
        SetFloatField(throwManager, "_maxThrowPower", 4.6f);
        SetFloatField(throwManager, "_powerThrowBonus", 1.2f);
        SetFloatField(throwManager, "_fullChargeSeconds", 1.4f);
        SetFloatField(throwManager, "_lobAngleDegrees", 38f);
        SetFloatField(throwManager, "_windEffect", 1.8f);

        SetObjectField(levelManager, "_throwManager", throwManager);
        SetObjectField(gameManager, "_playerHpSlider", ui.playerHp);
        SetObjectField(gameManager, "_enemyHpSlider", ui.enemyHp);
        SetObjectField(gameManager, "_resultPanel", ui.resultPanel);
        SetObjectField(gameManager, "_resultText", ui.resultText);

        SetObjectField(windManager, "fillLeft", ui.windLeftFillRoot);
        SetObjectField(windManager, "fillRight", ui.windRightFillRoot);
        SetObjectField(windManager, "fillImageLeft", ui.windLeftFill);
        SetObjectField(windManager, "fillImageRight", ui.windRightFill);
        SetObjectField(windManager, "arrowLeft", ui.windLeftArrow);
        SetObjectField(windManager, "arrowRight", ui.windRightArrow);
        SetObjectField(windManager, "textWind", ui.windText);

        WireButtons(ui, gameManager);

        Selection.activeGameObject = systems;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        Debug.Log($"Created {ScenePath}. Open it and press Play.");
    }

    private static void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.17f, 0.15f, 0.22f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.08f, 0.07f, 0.12f);
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.skybox = null;
    }

    private static void BuildFloor(Transform root)
    {
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                SpawnPrefab(Dungeon + "floor_tile_large.prefab", root, new Vector3(x * 2f, 0f, z * 2f), Vector3.zero, "FloorTile");
            }
        }

        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "GameplayGroundCollider";
        ground.tag = "Ground";
        ground.transform.SetParent(root);
        ground.transform.position = new Vector3(0f, -0.08f, 0f);
        ground.transform.localScale = new Vector3(10.5f, 0.1f, 6.5f);
        Object.DestroyImmediate(ground.GetComponent<MeshRenderer>());
    }

    private static void BuildBackWall(Transform root)
    {
        for (int x = -2; x <= 2; x++)
            SpawnPrefab(Dungeon + "wall.prefab", root, new Vector3(x * 2f, 0f, 3f), new Vector3(0f, 180f, 0f), "BackWall");

        SpawnPrefab(Dungeon + "wall_doorway.prefab", root, new Vector3(0f, 0f, 3.05f), new Vector3(0f, 180f, 0f), "CenterDoorway");
        SpawnPrefab(Dungeon + "banner_patternA_yellow.prefab", root, new Vector3(0f, 1.8f, 2.9f), Vector3.zero, "Banner");
    }

    private static void BuildProps(Transform root)
    {
        SpawnPrefab(Dungeon + "pillar.prefab", root, new Vector3(-1.05f, 0f, 0f), Vector3.zero, "CenterPillar_Left");
        SpawnPrefab(Dungeon + "pillar.prefab", root, new Vector3(1.05f, 0f, 0f), Vector3.zero, "CenterPillar_Right");
        SpawnPrefab(Dungeon + "torch_lit.prefab", root, new Vector3(-3.7f, 1.2f, 2.65f), Vector3.zero, "Torch_Left");
        SpawnPrefab(Dungeon + "torch_lit.prefab", root, new Vector3(3.7f, 1.2f, 2.65f), Vector3.zero, "Torch_Right");
        SpawnPrefab(Dungeon + "crate_small.prefab", root, new Vector3(-0.35f, 0f, -0.45f), new Vector3(0f, 25f, 0f), "Obstacle_Crate");
        SpawnPrefab(Dungeon + "barrel_small.prefab", root, new Vector3(0.35f, 0f, -0.3f), new Vector3(0f, -15f, 0f), "Obstacle_Barrel");
        SpawnPrefab(Dungeon + "chest.prefab", root, new Vector3(0f, 0f, 2.1f), new Vector3(0f, 180f, 0f), "RewardChest");
        SpawnPrefab(Dungeon + "table_small_decorated_A.prefab", root, new Vector3(0f, 0f, -2.1f), Vector3.zero, "Decor_Table");
    }

    private static GameObject SpawnCharacter(string name, string path, Vector3 position, float yaw, string tag)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.tag = tag;
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateHitbox("BodyHitbox", go.transform, "Body", new Vector3(0f, 0.85f, 0f), new Vector3(0.45f, 0.85f, 0.45f), false);
        CreateHitbox("HeadHitbox", go.transform, "Head", new Vector3(0f, 1.65f, 0f), new Vector3(0.34f, 0.34f, 0.34f), true);
        return go;
    }

    private static GameObject CreateHitbox(string name, Transform parent, string tag, Vector3 localPosition, Vector3 size, bool sphere)
    {
        var hitbox = new GameObject(name);
        hitbox.tag = tag;
        hitbox.transform.SetParent(parent, false);
        hitbox.transform.localPosition = localPosition;

        if (sphere)
        {
            var col = hitbox.AddComponent<SphereCollider>();
            col.radius = size.x;
        }
        else
        {
            var col = hitbox.AddComponent<CapsuleCollider>();
            col.center = Vector3.zero;
            col.radius = size.x;
            col.height = size.y * 2f;
        }
        return hitbox;
    }

    private static GameObject CreateMarker(string name, Transform parent, Vector3 localPosition, string tag = "Untagged")
    {
        var marker = new GameObject(name);
        marker.tag = tag;
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        return marker;
    }

    private static GameObject CreateProjectilePrefab(string sourcePath, string prefabName)
    {
        var targetPath = $"{ProjectileFolder}/{prefabName}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
        if (existing != null) return existing;

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        var root = new GameObject(prefabName);
        root.tag = "Item";

        if (source != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
        }

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.35f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(0.42f, 0.42f, 0.42f);
        root.AddComponent<ProjectileItem>();

        var saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static Camera CreateCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 3.1f, -7.5f);
        go.transform.rotation = Quaternion.Euler(21f, 0f, 0f);

        var camera = go.AddComponent<Camera>();
        camera.fieldOfView = 38f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.045f, 0.08f);
        go.AddComponent<AudioListener>();
        return camera;
    }

    private static void CreateWarmKeyLight()
    {
        var go = new GameObject("Warm Key Light");
        go.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.78f, 0.48f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
    }

    private static void CreateBlueFillLight()
    {
        var go = new GameObject("Blue Fill Light");
        go.transform.position = new Vector3(0f, 3f, -3.5f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.35f, 0.45f, 1f);
        light.intensity = 1.2f;
        light.range = 7f;
    }

    private static GameObject SpawnPrefab(string path, Transform parent, Vector3 position, Vector3 euler, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);
        return go;
    }

    private static UiRefs BuildUi(GameManager gameManager, WindManager windManager)
    {
        var uiManagerGo = new GameObject("UIManager");
        var uiManager = uiManagerGo.AddComponent<UIManager>();

        var mainMenu = CreateCanvas("MainMenuCanvas");
        var gameplay = CreateCanvas("GameplayCanvas");
        var result = CreateCanvas("ResultCanvas");
        result.gameObject.SetActive(false);
        gameplay.gameObject.SetActive(false);

        CreateEventSystem();

        var title = CreateText("Title", mainMenu.transform, "Dungeon Toss", 56, new Vector2(0f, 120f), new Vector2(520f, 80f));
        title.color = new Color(1f, 0.86f, 0.45f);
        var subtitle = CreateText("Subtitle", mainMenu.transform, "Adventurer vs Skeleton", 26, new Vector2(0f, 62f), new Vector2(520f, 50f));
        subtitle.color = new Color(0.8f, 0.75f, 0.95f);
        var playButton = CreateButton("PlayButton", mainMenu.transform, "Start Duel", new Vector2(0f, -50f), new Vector2(250f, 70f));
        UnityEventTools.AddPersistentListener(playButton.onClick, uiManager.PlayCurrentLevel);

        var playerHp = CreateSlider("PlayerHP", gameplay.transform, new Vector2(180f, -40f), new Vector2(260f, 24f));
        var enemyHp = CreateSlider("EnemyHP", gameplay.transform, new Vector2(-180f, -40f), new Vector2(260f, 24f));
        var power = CreatePowerBar(gameplay.transform);
        var wind = CreateWindUi(gameplay.transform);

        var resultPanel = CreatePanel("ResultPanel", result.transform, new Vector2(0f, 0f), new Vector2(460f, 310f));
        var resultText = CreateText("ResultText", resultPanel.transform, "Victory!", 42, new Vector2(0f, 88f), new Vector2(400f, 60f));
        resultText.color = new Color(1f, 0.86f, 0.45f);
        var retry = CreateButton("RetryButton", resultPanel.transform, "Retry", new Vector2(-112f, -32f), new Vector2(180f, 56f));
        var next = CreateButton("NextButton", resultPanel.transform, "Next", new Vector2(112f, -32f), new Vector2(180f, 56f));
        var revive = CreateButton("ReviveAdButton", resultPanel.transform, "Ad Revive", new Vector2(-112f, -106f), new Vector2(180f, 50f));
        var doubleCoins = CreateButton("DoubleCoinsAdButton", resultPanel.transform, "Ad x2 Gold", new Vector2(112f, -106f), new Vector2(180f, 50f));

        SetObjectField(uiManager, "_mainMenuCanvas", mainMenu);
        SetObjectField(uiManager, "_gameplayCanvas", gameplay);
        SetObjectField(uiManager, "_resultCanvas", result);
        SetCanvasList(uiManager, new List<Canvas> { mainMenu, gameplay, result });

        return new UiRefs
        {
            uiManager = uiManager,
            playerHp = playerHp,
            enemyHp = enemyHp,
            powerBar = power,
            resultPanel = resultPanel,
            resultText = resultText,
            retryButton = retry,
            nextButton = next,
            reviveButton = revive,
            doubleCoinsButton = doubleCoins,
            windLeftFillRoot = wind.leftRoot,
            windRightFillRoot = wind.rightRoot,
            windLeftFill = wind.leftFill,
            windRightFill = wind.rightFill,
            windLeftArrow = wind.leftArrow,
            windRightArrow = wind.rightArrow,
            windText = wind.label
        };
    }

    private static void WireButtons(UiRefs ui, GameManager gameManager)
    {
        UnityEventTools.AddPersistentListener(ui.retryButton.onClick, gameManager.OnRetryPressed);
        UnityEventTools.AddPersistentListener(ui.nextButton.onClick, gameManager.OnNextPressed);
        UnityEventTools.AddPersistentListener(ui.reviveButton.onClick, gameManager.ReviveWithAd);
        UnityEventTools.AddPersistentListener(ui.doubleCoinsButton.onClick, gameManager.DoubleCoinsWithAd);
    }

    private static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = CreatePanel(name, parent, anchoredPosition, sizeDelta);
        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.32f, 0.20f, 0.38f);
        colors.highlightedColor = new Color(0.45f, 0.28f, 0.55f);
        colors.pressedColor = new Color(0.22f, 0.14f, 0.28f);
        button.colors = colors;
        CreateText("Label", go.transform, label, 24, Vector2.zero, sizeDelta);
        return button;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        var image = go.AddComponent<Image>();
        image.color = new Color(0.08f, 0.06f, 0.12f, 0.86f);
        return go;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var bg = CreatePanel("Background", root.transform, Vector2.zero, sizeDelta);
        bg.GetComponent<Image>().color = new Color(0.18f, 0.12f, 0.18f, 0.9f);
        var fill = CreatePanel("Fill", root.transform, Vector2.zero, sizeDelta);
        fill.GetComponent<Image>().color = new Color(0.85f, 0.18f, 0.18f, 0.95f);

        var slider = root.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 5f;
        slider.value = 5f;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fill.GetComponent<Image>();
        return slider;
    }

    private static PowerBarUI CreatePowerBar(Transform parent)
    {
        var go = new GameObject("PowerBarUI");
        go.transform.SetParent(parent, false);
        var power = go.AddComponent<PowerBarUI>();

        var barRoot = CreatePanel("HumanPowerBar", go.transform, new Vector2(0f, 120f), new Vector2(340f, 30f));
        barRoot.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        barRoot.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        var fill = CreatePanel("HumanPowerFill", barRoot.transform, Vector2.zero, new Vector2(340f, 30f));
        var fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(1f, 0.68f, 0.14f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;

        var zombieRoot = CreatePanel("ZombiePowerBar", go.transform, new Vector2(0f, 120f), new Vector2(340f, 30f));
        zombieRoot.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        zombieRoot.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        zombieRoot.SetActive(false);
        var zombieFill = CreatePanel("ZombiePowerFill", zombieRoot.transform, Vector2.zero, new Vector2(340f, 30f));
        var zombieFillImage = zombieFill.GetComponent<Image>();
        zombieFillImage.color = new Color(0.45f, 0.7f, 1f, 1f);
        zombieFillImage.type = Image.Type.Filled;
        zombieFillImage.fillMethod = Image.FillMethod.Horizontal;

        var humanWarn = new GameObject("HumanTimeWarning");
        humanWarn.transform.SetParent(go.transform, false);
        humanWarn.SetActive(false);
        var zombieWarn = new GameObject("ZombieTimeWarning");
        zombieWarn.transform.SetParent(go.transform, false);
        zombieWarn.SetActive(false);

        SetObjectField(power, "_humanPowerBar", barRoot);
        SetObjectField(power, "_zombiePowerBar", zombieRoot);
        SetObjectField(power, "_humanTimeWarning", humanWarn);
        SetObjectField(power, "_zombieTimeWarning", zombieWarn);
        SetObjectField(power, "_humanPowerBarImage", fillImage);
        SetObjectField(power, "_zombiePowerBarImage", zombieFillImage);
        return power;
    }

    private static WindRefs CreateWindUi(Transform parent)
    {
        var root = CreatePanel("WindPanel", parent, new Vector2(0f, -84f), new Vector2(310f, 72f));
        root.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1f);
        root.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
        var label = CreateText("WindLabel", root.transform, "WIND", 20, new Vector2(0f, 20f), new Vector2(160f, 24f));

        var leftArrow = CreateText("ArrowLeft", root.transform, "<", 30, new Vector2(-116f, -12f), new Vector2(40f, 36f)).gameObject;
        var rightArrow = CreateText("ArrowRight", root.transform, ">", 30, new Vector2(116f, -12f), new Vector2(40f, 36f)).gameObject;
        var leftRoot = CreatePanel("LeftWindFillRoot", root.transform, new Vector2(-48f, -12f), new Vector2(92f, 18f));
        var rightRoot = CreatePanel("RightWindFillRoot", root.transform, new Vector2(48f, -12f), new Vector2(92f, 18f));
        var leftFill = leftRoot.GetComponent<Image>();
        var rightFill = rightRoot.GetComponent<Image>();
        leftFill.color = new Color(0.45f, 0.65f, 1f, 0.9f);
        rightFill.color = new Color(0.45f, 0.65f, 1f, 0.9f);
        leftFill.type = Image.Type.Filled;
        rightFill.type = Image.Type.Filled;
        leftFill.fillMethod = Image.FillMethod.Horizontal;
        rightFill.fillMethod = Image.FillMethod.Horizontal;

        return new WindRefs
        {
            leftRoot = leftRoot,
            rightRoot = rightRoot,
            leftFill = leftFill,
            rightFill = rightFill,
            leftArrow = leftArrow,
            rightArrow = rightArrow,
            label = label
        };
    }

    private static void SetObjectField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArrayField(Object target, string fieldName, GameObject[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetStringField(Object target, string fieldName, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloatField(Object target, string fieldName, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetCanvasList(UIManager uiManager, List<Canvas> canvases)
    {
        var so = new SerializedObject(uiManager);
        var prop = so.FindProperty("_allCanvases");
        prop.arraySize = canvases.Count;
        for (int i = 0; i < canvases.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = canvases[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == scenePath))
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        var name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private struct UiRefs
    {
        public UIManager uiManager;
        public Slider playerHp;
        public Slider enemyHp;
        public PowerBarUI powerBar;
        public GameObject resultPanel;
        public TMP_Text resultText;
        public Button retryButton;
        public Button nextButton;
        public Button reviveButton;
        public Button doubleCoinsButton;
        public GameObject windLeftFillRoot;
        public GameObject windRightFillRoot;
        public Image windLeftFill;
        public Image windRightFill;
        public GameObject windLeftArrow;
        public GameObject windRightArrow;
        public TMP_Text windText;
    }

    private struct WindRefs
    {
        public GameObject leftRoot;
        public GameObject rightRoot;
        public Image leftFill;
        public Image rightFill;
        public GameObject leftArrow;
        public GameObject rightArrow;
        public TMP_Text label;
    }
}
