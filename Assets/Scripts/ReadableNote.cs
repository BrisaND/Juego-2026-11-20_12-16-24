using UnityEngine;

public class ReadableNote : MonoBehaviour
{
    [Header("Configuración de la Nota")]
    [SerializeField] private string promptAction = "Leer nota";

    [TextArea(5, 15)]
    [SerializeField] private string noteText = "Escribí aquí el contenido de la nota...";

    public string GetPromptAction()
    {
        return promptAction;
    }

    public void Read()
    {
        if (NoteUIController.Instance != null)
        {
            NoteUIController.Instance.OpenNote(noteText);
        }
    }
}