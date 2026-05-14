using System.Collections.Generic;
using UnityEngine;

namespace DialogueNodeEditor
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "DialogueEditor/Dialogue Container")]
    public class DialogueContainer : ScriptableObject
    {
        public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
        public List<DialogueNodeData> DialogueNodeData = new List<DialogueNodeData>();
        
        public List<CharacterNodeData> CharacterNodeData = new List<CharacterNodeData>();
        public List<PortraitNodeData> PortraitNodeData = new List<PortraitNodeData>();
        public List<StillNodeData> StillNodeData = new List<StillNodeData>();
        public List<PanelSizeNodeData> PanelSizeNodeData = new List<PanelSizeNodeData>();
        public List<EndNodeData> EndNodeData = new List<EndNodeData>();
    }
}