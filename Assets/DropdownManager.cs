using System;
using System.Collections;
using System.Collections.Generic;
using Graph;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DropdownManager : MonoBehaviour
{
    private TMP_Dropdown dropdown = null;
    [SerializeField] private WaypointController waypointController;
    [SerializeField] private HeroController heroController;

    [SerializeField] private bool isSetStartnode = true;

    // Start is called before the first frame update
    void Start()
    {
        dropdown = gameObject.GetComponent<TMP_Dropdown>();

        //Add listener for when the value of the Dropdown changes, to take action
        dropdown.onValueChanged.AddListener(delegate {
            DropdownValueChanged(dropdown);
        });

        rebuildDropdown();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void rebuildDropdown()
    {
        if(waypointController == null)
            return;

        dropdown.ClearOptions();

        List<GraphNode> nodes = waypointController.GetWaypointGraph().GetAllNodes();
        foreach(GraphNode n in nodes)
        {
            var option = new TMP_Dropdown.OptionData(n.GameObjectNode.name);
            dropdown.options.Add(option);
        }

        dropdown.RefreshShownValue();
    }

    void DropdownValueChanged(TMP_Dropdown change)
    {
        if(isSetStartnode)
        {
            heroController.SetStartNode(change.value);
            Debug.Log("Set StartNode : " + change.value);
        }
        else
        {
            heroController.SetTargetNode(change.value);
            Debug.Log("Set TargetNode : " + change.value);
        }
    }
}
