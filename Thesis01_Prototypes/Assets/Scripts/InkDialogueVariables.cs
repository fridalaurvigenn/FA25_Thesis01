using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;

using InkObj = Ink.Runtime.Object;
using InkVal = Ink.Runtime.Value;

public class InkDialogueVariables
{
    //Cache of GLOBAL variables from globals.ink
    private readonly Dictionary<string, InkObj> _vars = new();
    private readonly Story _globalsStory;

    //Pass in the compiled JSON of Globals.ink
    public InkDialogueVariables(TextAsset globalsJSON)
    {
        _globalsStory = new Story(globalsJSON.text);

        //Cache every global's initial value
        foreach (string name in _globalsStory.variablesState)
        {
            InkObj value = _globalsStory.variablesState.GetVariableWithName(name);
            _vars[name] = value;
        }
    }

    //Push cached globals into a fresh story and listen for changes
    public void StartListening(Story story)
    {
        foreach (var kv in _vars)
        {
            var name = kv.Key;
            var obj = kv.Value;

            // All globals (including lists) come through as Ink.Runtime.Value.
            if (obj is InkVal v)
            {
                // v.valueObject is bool/int/float/string or an InkList instance
                story.variablesState[name] = v.valueObject;
            }
            else
            {
                Debug.LogWarning($"[Ink] Unsupported global '{name}' type {obj?.GetType().Name}");
            }
        }

        story.variablesState.variableChangedEvent += OnVariableChanged;
    }

    public void StopListening(Story story)
    {
        story.variablesState.variableChangedEvent -= OnVariableChanged;
    }

    //Keep cache up to date when Ink globals change
    private void OnVariableChanged(string name, InkObj value)
    {
        if (_vars.ContainsKey(name))
        {
            _vars[name] = value;
            //Debug useful for greeted flag
            if (name == "greeted_elise")
                Debug.Log($"[Ink] greeted_elise changed -> {value}");
        }
    }

    //Optional: read a cached global from Unity
    public T Get<T>(string name)
    {
        if (_vars.TryGetValue(name, out var obj) && obj is InkVal v)
            return (T)v.valueObject;
        return default;
    }
}
