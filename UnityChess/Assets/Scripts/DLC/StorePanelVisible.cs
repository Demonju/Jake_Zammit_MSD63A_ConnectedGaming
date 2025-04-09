using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorePanelVisible : MonoBehaviour
{
    public GameObject StorePanel;

    public void OnStoreButtonClicked()
    {
        StorePanel.SetActive(true);
    }

    public void OnCloseButtonClicked()
    {
        StorePanel.SetActive(false);
    }
}
