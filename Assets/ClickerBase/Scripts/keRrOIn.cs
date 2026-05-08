using System;
#if SC_OBFUS
using Beebyte.Obfuscator;
#endif

public class keRrOIn {
    public static event Action OnkeRrOInMuuTTUnut;

    public int versio = 1;
    double _value;
    long _startTicks;
    long _duration;
    bool _muutettu = false;

    public keRrOIn()
    {
        alusta();
    }

    public double KerroinArvo { get { return _value; } }
    public long AloitusAika { get { return _startTicks; } }
    public long Kesto { get { return _duration; } }
    public bool Muutettu { get { return _muutettu; } set { _muutettu = value; } }

    public void alusta()
    {
        _value = 0;
        _startTicks = 0;
        _duration = 0;
        _muutettu = true;
    }

    public void aSEtaKErroIN(double val, long start, long kesto)
    {
        _value = val;
        _startTicks = start;
        _duration = kesto;
        _muutettu = true;
        if (OnkeRrOInMuuTTUnut != null)
            OnkeRrOInMuuTTUnut();
    }

    public void noLLaaKerroin()
    {
        alusta();
        if (OnkeRrOInMuuTTUnut != null)
            OnkeRrOInMuuTTUnut();
    }

    public long anNAJaljellaOLEvaAIka()
    {
        return _duration - (DateTime.UtcNow.Ticks - _startTicks);
    }
}
