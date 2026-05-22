using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Linq;

namespace DialogueNodeEditor
{
    public class DialogueGraphWindow : EditorWindow
    {
        private DialogueGraphView _graphView;
        [SerializeField] private string _currentAssetPath = "";
        private Label _pathLabel;

        public DialogueContainer CurrentContainer { get; private set; }

        private const string LastOpenedContainerKey = "DialogueNodeEditor.LastOpenedContainerPath";

        [MenuItem("Window/Dialogue Node Editor")]
        public static void OpenDialogueGraphWindow()
        {
            // アセット指定なしで開く場合は従来通り
            var window = CreateInstance<DialogueGraphWindow>();
            window.titleContent = new GUIContent("Dialogue Editor");
            window.Show();
        }

        public static DialogueGraphWindow OpenDialogueGraphWindow(string assetPath)
        {
            // 既存ウィンドウを探す
            var windows = Resources.FindObjectsOfTypeAll<DialogueGraphWindow>();
            foreach (var win in windows)
            {
                if (win._currentAssetPath == assetPath)
                {
                    win.Focus();
                    return win;
                }
            }
            // 新規作成
            var window = CreateInstance<DialogueGraphWindow>();
            window._currentAssetPath = assetPath;
            window.titleContent = new GUIContent(System.IO.Path.GetFileName(assetPath));
            window.Show();
            return window;
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var container = EditorUtility.InstanceIDToObject(instanceID) as DialogueContainer;
            if (container != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(container);
                var window = OpenDialogueGraphWindow(assetPath);
                window.LoadGraphFromAsset(container, autoRestore: false);
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            // アセットパスがあれば自動復元
            if (!string.IsNullOrEmpty(_currentAssetPath))
            {
                var container = AssetDatabase.LoadAssetAtPath<DialogueContainer>(_currentAssetPath);
                if (container != null)
                {
                    LoadGraphFromAsset(container, autoRestore: true);
                }
            }
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.actionKey && evt.keyCode == KeyCode.S)
            {
                SaveData();
                evt.StopPropagation();
            }
            // ★追加: Ctrl+D で複製
            else if (evt.actionKey && evt.keyCode == KeyCode.D)
            {
                _graphView.DuplicateNodes();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Space)
            {
                var searchWindow = ScriptableObject.CreateInstance<DialogueSearchWindow>();
                searchWindow.Init(this, _graphView, null);
                SearchWindow.Open(new SearchWindowContext(evt.originalMousePosition), searchWindow);
                evt.StopPropagation();
            }
        }

        private void GenerateToolbar()
        {
            var toolbar = new UnityEditor.UIElements.Toolbar();

            _pathLabel = new Label("Unsaved Graph");
            _pathLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _pathLabel.style.width = 250;
            toolbar.Add(_pathLabel);

            toolbar.Add(new Button(() => SaveData()) { text = "Save" });
            toolbar.Add(new Button(() => SaveDataAs()) { text = "Save As..." });
            toolbar.Add(new Button(() => LoadDataDialog()) { text = "Load" });
            toolbar.Add(new Button(() => { _graphView.CreateNode(new DialogueNode(), Vector2.zero); }) { text = "Add Node" });

            toolbar.Add(new Button(() => _graphView.AutoLayoutNodes()) { text = "Auto Layout" });

            rootVisualElement.Add(toolbar);
        }

        private void SaveDataAs()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Dialogue Graph", "New Narrative", "asset", "Please enter a file name to save the dialogue graph to.");
            if (string.IsNullOrEmpty(path)) return;

            _currentAssetPath = path;
            UpdatePathLabel();

            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.SaveGraph(_currentAssetPath);
        }

        private void SaveData()
        {
            if (string.IsNullOrEmpty(_currentAssetPath))
            {
                SaveDataAs();
            }
            else
            {
                var saveUtility = GraphSaveUtility.GetInstance(_graphView);
                saveUtility.SaveGraph(_currentAssetPath);
            }
        }

        private void LoadDataDialog()
        {
            string path = EditorUtility.OpenFilePanel("Load Dialogue Graph", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            var container = AssetDatabase.LoadAssetAtPath<DialogueContainer>(path);
            if (container != null)
            {
                var window = OpenDialogueGraphWindow(path);
                window.LoadGraphFromAsset(container, autoRestore: false);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Selected file is not a Dialogue Container.", "OK");
            }
        }

        public void LoadGraphFromAsset(DialogueContainer container, bool autoRestore = false)
        {
            if (_graphView == null) return;

            CurrentContainer = container;
            _currentAssetPath = AssetDatabase.GetAssetPath(container);
            UpdatePathLabel();

            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            saveUtility.LoadGraph(container);
            // タイトルも更新
            titleContent = new GUIContent(System.IO.Path.GetFileName(_currentAssetPath));
        }

        private void UpdatePathLabel()
        {
            if (_pathLabel != null)
            {
                _pathLabel.text = string.IsNullOrEmpty(_currentAssetPath) ? "Unsaved Graph" : _currentAssetPath;
            }
        }

        private void ConstructGraphView()
        {
            _graphView = new DialogueGraphView(this) { name = "Dialogue Graph" };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);

            GenerateBlackboard();

            // 線をドロップした際に引っ張ったポートがあれば自動で DialogueNode を作成して接続する
            _graphView.nodeCreationRequest = context =>
            {
                Port sourcePort = null;
                if (context.target is Edge edge) sourcePort = edge.output ?? edge.input;
                else if (context.target is Port port) sourcePort = port;

                var windowMousePosition = rootVisualElement.ChangeCoordinatesTo(
                    rootVisualElement.parent,
                    context.screenMousePosition - position.position);
                var graphMousePosition = _graphView.contentViewContainer.WorldToLocal(windowMousePosition);

                // 新しい DialogueNode を作成
                var newNode = new DialogueNode();
                _graphView.CreateNode(newNode, graphMousePosition);

                // 引っ張ってきたポートがあれば自動接続を試みる
                if (sourcePort != null)
                {
                    Port targetPort = null;
                    if (sourcePort.direction == Direction.Output)
                    {
                        targetPort = newNode.inputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portType == sourcePort.portType);
                    }
                    else
                    {
                        targetPort = newNode.outputContainer.Children().OfType<Port>().FirstOrDefault(p => p.portType == sourcePort.portType);
                    }

                    if (targetPort != null)
                    {
                        var newEdge = new Edge
                        {
                            output = sourcePort.direction == Direction.Output ? sourcePort : targetPort,
                            input = sourcePort.direction == Direction.Input ? sourcePort : targetPort
                        };
                        newEdge.input.Connect(newEdge);
                        newEdge.output.Connect(newEdge);
                        _graphView.AddElement(newEdge);
                    }
                }
            };
        
            _graphView.schedule.Execute(() => {
                if (_graphView.nodes.ToList().Count == 0)
                {
                    _graphView.CreateNode(new EndNode(), new Vector2(600, 200));
                }
            }).StartingIn(50);
        }
        
        private void GenerateBlackboard()
        {
            var blackboard = new UnityEditor.Experimental.GraphView.Blackboard(_graphView);
            blackboard.Add(new BlackboardSection { title = "Exposed Properties" });
            blackboard.addItemRequested = _blackboard => {
                _graphView.AddPropertyToBlackBoard(new ExposedProperty());
            };
            
            blackboard.editTextRequested = (blackboard1, element, newValue) => {
                var oldPropertyName = ((BlackboardField)element).text;
                if (_graphView.ExposedProperties.Any(x => x.PropertyName == newValue))
                {
                    EditorUtility.DisplayDialog("Error", "This property name already exists.", "OK");
                    return;
                }
                var propertyIndex = _graphView.ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
                _graphView.ExposedProperties[propertyIndex].PropertyName = newValue;
                ((BlackboardField)element).text = newValue;

                var propNodes = _graphView.nodes.ToList().OfType<PropertyNode>().Where(x => x.PropertyName == oldPropertyName);
                foreach (var propNode in propNodes)
                {
                    propNode.PropertyName = newValue;
                    propNode.title = newValue;
                }
            };

            blackboard.SetPosition(new Rect(10, 30, 200, 300));
            _graphView.Add(blackboard);
            _graphView.Blackboard = blackboard;
        }
    }
}