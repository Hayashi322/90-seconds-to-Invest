using UnityEngine;

public class openPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        panel.SetActive(true);
    }
}
