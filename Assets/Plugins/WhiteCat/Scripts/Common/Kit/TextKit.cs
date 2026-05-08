
namespace WhiteCat
{
	/// <summary>
	/// 文本相关的方法
	/// </summary>
	public partial struct Kit
	{
		/// <summary>
		/// 判断一个字符是否为阿拉伯数字字符
		/// </summary>
		public static bool IsDigit(char c)
		{
			return c >= '0' && c <= '9';
		}


		/// <summary>
		/// 判断一个字符是否为英文小写字母
		/// </summary>
		public static bool IsEnglishLower(char c)
		{
			return c >= 'a' && c <= 'z';
		}


		/// <summary>
		/// 判断一个字符是否为英文大写字母
		/// </summary>
		public static bool IsEnglishUpper(char c)
		{
			return c >= 'A' && c <= 'Z';
		}


		/// <summary>
		/// 判断一个字符是否为英文字母
		/// </summary>
		public static bool IsEnglishLetter(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		}


		/// <summary>
		/// 找到第一个空白字符
		/// </summary>
		public static int IndexOfWhiteSpace(string s, int startIndex = 0)
		{
			while (startIndex < s.Length)
			{
				if (char.IsWhiteSpace(s, startIndex))
				{
					return startIndex;
				}
				startIndex++;
			}
			return -1;
		}


		/// <summary>
		/// 找到第一个非空白字符
		/// </summary>
		public static int IndexOfNonWhiteSpace(string s, int startIndex = 0)
		{
			while (startIndex < s.Length)
			{
				if (!char.IsWhiteSpace(s, startIndex))
				{
					return startIndex;
				}
				startIndex++;
			}
			return -1;
		}


		/// <summary>
		/// 找到最后一个空白字符
		/// </summary>
		public static int LastIndexOfWhiteSpace(string s, int startIndex)
		{
			while (startIndex >= 0)
			{
				if (char.IsWhiteSpace(s, startIndex))
				{
					return startIndex;
				}
				startIndex--;
			}
			return -1;
		}


		/// <summary>
		/// 找到最后一个非空白字符
		/// </summary>
		public static int LastIndexOfNonWhiteSpace(string s, int startIndex)
		{
			while (startIndex >= 0)
			{
				if (!char.IsWhiteSpace(s, startIndex))
				{
					return startIndex;
				}
				startIndex--;
			}
			return -1;
		}

	} // struct Kit

} // namespace WhiteCat