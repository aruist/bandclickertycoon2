using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace BusinessTycoonSim
{
    // This class manages the interaction between our UI and the main game manager and events
    public class UIManager : MonoBehaviour
    {
        // Constants
        const string EPSLabelStr = "EPS: ";
        const string MaxMultipleStr = "MAX";
        const string MultipleSuffixStr = "X ";
        const string UpgradeButtonStr = "Upgrade ";

        // Class properties

        // Example of simple State Machine design pattern
        // Possible states our interface can be in
        // TODO: Extend with pause/resume state
        public enum State
        {
            Main, Managers, Upgrades, LoadGame
        }

        // Used for Singleton design to hold one and only one instance of the gamemanager
        public static UIManager instance;


        public delegate void LoadUIComplete();
        public static event LoadUIComplete OnLoadUIComplete;


        // Text references for the company name and money and other main game items
        public Text CurrentBalanceText;
        public Text CompanyNameText;
        public Text GameNameText;
        public Text TipText;

        // The current state the interface is in
        public static State CurrentState;

        // References to the managerpanel and upgradepanel so we can easily hide/show them
        public GameObject ManagerPanel;
        
        public GameObject LoadGamePanel;
        public GameObject UpgradePanel;
        public GameObject UpgradesListPanel;
        public GameObject ManagerListPanel;

        public Text MultiplierText;

        private static int CurrentBuyMultiplierIndex;
        static int[] BuyMultipliers = new int[] { 1, 5, 10, 25, 50, 100 };

        public Text EPSText;
        

        //Shareholders Text
        public Text ActiveShareholdersText;
        public Text AvailableShareholdersText;
        public Text ShareHolderBonusText;
        // Example of singleton design pattern
        // When we awake check if the instance is null. If it is then we assign instance to this.
        void Awake()
        {
            if (instance == null)
                instance = this;
        }

        public void OnClickMultiplierButton()
        {
            CurrentBuyMultiplierIndex++;
            string MultipleText = "";
            if (CurrentBuyMultiplierIndex >= BuyMultipliers.Length)
            {
                MultipleText = MaxMultipleStr;
                CurrentBuyMultiplierIndex = -1;
                gamemanager.CurrentBuyMultiplier = int.MaxValue;
            }
            else
            {
                MultipleText = MultipleSuffixStr + BuyMultipliers[CurrentBuyMultiplierIndex].ToString();
                gamemanager.CurrentBuyMultiplier = BuyMultipliers[CurrentBuyMultiplierIndex];
            }
            MultiplierText.text = MultipleText;

        }

        void OnEnable()
        {
            // Example of Observer pattern
            // We will need update our UI anytime the game balance changes (and when we load our data on startup)
            gamemanager.OnUpdateBalance += UpdateUI;
            LoadGameData.OnLoadDataComplete += LoadUI;
            OnLoadUIComplete += UpdateUI;
        }
        // Example of Observer design pattern
        // When we disable this object it is good practice to remove any associated subscriptions
        void OnDisable()
        {

            gamemanager.OnUpdateBalance -= UpdateUI;
            LoadGameData.OnLoadDataComplete -= LoadUI;
            OnLoadUIComplete -= UpdateUI;
        }
        public void LoadUI()
        {

            CurrentState = State.Main;
            CurrentBuyMultiplierIndex = 0;

            
            CompanyNameText.text = gamemanager.CompanyName;

            GameNameText.text = gamemanager.GameName;
            LoadUpgradesUI();
            if (OnLoadUIComplete != null)
                OnLoadUIComplete();
        }
        private void LoadUpgradesUI()
        {
            foreach (storeupgrade item in gamemanager.StoreUpgrades)
            {
                 
                // Assign the parent of the upgrade so it will show in the upgrade panel
                item.UpgradePrefab.transform.SetParent(UpgradesListPanel.transform);

                // Set the description of the upgrade 
                Text UpgradeText = item.UpgradePrefab.transform.Find("UpgradeText").GetComponent<Text>();

            // TODO: Upgrade to pull the upgrade name from our XML data
            UpgradeText.text = item.Store.StoreName + ": " + item.UpgradeName;

                string SpriteFile = "StoreIcons/" + item.Store.StoreImage;

                Sprite newSprite = Resources.Load<Sprite>(SpriteFile);
                Image StoreUpgradeImage = item.UpgradePrefab.transform.Find("ImageButtonClick").GetComponent<Image>();
                StoreUpgradeImage.sprite = newSprite;

                // Set the upgrade cost on our button 


                Button UpgradeButton = item.UpgradePrefab.transform.Find("UnlockUpgradeButton").GetComponent<Button>();
            Text ButtonText = UpgradeButton.transform.Find("UnlockUpgradeButtonText").GetComponent<Text>();
             
            ButtonText.text = UpgradeButtonStr + gamemanager.FormatNumber(item.UpgradeCost);


                // Add a listener to the Manager button so that the UnlockManager method is called inside the correct store
                UpgradeButton.onClick.AddListener(item.UnlockUpgrade);
            }
        }
        // This is called to display the manager panel
        // Having the reference stored in a property makes the code clean
        void onShowManagers()
        {
            CurrentState = State.Managers;
            ManagerPanel.SetActive(true);
        }

        // Display the Upgrade Panel
        void onShowUpgrades()
        {
            CurrentState = State.Upgrades;
            UpgradePanel.SetActive(true);
        }

        public void onLoadGame()
        {
            CurrentState = State.LoadGame;
            LoadGamePanel.SetActive(true);
            GameObject.Find("IdleProfitsText").GetComponent<Text>().text = gamemanager.FormatNumber(gamemanager.Idleprofits);
        }
        // Display the main screen
        void onShowMain()
        {
            CurrentState = State.Main;

            // Hide the other panels if we are showing main
            // This design should likely be reconsidered as more interface options are added
            ManagerPanel.SetActive(false);
            UpgradePanel.SetActive(false);
            LoadGamePanel.SetActive(false);
        }

        // This captures the click and uses the state to determine if the panel needs to be hidden or shown
        // This allows the buttons to act as a toggle
        public void onClickManager()
        {
            if (CurrentState == State.Main)
                onShowManagers();
            else
                onShowMain();


        }

        public void onClickReset()
        {
            gamemanager.ResetGame();
        }

        // Handle the onclick for upgrades.. same as the method above but for upgrades
        public void onClickUpgrades()
        {
            if (CurrentState == State.Main)
                onShowUpgrades();
            else
                onShowMain();


        }

  
        // Example of Observer pattern
        // This is the method that is wired to the event subscriptions in the OnEnable method
        public void UpdateUI()
        {
             
            // Set the balance string
            string GetFormatString = gamemanager.GetFormattedBalance();
            CurrentBalanceText.text = GetFormatString;

            TipText.text = TipRotator.CurrentTipText;

            double GetEPS = gamemanager.EarningsPerSecond;
            EPSText.text = EPSLabelStr + gamemanager.FormatNumber(GetEPS);

            ActiveShareholdersText.text = "Active: " + gamemanager.ActiveShareholders.ToString();
            AvailableShareholdersText.text = "Available: " + (gamemanager.TotalShareholders - gamemanager.ActiveShareholders ).ToString();
            ShareHolderBonusText.text = "Bonus %: " + (gamemanager.ActiveShareholders * gamemanager.ShareholderMultiplier).ToString("0.00") + "%" ;

            // Update StoreUI's
            UpdateStoreUIs();
            


        }

        // When we update the main UI we call all the store UI's to update them as well
        public static void UpdateStoreUIs()
        {
            foreach (store Store in gamemanager.StoreList)
            {
                Store.UpdateUI();
            }
        }


    }
}