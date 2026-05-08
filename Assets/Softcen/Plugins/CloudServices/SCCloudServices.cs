#if SCCLOUD
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Softcen.Plugins
{
    public class SCCloudServices : MonoBehaviour
    {
        public enum cloudValueChangeReason
        {
            SERVER,
            INITIAL_SYNC,
            QUOTA_VIOLATION,
            STORE_ACCOUNT
        }
        public delegate void SynchroniseCompletion(bool _success);
        public static event SynchroniseCompletion KeyValueStoreDidSynchroniseEvent;


        public delegate void CloudKeyValueChangeHandler(cloudValueChangeReason _reason, string[] _keys);
        public static event CloudKeyValueChangeHandler CloudKeyValueChangeHandlerEvent;

		protected virtual void CloudKeyValueStoreDidChangeExternally (string _str) {
		}
        protected void CloudKeyValueStoreDidChangeExternally(cloudValueChangeReason _reason, string[] _keys)
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "SCCloudServices Received key store value changed event.");
#endif

            if (CloudKeyValueChangeHandlerEvent != null)
                CloudKeyValueChangeHandlerEvent(_reason, _keys);
        }

#region Values Set
        /// <summary>
        /// Sets a Boolean value for the specified key in the cloud data store.
        /// </summary>
        /// <param name="_key">The key under which to store the value. The length of this key must not exceed 64 bytes.</param>
        /// <param name="_value">The Boolean value to store.</param>
        public virtual void SetBool(string _key, bool _value)
        { }

        /// <summary>
        /// Sets a long value for the specified key in the cloud data store.
        /// </summary>
        /// <param name="_key">The key under which to store the value. The length of this key must not exceed 64 bytes.</param>
        /// <param name="_value">The long value to store.</param>
        public virtual void SetLong(string _key, long _value)
        { }

        /// <summary>
        /// Sets a double value for the specified key in the cloud data store.
        /// </summary>
        /// <param name="_key">The key under which to store the value. The length of this key must not exceed 64 bytes.</param>
        /// <param name="_value">The double value to store.</param>
        public virtual void SetDouble(string _key, double _value)
        { }

        /// <summary>
        /// Sets a string value for the specified key in the cloud data store.
        /// </summary>
        /// <param name="_key">The key under which to store the value. The length of this key must not exceed 64 bytes.</param>
        /// <param name="_value">The string value to store.</param>
        public virtual void SetString(string _key, string _value)
        { }

        /// <summary>
        /// Sets a list object for the specified key in the cloud data store.
        /// </summary>
        /// <param name="_key">The key under which to store the value. The length of this key must not exceed 64 bytes.</param>
        /// <param name="_value">The list object whose contents has to be stored. The objects in the list must be <c>primitive</c>, <c>IList</c>, <c>IDictionary</c>.</param>
        public virtual void SetList(string _key, IList _value)
        { }

        /// <summary>
        /// Sets a dictionary object for the specified key in the cloud data store.
        /// </summary>
        /// <param name="_key">The key under which to store the value. The length of this key must not exceed 64 bytes.</param>
        /// <param name="_value">A dictionary whose contents has to be stored. The objects in the dictionary must be <c>primitive</c>, <c>IList</c>, <c>IDictionary</c>.</param>
        public virtual void SetDictionary(string _key, IDictionary _value)
        { }
#endregion

#region Values Get
        /// <summary>
        /// Returns the Boolean value associated with the specified key.
        /// </summary>
        /// <returns>The Boolean value associated with the specified key, that value is returned. or <c>false</c> if the key was not found.</returns>
        /// <param name="_key">A string used to identify the value stored in the cloud data store.</param>
        public virtual bool GetBool(string _key)
        {
            return false;
        }

        /// <summary>
        /// Returns the long value associated with the specified key.
        /// </summary>
        /// <returns>The long value associated with the specified key or <c>0</c> if the key was not found.</returns>
        /// <param name="_key">A string used to identify the value stored in the cloud data store.</param>
        public virtual long GetLong(string _key)
        {
            return 0L;
        }

        /// <summary>
        /// Returns the double value associated with the specified key.
        /// </summary>
        /// <returns>The double value associated with the specified key or <c>0</c> if the key was not found.</returns>
        /// <param name="_key">A string used to identify the value stored in the cloud data store.</param>
        public virtual double GetDouble(string _key)
        {
            return 0D;
        }

        /// <summary>
        /// Returns the string value associated with the specified key.
        /// </summary>
        /// <returns>The string associated with the specified key, or <c>null</c> if the key was not found or its value is not an <c>string</c> object.</returns>
        /// <param name="_key">A string used to identify the value stored in the cloud data store.</param>
        public virtual string GetString(string _key)
        {
            return null;
        }

        /// <summary>
        /// Returns the list object associated with the specified key.		
        /// </summary>
        /// <returns>The list object associated with the specified key, or <c>null</c> if the key was not found or its value is not an <c>IList</c> object.</returns>
        /// <param name="_key">A string used to identify the value stored in the cloud data store.</param>
        public virtual IList GetList(string _key)
        {
            return null;
        }

        /// <summary>
        /// Returns the dictionary object associated with the specified key.
        /// </summary>
        /// <returns>The dictionary object associated with the specified key, or <c>null</c> if the key was not found or its value is not an <c>IDictionary</c> object.</returns>
        /// <param name="_key">A string used to identify the value stored in the cloud data store.</param>
        public virtual IDictionary GetDictionary(string _key)
        {
            return null;
        }
#endregion

#region Misc
        /// <summary>
        /// Initialises the component.
        /// </summary>
        ///	<remarks> 
        /// \note You need to call this method, before using any features. 
        /// </remarks>
        public virtual void Initialise()
        { }

        /// <summary>
        /// Explicitly synchronizes in-memory data with those stored on disk.
        /// </summary>
        /// <remarks>
        /// \note <see cref="KeyValueStoreDidSynchroniseEvent"/> is triggered, when your app has completed processing synchronisation request. 
        /// </remarks>
        public virtual void Synchronise()
        { }

        /// <summary>
        /// Removes the value associated with the specified key from the cloud data store.
        /// </summary>
        /// <param name="_key">The key corresponding to the value you want to remove.</param>
        public virtual void RemoveKey(string _key)
        { }

        protected virtual void CloudKeyValueStoreDidSynchronise(string _successStr)
        {
            bool _success = bool.Parse(_successStr);

            // Invoke handler
            CloudKeyValueStoreDidSynchronise(_success);
        }

        protected void CloudKeyValueStoreDidSynchronise(bool _success)
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "SCCloudServices Received key store value synchronised event.");
#endif
            if (KeyValueStoreDidSynchroniseEvent != null)
                KeyValueStoreDidSynchroniseEvent(_success);
        }

        public bool CheckAndRefreshDouble(string key, ref double value, bool forceRefresh=false)
        {
            double dblCloud = GetDouble(key);
            bool changed = dblCloud != value ? true : false;
            if (forceRefresh || dblCloud > value)
            {
                changed = true;
                value = dblCloud;
            }
            SetDouble(key, value);
            return changed;
        }
        public bool CheckAndRefreshLong(string key, ref long value, bool forceRefresh = false)
        {
            long longCloud = GetLong(key);
            bool changed = longCloud != value ? true : false;
            if (forceRefresh || longCloud > value)
            {
                value = longCloud;
            }
            SetLong(key, value);
            return changed;
        }

#endregion
    }
}
#endif
