using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class ArkkuTest {
    [Test]
    public void ArkkuTestTyyppi() {
        ArKKuTyyPPi.tyyppi tyyppi; // = ArKKuTyyPPi.tyyppi.ILMAINEN;
    }

	[Test]
	public void ArkkuTestTimeLeft() {
		// Use the Assert class to test conditions.
        ArKKu ilmainen = new ArKKu (ArKKuTyyPPi.tyyppi.ILMAINEN);
        ArKKu hopea = new ArKKu (ArKKuTyyPPi.tyyppi.HOPEA);
        ArKKu kulta = new ArKKu (ArKKuTyyPPi.tyyppi.KULTA);
        ArKKu jattilainen = new ArKKu (ArKKuTyyPPi.tyyppi.JATTILAIS);
        ArKKu eeppinen = new ArKKu (ArKKuTyyPPi.tyyppi.EEPPINEN);
        ArKKu legendaarinen = new ArKKu (ArKKuTyyPPi.tyyppi.LEGENDAARINEN);

        ilmainen.KaynnistaAika ();
        long left = ilmainen.aikaaJaljellaMinuutteina ();
        Assert.That(left >= 239 && left <= 240 );
        left = ilmainen.aikaaJaljellaSekuntteina ();
        Assert.That(left >= 14398 && left <= 14400 );

        hopea.KaynnistaAika ();
        left = hopea.aikaaJaljellaMinuutteina ();
        Assert.That(left >= 179 && left <= 180 );
        left = hopea.aikaaJaljellaSekuntteina ();
        Assert.That(left >= 10798 && left <= 10800 );

        kulta.KaynnistaAika ();
        left = kulta.aikaaJaljellaMinuutteina ();
        Assert.That(left >= 478 && left <= 480 );
        left = kulta.aikaaJaljellaSekuntteina ();
        Assert.That(left >= 28798 && left <= 28800 );

        jattilainen.KaynnistaAika ();
        left = jattilainen.aikaaJaljellaMinuutteina ();
        Assert.That(left >= 719 && left <= 720 );
        left = jattilainen.aikaaJaljellaSekuntteina ();
        Assert.That(left >= 43198 && left <= 43200 );

        eeppinen.KaynnistaAika ();
        left = eeppinen.aikaaJaljellaMinuutteina ();
        Assert.That(left >= 719 && left <= 720 );
        left = eeppinen.aikaaJaljellaSekuntteina ();
        Assert.That(left >= 43198 && left <= 43200 );

        legendaarinen.KaynnistaAika ();
        left = legendaarinen.aikaaJaljellaMinuutteina ();
        Assert.That(left >= 1439 && left <= 1440 );
        left = legendaarinen.aikaaJaljellaSekuntteina ();
        Assert.That(left >= 86398 && left <= 86400 );

        Assert.That (!ilmainen.voikoAvata ());
        Assert.That (!hopea.voikoAvata ());
        Assert.That (!kulta.voikoAvata ());
        Assert.That (!jattilainen.voikoAvata ());
        Assert.That (!eeppinen.voikoAvata ());
        Assert.That (!legendaarinen.voikoAvata ());
        }

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator ArkkuTestWithEnumeratorPasses() {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
	}
}
