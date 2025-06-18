using System.Collections.Generic;

public class SplitScenesClasses
{
    // Clases para mapear el JSON
    public class Escena
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public Elementos Elementos { get; set; }
    }

    public class Elementos
    {
        public List<string> SpritesPrincipales { get; set; }
        public List<NPC> NPCs { get; set; }
        public List<string> ObjetosInteractivos { get; set; }
        public List<ScriptEvento> ScriptsEventos { get; set; }
    }

    public class NPC
    {
        public string Nombre { get; set; }
        public string DescripcionFisica { get; set; }
        public string Funcion { get; set; }
    }

    public class ScriptEvento
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string ObjetoAsociado { get; set; }

    }
}
