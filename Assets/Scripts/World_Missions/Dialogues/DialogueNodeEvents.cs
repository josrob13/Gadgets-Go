using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Unity.Cinemachine;

public class DialogueNodeEvents : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public DialogueNode node;
        public UnityEvent onEnter;
    }

    [SerializeField] private List<Entry> entries = new();
    private Dictionary<DialogueNode, UnityEvent> map;

    void Awake()
    {
        map = new(entries.Count);
        foreach (var e in entries)
            if (e.node != null && e.onEnter != null)
                map[e.node] = e.onEnter;

        Debug.Log($"[RASTREADOR] DialogueNodeEvents ha inicializado su mapa con {map.Count} entradas.");
        Debug.Log($"[RASTREADOR] Detalles del mapa:");
        foreach (var kvp in map)
        {
            DialogueNode node = kvp.Key;
            UnityEvent ev = kvp.Value;
            Debug.Log($"- Nodo: '{node.name}' tiene {ev.GetPersistentEventCount()} funciones conectadas.");
            Debug.Log($"  Funciones conectadas:");
            for (int i = 0; i < ev.GetPersistentEventCount(); i++)            {
                string targetName = ev.GetPersistentTarget(i)?.name ?? "null";
                string methodName = ev.GetPersistentMethodName(i) ?? "null";
                Debug.Log($"    {i + 1}. Target: '{targetName}', Method: '{methodName}'");
            }
        }
    }

    public void InvokeFor(DialogueNode node)
    {
        Debug.Log("ENTRA A INVOKE FOR");
        // out var ev => try to access to the value of the node, i.e., to the actions
        // related to that node, keep this in 'ev', and then call to its Invoke()
        if (node != null && map != null && map.TryGetValue(node, out var ev))
        {
            Debug.Log($"[RASTREADOR] Nodo encontrado. Este evento tiene {ev.GetPersistentEventCount()} funciones conectadas en el Inspector.");
            ev.Invoke();
            Debug.Log("Uno ha sido invocado...");
        }
        else if (node == null)
        {
            Debug.LogWarning("[RASTREADOR] El nodo proporcionado es null. No se puede invocar ningún evento.");
        }
        else if (map == null)
        {
            Debug.LogWarning("[RASTREADOR] El mapa de eventos no ha sido inicializado. Asegúrate de que el método Awake() se haya ejecutado correctamente.");
        }
        else
        {
            Debug.LogWarning($"[RASTREADOR] No se encontró ningún evento para el nodo '{node.name}'. Verifica que el nodo esté correctamente asignado en la lista de entradas.");
        }
    }

    public void AddPriorityCamera(CinemachineCamera camera)
    {
        Debug.Log("ENTRA A ADD PRIORITY CAMERA - " + camera.name);
        camera.gameObject.SetActive(true);
        camera.Priority = 5;
    }

    public void QuitCamera(CinemachineCamera camera)
    {
        Debug.Log("ENTRA A QUIT CAMERA - " + camera.name);
        camera.Priority = 0;
        camera.gameObject.SetActive(false);
    }
}
