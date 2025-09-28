using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

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
    }

    public void InvokeFor(DialogueNode node)
    {
        Debug.Log("ENTRA A INVOKE FOR");
        // out var ev => try to access to the value of the node, i.e., to the actions
        // related to that node, keep this in 'ev', and then call to its Invoke()
        if (node != null && map != null && map.TryGetValue(node, out var ev))
        {
            ev.Invoke();
            Debug.Log("Uno ha sido invocado...");
        }
    }

    public void Prueba()
    {
        Debug.Log("eventooooooooooooooooooooos :)");
    }
}
