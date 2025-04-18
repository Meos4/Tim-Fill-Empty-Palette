namespace Tim_Fill_Empty_Palette;

public class TimHeader
{
	enum PaletteType : uint
	{
		_4Bits = 0x08,
		_8Bits = 0x09,
		_16Bits = 0x02
	}

	public const uint DefaultPaletteOffset = 0x0C;
	public const int HeaderSize = 20;

	public uint Magic { get; }
	public uint Type { get; }
	public uint Offset { get; }
	public short PaletteX { get; }
	public short PaletteY { get; }
	public short PaletteColors { get; }
	public short NbPalettes { get; }

	public static bool IsSupportedPaletteType(uint type) => type == (uint)PaletteType._4Bits || type == (uint)PaletteType._8Bits;

	public static uint ExpectedPaletteSize(uint paletteType)
	{
		return paletteType switch
		{
			(uint)PaletteType._4Bits => 16,
			(uint)PaletteType._8Bits => 512,
			_ => throw new ArgumentOutOfRangeException(nameof(paletteType), "Unknown palette type.")
		};
	}

	private TimHeader(ReadOnlySpan<byte> viewer)
	{
		Magic = BitConverter.ToUInt32(viewer.Slice(0, sizeof(uint)));
		Type = BitConverter.ToUInt32(viewer.Slice(4, sizeof(uint)));
		Offset = BitConverter.ToUInt32(viewer.Slice(8, sizeof(uint)));
		PaletteX = BitConverter.ToInt16(viewer.Slice(12, sizeof(short)));
		PaletteY = BitConverter.ToInt16(viewer.Slice(14, sizeof(short)));
		PaletteColors = BitConverter.ToInt16(viewer.Slice(16, sizeof(short)));
		NbPalettes = BitConverter.ToInt16(viewer.Slice(18, sizeof(short)));
	}

	public static TimHeader? Create(ReadOnlySpan<byte> viewer)
	{
		if (viewer.Length < HeaderSize)
		{
			throw new ArgumentException("Viewer is too small to create a valid .TIM header");
		}

		var timHeader = new TimHeader(viewer);

		if (!IsSupportedPaletteType(timHeader.Type))
		{
			return null;
		}

		var paletteSize = timHeader.Offset & ~DefaultPaletteOffset;
		var validPaletteSize = (paletteSize == 0 || paletteSize == timHeader.ExpectedPaletteSize());

		if (timHeader.Magic == 0x10 && validPaletteSize)
		{
			return timHeader;
		}

		return null;
	}

	public bool IsEmptyPalette()
	{
		return Offset <= DefaultPaletteOffset;
	}

	public uint PaletteSize()
	{
		return Offset - DefaultPaletteOffset;
	}

	public uint ExpectedPaletteSize()
	{
		return (uint)(ExpectedPaletteSize(Type) * NbPalettes);
	}
}