using System;
using UnityEngine;

public class pEliTiEdOStot {
    public static double annaSeiSontaAIka() {
        double sec = 0;
        if (ES2.Exists(GameConsts.Tiedostot.SeiSOnTAaikA + GameConsts.ES2Base.ecpw + GameConsts.Tiedostot.SeiSOnTAaikASS + GameConsts.ES2Base.sl))
        {
            try
            {
                long a = ES2.Load<long> (GameConsts.Tiedostot.SeiSOnTAaikA + GameConsts.ES2Base.ecpw + GameConsts.Tiedostot.SeiSOnTAaikASS + GameConsts.ES2Base.sl);
                //Convert the old time from binary to a DataTime variable
                DateTime oldDate = DateTime.FromBinary(a);

                //Use the Subtract method and store the result as a timespan variable
                DateTime currentDate = System.DateTime.Now;
                TimeSpan difference = currentDate.Subtract(oldDate);

                // Save the idle time in seconds so we can calculate profits when loading the 
                sec = difference.TotalSeconds;
            }
            catch
            {                
                #if SOFTCEN_DEBUG
                Debug.Log ("annaSeiSontaAIka corrupted");
                #endif
            }
        } else {
            #if SOFTCEN_DEBUG
            Debug.Log ("annaSeiSontaAIka no time");
            #endif
        }
        #if SOFTCEN_DEBUG
        Debug.Log ("annaSeiSontaAIka sec: " + sec);
        #endif
        return sec;
    }

    public static void taLLeNnaSeiSontaAiKa() {
        ES2.Save(System.DateTime.Now.ToBinary (), GameConsts.Tiedostot.SeiSOnTAaikA + GameConsts.ES2Base.ecpw + GameConsts.Tiedostot.SeiSOnTAaikASS + GameConsts.ES2Base.sl);
    }
}
