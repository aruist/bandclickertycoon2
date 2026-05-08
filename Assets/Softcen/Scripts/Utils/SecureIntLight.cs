using UnityEngine;

[System.Serializable]
public struct SecureIntLight
{
    [SerializeField] private int _obscuredValue;
    [SerializeField] private int _checksum;
    [SerializeField] private bool _isTampered;

    // Static keys shared across all instances to save memory/processing
    private static int _sessionXorKey;
    private static int _sessionChecksumKey;
    private static bool _keysInitialized;

    public int Value => Decode();

    public SecureIntLight(int initialValue)
    {
        _obscuredValue = 0;
        _checksum = 0;
        _isTampered = false;
        Encode(initialValue);
    }

    public bool TryGrant(int amount)
    {
        if (amount < 0) return false;

        int current = Decode();
        // Check for overflow
        if (amount > int.MaxValue - current) return false;

        Encode(current + amount);
        return true;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;

        int current = Decode();
        if (current < amount) return false;

        Encode(current - amount);
        return true;
    }

    public void ForceSet(int value)
    {
        if (value < 0)
        {
            value = 0;
        }

        _isTampered = false;
        Encode(value);
    }

    private void Encode(int val)
    {
        EnsureKeys();
        _obscuredValue = val ^ _sessionXorKey;
        _checksum = val ^ _sessionChecksumKey;
    }

    private int Decode()
    {
        EnsureKeys();
        int decoded = _obscuredValue ^ _sessionXorKey;

        // Integrity check: Does the checksum match the decoded value?
        if ((decoded ^ _sessionChecksumKey) != _checksum)
        {
            if (!_isTampered)
            {
                Debug.LogError("Memory Tampering Detected!");
                _isTampered = true;
            }
            return 0;
        }
        return decoded;
    }

    private static void EnsureKeys()
    {
        if (_keysInitialized) return;
        // Fast randomization once per game session
        _sessionXorKey = Random.Range(0x1000, 0x7FFF);
        // Simple do-while loop ensures they are never the same
        do {
            _sessionChecksumKey = Random.Range(0x1000, 0x7FFF);
        } while (_sessionChecksumKey == _sessionXorKey);

        _keysInitialized = true;
    }

    // Overload operators to make it act like a normal int
    public static implicit operator int(SecureIntLight s) => s.Value;
    public static implicit operator SecureIntLight(int i) => new SecureIntLight(i);
}