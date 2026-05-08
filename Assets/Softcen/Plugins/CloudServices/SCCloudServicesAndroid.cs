#if UNITY_ANDROID && SCCLOUD

using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;

using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SC.Utility;
namespace Softcen.Plugins
{
    public class SCCloudServicesAndroid : SCCloudServices
    {
        private const string m_filename = "comsoftcenrallytycoon2";
        private IDictionary m_dataStore;
        private IDictionary m_cloudStore;
        private bool m_isInitialized = false;
        private bool m_onkoPAiKAllINEnLikAINen;
        private bool m_lataus = false;
        private bool m_synkkausMenossa = false;
        private List<string> m_changedKeys;
        private float m_timer;
        private float m_refreshtime;

#region External Methods
#endregion

        private void Awake()
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "SCCloudServicesAndroid Awake");
#endif
            m_timer = 0;
            m_changedKeys = new List<string>();
            m_dataStore = new Dictionary<string, object>();
            //LataaPAiKALLinenTIeto();
        }

        /*void Update()
        {
            m_timer += Time.deltaTime;
            if (m_timer >= m_refreshtime )
            {
                m_timer = 0;
                LataaPilvesta();
            }
        }*/

#region Setting Values

        public override void SetBool(string _key, bool _value)
        {
            SetValue(_key, _value);
        }

        public override void SetLong(string _key, long _value)
        {
            SetValue(_key, _value);
        }

        public override void SetDouble(string _key, double _value)
        {
            SetValue(_key, _value);
        }

        public override void SetString(string _key, string _value)
        {
            SetValue(_key, _value);
        }

        public override void SetList(string _key, IList _value)
        {
            SetValue(_key, _value == null ? null : _value.ToJSON());
        }

        public override void SetDictionary(string _key, IDictionary _value)
        {
            SetValue(_key, _value == null ? null : _value.ToJSON());
        }

#endregion

#region Getting Values

        public override bool GetBool(string _key)
        {
            return GetValue<bool>(_key);
        }

        public override long GetLong(string _key)
        {
            return GetValue<long>(_key);
        }

        public override double GetDouble(string _key)
        {
            return GetValue<double>(_key);
        }

        public override string GetString(string _key)
        {
            return GetValue<string>(_key);
        }

        public override IList GetList(string _key)
        {
            string _JSONString = GetValue<string>(_key);
            return (_JSONString == null) ? null : (IList)JSONUtility.FromJSON(_JSONString);
        }
        
        public override IDictionary GetDictionary(string _key)
        {
            string _JSONString = GetValue<string>(_key);
            return (_JSONString == null) ? null : (IDictionary)JSONUtility.FromJSON(_JSONString);
        }

#endregion

#region Misc
        public override void Initialise()
        {
            base.Initialise();
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "SCCloudServicesAndroid Initialise");
#endif
            m_timer = 0;
            if (SCPlugins.instance != null)
                m_refreshtime = SCPlugins.instance.AndroidCloudRefreshTime;
            else
                m_refreshtime = 60;
            LataaPilvesta();
        }

        public override void Synchronise()
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "SCCloudServicesAndroid Synchronise " + m_isInitialized);
#endif
            if (m_isInitialized)
                TalletaPilveen();
        }

        public override void RemoveKey(string _key)
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "RemoveKey key: " + _key);
#endif
            //scCloudServicesRemoveKey(_key);
        }

#endregion

#region Android Callbacks

		protected override void CloudKeyValueStoreDidChangeExternally (string _dataStr)
		{
#if SOFTCEN_DEBUG
			Debug.Log(SCPlugins.TAG + "CloudKeyValueStoreDidChangeExternally: " + _dataStr);
#endif
		}

#endregion

#region Helper functions
        private void SetValue(string _key, object _value)
        {
            m_dataStore[_key] = _value;
            m_onkoPAiKAllINEnLikAINen = true;
        }

        private T GetValue<T>(string _key)
        {
            return SCPlugins.GetDictionaryType<T>(m_cloudStore, _key);
        }

        private void RemoveKeyValuePair(string _key)
        {
            m_dataStore.Remove(_key);
            m_onkoPAiKAllINEnLikAINen = true;
        }

        public void LataaPAiKALLinenTIeto()
        {
            string tiedosto = "albvk3s?encrypt=true&password=bgs3qqlg33kkz3tW3g&saveLocation=playerprefs&tag=myTag";
            if (ES2.Exists(tiedosto))
            {
                try
                {
                    m_dataStore = ES2.Load<IDictionary>(tiedosto);
                    m_onkoPAiKAllINEnLikAINen = false;
                }
                catch
                {
                    m_dataStore = new Dictionary<string, object>();
                }
            }
            else
            {
                m_dataStore = new Dictionary<string, object>();
            }

        }
#endregion

        public void LataaPilvesta()
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "LataaPilvesta, sync menossa: " + m_synkkausMenossa);
#endif
            m_lataus = true;
            OpenSavedGame(m_filename);
        }

        private void TalletaPilveen()
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "TalletaPilveen, sync menossa: " + m_synkkausMenossa);
#endif
            if (m_onkoPAiKAllINEnLikAINen)
            {
                m_lataus = false;
                OpenSavedGame(m_filename);
            }
        }
        void OpenSavedGame(string filename)
        {
            if (SCPlugins.SocialServices.IsInitializedAndAuthenticated() && !m_synkkausMenossa)
            {
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "OpenSavedGame start");
#endif
                m_synkkausMenossa = true;
                ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
                savedGameClient.OpenWithAutomaticConflictResolution(filename, DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseLongestPlaytime, OnSavedGameOpened);
            }
        }

        public void OnSavedGameOpened(SavedGameRequestStatus status, ISavedGameMetadata game)
        {
            if (status == SavedGameRequestStatus.Success)
            {
                // handle reading or writing of saved game.
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "OpenSavedGame ok!");
#endif
                if (!m_lataus)
                {
                    m_onkoPAiKAllINEnLikAINen = false;
                    byte[] bytes = getDataBytes();
                    if (bytes != null && bytes.Length > 0)
                    {
                        SaveGame(game, bytes, pELiNhaLLitSIJa.Instance.mAjat.KOkonaISPeLAtTuAIka);
                    } else
                    {
                        m_synkkausMenossa = false;
#if SOFTCEN_DEBUG
                        Debug.LogWarning(SCPlugins.TAG + "OpenSavedGame zero length!");
#endif
                    }
                } else
                {
                    LoadGameData(game);
                }
            }
            else
            {
                // handle error
                m_synkkausMenossa = false;
#if SOFTCEN_DEBUG
                Debug.LogWarning(SCPlugins.TAG + "OpenSavedGame failed!");
#endif
            }
        }

        void SaveGame(ISavedGameMetadata game, byte[] savedData, TimeSpan totalPlaytime)
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "SaveGame length: " + savedData.Length + ", totalPlaytime: " + totalPlaytime.Minutes + " min");
#endif
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;

            SavedGameMetadataUpdate.Builder builder = new SavedGameMetadataUpdate.Builder();
            DateTime dateTime = DateTime.Now;
            builder = builder
                .WithUpdatedPlayedTime(totalPlaytime)
                .WithUpdatedDescription("Saved game at " + dateTime.ToString());
            // if (savedImage != null)
            // {
                // This assumes that savedImage is an instance of Texture2D
                // and that you have already called a function equivalent to
                // getScreenshot() to set savedImage
                // NOTE: see sample definition of getScreenshot() method below
                // byte[] pngData = savedImage.EncodeToPNG();
                // builder = builder.WithUpdatedPngCoverImage(pngData);
            // }
            SavedGameMetadataUpdate updatedMetadata = builder.Build();
            savedGameClient.CommitUpdate(game, updatedMetadata, savedData, OnSavedGameWritten);
        }

        public void OnSavedGameWritten(SavedGameRequestStatus status, ISavedGameMetadata game)
        {
            m_synkkausMenossa = false;
            if (status == SavedGameRequestStatus.Success)
            {
                // handle reading or writing of saved game.
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "OnSavedGameWritten ok!");
#endif
            }
            else
            {
                // handle error
#if SOFTCEN_DEBUG
                Debug.LogWarning(SCPlugins.TAG + "OnSavedGameWritten failed");
#endif
            }
        }

        void LoadGameData(ISavedGameMetadata game)
        {
            ISavedGameClient savedGameClient = PlayGamesPlatform.Instance.SavedGame;
            savedGameClient.ReadBinaryData(game, OnSavedGameDataRead);
        }

        public void OnSavedGameDataRead(SavedGameRequestStatus status, byte[] data)
        {
            m_synkkausMenossa = false;
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "OnSavedGameDataRead status: " + status.ToString());
#endif
            if (status == SavedGameRequestStatus.Success)
            {
                m_isInitialized = true;
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "OnSavedGameDataRead data length: " + data.Length);
#endif
                // handle processing the byte array data
                if (data.Length > 0)
                {
                    m_cloudStore = ProcessCloudData(data);
                } else
                {
                    m_cloudStore = new Dictionary<string, object>();
                }
                CloudKeyValueStoreDidSynchronise(true);
            }
            else
            {
                // handle error
                CloudKeyValueStoreDidSynchronise(false);
            }
        }

        private void CheckChangedValues()
        {
            m_changedKeys.Clear();
            if (m_dataStore != null && m_cloudStore != null)
            {
#if SOFTCEN_DEBUG
                string keys = SCPlugins.TAG + "Local keys: \n";
                foreach (string _key in m_dataStore.Keys)
                {
                    keys += SCPlugins.TAG + "key: " + _key + " : " + JSONUtility.ToJSON(m_dataStore[_key]) + "\n";
                }
                Debug.Log(keys);
                keys = SCPlugins.TAG + "Cloud keys: \n";
                foreach (string _key in m_cloudStore.Keys)
                {
                    keys += SCPlugins.TAG + "key: " + _key + " : " + JSONUtility.ToJSON(m_cloudStore[_key]) + "\n";
                }
                Debug.Log(keys);
#endif
                foreach (string _key in m_cloudStore.Keys)
                {
                    if (m_dataStore.Contains(_key))
                    {
                        object _local = m_dataStore[_key];
                        object _cloud = m_cloudStore[_key];

                        if (!JSONUtility.ToJSON(_local).Equals(JSONUtility.ToJSON(_cloud)))
                        {
#if SOFTCEN_DEBUG
                            Debug.Log(SCPlugins.TAG + "Found changed: " + _key);
#endif
                            m_changedKeys.Add(_key);
                        }
                    }
                }
                foreach (string _key in m_dataStore.Keys)
                {
                    if (!m_cloudStore.Contains(_key))
                    {
                        m_changedKeys.Add(_key);
#if SOFTCEN_DEBUG
                        Debug.Log(SCPlugins.TAG + "Found missing key: " + _key);
#endif
                    }
                }

            }
            if (m_changedKeys.Count > 0)
            {
                CloudKeyValueStoreDidChangeExternally(cloudValueChangeReason.SERVER, m_changedKeys.ToArray());
            }
        }

        public IDictionary ProcessCloudData(byte[] cloudData)
        {
            if (cloudData == null)
            {
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "ProcessCloudData length: 0");
#endif
                return null;
            }
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "ProcessCloudData length: " + cloudData.Length);
#endif
            try
            {
                string _decoded = System.Text.Encoding.UTF8.GetString(cloudData, 0, cloudData.Length);
                IDictionary _newCloudData = (IDictionary)JSONUtility.FromJSON(_decoded);
                return _newCloudData;
            }
            catch
            {
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "ProcessCloudData error!");
#endif
                return null;
            }
        }

        public byte[] getDataBytes()
        {
            try
            {
                string _jsonString = m_dataStore.ToJSON();
                byte[] bytes = Encoding.UTF8.GetBytes(_jsonString);
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "Save To Cloud byte length: " + bytes.Length + ", json: " + _jsonString);
#endif
                return bytes;
            }
            catch
            {
                return null;
            }
        }
    }
}
#endif
