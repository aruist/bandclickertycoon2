using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
namespace BusinessTycoonSim {


    // A very simple tip class...
    public class tip
    {
        // We use a key to determine which tip the class needs to show
        private string tipKey;
        // The text that will go along with the tip
        private string tipText;

        // You can chain automatically from one tip to another using nextTipKey
        private string nextTipKey = "";

        // Determines how long to wait before showing the next tip
        private float nextTipDelay = 0f;

        // Make the tip hide automatically in seconds
        private float autoHideDelay = -1f; // -1 for never hide

        // Flag to keep track if the tip has already been displayed
        bool tipdisplayed = false;

        public string TipKey
        {
            get
            {
                return tipKey;
            }

            set
            {
                tipKey = value;
            }
        }

        public string TipText
        {
            get
            {
                return tipText;
            }

            set
            {
                tipText = value;
            }
        }

        public bool Tipdisplayed
        {
            get
            {
                return tipdisplayed;
            }

            set
            {
                tipdisplayed = value;
            }
        }

        public string NextTipKey
        {
            get
            {
                return nextTipKey;
            }

            set
            {
                nextTipKey = value;
            }
        }

        public float NextTipDelay
        {
            get
            {
                return nextTipDelay;
            }

            set
            {
                nextTipDelay = value;
            }
        }

        public float AutoHideDelay
        {
            get
            {
                return autoHideDelay;
            }

            set
            {
                autoHideDelay = value;
            }
        }
    }
    public class TipRotator : MonoBehaviour {

        // Maintains the list of tips
        static ArrayList Tips = new ArrayList();

        // We use a singleton to make it easy to access the class
        public static TipRotator instance;

        // Determines if the tip rotator is active or not
        public bool Active = true;

        // The current text to display (Makes it easy for the UIManager to update the text on screen
        public static string CurrentTipText;

        // Keeps track of the next key to display
        public string NextKeyToDisplay;

        // Singleton design pattern
        // Create an instance
        void Awake()
        {
            if (instance == null)
                instance = this;
        }

        // Setup the Tip Rotator
        void Start() {
            CurrentTipText = "";
            LoadTips();

            // if the starting balance and game balance are equal, then display a tip to the user
            // to buy a store

            // Note: This would be re-factored to create a more generic reusable tip rotator
            if (gamemanager.StartingBalance == gamemanager.CurrentBalance)
            {
                DisplayTip("buy");
            }
        }

        // Setup the next tip to display
        public void DisplayNextTip()
        {
            DisplayTip(NextKeyToDisplay);
        }

        // Find the tip to display and then show it
        public void DisplayTip(string KeyVal)
        {
            // Only display tips if the tip rotator is active
            if (Active)
            {

                // Loop through the tips
                foreach (tip Tip in Tips)
                {

                    // Find the matching key
                    if (Tip.TipKey == KeyVal && !Tip.Tipdisplayed)
                    {
                        // Set the currenttiptext... the UIManager references this to update the screen
                        CurrentTipText = Tip.TipText;

                        // If a delay is specified 
                        if (Tip.NextTipDelay > 0)
                        {
                            // then we setup the next key
                            NextKeyToDisplay = Tip.NextTipKey;

                            // Call the method to display the next tip... after the specified delay
                            Invoke("DisplayNextTip", Tip.NextTipDelay);
                        }

                        // Automatically hide the tip after the specified delay
                        if (Tip.AutoHideDelay > 0)
                        {

                            Invoke("HideTip", Tip.AutoHideDelay);
                        }

                        // Mark that this tip has been displayed so it won't be displayed again
                        Tip.Tipdisplayed = true;
                    }
                }

            }
        }
        // Hiding the tip is simple... just set the CurrentTipText to empty string
        void HideTip()

        {
            CurrentTipText = "";
        }

        // Load the tips
        // TODO: Refactor this into a text file using same methods as other text
        // files
        void LoadTips() {

            // We have just hardcoded these for now... this method can be useful
            // to quickly create small sets of data
            tip Tip = new tip();
            Tip.TipKey = "buy";
            Tip.TipText = "Click the Buy button to purchase a store";
            Tips.Add(Tip);

            Tip = new tip();
            Tip.TipKey = "produce";
            Tip.TipText = "Click the Store icon to begin production";
            Tips.Add(Tip);

            Tip = new tip();
            Tip.TipKey = "purchasemore";
            Tip.TipText = "Continue purchasing more stores to make more money!";
            Tip.NextTipKey = "managers";
            Tip.NextTipDelay = 8f;
            Tips.Add(Tip);

            Tip = new tip();
            Tip.TipKey = "managers";
            Tip.TipText = "Unlock Managers to automate store operations";
            Tip.AutoHideDelay =20f;

            Tips.Add(Tip);

        }
    }
}
