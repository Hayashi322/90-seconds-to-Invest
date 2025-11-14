using UnityEngine;

public class tutorial : MonoBehaviour
{
    [SerializeField] private CanvasGroup[] canvases;
    public void step_1()
    {
      //  canvases[0].alpha = 0 ;
      //  canvases[1].alpha = 1;
       // canvases[2].alpha = 0;
        canvases[0].gameObject.SetActive(false);
        canvases[1].gameObject.SetActive(true);
        canvases[2].gameObject.SetActive(false);
        // canvases[1].enabled = false;
        // canvases[3].enabled = false;
    }
    public void step_2() 
    {
        canvases[0].gameObject.SetActive(false);
        canvases[1].gameObject.SetActive(false);
        canvases[2].gameObject.SetActive(true);
        /* canvases[0].alpha = 0;
         canvases[1].alpha = 0;
         canvases[2].alpha = 1;*/
    }
    public void step_3()
    {
        close();
    }
    public void close ()
    {
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].gameObject.SetActive(false);
        }
    }

}
