#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Plugins.Machination.Notepad
{
    public class Notepad : EditorWindow, IModelObserver
    {
        private NotepadModel _model;
        private GUIStyle _textAreaStyle;
        private Font _customFont;
        private Vector2 _scrollPosition;
        private int _fontSize = 14;
        private Texture2D _reloadButtonTexture;
        private Texture2D _newFileButtonTexture;
        private GUIStyle _buttonStyle;

        private NotepadSettings _notepadSettings;

        [MenuItem(NotepadConstants.MenuDir + "Open Notepad", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<Notepad>(NotepadConstants.NotepadTitle);
            var icon = (Texture2D)AssetDatabase.LoadAssetAtPath(NotepadConstants.NotepadFolder + "/Resources/icon.png", typeof(Texture2D));
            window.titleContent = icon != null ? new GUIContent(NotepadConstants.NotepadTitle, icon) : new GUIContent(NotepadConstants.NotepadTitle);
        }

        private void OnEnable()
        {
            if (_model == null)
            {
                _model = new NotepadModel(this);
                _model.Init();
            }

            LoadTextures();
            LoadColorSettings();
            EditorApplication.quitting += OnEditorQuitting;
        }

        private void OnDisable()
        {
            EditorApplication.quitting -= OnEditorQuitting;
        }

        private void OnDestroy()
        {
            _model.OnDestroy();
        }

        private void OnGUI()
        {
            _model ??= new NotepadModel(this);

            HandleShortcuts();
            LoadCustomFont();
            SetupStyles();

            EditorGUILayout.BeginHorizontal();
            RenderFileSelection();
            if (GUILayout.Button(new GUIContent(_reloadButtonTexture, "Reload files"), _buttonStyle)) { _model.LoadFiles(); }
            if (GUILayout.Button(new GUIContent(_newFileButtonTexture, "Create new file"), _buttonStyle)) { _model.CheckForUnsavedChangesBeforeCreatingNewFile(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            RenderFontSizeInput();
            EditorGUILayout.EndHorizontal();

            RenderTextArea();
        }

        public void ModelUpdated()
        {
            UpdateWindowTitle();
            Repaint();
        }

        private void UpdateWindowTitle()
        {
            titleContent.text = NotepadConstants.NotepadTitle + (_model.HasUnsavedChanges ? " *" : "");
        }

        private void RenderTextArea()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var newText = EditorGUILayout.TextArea(_model.Text, _textAreaStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            _model.UpdateTextIfChanged(newText);
        }

        private void RenderFileSelection()
        {
            GUILayout.Label(NotepadConstants.SelectFile);
            var newSelectedFileIndex = EditorGUILayout.Popup(_model.SelectedFileIndex, _model.Files.AsEnumerable().ToArray());
            _model.SelectFileFromList(newSelectedFileIndex);
        }
        
        private void RenderFontSizeInput()
        {
            GUILayout.Label(NotepadConstants.FontSizeLabel);
            _fontSize = EditorGUILayout.IntSlider(_fontSize, 10, 30);
        }

        private void HandleShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || (!e.control && !e.command) || e.keyCode != KeyCode.S) return;
            e.Use();
            _model.SaveTextToFile();
        }

        private void LoadCustomFont()
        {
            if (_notepadSettings.useCustomFont)
            {
                var fontPath = AssetDatabase.FindAssets("t:Font", new[] { "Assets/Plugins/Machination/Notepad/Fonts" })
                    .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(path =>
                        System.IO.Path.GetFileNameWithoutExtension(path) == _notepadSettings.selectedFont);
                if (!string.IsNullOrEmpty(fontPath))
                {
                    _customFont = (Font)AssetDatabase.LoadAssetAtPath(fontPath, typeof(Font));
                }
                if (_customFont)
                {
                    _textAreaStyle = new GUIStyle(GUI.skin.textArea)
                    {
                        font = _customFont, fontSize = _fontSize,
                        normal = { textColor = _notepadSettings.textColor, background = MakeTex(1, 1, _notepadSettings.backgroundColor) }
                    };
                }
                else
                {
                    Debug.LogError(NotepadConstants.FontLoadError);
                    _textAreaStyle = new GUIStyle(GUI.skin.textArea)
                    {
                        normal =
                        {
                            textColor = _notepadSettings.textColor,
                            background = MakeTex(1, 1, _notepadSettings.backgroundColor)
                        }
                    };
                }
            }
            else
            {
                _textAreaStyle = new GUIStyle(GUI.skin.textArea)
                {
                    normal =
                    {
                        textColor = _notepadSettings.textColor, background = MakeTex(1, 1, _notepadSettings.backgroundColor)
                    }
                };
            }
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var size = new Color[width * height];
            for (var i = 0; i < size.Length; i++)
            {
                size[i] = col;
            }
            var result = new Texture2D(width, height);
            result.SetPixels(size);
            result.Apply();
            return result;
        }

        private void LoadTextures()
        {
            _reloadButtonTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(NotepadConstants.NotepadFolder + "/Resources/reload.png", typeof(Texture2D));
            _newFileButtonTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(NotepadConstants.NotepadFolder + "/Resources/newFile.png", typeof(Texture2D));
        }

        private void SetupStyles()
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 24,
                fixedHeight = 24,
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        private void OnEditorQuitting()
        {
            _model.CheckForUnsavedChanges();
        }

        private void LoadColorSettings()
        {
            _notepadSettings =
                AssetDatabase.LoadAssetAtPath<NotepadSettings>(NotepadConstants.ColorSettingsDir);
            if (_notepadSettings == null)
            {
                Debug.LogError("Notepad::ColorSettings is NULL");
            }
        }
    }
}
#endif