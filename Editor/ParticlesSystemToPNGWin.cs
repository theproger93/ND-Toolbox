using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

    public class ParticlesSystemToPNGWin : EditorWindow
    {
#if UNITY_EDITOR
        private ParticleSystem _particlesSystem;
        private int _width = 512;
        private int _height = 512;
        private string _fileName = "ParticlesFrame";
        private string _folderName = "ParticlesRendered";
        private int _numFrames = 30;
        private float _frameDuration = 0.1f;
        private bool _cleanBackground = true;
        private bool _cameraAutoFit = true;

        private const string _helpBox = "1. Drag and drop your particle system into the Particle System field.\n" +
                                       "2. Specify a file name for the PNGs you want to capture.\n" +
                                       "3. Specify a folder name to save the files. You can also include subfolders by using the forward slash (/) character. If the folder doesn't exist, the plugin will create it automatically and save all the files in it.\n" +
                                       "4. Determine the number of frames you want to capture.\n" +
                                       "5. Set the duration between frames capturing.\n" +
                                       "6. Enable (Camera Auto Size) to optimize PNG capture by adjusting the camera size to fit the particle system, reducing overdraw.";

        private bool showTabContent;

        // Store original layers
        private Dictionary<GameObject, int> _originalLayers = new Dictionary<GameObject, int>();

        [MenuItem("Tools/ND Toolbox/Optimization/Particles System To PNGs")]
        public static void ShowWindow()
        {
            GetWindow<ParticlesSystemToPNGWin>("Particles to PNGs");
        }

        private void OnGUI()
        {
            GUIStyle headLine = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            GUILayout.Label("Capture Settings", headLine);
            GUILayout.Space(20);

            // Particle System Field
            GUIContent particleSystemContent = new GUIContent("   Particles System", EditorGUIUtility.IconContent("Particle Effect").image);
            _particlesSystem = EditorGUILayout.ObjectField(particleSystemContent, _particlesSystem, typeof(ParticleSystem), true) as ParticleSystem;

            // Width and Height Fields
            GUIContent widthContent = new GUIContent("   Width", EditorGUIUtility.IconContent("d_RectTool On").image);
            _width = EditorGUILayout.IntField(widthContent, _width);
            GUIContent heightContent = new GUIContent("   Height", EditorGUIUtility.IconContent("d_RectTool On").image);
            _height = EditorGUILayout.IntField(heightContent, _height);

            // File and Folder Name Fields
            GUIContent fileNameContent = new GUIContent("   Files Name", EditorGUIUtility.IconContent("d_RawImage Icon").image);
            _fileName = EditorGUILayout.TextField(fileNameContent, _fileName);
            GUIContent folderNameContent = new GUIContent("   Folder Name", EditorGUIUtility.IconContent("d_FolderOpened Icon").image);
            _folderName = EditorGUILayout.TextField(folderNameContent, _folderName);

            // Number of Frames and Frame Duration Fields
            GUIContent numFramesContent = new GUIContent("   Num. of Frames", EditorGUIUtility.IconContent("PreTextureArrayFirstSlice").image);
            _numFrames = EditorGUILayout.IntField(numFramesContent, _numFrames);
            GUIContent frameTimeContent = new GUIContent("   Frame Duration", EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image);
            _frameDuration = EditorGUILayout.FloatField(frameTimeContent, _frameDuration);

            // Clean Background and Camera Auto Fit Toggles
            GUIContent cleanBackgroundContent = new GUIContent("   Clean Background", EditorGUIUtility.IconContent("d_RectMask2D Icon").image);
            _cleanBackground = EditorGUILayout.Toggle(cleanBackgroundContent, _cleanBackground);
            GUIContent cameraAutoSizeContent = new GUIContent("   Camera Auto Size", EditorGUIUtility.IconContent("d_ScaleTool On").image);
            _cameraAutoFit = EditorGUILayout.Toggle(cameraAutoSizeContent, _cameraAutoFit);

            GUILayout.Space(10);

            // Capture Button
            GUIStyle greenButtonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.cyan },
                hover = { textColor = Color.green },
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            GUIContent captureButtonContent = new GUIContent("   CAPTURE", EditorGUIUtility.IconContent("Animation.Record@2x").image);
            if (GUILayout.Button(captureButtonContent, greenButtonStyle, GUILayout.Height(50)))
            {
                if (_particlesSystem == null)
                {
                    Debug.LogError("No particle system selected!");
                    return;
                }
                Capture();
            }

            // Help Box
            showTabContent = EditorGUILayout.Foldout(showTabContent, "How to use ?");
            if (showTabContent)
            {
                EditorGUILayout.HelpBox(_helpBox, MessageType.Info);
            }

            // Link to Website
            GUIStyle linkButtonStyle = new GUIStyle(GUI.skin.button)
            {
                hover = { textColor = Color.green },
                fontSize = 8
            };

            if (GUILayout.Button("NikDorn.com", linkButtonStyle))
            {
                Application.OpenURL("https://nikdorn.com/");
            }
        }

        private void Capture()
        {
            // Create a new camera for rendering
            Camera captureCamera = new GameObject("CaptureCamera", typeof(Camera)).GetComponent<Camera>();
            captureCamera.clearFlags = CameraClearFlags.Color;
            captureCamera.backgroundColor = Color.clear;
            captureCamera.orthographic = true;
            captureCamera.nearClipPlane = 0.01f;
            captureCamera.farClipPlane = 100f;

            // Create the output folder if it doesn't exist
            string folderPath = Path.Combine(Application.dataPath, _folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Set up the render texture
            RenderTexture renderTexture = new RenderTexture(_width, _height, 32, RenderTextureFormat.ARGB32);
            captureCamera.targetTexture = renderTexture;

            // Configure the particle system layer for background cleaning
            if (_cleanBackground)
            {
                // Store original layers
                _originalLayers.Clear();
                StoreOriginalLayer(_particlesSystem.gameObject);
                foreach (var renderer in _particlesSystem.GetComponentsInChildren<ParticleSystemRenderer>())
                {
                    StoreOriginalLayer(renderer.gameObject);
                }

                // Find an available layer for the particle system
                int particleLayer = FindAvailableLayer();
                if (particleLayer == -1)
                {
                    Debug.LogError("No available layer found for particle system. Please ensure at least one layer is unused.");
                    DestroyImmediate(captureCamera.gameObject);
                    return;
                }

                // Assign the particle system and its children to the new layer
                _particlesSystem.gameObject.layer = particleLayer;
                foreach (var renderer in _particlesSystem.GetComponentsInChildren<ParticleSystemRenderer>())
                {
                    renderer.gameObject.layer = particleLayer;
                }
                captureCamera.cullingMask = 1 << particleLayer;
            }

            // Auto-fit the camera to the particle system bounds
            if (_cameraAutoFit)
            {
                ParticleSystemRenderer[] renderers = _particlesSystem.GetComponentsInChildren<ParticleSystemRenderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }

                    float cameraSize = Mathf.Max(bounds.size.x, bounds.size.y) / 2f;
                    captureCamera.orthographicSize = cameraSize;
                    captureCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
                }
            }

        // Capture frames
        Texture2D texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        for (int i = 0; i < _numFrames; i++)
        {
            EditorUtility.DisplayProgressBar("Capturing Frames", $"Frame {i + 1} of {_numFrames}", (float)i / _numFrames);

            _particlesSystem.Simulate(_frameDuration, true, false);
            _particlesSystem.Play();
            captureCamera.Render();

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();
            string filePath = Path.Combine(folderPath, $"{_fileName}_{i}.png");
            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"Saved frame {i} to {filePath}");
        }

        // Reset active RenderTexture before cleanup
        RenderTexture.active = null;

        // Clean up
        EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        renderTexture.Release();
        DestroyImmediate(captureCamera.gameObject);

            // Restore original layers
            if (_cleanBackground)
            {
                RestoreOriginalLayers();
            }

            Debug.Log("Capture complete!");
        }

       
        private void StoreOriginalLayer(GameObject gameObject)
        {
            _originalLayers[gameObject] = gameObject.layer;
        }

        
        private void RestoreOriginalLayers()
        {
            foreach (var kvp in _originalLayers)
            {
                kvp.Key.layer = kvp.Value;
            }
            _originalLayers.Clear();
        }

        
        private int FindAvailableLayer()
        {
            for (int i = 0; i < 32; i++)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i)))
                {
                    return i; 
                }
            }
            return -1; 
        }
#endif
    }