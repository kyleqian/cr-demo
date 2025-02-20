﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.iOS;

public class ViewfinderUI : MonoBehaviour
{
    [SerializeField] AnchoringUI anchoringUI;
    [SerializeField] ContentUI contentUI;

    public void HomeButton()
    {
        SceneManager.LoadScene("Title", LoadSceneMode.Single);
    }

    public void ResetButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    void Start()
    {
        UnityARSessionNativeInterface.ARUserAnchorAddedEvent += UnityARSessionNativeInterface_ARUserAnchorAddedEvent;
    }

    void Update()
    {
        DetectLetterTouch();
    }

    void OnDestroy()
    {
        UnityARSessionNativeInterface.ARUserAnchorAddedEvent -= UnityARSessionNativeInterface_ARUserAnchorAddedEvent;
    }

    void UnityARSessionNativeInterface_ARUserAnchorAddedEvent(ARUserAnchor anchorData)
    {
        // Hide anchoring canvas now that anchoring is achieved
        anchoringUI.FadeOut();
    }

    void DetectLetterTouch()
    {
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && !IsPointerOverUIObject())
        {
            Ray raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            if (Physics.Raycast(raycast, out RaycastHit raycastHit))
            {
                contentUI.HideSelf();
                switch (raycastHit.collider.tag)
                {
                    case "LetterCups":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("SL"));
                        break;
                    case "LetterFlashlight":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("AK"));
                        break;
                    case "LetterFlower":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("JR"));
                        break;
                    case "LetterPlaque":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("ED1"));
                        break;
                    case "LetterPlaqueBig":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("ED2"));
                        break;
                    case "LetterMegaphone":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("SP"));
                        break;
                    case "LetterKA":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("NH"));
                        break;
                    case "LetterGavel1":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("JT"));
                        break;
                    case "LetterGavel2":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("SK"));
                        break;
                    case "LetterCereal":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("MS"));
                        break;
                    case "LetterPaper":
                        contentUI.ShowSelf(GlobalDatabase.Instance.FindVoiceByName("WTSD"));
                        break;
                }
            }
        }
    }

    bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}
