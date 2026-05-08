using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace WhiteCat
{
	public static class Extensions
	{
		/// <summary>
		/// 安全获取组件. 如果物体上没有组件则自动添加
		/// </summary>
		public static T SafeGetComponent<T>(this GameObject target) where T : Component
		{
			T component = target.GetComponent<T>();
			if (!component) component = target.AddComponent<T>();
			return component;
		}


		/// <summary>
		/// 安全获取组件. 如果物体上没有组件则自动添加
		/// </summary>
		public static T SafeGetComponent<T>(this Component target) where T : Component
		{
			T component = target.GetComponent<T>();
			if (!component) component = target.gameObject.AddComponent<T>();
			return component;
		}


		/// <summary>
		/// 重置 Transform 的 localPosition, localRotation 和 localScale
		/// </summary>
		public static void ResetLocal(this Transform transform)
		{
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}


		/// <summary>
		/// (深度优先)遍历 Transform 层级, 对每一个节点执行一个自定义的操作
		/// </summary>
		/// <param name="root"> 遍历开始的根部 Transform 对象 </param>
		/// <param name="operate"> 遍历到每一个节点时将调用此方法 </param>
		/// <param name="depthLimit"> 访问深度限制, 负值表示不限制, 0 表示只访问 root 本身而不访问其子级, 正值表示最多访问的子级层数 </param>
		public static void TraverseHierarchy(this Transform root, Action<Transform> operate, int depthLimit = -1)
		{
			operate(root);
			if (depthLimit == 0) return;

			int count = root.childCount;

			for (int i = 0; i < count; i++)
			{
				TraverseHierarchy(root.GetChild(i), operate, depthLimit - 1);
			}
		}


		/// <summary>
		/// (深度优先)遍历 Transform 层级, 判断每一个节点是否为查找目标, 发现查找目标则立即终止查找
		/// </summary>
		/// <param name="root"> 遍历开始的根部 Transform 对象 </param>
		/// <param name="match"> 判断当前节点是否为查找目标 </param>
		/// <param name="depthLimit"> 遍历深度限制, 负值表示不限制, 0 表示只访问 root 本身而不访问其子级, 正值表示最多访问的子级层数 </param>
		/// <returns> 如果查找到目标则返回此目标; 否则返回 null </returns>
		public static Transform FindInHierarchy(this Transform root, Predicate<Transform> match, int depthLimit = -1)
		{
			if (match(root)) return root;
			if (depthLimit == 0) return null;

			int count = root.childCount;
			Transform result = null;

			for (int i = 0; i < count; i++)
			{
				result = FindInHierarchy(root.GetChild(i), match, depthLimit - 1);
				if (result) break;
			}

			return result;
		}


		/// <summary>
		/// 为 EventTrigger 添加事件
		/// </summary>
		public static void AddListener(this EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> callback)
		{
			var triggers = eventTrigger.triggers;
			var index = triggers.FindIndex(entry => entry.eventID == type);
			if (index < 0)
			{
				var entry = new EventTrigger.Entry();
				entry.eventID = type;
				entry.callback.AddListener(callback);
				triggers.Add(entry);
			}
			else
			{
				triggers[index].callback.AddListener(callback);
			}
		}


		/// <summary>
		/// 为 EventTrigger 移除事件
		/// </summary>
		public static void RemoveListener(this EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> callback)
		{
			var triggers = eventTrigger.triggers;
			var index = triggers.FindIndex(entry => entry.eventID == type);
			if (index >= 0)
			{
				triggers[index].callback.RemoveListener(callback);
			}
		}

	} // class Extensions

} // namespace WhiteCat