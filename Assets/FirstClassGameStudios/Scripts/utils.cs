using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class utils : MonoBehaviour {

    public static int LoadGameDataElementInt(string String, XmlDocument xmlDoc)


    {
        int ReturnInt = 0;
        try
        {
            ReturnInt = int.Parse(LoadGameDataElement(String, xmlDoc));

        }
        catch (System.Exception)
        {

            Debug.Log("Can't parse " + String + " to int");

        }


        return ReturnInt;
    }
    public static float LoadGameDataElementFloat(string String, XmlDocument xmlDoc)


    {
        float ReturnFloat = 0f;
        try
        {
            ReturnFloat = float.Parse(LoadGameDataElement(String, xmlDoc));

        }
        catch (System.Exception)
        {

            Debug.Log("Can't parse " + String + " to float");

        }


        return ReturnFloat;
    }
    public static string LoadGameDataElement(string String, XmlDocument xmlDoc)
    {
        string mystr = "";
        try
        {
            mystr = xmlDoc.GetElementsByTagName(String)[0].InnerText;
        }
        catch (System.Exception)
        {

            Debug.Log("Could not load element: " + String);
        }
        return mystr;
    }

    public static Object FindInactiveObjects(string name, System.Type type)

    {
        Object[] objs = Resources.FindObjectsOfTypeAll(type);

        foreach (GameObject obj in objs)
        {
            if (obj.name == name)
            {
                return obj;
            }
        }

        return null;

    }
}
