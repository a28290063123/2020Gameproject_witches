﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class OnInteract : MonoBehaviour
{
    
    /// <summary>
    ///     Data struct which contains needed data for <c>CustomEvent</c>
    ///     <value>
    ///         <para>(Need Implement)<c>path</c> is for needed file path</para>
    ///         <para>gameObject is for target <c>gameObject</c> of the Event</para>
    ///         <para>vec if for target <c>Vector2</c> of the Event</para>
    ///         <para>removeAfterDone is a mark for one time Event</para>
    ///     </value>
    /// </summary>
    [Serializable]
    public struct DataStruct
    {
        [CanBeNull] public String path;
        [CanBeNull] public GameObject gameObject;
        [CanBeNull] public Vector2 vec;
        public bool removeAfterDone;
    }
    
    [Serializable]
    public struct EventsStruct
    {
        [StringInList(typeof(OnInteract),"GetEvents")]
        public List<String> EventsList;
        public List<DataStruct> Data;
    }
    
    [SerializeField]
    private EventsStruct EventsData;
    public Queue<String> EventsQueue=new Queue<String>();

    private bool EventDone=true;
    private int DataIndex=0;
    private LayerMask playerLayer;
    public GameObject Hint;
    [CanBeNull] public GameObject HintObject;

    private Renderer shader;
    private Material mat;
    private int shaderID;
    public float maxEffectStrength = 0.04f;
    public bool canInteract = true;

    private PlayerManager player;
    
    //get all CustomEvents using Reflection
    public static String[] GetEvents()
    {
        String name = nameof(CustomEventNamespace);
        var clazz = from t in Assembly.GetExecutingAssembly().GetTypes()
            where t.IsClass && t.Namespace == name
            select t.ToString();
        return clazz.Select(n=>n.Replace("CustomEventNamespace.","")).ToArray();
    }

    private void Start()
    {
        playerLayer = PlayerManager.instance.layer;
        HintObject = HintObject == null ? gameObject : HintObject;
        shader = HintObject.GetComponent<SpriteRenderer>();
        mat = shader.material;
        player = PlayerManager.instance;
        shaderID = Shader.PropertyToID("Vector1_4C8E13CA");
        mat.SetFloat(shaderID, 0);
        canInteract = true;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        //check if gameObject layer is in playerLayer
        if (((1 << col.gameObject.layer) & playerLayer) != 0 && canInteract)
        {
            mat.SetFloat(shaderID, maxEffectStrength);
            Hint.SetActive(true);
        }
    }

    void OnTriggerStay2D(Collider2D col)
    {
        if (((1 << col.gameObject.layer) & playerLayer) != 0 && canInteract)
        {
            if (EventsQueue.Count==0 && player.input.GetKeyDown(InputAction.Interact))
            {
                player.input.StopInput(true);
                Hint.SetActive(false);
                DataIndex = 0;
                foreach (String e in EventsData.EventsList)
                    EventsQueue.Enqueue(e);
                if (EventsQueue.Count!=0) 
                    ExecuteEvent();
            }
        }
        
    }
    private void OnTriggerExit2D(Collider2D col)
    {
        if (((1 << col.gameObject.layer) & playerLayer) != 0 && canInteract)
        {
            mat.SetFloat(shaderID, 0);
            Hint.SetActive(false);
        }
    }
    
    
    public void ExecuteEvent()
    {   
        if(!EventDone || EventsQueue.Count == 0) return;
        EventDone = false;
        player.input.StopInput(true);
        Type t = Type.GetType("CustomEventNamespace." + EventsQueue.Peek());
        ICustomEvent _event =(ICustomEvent)Activator.CreateInstance(t,EventsData.Data[DataIndex]);
        _event.StartEvent(this);
    }

    public void SetEventDone(bool success)
    {
        EventDone = true;
        player.input.StopInput(false);
        if (success)
        {
            EventsQueue.Dequeue();
            if (EventsData.Data[DataIndex].removeAfterDone)
            {
                EventsData.EventsList.RemoveAt(DataIndex);
                EventsData.Data.RemoveAt(DataIndex);
            }else DataIndex++;
            if (EventsData.EventsList.Count == 0)
            {
                    mat.SetFloat(shaderID,0);
                    Hint.SetActive(false);
                    canInteract = false; 
            }else ExecuteEvent();
        }
        else EventsQueue.Clear();
    }
    
    public OnInteract AddEvent(String e,DataStruct data)
    {
        if (GetEvents().Contains(e))
        {
            EventsData.EventsList.Add(e);
            EventsData.Data.Add(data);
        }
        else Debug.LogWarning("[Warning] No event called "+e+", skip");
        return this;

    }

    public OnInteract AddEvent(ICustomEvent e, DataStruct data)
    {
        String str = e.ToString().Replace("CustomEventNamespace.", "");
        EventsData.EventsList.Add(str);
        EventsData.Data.Add(data);
        return this;
    }
    public OnInteract ClearEvent()
    {
        EventsData.EventsList.Clear();
        EventsData.Data.Clear();
        return this;
    }

}
