using System.Collections;

namespace Softcen.Clicker.Core
{
    public interface IScenePrewarmProvider
    {
        IEnumerator Prewarm();
    }
}
