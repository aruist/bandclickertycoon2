using System;
using UnityEngine;

[System.Serializable]
public struct SecureDoubleLight
{
    [SerializeField] private long _obscuredValue;
    [SerializeField] private long _checksum;
    [SerializeField] private bool _isTampered;

    // Static keys shared across all instances to save memory/processing
    private static long _sessionXorKey;
    private static long _sessionChecksumKey;
    private static bool _keysInitialized;

    public double Value => Decode();

    public SecureDoubleLight(double initialValue)
    {
        _obscuredValue = 0L;
        _checksum = 0L;
        _isTampered = false;
        Encode(initialValue);
    }

    public bool TryGrant(double amount)
    {
        if (amount < 0d || double.IsNaN(amount) || double.IsInfinity(amount)) return false;

        double current = Decode();
        if (double.IsNaN(current) || double.IsInfinity(current)) return false;

        double result = current + amount;
        if (double.IsInfinity(result) || double.IsNaN(result)) return false;

        Encode(result);
        return true;
    }

    public bool TrySpend(double amount)
    {
        if (amount < 0d || double.IsNaN(amount) || double.IsInfinity(amount)) return false;

        double current = Decode();
        if (double.IsNaN(current) || double.IsInfinity(current)) return false;
        if (current < amount) return false;

        Encode(current - amount);
        return true;
    }

    public void ForceSet(double value)
    {
        if (value < 0d || double.IsNaN(value) || double.IsInfinity(value))
        {
            value = 0d;
        }

        _isTampered = false;
        Encode(value);
    }

    private void Encode(double val)
    {
        EnsureKeys();

        long rawValue = BitConverter.DoubleToInt64Bits(val);
        _obscuredValue = rawValue ^ _sessionXorKey;
        _checksum = rawValue ^ _sessionChecksumKey;
    }

    private double Decode()
    {
        EnsureKeys();

        long rawValue = _obscuredValue ^ _sessionXorKey;

        // Integrity check: Does the checksum match the decoded value?
        if ((rawValue ^ _sessionChecksumKey) != _checksum)
        {
            if (!_isTampered)
            {
                Debug.LogError("Memory Tampering Detected!");
                _isTampered = true;
            }
            return 0d;
        }

        return BitConverter.Int64BitsToDouble(rawValue);
    }

    private static void EnsureKeys()
    {
        if (_keysInitialized) return;

        // Fast randomization once per game session.
        // UnityEngine.Random.Range only supports int reliably, so combine two ints into one long key.
        _sessionXorKey = CreateRandomLongKey();

        // Simple do-while loop ensures they are never the same
        do
        {
            _sessionChecksumKey = CreateRandomLongKey();
        } while (_sessionChecksumKey == _sessionXorKey);

        _keysInitialized = true;
    }

    private static long CreateRandomLongKey()
    {
        int high = UnityEngine.Random.Range(0x1000, 0x7FFF);
        int low = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        return ((long)high << 32) ^ (uint)low;
    }

    // Overload operators to make it act like a normal double
    public static implicit operator double(SecureDoubleLight s) => s.Value;
    public static implicit operator SecureDoubleLight(double d) => new SecureDoubleLight(d);
}
