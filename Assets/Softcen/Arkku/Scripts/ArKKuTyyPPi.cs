[System.Serializable]
public class ArKKuTyyPPi {
    public enum tyyppi {
        TYHJA = 0,
        ILMAINEN = 1,
        HOPEA = 2,
        KULTA = 3,
        JATTILAIS = 4,
        EEPPINEN = 5,
        LEGENDAARINEN = 6
    };

    public static int toInt(tyyppi t) {
        return (int)t;
    }

    public static tyyppi fromInt(int i) {
        switch (i) {
        case 1:
            return tyyppi.ILMAINEN;
        case 2:
            return tyyppi.HOPEA;
        case 3:
            return tyyppi.KULTA;
        case 4:
            return tyyppi.JATTILAIS;
        case 5:
            return tyyppi.EEPPINEN;
        case 6:
            return tyyppi.LEGENDAARINEN;
        default:
            return tyyppi.TYHJA;
        }
    }

    public static long aika(tyyppi t) {
        long aika;
        switch(t) {
        case tyyppi.ILMAINEN:
            aika = 240;
            break;
        case tyyppi.HOPEA:
            aika = 180;
            break;
        case tyyppi.KULTA:
            aika = 480;
            break;
        case tyyppi.JATTILAIS:
            aika = 720;
            break;
        case tyyppi.EEPPINEN:
            aika = 720;
            break;
        case tyyppi.LEGENDAARINEN:
            aika = 1440;
            break;
        default:            
            aika = 0;
            break;
        }

        return aika * System.TimeSpan.TicksPerMinute;

    }
}
