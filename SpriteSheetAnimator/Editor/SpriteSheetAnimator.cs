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

    private bool useManualAnimations = false;

    private List<AnimationData> animations = new List<AnimationData>();


    [MenuItem("Tools/Sprite Sheet Animator")]
    public static void ShowWindow()
    {
        GetWindow<SpriteSheetAnimator>("Sprite Sheet Animator");
    }

    void OnGUI()
    {
        GUILayout.Label("Sprite Sheet Animation Generator", EditorStyles.boldLabel);

        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);

        EditorGUILayout.Space();

        useManualAnimations = EditorGUILayout.Toggle("Use Manual Animations", useManualAnimations);

        if (useManualAnimations != true)
        {

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
        else
        {
            if (spriteSheet != null)
            {
                EditorGUILayout.Space();
                rowCount = EditorGUILayout.IntField("Number of Rows", rowCount);
                rowCount = Mathf.Max(1, rowCount); // Ensure rowCount is not less than 1.

                columnCount = EditorGUILayout.IntField("Number of Columns", columnCount);
                columnCount = Mathf.Max(1, columnCount); // Ensure columnCount is not less than 1.

                EditorGUILayout.Space();
                controllerPath = EditorGUILayout.TextField("Controller Path", controllerPath);
                animatorControllerName = EditorGUILayout.TextField("Animator Controller Name", animatorControllerName);

                createSubfolder = EditorGUILayout.Toggle("Create Subfolder", createSubfolder);

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

                if (GUILayout.Button("Generate Manuel Animations"))
                {
                    GenerateManualAnimations();
                }
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


    private void GenerateManualAnimations()
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
            Debug.LogError("Invalid sprite sheet configuration. Expected " + (rowCount * columnCount) + " sprites, but got " + sprites.Length);
            return;
        }

        if (!Directory.Exists(controllerPath))
        {
            Directory.CreateDirectory(controllerPath);
        }

        string fullPath = Path.Combine(controllerPath, animatorControllerName + ".controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(fullPath);

        //create clips

        foreach (var anim in animations)
        {
            // Adjusting for 1-based indexing by subtracting 1
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

        //reset file path
        controllerPath = "Assets/1. Art/Animations";
        animatorControllerName = "SpriteAnimatorController";
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

public class AnimationData
{
    public string Name;
    public int StartFrame;
    public int EndFrame;
}
