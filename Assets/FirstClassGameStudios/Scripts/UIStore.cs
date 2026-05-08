using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace BusinessTycoonSim
{
    // Manages the UI for the store
    // This is instanced so there will be one of instance of this class
    // For each store in the game
    public class UIStore : MonoBehaviour
    {
        const string StoreBuyButtonStr= "Buy ";
        const string ManagerButtonPurchaseStr = "PURCHASED";
        const string ManagerUnlockButtonStr = "Unlock ";
        // Displays how many stores we own
        public Text StoreCountText;

        // Reference or the progress indicator for the store as it is making money
        public Slider ProgressSlider;

        // References to the buy button and its text
        public Text BuyButtonText;
        public Button BuyButton;

        // What store are we managing
        public store Store;

        // References to both the manager and upgrade buttons that are associated with this store
        public Button ManagerButton;
        public Button UpgradeButton;

        public GameObject ManagerPrefab;


        // Holds a reference to the ManagerPanel
        public GameObject ManagerPanel;

        
        // Example of Observer Design pattern
        // Update our store UI when the balance changes in the gamemanager or when the data is initially loaded
        void OnEnable()
        {

            gamemanager.OnUpdateBalance += UpdateUI;
            LoadGameData.OnLoadDataComplete += LoadUI;
            UIManager.OnLoadUIComplete += UpdateUI;

        }

        // Example of Observer Design pattern
        // Remove our subscriptions if this object is disabled
        void OnDisable()
        {

            gamemanager.OnUpdateBalance -= UpdateUI;
            LoadGameData.OnLoadDataComplete -= LoadUI;
            UIManager.OnLoadUIComplete -= UpdateUI;
        }


        // Just keeps a refrence to the store component to save some code 
        void Awake()
        {
            Store = transform.GetComponent<store>();
        }

        // We set the storecount in the text to whatever the string is
        // TODO: Refactor this... it works but isn't very elegant
        void Start()
        {
            //StoreCountText.text = Store.StoreCount.ToString();
            ManagerPanel = GameObject.Find("ManagersPanel");
             
        }

        // Update is called once per frame
        void Update()
        {
            //Debug.Log(Store.GetCurrentTimer());

            // Shows the progress of the store before it posts its profit
            if (Store.StartTimer && gamemanager.CurrentState == gamemanager.State.Running)
                ProgressSlider.value = Store.GetCurrentTimer() / Store.GetStoreTimer();
            if (!Store.StartTimer)
                ProgressSlider.value = 0f;
        }
        public void LoadUI()

        {
             
            // Set the parent of the prefab to the manager panel
            ManagerPrefab.transform.SetParent(UIManager.instance.ManagerListPanel.transform);

            // Set the name of the manager text to the name of the store
            // You could upgrade this to set a name for the manager
            Text StoreNameText = ManagerPrefab.transform.Find("StoreNameText").GetComponent<Text>();
            StoreNameText.text = Store.StoreName;

            Text ManagerNameText = ManagerPrefab.transform.Find("ManagerNameText").GetComponent<Text>();
            ManagerNameText.text = Store.ManagerName;



            // Find the ManagerButton in our prefab and text in the freab and set the button text to include the cost of the upgrade
            Button ManagerButton = ManagerPrefab.transform.Find("UnlockManagerButton").GetComponent<Button>();
            Text ButtonText = ManagerButton.transform.Find("UnlockManagerButtonText").GetComponent<Text>();

            // TODO: Upgrade to use the big number function
            ButtonText.text = ManagerUnlockButtonStr + gamemanager.FormatNumber(Store.ManagerCost);

             
            ManagerButton =  ManagerPrefab.transform.Find("UnlockManagerButton").GetComponent<Button>();

             
            // Add a listener to the Manager button so that the UnlockManager method is called inside the correct store
            ManagerButton.onClick.AddListener(Store.UnlockManager);
            
        }
        // Example of Observer design pattern

        // This is called as an event when the balance changes in the game manager (using the event setup in OnEnable)
        public void UpdateUI()
        {
            // Hide panel until you can afford the store
            CanvasGroup cg = this.transform.GetComponent<CanvasGroup>();
            if (!Store.StoreUnlocked && !gamemanager.CanBuy(Store.GetNextStoreCost()))
            {

                cg.interactable = false;
                cg.alpha = 0; // 0% alpha makes it invisible

            }
            else
            {
                cg.interactable = true;
                cg.alpha = 1; // 100% alpha makes it visible
                Store.StoreUnlocked = true;


            }
            // Update button if you can afford at least one store
            if (gamemanager.CanBuy(Store.GetNextStoreCost()))
                BuyButton.interactable = true;
            else
                BuyButton.interactable = false;

            BuyButtonText.text = StoreBuyButtonStr + gamemanager.FormatNumber(Store.GetNextStoreCost()) ;

            // Update our store count
            StoreCountText.text = Store.StoreCount.ToString();

            ManagerButton = ManagerPrefab.transform.Find("UnlockManagerButton").GetComponent<Button>();
            if (ManagerButton)
            {

                if (Store.ManagerUnlocked)
                {
                    Text ButtonText = ManagerButton.transform.Find("UnlockManagerButtonText").GetComponent<Text>();
                    ButtonText.text = ManagerButtonPurchaseStr;
                    ManagerButton.interactable = false;
                }
                else
                {

                    // Update manager button if store can be afforded.
                    if (gamemanager.CanBuy(Store.ManagerCost))
                        ManagerButton.interactable = true;
                    else
                        ManagerButton.interactable = false;
                }
            }

        }
        public void ResetManagerButton()
        {
            Text ButtonText = ManagerButton.transform.Find("UnlockManagerButtonText").GetComponent<Text>();
            ButtonText.text = ManagerUnlockButtonStr + gamemanager.FormatNumber(Store.ManagerCost);
        }
        // This is the method called when a user clicks the buy store button
        public void BuyStoreOnClick()
        {



                Store.BuyStores();
                

            }



        // When the player clicks the store icon... start the timer!
        public void OnTimerClick()
        {

            Store.OnStartTimer();
            UIManager.instance.UpdateUI() ;
        }

    }
}