using System.Collections.Generic;
using UnityEngine;

public class ScenePosition
{
    public SceneSize sceneSize { get; set; }
    public List<PositionEntry> positions { get; set; }
}

public class SceneSize
{
    public int width { get; set; }
    public int height { get; set; }
}

public class PositionEntry
{
    public string id { get; set; }
    public Position position { get; set; }
}

public class Position
{
    public int x { get; set; }
    public int y { get; set; }
}
