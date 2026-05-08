using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace BusinessTycoonSim
{
    public static class LoadPlayerDataPrefs
    {
        // Date Time if loading from saved game
        static DateTime currentDate;
        static DateTime oldDate;

        public static void LoadSavedGame()
        {

            // Let's just assume 0f idle time if we can't get anything out of the file
            // But we will be checking anyway and not call any load functions if we don't have a valid file

            // We use a try here as it is an easy way to see if we have a valid playerprefs file
            // If we don't... then we use the catch to get out of the load game function.

            if (IsGameSaved())
            {
           

                // Get the idle time that has passed since the game has been loaded
                float getIdleTime = GetIdleTime();



                gamemanager.ActiveShareholders = PlayerPrefs.GetInt("ActiveShareholders");

                // This method loads the store data from the playerpref file
                LoadSavedStoreData(getIdleTime);

                // Load the upgrades from the playerpref file
                LoadStoreUpgrades();

                // Get the saved cash through the game manager!
                gamemanager.SetStartingBalance(double.Parse(PlayerPrefs.GetString("Cash")));

                gamemanager.LifeTimeEarnings = double.Parse(PlayerPrefs.GetString("LifeTimeEarnings"));


            }

            else
            {
                gamemanager.AddToBalance(gamemanager.StartingBalance);
            }
            
        }

        // We use a simple int in the player prefs to determine if we have a saved game to load
        public static bool IsGameSaved()
        {
            // This is wrapped in a try block as the attempt to check GaameSavedflag will fail if we have never saved the game
            try
            {
                int GameSavedflag = PlayerPrefs.GetInt("GameSaved");
                if (GameSavedflag == 1)  // One way to reset the game on startup would be to store a 0 for this field in the playerprefs
                    return true;
                else
                    return false;
            }
            catch
            {
                Debug.Log("Can't read 'GameSaved' key in PlayerPrefs. Start a new game.");
                return false;
            }
        }

        // We need the idle time to calculate how much the player earned
        private static float GetIdleTime()
        {
            float IdleTime = 0;

            // Defensive programming... Once again we wrap this in a try block to handle invalid data/calculations
            try
            {

                // This gets the stored string into a 64bit integer
                long temp = Convert.ToInt64(PlayerPrefs.GetString("SaveDateTime"));

                //Convert the old time from binary to a DataTime variable
                DateTime oldDate = DateTime.FromBinary(temp);

                //Use the Subtract method and store the result as a timespan variable
                currentDate = System.DateTime.Now;
                TimeSpan difference = currentDate.Subtract(oldDate);

                // Save the idle time in seconds so we can calculate profits when loading the 
                IdleTime = (float)difference.TotalSeconds;

            }
            catch (FormatException)
            {
                // Something went wrong with the saved data
                Debug.Log("exception caught...  We will start a new game.");
                

            }
            return IdleTime;
        }

        // This method goes through all the StoreUpgrades and determines which ones the player has unlocked
        private static void LoadStoreUpgrades()
        {

            // We need this counter to keep a unique key for each upgrade
            int counter = 1;
            foreach (storeupgrade StoreUpgrade in gamemanager.StoreUpgrades)
            {
                string stringKeyName = "storeupgradeunlocked_" + counter.ToString();
                int Unlocked = PlayerPrefs.GetInt(stringKeyName);
                // Debug.Log("Get StoreUpgrade for key-" + stringKeyName + " - " + StoreUpgrade.Store.StoreName + " - " + StoreUpgrade.UpgradeName + " Unlock Value=" + Unlocked.ToString());

                if (Unlocked == 1)
                {
                
                    StoreUpgrade.UpgradeUnlocked = true;
                }
                else
                    StoreUpgrade.UpgradeUnlocked = false;
               
                counter++;
            }
        }

        // Load the store data that has been saved in the playerpref file
        private static void LoadSavedStoreData(float IdleTime)
        {
            // the counter is used to load the unique key for each store from the playerprefs as it doesn't handle arrays
            int counter = 1;
            foreach (store StoreObj in gamemanager.StoreList)
            {
                 // Get the number of stores the player owns
                int StoreCount = PlayerPrefs.GetInt("storecount_" + counter);

                // If they don't own any stores of this type then what are we doing here?
                if (StoreCount > 0)
                {
                    // Store the # of stores in the store object
                    StoreObj.StoreCount = StoreCount;

                    // Check to see if the store is unlocked for the player
                    int Unlocked = PlayerPrefs.GetInt("storeunlocked_" + counter);
                    if (Unlocked == 1)
                    {
                        // Set the store to unlocked
                        StoreObj.StoreUnlocked = true;

                    }
                    else
                    {
                        
                        StoreObj.StoreUnlocked = false;
                    }

                    // Load if the timer was active for the store when they quit the game
                    Unlocked = PlayerPrefs.GetInt("storetimeractive_" + counter);
                    if (Unlocked == 1)
                    {
                        //.Log(StoreObj.StoreName + " timer was active... restart it");
                        StoreObj.StartTimer = true;
                    }
                    else
                        StoreObj.StartTimer = false;

                    // This is where we set the store multipler from the upgrades that have been unlocked
                    StoreObj.CurrentMultiplier = PlayerPrefs.GetFloat("storemultiple_" + counter);


                    float LastTimerValue = PlayerPrefs.GetFloat("storecurrenttimer_" + counter);
                    StoreObj.StoreTimer = PlayerPrefs.GetFloat("storetimer_" + counter);

                    Unlocked = PlayerPrefs.GetInt("storemanagerunlocked_" + counter);
                    if (Unlocked == 1)
                    {
                        StoreObj.ManagerUnlocked = true;


                    }
                    StoreObj.CalculateIdleProfit(IdleTime, LastTimerValue);
                }
                counter++;
            }
        }
    }
}