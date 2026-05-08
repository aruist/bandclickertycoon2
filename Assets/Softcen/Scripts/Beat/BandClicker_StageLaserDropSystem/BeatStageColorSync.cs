using UnityEngine;

public enum StageLightGroup
{
    MainBeam,
    SideBeam,
    Laser,
    Drop
}

public class BeatStageColorSync : MonoBehaviour
{
    public static BeatStageColorSync Instance { get; private set; }

    [SerializeField] private SongColorPalette currentPalette;

    public SongColorPalette CurrentPalette => currentPalette;

    private void Awake()
    {
        Instance = this;
    }

    public void SetPalette(SongColorPalette palette)
    {
        currentPalette = palette;
    }

    public Gradient GetLaserColorGradient(int index = 0)
    {
        if (currentPalette == null)
            return null;

        return currentPalette.GetLaserColorGradient(index);
    }

    public Color GetColor(StageLightGroup group, int index = 0)
    {
        if (currentPalette == null)
            return Color.white;

        switch (group)
        {
            case StageLightGroup.MainBeam:
                return currentPalette.mainBeamColor;

            case StageLightGroup.SideBeam:
                return currentPalette.sideBeamColor;

            case StageLightGroup.Laser:
                return currentPalette.GetLaserColor(index);

            case StageLightGroup.Drop:
                return currentPalette.dropColor;

            default:
                return Color.white;
        }
    }
}
