using UnityEngine;

[CreateAssetMenu(menuName = "Band Clicker/Stage/Song Color Palette")]
public class SongColorPalette : ScriptableObject
{
    [Header("Song")]
    public string songId = "default";

    [Header("Main Colors")]
    public Color[] mainBeamColors; // = new Color(0.2f, 0.8f, 1f, 1f);
    public Color[] mainBeamPulseColors; // = new Color(0.2f, 0.8f, 1f, 1f);
    public Color[] sideBeamColors; // = new Color(1f, 0.2f, 0.9f, 1f);
    public Color[] sideBeamPulseColors; // = new Color(1f, 0.2f, 0.9f, 1f);
    public Color laserColor = new Color(0.2f, 1f, 0.5f, 1f);
    public Gradient laserGradientColor;
    public Color dropColor = Color.white;

    [Header("Optional Alternating Laser Colors")]
    public Color[] laserAccentColors =
    {
        new Color(0.2f, 1f, 0.5f, 1f),
        new Color(0.2f, 0.8f, 1f, 1f),
        new Color(1f, 0.2f, 0.9f, 1f)
    };

    public Gradient[] laserGradientColors;

    public Color GetMainBeamColor(int index)
    {
        if(mainBeamColors == null || mainBeamColors.Length <= 0) return Color.white;
        if (index < 0 || index >= mainBeamColors.Length) return Color.white;
        return mainBeamColors[index];
    }
    public Color GetMainBeamPulseColor(int index)
    {
        if(mainBeamPulseColors == null || mainBeamPulseColors.Length <= 0) return Color.white;
        if (index < 0 || index >= mainBeamPulseColors.Length) return Color.white;
        return mainBeamPulseColors[index];
    }
    public Color GetSideBeamColor(int index)
    {
        if(sideBeamColors == null || sideBeamColors.Length <= 0) return Color.white;
        if (index < 0 || index >= sideBeamColors.Length) return Color.white;
        return sideBeamColors[index];
    }
    public Color GetSideBeamPulseColor(int index)
    {
        if(sideBeamPulseColors == null || sideBeamPulseColors.Length <= 0) return Color.white;
        if (index < 0 || index >= sideBeamPulseColors.Length) return Color.white;
        return sideBeamPulseColors[index];
    }

    public Color GetLaserColor(int index)
    {
        if (laserAccentColors == null || laserAccentColors.Length == 0)
            return laserColor;

        return laserAccentColors[Mathf.Abs(index) % laserAccentColors.Length];
    }

    public Gradient GetLaserColorGradient(int index)
    {
        if (laserGradientColors == null || laserGradientColors.Length == 0)
            return laserGradientColor;

        return laserGradientColors[Mathf.Abs(index) % laserGradientColors.Length];
    }

}
