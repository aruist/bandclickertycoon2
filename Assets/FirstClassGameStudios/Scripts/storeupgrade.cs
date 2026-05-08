using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BusinessTycoonSim
{
    // This class defines our storeupgrade class that is instanced for each store upgrade in the game
    public class storeupgrade : MonoBehaviour
    {
        // TODO: We don't really want to hardcode this in our exe
        // perhaps a .dat or .config file to better implement
        const string PurchaseText = "PURCHASED";

        // Store upgrades increase the multiplier for the store... this is by what factor the multiplier with increase the profit
        private float upgradeMultiplier;

        // Cost to unlock the upgrade
        private float upgradeCost;

        // The name of the upgrade
        private string upgradeName;

        // This stores if the upgrade is unlocked or not
        private bool upgradeUnlocked;

        // Reference to the store this upgrade is associated with
        // This reference is set when the data is loaded
        private store store;

        // When we unlock the upgrade we want to change the text and behavior of the button
        // This holds these references

        // TODO:  A better design may refactor this into a UI class
        public Text ButtonText;
        public Button UpgradeButton;

        public GameObject UpgradePrefab;

        public float UpgradeMultiplier
        {
            get
            {
                return upgradeMultiplier;
            }

            set
            {
                upgradeMultiplier = value;
            }
        }

        public float UpgradeCost
        {
            get
            {
                return upgradeCost;
            }

            set
            {
                upgradeCost = value;
            }
        }

        public string UpgradeName
        {
            get
            {
                return upgradeName;
            }

            set
            {
                upgradeName = value;
            }
        }

        public bool UpgradeUnlocked
        {
            get
            {
                return upgradeUnlocked;
            }

            set
            {
                upgradeUnlocked = value;
            }
        }

        public store Store
        {
            get
            {
                return store;
            }

            set
            {
                store = value;
            }
        }

        public static void CreateStoreUpgrade(float UpgradeCost, float UpgradeMultiplier, store Storeobj,string UpgradeName)
        {
            GameObject NewUpgrade = (GameObject)Instantiate(Resources.Load("Prefabs/UpgradePrefab"));
           
            // StoreUpgrade class that is a component of that prefab
            // These will be used when we do our calculations when we unlock the prefab
            storeupgrade StoreUpgrade = NewUpgrade.GetComponent<storeupgrade>();
            StoreUpgrade.UpgradeCost = UpgradeCost;
            StoreUpgrade.UpgradeMultiplier = UpgradeMultiplier;
            StoreUpgrade.Store = Storeobj;
            StoreUpgrade.UpgradeName = UpgradeName;
            StoreUpgrade.UpgradePrefab = NewUpgrade;

            // Add upgrade to upgradelist in gamemanager
            gamemanager.StoreUpgrades.Add(StoreUpgrade);
        }
        void OnEnable()
        {
            // Example of Observer design pattern
            // This class needs to know when the balance is updated and when the data is loaded
            gamemanager.OnUpdateBalance += StoreUpgradeAvailable;
            LoadGameData.OnLoadDataComplete += StoreUpgradeAvailable;

        }
        void OnDisable()
        {
            // Example of Observer design pattern
            // It is good practice to clean up your events
            gamemanager.OnUpdateBalance -= StoreUpgradeAvailable;
            LoadGameData.OnLoadDataComplete -= StoreUpgradeAvailable;

        }

        // Unlock the upgrade
        public void UnlockUpgrade()
        {
            // If the upgrade has already been unlocked get out of here (defensive programming)
            if (UpgradeUnlocked)
                return;

            // Check to make sure we can unlock this upgrade
            // The UI checks for this... an alternative design may omit check
            if (gamemanager.CanBuy(UpgradeCost))
            {
                //Debug.Log("Unlocked for + " + UpgradeCost.ToString());

                // Here we call to the game manager to spend the money for the upgrade... no getting it back now!
                gamemanager.AddToBalance(-UpgradeCost);
                // We have unlocked this upgrade... let's make it so
                UpgradeUnlocked = true;

                // Apply the upgrade by updating the multipler of the store
                Store.CurrentMultiplier = Store.CurrentMultiplier + UpgradeMultiplier;

            }
        }

        // Example of Observer design pattern
        // This is our method that is tied to our observer event we described in OnEnable
        //  When the balance changes (or when the game is first loaded) this method is called
        // So that the Upgrade button can properly display its state
        public void StoreUpgradeAvailable()
        {

            // Update our button text to show the upgrade has been purchased
            // The code here is ultra simple because we setup this reference when loading the game data
            if (UpgradeUnlocked)
                ButtonText.text = PurchaseText;

            // Update upgrade button if store can be afforded.
            if (!UpgradeUnlocked && gamemanager.CanBuy(UpgradeCost))
                UpgradeButton.interactable = true;
            else

            {

                UpgradeButton.interactable = false;
            }


        }


    }
}