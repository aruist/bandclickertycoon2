using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if SCSOCIAL || SCCLOUD
using Softcen.Plugins;
#endif
using System;

public class SCPlugins : MonoBehaviour {
#if SOFTCEN_DEBUG
    public const string TAG = "SCPlugins ";
#endif
    public static SCPlugins instance = null;
    public int AndroidCloudRefreshTime = 30;
#if SCCLOUD

    private static SCCloudServices cloudServices;
#endif
#if SCSOCIAL
    private static SCSocialServices socialServices;
#endif

#if SCSOCIAL
    public static SCSocialServices SocialServices
    {
        get
        {
            if (instance == null)
                return null;
            if (socialServices == null)
                socialServices = instance.GetPlatformClass<SCSocialServices>();
            return socialServices;
        }
    }
#endif
#if SCCLOUD
    public static SCCloudServices CloudServices
    {
        get
        {
            if (instance == null)
                return null;
            if (cloudServices == null)
                cloudServices = instance.GetPlatformClass<SCCloudServices>();
            return cloudServices;
        }
    }
#endif

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
#if SCSOCIAL
        socialServices = instance.GetPlatformClass<SCSocialServices>();
#endif
#if SCCLOUD
        cloudServices = instance.GetPlatformClass<SCCloudServices>();
#endif
    }

    private T GetPlatformClass<T>() where T : MonoBehaviour
    {
        T _existingComponent = GetComponent<T>();

        if (_existingComponent != null)
            return _existingComponent;

        // Until now this component hasn't been added so let's add it now
        System.Type _basicType = typeof(T);
        string _baseTypeName = _basicType.ToString();

        string _platformSpecificTypeName = null;

#if UNITY_EDITOR
        _platformSpecificTypeName = _baseTypeName + "Editor";
#elif UNITY_IOS
		_platformSpecificTypeName	= _baseTypeName + "IOS";	
#elif UNITY_ANDROID
		_platformSpecificTypeName	= _baseTypeName + "Android";
#endif

        if (!string.IsNullOrEmpty(_platformSpecificTypeName))
        {
#if !NETFX_CORE
            System.Type _platformSpecificClassType = _basicType.Assembly.GetType(_platformSpecificTypeName, false);
#else
			System.Type _platformSpecificClassType	= _basicType;
#endif
            if (_platformSpecificClassType != null)
                return gameObject.AddComponent(_platformSpecificClassType) as T;
        }

        return gameObject.AddComponent<T>();
    }

    public static T GetDictionaryType<T> (IDictionary _dict, string _key)
    {
        if (_dict == null)
            return default(T);

        if (_key == null || !_dict.Contains(_key))
            return default(T);
        object _value = _dict[_key];
        Type _targetType = typeof(T);

        if (_value == null)
            return default(T);

        if (_targetType.IsInstanceOfType(_value))
            return (T)_value;

        if (_targetType.IsEnum)
            return (T)Enum.ToObject(_targetType, _value);
        else
            return (T)System.Convert.ChangeType(_value, _targetType);
    }
}
