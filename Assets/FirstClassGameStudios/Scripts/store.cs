using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;


namespace BusinessTycoonSim
{
    // Primary class for each store in the game
    // One instance is created for each store in the gamedata XML
    // The instance is attached as component of the store prefab
    public class store : MonoBehaviour
    {

        // IdleSlack... This allows us to give a production penalties (or rewards if you wish to go that way) for idle vs active play
        const float IdleSlackPercent = 1f;

        const float IdleSlackFixed = 1f;

        public UIStore StoreUI;

        // Name of the store... like all these properties they are populated by the LoadGameData class
        [SerializeField]
        private string storeName;

        // Name of the store... like all these properties they are populated by the LoadGameData class
        [SerializeField]
        private string storeImage;

        // This is the cost to buy one unit of the store... this is just the base.. each store you buy costs more based on the StoreMultiplier
        [SerializeField]
        private float baseStoreCost;

        // This is how much the store makes each time the store timer runs out...this also is just the base... upgrades will change the profit
        [SerializeField]
        private float baseStoreProfit;

        // How long (in seconds) does it take for the store to make its profit
        [SerializeField]
        private float storeTimer;

        [SerializeField]
        private float baseTimer;

        // How many of this store does the player own
        [SerializeField]
        private int storeCount;

        // Current multiplier for the store.. Starts out at 1f and increases with store upgrades
        [SerializeField]
        private float storeMultiplier;

        // Has the store been unlocked?
        [SerializeField]
        private bool storeUnlocked;


        // Every X number of stores the timer is cut in half... This value determines that division
        [SerializeField]
        private int storeTimerDivision;


        // Keeps track of the current timer for the store. When the timer runs out the profit is posted and the timer starts over
        private float currentTimer = 0;

        // Keeps track of the timer stop/started
        [SerializeField]
        public bool StartTimer;

        // Mangers
        [SerializeField]
        private float managerCost;
        [SerializeField]
        private bool managerUnlocked;
        [SerializeField]
        public string ManagerName;

        [SerializeField]
        public ArrayList Upgrades = new ArrayList();

        // Our currentmultplier from upgrades
        [SerializeField]
        private float currentMultiplier;

        // Profit per second
        [SerializeField]
        private double profitPerSecond;

        // Store ID --- Used for more easily loading, saving, and serializing data
        private int Storeid;

        //public GameObject ManagerPrefab;
        
        #region Property Getters & Setters
        //---------------------------
        // Property Getters & Setters
        //---------------------------
        public string StoreName
        {
            get
            {
                return storeName;
            }

            set
            {
                storeName = value;
            }
        }


        public float BaseStoreCost
        {
            get
            {
                return baseStoreCost;
            }

            set
            {
                baseStoreCost = value;
            }
        }

        public float BaseStoreProfit
        {
            get
            {
                return baseStoreProfit;
            }

            set
            {
                baseStoreProfit = value;
            }
        }

        public float StoreTimer
        {
            get
            {
                return storeTimer;
            }

            set
            {
                storeTimer = value;
            }
        }

        public int StoreCount
        {
            get
            {
                return storeCount;
            }

            set
            {
                storeCount = value;
            }
        }

        public float StoreMultiplier
        {
            get
            {
                return storeMultiplier;
            }

            set
            {
                storeMultiplier = value;
            }
        }

        public bool StoreUnlocked
        {
            get
            {
                return storeUnlocked;
            }

            set
            {
                storeUnlocked = value;
            }
        }

        public int StoreTimerDivision
        {
            get
            {
                return storeTimerDivision;
            }

            set
            {
                storeTimerDivision = value;
            }
        }

        public float ManagerCost
        {
            get
            {
                return managerCost;
            }

            set
            {
                managerCost = value;
            }
        }

        public bool ManagerUnlocked
        {
            get
            {
                return managerUnlocked;
            }

            set
            {
                managerUnlocked = value;
            }
        }

        public float CurrentMultiplier
        {
            get
            {
                return currentMultiplier;
            }

            set
            {
                currentMultiplier = value;
            }
        }

        public int StoreID
        {
            get
            {
                return Storeid;
            }

            set
            {
                Storeid = value;
            }
        }

        public string StoreImage
        {
            get
            {
                return storeImage;
            }

            set
            {
                storeImage = value;
            }
        }

        public double ProfitPerSecond
        {
            get
            {
                return profitPerSecond;
            }

            set
            {
                profitPerSecond = value;
            }
        }

        public float BaseTimer
        {
            get
            {
                return baseTimer;
            }

            set
            {
                baseTimer = value;
            }
        }

        public float CurrentTimer
        {
            get
            {
                return currentTimer;
            }

            set
            {
                currentTimer = value;
            }
        }


        #endregion


        // Use this for initialization
        void Start()
        {

            // They haven't clicked yet! So the timer isn't started
            //StartTimer = false;

            // No upgrades.. mulipler should just be 1
            CurrentMultiplier = 1f;

            // There is no need to process our model every frame... instead we can
            // Determine here how often to check the timers and post profits & udpate UI
            // The only update method in the entire game is for the progress bars on the stores
            // and to delta time to track those timers.

           
            //InvokeRepeating("ProcessTick", .25f, 0.25f);
            StartCoroutine(ProcessTick());
        }

        public void UpdateUI()
        {
            StoreUI.UpdateUI();
        }

        // Just protecting CurrentTimer...
        public float GetCurrentTimer()
        {
            return CurrentTimer;
        }


        public double GetNextStoreCost()
        {
            return baseStoreCost * System.Math.Pow(StoreMultiplier, StoreCount);
            //return NextStoreCost;
        }


        // Return the current timer
        // Once again we have protected the private variable StoreTimer
        public float GetStoreTimer()
        {
            return StoreTimer;
        }

        private void Update()
        {
            // This is how we increment the timer. We use Time.deltatime as it knows how much time went by since the last frame
            //Debug.Log("Timer Going..." + CurrentTimer.ToString() + " Store Timer= " + StoreTimer.ToString());
            CurrentTimer += Time.deltaTime;
        }

        // Update is called once per frame
        // TODO: Refactor to use Involking or other method to avoid code in the Update

         IEnumerator  ProcessTick()
        {
            while (gamemanager.CurrentState ==  gamemanager.State.Running)
            {

            
            // If the timer isn't running... there is nothing to update
            if (StartTimer && gamemanager.CurrentState == gamemanager.State.Running)
            {


                // Once our timer gets to the end we need to post the profit and start the timer over
                 
                if (GetCurrentTimer() > GetStoreTimer())
                {
                    // If we have not unlocked the manager turn off the timer... forcing the user to click to start it again
                    if (!ManagerUnlocked)
                        StartTimer = false;

                    // Reset the timer
                    CurrentTimer = 0f;

                    // Store makes its money... CurrentMultiplier will be increasing based on upgrades 
                    gamemanager.AddToBalance(CalculateStoreProfit());
                    //Debug.Log("Add to Balance:" + CalculateStoreProfit().ToString());
                    
                }
            }
                if (!StartTimer)
                    CurrentTimer = 0f;

                
                //UIManager.instance.UpdateUI();
                yield return new WaitForSeconds(.1f);
            }

            
        }

        public double CalculateStoreProfit()
        {

            double Amt = BaseStoreProfit * StoreCount * CurrentMultiplier;
            return Amt;
        }
        public void CreateStoreManager(float ManagerCostVal, string ManagerNameString)
        {

            ManagerCost = ManagerCostVal;
            ManagerName = ManagerNameString;
            //GameObject NewManager = (GameObject)Resources.Load("ManagerPrefab");   
            GameObject NewManager = (GameObject)Instantiate(Resources.Load("Prefabs/ManagerPrefab"));

            //Debug.Log("Create Manager" + NewManager.ToString());
            // We want to make it ultra easy to talk with the UIStore manager and vice versa
            StoreUI = transform.GetComponent<UIStore>();

            // Get a refrence to the UIManagerprefab for the store and associate our manager button to the store
            // This is how the UIStore can know exactly which store manager needs to be unlocked
            StoreUI.ManagerPrefab = NewManager;

        }
        // This is the routine that figures out how much profit was earned while the game was not running
        public void CalculateIdleProfit(float SecondsIdle, float LastTimerValue)
        {


            SecondsIdle = (SecondsIdle - IdleSlackFixed) * IdleSlackPercent;

            // Only give profit if we are idle longer than the store timer
            double IdleAmt = 0d;
            
            if (StartTimer && SecondsIdle > 0f)
            {
                if (SecondsIdle > StoreTimer - LastTimerValue)
                {
                    if (ManagerUnlocked)
                    {
                        IdleAmt = CalculateStoreProfit() * (double)(SecondsIdle / StoreTimer);
                        CurrentTimer = (SecondsIdle % (StoreTimer - LastTimerValue)) + LastTimerValue;
                        //this.transform.GetComponent<UIStore>().ManagerUnlocked();

                    }
                    else
                    {
                        IdleAmt = CalculateStoreProfit();
                        CurrentTimer = 0f;
                    }
                    gamemanager.Idleprofits += IdleAmt + gamemanager.CalculateShareHolderBonus(IdleAmt) ;
                    gamemanager.AddToBalance(gamemanager.Idleprofits);
                }
                else
                {
                    CurrentTimer = LastTimerValue + SecondsIdle;
                }
            }
            
            //Debug.Log(StoreName + " made " + IdleAmt.ToString() + " while idle.");
        }

     
        public double CalculateProfitPerSecond()
        {
            // To update the master we will try an efficient trick
            // Instead of adding up all the stores in an update we will attempt to first subtract out the EPS for this store
            // from the global EPS value and then add it back in once we have recalculated

            if (StartTimer)
            {
                 
                ProfitPerSecond = CalculateStoreProfit() / StoreTimer;
                 
            }
            else
                ProfitPerSecond = 0;
            
            return ProfitPerSecond;
        }
        // We have bought a store!!!
        // Let's update the required values
        public void BuyStores()
        {
            TipRotator.instance.DisplayTip("produce");
            // Try to buy the amount of stores the player wants to buy
            double StoreBuyCost = CostForXStores(gamemanager.CurrentBuyMultiplier);
            if (StoreBuyCost < gamemanager.CurrentBalance)
            {
                gamemanager.CurrentBalance -= StoreBuyCost;
                StoreCount += gamemanager.CurrentBuyMultiplier;
            }
            else  // if they can't buy that many then buy as many as they possibly can
            {
                int NumStoresToBuy = BuyMax();
                StoreBuyCost = CostForXStores(NumStoresToBuy);
                gamemanager.CurrentBalance -= StoreBuyCost;
                StoreCount += NumStoresToBuy;
            
            }

      
            StoreTimer = BaseTimer / (Mathf.Floor(StoreCount / StoreTimerDivision)+1);
            

        }

        // This calculates the cost to buy a specific number of stores
        public double CostForXStores(int NumberofStores)
        {
            // Forumla source: http://blog.kongregate.com/the-math-of-idle-games-part-i/
            double expr1 = Math.Pow(StoreMultiplier, StoreCount) *(Math.Pow(StoreMultiplier, NumberofStores)-1) ;
            return (BaseStoreCost * (expr1 / (StoreMultiplier - 1)));
        }

        // This method calculates the maximum number of stores that can be purchased
        public int BuyMax()
        {

            // Forumla source: http://blog.kongregate.com/the-math-of-idle-games-part-i/
            double expr1 = gamemanager.CurrentBalance * (StoreMultiplier - 1);
            double expr2 = baseStoreCost * Math.Pow(StoreMultiplier, StoreCount);
            double expr3 = Math.Log((expr1 / expr2) + 1);
            double expr4 = expr3 / Math.Log(StoreMultiplier);

            return (int)Math.Floor(expr4);


        }
        public void OnStartTimer()
        {
           
            // Some defensive programming just to make sure we have at least one store
            if (!StartTimer && StoreCount > 0) { 
                StartTimer = true;
                TipRotator.instance.DisplayTip("purchasemore");
        }
        }

        // Unlock the store manager (automates store timers so you don't have to click to make money!)
        public void UnlockManager()
        {
            // Defensive programming... already unlocked we don't need to do anything
            if (ManagerUnlocked)
                return;

            // Make sure we can afford to unlock the manager
            if (gamemanager.CanBuy(ManagerCost))
            {
                // spend the money
                gamemanager.AddToBalance(-ManagerCost);
                // Unlock the manger
                ManagerUnlocked = true;
                
            }
        }

    }
}
