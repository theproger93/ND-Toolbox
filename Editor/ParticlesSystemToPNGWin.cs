using UnityEngine;
using UnityEditor;
using System.IO;

namespace Scripts
{
    public class ParticlesSystemToPNGWin : EditorWindow
    {

#if UNITY_EDITOR
        private ParticleSystem _particlesSystem;
        private int _width = 512;
        private int _height = 512;
        private string _fileName = "ParticlesFrame";
        private string _folderName = "ParticlesRenderd";
        private int _numFrames = 30;
        private float _frameDuration = 0.1f;
        private bool _cleanBackground = true;
        private bool _cameraAutoFit = true;
        private const string _helpBox = "1. Drag and drop your particle system into the Particle System field." +
            "\n2. Specify a file name for the PNGs you want to capture." +
            "\n3. Specify a folder name to save the files. You can also include subfolders by using the forward slash (/) character. If the folder doesn't exist, the plugin will create it automatically and save all the files in it." +
            "\n4. Determine the number of frames you want to capture." +
            "\n5. Set the duration between frames capturing." +
            "\n6. Enable (Camera Auto Size) to optimize PNG capture by adjusting the camera size to fit the particle system, reducing overdraw.";


        private float startTime;
        private bool showTabContent;

        [MenuItem("Tools/ND Toolbox/Optimization/Particles System To PNGs")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ParticlesSystemToPNGWin));
        }

        private void OnGUI()
        {
            GUIStyle headLine = new GUIStyle(GUI.skin.label);
            headLine.fontSize = 20;
            headLine.fontStyle = FontStyle.Bold;

            GUILayout.Label("Capture Settings", headLine);
            GUILayout.Space(20);
            GUIContent particleSystem = new GUIContent("   Particles System", EditorGUIUtility.IconContent("Particle Effect").image);
            _particlesSystem = EditorGUILayout.ObjectField(particleSystem, _particlesSystem, typeof(ParticleSystem), true) as ParticleSystem;
            GUILayout.Space(5);
            GUIContent width = new GUIContent("   Width", EditorGUIUtility.IconContent("d_RectTool On").image);
            _width = EditorGUILayout.IntField(width, _width);
            GUIContent height = new GUIContent("   Height", EditorGUIUtility.IconContent("d_RectTool On").image);
            _height = EditorGUILayout.IntField(height, _height);
            GUILayout.Space(5);
            GUIContent fileName = new GUIContent("   Files Name", EditorGUIUtility.IconContent("d_RawImage Icon").image);
            _fileName = EditorGUILayout.TextField(fileName, _fileName);
            GUIContent folderName = new GUIContent("   Folder Name", EditorGUIUtility.IconContent("d_FolderOpened Icon").image);
            _folderName = EditorGUILayout.TextField(folderName, _folderName);
            GUILayout.Space(5);
            GUIContent numFrames = new GUIContent("   Num. of Frames", EditorGUIUtility.IconContent("PreTextureArrayFirstSlice").image);
            _numFrames = EditorGUILayout.IntField(numFrames, _numFrames);
            GUIContent frameTime = new GUIContent("   Frame Duration", EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow").image);
            _frameDuration = EditorGUILayout.FloatField(frameTime, _frameDuration);
            GUILayout.Space(5);
            GUIContent cleanBackground = new GUIContent("   Clean Background", EditorGUIUtility.IconContent("d_RectMask2D Icon").image);
            _cleanBackground = EditorGUILayout.Toggle(cleanBackground, _cleanBackground);
            GUIContent cameraAutoSize = new GUIContent("   Camera auto size", EditorGUIUtility.IconContent("d_ScaleTool On").image);
            _cameraAutoFit = EditorGUILayout.Toggle(cameraAutoSize, _cameraAutoFit);

            GUILayout.Space(10);
            GUIStyle greenButtonStyle = new GUIStyle(GUI.skin.button);
            greenButtonStyle.normal.textColor = Color.cyan;
            greenButtonStyle.hover.textColor = Color.green;
            greenButtonStyle.fontSize = 20;
            greenButtonStyle.fontStyle = FontStyle.Bold;

            GUIContent captureButtonContent = new GUIContent("   CAPTURE", EditorGUIUtility.IconContent("Animation.Record@2x").image);
            if (GUILayout.Button(captureButtonContent, greenButtonStyle, GUILayout.Height(50)))
            {
                Capture();
            }

            showTabContent = EditorGUILayout.Foldout(showTabContent, "How to use ?");
            if (showTabContent)
            {
                EditorGUILayout.HelpBox(_helpBox, MessageType.Info);
            }

            GUIStyle linkButton = new GUIStyle(GUI.skin.button);
            linkButton.hover.textColor = Color.green;
            linkButton.fontSize = 8;
            if (GUILayout.Button("NikDorn.com", linkButton))
            {
                Application.OpenURL("https://nikdorn.com/");
            }
        }

        private void Capture()
        {
            Camera newCamera = new GameObject("CaptureCamera", typeof(Camera)).GetComponent<Camera>();

            // Set up camera properties
            newCamera.clearFlags = CameraClearFlags.Color; // Remove skybox
            newCamera.backgroundColor = Color.clear; // Transparent background
            newCamera.orthographic = true;
            newCamera.nearClipPlane = 0.01f;
            newCamera.farClipPlane = 100f;

            string folderPath = Application.dataPath + "/" + _folderName;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            startTime = Time.time;
            float timePerFrame = _particlesSystem.main.duration / (float)_numFrames;
            RenderTexture renderTexture = new RenderTexture(_width, _height, 32, RenderTextureFormat.ARGB32);
            _particlesSystem.gameObject.SetActive(true);

            if (_cleanBackground)
            {
                int particleLayer = LayerMask.NameToLayer("PNGs");
                _particlesSystem.gameObject.layer = particleLayer;
                foreach (var renderer in _particlesSystem.GetComponentsInChildren<ParticleSystemRenderer>())
                {
                    renderer.gameObject.layer = particleLayer;
                }
                newCamera.cullingMask = 1 << particleLayer;
            }

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
                    newCamera.orthographicSize = cameraSize;
                    newCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
                }
            }

            newCamera.targetTexture = renderTexture;

            for (int i = 0; i < _numFrames; i++)
            {
                EditorUtility.DisplayProgressBar("Capturing Frames", $"Frame {i} of {_numFrames}", (float)i / _numFrames);

                _particlesSystem.Simulate(_frameDuration, true, false);
                _particlesSystem.Play();
                newCamera.Render();

                RenderTexture.active = renderTexture;
                Texture2D texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
                texture.Apply();

                byte[] bytes = texture.EncodeToPNG();
                string filePath = Path.Combine(folderPath, $"{_fileName}_{i}.png");
                File.WriteAllBytes(filePath, bytes);
                Debug.Log($"Saved frame {i} to {filePath}");

                RenderTexture.active = null;
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            DestroyImmediate(newCamera.gameObject);
        }


#endif
    }

}
