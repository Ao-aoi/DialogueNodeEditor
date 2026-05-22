using System.Collections.Generic;
using UnityEngine;

namespace DialogueNodeEditor
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "DialogueEditor/Dialogue Container")]
    public class DialogueContainer : ScriptableObject
    {
        [Tooltip("文字表示完了後に次ページへ進むまでの待機時間（秒）")]
        public float AdvanceDelay = 2.0f;

        public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
        public List<DialogueNodeData> DialogueNodeData = new List<DialogueNodeData>();
        
        public List<CharacterNodeData> CharacterNodeData = new List<CharacterNodeData>();
        public List<PortraitNodeData> PortraitNodeData = new List<PortraitNodeData>();
        public List<StillNodeData> StillNodeData = new List<StillNodeData>();
        public List<PanelSettingNodeData> PanelSizeNodeData = new List<PanelSettingNodeData>();
        public List<StartNodeData> StartNodeData = new List<StartNodeData>();
        public List<EndNodeData> EndNodeData = new List<EndNodeData>();

        public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
        public List<PropertyNodeData> PropertyNodeData = new List<PropertyNodeData>();
    }
}