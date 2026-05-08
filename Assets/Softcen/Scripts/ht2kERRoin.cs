using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ht2kERRoin : ES2Type
{
    public ht2kERRoin() : base(typeof(keRrOIn))
	{
    }

    public override void Write(object obj, ES2Writer writer)
    {
        keRrOIn data = (keRrOIn)obj;
        writer.Write(data.versio);
        writer.Write(data.KerroinArvo);
        writer.Write(data.AloitusAika);
        writer.Write(data.Kesto);
        data.Muutettu = false;
    }

    public override object Read(ES2Reader reader)
    {
        keRrOIn data = new keRrOIn();
        Read(reader, data);
        return data;
    }

    public override void Read(ES2Reader reader, object c)
    {
        keRrOIn data = (keRrOIn)c;
        int versio = reader.Read<int>();
        if (versio >= 0)
        {
            double ka = reader.Read<double>();
            long aa = reader.Read<long>();
            long ke = reader.Read<long>();
            data.aSEtaKErroIN(ka, aa, ke);
            data.Muutettu = false;
        }
    }
}
