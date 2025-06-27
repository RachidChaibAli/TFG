using System.Collections.Generic;

public class SplitScenesClasses
{
    // Clases para mapear el JSON
    public class Escena
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Elementos Elementos { get; set; }
    }

    public class Elementos
    {
        public List<NPC> NPCs { get; set; }
        public List<InteractiveObject> InteractiveObjects { get; set; }
        public List<Sprites> MainSprites { get; set; }
        public List<EventScript> EventScripts { get; set; }
    }

    public class NPC
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
    }

    public class EventScript
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AssociatedObject { get; set; }

    }

    public class Sprites
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Ruta { get; set; }
        public string Description { get; set; }
        public string AssociatedObject { get; set; }
    }

    public class InteractiveObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
