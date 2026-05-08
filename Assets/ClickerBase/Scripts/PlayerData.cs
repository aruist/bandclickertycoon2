using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Diagnostics;
[Serializable]
public class PlayerData // : ISerializable
{
    public static event Action OnMoneyChanged;
    public static event Action OnVaultMoneyChanged;
    public static event Action OnProfitChanged;

    private const int _version = 7;
    public int Version { get { return _version; } }

    public double _moneyIterations;
    public double _earnedMoney;
    public double _usedMoney;
    public bool _changed;
    public long _lastusedTicks;
    public bool _signInOnStartup;
    public Dictionary<int, PlaceData> places;
    public float _aanivoluumi;
    public float _musiikkivoluumi;
    // New idle engine
    public int _activeShareHolders;

    // public double Money
    // {
    //     get { return _earnedMoney - _usedMoney; }
    // }

    // public void ChangeMoney(double amount)
    // {
    //     if (amount < 0)
    //     {
    //         if (_usedMoney + amount > Math.Pow(10, 307))
    //         {
    //             _moneyIterations += 1;
    //             _earnedMoney = Money - Math.Abs(amount);
    //             _usedMoney = 0;
    //         }
    //         else
    //         {

    //         }
    //         _usedMoney += Math.Abs(amount);
    //         _changed = true;
    //     }
    //     else
    //     {
    //         if (_earnedMoney + amount > Math.Pow(10, 307))
    //         {
    //             _moneyIterations += 1;
    //             _earnedMoney = Money + amount;
    //             _usedMoney = 0;
    //         }
    //         else
    //         {
    //             _earnedMoney += amount;
    //         }
    //         _changed = true;
    //     }
    //     if (OnMoneyChanged != null)
    //         OnMoneyChanged();
    // }

    public void Init()
    {
        if (places == null)
        {
            places = new Dictionary<int, PlaceData>();
        }
        else
        {
            places.Clear();
        }
        _aanivoluumi = 0;
        _musiikkivoluumi = 0;
        _moneyIterations = 0;
        _earnedMoney = 5;
        _usedMoney = 0;
        _lastusedTicks = DateTime.UtcNow.Ticks;
        _signInOnStartup = true;
    }

    public PlayerData()  { }

    /*public PlayerData(SerializationInfo info, StreamingContext ctxt)
    {
        Init();
        int fileVersion = (int)info.GetValue("fkaj2a", typeof(int));
        _earnedMoney = (double)info.GetValue("afb3e3", typeof(double));
        _usedMoney = (double)info.GetValue("bfba2j", typeof(double));
        if (fileVersion >= 1)
            _moneyIterations = (double)info.GetValue("bfga2j", typeof(double));
        else
            _moneyIterations = 0;
        if (fileVersion >= 2)
        {
            int count = (int)info.GetValue("fkai2a", typeof(int));
            for (int i=0; i < count; i++)
            {
                int id = (int)info.GetValue("fhbi2" + i.ToString(), typeof(int));
                PlaceData pd = new PlaceData(id);
                int count2 = (int)info.GetValue("fkbi2" + i.ToString(), typeof(int));
                for (int j=0; j < count2; j++)
                {
                    int key = (int)info.GetValue("fibi2" + j.ToString(), typeof(int));
                    int value = (int)info.GetValue("fjbi2" + j.ToString(), typeof(int));
                    pd.item.Add(key, value);
                }
                places.Add(id, pd);
            }
        }
        if (fileVersion >= 3)
        {
            _lastusedTicks = (long)info.GetValue("bgaeq8", typeof(long));
        }
        else
        {
            _lastusedTicks = DateTime.UtcNow.Ticks;
        }
    }
    public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
    {
        info.AddValue("fkaj2a", version);
        info.AddValue("afb3e3", _earnedMoney);
        info.AddValue("bfba2j", _usedMoney);
        info.AddValue("bfga2j", _moneyIterations);
        // Version 2
        info.AddValue("fkai2a", places.Count);
        for (int i=0; i < places.Count; i++)
        {
            info.AddValue("fhbi2" + i.ToString(), places[i].id);
            info.AddValue("fkbi2" + i.ToString(), places[i].item.Count);
            int j = 0;
            foreach(var itm in places[i].item)
            {
                info.AddValue("fibi2" + j.ToString(), itm.Key);
                info.AddValue("fjbi2" + j.ToString(), itm.Value);
                j++;
            }
        }
        info.AddValue("bgaeq8", _lastusedTicks);
    }*/

    public PlaceData GetPlaceData(int id)
    {
        if (places.ContainsKey(id))
        {
            return places[id];
        }
        _changed = true;
        PlaceData pd = new PlaceData(id);
        places.Add(id, pd);
        return pd;
    }

    public int GetPlaceItemLevel(int placeId, int itemId )
    {
        if (places.ContainsKey(placeId))
        {
            if (places[placeId].item.ContainsKey(itemId))
            {
                return places[placeId].item[itemId];
            }
            else
            {
                places[placeId].item.Add(itemId, 0);
                _changed = true;
                return 0;
            }
        }
        _changed = true;
        PlaceData pd = new PlaceData(placeId);
        pd.item.Add(itemId, 0);
        places.Add(placeId, pd);
        return 0;
    }

    public void CallOnProfitChanged()
    {
        if (OnProfitChanged != null)
            OnProfitChanged();
    }

    public int PurchaseItem(int placeId, int itemId, bool callEvent)
    {
        _changed = true;
        if (places.ContainsKey(placeId))
        {
            int lvl = places[placeId].PurchaseItem(itemId);
            if (callEvent)
                CallOnProfitChanged();
            return lvl;
        }
        PlaceData pd = new PlaceData(placeId);
        pd.PurchaseItem(itemId);
        places.Add(placeId, pd);
        if (callEvent)
            CallOnProfitChanged();
        return 1;
    }

    public int GetVaultOwned(int id)
    {
        if (places.ContainsKey(id))
        {
            return places[id].kassaKaappiKoko;
        }
        return 0;
    }

    public double GetRegionVaultMoney(int id)
    {
        if (places.ContainsKey(id))
            return places[id].vaultMoney;
        return 0d;
    }


    public void SetRegionVaultMoney(int id, double value)
    {
        #if SOFTCEN_DEBUG
        UnityEngine.Debug.Log($"PlayerData - SetRegionVaultMoney {id}, {value}");
        #endif
        PlaceData pd = GetPlaceData(id);
        if (Math.Abs(pd.vaultMoney - value) < 0.0000001d)
            return;
        pd.vaultMoney = value;
        _changed = true;
    }

    public void ChangeRegionValutMoney(int id, double value)
    {
        #if SOFTCEN_DEBUG
        UnityEngine.Debug.Log($"PlayerData - ChangeRegionValutMoney {id}, {value}");
        #endif
        if (value == 0) return;
        PlaceData pd = GetPlaceData(id);
        pd.vaultMoney += value;
        if (pd.vaultMoney < 0) pd.vaultMoney = 0d;
        OnVaultMoneyChanged?.Invoke();
    }

    public void ChangeRegionMoney(int id, double value)
    {
        if (value == 0) return;
        PlaceData pd = GetPlaceData(id);
        pd.money += value;
        if (pd.money < 0) pd.money = 0d;
        OnMoneyChanged?.Invoke();
    }

    public double GetRegionMoney(int id)
    {
        if (places.ContainsKey(id))
            return places[id].money;
        return 0d;
    }

}

// Runtime PlaceData
public class PlaceData
{
    public PlaceData(int _id)
    {
        id = _id;
        kassaKaappiKoko = 0;
        item = new Dictionary<int, int>();
        money = 0;
        vaultMoney = 0;
    }

    public int id;
    public Dictionary<int, int> item;
    public int kassaKaappiKoko;
    public SecureDoubleLight vaultMoney;
    public SecureDoubleLight money;

    public int PurchaseItem(int itemId)
    {
        if (item.ContainsKey(itemId))
        {
            item[itemId]++;
            return item[itemId];
        }
        item.Add(itemId, 1);
        return 1;
    }

}
