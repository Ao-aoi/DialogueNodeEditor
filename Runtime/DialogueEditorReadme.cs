using UnityEngine;
namespace DialogueNodeEditor
{
    [CreateAssetMenu(fileName = "README", menuName = "DialogueEditor/Create README Asset")]
    public class DialogueEditorReadme : ScriptableObject
    {
        public string Title = "Dialogue Node Editor Usage";
        [TextArea(15, 20)]
        public string Description;
    }
}