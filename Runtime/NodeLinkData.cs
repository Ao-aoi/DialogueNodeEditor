using System;

namespace DialogueNodeEditor
{
[Serializable]
public class NodeLinkData
{
    public string PortName;
    public string TargetPortName;
    public string BaseNodeGuid;
    public string TargetNodeGuid;
}
}