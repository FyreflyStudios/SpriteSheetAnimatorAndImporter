using System.Collections.Generic;
using System.Linq;
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
        MultipleSheets,
        AnimatorTransfer
    }

    private Tab currentTab = Tab.SingleSheet;
    private Vector2 singleSheetScrollPosition;
    private Vector2 multipleSheetsScrollPosition;

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
    private Vector2 singleSheetPivot = new Vector2(0.5f, 0.5f);

    private bool useExistingAnimatorController = false;
    private AnimatorController existingAnimatorController;
    private bool specifySavePath = false;
    private string savePath = "";
    private AnimatorController templateAnimatorController;

    private AnimatorController sourceAnimatorController;
    private AnimatorController destinationAnimatorController;

    private List<MultipleSheetAnimationData> multipleSheetAnimations = new List<MultipleSheetAnimationData>();

    [MenuItem("Tools/Sprite Sheet Animator")]
    public static void ShowWindow()
    {
        GetWindow<SpriteSheetAnimator>("Sprite Sheet Animator");
    }

    void OnGUI()
    {
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new string[] { "Single Sheet", "Multiple Sheets", "Animator Transfer" });

        switch (currentTab)
        {
            case Tab.SingleSheet:
                DrawSingleSheetTab();
                break;
            case Tab.MultipleSheets:
                DrawMultipleSheetsTab();
                break;
            case Tab.AnimatorTransfer:
                DrawAnimatorTransferTab();
                break;
        }
    }

    void DrawSingleSheetTab()
    {
        singleSheetScrollPosition = GUILayout.BeginScrollView(singleSheetScrollPosition);

        GUILayout.Label("Sprite Sheet Animation Generator", EditorStyles.boldLabel);

        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);
        EditorGUILayout.Space();

        useImageComponent = EditorGUILayout.Toggle("Use Image Component", useImageComponent);
        useManualAnimations = EditorGUILayout.Toggle("Use Manual Animations", useManualAnimations);
        rowCount = EditorGUILayout.IntField("Number of Rows", rowCount);
        rowCount = Mathf.Max(1, rowCount);
        columnCount = EditorGUILayout.IntField("Number of Columns", columnCount);
        columnCount = Mathf.Max(1, columnCount);

        singleSheetPivot = EditorGUILayout.Vector2Field("Pivot Point", singleSheetPivot);

        EditorGUILayout.Space();
        useExistingAnimatorController = EditorGUILayout.Toggle("Use Existing Animator Controller", useExistingAnimatorController);

        if (useExistingAnimatorController)
        {
            existingAnimatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", existingAnimatorController, typeof(AnimatorController), false);
            specifySavePath = EditorGUILayout.Toggle("Specify Save Path for Animations", specifySavePath);
            if (specifySavePath)
            {
                savePath = EditorGUILayout.TextField("Save Path", savePath);
            }
        }
        else
        {
            controllerPath = EditorGUILayout.TextField("Controller Path", controllerPath);
            animatorControllerName = EditorGUILayout.TextField("Animator Controller Name", animatorControllerName);
            createSubfolder = EditorGUILayout.Toggle("Create Subfolder", createSubfolder);
        }

        EditorGUILayout.Space();
        templateAnimatorController = (AnimatorController)EditorGUILayout.ObjectField("Template Animator Controller", templateAnimatorController, typeof(AnimatorController), false);

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

        GUILayout.EndScrollView();
    }

    void DrawMultipleSheetsTab()
    {
        multipleSheetsScrollPosition = GUILayout.BeginScrollView(multipleSheetsScrollPosition);

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
            animData.Pivot = EditorGUILayout.Vector2Field("Pivot Point", animData.Pivot);
            animData.IsLooping = EditorGUILayout.Toggle("Looping", animData.IsLooping);

            if (GUILayout.Button("Remove"))
            {
                multipleSheetAnimations.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        useImageComponent = EditorGUILayout.Toggle("Use Image Component", useImageComponent);
        useExistingAnimatorController = EditorGUILayout.Toggle("Use Existing Animator Controller", useExistingAnimatorController);

        if (useExistingAnimatorController)
        {
            existingAnimatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", existingAnimatorController, typeof(AnimatorController), false);
            specifySavePath = EditorGUILayout.Toggle("Specify Save Path for Animations", specifySavePath);
            if (specifySavePath)
            {
                savePath = EditorGUILayout.TextField("Save Path", savePath);
            }
        }
        else
        {
            controllerPath = EditorGUILayout.TextField("Controller Path", controllerPath);
            animatorControllerName = EditorGUILayout.TextField("Animator Controller Name", animatorControllerName);
            createSubfolder = EditorGUILayout.Toggle("Create Subfolder", createSubfolder);
        }

        EditorGUILayout.Space();
        templateAnimatorController = (AnimatorController)EditorGUILayout.ObjectField("Template Animator Controller", templateAnimatorController, typeof(AnimatorController), false);

        if (GUILayout.Button("Generate Multiple Sheet Animations"))
        {
            GenerateMultipleSheetAnimations();
        }

        GUILayout.EndScrollView();
    }

    void DrawAnimatorTransferTab()
    {
        GUILayout.Label("Animator Transfer", EditorStyles.boldLabel);

        sourceAnimatorController = (AnimatorController)EditorGUILayout.ObjectField("Source Animator Controller", sourceAnimatorController, typeof(AnimatorController), false);
        destinationAnimatorController = (AnimatorController)EditorGUILayout.ObjectField("Destination Animator Controller", destinationAnimatorController, typeof(AnimatorController), false);

        if (GUILayout.Button("Transfer Parameters and Transitions"))
        {
            TransferParametersAndTransitions();
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
                bool isLooping = EditorGUILayout.Toggle("Looping", animations.Count > i ? animations[i].IsLooping : false);
                if (animations.Count > i)
                {
                    animations[i].IsLooping = isLooping;
                }
                else
                {
                    animations.Add(new AnimationData { IsLooping = isLooping });
                }
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
                animations[i].IsLooping = EditorGUILayout.Toggle("Looping", animations[i].IsLooping);
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
        var sprites = SliceSpriteSheet(filePath, rowCount, columnCount, singleSheetPivot);

        if (sprites.Length != rowCount * columnCount)
        {
            Debug.LogError($"Invalid sprite sheet configuration. Expected {rowCount * columnCount} sprites, but got {sprites.Length}");
            return;
        }

        AnimatorController controller = useExistingAnimatorController ? existingAnimatorController : CreateAnimatorController();

        string saveDirectory = GetSaveDirectory(controller);

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

            AnimationClip clip = GenerateAnimationClip(FilterEmptySprites(sprites.Skip(adjustedStartFrame).Take(count).ToArray()), saveDirectory, anim.Name, anim.IsLooping);
            AnimatorState state = controller.layers[0].stateMachine.AddState(anim.Name);
            state.motion = clip;
        }

        if (templateAnimatorController != null)
        {
            CopyParametersAndTransitionsFromTemplate(controller, templateAnimatorController);
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
        var sprites = SliceSpriteSheet(filePath, rowCount, columnCount, singleSheetPivot);

        if (sprites.Length != rowCount * columnCount)
        {
            Debug.LogError($"Invalid sprite sheet configuration. Expected {rowCount * columnCount} sprites, but got {sprites.Length}");
            return;
        }

        AnimatorController controller = useExistingAnimatorController ? existingAnimatorController : CreateAnimatorController();

        string saveDirectory = GetSaveDirectory(controller);

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

            AnimationClip clip = GenerateAnimationClip(FilterEmptySprites(sprites.Skip(spriteStartIndex).Take(spriteCount).ToArray()), saveDirectory, animationNames[i], animations[i].IsLooping);
            AnimatorState state = controller.layers[0].stateMachine.AddState(animationNames[i]);
            state.motion = clip;
        }

        if (templateAnimatorController != null)
        {
            CopyParametersAndTransitionsFromTemplate(controller, templateAnimatorController);
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

        AnimatorController controller = useExistingAnimatorController ? existingAnimatorController : CreateAnimatorController();

        string saveDirectory = GetSaveDirectory(controller);

        foreach (var animData in multipleSheetAnimations)
        {
            if (animData.SpriteSheet == null)
            {
                Debug.LogWarning($"Sprite Sheet for animation '{animData.Name}' is null");
                continue;
            }

            string filePath = AssetDatabase.GetAssetPath(animData.SpriteSheet);
            var sprites = SliceSpriteSheet(filePath, animData.RowCount, animData.ColumnCount, animData.Pivot);

            if (sprites.Length != animData.RowCount * animData.ColumnCount)
            {
                Debug.LogError($"Invalid sprite sheet configuration for animation '{animData.Name}'. Expected {animData.RowCount * animData.ColumnCount} sprites, but got {sprites.Length}");
                continue;
            }

            AnimationClip clip = GenerateAnimationClip(FilterEmptySprites(sprites), saveDirectory, animData.Name, animData.IsLooping);
            AnimatorState state = controller.layers[0].stateMachine.AddState(animData.Name);
            state.motion = clip;
        }

        if (templateAnimatorController != null)
        {
            CopyParametersAndTransitionsFromTemplate(controller, templateAnimatorController);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    AnimatorController CreateAnimatorController()
    {
        if (!Directory.Exists(controllerPath))
        {
            Directory.CreateDirectory(controllerPath);
        }

        string fullPath = Path.Combine(controllerPath, $"{animatorControllerName}.controller");
        return AnimatorController.CreateAnimatorControllerAtPath(fullPath);
    }

    string GetSaveDirectory(AnimatorController controller)
    {
        if (useExistingAnimatorController)
        {
            if (specifySavePath && !string.IsNullOrEmpty(savePath))
            {
                return savePath;
            }
            else
            {
                return Path.GetDirectoryName(AssetDatabase.GetAssetPath(controller));
            }
        }
        else
        {
            return controllerPath;
        }
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

    void CopyParametersAndTransitionsFromTemplate(AnimatorController targetController, AnimatorController templateController)
    {
        foreach (var param in templateController.parameters)
        {
            if (!HasParameter(targetController, param.name))
            {
                targetController.AddParameter(param.name, param.type);
            }
        }

        foreach (var layer in templateController.layers)
        {
            AnimatorStateMachine sourceStateMachine = layer.stateMachine;
            AnimatorStateMachine destinationStateMachine = GetDestinationStateMachine(targetController, layer.name);

            if (destinationStateMachine == null)
            {
                Debug.LogWarning($"Could not find or create state machine for layer: {layer.name}");
                continue;
            }

            CopyTransitions(sourceStateMachine, destinationStateMachine);

            // Copy AnyState transitions
            CopyAnyStateTransitions(sourceStateMachine, destinationStateMachine);

            // Copy State positions
            CopyStatePositions(sourceStateMachine, destinationStateMachine);
        }
    }

    void TransferParametersAndTransitions()
    {
        if (sourceAnimatorController == null || destinationAnimatorController == null)
        {
            Debug.LogError("Source or Destination Animator Controller is null.");
            return;
        }

        // Clear existing parameters and transitions
        ClearParametersAndTransitions(destinationAnimatorController);

        // Copy parameters
        foreach (var param in sourceAnimatorController.parameters)
        {
            if (!HasParameter(destinationAnimatorController, param.name))
            {
                destinationAnimatorController.AddParameter(param.name, param.type);
            }
        }

        // Copy transitions and state positions
        foreach (var layer in sourceAnimatorController.layers)
        {
            AnimatorStateMachine sourceStateMachine = layer.stateMachine;
            AnimatorStateMachine destinationStateMachine = GetDestinationStateMachine(destinationAnimatorController, layer.name);

            if (destinationStateMachine == null)
            {
                Debug.LogWarning($"Could not find or create state machine for layer: {layer.name}");
                continue;
            }

            CopyTransitions(sourceStateMachine, destinationStateMachine);
            CopyAnyStateTransitions(sourceStateMachine, destinationStateMachine);
            CopyStatePositions(sourceStateMachine, destinationStateMachine);

            // Copy default state
            if (layer.stateMachine.defaultState != null)
            {
                var defaultState = destinationStateMachine.states.FirstOrDefault(s => s.state.name == layer.stateMachine.defaultState.name).state;
                if (defaultState != null)
                {
                    destinationStateMachine.defaultState = defaultState;
                }
            }
        }

        Debug.Log("Parameters, transitions, and state positions transferred successfully.");
    }

    void ClearParametersAndTransitions(AnimatorController controller)
    {
        // Clear parameters
        foreach (var param in controller.parameters.ToList())
        {
            controller.RemoveParameter(param);
        }

        // Clear transitions for each layer
        foreach (var layer in controller.layers)
        {
            var stateMachine = layer.stateMachine;

            // Clear state transitions
            foreach (var state in stateMachine.states)
            {
                state.state.transitions = new AnimatorStateTransition[0];
            }

            // Clear AnyState transitions
            stateMachine.anyStateTransitions = new AnimatorStateTransition[0];
        }
    }

    AnimatorStateMachine GetDestinationStateMachine(AnimatorController destinationController, string layerName)
    {
        foreach (var layer in destinationController.layers)
        {
            if (layer.name == layerName)
            {
                return layer.stateMachine;
            }
        }

        // If the layer doesn't exist, create it
        AnimatorControllerLayer newLayer = new AnimatorControllerLayer
        {
            name = layerName,
            stateMachine = new AnimatorStateMachine()
        };
        destinationController.AddLayer(newLayer);
        return newLayer.stateMachine;
    }

    void CopyTransitions(AnimatorStateMachine sourceStateMachine, AnimatorStateMachine destinationStateMachine)
    {
        foreach (var state in sourceStateMachine.states)
        {
            AnimatorState destinationState = destinationStateMachine.states.FirstOrDefault(s => s.state.name == state.state.name).state;

            if (destinationState == null)
            {
                Debug.LogWarning($"State '{state.state.name}' does not exist in the destination state machine. Skipping transition copy.");
                continue;
            }

            foreach (var transition in state.state.transitions)
            {
                AnimatorState destinationTargetState = destinationStateMachine.states.FirstOrDefault(s => s.state.name == transition.destinationState.name).state;

                if (destinationTargetState == null)
                {
                    Debug.LogWarning($"Target state '{transition.destinationState.name}' for transition does not exist in the destination state machine. Skipping transition copy.");
                    continue;
                }

                AnimatorStateTransition newTransition = destinationState.AddTransition(destinationTargetState);
                CopyTransitionSettings(transition, newTransition);
            }
        }
    }

    void CopyAnyStateTransitions(AnimatorStateMachine sourceStateMachine, AnimatorStateMachine destinationStateMachine)
    {
        foreach (var transition in sourceStateMachine.anyStateTransitions)
        {
            AnimatorState destinationTargetState = destinationStateMachine.states.FirstOrDefault(s => s.state.name == transition.destinationState.name).state;

            if (destinationTargetState == null)
            {
                Debug.LogWarning($"Target state '{transition.destinationState.name}' for AnyState transition does not exist in the destination state machine. Skipping transition copy.");
                continue;
            }

            AnimatorStateTransition newTransition = destinationStateMachine.AddAnyStateTransition(destinationTargetState);
            CopyTransitionSettings(transition, newTransition);
        }
    }

    void CopyTransitionSettings(AnimatorStateTransition sourceTransition, AnimatorStateTransition destinationTransition)
    {
        destinationTransition.conditions = sourceTransition.conditions;
        destinationTransition.hasExitTime = sourceTransition.hasExitTime;
        destinationTransition.exitTime = sourceTransition.exitTime;
        destinationTransition.duration = sourceTransition.duration;
        destinationTransition.offset = sourceTransition.offset;
        destinationTransition.interruptionSource = sourceTransition.interruptionSource;
        destinationTransition.orderedInterruption = sourceTransition.orderedInterruption;
        destinationTransition.canTransitionToSelf = sourceTransition.canTransitionToSelf;
        destinationTransition.isExit = sourceTransition.isExit;
        destinationTransition.mute = sourceTransition.mute;
        destinationTransition.solo = sourceTransition.solo;
    }

    void CopyStatePositions(AnimatorStateMachine sourceStateMachine, AnimatorStateMachine destinationStateMachine)
    {
        // Create a dictionary to map source state names to their positions
        Dictionary<string, Vector3> sourceStatePositions = new Dictionary<string, Vector3>();
        foreach (var sourceState in sourceStateMachine.states)
        {
            sourceStatePositions[sourceState.state.name] = sourceState.position;
        }

        // Apply positions to destination states
        var destinationStates = destinationStateMachine.states;
        for (int i = 0; i < destinationStates.Length; i++)
        {
            var destinationState = destinationStates[i];
            if (sourceStatePositions.TryGetValue(destinationState.state.name, out var position))
            {
                destinationState.position = position;
            }
            else
            {
                destinationState.position = new Vector3(50 + i * 10, 50 + i * 10); // Offset position for unmatched states
            }
            destinationStates[i] = destinationState;
        }

        // Update the states in the destination state machine
        destinationStateMachine.states = destinationStates;

        // Copy positions for Entry and AnyState nodes
        destinationStateMachine.entryPosition = sourceStateMachine.entryPosition;
        destinationStateMachine.anyStatePosition = sourceStateMachine.anyStatePosition;

        // Copy positions for Exit node if it exists
        if (sourceStateMachine.exitPosition != Vector3.zero)
        {
            destinationStateMachine.exitPosition = sourceStateMachine.exitPosition;
        }
    }




    bool HasParameter(AnimatorController controller, string paramName)
    {
        foreach (var param in controller.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }
        return false;
    }

    bool IsSpriteEmpty(Sprite sprite)
    {
        if (sprite == null)
        {
            return true;
        }

        var texture = sprite.texture;
        var pixels = texture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);
        return pixels.All(p => p.a == 0);
    }

    Sprite[] FilterEmptySprites(Sprite[] sprites)
    {
        return sprites.Where(s => !IsSpriteEmpty(s)).ToArray();
    }

    Sprite[] SliceSpriteSheet(string filePath, int rows, int columns, Vector2 pivot)
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
            textureImporter.isReadable = true; // Ensure the texture is readable

            var spriteData = new List<SpriteMetaData>();
            float spriteWidth = spriteSheet.width / columns;
            float spriteHeight = spriteSheet.height / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    SpriteMetaData metaData = new SpriteMetaData
                    {
                        pivot = pivot,
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

    AnimationClip GenerateAnimationClip(Sprite[] sprites, string directory, string animationName, bool isLooping)
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

        List<ObjectReferenceKeyframe> spriteKeyFrames = new List<ObjectReferenceKeyframe>();
        float frameTime = 1.0f / clip.frameRate;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && !IsSpriteEmpty(sprites[i]))
            {
                spriteKeyFrames.Add(new ObjectReferenceKeyframe
                {
                    time = i * frameTime,
                    value = sprites[i]
                });
            }
        }

        // Set the keyframes only if there are valid keyframes to set
        if (spriteKeyFrames.Count > 0)
        {
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames.ToArray());

            if (isLooping)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            string clipPath = Path.Combine(directory, $"{animationName}.anim");
            AssetDatabase.CreateAsset(clip, AssetDatabase.GenerateUniqueAssetPath(clipPath));
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogWarning($"No valid keyframes found for animation '{animationName}'. Clip was not created.");
        }

        return clip;
    }

    private class MultipleSheetAnimationData
    {
        public string Name;
        public Texture2D SpriteSheet;
        public int RowCount = 1;
        public int ColumnCount = 1;
        public Vector2 Pivot = new Vector2(0.5f, 0.5f);
        public bool IsLooping = false;
    }

    private class AnimationData
    {
        public string Name;
        public int StartFrame;
        public int EndFrame;
        public bool IsLooping = false;
    }
}
