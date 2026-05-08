using UnityEngine;
using System.Collections;
using System.Linq;


// Unique and shared namespace for all scripts protects from conflicts with other libraries
namespace BusinessTycoonSim
{


    // This is the main game manager class
    public class gamemanager : MonoBehaviour
    {
        // Constants
        // TODO: We don't really want to hardcode this in our exe
        // perhaps a .dat or .config file to better implement?
        private const string currencyFormatString = "C2";
        private const double ShareHolderBaseStart = 150d;
        private const float UIUpdateFrequency = .25f;

        // Example of Observer design pattern
        // These events update all subscriber objects that require it when the balance changes
        public delegate void UpdateBalance();
        public static event UpdateBalance OnUpdateBalance;

        public enum State
        {
            Loading, Running, Paused, Quitting
        }

		// Lifetime earnings - This is used for the calculations for shareholders (angel investors)
        private static double lifeTimeEarnings;
        // The current state the interface is in
        private static State currentState;

        // Used for Singleton design to hold one and only one instance of the gamemanager
        public static gamemanager instance;

        // Holds the most important value for the entire game... the amount of money you have
        private static double currentBalance = 0f;

        private static int firstStoreCount = 1;

        private static double startingBalance = 0f;
        // A simple string to hold the company name

        public static float ShareholderMultiplier = .02f;

        // Game Name and Company Name
        private static string gameName;
        private static string companyName;
        

        // Buy multiplier... keeps track of x1, x5, x10, x25 to buy multiple stores without carpel tunel 
        private static int currentBuyMultiplier = 1;

        // This is the total EPS for all stores updated by store * count * storemultiplierfromupgrades
        private static double earningsPerSecond;

        // This array holds the list of currency names
        //    loaded from CSV file in data folder (bignumbers.csv)
        // Data source: http://www.olsenhome.com/bignumbers/
        private static ArrayList currencyArray;

        // The gamemanager should for the most part have easy access to the major collections in the game
        // We load these lists in the LoadGameData script
        private static ArrayList storeList = new ArrayList();
        private static ArrayList storeUpgrades = new ArrayList();

        // Reference to our UIManager
        private static UIManager uiManagerObj;
        

        // If you have a saved game, this stores the profits from the time you were last idle
        private static double idleprofits =0f;

        // This flag is used as an easy way to tell the engine not to save the game when OnApplicationQuit() is called
        // Primarily used to reset the game and clear all store counts, upgrades, cash, etc.
        private static bool dontSave = false;

        private static int totalShareholders = 0;

        private static int activeShareholders =0;

        // We provide access to the class through these
		//TODO: remove unnecessary setters & getters
        #region Getters & Setters
        public static string CompanyName
        {
            get
            {
                return companyName;
            }

            set
            {
                companyName = value;
            }
        }

        public static ArrayList CurrencyArray
        {
            get
            {
                return currencyArray;
            }

            set
            {
                currencyArray = value;
            }
        }

        public static int CurrentBuyMultiplier
        {
            get
            {
                return currentBuyMultiplier;
            }

            set
            {
                currentBuyMultiplier = value;
            }
        }

        public static double EarningsPerSecond
        {
            get
            {
                return earningsPerSecond;
            }

            set
            {
                earningsPerSecond = value;
            }
        }

        public static ArrayList StoreList
        {
            get
            {
                return storeList;
            }

            set
            {
                storeList = value;
            }
        }

        public static ArrayList StoreUpgrades
        {
            get
            {
                return storeUpgrades;
            }

            set
            {
                storeUpgrades = value;
            }
        }

        public static UIManager UIManagerObj
        {
            get
            {
                return uiManagerObj;
            }

            set
            {
                uiManagerObj = value;
            }
        }

        public static double Idleprofits
        {
            get
            {
                return idleprofits;
            }

            set
            {
                 
                idleprofits = value;
            }
        }

        public static bool DontSave
        {
            get
            {
                return dontSave;
            }

            set
            {
                dontSave = value;
            }
        }

        public static string CurrencyFormatString
        {
            get
            {
                return currencyFormatString;
            }
        }

        public static State CurrentState
        {
            get
            {
                return currentState;
            }

            set
            {
                currentState = value;
            }
        }

        public static double LifeTimeEarnings
        {
            get
            {
                return lifeTimeEarnings;
            }

            set
            {
                lifeTimeEarnings = value;
            }
        }

        public static int TotalShareholders
        {
            get
            {
                return totalShareholders;
            }
        }

        public static int ActiveShareholders
        {
            get
            {
                return activeShareholders;
            }

            set
            {
                activeShareholders = value;
            }
        }

        public static string GameName
        {
            get
            {
                return GameName1;
            }

            set
            {
                GameName1 = value;
            }
        }

        public static string GameName1
        {
            get
            {
                return gameName;
            }

            set
            {
                gameName = value;
            }
        }

        public static double CurrentBalance
        {
            get
            {
                return currentBalance;
            }

            set
            {
                currentBalance = value;
            }
        }

        public static double StartingBalance
        {
            get
            {
                return startingBalance;
            }

            set
            {
                startingBalance = value;
            }
        }

        public static int FirstStoreCount
        {
            get
            {
                return firstStoreCount;
            }

            set
            {
                firstStoreCount = value;
            }
        }


        #endregion

        public void Start()
        {

        }
        private void OnEnable()
        {
            // We don't use the start() method to actually start the game as we want
            // to make sure everything is loaded, setup, and ready to go.
            // This is when the UIManager announces that the UI has completed loading.
            UIManager.OnLoadUIComplete += StartGame;
        }


        public void StartGame()
        {
            LoadPlayerDataPrefs.LoadSavedGame();
            //UIManager.instance.UpdateUI();
            // If we made money while we were gone... then show us the money!
            if (Idleprofits > 0)
                UIManager.instance.onLoadGame();
            CurrentState = State.Running;
            StartCoroutine(GameUpdate());

        }
        IEnumerator GameUpdate()
        {
            while (CurrentState == State.Running)
            {

                CalculateEPS();
                UIManager.instance.UpdateUI();
                //UIManager.instance.UpdateUI();
                yield return new WaitForSeconds(UIUpdateFrequency);
            }


        }

        public static void ResetGame()
        {
            PlayerPrefs.DeleteAll();
            LifeTimeEarnings = 0;
            totalShareholders = 0;
            ActiveShareholders = 0;
            
            RestartGame();
        }
        public static void RestartGame()
        {

            // TODO: We have started attempting to reset the game without
            // quitting ... needs more testing
            CurrentBalance = StartingBalance;
            
            foreach (store Store in storeList) {  
            
				if ((object)Store == (object)storeList[0] && FirstStoreCount > 0)
                   {
                    Store.StoreCount = FirstStoreCount;
                    Store.StoreUnlocked = true;
                   }
                else { 
                    Store.StoreUnlocked = false;
                    Store.StoreCount = 0;
                    }
                Store.CurrentMultiplier = 1f;
                Store.StoreTimer = Store.BaseTimer;
                Store.StartTimer = false;
                Store.CurrentTimer = 0f;
                
                Store.ManagerUnlocked = false;
                Store.StoreUI.ResetManagerButton();

            }
            ClearUpgrades();
            //UIManager.instance.UpdateUI();
            //DontSave = true;
            //Application.Quit();
        }

        public static void ClearUpgrades()
        {
            foreach (storeupgrade StoreUpgrade in storeUpgrades)
            {
                StoreUpgrade.UpgradeUnlocked = false;
            }
        }

        public void QuitGame()
        {
  


            #if UNITY_EDITOR
                        // Application.Quit() does not work in the editor so
                        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                        UnityEditor.EditorApplication.isPlaying = false;
            #else
                             Application.Quit();
            #endif
        }

        public static void AddStore(store NewStore)
        {

            StoreList.Add(NewStore);
			if ((object)NewStore == (object)StoreList[0])
            {
                NewStore.StoreCount = FirstStoreCount;
                if (FirstStoreCount > 0)
                    NewStore.StoreUnlocked = true;
            }


        }


        // Example of singleton design pattern
        // When we awake check if the instance is null. If it is then we assign instance to this.
        void Awake()
        {
            if (instance == null)
                instance = this;
        }


        public void ProcessShareholders()
        {
             
            ActiveShareholders = TotalShareholders;

            RestartGame();
        }

        public void OnApplicationQuit()
        {
            CurrentState = State.Quitting;
            SaveGameData.Save();
        }

        // This method bypasses shareholder bonuses and adding to LifeTimeEarnings
        public static void SetStartingBalance(double amt)
        {
            if (amt > 0)
            {
                CurrentBalance += amt;
            }
        }
        // We call this function anytime we need to add to the balance of the game.
        // Note we send a negative amount to take away money... We don't need a subtractFromBalance
        public static void AddToBalance(double amt)
        {

            if (amt > 0)  // We made profit
            {
                if (ActiveShareholders > 1) {
                    amt = amt + CalculateShareHolderBonus(amt);
                     
                }
                LifeTimeEarnings += amt;

                totalShareholders = (int)System.Math.Floor(ShareHolderBaseStart * System.Math.Sqrt(LifeTimeEarnings / 1.0e+15f));
            }
            CurrentBalance += amt;

            CalculateEPS();

            // Example of Observer pattern
            // Notify all observers that we have updated the game balance
            // This is how the interface knows to update without using updates
            if (OnUpdateBalance != null)
                OnUpdateBalance();
            
        }

        // Returns true if we have enough money to buy an item costing 'AmtToSpend'
        public static bool CanBuy(double AmtToSpend)
        {
            if (AmtToSpend > CurrentBalance)
                return false;
            else
                return true;

        }

        // Returns the current balance. An example of how to protect CurrentBalance from manipulation outside this class.
        public static double GetCurrentBalance()
        {

            return CurrentBalance;
        }

        // This function will format a large number to have a descripiton (ie million, billion, trillion, etc)
        public static string FormatNumber(double NumberToFormat)
        {
            // By defaut we will return the number formated as currency (ie no currency name sufix)
            string ReturnString = NumberToFormat.ToString(CurrencyFormatString);
            // This is where we convert the very large number into a log10 format that we look up as the key value
             
             


            // If the currentbalance isn't greater than 0 let's not waste our time (example of defensive programming)
            if (NumberToFormat > 0)
            {
                // LINQ query example... Maybe a little heavy if performance is critical but can be very useful for readable code
                // We sort our key values descending and only take 1 in order to get the correct key value
                var query = (from CurrencyItem FindItem in CurrencyArray
                             where FindItem.KeyVal <= System.Math.Log10(NumberToFormat)
                             orderby FindItem.KeyVal descending
                             select FindItem).Take(1);
                
                // While we only should find one this is a clean way to process
                foreach (CurrencyItem s in query)
                    
                {
                    //Debug.Log(s.KeyVal);

                    // We format our current string by dividing our large number by the ExpVal property we have set
                    // when we loaded the data from XML. We then append the appropriate currency name.
                    // TODO: Have an option to display alternative short descriptions (ie mil, bil, tri, etc)
                    ReturnString = (NumberToFormat / s.ExpVal).ToString(CurrencyFormatString) + " " + s.CurrencyName;



                }
            }

            return ReturnString;
        }

        // This function is a wrapper for Format number so the calling methods do not need to reference the CurrentBalance
        public static string GetFormattedBalance()
        {

            return FormatNumber(CurrentBalance);
        }
        public static void CalculateEPS()
        {
            
            double EPS = 0;
            foreach (store Store in StoreList)
            {
                EPS = EPS + Store.CalculateProfitPerSecond();
            }
             
            if (ActiveShareholders > 1)
            {
                
                EPS = EPS + CalculateShareHolderBonus(EPS);
            }
            EarningsPerSecond = (float)EPS;

            
        }

        public static double CalculateShareHolderBonus(double amt)
        {
            return  amt * ActiveShareholders * ShareholderMultiplier;
        }
        
    }
}