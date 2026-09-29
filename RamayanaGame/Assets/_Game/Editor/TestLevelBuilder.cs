using System;
using System.Collections.Generic;
using System.IO;
using Ramayana.CameraSystem;
using Ramayana.Combat;
using Ramayana.Core;
using Ramayana.Data;
using Ramayana.Gameplay;
using Ramayana.Player;
using Ramayana.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ramayana.EditorTools
{
    // Builds a placeholder-art training level to test movement, bow and sequential tasks.
    static class TestLevelBuilder
    {
        const string Root = "Assets/_Game";
        const string ScenePath = Root + "/Scenes/TestLevel.unity";

        static readonly Color Sky = new(0.98f, 0.86f, 0.62f);
        static readonly Color Earth = new(0.55f, 0.38f, 0.22f);
        static readonly Color Palace = new(0.93f, 0.72f, 0.5f);
        static readonly Color RamaBlue = new(0.18f, 0.32f, 0.72f);
        static readonly Color Gold = new(0.95f, 0.75f, 0.2f);
        static readonly Color TargetRed = new(0.8f, 0.15f, 0.12f);

        [MenuItem("Ramayana/Build Test Level")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var dir in new[] { "Art/Placeholder", "Data/Characters", "Data/Chapters", "Prefabs", "Scenes", "Settings" })
                ProjectSetup.EnsureFolder($"{Root}/{dir}");

            // Must come before loading assets: opening a new scene unloads unreferenced assets.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite square = MakeSprite("Square", 32, circle: false);
            Sprite circle = MakeSprite("Circle", 64, circle: true);
            var noFriction = LoadOrCreate($"{Root}/Settings/NoFriction.physicsMaterial2D",
                () => new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f });

            var form = LoadOrCreate($"{Root}/Data/Characters/Rama_YoungPrince.asset", () =>
            {
                var f = ScriptableObject.CreateInstance<CharacterForm>();
                f.characterId = "rama";
                f.formName = "Young Prince";
                return f;
            });

            var chapter = LoadOrCreate($"{Root}/Data/Chapters/Test_TrainingGround.asset", () =>
            {
                var c = ScriptableObject.CreateInstance<ChapterData>();
                c.chapterId = "test_training";
                c.act = 1;
                c.chapterNumber = 0;
                c.title = "Training Ground (Test)";
                c.playableForm = form;
                c.tasks = new List<ChapterTask>
                {
                    new() { id = "go_to_range", description = "Walk to the archery range", type = TaskType.ReachPoint },
                    new() { id = "hit_targets", description = "Hit the targets with your bow (jump for the high ones)", type = TaskType.HitTargets, requiredCount = 5 },
                    new() { id = "reach_gate", description = "Race your brothers to the river gate", type = TaskType.ReachPoint },
                };
                return c;
            });

            var arrowPrefab = BuildArrowPrefab(square);

            var level = new GameObject("Level").transform;
            BuildEnvironment(level, square);
            var player = BuildPlayer(square, circle, noFriction, form, arrowPrefab);
            BuildTargets(level, circle);
            BuildTriggers(level, square);
            BuildCameraAndLight(player);
            BuildManagers(chapter);
            BuildUI(circle);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Ramayana] Test level built: " + ScenePath);
        }

        static void BuildEnvironment(Transform parent, Sprite square)
        {
            // Ground with two gaps to jump.
            Block("Ground_A", square, new Vector2(7.5f, -1f), new Vector2(35f, 2f), Earth, parent);
            Block("Ground_B", square, new Vector2(36.5f, -1f), new Vector2(17f, 2f), Earth, parent);
            Block("Ground_C", square, new Vector2(62f, -1f), new Vector2(28f, 2f), Earth, parent);
            Block("Ledge", square, new Vector2(46.5f, 1.5f), new Vector2(2f, 0.4f), Earth, parent);

            // Invisible bounds.
            Block("Wall_Left", square, new Vector2(-10.5f, 5f), new Vector2(1f, 20f), Color.clear, parent);
            Block("Wall_Right", square, new Vector2(76.5f, 5f), new Vector2(1f, 20f), Color.clear, parent);

            // Background palace silhouettes (no colliders).
            for (int i = 0; i < 8; i++)
            {
                float x = -6f + i * 10f;
                var tower = Block($"BG_Tower_{i}", square, new Vector2(x, 3f + (i % 3)), new Vector2(3f, 6f + 2 * (i % 3)), Palace, parent, collider: false);
                tower.GetComponent<SpriteRenderer>().sortingOrder = -10;
            }

            // River gate at the end.
            Block("Gate_PillarL", square, new Vector2(68f, 2f), new Vector2(0.6f, 4f), Gold, parent, collider: false);
            Block("Gate_PillarR", square, new Vector2(72f, 2f), new Vector2(0.6f, 4f), Gold, parent, collider: false);
            Block("Gate_Top", square, new Vector2(70f, 4.2f), new Vector2(5.2f, 0.5f), Gold, parent, collider: false);
        }

        static PlayerController2D BuildPlayer(Sprite square, Sprite circle, PhysicsMaterial2D mat, CharacterForm form, Arrow arrowPrefab)
        {
            var go = new GameObject("Rama");
            go.transform.position = new Vector3(0f, 1f, 0f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.8f, 1.8f);
            col.sharedMaterial = mat;

            var body = Block("Body", square, Vector2.zero, new Vector2(0.8f, 1.4f), RamaBlue, go.transform, collider: false);
            body.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            var head = Block("Head", circle, Vector2.zero, new Vector2(0.6f, 0.6f), RamaBlue, go.transform, collider: false);
            head.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            var crown = Block("Crown", square, Vector2.zero, new Vector2(0.35f, 0.18f), Gold, go.transform, collider: false);
            crown.transform.localPosition = new Vector3(0.12f, 0.98f, 0f);
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder = 10;

            var bowPoint = new GameObject("BowPoint").transform;
            bowPoint.SetParent(go.transform, false);
            bowPoint.localPosition = new Vector3(0.55f, 0.2f, 0f);

            var pc = go.AddComponent<PlayerController2D>();
            SetField(pc, "form", form);
            SetField(pc, "arrowPrefab", arrowPrefab);
            SetField(pc, "bowPoint", bowPoint);
            go.AddComponent<FallRespawn>();
            return pc;
        }

        static void BuildTargets(Transform parent, Sprite circle)
        {
            var positions = new[] { new Vector2(17f, 1.2f), new Vector2(19.5f, 1.2f), new Vector2(22f, 1.2f), new Vector2(20.5f, 3.6f), new Vector2(23.5f, 3.6f) };
            for (int i = 0; i < positions.Length; i++)
            {
                var t = Block($"Target_{i + 1}", circle, positions[i], new Vector2(0.8f, 0.8f), TargetRed, parent, collider: false);
                var cc = t.AddComponent<CircleCollider2D>();
                cc.isTrigger = true;
                var center = Block("Center", circle, Vector2.zero, new Vector2(0.4f, 0.4f), Color.white, t.transform, collider: false);
                center.transform.localPosition = Vector3.zero;
                center.GetComponent<SpriteRenderer>().sortingOrder = 1;
                var target = t.AddComponent<ArcheryTarget>();
                SetField(target, "sprite", t.GetComponent<SpriteRenderer>());
            }
        }

        static void BuildTriggers(Transform parent, Sprite square)
        {
            Trigger("Trigger_Range", "go_to_range", new Vector2(12f, 2f), new Vector2(2f, 4f), parent, square);
            Trigger("Trigger_Gate", "reach_gate", new Vector2(70f, 2f), new Vector2(3f, 4f), parent, square);
        }

        static void Trigger(string name, string taskId, Vector2 pos, Vector2 size, Transform parent, Sprite square)
        {
            var go = Block(name, square, pos, size, new Color(1f, 1f, 1f, 0.15f), parent, collider: false);
            go.GetComponent<SpriteRenderer>().sortingOrder = -5;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            SetField(go.AddComponent<TaskTrigger>(), "taskId", taskId);
        }

        static void BuildCameraAndLight(PlayerController2D player)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 2f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Sky;
            camGo.AddComponent<AudioListener>();
            SetField(camGo.AddComponent<CameraFollow2D>(), "target", player);

            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        static void BuildManagers(ChapterData chapter)
        {
            var go = new GameObject("ChapterManager");
            SetField(go.AddComponent<ChapterManager>(), "chapter", chapter);
        }

        static void BuildUI(Sprite circle)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Task banner.
            var banner = UIRect("TaskBanner", root, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1500f, 90f));
            banner.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            var label = MakeText("TaskText", banner, font, 40, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            SetField(canvasGo.AddComponent<TaskHUD>(), "label", label);

            // Keyboard hint.
            var hint = MakeText("KeyboardHint", root, font, 26, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 50f));
            hint.text = "Keyboard: A/D move  ·  Space jump  ·  F bow";
            hint.color = new Color(0f, 0f, 0f, 0.6f);

            // Virtual stick (bottom-left).
            var stickBase = UIRect("StickBase", root, Vector2.zero, new Vector2(230f, 230f), new Vector2(280f, 280f));
            var baseImg = stickBase.gameObject.AddComponent<Image>();
            baseImg.sprite = circle;
            baseImg.color = new Color(1f, 1f, 1f, 0.25f);
            var knob = UIRect("StickKnob", stickBase, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            var knobImg = knob.gameObject.AddComponent<Image>();
            knobImg.sprite = circle;
            knobImg.color = new Color(1f, 1f, 1f, 0.6f);
            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 100f;

            TouchButton("JumpButton", "Jump", "<Gamepad>/buttonSouth", root, new Vector2(-200f, 230f), 190f, circle, font);
            TouchButton("BowButton", "Bow", "<Gamepad>/buttonWest", root, new Vector2(-430f, 170f), 170f, circle, font);
        }

        static void TouchButton(string name, string text, string path, Transform root, Vector2 pos, float size, Sprite circle, Font font)
        {
            var rt = UIRect(name, root, new Vector2(1f, 0f), pos, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = circle;
            img.color = new Color(1f, 1f, 1f, 0.45f);
            rt.gameObject.AddComponent<OnScreenButton>().controlPath = path;
            var label = MakeText("Label", rt, font, 36, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.text = text;
            label.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            label.raycastTarget = false;
        }

        static Arrow BuildArrowPrefab(Sprite square)
        {
            string path = $"{Root}/Prefabs/Arrow.prefab";
            var go = new GameObject("Arrow");
            go.transform.localScale = new Vector3(0.8f, 0.08f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = Gold;
            sr.sortingOrder = 11;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            go.AddComponent<Arrow>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Arrow>();
        }

        // ---------- helpers ----------

        static GameObject Block(string name, Sprite sprite, Vector2 pos, Vector2 size, Color color, Transform parent, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            if (collider) go.AddComponent<BoxCollider2D>();
            return go;
        }

        static Sprite MakeSprite(string name, int size, bool circle)
        {
            string path = $"{Root}/Art/Placeholder/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    if (circle)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                        a = Mathf.Clamp01(r - d);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static T LoadOrCreate<T>(string path, Func<T> create) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetField(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform UIRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Place(rt, anchor, pos, size);
            return rt;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            return t;
        }
    }
}
