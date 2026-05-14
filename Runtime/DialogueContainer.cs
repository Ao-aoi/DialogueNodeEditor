using System.Collections.Generic;
using UnityEngine;
namespace DialogueNodeEditor
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "DialogueEditor/Dialogue Container")]
    public class DialogueContainer : ScriptableObject
    {
        public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
        public List<DialogueNodeData> DialogueNodeData = new List<DialogueNodeData>();
    }
}