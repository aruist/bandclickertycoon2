using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

#if UNITY_EDITOR && SCCLOUD
using System.Runtime.InteropServices;
using SC.Utility;
namespace Softcen.Plugins
{
    public class SCCloudServicesEditor : SCCloudServices
    {
        private IDictionary m_dataStore;
        private IDictionary m_cloudStore;
        //private const string kChangedKeys = "keys";
        //private const string kReason = "reason";


#region Misc
        public override void Initialise()
        {
			//CloudKeyValueStoreDidChangeExternally ("{\"keys\":[\"cloudCounter\",\"cloudCounter2\"],\"reason\":0}");
            base.Initialise();
#if SOFTCEN_DEBUG
            Debug.Log("SCCloudServicesEditor Initialise");
#endif
#if UNITY_ANDROID
            TestAndroid();
#endif

        }

#endregion

        /*private enum NSUbiquitousKeyValueStoreChangeReason
		{
			NSUbiquitousKeyValueStoreServerChange,
			NSUbiquitousKeyValueStoreInitialSyncChange,
			NSUbiquitousKeyValueStoreQuotaViolationChange,
			NSUbiquitousKeyValueStoreAccountChange
		}*/

        /*protected override void CloudKeyValueStoreDidChangeExternally (string _dataStr)
		{
#if SOFTCEN_DEBUG
			Debug.Log("CloudKeyValueStoreDidChangeExternally: " + _dataStr);
#endif
            try
            {
                IDictionary _dataDict = (IDictionary)JSONUtility.FromJSON(_dataStr);
                IList _changedKeysJSONList = SCPlugins.GetDictionaryType<IList>(_dataDict, kChangedKeys);
                NSUbiquitousKeyValueStoreChangeReason _kvReason = SCPlugins.GetDictionaryType<NSUbiquitousKeyValueStoreChangeReason>(_dataDict, kReason);
                cloudValueChangeReason _reason = ConvertReason(_kvReason);
                if (_changedKeysJSONList != null && _changedKeysJSONList.Count > 0)
                {
                    string[] _changedKeysArray = new string[_changedKeysJSONList.Count];
                    _changedKeysJSONList.CopyTo(_changedKeysArray, 0);
                    CloudKeyValueStoreDidChangeExternally(_reason, _changedKeysArray);
                }
            }
            catch (Exception ex)
            {
#if SOFTCEN_DEBUG
                Debug.LogWarning("Something was wrong! ex: " + ex.Message);
#endif
            }
		}*/

        /*private cloudValueChangeReason ConvertReason (NSUbiquitousKeyValueStoreChangeReason _reason)
		{
			switch(_reason)
			{
			case NSUbiquitousKeyValueStoreChangeReason.NSUbiquitousKeyValueStoreServerChange:
				return cloudValueChangeReason.SERVER;

			case NSUbiquitousKeyValueStoreChangeReason.NSUbiquitousKeyValueStoreInitialSyncChange:
				return cloudValueChangeReason.INITIAL_SYNC;

			case NSUbiquitousKeyValueStoreChangeReason.NSUbiquitousKeyValueStoreQuotaViolationChange:
				return cloudValueChangeReason.QUOTA_VIOLATION;

			case NSUbiquitousKeyValueStoreChangeReason.NSUbiquitousKeyValueStoreAccountChange:
				return cloudValueChangeReason.STORE_ACCOUNT;
			}
			return cloudValueChangeReason.SERVER;
		}*/

        private void TestAndroid()
        {
            SCCloudServicesAndroid android = new SCCloudServicesAndroid();
            android.LataaPAiKALLinenTIeto();
            android.SetDouble("testValue", 123);
            android.SetDouble("testValue2", 99);
            android.SetBool("boolVal1", true);
            android.SetBool("boolVal2", false);
            android.SetLong("longVal1", 1234567);
            android.SetString("stringVal1", "Hello world");
            List<int> list = new List<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            android.SetList("listVal1", list);
            Dictionary<string, int> dict = new Dictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);
            dict.Add("c", 3);
            android.SetDictionary("dictVal1", dict);

            double d = android.GetDouble("testValue");
            double d2 = android.GetDouble("testValue2");
            Debug.Log("d: " + d);

            byte[] bytes = android.getDataBytes();
            IDictionary _test = android.ProcessCloudData(bytes);

            if (_test.Count > 0)
            {

            }

        }

    }

}
#endif
