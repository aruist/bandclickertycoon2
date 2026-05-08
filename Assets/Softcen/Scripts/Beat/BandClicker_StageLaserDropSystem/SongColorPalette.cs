using UnityEngine;

[CreateAssetMenu(menuName = "Band Clicker/Stage/Song Color Palette")]
public class SongColorPalette : ScriptableObject
{
    [Header("Song")]
    public string songId = "default";

    [Header("Main Colors")]
    public Color mainBeamColor = new Color(0.2f, 0.8f, 1f, 1f);
    public Color sideBeamColor = new Color(1f, 0.2f, 0.9f, 1f);
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
