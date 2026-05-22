using System;

namespace DialogueNodeEditor
{
[Serializable]
public class NodeLinkData
{
    public string PortName;
    public string TargetPortName;
    public int TargetPortIndex = -1;
    public string BaseNodeGuid;
    public string TargetNodeGuid;
}
}