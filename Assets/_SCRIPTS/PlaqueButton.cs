﻿using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaqueButton : MonoBehaviour
{
    void Update()
    {
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit raycastHit;
            if (Physics.Raycast(raycast, out raycastHit))
            {
                if (raycastHit.collider.name == name)
                {
                    if (SceneManager.GetActiveScene().name == "Viewfinder")
                    {
                        FindObjectOfType<ContentUI>().ShowSelf(GlobalDatabase.Instance.FindVoiceByName("Gabby"));
                    }
                    else
                    {
                        SceneManager.LoadScene("PrototypeMode3", LoadSceneMode.Single);
                    }
                }
            }
        }
    }
}
