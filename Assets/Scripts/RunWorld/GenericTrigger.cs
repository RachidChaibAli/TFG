using UnityEngine;

public class GenericTrigger : MonoBehaviour
{
    // Tag del objeto que activa el trigger 
    public string triggerTag = "player";
    // Referencia al orquestador central 
    public GameOrquestrator orquestrator;

    private void Awake()
    {
        // Si no hay orquestador asignado, intenta encontrarlo automáticamente
        if (orquestrator == null)
            orquestrator = GameOrquestrator.Instance;
    }

    // Detección de entrada en trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            // Notifica al orquestador central: origen = este objeto, destino = objeto que entra, tipo = "OnTriggerEnter"
            orquestrator.RegisterTrigger(gameObject, other.gameObject, "OnTriggerEnter");
        }
    }

    // Detección de salida de trigger (opcional)
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            // Notifica al orquestador central: origen = este objeto, destino = objeto que sale, tipo = "OnTriggerExit"
            orquestrator.RegisterTrigger(gameObject, other.gameObject, "OnTriggerExit");
        }
    }
}
