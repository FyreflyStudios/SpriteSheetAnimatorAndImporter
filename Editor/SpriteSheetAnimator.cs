using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class SpriteSheetAnimator : EditorWindow
{
    private Texture2D spriteSheet;
    private List<string> animationNames = new List<string>();
    private int rowCount = 0;
    private int columnCount = 0;
    private string controllerPath = "Assets/1. Art/Animations";
    private string animatorControllerName = "SpriteAnimatorController";
    private bool createSubfolder = false; // Field to store checkbox state

    [MenuItem("Tools/Sprite Sheet Animator")]
    public static void ShowWindow()
    {
        GetWindow<SpriteSheetAnimator>("Sprite Sheet Animator");
    }

    void OnGUI()
    {
        GUILayout.Label("Sprite Sheet Animation Generator", EditorStyles.boldLabel);

        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);

        if (spriteSheet != null)
        {
            EditorGUILayout.Space();
            rowCount = EditorGUILayout.IntField("Number of Rows", rowCount);
            rowCount = Mathf.Max(1, rowCount); // Ensure rowCount is not less than 1.

            columnCount = EditorGUILayout.IntField("Number of Columns", columnCount);
            columnCount = Mathf.Max(1, columnCount); // Ensure columnCount is not less than 1.

            if (animationNames.Count != rowCount)
            {
                UpdateAnimationNameList(rowCount);
            }

            for (int i = 0; i < rowCount; i++)
            {
                animationNames[i] = EditorGUILayout.TextField($"Animation {i + 1} Name", animationNames[i]);
            }

            EditorGUILayout.Space();
            controllerPath = EditorGUILayout.TextField("Controller Path", controllerPath);
            animatorControllerName = EditorGUILayout.TextField("Animator Controller Name", animatorControllerName);

            createSubfolder = EditorGUILayout.Toggle("Create Subfolder", createSubfolder);


            if (GUILayout.Button("Generate Animations"))
            {
                GenerateAnimations();
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

    void GenerateAnimations()
    {
        if (spriteSheet == null)
        {
            Debug.LogError("Sprite Sheet is null");
            return;
        }

        string filePath = AssetDatabase.GetAssetPath(spriteSheet);

        if (createSubfolder)
        {
            string subFolderPath = Path.Combine(controllerPath, animatorControllerName);
            if (!Directory.Exists(subFolderPath))
            {
                Directory.CreateDirectory(subFolderPath);
            }
            // Update controllerPath to the new subfolder path
            controllerPath = subFolderPath;
        }

        // Slice the sprite sheet
        // Slice the sprite sheet
        var sprites = SliceSpriteSheet(filePath, rowCount, columnCount);


        if (sprites.Length != rowCount * columnCount)
        {
            Debug.LogError("The number of sliced sprites does not match the expected count (rows x columns).");
            return;
        }

        if (!Directory.Exists(controllerPath))
        {
            Directory.CreateDirectory(controllerPath);
        }

        string fullPath = Path.Combine(controllerPath, animatorControllerName + ".controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

        for (int i = 0; i < animationNames.Count; i++)
        {
            if (string.IsNullOrEmpty(animationNames[i])) continue;

            // Calculate the start index and number of sprites per animation
            int spriteStartIndex = i * columnCount;
            int spriteCount = columnCount;

            // Check if the spriteStartIndex + spriteCount exceeds the length of sprites array
            if (spriteStartIndex + spriteCount > sprites.Length)
            {
                Debug.LogError($"Animation '{animationNames[i]}' exceeds the sprite array bounds.");
                continue;
            }

            // Generate Animation Clip
            AnimationClip clip = GenerateAnimationClip(sprites, spriteStartIndex, spriteCount, controllerPath, animationNames[i]);

            // Add State to Animator Controller
            AnimatorState state = controller.layers[0].stateMachine.AddState(animationNames[i]);
            state.motion = clip;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //reset file path
        controllerPath = "Assets/1. Art/Animations";
        animatorControllerName = "SpriteAnimatorController";

    }


    public Sprite[] SliceSpriteSheet(string filePath, int rows, int columns)
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

        // Need to reload the asset to get the new sprites
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
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 12; // Set the frame rate (12 frames per second as an example).

        // Create the Editor Curve Binding for sprite renderer
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = ""; // Path to the Sprite Renderer component in the GameObject that will use this animation.
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[count];

        // Duration of each sprite frame (1 / frame rate)
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

        // Create the clip with the keyframes
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);

        // Save the generated animation clip asset
        string clipPath = Path.Combine(directory, $"{animationName}.anim");
        AssetDatabase.CreateAsset(clip, AssetDatabase.GenerateUniqueAssetPath(clipPath));
        AssetDatabase.Refresh();

        return clip;
    }

}
