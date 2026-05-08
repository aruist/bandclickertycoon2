
using System;

public class ajattieto  {
    public int version = 2;

    public int _ilmainenLaskuri;
    public long _ilmainenArkkuAlku;

    private DateTime mLATausAikA;
    private TimeSpan mPElaTTuAIka;

    public ajattieto()
    {
        alusta();
    }

    private void alusta()
    {
        ASetAPelaTTuAIka(0);
        _ilmainenLaskuri = 0;
        _ilmainenArkkuAlku = -1;
    }

    public double PElaTTuAiKA()
    {
        return KOkonaISPeLAtTuAIka.TotalMilliseconds;
    }

    public void ASetAPelaTTuAIka(double val)
    {
        mPElaTTuAIka = TimeSpan.FromMilliseconds(val > 0f ? val : 0f);
        mLATausAikA = DateTime.UtcNow;
    }

    public TimeSpan KOkonaISPeLAtTuAIka
    {
        get
        {
            TimeSpan erotus = DateTime.UtcNow.Subtract(mLATausAikA);
            return mPElaTTuAIka.Add(erotus);
        }
    }

}
