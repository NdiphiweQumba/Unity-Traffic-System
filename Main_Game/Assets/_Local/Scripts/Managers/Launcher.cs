using System.Diagnostics.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class Launcher : MonoBehaviour
    {
        [SerializeField]
        private string SceneName;
        [SerializeField]
        private PanelItems[] PanelItems;
        
        private void Start()
        {
            
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                LeaveGame();
        }
        public void LeaveGame()
        {
            Application.Quit();
        }
        public void StartGame() => SceneManager.LoadScene(SceneName);
        public void OpenClosePanel(string panelName) 
        {
            foreach (PanelItems i in PanelItems)
            {
                if (i.Name == panelName && !i.PanelItem.activeSelf)
                    new PanelItems(panelName, i.PanelItem);

                if (i.Name == panelName && i.PanelItem.activeSelf)
                    new PanelItems(panelName, i.PanelItem);
            }
        }
    }
}
[System.Serializable]
public class PanelItems
{
    public string Name;
    public GameObject PanelItem;

    public PanelItems(string n, GameObject g)
    {
        if (this.Name == n)
            PanelItem.SetActive(true);
        if (this.Name == n && PanelItem.activeSelf)
            PanelItem.SetActive(false);
    }
}