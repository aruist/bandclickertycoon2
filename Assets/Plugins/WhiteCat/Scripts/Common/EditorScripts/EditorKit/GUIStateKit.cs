#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace WhiteCatEditor
{
	/// <summary>
	/// Hot Control 事件
	/// </summary>
	public enum HotControlEvent
	{
		None,
		MouseDown,
		MouseUp,
	}


	/// <summary>
	/// 编辑器 GUI 状态相关工具
	/// 注意: Begin 和 End 方法不支持嵌套调用
	/// </summary>
	public partial struct EditorKit
	{
		static GUIStyle _buttonStyle;
		static GUIStyle _buttonLeftStyle;
		static GUIStyle _buttonMiddleStyle;
		static GUIStyle _buttonRightStyle;
		static GUIStyle _centeredBoldLabelStyle;

		static int _lastRecordedHotControl;
		static float _lastRecordedLabelWidth;
		static bool _lastRecordedWideMode;
		static Color _lastRecordedContentColor;
		static Color _lastRecordedBackgroundColor;
		static Color _lastRecordedColor;
		static Color _lastRecordedHandlesColor;
		static Matrix4x4 _lastHandlesMatrix;

		static ColorPickerHDRConfig _colorPickerHDRConfig;


		/// <summary>
		/// 按钮 GUIStyle
		/// </summary>
		public static GUIStyle buttonStyle
		{
			get
			{
				if (_buttonStyle == null) _buttonStyle = "Button";
				return _buttonStyle;
			}
		}


		/// <summary>
		/// 左侧按钮 GUIStyle
		/// </summary>
		public static GUIStyle buttonLeftStyle
		{
			get
			{
				if (_buttonLeftStyle == null) _buttonLeftStyle = "ButtonLeft";
				return _buttonLeftStyle;
			}
		}


		/// <summary>
		/// 中部按钮 GUIStyle
		/// </summary>
		public static GUIStyle buttonMiddleStyle
		{
			get
			{
				if (_buttonMiddleStyle == null) _buttonMiddleStyle = "ButtonMid";
				return _buttonMiddleStyle;
			}
		}


		/// <summary>
		/// 右侧按钮 GUIStyle
		/// </summary>
		public static GUIStyle buttonRightStyle
		{
			get
			{
				if (_buttonRightStyle == null) _buttonRightStyle = "ButtonRight";
				return _buttonRightStyle;
			}
		}


		/// <summary>
		/// 居中且加粗的 Label
		/// </summary>
		public static GUIStyle centeredBoldLabelStyle
		{
			get
			{
				if (_centeredBoldLabelStyle == null)
				{
					_centeredBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
					_centeredBoldLabelStyle.alignment = TextAnchor.MiddleCenter;
				}
				return _centeredBoldLabelStyle;
			}
		}


		/// <summary>
		/// 编辑器默认内容颜色 (文本, 按钮图片等)
		/// </summary>
		public static Color defaultContentColor
		{
			get { return EditorStyles.label.normal.textColor; }
		}


		/// <summary>
		/// 编辑器默认背景颜色
		/// </summary>
		public static Color defaultBackgroundColor
		{
			get
			{
				float rgb = EditorGUIUtility.isProSkin ? 56f : 194f;
				rgb /= 255f;
				return new Color(rgb, rgb, rgb, 1f);
			}
		}


		/// <summary>
		/// HDR 拾色器设置
		/// </summary>
		public static ColorPickerHDRConfig colorPickerHDRConfig
		{
			get
			{
				if (_colorPickerHDRConfig == null)
				{
					_colorPickerHDRConfig = new ColorPickerHDRConfig(0f, 8f, 0.125f, 3f);
				}
				return _colorPickerHDRConfig;
			}
		}


		/// <summary>
		/// 在绘制控件之前调用, 用以检查控件是否被鼠标选中
		/// </summary>
		public static void BeginHotControlChangeCheck()
		{
			_lastRecordedHotControl = GUIUtility.hotControl;
		}


		/// <summary>
		/// 在绘制控件之后调用, 返回该控件被鼠标选中的事件
		/// </summary>
		public static HotControlEvent EndHotControlChangeCheck()
		{
			if (_lastRecordedHotControl == GUIUtility.hotControl)
			{
				return HotControlEvent.None;
			}

			return GUIUtility.hotControl == 0 ? HotControlEvent.MouseUp : HotControlEvent.MouseDown;
		}


		/// <summary>
		/// 记录并设置 LabelWidth
		/// </summary>
		public static void BeginLabelWidth(float newWidth)
		{
			_lastRecordedLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = newWidth;
		}


		/// <summary>
		/// 恢复 LabelWidth
		/// </summary>
		public static void EndLabelWidth()
		{
			EditorGUIUtility.labelWidth = _lastRecordedLabelWidth;
		}


		/// <summary>
		/// 记录并设置 WideMode
		/// </summary>
		public static void BeginWideMode(bool newWideMode)
		{
			_lastRecordedWideMode = EditorGUIUtility.wideMode;
			EditorGUIUtility.wideMode = newWideMode;
		}


		/// <summary>
		/// 恢复 WideMode
		/// </summary>
		public static void EndWideMode()
		{
			EditorGUIUtility.wideMode = _lastRecordedWideMode;
		}


		/// <summary>
		/// 记录并设置 ContentColor
		/// </summary>
		public static void BeginGUIContentColor(Color newColor)
		{
			_lastRecordedContentColor = GUI.contentColor;
			GUI.contentColor = newColor;
		}


		/// <summary>
		/// 恢复 BackgroundColor
		/// </summary>
		public static void EndGUIContentColor()
		{
			GUI.contentColor = _lastRecordedContentColor;
		}


		/// <summary>
		/// 记录并设置 BackgroundColor
		/// </summary>
		public static void BeginGUIBackgroundColor(Color newColor)
		{
			_lastRecordedBackgroundColor = GUI.backgroundColor;
			GUI.backgroundColor = newColor;
		}


		/// <summary>
		/// 恢复 BackgroundColor
		/// </summary>
		public static void EndGUIBackgroundColor()
		{
			GUI.backgroundColor = _lastRecordedBackgroundColor;
		}


		/// <summary>
		/// 记录并设置 Color
		/// </summary>
		public static void BeginGUIColor(Color newColor)
		{
			_lastRecordedColor = GUI.color;
			GUI.color = newColor;
		}


		/// <summary>
		/// 恢复 Color
		/// </summary>
		public static void EndGUIColor()
		{
			GUI.color = _lastRecordedColor;
		}


		/// <summary>
		/// 记录并设置 Handles.color
		/// </summary>
		public static void BeginHandlesColor(Color newColor)
		{
			_lastRecordedHandlesColor = Handles.color;
			Handles.color = newColor;
		}


		/// <summary>
		/// 恢复 Handles.color
		/// </summary>
		public static void EndHandlesColor()
		{
			Handles.color = _lastRecordedHandlesColor;
		}


		/// <summary>
		/// 记录并设置 Handles.matrix
		/// </summary>
		public static void BeginHandlesMatrix(ref Matrix4x4 newMatrix)
		{
			_lastHandlesMatrix = Handles.matrix;
			Handles.matrix = newMatrix;
		}


		/// <summary>
		/// 记录并设置 Handles.matrix
		/// </summary>
		public static void BeginHandlesMatrix(Matrix4x4 newMatrix)
		{
			_lastHandlesMatrix = Handles.matrix;
			Handles.matrix = newMatrix;
		}


		/// <summary>
		/// 恢复 Handles.matrix
		/// </summary>
		public static void EndHandlesMatrix()
		{
			Handles.matrix = _lastHandlesMatrix;
		}

	} // struct EditorKit

} // namespace WhiteCatEditor

#endif // UNITY_EDITOR