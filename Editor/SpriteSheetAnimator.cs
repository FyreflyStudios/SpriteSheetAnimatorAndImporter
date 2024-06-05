using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class SpriteSheetAnimator : EditorWindow
{
    private enum Tab
    {
        SingleSheet,
        MultipleSheets
    }

    private Tab currentTab = Tab.SingleSheet;

    private Texture2D spriteSheet;
    private List<string> animationNames = new List<string>();
    private int rowCount = 1;
    private int columnCount = 1;
    private string controllerPath = "Assets/1. Art/Animations";
    private string animatorControllerName = "SpriteAnimatorController";
    private bool createSubfolder = false;
    private bool useManualAnimations = false;
    private List<AnimationData> animations = new List<AnimationData>();
    private bool useImageComponent = false;

    private List<MultipleSheetAnimationData> multipleSheetAnimations = new List<MultipleSheetAnimationData>();

    [MenuItem("Tools/Sprite Sheet Animator")]
    public static void ShowWindow()
    {
        GetWindow<SpriteSheetAnimator>("Sprite Sheet Animator");
    }

    void OnGUI()
    {
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new string[] { "Single Sheet", "Multiple Sheets" });

        useImageComponent = EditorGUILayout.Toggle("Use Image Component", useImageComponent);

        switch (currentTab)
        {
            case Tab.SingleSheet:
                DrawSingleSheetTab();
                break;
            case Tab.MultipleSheets:
                DrawMultipleSheetsTab();
                break;
        }
    }

    void DrawSingleSheetTab()
    {
        GUILayout.Label("Sprite Sheet Animation Generator", EditorStyles.boldLabel);

        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);
        EditorGUILayout.Space();

        useManualAnimations = EditorGUILayout.Toggle("Use Manual Animations", useManualAnimations);
        rowCount = EditorGUILayout.IntField("Number of Rows", rowCount);
        rowCount = Mathf.Max(1, rowCount);
        columnCount = EditorGUILayout.IntField("Number of Columns", columnCount);
        columnCount = Mathf.Max(1, columnCount);

        EditorGUILayout.Space();
        controllerPath = EditorGUILayout.TextField("Controller Path", controllerPath);
        animatorControllerName = EditorGUILayout.TextField("Animator Controller Name", animatorControllerName);
        createSubfolder = EditorGUILayout.Toggle("Create Subfolder", createSubfolder);

        if (useManualAnimations)
        {
            DisplayManualAnimations();
        }
        else
        {
            DisplayAutomaticAnimations();
        }

        if (GUILayout.Button("Generate Animations"))
        {
            if (useManualAnimations)
            {
                GenerateManualAnimations();
            }
            else
            {
                GenerateAutomaticAnimations();
            }
        }
    }

    void DrawMultipleSheetsTab()
    {
        GUILayout.Label("Multiple Sheets Animation Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Animation"))
        {
            multipleSheetAnimations.Add(new MultipleSheetAnimationData());
        }

        for (int i = 0; i < multipleSheetAnimations.Count; i++)
        {
            var animData = multipleSheetAnimations[i];
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"Animation {i + 1}", EditorStyles.boldLabel);
            animData.Name = EditorGUILayout.TextField("Name", animData.Name);
            animData.SpriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", animData.SpriteSheet, typeof(Texture2D), false);
            animData.RowCount = EditorGUILayout.IntField("Number of Rows", animData.RowCount);
            animData.RowCount = Mathf.Max(1, animData.RowCount);
            animData.ColumnCount = EditorGUILayout.IntField("Number of Columns", animData.ColumnCount);
            animData.ColumnCount = Mathf.Max(1, animData.ColumnCount);

            if (GUILayout.Button("Remove"))
            {
                multipleSheetAnimations.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        controllerPath = EditorGUILayout.TextField("Controller Path", controllerPath);
        animatorControllerName = EditorGUILayout.TextField("Animator Controller Name", animatorControllerName);
        createSubfolder = EditorGUILayout.Toggle("Create Subfolder", createSubfolder);

        if (GUILayout.Button("Generate Multiple Sheet Animations"))
        {
            GenerateMultipleSheetAnimations();
        }
    }

    void DisplayAutomaticAnimations()
    {
        if (spriteSheet != null)
        {
            if (animationNames.Count != rowCount)
            {
                UpdateAnimationNameList(rowCount);
            }

            for (int i = 0; i < rowCount; i++)
            {
                animationNames[i] = EditorGUILayout.TextField($"Animation {i + 1} Name", animationNames[i]);
            }
        }
    }

    void DisplayManualAnimations()
    {
        if (spriteSheet != null)
        {
            if (GUILayout.Button("Add Animation"))
            {
                animations.Add(new AnimationData());
            }

            for (int i = 0; i < animations.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                animations[i].Name = EditorGUILayout.TextField("Name", animations[i].Name);
                animations[i].StartFrame = EditorGUILayout.IntField("Start Frame", animations[i].StartFrame);
                animations[i].EndFrame = EditorGUILayout.IntField("End Frame", animations[i].EndFrame);
                if (GUILayout.Button("Remove"))
                {
                    animations.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    void UpdateAnimationNameList(int newSize)
    {
        if (newSize < animationNames.Count)
            animationNames.RemoveRange(newSize, animationNames.Count - newSize);
        else
        {
            while (animationNames.Count < newSize)
                animationNames.Add("");
        }
    }

    void GenerateManualAnimations()
    {
        if (spriteSheet == null)
        {
            Debug.LogError("Sprite Sheet is null");
            return;
        }

        string filePath = AssetDatabase.GetAssetPath(spriteSheet);
        CreateSubfolderIfNeeded();
        var sprites = SliceSpriteSheet(filePath, rowCount, columnCount);

        if (sprites.Length != rowCount * columnCount)
        {
            Debug.LogError($"Invalid sprite sheet configuration. Expected {rowCount * columnCount} sprites, but got {sprites.Length}");
            return;
        }

        if (!Directory.Exists(controllerPath))
        {
            Directory.CreateDirectory(controllerPath);
        }

        string fullPath = Path.Combine(controllerPath, $"{animatorControllerName}.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

        foreach (var anim in animations)
        {
            int adjustedStartFrame = Mathf.Max(0, anim.StartFrame - 1);
            int adjustedEndFrame = Mathf.Max(0, anim.EndFrame - 1);

            if (string.IsNullOrEmpty(anim.Name) || adjustedStartFrame < 0 || adjustedEndFrame >= sprites.Length)
            {
                Debug.LogWarning($"Invalid animation data for {anim.Name}. Start or end frame out of range.");
                continue;
            }

            int count = adjustedEndFrame - adjustedStartFrame + 1;
            if (adjustedStartFrame + count > sprites.Length)
            {
                Debug.LogError($"Animation '{anim.Name}' exceeds the sprite array bounds.");
                continue;
            }

            AnimationClip clip = GenerateAnimationClip(sprites, adjustedStartFrame, count, controllerPath, anim.Name);
            AnimatorState state = controller.layers[0].stateMachine.AddState(anim.Name);
            state.motion = clip;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void GenerateAutomaticAnimations()
    {
        if (spriteSheet == null)
        {
            Debug.LogError("Sprite Sheet is null");
            return;
        }

        string filePath = AssetDatabase.GetAssetPath(spriteSheet);
        CreateSubfolderIfNeeded();
        var sprites = SliceSpriteSheet(filePath, rowCount, columnCount);

        if (sprites.Length != rowCount * columnCount)
        {
            Debug.LogError($"Invalid sprite sheet configuration. Expected {rowCount * columnCount} sprites, but got {sprites.Length}");
            return;
        }

        if (!Directory.Exists(controllerPath))
        {
            Directory.CreateDirectory(controllerPath);
        }

        string fullPath = Path.Combine(controllerPath, $"{animatorControllerName}.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

        for (int i = 0; i < animationNames.Count; i++)
        {
            if (string.IsNullOrEmpty(animationNames[i])) continue;

            int spriteStartIndex = i * columnCount;
            int spriteCount = columnCount;

            if (spriteStartIndex + spriteCount > sprites.Length)
            {
                Debug.LogError($"Animation '{animationNames[i]}' exceeds the sprite array bounds.");
                continue;
            }

            AnimationClip clip = GenerateAnimationClip(sprites, spriteStartIndex, spriteCount, controllerPath, animationNames[i]);
            AnimatorState state = controller.layers[0].stateMachine.AddState(animationNames[i]);
            state.motion = clip;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void GenerateMultipleSheetAnimations()
    {
        if (multipleSheetAnimations.Count == 0)
        {
            Debug.LogError("No animations to generate");
            return;
        }

        CreateSubfolderIfNeeded();

        if (!Directory.Exists(controllerPath))
        {
            Directory.CreateDirectory(controllerPath);
        }

        string fullPath = Path.Combine(controllerPath, $"{animatorControllerName}.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

        foreach (var animData in multipleSheetAnimations)
        {
            if (animData.SpriteSheet == null)
            {
                Debug.LogWarning($"Sprite Sheet for animation '{animData.Name}' is null");
                continue;
            }

            string filePath = AssetDatabase.GetAssetPath(animData.SpriteSheet);
            var sprites = SliceSpriteSheet(filePath, animData.RowCount, animData.ColumnCount);

            if (sprites.Length != animData.RowCount * animData.ColumnCount)
            {
                Debug.LogError($"Invalid sprite sheet configuration for animation '{animData.Name}'. Expected {animData.RowCount * animData.ColumnCount} sprites, but got {sprites.Length}");
                continue;
            }

            AnimationClip clip = GenerateAnimationClip(sprites, 0, sprites.Length, controllerPath, animData.Name);
            AnimatorState state = controller.layers[0].stateMachine.AddState(animData.Name);
            state.motion = clip;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void CreateSubfolderIfNeeded()
    {
        if (createSubfolder)
        {
            string subFolderPath = Path.Combine(controllerPath, animatorControllerName);
            if (!Directory.Exists(subFolderPath))
            {
                Directory.CreateDirectory(subFolderPath);
            }
            controllerPath = subFolderPath;
        }
    }

    Sprite[] SliceSpriteSheet(string filePath, int rows, int columns)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("Directory is null or empty.");
            return null;
        }

        Texture2D spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
        if (spriteSheet == null)
        {
            Debug.LogError("Failed to load the texture at: " + filePath);
            return null;
        }

        string assetPath = AssetDatabase.GetAssetPath(spriteSheet);
        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (textureImporter != null)
        {
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;

            var spriteData = new List<SpriteMetaData>();
            float spriteWidth = spriteSheet.width / columns;
            float spriteHeight = spriteSheet.height / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    SpriteMetaData metaData = new SpriteMetaData
                    {
                        pivot = new Vector2(0.5f, 0.5f),
                        name = $"{Path.GetFileNameWithoutExtension(filePath)}_r{r}_c{c}",
                        rect = new Rect(c * spriteWidth, spriteSheet.height - (r + 1) * spriteHeight, spriteWidth, spriteHeight)
                    };
                    spriteData.Add(metaData);
                }
            }

            textureImporter.spritesheet = spriteData.ToArray();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh();
        spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);

        string spriteSheetName = spriteSheet.name;
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (o is Sprite sprite && sprite.texture.name == spriteSheetName)
            {
                sprites.Add(sprite);
            }
        }

        return sprites.ToArray();
    }

    AnimationClip GenerateAnimationClip(Sprite[] sprites, int startIndex, int count, string directory, string animationName)
    {
        AnimationClip clip = new AnimationClip
        {
            frameRate = 12
        };

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = useImageComponent ? typeof(Image) : typeof(SpriteRenderer),
            path = "",
            propertyName = useImageComponent ? "m_Sprite" : "m_Sprite"
        };

        ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[count];
        float frameTime = 1.0f / clip.frameRate;

        for (int i = 0; i < count; i++)
        {
            if ((startIndex + i) < sprites.Length)
            {
                spriteKeyFrames[i] = new ObjectReferenceKeyframe
                {
                    time = i * frameTime,
                    value = sprites[startIndex + i]
                };
            }
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);

        string clipPath = Path.Combine(directory, $"{animationName}.anim");
        AssetDatabase.CreateAsset(clip, AssetDatabase.GenerateUniqueAssetPath(clipPath));
        AssetDatabase.Refresh();

        return clip;
    }

    private class MultipleSheetAnimationData
    {
        public string Name;
        public Texture2D SpriteSheet;
        public int RowCount = 1;
        public int ColumnCount = 1;
    }

    private class AnimationData
    {
        public string Name;
        public int StartFrame;
        public int EndFrame;
    }
}
