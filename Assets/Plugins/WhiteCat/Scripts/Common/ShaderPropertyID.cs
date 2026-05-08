using UnityEngine;

namespace WhiteCat
{
    /// <summary>
    /// 快速访问 Unity 内置 shader property id (限主线程访问)
    /// </summary>
	public struct ShaderPropertyID
	{
		public readonly static int mainTex = Shader.PropertyToID("_MainTex");
		public readonly static int color = Shader.PropertyToID("_Color");
		public readonly static int emissionColor = Shader.PropertyToID("_EmissionColor");
	}
}