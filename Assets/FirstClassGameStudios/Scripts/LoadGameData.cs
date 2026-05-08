using UnityEngine;
using System.Collections;
using System.Xml;
using UnityEngine.UI;
using System;

namespace BusinessTycoonSim
{
    // TODO:  This should be refactored to remove filling the UI
    // Once refactored LoadGameData would not (need to) inherit from MonoBehaviour
    // Ideally this class would have no concerns over how the UI is loaded
    // Because prefab initation is an extremely popular design pattern in Unity
    // This is not so much 'bad' design for smaller game projects. 
    // In complex projects you should not rely at all on drag/drop components
    public class LoadGameData : MonoBehaviour
    {


        // Constants
        // TODO: We don't really want to hardcode this in our exe
        // perhaps a .dat or .config file to better implement
        //const string GameDataFile = "GameData_UnlimitedStores";
         
        const string CurrencyDataFile = "BigNumbers";
        const string resourcepath = "/FirstClassGameStudios/Resources/";

        //  Example of observer design pattern
        //  Send out message to all subscribers when we are finished loading our game data
        //  This technique is great when you need to have certain components loaded and in place before others
        public delegate void LoadDataComplete();
        public static event LoadDataComplete OnLoadDataComplete;

        // Used to hold the XML data that defines the stores...
        // By editing the XML data you can change the balance of your stores, add new stores, and control the behavior of the game
        private TextAsset gameData;

        // We hold references here to our key interface objects and data stores
        // so we can easily load them with our gamedata


        // Holds a reference to our StorePanel
        private GameObject storePanel;

        // Holds a reference to our UpgradePanel and UpgradePrefab
        private GameObject upgradePanel;



        // Counts how many upgrades we have loaded... used to size the updatepanel 
        // As transform.childcount returns 0. Probably because the panel is disabled.
        private int upgradeCount;


        // Holds a link to our UIManager
        private UIManager uiManagerObj;

        public string GameDataFile;

        #region Setters & Getters 
        public TextAsset GameData
        {
            get
            {
                return gameData;
            }

            set
            {
                gameData = value;
            }
        }
        public GameObject StorePanel
        {
            get
            {
                return storePanel;
            }

            set
            {
                storePanel = value;
            }
        }
        public int UpgradeCount
        {
            get
            {
                return upgradeCount;
            }

            set
            {
                upgradeCount = value;
            }
        }
        public UIManager UIManagerObj
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

        public GameObject UpgradePanel
        {
            get
            {
                return upgradePanel;
            }

            set
            {
                upgradePanel = value;
            }
        }

        #endregion

        public void Start()
        {
            // Load the references to the UI panels we will load the prefabs into
            // These were previously components that we used a drag and drop to wire them into the class
            // This method allows us to keep these private to the class... they must be public to drag and drop components
            gamemanager.CurrentState = gamemanager.State.Loading;

            storePanel = GameObject.Find("StorePanel");

            UpgradePanel = (GameObject)utils.FindInactiveObjects("UpgradesListPanel", typeof(GameObject));


            //GameObject Canvas = GameObject.Find("Canvas");
            //upgradePanel =  Canvas.transform.Find("UpgradesListPanel").gameObject;

            if (storePanel == null || UpgradePanel == null)
            {
                Debug.LogError("Could not find critical references to load game data: storePanel=" + storePanel.ToString() + ", UpgradePanel=" + UpgradePanel.ToString());
                
            }
             
            // Loads the Game data... Changed name from LoadData to better describe the purpose
            LoadGameDataFromXML();
        }

        public void LoadGameDataFromXML()
        {
             
            // Load our game data here... 
            // TODO: Create a more elegant solution for loading in alternate game data
           

            GameData = Resources.Load("GameData/" + GameDataFile) as TextAsset;

 
            // Load Currency Data  (ie million, billion, trillion, etc... up to 300+ descriptions)
            LoadCurrencyData();

            // Create XML Document to hold game data
            XmlDocument xmlDoc = new XmlDocument();

			// Attempt to load the gamedata
			try{
            	xmlDoc.LoadXml(GameData.text);
			}
			catch(NullReferenceException) {
				// If we can't find the file we load the one provided in the last update of the project
				// TODO: Refactor to not be hardcoded and perhaps better designed
				GameData = Resources.Load("GameData/savedata_update") as TextAsset;
				xmlDoc.LoadXml (GameData.text);
			}
				

            // Load Game Manager Data
            LoadGameManagerData(xmlDoc);

            // Load the Stores
            LoadStores(xmlDoc);

            // After we have loaded the stores we need to resize our update panel to hold all the children
            DynamicScrollFitter(upgradePanel, upgradeCount,1);
            DynamicScrollFitter(storePanel, gamemanager.StoreList.Count,2);

            if (OnLoadDataComplete != null) // Make sure we have at least one observer
                OnLoadDataComplete();
            //new LoadPlayerDataPrefs().LoadSavedGame();

        }


        public void LoadGameManagerData(XmlDocument xmlDoc)
        {
            // Load data items used within the gamemanager 
            gamemanager.StartingBalance = double.Parse(utils.LoadGameDataElement("StartingBalance", xmlDoc));
            gamemanager.CompanyName = utils.LoadGameDataElement("CompanyName", xmlDoc);
            gamemanager.GameName = utils.LoadGameDataElement("GameName", xmlDoc);
            gamemanager.FirstStoreCount = utils.LoadGameDataElementInt("FirstStoreCount", xmlDoc);
            gamemanager.ShareholderMultiplier = utils.LoadGameDataElementFloat("ShareHolderBonusPercent", xmlDoc);

            
             
        }
        // Loads  the stores that are to be used in the game
        void LoadStores(XmlDocument xmlDoc)
        {
            // Look through our XML nodes to get a list of all the stores
            XmlNodeList StoreList = xmlDoc.GetElementsByTagName("store");

            // loop through all the store notes
            foreach (XmlNode StoreInfo in StoreList)
            {
                //Load Store Nodes for each store
                LoadStoreNodes(StoreInfo);


            }

        }

        // Because our panel is disabled the dynamic content components do not work reliably
        // This method will dynamically resize our updatepanel to the right height
        // for our stores
        //
        // Depreciated: We no longer call this method once we figured out Unity's built in tools to dynamically resize the updatepanel
        // Leaving it in just in case at some point in the future we need to manually set the scroll view
        void DynamicScrollFitter(GameObject Panel, int NumberOfItems, int NumCols)
        {

            GridLayoutGroup layoutGroup = Panel.GetComponent<GridLayoutGroup>();
            RectTransform myTransform = Panel.GetComponent<RectTransform>();

            // Get the cellsize and padding
            Vector2 cellSize = layoutGroup.cellSize;
            RectOffset padding = layoutGroup.padding;

            // Get a reference to the size of our panel
            Vector2 newScale = myTransform.sizeDelta;

            // Calculate the size of the panel with the upgradelist
            newScale.y = ((cellSize.y + layoutGroup.spacing.y) * NumberOfItems / NumCols);
            newScale.y = newScale.y + padding.bottom;
            // Set the size of the panel
            myTransform.sizeDelta = newScale;
             

        }
        

		GameObject LoadStorePrefab(XmlNodeList StoreNodes)
		{
			// TODO: Optimize using LINQ
			// Determine if we have a specific store prefab to load
			// This is not very optimized as we look through all the nodes to find the StorePrefab
			// It is "ok" because we have small numbers of elements and we only have to do it on load
			GameObject NewStore = null;
			foreach (XmlNode StoreNode in StoreNodes) {
				
				if (StoreNode.Name == "StorePrefab")
					NewStore = (GameObject)Instantiate(Resources.Load("Prefabs/" + StoreNode.InnerText));
			}

			// As this is an upgrade... let's make sure we don't break the old game data by loading the default storeprefab
			// if the XML does not specify
			if (NewStore == null)
				NewStore = (GameObject)Instantiate(Resources.Load("Prefabs/StorePrefab"));

			return NewStore;
		}

        // Each store in XML has several nodes that describe the store
        // Loop through the nodes and load their values into the store prefab
        void LoadStoreNodes(XmlNode StoreInfo)
        {

            XmlNodeList StoreNodes = StoreInfo.ChildNodes;

			// Factory Design pattern... We can now initiate alternate prefabs based on our XML data.
			// The 'StorePrefab' string inside of the store game data will determine which prefab to load.

			GameObject NewStore = LoadStorePrefab (StoreNodes);

			store storeobj = NewStore.GetComponent<store>();

            foreach (XmlNode StoreNode in StoreNodes)
            {

                SetStoreObj(storeobj, StoreNode, NewStore);

            }

            // Connect our store to the parent panel
            NewStore.transform.SetParent(StorePanel.transform);

           

            // Add store to list in game manager... we could get them from the store panel
            // But that would be  bad form
            //Debug.Log(storeobj.name + " should be adding into list");
            gamemanager.AddStore(storeobj);
        }

        // Check each node name and store the value in the appropriate property of the store node
        void SetStoreObj(store storeobj, XmlNode StoreNode, GameObject NewStore)
        {
            // This will match the XML node 'name' for the store
            if (StoreNode.Name == "name")
            {
                // Locate the text object in the StoreNode
                Text StoreText = NewStore.transform.Find("StoreNameText").GetComponent<Text>();
                // This sets the visual store node name in the UI for the store
                StoreText.text = StoreNode.InnerText;
                // This sets the storename as property in the store object
                // An alternative design would move this to the StoreUI and perhaps set it using the obersver pattern
                storeobj.StoreName = StoreNode.InnerText;

            }

            // We load the image out of our Resources and store it in the Storeimage.sprite property. This is how we 
            // Load the correct image out of XML for each store

            // TODO: Create a utility function to wrap the calls to float.parse and other calls to StoreNode.InnerText in try blocks
            if (StoreNode.Name == "image")
            {
                storeobj.StoreImage = StoreNode.InnerText;
                string SpriteFile = "StoreIcons/" + storeobj.StoreImage;
                
                Sprite newSprite = Resources.Load<Sprite>(SpriteFile);
                Image StoreImage = NewStore.transform.Find("ImageButtonClick").GetComponent<Image>();
                StoreImage.sprite = newSprite;
            }

            // Continue loading all the store properties from XML
            if (StoreNode.Name == "BaseStoreProfit")
                storeobj.BaseStoreProfit = float.Parse(StoreNode.InnerText);
            if (StoreNode.Name == "BaseStoreCost")
                storeobj.BaseStoreCost = float.Parse(StoreNode.InnerText);

            if (StoreNode.Name == "StoreTimer") { 
                storeobj.StoreTimer = float.Parse(StoreNode.InnerText);
                storeobj.BaseTimer = storeobj.StoreTimer;
            }
            if (StoreNode.Name == "StoreMultiplier")
                storeobj.StoreMultiplier = float.Parse(StoreNode.InnerText);
            if (StoreNode.Name == "StoreTimerDivision")
                storeobj.StoreTimerDivision = int.Parse(StoreNode.InnerText);
            if (StoreNode.Name == "StoreCount")
                storeobj.StoreCount = int.Parse(StoreNode.InnerText);
            if (StoreNode.Name == "Manager")
                CreateManager(StoreNode, storeobj);
            if (StoreNode.Name == "Upgrades")
                CreateUpgrades(StoreNode, storeobj);
            if (StoreNode.Name == "id")
                storeobj.StoreID = int.Parse(StoreNode.InnerText);
        }

        // Loop through all the upgrade nodes
        void CreateUpgrades(XmlNode UpgradesNode, store Storeobj)
        {
            foreach (XmlNode UpgradeNode in UpgradesNode)
            {

                CreateUpgrade(UpgradeNode, Storeobj);

            }

        }

        // Creating a manager requires creating a new prefab, loading the items from XML and the storing the prefab in the managerpanel
        void CreateManager(XmlNode ManagerNode, store Storeobj)
        {
            float ManagerCost = 0f;
            string ManagerName = "";
            foreach (XmlNode ManagerInfo in ManagerNode)
            {
                if (ManagerInfo.Name == "ManagerCost")
                    ManagerCost = float.Parse(ManagerInfo.InnerText);
                if (ManagerInfo.Name == "ManagerName")
                    ManagerName = ManagerInfo.InnerText;

            }
             
            // Create the Store Manager Object... In this design pattern we have left all this work to the store class
            
            Storeobj.CreateStoreManager(ManagerCost, ManagerName);
           
        }

        // Create each upgrade from the XML gamedata
        void CreateUpgrade(XmlNode UpgradeNode, store Storeobj)
        {
            // By default we will have a 1f multipler and 1f upgrade cost
            float UpgradeMultiplier = 1f;
            float UpgradeCost = 1f;
            string UpgradeName = "";
            // Loop through the XML nodes for the upgrade and load the values into our variables
            // If we had more than a few we should refactor into another method
            foreach (XmlNode UpgradeInfo in UpgradeNode)
            {

                if (UpgradeInfo.Name == "UpgradeMultiplier")
                    UpgradeMultiplier = float.Parse(UpgradeInfo.InnerText);
                if (UpgradeInfo.Name == "UpgradeCost")
                    UpgradeCost = float.Parse(UpgradeInfo.InnerText);
                if (UpgradeInfo.Name == "UpgradeName")
                    UpgradeName = UpgradeInfo.InnerText;

            }


            // We use a static method in storeupgrade class to create our store upgrade

            storeupgrade.CreateStoreUpgrade(UpgradeCost, UpgradeMultiplier, Storeobj, UpgradeName);


            // Increment upgrade count... used to set the height of our updatepanel afteer transform.childcount continued to return 0
            UpgradeCount++;

        }

        // Loads the array of currency names
        // Sourced from a CSV file in data folder (bignumbers.csv) that
        // was drag and dropped into the CurrencyData propery on the LoadGameData object

        // Because we have over 300 currency names we have chosen not to hardcode them but 
        // Instead to load them for a data file. This provices greater flexibility as you could
        // easily change how your processes without changing hundreds of lines of code. 
        void LoadCurrencyData()
        {
            TextAsset CurrencyData = Resources.Load(CurrencyDataFile) as TextAsset;

            // Get the data from the csv file
            string FileData = CurrencyData.text;

            // Get all the rows of data... \n finds the hidden return that separates the lines
            string[] lines = FileData.Split("\n"[0]);

            // Create a new array list to hold our array
            ArrayList CurrencyArray = new ArrayList();

            // We start at 6 because that is the millionth 'exponent'... this will be the first text description for an amount in the game
            int Counter = 6;
            // Load each row of the data
            foreach (string line in lines)
            {
                // This command breaks the comma delimited line of data into separate fields
                //string[] linedata = (line.Trim()).Split(","[0]);

                // Create a new Curreny item to hold our data
                CurrencyItem CreateItem = new CurrencyItem();

                // Set the key value we will lookup to find the correct currency description
                CreateItem.KeyVal = Counter;

                try
                {
                    // Format our exponent for this currency description. This is used to divide our currency amount so we can properly format the string
                    CreateItem.ExpVal = double.Parse("1e" + (Counter).ToString());

                }
                catch (OverflowException)
                {
                     
                    break;
                }
 
                // Get the currency name from the data file
				CreateItem.CurrencyName = line;

                // Keeping this debug commented inline in case we need it again for troubleshooting
                //Debug.Log("Key:" + CreateItem.KeyVal.ToString() + "-" + string.Format("{0:0.##E+00}", CreateItem.ExpVal) + " " + CreateItem.CurrencyName);

                // Add the currency item to the array
                CurrencyArray.Add(CreateItem);

                // We need to increment by 3 for each currency description as they will only change every 3 decimal places as the value grows 
                // (ie 1.2 million, 12.3 million, 123.0 million ----  2.1 Billion,21.0 Billion, 210.0 Billion, ---  3.2 Trillion, 33.2. 333.3 --- etc.)
                Counter = Counter + 3;
            }

            // Store the array we have created in our game manager for access during gameplay
            gamemanager.CurrencyArray = CurrencyArray;
        }



    }

    // Class to hold each currency level... These are every 3 exponents.... 
    // e1 for 1,000, e3 for millions, e6 for billions, e9 for trillions, etc.

    // TODO: Refactor with simple arrays to gain some performance increase
    // As the game never really 'grows' in terms of new objects as the game progresses
    // it probably just isn't a priority
    public class CurrencyItem
    {
        // We look up this value by using the log10 of the amount. 
        private int keyVal;

        // Thist stores the amount we need to divide by in order to properly format the results. (ie 3,403,000 / 1e6) = 3.403
        private double expVal;

        // Currency label (ie million, billion, trillion, etc)
        private string currencyName;

        // Protect all our variables by keeping them private -- good practice to do throughout
        public int KeyVal
        {
            get
            {
                return keyVal;
            }

            set
            {
                keyVal = value;
            }
        }

        public double ExpVal
        {
            get
            {
                return expVal;
            }

            set
            {
                expVal = value;
            }
        }

        public string CurrencyName
        {
            get
            {
                return currencyName;
            }

            set
            {
                currencyName = value;
            }
        }
    }
}