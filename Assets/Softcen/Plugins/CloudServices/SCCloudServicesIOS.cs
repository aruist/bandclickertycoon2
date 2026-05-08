#if UNITY_IOS && SCCLOUD
using System.Collections;
using UnityEngine;


using System.Runtime.InteropServices;
using SC.Utility;
namespace Softcen.Plugins
{
    public class SCCloudServicesIOS : SCCloudServices
    {
		private const string kChangedKeys = "keys";
		private const string kReason = "reason";

#region External Methods

        [DllImport("__Internal")]
        private static extern void scCloudServicesInitialise();

        [DllImport("__Internal")]
        private static extern void scCloudServicesSetBool(string _key, bool _value);

        [DllImport("__Internal")]
        private static extern void scCloudServicesSetLong(string _key, long _value);

        [DllImport("__Internal")]
        private static extern void scCloudServicesSetDouble(string _key, double _value);

        [DllImport("__Internal")]
        private static extern void scCloudServicesSetString(string _key, string _value);

        [DllImport("__Internal")]
        private static extern void scCloudServicesSetList(string _key, string _value);

        [DllImport("__Internal")]
        private static extern void scCloudServicesSetDictionary(string _key, string _value);

        [DllImport("__Internal")]
        private static extern bool scCloudServicesGetBool(string _key);

        [DllImport("__Internal")]
        private static extern long scCloudServicesGetLong(string _key);

        [DllImport("__Internal")]
        private static extern double scCloudServicesGetDouble(string _key);

        [DllImport("__Internal")]
        private static extern string scCloudServicesGetString(string _key);

        [DllImport("__Internal")]
        private static extern string scCloudServicesGetList(string _key);

        [DllImport("__Internal")]
        private static extern string scCloudServicesGetDictionary(string _key);

        [DllImport("__Internal")]
        private static extern bool scCloudServicesSynchronise();

        [DllImport("__Internal")]
        private static extern void scCloudServicesRemoveKey(string _key);

#endregion

#region Setting Values

        public override void SetBool(string _key, bool _value)
        {
            scCloudServicesSetBool(_key, _value);
        }

        public override void SetLong(string _key, long _value)
        {
            scCloudServicesSetLong(_key, _value);
        }

        public override void SetDouble(string _key, double _value)
        {
            scCloudServicesSetDouble(_key, _value);
        }

        public override void SetString(string _key, string _value)
        {
            scCloudServicesSetString(_key, _value);
        }

        public override void SetList(string _key, IList _value)
        {
            scCloudServicesSetList(_key, _value == null ? null : _value.ToJSON());
        }

        public override void SetDictionary(string _key, IDictionary _value)
        {
            scCloudServicesSetDictionary(_key, _value == null ? null : _value.ToJSON());
        }

#endregion

#region Getting Values

        public override bool GetBool(string _key)
        {
            return scCloudServicesGetBool(_key);
        }

        public override long GetLong(string _key)
        {
            return scCloudServicesGetLong(_key);
        }

        public override double GetDouble(string _key)
        {
            return scCloudServicesGetDouble(_key);
        }

        public override string GetString(string _key)
        {
            return scCloudServicesGetString(_key);
        }

        public override IList GetList(string _key)
        {
            string _JSONString = scCloudServicesGetList(_key);

            return (_JSONString == null) ? null : (IList)JSONUtility.FromJSON(_JSONString);
        }

        public override IDictionary GetDictionary(string _key)
        {
            string _JSONString = scCloudServicesGetDictionary(_key);

            return (_JSONString == null) ? null : (IDictionary)JSONUtility.FromJSON(_JSONString);
        }

#endregion

#region Misc
        public override void Initialise()
        {
			//CloudKeyValueStoreDidChangeExternally ("{\"keys\":[\"cloudCounter\"],\"reason\":0}");
            base.Initialise();
#if SOFTCEN_DEBUG
            Debug.Log("SCCloudServicesIOS Initialise");
#endif
            // Native method call
            scCloudServicesInitialise();
            Synchronise();
        }

        public override void Synchronise()
        {
            bool _success = scCloudServicesSynchronise();

            // Send event
            CloudKeyValueStoreDidSynchronise(_success);
        }

        public override void RemoveKey(string _key)
        {
            scCloudServicesRemoveKey(_key);
        }

#endregion

#region iOS Callbacks
		private enum NSUbiquitousKeyValueStoreChangeReason
		{
			NSUbiquitousKeyValueStoreServerChange,
			NSUbiquitousKeyValueStoreInitialSyncChange,
			NSUbiquitousKeyValueStoreQuotaViolationChange,
			NSUbiquitousKeyValueStoreAccountChange
		}

		protected override void CloudKeyValueStoreDidChangeExternally (string _dataStr)
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
			catch
            {
#if SOFTCEN_DEBUG
                Debug.LogWarning("Something was wrong!");
#endif
            }
		}

		private cloudValueChangeReason ConvertReason (NSUbiquitousKeyValueStoreChangeReason _reason)
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
		}

#endregion
    }
}
#endif
