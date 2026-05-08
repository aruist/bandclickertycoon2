using UnityEngine;
using System.Collections;
using UnityEditor;

namespace BusinessTycoonSim
{
    [CustomEditor(typeof(IdleGameDataGenerator))]
    public class GameDataGeneratorEditor : Editor
    {
 

        // This method is called to draw our custom inspector
        public override void OnInspectorGUI()
        {
            IdleGameDataGenerator myTarget = (IdleGameDataGenerator)target;

            // We can make labels to it is easier to organize the data in the editor
            EditorGUILayout.LabelField("Game Settings", EditorStyles.boldLabel);

            // You use BeginHorizontal to tell Unity that all of the inspectors elements from here on should be on the same line
            EditorGUILayout.BeginHorizontal();
            myTarget.GameName = EditorGUILayout.TextField("Game Name", myTarget.GameName);

            // We have a simple random button in our editor so we can easily generate a random game name
            if (GUILayout.Button("Random", GUILayout.Width(75f), GUILayout.MinWidth(0f)))
            {
                // It's possible we could have used up all our random names in the list... if so just load them up again
                if (myTarget.CompanyNames.Count == 0)
                    myTarget.LoadCompanyNames();
                myTarget.GameName = myTarget.ChooseRandomCompanyName();
            }

            // This indicates we are ready to create a new line in the inspector
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            myTarget.CompanyName = EditorGUILayout.TextField("Company Name", myTarget.CompanyName);
            if (GUILayout.Button("Random", GUILayout.Width(75f), GUILayout.MinWidth(0f)))
            {
                if (myTarget.CompanyNames.Count == 0)
                    myTarget.LoadCompanyNames();
                myTarget.CompanyName = myTarget.ChooseRandomCompanyName();
            }
            EditorGUILayout.EndHorizontal();
             
			myTarget.StorePrefab = EditorGUILayout.TextField("Store Prefab", myTarget.StorePrefab);
            myTarget.NumberOfStores = EditorGUILayout.IntSlider("Number Of Stores", myTarget.NumberOfStores, 1, 84);
            myTarget.StartingBalance = EditorGUILayout.DoubleField("Starting Balance", myTarget.StartingBalance);
            myTarget.FirstStoreCount = EditorGUILayout.IntSlider("First Store Count", myTarget.FirstStoreCount, 0, 25);
            myTarget.ShareHolderBonusPercent = EditorGUILayout.FloatField("Shareholder Bonus %", myTarget.ShareHolderBonusPercent);
            
            EditorGUILayout.LabelField("Store Settings", EditorStyles.boldLabel);

            // Because we want the exact same layout for this next set of options we use some inline variables to make it
            // somewhat more maintainable
            GUILayoutOption[] Fieldoptions = { GUILayout.MaxWidth(100.0f), GUILayout.MinWidth(65.0f) };
            GUILayoutOption[] FirstLabeloptions = { GUILayout.MaxWidth(160.0f), GUILayout.MinWidth(140.0f) };
            GUILayoutOption[] SecondLabeloptions = { GUILayout.MaxWidth(100.0f), GUILayout.MinWidth(65.0f) };

            EditorGUILayout.BeginHorizontal();
            // Store Cost
            GUILayout.Label("Cost for first store", FirstLabeloptions) ;
            myTarget.StartingCost = EditorGUILayout.FloatField( myTarget.StartingCost, Fieldoptions);
            GUILayout.Label("x multipler", SecondLabeloptions);
            myTarget.StartingCostFactor = EditorGUILayout.FloatField( myTarget.StartingCostFactor, Fieldoptions);
            EditorGUILayout.EndHorizontal();

            // Store Timer
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Starting Timer", FirstLabeloptions);
            myTarget.StartingTimer = EditorGUILayout.FloatField(myTarget.StartingTimer, Fieldoptions);
            GUILayout.Label("x multipler", SecondLabeloptions);
            myTarget.StartingTimerFactor = EditorGUILayout.FloatField( myTarget.StartingTimerFactor, Fieldoptions);
            EditorGUILayout.EndHorizontal();

            // Store Profit
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Profit for first store", FirstLabeloptions);
            myTarget.StartingProfit = EditorGUILayout.FloatField(myTarget.StartingProfit, Fieldoptions);
            GUILayout.Label("x multipler", SecondLabeloptions);
            myTarget.StartingProfitFactor = EditorGUILayout.FloatField( myTarget.StartingProfitFactor, Fieldoptions);
            EditorGUILayout.EndHorizontal();

            // Manager
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Manager Cost", FirstLabeloptions);
            myTarget.StartingManagerCost = EditorGUILayout.FloatField( myTarget.StartingManagerCost, Fieldoptions);
            GUILayout.Label("x multipler", SecondLabeloptions);
            myTarget.StartingManagerFactor = EditorGUILayout.FloatField( myTarget.StartingManagerFactor, Fieldoptions);
            EditorGUILayout.EndHorizontal();

            // Buy multiplier for additional stores after the 1st
            myTarget.StoreMultiplier = EditorGUILayout.FloatField("Buy Multiplier", myTarget.StoreMultiplier);
            myTarget.StoreTimerDivision = EditorGUILayout.IntSlider("Store Timer Division", myTarget.StoreTimerDivision, 1, 100);

            // Upgrades
            EditorGUILayout.LabelField("Store Upgrades", EditorStyles.boldLabel);
            myTarget.UpgradesPerStore = EditorGUILayout.IntSlider("Upgrades Per Store", myTarget.UpgradesPerStore, 1, 100);
            myTarget.UpgradeBaseFactor = EditorGUILayout.FloatField("Upgrade Base Factor", myTarget.UpgradeBaseFactor);
            myTarget.UpgradeMultiplier = EditorGUILayout.FloatField("Upgrade Multiplier Applied to Store", myTarget.UpgradeMultiplier);

            // Load and save data options
            EditorGUILayout.LabelField("Load / Save Idle Game Options", EditorStyles.boldLabel);
            myTarget.ResetPlayerData = EditorGUILayout.Toggle("Reset player data after generating data", myTarget.ResetPlayerData);
            myTarget.CompanyDataFile = EditorGUILayout.TextField("Random Company Names File", myTarget.CompanyDataFile);
            myTarget.ManagerDataFile = EditorGUILayout.TextField("Random Manager Names File", myTarget.ManagerDataFile);
            myTarget.UpgradesDataFile = EditorGUILayout.TextField("Random Upgrades Description File", myTarget.UpgradesDataFile);
            myTarget.GameDataFile = EditorGUILayout.TextField("Default Game Data File", myTarget.GameDataFile);

           // Display the buttons to load and save the game data
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Game Data"))
            {
                OpenDataFile();
                AssetDatabase.Refresh();
            }

            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Generate Idle Game Data"))
            {
                SaveDataFile();
                
            }
        }
		// This method loads the settings that were used to create a data file back into the inspector. It doesn't load the actual data that 
		// was generated... just the settings.
		public void OpenDataFile()
		{
			// This code gets a reference to the target class for the editor (IdleGameDataGenerator) and puts it in myTarget for easy access
			IdleGameDataGenerator myTarget = (IdleGameDataGenerator)target;

			// This brings up a standard file open dialog box to open the data file
			string FileToLoad = EditorUtility.OpenFilePanel("Load Idle Game Data File:", "", "xml");
			Debug.Log(FileToLoad);
			if (FileToLoad !=null ) { 
				myTarget.LoadGameData(FileToLoad);
				EditorUtility.SetDirty(target); // This marks the editor as needing updated
				AssetDatabase.Refresh(); // This forces Unity to (hopefully... it seems just a tad bit unreliable) recognize the file we just saved
			}
		}

		// Prompt and save the data file
		public void SaveDataFile()
		{
			IdleGameDataGenerator myTarget = (IdleGameDataGenerator)target;
			string dpath = Application.dataPath.ToString();
			Debug.Log(dpath);


			string FileToSave = EditorUtility.SaveFilePanel("Save idle game data", dpath + "/FirstClassGameStudios/Resources/Gamedata/"  , myTarget.GameDataFile + ".xml", "xml"); ;
			 
			if (FileToSave !=null) { 
				myTarget.GenerateData(FileToSave);
				EditorUtility.SetDirty(target);  
				AssetDatabase.Refresh();
			}
		} // SaveDataFile
    }  // GameDataGeneratorEditor


}  // namespace