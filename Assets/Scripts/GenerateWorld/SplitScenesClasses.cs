using System.Collections.Generic;

public class SplitScenesClasses
{
    // Clases para mapear el JSON
    public class Escena
    {
        public string Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public Elementos Elementos { get; set; }
    }

    public class Elementos
    {
        public List<sprites> SpritesPrincipales { get; set; }
        public List<NPC> NPCs { get; set; }
        public List<objetosInteractivos> ObjetosInteractivos { get; set; }
        public List<ScriptEvento> ScriptsEventos { get; set; }
    }

    public class NPC
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string DescripcionFisica { get; set; }
        public string Funcion { get; set; }
    }

    public class ScriptEvento
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string ObjetoAsociado { get; set; }

    }

    public class sprites
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Ruta { get; set; }
        public string Descripcion { get; set; }
    }

    public class objetosInteractivos
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string RutaSprite { get; set; }
    }
}
