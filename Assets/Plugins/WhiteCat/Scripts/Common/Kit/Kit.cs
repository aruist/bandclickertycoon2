using System.Collections;

namespace WhiteCat
{
	/// <summary>
	/// 常用方法
	/// </summary>
	public partial struct Kit
	{
		/// <summary>
		/// 全局 Random 对象, 用以执行常规随机任务
		/// </summary>
		public static readonly Random random = new Random();


		/// <summary>
		/// 交换两个变量的值
		/// </summary>
		public static void Swap<T>(ref T a, ref T b)
		{
			T c = a;
			a = b;
			b = c;
		}


		/// <summary>
		/// 判断集合是否为 null 或元素个数是否为 0
		/// </summary>
		public static bool IsNullOrEmpty(ICollection collection)
		{
			return collection == null || collection.Count == 0;
		}

	} // struct Kit

} // namespace WhiteCat