namespace Tim_Fill_Empty_Palette;

public static class Pattern
{
	public static bool Match(byte[] buffer, int beginBuffer, byte[] pattern, params int[] skipIndex)
	{
		var viewer = new ReadOnlySpan<byte>(buffer, beginBuffer, pattern.Length);

		for (int i = 0; i < pattern.Length; ++i)
		{
			if (skipIndex.Contains(i))
			{
				continue;
			}

			if (viewer[i] != pattern[i])
			{
				return false;
			}
		}

		return true;
	}

	public static List<int> OccurenceOffsets(byte[] buffer, byte[] pattern, params int[] skipIndex)
	{
		var offsets = new List<int>();

		for (int i = 0; i < buffer.Length - pattern.Length; ++i)
		{
			if (Match(buffer, i, pattern, skipIndex))
			{
				offsets.Add(i);
				i += pattern.Length - 1;
			}
		}

		return offsets;
	}
}