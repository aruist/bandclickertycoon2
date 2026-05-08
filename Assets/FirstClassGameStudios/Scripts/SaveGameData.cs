using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
namespace BusinessTycoonSim
{
    public static class SaveGameData
    {
        // Saves the game automatically when the player quits
        // TODO: Serialize the objects and save the data in a binary or XML format
        public static void Save()
        {
            // The game has a reset feature allowing you to restart the game without saving progress
            // When the game restarts you will have a new game
            // TODO: Make a roboust 'ResetGame' method that can be executed to reset the game without restarting
            if (!gamemanager.DontSave)
            {
                //Save the current system time as a string in the player prefs class
                PlayerPrefs.SetString("SaveDateTime", System.DateTime.Now.ToBinary().ToString());
                PlayerPrefs.SetString("Cash", gamemanager.GetCurrentBalance().ToString());
                PlayerPrefs.SetString("LifeTimeEarnings", gamemanager.LifeTimeEarnings.ToString());
                PlayerPrefs.SetInt("ActiveShareholders", gamemanager.ActiveShareholders);
                PlayerPrefs.SetInt("TotalShareholders", gamemanager.TotalShareholders);
                // Save Stores
                SaveStores();

                // Save Upgrades
                SaveUpgrades();

                // Update the preference file so we know we saved the game
                PlayerPrefs.SetInt("GameSaved",1);
            }
            else
            {
                // We have not saved the game... as the pref file was cleared in the reset
                // This should be the only key in the playerpref file.
                PlayerPrefs.SetInt("GameSaved",0);
            }

        }
        static void SaveStores()
        {

            // This counter is to label our save keys for the preference file
            // In this manner we can save unlimited stores along with the required fields
            int counter = 1;
            foreach (store StoreObj in gamemanager.StoreList)
            {
                
                PlayerPrefs.SetInt("storecount_" + counter, StoreObj.StoreCount);
                PlayerPrefs.SetFloat("storemultiple_" + counter, StoreObj.CurrentMultiplier);
                PlayerPrefs.SetFloat("storecurrenttimer_" + counter, StoreObj.GetCurrentTimer());
                PlayerPrefs.SetFloat("storetimer_" + counter, StoreObj.GetStoreTimer());
                int Unlocked = 0;
                if (StoreObj.StartTimer)
                {
                    Unlocked = 1;

                }
                PlayerPrefs.SetInt("storetimeractive_" + counter, Unlocked);

                Unlocked = 0;
                if (StoreObj.ManagerUnlocked)
                    Unlocked = 1;
                PlayerPrefs.SetInt("storemanagerunlocked_" + counter, Unlocked);

                Unlocked = 0;
                if (StoreObj.StoreUnlocked)
                    Unlocked = 1;
                PlayerPrefs.SetInt("storeunlocked_" + counter, Unlocked);



                counter++;
            }



        }

        // Saves the store upgrades in the playerprefs
        private static void SaveUpgrades()
        {
            int counter = 1;
            foreach (storeupgrade StoreUpgrade in gamemanager.StoreUpgrades)
            {
                // We only need to store if it is unlocked or not 
                // The game data knows which store it goes with
                string stringKeyName = "storeupgradeunlocked_" + counter.ToString();
               
                // Save upgrade unlock status
                int Unlocked = 0;
                if (StoreUpgrade.UpgradeUnlocked)
                    Unlocked = 1;
                PlayerPrefs.SetInt(stringKeyName, Unlocked);
                //Debug.Log("Save StoreUpgrade for key-" + stringKeyName + ":" + StoreUpgrade.UpgradeName + " Unlock Value=" + Unlocked.ToString());
                counter++;
            }
        }

    }
}