using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace BearPi.TiltMaze.Editor
{
    public static class TiltMazeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TiltMaze.unity";
        private const string MaterialsPath = "Assets/Art/Materials";
        private const float Cell = 1.75f;

        // S is the spawn, G is the goal and a space marks the drop at the end of a dead end.
        private static readonly string[] Map =
        {
            "###########",
            "#...#....G#",
            "#.###.###.#",
            "#.....#.WW#",
            "###.###D###",
            "#.IITPP.#.#",
            "#.#######.#",
            "#S........ ",
            "###########"
        };

        [MenuItem("Tilt Maze/Rebuild Prototype Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(MaterialsPath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var baseMaterial = Surface("Chassis", "#172832", 0.18f, 0.48f);
            var floor = Surface("Slate", "#536F7A", 0.05f, 0.36f);
            var floorAlt = Surface("Slate Light", "#577582", 0.05f, 0.36f);
            var wall = Surface("Wall", "#293F4C", 0.1f, 0.33f);
            var cap = Surface("Wall Cap", "#435D6D", 0.14f, 0.5f);
            var gold = Surface("Amber", "#FFAF51", 0.22f, 0.64f);
            var mint = Surface("Mint", "#64F4BB", 0.22f, 0.66f);
            mint.EnableKeyword("_EMISSION");
            mint.SetColor("_EmissionColor", ColorOf("#45D89B") * 0.32f);
            var voidMaterial = Surface("Backdrop", "#091625", 0f, 0.2f);
            var ice = Surface("Ice", "#78D8EC", 0.18f, 0.84f);
            var water = Surface("Water", "#277DCA", 0.12f, 0.79f);
            var wet = Surface("Wet", "#906CBC", 0.1f, 0.8f);
            var coral = Surface("Hazard", "#FA6F53", 0.12f, 0.54f);
            var iceContact = Contact("Ice Contact", 0.025f, 0.03f);
            var wetContact = Contact("Wet Contact", 0.17f, 0.06f);
            var grip = Contact("Board Contact", 0.54f, 0.04f);
            var ballContact = Contact("Ball Contact", 0.46f, 0.16f);

            var board = new GameObject("01  |  Tilt platform");
            var boardBody = board.AddComponent<Rigidbody>();
            boardBody.isKinematic = true;
            boardBody.interpolation = RigidbodyInterpolation.Interpolate;
            boardBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var tilt = board.AddComponent<MazeTiltController>();
            var deck = Child(board.transform, "Deck");
            var walls = Child(board.transform, "Walls");
            var details = Child(board.transform, "Markers");
            var span = new Vector3(Map[0].Length * Cell, 0.34f, Map.Length * Cell);
            Primitive("Chassis", PrimitiveType.Cube, deck, new Vector3(0f, -0.41f, 0f), span, baseMaterial);
            Primitive("Lower rim", PrimitiveType.Cube, deck, new Vector3(0f, -0.61f, 0f),
                new Vector3(span.x - 0.14f, 0.09f, span.z - 0.14f), cap);

            Transform spawn = null;
            GoalTrigger goal = null;
            MechanismActuator gate = null;
            MechanismActuator sweeper = null;
            for (int row = 0; row < Map.Length; row++)
            for (int column = 0; column < Map[row].Length; column++)
            {
                char tile = Map[row][column];
                var point = CellPosition(column, row);
                if (tile == ' ')
                {
                    Primitive("Drop warning", PrimitiveType.Cube, details,
                        point + new Vector3(-Cell * 0.54f, 0.012f, 0f),
                        new Vector3(0.075f, 0.024f, Cell - 0.13f), gold);
                    continue;
                }

                if (tile == '#')
                {
                    var block = Primitive($"Wall {column:00}-{row:00}", PrimitiveType.Cube, walls,
                        point + Vector3.up * 0.44f, new Vector3(Cell, 0.88f, Cell), wall, true);
                    block.GetComponent<Collider>().sharedMaterial = grip;
                    Primitive("Cap", PrimitiveType.Cube, block.transform.parent,
                        point + Vector3.up * 0.88f, new Vector3(Cell - 0.055f, 0.07f, Cell - 0.055f), cap);
                    continue;
                }

                var kind = tile switch
                {
                    'I' => TerrainKind.Ice,
                    'W' => TerrainKind.Water,
                    'P' => TerrainKind.Wet,
                    _ => TerrainKind.Normal
                };
                var surface = kind switch
                {
                    TerrainKind.Ice => ice,
                    TerrainKind.Water => water,
                    TerrainKind.Wet => wet,
                    _ => (row + column) % 2 == 0 ? floor : floorAlt
                };
                var plate = Primitive($"Tile {column:00}-{row:00}", PrimitiveType.Cube, deck,
                    point + Vector3.down * 0.12f, new Vector3(Cell, 0.24f, Cell),
                    surface, true);
                plate.GetComponent<Collider>().sharedMaterial = kind switch
                {
                    TerrainKind.Ice => iceContact,
                    TerrainKind.Wet => wetContact,
                    _ => grip
                };
                var zone = plate.AddComponent<TerrainZone>();
                Assign(zone, "kind", kind, "drag", kind == TerrainKind.Water ? 2.8f : 0f,
                    "lateralSlip", kind == TerrainKind.Wet ? 0.75f : 0f);

                if (tile == 'D')
                {
                    gate = Actuator("06  |  Lift gate", MechanismKind.LiftGate,
                        point + Vector3.up * 0.42f, Vector3.up * 1.48f,
                        new Vector3(1.69f, 0.84f, 0.25f), gold, 6.3f, 0f);
                    Primitive("Gate jamb", PrimitiveType.Cube, details,
                        point + new Vector3(-0.89f, 0.63f, 0f), new Vector3(0.13f, 1.28f, 0.44f), gold);
                    Primitive("Gate jamb", PrimitiveType.Cube, details,
                        point + new Vector3(0.89f, 0.63f, 0f), new Vector3(0.13f, 1.28f, 0.44f), gold);
                }
                if (tile == 'T')
                {
                    sweeper = Actuator("07  |  Sweeper", MechanismKind.Sweeper,
                        point + Vector3.up * 0.24f, Vector3.zero,
                        new Vector3(0.34f, 0.48f, 0.40f), coral, 3.4f, 15f);
                    Primitive("Sweep track", PrimitiveType.Cube, details, point + Vector3.up * 0.012f,
                        new Vector3(0.49f, 0.023f, 1.67f), baseMaterial);
                }

                if (tile == 'S')
                {
                    spawn = Child(board.transform, "Spawn");
                    spawn.localPosition = point + Vector3.up * 0.36f;
                    Primitive("Start marker", PrimitiveType.Cylinder, details, point + Vector3.up * 0.007f,
                        new Vector3(0.86f, 0.007f, 0.86f), gold);
                }
                if (tile == 'G')
                {
                    var target = Child(details, "Goal");
                    target.localPosition = point;
                    var collider = target.gameObject.AddComponent<BoxCollider>();
                    collider.center = Vector3.up * 0.35f;
                    collider.size = new Vector3(1.04f, 0.7f, 1.04f);
                    collider.isTrigger = true;
                    goal = target.gameObject.AddComponent<GoalTrigger>();
                    var goalRing = Primitive("Goal marker", PrimitiveType.Cylinder, target, Vector3.up * 0.012f,
                        new Vector3(1.14f, 0.012f, 1.14f), mint);
                    Primitive("Goal inset", PrimitiveType.Cylinder, target, Vector3.up * 0.019f,
                        new Vector3(0.78f, 0.013f, 0.78f), baseMaterial);
                    Primitive("Goal center", PrimitiveType.Cylinder, target, Vector3.up * 0.023f,
                        new Vector3(0.33f, 0.014f, 0.33f), mint);
                    Assign(target.gameObject.AddComponent<GoalBeacon>(), "ring", goalRing.transform);
                }
            }

            var ballObject = new GameObject("02  |  Ball");
            ballObject.transform.position = spawn.position;
            var shell = Primitive("Sphere shell", PrimitiveType.Sphere, ballObject.transform, Vector3.zero,
                Vector3.one * 0.56f, gold);
            Primitive("Dark face", PrimitiveType.Sphere, shell.transform, Vector3.up * 0.465f,
                new Vector3(0.34f, 0.09f, 0.34f), baseMaterial);
            Primitive("Dark side", PrimitiveType.Sphere, shell.transform, Vector3.forward * 0.465f,
                new Vector3(0.34f, 0.34f, 0.09f), baseMaterial);
            var sphere = ballObject.AddComponent<SphereCollider>();
            sphere.radius = 0.28f;
            sphere.sharedMaterial = ballContact;
            var ballBody = ballObject.AddComponent<Rigidbody>();
            ballBody.isKinematic = true;
            ballBody.interpolation = RigidbodyInterpolation.Interpolate;
            ballBody.linearDamping = 0.32f;
            ballBody.angularDamping = 0.12f;
            ballBody.maxAngularVelocity = 36f;
            ballBody.solverIterations = 12;
            ballBody.solverVelocityIterations = 5;
            var ball = ballObject.AddComponent<BallController>();

            var cameraObject = new GameObject("03  |  Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorOf("#0D1724");
            camera.orthographic = true;
            camera.orthographicSize = 10.2f;
            cameraObject.transform.position = new Vector3(0f, 20f, -11f);
            cameraObject.transform.LookAt(Vector3.zero);
            cameraObject.AddComponent<AudioListener>();

            var studio = new GameObject("04  |  Studio");
            var key = Child(studio.transform, "Key light").gameObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.02f;
            key.color = ColorOf("#FFF1DE");
            key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(51f, -32f, 0f);
            var fill = Child(studio.transform, "Fill light").gameObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.30f;
            fill.color = ColorOf("#9AD8FF");
            fill.transform.rotation = Quaternion.Euler(32f, 148f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ColorOf("#7B909D");
            Primitive("Backdrop", PrimitiveType.Cube, studio.transform, Vector3.down * 5.8f,
                new Vector3(170f, 0.1f, 170f), voidMaterial);

            var session = new GameObject("05  |  Game systems");
            var keyboard = session.AddComponent<KeyboardInputSource>();
            session.AddComponent<MockInputSource>();
            var level = board.AddComponent<LevelDefinition>();
            var mechanisms = board.AddComponent<MechanismManager>();
            Assign(mechanisms, "tilt", tilt, "ball", ball);
            AssignList(mechanisms, "actuators", gate, sweeper);
            Assign(level, "tilt", tilt, "spawn", spawn, "goal", goal, "mechanisms", mechanisms);
            var levels = session.AddComponent<LevelManager>();
            Assign(levels, "ball", ball);
            AssignList(levels, "levels", level);
            var cameras = cameraObject.AddComponent<CameraManager>();
            Assign(cameras, "levels", levels);
            var feedback = session.AddComponent<GameFeedback>();
            var audio = session.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            var local = session.AddComponent<LocalFeedbackSink>();
            AssignList(feedback, "sinks", local);
            var terrain = ballObject.AddComponent<TerrainController>();
            Assign(terrain, "levels", levels);
            var skills = ballObject.AddComponent<SkillManager>();
            var shield = CreateShield(ballObject.transform);
            Assign(skills, "levels", levels, "feedback", feedback, "shieldVisual", shield);
            var damage = ballObject.AddComponent<BallDamageSystem>();
            Assign(damage, "feedback", feedback);
            var game = session.AddComponent<GameManager>();
            Assign(game, "inputSource", keyboard, "levels", levels, "cameras", cameras,
                "terrain", terrain, "damage", damage, "skills", skills, "feedback", feedback);

            var interfaceObject = new GameObject("08  |  Interface");
            var document = interfaceObject.AddComponent<UIDocument>();
            document.panelSettings = CreatePanel();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/TiltMaze.uxml");
            var hud = interfaceObject.AddComponent<MazeHud>();
            Assign(hud, "game", game, "levels", levels, "cameras", cameras, "bindings", keyboard,
                "damage", damage, "skills", skills, "terrain", terrain);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Tilt Maze scene saved to {ScenePath}.");
        }

        private static PanelSettings CreatePanel()
        {
            const string path = "Assets/UI/Panel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, path);
            }

            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/Runtime.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1440, 810);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.45f;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        private static GameObject CreateShield(Transform ball)
        {
            var shield = Child(ball, "Shield field");
            var material = Surface("Shield Field", "#84F4D8", 0f, 0.6f);
            for (int i = 0; i < 3; i++)
            {
                var ring = Child(shield, "Shield ring");
                ring.localRotation = Quaternion.Euler(i * 60f, i * 42f, 0f);
                var line = ring.gameObject.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 48;
                line.widthMultiplier = 0.022f;
                for (int j = 0; j < line.positionCount; j++)
                {
                    float angle = j * Mathf.PI * 2f / line.positionCount;
                    line.SetPosition(j, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.37f);
                }
            }
            shield.gameObject.SetActive(false);
            return shield.gameObject;
        }

        private static MechanismActuator Actuator(string name, MechanismKind kind, Vector3 anchor,
            Vector3 startOffset, Vector3 scale, Material material, float period, float damage)
        {
            var item = new GameObject(name);
            item.transform.position = anchor + startOffset;
            Primitive("Body", PrimitiveType.Cube, item.transform, Vector3.zero, scale, material, true);
            var body = item.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var actuator = item.AddComponent<MechanismActuator>();
            Assign(actuator, "kind", kind, "boardAnchor", anchor, "period", period, "trapDamage", damage);
            return actuator;
        }

        private static Vector3 CellPosition(int column, int row) =>
            new((column - (Map[0].Length - 1) * 0.5f) * Cell, 0f,
                ((Map.Length - 1) * 0.5f - row) * Cell);

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static GameObject Primitive(string name, PrimitiveType shape, Transform parent,
            Vector3 position, Vector3 size, Material material, bool collision = false)
        {
            var item = GameObject.CreatePrimitive(shape);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = size;
            item.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }

        private static Color ColorOf(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        private static Material Surface(string name, string hex, float metallic, float gloss)
        {
            var path = $"{MaterialsPath}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = ColorOf(hex);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", gloss);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PhysicsMaterial Contact(string name, float friction, float bounce)
        {
            var path = $"{MaterialsPath}/{name}.physicMaterial";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(material, path);
            }

            material.dynamicFriction = friction;
            material.staticFriction = friction;
            material.bounciness = bounce;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignList(Component component, string field, params Object[] values)
        {
            var serialized = new SerializedObject(component);
            var list = serialized.FindProperty(field);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(Component component, params object[] fields)
        {
            var serialized = new SerializedObject(component);
            for (int i = 0; i < fields.Length; i += 2)
                {
                var property = serialized.FindProperty((string)fields[i]);
                switch (fields[i + 1])
                {
                    case float number: property.floatValue = number; break;
                    case Vector3 vector: property.vector3Value = vector; break;
                    case System.Enum choice: property.enumValueIndex = System.Convert.ToInt32(choice); break;
                    default: property.objectReferenceValue = (Object)fields[i + 1]; break;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
