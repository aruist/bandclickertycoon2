using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.IO;
using System.Linq;


namespace BusinessTycoonSim {

    // This class generates an entire full game worth of idle data by setting properties
    // In the associated custom inspector (GameDataGeneratorEditor)
    public class IdleGameDataGenerator : MonoBehaviour {

        const string resourcepath = "/FirstClassGameStudios/Resources/";

        // You can use this variable to name the idle game whatever you wish
        public string GameName = "Idle Business Tycoon Sim (Unity3D)";

        // Company game
        public string CompanyName = "Big Biz";

        // Game settings
        public int NumberOfStores = 6;

		public string StorePrefab = "StorePrefab";

        // Some idle games start you out with 1 store... others make you buy the first one... this lets you do either
        public int FirstStoreCount = 1;

        // How much money the player starts with
        public double StartingBalance = 10f;
        
        // Bonus that shareholders (sometimes called angels in other idle games) provide to each store
        public float ShareHolderBonusPercent = .02f;

        // The starting cost for the first store to be generated
        public float StartingCost = 1f;

        // The starting timer (time to make X profits) for the first store
        public float StartingTimer = 5f;
        public float StartingProfit = .30f;
        public float StartingManagerCost = 100f;
        
        

        public float StartingCostFactor = 4.15f;
        public float StartingTimerFactor = 1.8f;
        public float StartingProfitFactor = 4.05f;
        public float StartingManagerFactor = 8f;

        public float StoreMultiplier = 1.15f;
        public int StoreTimerDivision = 25;

        public int UpgradesPerStore = 5;

        public float UpgradeBaseFactor = 5f;
        public float UpgradeCostMultiplierFactor = 1.5f;
        public float UpgradeMultiplier = 3f;

        float CurrentCost;
        float CurrentTimer;
        float CurrentProfit;
        float CurrentManagerCost;

        float CurrentCostFactor;
        float CurrentTimerFactor;
        float CurrentProfitFactor;

        float CurrentUpgradeCost;

        public ArrayList DataSet = new ArrayList();

        public ArrayList UpgradeDataSet = new ArrayList();

        public bool ResetPlayerData = true;


        // Keeps the random list of Manager Names
        public string ManagerDataFile = "ManagerNames";
        public ArrayList ManagerNames = new ArrayList();

        // Keeps the random list of Company Names
        public string CompanyDataFile = "CompanyNames";
        public ArrayList CompanyNames = new ArrayList();

        // Keeps the random list of Upgrade Descriptions
        public string UpgradesDataFile = "UpgradeNames";
        public ArrayList UpgradeNames = new ArrayList();

        // Keeps the random list of store icons
        public ArrayList Icons = new ArrayList();

        // What is the default name for the data file
        public string GameDataFile = "savedata.xml";
       

		public void GenerateData(string SaveDataFile)
		{
			LoadImages();
			LoadManagerNames();
			LoadCompanyNames();
			LoadUpgradeNames();

			CurrentCost = StartingCost;
			CurrentTimer = StartingTimer;
			CurrentProfit = StartingProfit;
			CurrentManagerCost = StartingManagerCost;

			//CurrentCostFactor = StartingCostFactor;
			//CurrentTimerFactor = StartingTimerFactor;
			//CurrentProfitFactor = StartingProfitFactor;
			for (int i = 0; i < NumberOfStores; i++)
			{
				store StoreClassOnly = new store();

				//GameObject NewStore = (GameObject)Instantiate(Resources.Load("Prefabs/StorePrefab"));
				//NewStore.gameObject.tag = "GeneratedStore";
				//store storeobj = NewStore.GetComponent<store>();
				 
				StoreClassOnly.StoreName = ChooseRandomCompanyName();
				StoreClassOnly.StoreTimer = CurrentTimer;
				StoreClassOnly.BaseStoreCost = CurrentCost;
				StoreClassOnly.BaseStoreProfit = CurrentProfit;
				StoreClassOnly.StoreMultiplier = StoreMultiplier;
				StoreClassOnly.StoreTimerDivision = StoreTimerDivision;
				StoreClassOnly.ManagerCost = CurrentManagerCost;
				StoreClassOnly.ManagerName = ChooseRandomName();
				StoreClassOnly.StoreImage = ChooseRandomIcon();
				if (FirstStoreCount > 0 && i == 0)
					StoreClassOnly.StoreCount = i;
				//Debug.Log("---------------------------------"); 
				//Debug.Log("Store Name = " + StoreName);
				//Debug.Log("Store Timer = " + CurrentTimer.ToString());
				//Debug.Log("Store Cost = " + gamemanager.FormatNumber(CurrentCost));
				//Debug.Log("Store Profit=" + gamemanager.FormatNumber(CurrentProfit));

				//storeobj.transform.SetParent(transform);
				DataSet.Add(StoreClassOnly);


				CurrentCost = CurrentCost * StartingCostFactor;
				CurrentTimer = CurrentTimer * StartingTimerFactor;
				CurrentProfit = CurrentProfit * StartingProfitFactor;
				CurrentManagerCost = CurrentManagerCost * StartingManagerFactor;

				GenerateUpgrades(StoreClassOnly);

			}

			WriteData(SaveDataFile);
//			GameObject[] objstodestroy = GameObject.FindGameObjectsWithTag("GeneratedStore");
//			foreach (GameObject DestroyObj in objstodestroy)
//				DestroyImmediate(DestroyObj);

			DataSet.Clear();

			if (ResetPlayerData)
			{
				Debug.Log("Delete player prefs");
				PlayerPrefs.DeleteAll();
				gamemanager.DontSave = true;
			}

			Debug.Log("Idle data generated");

		}
        // Find a random icon
        private string ChooseRandomIcon()
        {
            
            System.Random rnd = new System.Random();
            int Value = rnd.Next(0, Icons.Count - 1);
            string returnname = Icons[Value].ToString();
            Icons.RemoveAt(Value);  // Once we find an icon we need to remove it from our list so we don't find it again
            return returnname;
        }
      
        public string ChooseRandomCompanyName()
        {
            
            System.Random rnd = new System.Random();
            int Value = rnd.Next(0, CompanyNames.Count - 1);
            string returnname = CompanyNames[Value].ToString();
            CompanyNames.RemoveAt(Value);
            return returnname;
        }

        void LoadUpgradeNames()
        {
            if (UpgradeNames.Count > 0)
                return;
            TextAsset UpgradeData = Resources.Load(UpgradesDataFile) as TextAsset;

            // Get the data from the csv file
            string FileData = UpgradeData.text;

            // Get all the rows of data... \n finds the hidden return that separates the lines
            string[] lines = FileData.Split("\n"[0]);


            // Load each row of the data

            foreach (string line in lines)
            {


                UpgradeNames.Add(line);
                Debug.Log(line);
            }
        }

        private string ChooseRandomUpgradeName()
        {
            //Random.state = System.DateTime.Now.Millisecond;

            int Value = Random.Range(0, UpgradeNames.Count - 1);
            string returnname = UpgradeNames[Value].ToString();
            UpgradeNames.RemoveAt(Value);
            return returnname;
        }
        public void LoadCompanyNames()
        {
            CompanyNames.Clear();
            TextAsset CompanyData = Resources.Load(CompanyDataFile) as TextAsset;

            // Get the data from the csv file
            string FileData = CompanyData.text;

            // Get all the rows of data... \n finds the hidden return that separates the lines
            string[] lines = FileData.Split("\n"[0]);


            // Load each row of the data

            foreach (string line in lines) { 

                 
                CompanyNames.Add(line);
                
            }
        }

        private string ChooseRandomName()
        {
           
            System.Random rnd = new System.Random();
            int Value = rnd.Next(0, ManagerNames.Count-1);
            string returnname = ManagerNames[Value].ToString();
            ManagerNames.RemoveAt(Value);
            return returnname;
        }
        void LoadManagerNames()
        {
            ManagerNames.Clear();
            TextAsset ManagerData = Resources.Load(ManagerDataFile) as TextAsset;

            // Get the data from the csv file
            string FileData = ManagerData.text;

            // Get all the rows of data... \n finds the hidden return that separates the lines
            string[] lines = FileData.Split("\n"[0]);


            // Load each row of the data
            
            foreach (string line in lines)
            {
                ManagerNames.Add(line);
                
            }
        }
        public void WriteData(string Datafile)
        {

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            //string Datafile = Application.dataPath + resourcepath + "GameData/" + GameDataFile + ".xml";
            
            XmlWriter writer = XmlWriter.Create(Datafile, settings);
            

            writer.WriteStartElement("gamedata");
            writer.WriteElementString("StartingBalance", StartingBalance.ToString());

             
            writer.WriteElementString("CompanyName", CompanyName);
            writer.WriteElementString("GameName", GameName);

            writer.WriteElementString("ShareHolderBonusPercent",ShareHolderBonusPercent.ToString());

			writer.WriteElementString("StorePrefab", StorePrefab);
            writer.WriteElementString("NumberOfStores", NumberOfStores.ToString());
            writer.WriteElementString("FirstStoreCount", FirstStoreCount.ToString());
            writer.WriteElementString("StartingCost", StartingCost.ToString());
            writer.WriteElementString("StartingTimer", StartingTimer.ToString());
            writer.WriteElementString("StartingProfit", StartingProfit.ToString());
            writer.WriteElementString("StartingManagerCost", StartingManagerCost.ToString());
            writer.WriteElementString("StartingCostFactor", StartingCostFactor.ToString());
            writer.WriteElementString("StartingTimerFactor", StartingTimerFactor.ToString());
            writer.WriteElementString("StartingProfitFactor", StartingProfitFactor.ToString());
            writer.WriteElementString("StartingManagerFactor", StartingManagerFactor.ToString());
            writer.WriteElementString("StoreMultiplier", StoreMultiplier.ToString());
            writer.WriteElementString("StoreTimerDivision", StoreTimerDivision.ToString());
            writer.WriteElementString("UpgradesPerStore", UpgradesPerStore.ToString());
            writer.WriteElementString("UpgradeBaseFactor", UpgradeBaseFactor.ToString());
            writer.WriteElementString("UpgradeCostMultiplierFactor", UpgradeCostMultiplierFactor.ToString());
            writer.WriteElementString("UpgradeMultiplier", UpgradeMultiplier.ToString());
            int i = 1;
            foreach (store Store in DataSet)
            {
                 
                writer.WriteStartElement("store");
                //writer.WriteAttributeString("Bar", "Some & value");

                writer.WriteElementString("id", i.ToString());
                writer.WriteElementString("name", Store.StoreName);
				writer.WriteElementString("StorePrefab", StorePrefab);
                writer.WriteElementString("image", Store.StoreImage);
                writer.WriteElementString("BaseStoreCost", Store.BaseStoreCost.ToString());
                
                writer.WriteElementString("BaseStoreProfit", Store.BaseStoreProfit.ToString());
                
                writer.WriteElementString("StoreTimer", Store.StoreTimer.ToString());
                writer.WriteElementString("StoreMultiplier", Store.StoreMultiplier.ToString());
                writer.WriteElementString("StoreTimerDivision", Store.StoreTimerDivision.ToString());

                writer.WriteStartElement("Manager");
                	writer.WriteElementString("ManagerCost", Store.ManagerCost.ToString());
                	writer.WriteElementString("ManagerName", Store.ManagerName);
                
                writer.WriteEndElement();
                

                WriteUpgrades(writer, Store);
                writer.WriteEndElement();
                i++;
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
            
        }
        private void WriteUpgrades(XmlWriter writer, store Store)
        {
            writer.WriteStartElement("Upgrades");
            foreach (storeupgrade StoreUpgrade in Store.Upgrades)
            {
                writer.WriteStartElement("Upgrade");
                writer.WriteElementString("UpgradeName", StoreUpgrade.UpgradeName);
                writer.WriteElementString("UpgradeCost", StoreUpgrade.UpgradeCost.ToString());
                writer.WriteElementString("UpgradeMultiplier", StoreUpgrade.UpgradeMultiplier.ToString());

                writer.WriteEndElement();
            }
             

            writer.WriteEndElement();
        }
        private void LoadImages()
        {
            Icons.Clear();
            string[] filePaths  = Directory.GetFiles(Application.dataPath + resourcepath + "StoreIcons/", "*.*");
            foreach (string String in filePaths)
            {
                if (!String.Contains(".meta"))
                {
                     
                    string fileName = Path.GetFileNameWithoutExtension(String);
                    int fileExtPos = fileName.LastIndexOf(".");
                    if (fileExtPos >= 0)
                        fileName = fileName.Substring(0, fileExtPos);
                    Icons.Add(fileName);
                }
                
            }
        }
  

        public void LoadGameData(string FileStr)
        {
            
            Debug.Log(FileStr);
            if (FileStr.Length != 0)
            {
                 
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(FileStr);
                //Debug.Log(GameData);
                //xmlDoc.LoadXml(GameData.text);
                CompanyName = utils.LoadGameDataElement("CompanyName", xmlDoc);
                StartingBalance = float.Parse(utils.LoadGameDataElement("StartingBalance", xmlDoc));
                 
                ShareHolderBonusPercent = utils.LoadGameDataElementFloat("ShareHolderBonusPercent", xmlDoc);
                NumberOfStores = utils.LoadGameDataElementInt("NumberOfStores", xmlDoc);
                StartingCost = utils.LoadGameDataElementFloat("StartingCost", xmlDoc);
                StartingTimer = utils.LoadGameDataElementFloat("StartingTimer", xmlDoc);
                StartingProfit = utils.LoadGameDataElementFloat("StartingProfit", xmlDoc);
                StartingManagerCost = utils.LoadGameDataElementFloat("StartingManagerCost", xmlDoc);
                StartingCostFactor = utils.LoadGameDataElementFloat("StartingCostFactor", xmlDoc);
                StartingTimerFactor = utils.LoadGameDataElementFloat("StartingTimerFactor", xmlDoc);
                StartingProfitFactor = utils.LoadGameDataElementFloat("StartingProfitFactor", xmlDoc);
                StartingManagerFactor = utils.LoadGameDataElementFloat("StartingManagerFactor", xmlDoc);
                StoreMultiplier = utils.LoadGameDataElementFloat("StoreMultiplier", xmlDoc);
                StoreTimerDivision = utils.LoadGameDataElementInt("StoreTimerDivision", xmlDoc);
                UpgradesPerStore = utils.LoadGameDataElementInt("UpgradesPerStore", xmlDoc);
                UpgradeBaseFactor = utils.LoadGameDataElementInt("UpgradeBaseFactor", xmlDoc);
                UpgradeCostMultiplierFactor = utils.LoadGameDataElementFloat("UpgradeCostMultiplierFactor", xmlDoc);
                UpgradeMultiplier = utils.LoadGameDataElementFloat("UpgradeMultiplier", xmlDoc);
            }
        }
   
        void GenerateUpgrades(store Store)
        {
            for (int i = 0; i < UpgradesPerStore; i++)
            {
				//Refactored...
                //GameObject NewUpgrade = (GameObject)Instantiate(Resources.Load("Prefabs/UpgradePrefab"));
                //NewUpgrade.gameObject.tag = "GeneratedStore";

				storeupgrade storeupgradeobj = new storeupgrade(); 
                //storeupgrade storeupgradeobj = NewUpgrade.GetComponent<storeupgrade>();
                storeupgradeobj.UpgradeName= ChooseRandomUpgradeName();
                storeupgradeobj.UpgradeCost = UpgradeBaseFactor * (i+1 ) * UpgradeCostMultiplierFactor * Store.BaseStoreCost;
                storeupgradeobj.UpgradeMultiplier = UpgradeMultiplier;
                storeupgradeobj.Store = Store;
                Store.Upgrades.Add(storeupgradeobj);
                
                
            }

        }
     
    }
}