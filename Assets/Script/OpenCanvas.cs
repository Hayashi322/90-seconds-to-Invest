using Unity.VisualScripting;
using UnityEngine;

public class OpenCanvas : MonoBehaviour
{
    public CanvasGroup[] canvas;
    
   public void openCanvas(int number)
    {
        Debug.Log("opencanvas");
        canvas[number].alpha = 1;
        canvas[number].blocksRaycasts = true;
        canvas[number].interactable = true;
    }

    ///////////////////////////////////////////////////////

    public void closeCanvas()
    {
        for (int i = 0; i < canvas.Length; i++)  //�Դ˹�ҵ�ҧ������
        {
            canvas[i].alpha = 0;
            canvas[i].blocksRaycasts = false;
            canvas[i].interactable = false;
        }

    }
    /* public void openCanvas1()
     {
         Debug.Log("opencanvas");              
         canvas[0].alpha = 1;                   //������ͧ���canvas
         canvas[0].blocksRaycasts = true;       //������ԧ������ ������canvas
         canvas[0].interactable = true;         //���������ö�������canvas��
     }
     public void openCanvas2()
     {
         Debug.Log("opencanvas");
         canvas[1].alpha = 1;
         canvas[1].blocksRaycasts = true;
         canvas[1].interactable = true;
     }
     public void openCanvas3()
     {
         Debug.Log("opencanvas");
         canvas[2].alpha = 1;
         canvas[2].blocksRaycasts = true;
         canvas[2].interactable = true;
     }
     public void openCanvas4()
     {
         Debug.Log("opencanvas");
         canvas[3].alpha = 1;
         canvas[3].blocksRaycasts = true;
         canvas[3].interactable = true;
     }
     public void openCanvas5()
     {
         Debug.Log("opencanvas");
         canvas[4].alpha = 1;
         canvas[4].blocksRaycasts = true;
         canvas[4].interactable = true;
     }
     public void openCanvas6()
     {
         Debug.Log("opencanvas");
         canvas[5].alpha = 1;
         canvas[5].blocksRaycasts = true;
         canvas[5].interactable = true;
     }
     public void openCanvas7()
     {
         Debug.Log("opencanvas");
         canvas[6].alpha = 1;
         canvas[6].blocksRaycasts = true;
         canvas[6].interactable = true;
     }
     public void openCanvas8()
     {
         Debug.Log("opencanvas");
         canvas[7].alpha = 1;
         canvas[7].blocksRaycasts = true;
         canvas[7].interactable = true;
     }*/






}
